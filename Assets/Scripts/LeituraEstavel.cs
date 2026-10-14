// Confirma um codigo de barras so quando ele sai igual em leituras seguidas (um quadro borrado pode dar
// um numero errado que ainda passa no digito verificador) e ignora o mesmo codigo por um tempo depois de aceito.
// O EAN-8 pede uma leitura a mais: pedaco de um EAN-13 sai como EAN-8 com digito verificador valido
// (no teste de 14/10/2026, 89647714 enquanto o leitor lia o 7896036099537).
public class LeituraEstavel
{
    readonly int leiturasIguais;
    readonly int leiturasIguaisEan8;
    readonly float janelaSegundos;
    readonly float silencioSegundos;

    string ultimo;
    float ultimoEm = float.NegativeInfinity;
    int repeticoes;
    string aceito;
    float aceitoEm = float.NegativeInfinity;

    public LeituraEstavel(int leiturasIguais = 2, float janelaSegundos = 1.5f, float silencioSegundos = 8f,
                          int leiturasIguaisEan8 = 3)
    {
        this.leiturasIguais = leiturasIguais;
        this.janelaSegundos = janelaSegundos;
        this.silencioSegundos = silencioSegundos;
        this.leiturasIguaisEan8 = leiturasIguaisEan8;
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

        if (repeticoes < (codigo.Length == 8 ? leiturasIguaisEan8 : leiturasIguais)) return null;
        if (codigo == aceito && agora - aceitoEm < silencioSegundos) return null;

        aceito = codigo;
        aceitoEm = agora;
        repeticoes = 0;
        return codigo;
    }

    // O codigo acabou de ser aceito e ainda e ignorado (a mesma embalagem continua na frente da camera)
    public bool Silenciado(string codigo, float agora)
    {
        return codigo != null && codigo == aceito && agora - aceitoEm < silencioSegundos;
    }
}
