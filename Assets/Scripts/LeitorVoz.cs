using UnityEngine;

// Le um texto em voz alta. No Android usa o TextToSpeech do sistema, em portugues do Brasil; no Editor do Windows,
// a voz do Windows (a demo da disciplina roda no Editor); no resto, so escreve o texto no log.
// Fica no Canvas da interface, que nunca desliga.
public class LeitorVoz : MonoBehaviour
{
    // De quanto em quanto tempo pergunta ao sistema se ainda esta falando
    const float IntervaloConsulta = 0.25f;
    // Depois de pedir a fala, o Android pode levar alguns segundos para carregar a voz: ate o sistema dizer que
    // comecou, a fala conta como em andamento por no maximo este tempo
    const float EsperaMaximaInicio = 8f;

    // Variavel de ambiente que leva o texto para o PowerShell no Editor do Windows
    public const string VariavelTexto = "RA_TEXTO_VOZ";

    // Comando que faz o Windows falar, com a voz em portugues se houver uma instalada. O texto nunca entra no
    // comando: o PowerShell trata ate as aspas tipograficas como aspas simples, e um nome de produto do Open Food
    // Facts (editavel por qualquer pessoa) poderia fechar a string e rodar outro comando.
    public const string ComandoPowerShell =
        "Add-Type -AssemblyName System.Speech; " +
        "$v = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
        "$pt = $v.GetInstalledVoices() | Where-Object { $_.VoiceInfo.Culture.Name -eq 'pt-BR' } | Select-Object -First 1; " +
        "if ($pt) { $v.SelectVoice($pt.VoiceInfo.Name) }; " +
        "$v.Speak($env:" + VariavelTexto + ")";

    bool falando;
    // O sistema ja disse que esta falando (antes disso, "nao esta falando" so quer dizer "ainda nao comecou")
    bool comecou;
    float limiteInicio;
    float proximaConsulta;

    public bool Falando => falando;

    // Devolve false quando nao ha como falar (sem sintetizador): a interface avisa a pessoa
    public bool Falar(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return false;
        Parar();
        Debug.Log("[RA] Voz: " + texto);
        falando = IniciarFala(texto);
        ReiniciarEspera();
        return falando;
    }

    public void Parar()
    {
        PararFala();
        falando = false;
    }

    void ReiniciarEspera()
    {
        comecou = false;
        limiteInicio = Time.unscaledTime + EsperaMaximaInicio;
        proximaConsulta = Time.unscaledTime + IntervaloConsulta;
    }

    void Update()
    {
        AtualizarPlataforma();
        if (!falando || Time.unscaledTime < proximaConsulta) return;
        proximaConsulta = Time.unscaledTime + IntervaloConsulta;
        if (AindaFalando()) comecou = true;
        else if (comecou || Time.unscaledTime >= limiteInicio) falando = false;
    }

    void OnApplicationPause(bool pausado)
    {
        if (pausado) Parar();
    }

    void OnDisable()
    {
        Parar();
    }

    void OnDestroy()
    {
        Encerrar();
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // Constantes do android.speech.tts.TextToSpeech
    const int Sucesso = 0;
    const int FilaSubstitui = 0;
    const int IdiomaSemDados = -1;
    const int IdiomaNaoSuportado = -2;

    AndroidJavaObject tts;
    // A voz so e criada no primeiro Ouvir, para nao somar trabalho a abertura do app (quando o Vuforia inicia)
    bool criacaoTentada;
    // O OnInit chega numa thread do Java: so estes dois campos sao escritos la
    volatile bool iniciou;
    volatile int statusInicio;
    bool pronto;
    // Texto pedido antes de o sistema terminar de preparar a voz
    string aguardando;

    class OuvinteInicio : AndroidJavaProxy
    {
        readonly LeitorVoz dono;

        public OuvinteInicio(LeitorVoz dono) : base("android.speech.tts.TextToSpeech$OnInitListener")
        {
            this.dono = dono;
        }

        // Chamado pelo Java por reflexao: o Preserve impede o IL2CPP de remover o metodo
        [UnityEngine.Scripting.Preserve]
        public void onInit(int status)
        {
            dono.statusInicio = status;
            dono.iniciou = true;
        }
    }

    void Criar()
    {
        criacaoTentada = true;
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                tts = new AndroidJavaObject("android.speech.tts.TextToSpeech", activity, new OuvinteInicio(this));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[RA] Voz: o celular nao tem sintetizador de voz disponivel: " + e.Message);
        }
    }

    // Termina a preparacao (idioma) na thread da Unity e fala o que estava esperando
    void AtualizarPlataforma()
    {
        if (tts == null || !iniciou) return;
        if (!pronto)
        {
            pronto = true;
            if (statusInicio != Sucesso)
            {
                Debug.LogWarning("[RA] Voz: o sintetizador do celular nao iniciou (status " + statusInicio + ").");
                Encerrar();
                falando = false;
                return;
            }
            using (var portugues = new AndroidJavaObject("java.util.Locale", "pt", "BR"))
            {
                int idioma = tts.Call<int>("setLanguage", portugues);
                if (idioma == IdiomaSemDados || idioma == IdiomaNaoSuportado)
                    Debug.LogWarning("[RA] Voz: sem voz em portugues do Brasil instalada; usando a voz padrao do celular.");
            }
        }
        if (aguardando != null)
        {
            string texto = aguardando;
            aguardando = null;
            falando = Pronunciar(texto);
            ReiniciarEspera();
        }
    }

    bool IniciarFala(string texto)
    {
        if (tts == null && !criacaoTentada) Criar();
        if (tts == null) return false;
        if (pronto) return Pronunciar(texto);
        aguardando = texto;
        return true;
    }

    bool Pronunciar(string texto)
    {
        using (var parametros = new AndroidJavaObject("android.os.Bundle"))
        {
            if (tts.Call<int>("speak", texto, FilaSubstitui, parametros, "rotulos-ra") == Sucesso) return true;
        }
        Debug.LogWarning("[RA] Voz: o celular recusou o texto.");
        return false;
    }

    bool AindaFalando()
    {
        if (tts == null) return false;
        return aguardando != null || tts.Call<bool>("isSpeaking");
    }

    void PararFala()
    {
        aguardando = null;
        if (tts != null && pronto) tts.Call<int>("stop");
    }

    void Encerrar()
    {
        if (tts == null) return;
        tts.Call("shutdown");
        tts.Dispose();
        tts = null;
    }
#elif UNITY_EDITOR_WIN
    System.Diagnostics.Process processo;

    // Processo do PowerShell que fala o texto; o texto vai so pela variavel de ambiente
    public static System.Diagnostics.ProcessStartInfo InfoProcessoVoz(string texto)
    {
        var info = new System.Diagnostics.ProcessStartInfo("powershell.exe",
            "-NoProfile -NonInteractive -Command \"" + ComandoPowerShell + "\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        info.EnvironmentVariables[VariavelTexto] = texto;
        return info;
    }

    void AtualizarPlataforma() { }

    bool IniciarFala(string texto)
    {
        try
        {
            processo = System.Diagnostics.Process.Start(InfoProcessoVoz(texto));
            return processo != null;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[RA] Voz: nao foi possivel usar a voz do Windows: " + e.Message);
            return false;
        }
    }

    bool AindaFalando()
    {
        return processo != null && !processo.HasExited;
    }

    void PararFala()
    {
        if (processo == null) return;
        try
        {
            if (!processo.HasExited) processo.Kill();
        }
        catch (System.Exception)
        {
            // Terminou de falar entre a pergunta e o Kill (InvalidOperationException ou Win32Exception): nada a parar
        }
        finally
        {
            processo.Dispose();
            processo = null;
        }
    }

    void Encerrar()
    {
        PararFala();
    }
#else
    void AtualizarPlataforma() { }

    // Sem sintetizador nesta plataforma: o texto fica so no log
    bool IniciarFala(string texto) => false;

    bool AindaFalando() => false;

    void PararFala() { }

    void Encerrar() { }
#endif
}
