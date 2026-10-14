using System.Collections;
using UnityEngine;
using Vuforia;

// Foco e lanterna da camera do Vuforia. Quando o Vuforia comeca ou volta de uma pausa, pede o foco
// continuo e devolve a lanterna ao estado escolhido. Tocar na tela pede um foco naquele ponto.
public class ControleCamera : MonoBehaviour
{
    [Tooltip("Depois de um foco pedido pelo toque, volta ao foco continuo apos este tempo, em segundos.")]
    public float voltarAoFocoContinuo = 2f;

    // Tamanho da regiao de foco em volta do toque, em fracao da imagem (so vale sem ARCore)
    const float RegiaoFoco = 0.2f;

    public bool LanternaDisponivel { get; private set; }
    public bool LanternaLigada { get; private set; }

    Coroutine focoPorToque;
    CameraRegionOfInterest regiaoPadrao;
    bool temRegiaoPadrao;

    void Start()
    {
        VuforiaApplication.Instance.OnVuforiaStarted += Configurar;
        VuforiaApplication.Instance.OnVuforiaPaused += AoPausar;
        if (VuforiaApplication.Instance.IsRunning) Configurar();
    }

    void OnDestroy()
    {
        if (VuforiaApplication.Instance == null) return;
        VuforiaApplication.Instance.OnVuforiaStarted -= Configurar;
        VuforiaApplication.Instance.OnVuforiaPaused -= AoPausar;
    }

    // Na pausa (app em segundo plano) o Vuforia apaga a lanterna e pode perder o modo de foco
    void AoPausar(bool pausado)
    {
        if (!pausado) Configurar();
    }

    void Configurar()
    {
        if (VuforiaBehaviour.Instance == null) return;
        var camera = VuforiaBehaviour.Instance.CameraDevice;
        bool focoContinuo = camera.SetFocusMode(FocusMode.FOCUS_MODE_CONTINUOUSAUTO);
        temRegiaoPadrao = camera.FocusRegionSupported;
        if (temRegiaoPadrao) regiaoPadrao = camera.GetFocusRegion();
        LanternaDisponivel = camera.IsFlashSupported();
        if (LanternaLigada) LanternaLigada = camera.SetFlash(true);

        VideoModeData modo = camera.GetVideoMode();
        Debug.Log($"[RA] Camera: video {modo.width}x{modo.height} a {modo.frameRate} fps, foco continuo " +
                  $"{(focoContinuo ? "ligado" : "recusado")} (modo {camera.FocusMode}), regiao de foco " +
                  $"{(temRegiaoPadrao ? "sim" : "nao")}, lanterna {(LanternaDisponivel ? "sim" : "nao")}");
    }

    public void AlternarLanterna()
    {
        if (!VuforiaApplication.Instance.IsRunning || !LanternaDisponivel) return;
        bool ligar = !LanternaLigada;
        if (VuforiaBehaviour.Instance.CameraDevice.SetFlash(ligar))
            LanternaLigada = ligar;
        else
            Debug.LogWarning($"[RA] A camera recusou {(ligar ? "ligar" : "apagar")} a lanterna.");
    }

    // Um foco unico no ponto tocado; a regiao so e aceita quando o ARCore nao esta em uso
    public void FocarEm(Vector2 posicaoTela)
    {
        if (!VuforiaApplication.Instance.IsRunning) return;
        var camera = VuforiaBehaviour.Instance.CameraDevice;
        if (temRegiaoPadrao)
            camera.SetFocusRegion(new CameraRegionOfInterest(posicaoTela, RegiaoFoco));
        if (!camera.SetFocusMode(FocusMode.FOCUS_MODE_TRIGGERAUTO)) return;

        if (focoPorToque != null) StopCoroutine(focoPorToque);
        focoPorToque = StartCoroutine(VoltarAoFocoContinuo());
    }

    // O foco continuo volta a olhar a imagem inteira, e nao so o ponto tocado
    IEnumerator VoltarAoFocoContinuo()
    {
        yield return new WaitForSecondsRealtime(voltarAoFocoContinuo);
        focoPorToque = null;
        if (!VuforiaApplication.Instance.IsRunning) yield break;
        var camera = VuforiaBehaviour.Instance.CameraDevice;
        if (temRegiaoPadrao) camera.SetFocusRegion(regiaoPadrao);
        camera.SetFocusMode(FocusMode.FOCUS_MODE_CONTINUOUSAUTO);
    }
}
