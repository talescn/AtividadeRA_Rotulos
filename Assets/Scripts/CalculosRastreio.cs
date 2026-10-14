using System.Collections.Generic;

// Qual tabela aparece quando varias vistas ensinadas do mesmo produto estao na camera ao mesmo tempo
// (no diario de 14/10/2026, duas vistas do mesmo rotulo mostraram duas tabelas sobrepostas por uns 11 s).
public static class CalculosRastreio
{
    // Recebe o GTIN de cada marcador rastreado, na ordem em que entraram na camera, e devolve quais mostram a
    // tabela: so a primeira vista de cada produto. Sem GTIN, cada marcador conta sozinho.
    public static bool[] QuaisMostrar(IList<string> gtins)
    {
        var mostrar = new bool[gtins.Count];
        var vistos = new HashSet<string>();
        for (int i = 0; i < gtins.Count; i++)
            mostrar[i] = string.IsNullOrEmpty(gtins[i]) || vistos.Add(gtins[i]);
        return mostrar;
    }
}
