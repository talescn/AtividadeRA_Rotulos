using System;
using System.Collections.Generic;

// Texto lido pelo botao Ouvir: o nome, os alertas da lupa primeiro e depois poucos valores da porcao, com as
// unidades por extenso para a voz do celular ler direito. Se a pessoa escolheu outra quantidade no painel,
// acrescenta os carboidratos nela.
public static class ResumoFalado
{
    public static string Montar(DadosProduto dados, float? quantidade = null)
    {
        var frases = new List<string>();
        if (!string.IsNullOrEmpty(dados.nome)) frases.Add(dados.nome + ".");

        var lupa = RotulagemFrontal.Avaliar(dados);
        var altos = lupa.Altos();
        if (altos.Count > 0) frases.Add("Alto em " + RotulagemFrontal.Juntar(RotulagemFrontal.Minusculas(altos)) + ".");
        string falta = RotulagemFrontal.TextoFaltaDado(lupa);
        if (falta != null) frases.Add(falta);
        string incerto = RotulagemFrontal.TextoIncerto(lupa);
        if (incerto != null) frases.Add(incerto);

        string porcao = FrasePorcao(dados);
        if (porcao != null) frases.Add(porcao);
        string naQuantidade = FraseQuantidade(dados, quantidade);
        if (naQuantidade != null) frases.Add(naQuantidade);
        return string.Join(" ", frases);
    }

    // "grama" ou "gramas", "quilocaloria" ou "quilocalorias"...
    public static string UnidadePorExtenso(string unidade, float valor)
    {
        bool um = Math.Abs(valor - 1f) < 0.001f;
        switch (unidade)
        {
            case "g": return um ? "grama" : "gramas";
            case "mg": return um ? "miligrama" : "miligramas";
            case "µg": return um ? "micrograma" : "microgramas";
            case "kcal": return um ? "quilocaloria" : "quilocalorias";
            case "ml": return um ? "mililitro" : "mililitros";
            default: return unidade ?? "";
        }
    }

    // "Por porção de 200 mililitros: 81 quilocalorias, 20 gramas de carboidratos, ... e 15 miligramas de sódio."
    static string FrasePorcao(DadosProduto dados)
    {
        var valores = new List<string>();
        var nutrientes = new[]
        {
            dados.Buscar("energia"), dados.Buscar("carboidratos"), CalculosPorcao.Acucares(dados),
            dados.Buscar("gordurasSaturadas"), dados.Buscar("sodio")
        };
        foreach (var n in nutrientes)
        {
            if (n == null || !n.porPorcao.HasValue) continue;
            // A energia ja se diz pela unidade: "81 quilocalorias"
            string nome = n.chave == "energia" ? "" : " de " + n.nome.ToLowerInvariant();
            valores.Add(Quantidade(n.porPorcao.Value, n.unidade) + nome);
        }
        if (valores.Count == 0) return null;

        var p = dados.porcao;
        string medida = p != null && p.quantidade.HasValue ? " de " + Quantidade(p.quantidade.Value, p.unidade) : "";
        return "Por porção" + medida + ": " + RotulagemFrontal.Juntar(valores) + ".";
    }

    // "Em 300 mililitros: 30 gramas de carboidratos."; nada quando a quantidade e a propria porcao
    static string FraseQuantidade(DadosProduto dados, float? quantidade)
    {
        if (!quantidade.HasValue) return null;
        string unidade = CalculosPorcao.Unidade(dados);
        var p = dados.porcao;
        bool ePorcao = p != null && p.unidade == unidade && p.quantidade.HasValue &&
                       Math.Abs(p.quantidade.Value - quantidade.Value) < 0.01f;
        if (ePorcao) return null;

        float? carboidratos = CalculosPorcao.ValorPara(dados.Buscar("carboidratos"), dados, quantidade.Value);
        if (!carboidratos.HasValue) return null;
        return "Em " + Quantidade(quantidade.Value, unidade) + ": " +
               Quantidade(CalculosPorcao.Arredondar(carboidratos.Value, 1), "g") + " de carboidratos.";
    }

    // "20 gramas", "1 grama", "8,8 gramas"
    static string Quantidade(float valor, string unidade)
    {
        return TabelaNutricionalUI.FormatarNumero(valor) + " " + UnidadePorExtenso(unidade, valor);
    }
}
