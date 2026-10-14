using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// Diario de testes: copia para um arquivo no aparelho as linhas "[RA]" do log e os erros, um arquivo por dia.
// No mercado o celular fica sem cabo e o logcat se perde; na volta, a pasta sai com
// adb pull /sdcard/Android/data/com.talescn.rotulosra/files/Diario
// So liga no aparelho: no Editor o Console ja mostra tudo.
public class DiarioTestes
{
    public const string PastaDiario = "Diario";
    // Acima disso o arquivo do dia para de crescer (uma ultima linha avisa)
    public const long LimitePadraoBytes = 5L * 1024 * 1024;
    const string Prefixo = "[RA]";

    static DiarioTestes ativo;

    // Falhas de disco seguidas antes de desligar (uma so pode ser passageira)
    const int FalhasParaDesligar = 3;

    readonly string pasta;
    readonly long limiteBytes;
    readonly object trava = new object();
    // Arquivo que ja recebeu o aviso de cheio
    string arquivoCheio;
    int falhasSeguidas;
    // Ultima mensagem gravada e quantas vezes ela veio de novo logo em seguida, sem ser gravada
    string ultimaMensagem;
    LogType ultimoTipo;
    int repeticoes;

    // Desliga de vez depois de algumas falhas de disco seguidas: o diario nunca pode atrapalhar o app
    public bool Desligado { get; private set; }
    public string MotivoDesligado { get; private set; }

    public DiarioTestes(string pasta, long limiteBytes = LimitePadraoBytes)
    {
        this.pasta = pasta;
        this.limiteBytes = limiteBytes;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Iniciar()
    {
        if (Application.isEditor || ativo != null) return;
        ativo = new DiarioTestes(Path.Combine(Application.persistentDataPath, PastaDiario));
        Application.logMessageReceivedThreaded += ativo.Registrar;
        // Um "Diario iniciado" sem um "fora de foco" antes dele indica que o app caiu
        Application.focusChanged += foco => Debug.Log(foco ? "[RA] App em primeiro plano" : "[RA] App fora de foco");
        Debug.Log($"[RA] Diario iniciado: app {Application.version}, {SystemInfo.deviceModel}, {SystemInfo.operatingSystem}");
        // Aqui ja da para avisar pelo log: o registro acima passou pelo diario e voltou
        if (ativo.Desligado) Debug.LogWarning("[RA] Diario de testes desligado: " + ativo.MotivoDesligado);
    }

    // Chamado pelo Unity em qualquer thread. Nunca usa Debug.Log aqui dentro: a linha voltaria para ca.
    public void Registrar(string mensagem, string pilha, LogType tipo)
    {
        Registrar(mensagem, pilha, tipo, DateTime.Now);
    }

    public void Registrar(string mensagem, string pilha, LogType tipo, DateTime agora)
    {
        if (!DeveRegistrar(mensagem, tipo)) return;
        lock (trava)
        {
            if (Desligado) return;
            // Um erro a cada quadro vira uma linha so; a contagem e gravada quando chega outra mensagem
            if (mensagem == ultimaMensagem && tipo == ultimoTipo)
            {
                repeticoes++;
                return;
            }
            try
            {
                // A cada linha: a pasta pode ter sido apagada com o app aberto (depois de um adb pull, por exemplo)
                Directory.CreateDirectory(pasta);
                string caminho = Path.Combine(pasta, NomeArquivo(agora));
                if (repeticoes > 0)
                    Acrescentar(caminho, agora, FormatarLinha(agora, LogType.Log,
                        $"{Prefixo} A linha anterior se repetiu mais {repeticoes} vezes", ""));
                Acrescentar(caminho, agora, FormatarLinha(agora, tipo, mensagem, pilha));
                ultimaMensagem = mensagem;
                ultimoTipo = tipo;
                repeticoes = 0;
                falhasSeguidas = 0;
            }
            catch (Exception e)
            {
                MotivoDesligado = e.GetType().Name + ": " + e.Message;
                if (++falhasSeguidas >= FalhasParaDesligar) Desligado = true;
            }
        }
    }

    // Grava a linha se ela couber no limite do arquivo; na primeira que nao cabe, grava so o aviso de cheio
    void Acrescentar(string caminho, DateTime agora, string linha)
    {
        linha += "\n";
        long tamanho = File.Exists(caminho) ? new FileInfo(caminho).Length : 0;
        if (tamanho + Encoding.UTF8.GetByteCount(linha) <= limiteBytes)
        {
            File.AppendAllText(caminho, linha);
        }
        else if (arquivoCheio != caminho)
        {
            arquivoCheio = caminho;
            File.AppendAllText(caminho, FormatarLinha(agora, LogType.Warning,
                Prefixo + " Diario cheio: o resto do dia fica fora deste arquivo", "") + "\n");
        }
    }

    // ---------- Contas (publicas para os testes) ----------

    // As linhas do projeto e qualquer erro ou excecao (de onde vier)
    public static bool DeveRegistrar(string mensagem, LogType tipo)
    {
        if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert) return true;
        return mensagem != null && mensagem.StartsWith(Prefixo, StringComparison.Ordinal);
    }

    // "2026-10-14 09:05:03.042<TAB>Log<TAB>[RA] ...", numa linha so; erro e excecao levam a primeira linha da pilha
    public static string FormatarLinha(DateTime agora, LogType tipo, string mensagem, string pilha)
    {
        string texto = Achatar(mensagem);
        if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert)
        {
            string origem = PrimeiraLinha(pilha);
            if (origem != null) texto += " | " + origem;
        }
        return agora.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "\t" + tipo + "\t" + texto;
    }

    // "diario_20261014.txt"
    public static string NomeArquivo(DateTime agora)
    {
        return "diario_" + agora.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".txt";
    }

    // Quebras de linha viram " / " e tabs viram espaco, para cada registro ocupar uma linha com 3 colunas
    static string Achatar(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return "";
        return texto.Trim().Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", " / ").Replace('\t', ' ');
    }

    static string PrimeiraLinha(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return null;
        foreach (var linha in texto.Split('\n'))
            if (!string.IsNullOrWhiteSpace(linha)) return linha.Trim();
        return null;
    }
}
