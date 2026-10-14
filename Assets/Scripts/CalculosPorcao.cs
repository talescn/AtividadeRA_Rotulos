using System;
using System.Collections.Generic;

// Contas com a quantidade que a pessoa diz que vai consumir ("vou consumir 300 ml"), a partir da tabela.
// Servem tambem a lupa e a voz. So informam gramas: o app nao calcula dose de insulina.
public static class CalculosPorcao
{
    public const float QuantidadeMaxima = 2000f;
    // Sem porcao na unidade da quantidade, comeca na coluna "por 100" da tabela
    const float QuantidadeSemPorcao = 100f;

    // Unidade da coluna "por 100" ("ml" ou "g"); sem ela, a da porcao. No Maggi e ml do prato pronto.
    public static string UnidadeBase(DadosProduto dados)
    {
        string base100 = dados.base100?.Trim().ToLowerInvariant();
        if (base100 != null && base100.EndsWith("ml")) return "ml";
        if (base100 != null && base100.EndsWith("g")) return "g";
        return dados.porcao?.unidade;
    }

    // Unidade em que a pessoa escolhe a quantidade
    public static string Unidade(DadosProduto dados)
    {
        return UnidadeBase(dados) ?? "g";
    }

    // Comeca na porcao, se ela estiver na mesma unidade; senao em 100
    public static float QuantidadeInicial(DadosProduto dados)
    {
        return PorcaoNaUnidade(dados, Unidade(dados)) ?? QuantidadeSemPorcao;
    }

    // Passo dos botoes - e +
    public static float Passo(string unidade)
    {
        return unidade == "ml" ? 50f : 10f;
    }

    public static float Limitar(float quantidade)
    {
        return Math.Max(0f, Math.Min(QuantidadeMaxima, quantidade));
    }

    // Valor por 100 g ou 100 ml: a coluna do rotulo ou, sem ela, a porcao levada a 100. So na mesma unidade:
    // no Maggi a porcao e o po, em g, e a coluna "por 100" e do prato pronto, em ml.
    public static float? Por100(DadosProduto.Nutriente n, DadosProduto dados)
    {
        if (n == null) return null;
        if (n.por100.HasValue) return n.por100;
        float? porcao = PorcaoNaUnidade(dados, UnidadeBase(dados));
        if (!n.porPorcao.HasValue || !porcao.HasValue) return null;
        return n.porPorcao.Value * 100f / porcao.Value;
    }

    // Valor do nutriente na quantidade: pela porcao quando ela esta na mesma unidade (na quantidade da porcao
    // da o mesmo numero do rotulo); senao pela coluna "por 100"
    public static float? ValorPara(DadosProduto.Nutriente n, DadosProduto dados, float quantidade)
    {
        if (n == null) return null;
        float? porcao = PorcaoNaUnidade(dados, Unidade(dados));
        if (n.porPorcao.HasValue && porcao.HasValue) return n.porPorcao.Value * quantidade / porcao.Value;
        float? por100 = Por100(n, dados);
        return por100.HasValue ? por100.Value * quantidade / 100f : (float?)null;
    }

    // Acucares adicionados ou, no rotulo antigo (RDC 360), os totais
    public static DadosProduto.Nutriente Acucares(DadosProduto dados)
    {
        return dados.Buscar("acucaresAdicionados") ?? dados.Buscar("acucaresTotais");
    }

    // "200 ml"
    public static string TextoQuantidade(float quantidade, string unidade)
    {
        return TabelaNutricionalUI.FormatarNumero(quantidade) + " " + unidade;
    }

    // "Carboidratos 20 g · Açúcares adicionados 20 g · 81 kcal": uma casa decimal nos gramas, kcal inteiro
    public static string TextoResultado(DadosProduto dados, float quantidade)
    {
        var partes = new List<string>();
        foreach (var n in new[] { dados.Buscar("carboidratos"), Acucares(dados) })
        {
            float? valor = ValorPara(n, dados, quantidade);
            if (valor.HasValue)
                partes.Add(n.nome + " " + TabelaNutricionalUI.FormatarNumero(Arredondar(valor.Value, 1)) + " " + n.unidade);
        }
        float? kcal = ValorPara(dados.Buscar("energia"), dados, quantidade);
        if (kcal.HasValue) partes.Add(TabelaNutricionalUI.FormatarNumero(Arredondar(kcal.Value, 0)) + " kcal");
        return partes.Count > 0 ? string.Join(" · ", partes) : "Sem carboidratos nesta tabela";
    }

    // 11,25 -> 11,3: o meio arredonda para cima, e nao para o par como o Math.Round padrao
    public static float Arredondar(float valor, int casas)
    {
        return (float)Math.Round(valor, casas, MidpointRounding.AwayFromZero);
    }

    static float? PorcaoNaUnidade(DadosProduto dados, string unidade)
    {
        var p = dados.porcao;
        if (p == null || !p.quantidade.HasValue || p.quantidade.Value <= 0f || p.unidade != unidade) return null;
        return p.quantidade.Value;
    }
}
