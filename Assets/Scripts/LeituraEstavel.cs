// Confirma um codigo de barras so quando ele sai igual em leituras seguidas (um quadro borrado pode dar
// um numero errado que ainda passa no digito verificador) e ignora o mesmo codigo por um tempo depois de aceito.
public class LeituraEstavel
{
    readonly int leiturasIguais;
    readonly float janelaSegundos;
    readonly float silencioSegundos;

    string ultimo;
    float ultimoEm = float.NegativeInfinity;
    int repeticoes;
    string aceito;
    float aceitoEm = float.NegativeInfinity;

    public LeituraEstavel(int leiturasIguais = 2, float janelaSegundos = 1.5f, float silencioSegundos = 8f)
    {
        this.leiturasIguais = leiturasIguais;
        this.janelaSegundos = janelaSegundos;
        this.silencioSegundos = silencioSegundos;
    }

    // Recebe cada leitura (null quando o quadro nao tinha codigo) e devolve o codigo no momento em que ele e confirmado
    public string Registrar(string codigo, float agora)
    {
        // Quadro sem codigo nao zera a contagem: entre duas leituras boas costuma vir um quadro borrado
        if (codigo == null) return null;

        if (codigo == ultimo && agora - ultimoEm <= janelaSegundos) repeticoes++;
        else repeticoes = 1;
        ultimo = codigo;
        ultimoEm = agora;

        if (repeticoes < leiturasIguais) return null;
        if (codigo == aceito && agora - aceitoEm < silencioSegundos) return null;

        aceito = codigo;
        aceitoEm = agora;
        repeticoes = 0;
        return codigo;
    }
}
