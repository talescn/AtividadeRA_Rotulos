using UnityEngine;

// Le um texto em voz alta. No Android usa o TextToSpeech do sistema, em portugues do Brasil; no Editor do Windows,
// a voz do Windows (a demo da disciplina roda no Editor); no resto, so escreve o texto no log.
// Fica no Canvas da interface, que nunca desliga.
public class LeitorVoz : MonoBehaviour
{
    // De quanto em quanto tempo pergunta ao sistema se ainda esta falando
    const float IntervaloConsulta = 0.25f;
    // Logo depois de pedir a fala o sistema ainda nao diz que esta falando: espera antes de perguntar
    const float EsperaInicio = 1.5f;

    bool falando;
    float proximaConsulta;

    public bool Falando => falando;

    public void Falar(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return;
        Parar();
        Debug.Log("[RA] Voz: " + texto);
        falando = IniciarFala(texto);
        proximaConsulta = Time.unscaledTime + EsperaInicio;
    }

    public void Parar()
    {
        PararFala();
        falando = false;
    }

    void Update()
    {
        AtualizarPlataforma();
        if (falando && Time.unscaledTime >= proximaConsulta)
        {
            proximaConsulta = Time.unscaledTime + IntervaloConsulta;
            falando = AindaFalando();
        }
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

    // Comando que faz o Windows falar o texto, com a voz em portugues se houver uma instalada. O texto vai numa
    // string entre aspas simples: aspas simples sao dobradas e aspas duplas saem, para nao fechar o argumento.
    public static string ComandoPowerShell(string texto)
    {
        string seguro = (texto ?? "").Replace("\"", "").Replace("'", "''").Replace('\r', ' ').Replace('\n', ' ');
        return "Add-Type -AssemblyName System.Speech; " +
               "$v = New-Object System.Speech.Synthesis.SpeechSynthesizer; " +
               "$pt = $v.GetInstalledVoices() | Where-Object { $_.VoiceInfo.Culture.Name -eq 'pt-BR' } | Select-Object -First 1; " +
               "if ($pt) { $v.SelectVoice($pt.VoiceInfo.Name) }; " +
               "$v.Speak('" + seguro + "')";
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    // Constantes do android.speech.tts.TextToSpeech
    const int Sucesso = 0;
    const int FilaSubstitui = 0;
    const int IdiomaSemDados = -1;
    const int IdiomaNaoSuportado = -2;

    AndroidJavaObject tts;
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

    void Start()
    {
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
            proximaConsulta = Time.unscaledTime + EsperaInicio;
        }
    }

    bool IniciarFala(string texto)
    {
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

    void AtualizarPlataforma() { }

    bool IniciarFala(string texto)
    {
        var info = new System.Diagnostics.ProcessStartInfo("powershell.exe",
            "-NoProfile -NonInteractive -Command \"" + ComandoPowerShell(texto) + "\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        try
        {
            processo = System.Diagnostics.Process.Start(info);
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
        if (processo != null && !processo.HasExited)
        {
            try
            {
                processo.Kill();
            }
            catch (System.InvalidOperationException)
            {
                // Terminou de falar entre a pergunta e o Kill: nada a parar
            }
        }
        processo = null;
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
