using System.Threading.Tasks;
using UnityEngine;
using Vuforia;

// Procura codigos de barras na imagem da camera do Vuforia enquanto Ativo: algumas vezes por segundo
// e fora da thread principal, para nao travar a RA. Avisa quando confirma um GTIN.
public class LeitorCodigoBarras : MonoBehaviour
{
    [Tooltip("Intervalo entre duas leituras, em segundos.")]
    public float intervalo = 0.3f;

    // Ligado pela interface quando nenhum rotulo esta na camera e o modo leitura esta fechado
    public bool Ativo { get; set; }

    public event System.Action<string> AoLerCodigo;

    readonly LeituraEstavel confirmacao = new LeituraEstavel();
    PixelFormat formato = PixelFormat.UNKNOWN_FORMAT;
    int bytesPorPixel;
    float proximaLeitura;
    Task<string> leituraEmAndamento;

    void Start()
    {
        VuforiaApplication.Instance.OnVuforiaStarted += RegistrarFormato;
        VuforiaApplication.Instance.OnVuforiaStopped += EsquecerFormato;
        if (VuforiaApplication.Instance.IsRunning) RegistrarFormato();
    }

    void OnDestroy()
    {
        if (VuforiaApplication.Instance == null) return;
        VuforiaApplication.Instance.OnVuforiaStarted -= RegistrarFormato;
        VuforiaApplication.Instance.OnVuforiaStopped -= EsquecerFormato;
    }

    void RegistrarFormato()
    {
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
        Debug.Log($"[RA] Leitor de codigo de barras usando a imagem {formato}");
    }

    void EsquecerFormato()
    {
        formato = PixelFormat.UNKNOWN_FORMAT;
    }

    void Update()
    {
        // Resultado da leitura que estava rodando em outra thread
        if (leituraEmAndamento != null && leituraEmAndamento.IsCompleted)
        {
            string lido = leituraEmAndamento.Status == TaskStatus.RanToCompletion ? leituraEmAndamento.Result : null;
            leituraEmAndamento = null;
            string confirmado = confirmacao.Registrar(lido, Time.unscaledTime);
            if (confirmado != null && Ativo)
            {
                Debug.Log("[RA] Codigo de barras lido: " + confirmado);
                AoLerCodigo?.Invoke(confirmado);
            }
        }

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
