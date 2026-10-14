using NUnit.Framework;

// Testes do texto lido pelo botao Ouvir: alertas da lupa primeiro, poucos valores da porcao,
// unidades por extenso e, se a pessoa escolheu outra quantidade, os carboidratos nela.
public class TestesResumoFalado
{
    const string FimSprite = "Por porção de 200 mililitros: 81 quilocalorias, 20 gramas de carboidratos, " +
                             "20 gramas de açúcares adicionados, 0 gramas de gorduras saturadas e 15 miligramas de sódio.";

    static DadosProduto Carregar(string gtin) => RepositorioProdutos.CarregarLocal(gtin);

    [Test]
    public void ComecaPeloNomeEPeloAlertaDaLupa()
    {
        Assert.AreEqual("Sprite Sabor Limão 200 ml. Alto em açúcar adicionado. " + FimSprite,
            ResumoFalado.Montar(Carregar("78939745")));
    }

    [Test]
    public void QuantidadeDiferenteDaPorcaoFalaOsCarboidratosNela()
    {
        var sprite = Carregar("78939745");
        StringAssert.EndsWith(FimSprite + " Em 300 mililitros: 30 gramas de carboidratos.", ResumoFalado.Montar(sprite, 300f));
        Assert.AreEqual(ResumoFalado.Montar(sprite), ResumoFalado.Montar(sprite, 200f), "na quantidade da porcao nao repete");
    }

    [Test]
    public void RotuloAntigoAvisaQueFaltaOAcucarAdicionado()
    {
        Assert.AreEqual("Fanta Uva 200 ml. Sem dado de açúcar adicionado neste rótulo. Por porção de 200 mililitros: " +
                        "47 quilocalorias, 12 gramas de carboidratos, 10 gramas de açúcares, 0 gramas de gorduras saturadas " +
                        "e 31 miligramas de sódio.",
            ResumoFalado.Montar(Carregar("78938571")));
    }

    [Test]
    public void LeiteNaoFalaDeLupa()
    {
        Assert.AreEqual("Leite UHT Integral Ninho Forti+ 1 L. Por porção de 200 mililitros: 118 quilocalorias, " +
                        "8,8 gramas de carboidratos, 0 gramas de açúcares adicionados, 4 gramas de gorduras saturadas " +
                        "e 126 miligramas de sódio.",
            ResumoFalado.Montar(Carregar("7898215157403")));
    }

    [Test]
    public void MaggiFalaAPorcaoDoPoEOPratoProntoNaQuantidade()
    {
        StringAssert.EndsWith("Por porção de 17 gramas: 58 quilocalorias, 12 gramas de carboidratos, " +
                              "1,2 gramas de açúcares adicionados, 0 gramas de gorduras saturadas e 745 miligramas de sódio. " +
                              "Em 250 mililitros: 11,3 gramas de carboidratos.",
            ResumoFalado.Montar(Carregar("7891000538500"), 250f));
    }

    [Test]
    public void UnidadesPorExtensoNoSingularENoPlural()
    {
        Assert.AreEqual("grama", ResumoFalado.UnidadePorExtenso("g", 1f));
        Assert.AreEqual("gramas", ResumoFalado.UnidadePorExtenso("g", 1.2f));
        Assert.AreEqual("quilocalorias", ResumoFalado.UnidadePorExtenso("kcal", 2f));
        Assert.AreEqual("miligramas", ResumoFalado.UnidadePorExtenso("mg", 0f));
        Assert.AreEqual("microgramas", ResumoFalado.UnidadePorExtenso("µg", 120f));
        Assert.AreEqual("mililitro", ResumoFalado.UnidadePorExtenso("ml", 1f));
    }
}
