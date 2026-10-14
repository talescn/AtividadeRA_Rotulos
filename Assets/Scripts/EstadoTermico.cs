// Estado termico do Android (PowerManager.getCurrentThermalStatus) em texto para o diario de testes. Com o celular
// quente o sistema segura o processador e o leitor de codigo de barras fica mais lento (no teste de 14/10/2026, no
// estado 3, 130 ms por quadro contra 72-83 ms); a linha so e gravada quando o estado muda.
public class EstadoTermico
{
    // Na ordem das constantes THERMAL_STATUS_* do Android: NONE (0) ate SHUTDOWN (6)
    static readonly string[] Nomes = { "normal", "leve", "moderado", "severo", "critico", "emergencia", "desligando" };

    int? ultimo;

    // "severo"; um estado fora da tabela (de uma versao nova do Android) vira "desconhecido"
    public static string Nome(int estado)
    {
        return estado >= 0 && estado < Nomes.Length ? Nomes[estado] : "desconhecido";
    }

    // "[RA] Temperatura: estado termico 3 (severo)" na primeira medida e a cada mudanca; null se nada mudou
    public string Registrar(int estado)
    {
        if (estado == ultimo) return null;
        ultimo = estado;
        return $"[RA] Temperatura: estado termico {estado} ({Nome(estado)})";
    }

    // Quando o aparelho nao responde: "[RA] Temperatura: indisponivel (motivo)"
    public static string Indisponivel(string motivo)
    {
        return $"[RA] Temperatura: indisponivel ({motivo})";
    }
}
