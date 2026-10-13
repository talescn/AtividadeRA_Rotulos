using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Vuforia;
using Debug = UnityEngine.Debug;

// Procura codigos de barras na imagem da camera do Vuforia enquanto Ativo: varias vezes por segundo
// e fora da thread principal, para nao travar a RA. Avisa quando confirma um GTIN.
public class LeitorCodigoBarras : MonoBehaviour
{
    [Tooltip("Intervalo minimo entre duas leituras, em segundos (a proxima so comeca quando a anterior termina).")]
    public float intervalo = 0.1f;

    [Tooltip("Espera depois de ligado pela primeira vez, antes de pedir a imagem da camera ao Vuforia.")]
    public float esperaInicial = 1f;

    // Diagnostico no log: a cada tantos segundos, quantos quadros foram lidos e quanto tempo levaram
    const float IntervaloRelatorio = 5f;
    // Diagnostico: com este arquivo em persistentDataPath, alguns quadros da camera vao para Quadros/*.pgm
    const string ArquivoSalvarQuadros = "salvar_quadros";
    const int MaximoQuadrosSalvos = 40;
    const float IntervaloQuadrosSalvos = 0.5f;

    // Ligado pela interface quando os marcadores ja foram criados, nenhum rotulo esta na camera
    // e o modo leitura esta fechado
    public bool Ativo { get; set; }

    public event System.Action<string> AoLerCodigo;

    struct Leitura
    {
        public string codigo;
        public double milissegundos;
    }

    readonly LeituraEstavel confirmacao = new LeituraEstavel();
    PixelFormat formato = PixelFormat.UNKNOWN_FORMAT;
    int bytesPorPixel;
    bool registrado;
    float ligadoEm = -1f;
    float proximaLeitura;
    Task<Leitura> leituraEmAndamento;

    bool imagemDescrita;
    bool falhaRegistrada;
    int quadros, quadrosComCodigo, quadrosSemImagem;
    double msTotal, msMaximo;
    float proximoRelatorio;
    bool salvarQuadros;
    int quadrosSalvos;
    float proximoQuadroSalvo;

    void Start()
    {
        VuforiaApplication.Instance.OnVuforiaStopped += AoPararVuforia;
    }

    void OnDestroy()
    {
        if (VuforiaApplication.Instance != null)
            VuforiaApplication.Instance.OnVuforiaStopped -= AoPararVuforia;
        if (registrado && VuforiaBehaviour.Instance != null)
            VuforiaBehaviour.Instance.World.OnStateUpdated -= AoAtualizarEstado;
    }

    // So na primeira vez que o leitor e ligado, com o Vuforia rodando e os marcadores ja criados
    void Registrar()
    {
        registrado = true;
        // O leitor so precisa de cinza; se a camera nao entregar, usa RGB
        var camera = VuforiaBehaviour.Instance.CameraDevice;
        if (camera.SetFrameFormat(PixelFormat.GRAYSCALE, true))
        {
            formato = PixelFormat.GRAYSCALE;
            bytesPorPixel = 1;
        }
        else if (camera.SetFrameFormat(PixelFormat.RGB888, true))
        {
            formato = PixelFormat.RGB888;
            bytesPorPixel = 3;
        }
        else
        {
            Debug.LogWarning("[RA] A camera nao entrega imagem para o leitor de codigo de barras.");
            return;
        }
        // A documentacao do Vuforia pega a imagem da camera no fim de cada atualizacao do estado
        VuforiaBehaviour.Instance.World.OnStateUpdated += AoAtualizarEstado;
        proximoRelatorio = Time.unscaledTime + IntervaloRelatorio;
        salvarQuadros = File.Exists(Path.Combine(Application.persistentDataPath, ArquivoSalvarQuadros));
        Debug.Log($"[RA] Leitor de codigo de barras usando a imagem {formato}" +
                  (salvarQuadros ? ", salvando quadros em " + Path.Combine(Application.persistentDataPath, "Quadros") : ""));
    }

    void AoPararVuforia()
    {
        if (registrado && VuforiaBehaviour.Instance != null)
            VuforiaBehaviour.Instance.World.OnStateUpdated -= AoAtualizarEstado;
        registrado = false;
        ligadoEm = -1f;
        formato = PixelFormat.UNKNOWN_FORMAT;
    }

    void Update()
    {
        // O Vuforia pode seguir processando os marcadores recem-criados; o registro espera um pouco
        if (Ativo && !registrado && VuforiaApplication.Instance.IsRunning)
        {
            if (ligadoEm < 0f) ligadoEm = Time.unscaledTime;
            if (Time.unscaledTime - ligadoEm >= esperaInicial) Registrar();
        }

        if (registrado && Ativo && Time.unscaledTime >= proximoRelatorio) Relatar();

        // Resultado da leitura que estava rodando em outra thread
        if (leituraEmAndamento == null || !leituraEmAndamento.IsCompleted) return;
        var tarefa = leituraEmAndamento;
        leituraEmAndamento = null;
        if (tarefa.IsFaulted)
        {
            // Uma falha no decodificador nao pode deixar o leitor mudo sem aviso
            if (!falhaRegistrada) Debug.LogWarning("[RA] Falha ao decodificar: " + tarefa.Exception.GetBaseException().Message);
            falhaRegistrada = true;
            return;
        }
        Leitura leitura = tarefa.Result;
        Contabilizar(leitura);
        string confirmado = confirmacao.Registrar(leitura.codigo, Time.unscaledTime);
        if (confirmado != null && Ativo)
        {
            Debug.Log("[RA] Codigo de barras lido: " + confirmado);
            AoLerCodigo?.Invoke(confirmado);
        }
    }

    void AoAtualizarEstado()
    {
        if (!Ativo || leituraEmAndamento != null || formato == PixelFormat.UNKNOWN_FORMAT) return;
        if (Time.unscaledTime < proximaLeitura) return;
        proximaLeitura = Time.unscaledTime + intervalo;

        Image imagem = VuforiaBehaviour.Instance.CameraDevice.GetCameraImage(formato);
        // Pixels ja e uma copia: a outra thread nao toca em nada do Vuforia nem do Unity
        byte[] pixels = imagem != null && imagem.Width > 0 && imagem.Height > 0 ? imagem.Pixels : null;
        if (pixels == null || pixels.Length == 0)
        {
            quadrosSemImagem++;
            return;
        }
        int largura = imagem.Width, altura = imagem.Height, stride = imagem.Stride, bpp = bytesPorPixel;
        if (!imagemDescrita)
        {
            imagemDescrita = true;
            Debug.Log($"[RA] Imagem da camera para o leitor: {largura}x{altura}, stride {stride}, {formato}");
        }
        if (salvarQuadros) SalvarQuadro(pixels, largura, altura, stride, bpp);
        leituraEmAndamento = Task.Run(() => Decodificar(pixels, largura, altura, stride, bpp));
    }

    static Leitura Decodificar(byte[] pixels, int largura, int altura, int stride, int bpp)
    {
        var relogio = Stopwatch.StartNew();
        string codigo = DecodificadorBarras.Decodificar(pixels, largura, altura, stride, bpp);
        return new Leitura { codigo = codigo, milissegundos = relogio.Elapsed.TotalMilliseconds };
    }

    void Contabilizar(Leitura leitura)
    {
        quadros++;
        if (leitura.codigo != null) quadrosComCodigo++;
        msTotal += leitura.milissegundos;
        msMaximo = System.Math.Max(msMaximo, leitura.milissegundos);
    }

    // Quantos quadros o leitor processou desde o ultimo relatorio, quanto tempo levaram e quantas
    // vezes a camera nao entregou imagem (se so houver estes, o registro da imagem se perdeu)
    void Relatar()
    {
        Debug.Log($"[RA] Leitor: {quadros} quadros, {(quadros > 0 ? msTotal / quadros : 0):0} ms em media " +
                  $"(max {msMaximo:0}), {quadrosComCodigo} com codigo, {quadrosSemImagem} sem imagem");
        quadros = quadrosComCodigo = quadrosSemImagem = 0;
        msTotal = msMaximo = 0;
        proximoRelatorio = Time.unscaledTime + IntervaloRelatorio;
    }

    void SalvarQuadro(byte[] pixels, int largura, int altura, int stride, int bpp)
    {
        if (quadrosSalvos >= MaximoQuadrosSalvos || Time.unscaledTime < proximoQuadroSalvo) return;
        proximoQuadroSalvo = Time.unscaledTime + IntervaloQuadrosSalvos;
        string pasta = Path.Combine(Application.persistentDataPath, "Quadros");
        string caminho = Path.Combine(pasta, $"quadro_{quadrosSalvos++:000}.pgm");
        Task.Run(() =>
        {
            try
            {
                Directory.CreateDirectory(pasta);
                File.WriteAllBytes(caminho, Pgm(pixels, largura, altura, stride, bpp));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[RA] Nao foi possivel salvar {caminho}: {e.Message}");
            }
        });
    }

    // PGM binario (P5): cabecalho de texto e um byte de cinza por pixel; em RGB, a media dos canais
    static byte[] Pgm(byte[] pixels, int largura, int altura, int stride, int bpp)
    {
        byte[] cabecalho = System.Text.Encoding.ASCII.GetBytes($"P5\n{largura} {altura}\n255\n");
        var saida = new byte[cabecalho.Length + largura * altura];
        cabecalho.CopyTo(saida, 0);
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
            {
                int i = y * stride + x * bpp;
                saida[cabecalho.Length + y * largura + x] =
                    bpp == 1 ? pixels[i] : (byte)((pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3);
            }
        return saida;
    }
}
