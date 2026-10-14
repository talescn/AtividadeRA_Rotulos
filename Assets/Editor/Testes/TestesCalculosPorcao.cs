using NUnit.Framework;

// Testes das contas de quantidade: o valor de cada nutriente na quantidade que a pessoa escolhe,
// pela coluna da porcao ou pela coluna "por 100" (no Maggi, a do prato pronto).
public class TestesCalculosPorcao
{
    static DadosProduto Sprite() => RepositorioProdutos.CarregarLocal("78939745");
    static DadosProduto Fanta() => RepositorioProdutos.CarregarLocal("78938571");
    static DadosProduto Maggi() => RepositorioProdutos.CarregarLocal("7891000538500");

    [Test]
    public void UnidadeEADaColunaPor100()
    {
        Assert.AreEqual("ml", CalculosPorcao.UnidadeBase(Sprite()));
        // No Maggi a porcao e o po (g), mas a coluna "por 100" e do prato pronto (ml)
        Assert.AreEqual("ml", CalculosPorcao.UnidadeBase(Maggi()));
        Assert.AreEqual("g", CalculosPorcao.UnidadeBase(new DadosProduto { base100 = "100 g" }));
    }

    [Test]
    public void QuantidadeComecaNaPorcaoQuandoAUnidadeBate()
    {
        Assert.AreEqual(200f, CalculosPorcao.QuantidadeInicial(Sprite()));
        Assert.AreEqual(100f, CalculosPorcao.QuantidadeInicial(Maggi()), "porcao em g e quantidade em ml: comeca em 100");
    }

    [Test]
    public void PassoELimiteDaQuantidade()
    {
        Assert.AreEqual(50f, CalculosPorcao.Passo("ml"));
        Assert.AreEqual(10f, CalculosPorcao.Passo("g"));
        Assert.AreEqual(0f, CalculosPorcao.Limitar(-50f));
        Assert.AreEqual(CalculosPorcao.QuantidadeMaxima, CalculosPorcao.Limitar(5000f));
        Assert.AreEqual(250f, CalculosPorcao.Limitar(250f));
    }

    [Test]
    public void ValorUsaAPorcaoQuandoAUnidadeBate()
    {
        var sprite = Sprite();
        Assert.AreEqual(30f, CalculosPorcao.ValorPara(sprite.Buscar("carboidratos"), sprite, 300f).Value, 0.001f);
        // Na quantidade da porcao, o mesmo numero do rotulo (81 kcal, e nao 2 x 41)
        Assert.AreEqual(81f, CalculosPorcao.ValorPara(sprite.Buscar("energia"), sprite, 200f).Value, 0.001f);
    }

    [Test]
    public void ValorUsaAColunaPor100QuandoAPorcaoEstaEmOutraUnidade()
    {
        var maggi = Maggi();
        Assert.AreEqual(11.25f, CalculosPorcao.ValorPara(maggi.Buscar("carboidratos"), maggi, 250f).Value, 0.001f);
    }

    [Test]
    public void Por100VemDaPorcaoNoRotuloAntigo()
    {
        // Fanta: rotulo da RDC 360, so com a porcao de 200 ml
        var fanta = Fanta();
        Assert.AreEqual(6f, CalculosPorcao.Por100(fanta.Buscar("carboidratos"), fanta).Value, 0.001f);
        Assert.AreEqual(15.5f, CalculosPorcao.Por100(fanta.Buscar("sodio"), fanta).Value, 0.001f);
    }

    [Test]
    public void Por100NaoSaiDeUmaPorcaoEmOutraUnidade()
    {
        var dados = new DadosProduto
        {
            base100 = "100 ml",
            porcao = new DadosProduto.Porcao { quantidade = 17, unidade = "g" }
        };
        var sodio = new DadosProduto.Nutriente { chave = "sodio", unidade = "mg", porPorcao = 745 };
        dados.nutrientes.Add(sodio);
        Assert.IsNull(CalculosPorcao.Por100(sodio, dados));
        Assert.IsNull(CalculosPorcao.ValorPara(sodio, dados, 250f));
    }

    [Test]
    public void AcucaresAdicionadosOuTotais()
    {
        Assert.AreEqual("acucaresAdicionados", CalculosPorcao.Acucares(Sprite()).chave);
        Assert.AreEqual("acucaresTotais", CalculosPorcao.Acucares(Fanta()).chave, "o rotulo antigo so traz o total");
    }

    [Test]
    public void TextosDaQuantidadeEDoResultado()
    {
        Assert.AreEqual("200 ml", CalculosPorcao.TextoQuantidade(200f, "ml"));
        Assert.AreEqual("Carboidratos 20 g · Açúcares adicionados 20 g · 81 kcal",
            CalculosPorcao.TextoResultado(Sprite(), 200f));
        Assert.AreEqual("Carboidratos 12 g · Açúcares 10 g · 47 kcal", CalculosPorcao.TextoResultado(Fanta(), 200f));
        // Uma casa decimal, arredondando 0,05 para cima
        Assert.AreEqual("Carboidratos 11,3 g · Açúcares adicionados 1,3 g · 55 kcal",
            CalculosPorcao.TextoResultado(Maggi(), 250f));
    }
}
