using System.Collections.Generic;

// Mede cada tentativa de leitura de codigo de barras, para o diario de testes. A tentativa comeca no primeiro quadro
// em que um codigo foi decodificado e acaba na confirmacao, depois de um silencio sem nenhum codigo ou quando o
// leitor desliga. O tempo em que a pessoa mira antes do primeiro codigo decodificado nao entra.
public class MedidaLeitura
{
    readonly float silencioFim;

    float inicio, ultimo;
    int quadros;
    // Codigos diferentes vistos na tentativa, na ordem: mostra quando o leitor errou o numero
    readonly List<string> codigos = new List<string>();

    public MedidaLeitura(float silencioFim = 10f)
    {
        this.silencioFim = silencioFim;
    }

    public bool EmAndamento => quadros > 0;

    // Cada quadro decodificado; quadro sem codigo (null) nao conta
    public void Registrar(string codigo, float agora)
    {
        if (codigo == null) return;
        if (!EmAndamento) inicio = agora;
        quadros++;
        ultimo = agora;
        if (!codigos.Contains(codigo)) codigos.Add(codigo);
    }

    // "78939745 em 1,2 s (2 quadros com codigo, outros codigos: nenhum)"; comeca outra tentativa
    public string Confirmar(string codigo, float agora)
    {
        if (!EmAndamento) inicio = agora;
        var outros = new List<string>(codigos);
        outros.Remove(codigo);
        string texto = $"{codigo} em {Segundos(agora - inicio)} ({Quadros()}, outros codigos: " +
                       (outros.Count > 0 ? string.Join(", ", outros) : "nenhum") + ")";
        Zerar();
        return texto;
    }

    // "4 quadros com codigo em 12,3 s (7897076021403, 7897076021410)"; null se nao havia tentativa
    public string Abandonar(float agora)
    {
        if (!EmAndamento) return null;
        string texto = $"{Quadros()} em {Segundos(ultimo - inicio)} ({string.Join(", ", codigos)})";
        Zerar();
        return texto;
    }

    // Acaba a tentativa sem leitura depois de silencioFim segundos sem nenhum codigo
    public string VerificarSilencio(float agora)
    {
        return EmAndamento && agora - ultimo >= silencioFim ? Abandonar(agora) : null;
    }

    string Quadros()
    {
        return quadros == 1 ? "1 quadro com codigo" : quadros + " quadros com codigo";
    }

    static string Segundos(float segundos)
    {
        return TabelaNutricionalUI.FormatarNumero(CalculosPorcao.Arredondar(segundos, 1)) + " s";
    }

    void Zerar()
    {
        quadros = 0;
        codigos.Clear();
    }
}
