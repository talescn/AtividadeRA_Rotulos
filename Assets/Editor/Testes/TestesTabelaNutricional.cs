using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;

// Testes da tabela desenhada a partir dos dados: formatacao como no rotulo e conteudo das linhas.
public class TestesTabelaNutricional
{
    [Test]
    public void NumerosFicamComoNoRotulo()
    {
        Assert.AreEqual("4,4", TabelaNutricionalUI.FormatarNumero(4.4f));
        Assert.AreEqual("7,6", TabelaNutricionalUI.FormatarNumero(7.6f));
        Assert.AreEqual("120", TabelaNutricionalUI.FormatarNumero(120f));
        Assert.AreEqual("0", TabelaNutricionalUI.FormatarNumero(0f));
        Assert.AreEqual("", TabelaNutricionalUI.FormatarNumero(null), "valor ausente fica em branco");
    }

    [Test]
    public void DataFicaNoFormatoBrasileiro()
    {
        Assert.AreEqual("05/10/2026", TabelaNutricionalUI.FormatarData("2026-10-05"));
    }

    [Test]
    public void PorcaoMostraQuantidadeEMedidaCaseira()
    {
        var ninho = RepositorioProdutos.CarregarLocal("7898215157403");
        Assert.AreEqual("200 ml", TabelaNutricionalUI.RotuloPorcao(ninho.porcao));
        Assert.AreEqual("Porção: 200 ml (1 copo)", TabelaNutricionalUI.LinhaPorcao(ninho));
    }

    [Test]
    public void RodapeDizQuandoEDeOndeVeioODado()
    {
        var ninho = RepositorioProdutos.CarregarLocal("7898215157403");
        Assert.AreEqual("Conferido em 05/10/2026 · fonte: rótulo", TabelaNutricionalUI.LinhaConferido(ninho));

        ninho.fonte = DadosProduto.FonteOpenFoodFacts;
        StringAssert.EndsWith("fonte: Open Food Facts", TabelaNutricionalUI.LinhaConferido(ninho));
    }

    [Test]
    public void TabelaTemUmaLinhaPorNutrienteAPorcaoEADataConferida()
    {
        var ninho = RepositorioProdutos.CarregarLocal("7898215157403");
        var textos = Montar(ninho, out RectTransform tabela);

        foreach (var n in ninho.nutrientes)
            CollectionAssert.Contains(textos, n.nome + " (" + n.unidade + ")");
        CollectionAssert.Contains(textos, "Porções por embalagem: 5");
        CollectionAssert.Contains(textos, "Porção: 200 ml (1 copo)");
        CollectionAssert.Contains(textos, "Conferido em 05/10/2026 · fonte: rótulo");
        // Valores na coluna da porcao e %VD em branco quando o rotulo nao traz
        CollectionAssert.Contains(textos, "126");
        Assert.AreEqual(TabelaNutricionalUI.Largura, tabela.sizeDelta.x);
        Assert.Greater(tabela.sizeDelta.y, 54f * ninho.nutrientes.Count, "cabe uma linha por nutriente");

        Object.DestroyImmediate(tabela.parent.gameObject);
    }

    [Test]
    public void ColunaDoAlimentoProntoGanhaObservacao()
    {
        // Como no Maggi: a coluna "100 ml" e do alimento pronto para o consumo
        var dados = RepositorioProdutos.CarregarLocal("78939745");
        dados.observacaoBase100 = "No alimento pronto para o consumo.";
        var textos = Montar(dados, out RectTransform tabela);

        CollectionAssert.Contains(textos, "100 ml**");
        CollectionAssert.Contains(textos, "**No alimento pronto para o consumo.");

        Object.DestroyImmediate(tabela.parent.gameObject);
    }

    static List<string> Montar(DadosProduto dados, out RectTransform tabela)
    {
        var pai = new GameObject("TesteTabela", typeof(RectTransform));
        tabela = TabelaNutricionalUI.Montar(pai.transform, dados);
        return tabela.GetComponentsInChildren<TMP_Text>(true).Select(t => t.text).ToList();
    }
}
