using System.Threading.Tasks;
using UnityEngine;
using Vuforia;

// Procura codigos de barras na imagem da camera do Vuforia enquanto Ativo: algumas vezes por segundo
// e fora da thread principal, para nao travar a RA. Avisa quando confirma um GTIN.
public class LeitorCodigoBarras : MonoBehaviour
{
    [Tooltip("Intervalo entre duas leituras, em segundos.")]
    public float intervalo = 0.3f;

    [Tooltip("Espera depois de ligado pela primeira vez, antes de pedir a imagem da camera ao Vuforia.")]
    public float esperaInicial = 1f;

    // Ligado pela interface quando os marcadores ja foram criados, nenhum rotulo esta na camera
    // e o modo leitura esta fechado
    public bool Ativo { get; set; }

    public event System.Action<string> AoLerCodigo;

    readonly LeituraEstavel confirmacao = new LeituraEstavel();
    PixelFormat formato = PixelFormat.UNKNOWN_FORMAT;
    int bytesPorPixel;
    bool registrado;
    float ligadoEm = -1f;
    float proximaLeitura;
    Task<string> leituraEmAndamento;

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
        Debug.Log($"[RA] Leitor de codigo de barras usando a imagem {formato}");
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

        // Resultado da leitura que estava rodando em outra thread
        if (leituraEmAndamento == null || !leituraEmAndamento.IsCompleted) return;
        string lido = leituraEmAndamento.Status == TaskStatus.RanToCompletion ? leituraEmAndamento.Result : null;
        leituraEmAndamento = null;
        string confirmado = confirmacao.Registrar(lido, Time.unscaledTime);
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
        if (imagem == null || imagem.Width <= 0 || imagem.Height <= 0) return;

        // Pixels ja e uma copia: a outra thread nao toca em nada do Vuforia nem do Unity
        byte[] pixels = imagem.Pixels;
        if (pixels == null || pixels.Length == 0) return;
        int largura = imagem.Width, altura = imagem.Height, stride = imagem.Stride, bpp = bytesPorPixel;
        leituraEmAndamento = Task.Run(() => DecodificadorBarras.Decodificar(pixels, largura, altura, stride, bpp));
    }
}
