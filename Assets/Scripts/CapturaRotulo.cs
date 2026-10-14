using System;
using UnityEngine;
using Vuforia;

// Compara os quadros da camera por um instante e fica com o mais nitido, ja girado para ficar como a tela
// em retrato: e dele que sai o marcador de um rotulo ensinado. Escolher entre varios quadros evita levar
// um borrado pelo tremor da mao ou pelo foco ainda ajustando.
public class CapturaRotulo : MonoBehaviour
{
    [Tooltip("Por quanto tempo os quadros sao comparados, em segundos.")]
    public float duracao = 0.8f;

    public bool Capturando { get; private set; }

    // Recebe a imagem em cinza, a largura e a altura; a imagem e null se a camera nao entregou nenhum quadro
    Action<byte[], int, int> aoTerminar;
    float fim;
    int quadros;
    byte[] melhor;
    int melhorLargura, melhorAltura, melhorStride;
    float melhorNitidez;
    // A entrega do resultado fica para o Update: no evento do Vuforia so se compara quadros
    bool terminou;

    public void Iniciar(Action<byte[], int, int> aoTerminar)
    {
        if (Capturando) return;
        if (!VuforiaApplication.Instance.IsRunning)
        {
            aoTerminar?.Invoke(null, 0, 0);
            return;
        }
        this.aoTerminar = aoTerminar;
        // O leitor de codigo de barras normalmente ja pediu este formato; pedir de novo nao atrapalha
        VuforiaBehaviour.Instance.CameraDevice.SetFrameFormat(PixelFormat.GRAYSCALE, true);
        quadros = 0;
        melhor = null;
        melhorNitidez = -1f;
        fim = Time.unscaledTime + duracao;
        Capturando = true;
        VuforiaBehaviour.Instance.World.OnStateUpdated += AoAtualizarEstado;
    }

    // Desiste da captura em andamento sem avisar ninguem (a tela de ensino foi fechada)
    public void Cancelar()
    {
        if (Capturando) Parar();
        terminou = false;
        aoTerminar = null;
        melhor = null;
    }

    void OnDisable()
    {
        Cancelar();
    }

    void AoAtualizarEstado()
    {
        Image imagem = VuforiaBehaviour.Instance.CameraDevice.GetCameraImage(PixelFormat.GRAYSCALE);
        byte[] pixels = imagem != null && imagem.Width > 0 && imagem.Height > 0 ? imagem.Pixels : null;
        if (pixels != null && pixels.Length > 0)
        {
            quadros++;
            float nitidez = ImagemCinza.Nitidez(pixels, imagem.Width, imagem.Height, imagem.Stride);
            if (nitidez > melhorNitidez)
            {
                melhorNitidez = nitidez;
                // Copia: assim o quadro guardado nao muda mesmo que o Vuforia reaproveite o buffer
                melhor = (byte[])pixels.Clone();
                melhorLargura = imagem.Width;
                melhorAltura = imagem.Height;
                melhorStride = imagem.Stride;
            }
        }
        if (Time.unscaledTime < fim) return;
        Parar();
        terminou = true;
    }

    void Update()
    {
        if (!terminou) return;
        terminou = false;
        Terminar();
    }

    void Terminar()
    {
        var avisar = aoTerminar;
        aoTerminar = null;
        if (melhor == null)
        {
            avisar?.Invoke(null, 0, 0);
            return;
        }
        // Tira o preenchimento do fim das linhas
        byte[] compacto = melhorStride == melhorLargura ? melhor
            : ImagemCinza.Recortar(melhor, melhorStride, new RectInt(0, 0, melhorLargura, melhorAltura));
        melhor = null;
        Debug.Log($"[RA] Captura do rotulo: {quadros} quadros comparados, nitidez do escolhido {melhorNitidez:0}");
        // No celular em retrato a imagem da camera chega deitada (o sensor fica de lado, conferido no realme);
        // a webcam do Editor ja chega na orientacao da tela
        if (Application.platform == RuntimePlatform.Android)
            avisar?.Invoke(ImagemCinza.GirarHorario(compacto, melhorLargura, melhorAltura), melhorAltura, melhorLargura);
        else
            avisar?.Invoke(compacto, melhorLargura, melhorAltura);
    }

    void Parar()
    {
        Capturando = false;
        if (VuforiaBehaviour.Instance != null)
            VuforiaBehaviour.Instance.World.OnStateUpdated -= AoAtualizarEstado;
    }
}
