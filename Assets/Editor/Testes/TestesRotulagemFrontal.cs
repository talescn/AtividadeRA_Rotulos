using NUnit.Framework;

// Testes da lupa "ALTO EM" calculada pela tabela (limites do Anexo XV da IN 75/2020, maior ou igual).
// O gabarito dos produtos reais e a lupa impressa na frente de cada embalagem.
public class TestesRotulagemFrontal
{
    const string AcucarAdicionado = "AÇÚCAR ADICIONADO";

    static RotulagemFrontal.Resultado Avaliar(string gtin)
    {
        return RotulagemFrontal.Avaliar(RepositorioProdutos.CarregarLocal(gtin));
    }

    // Produto com um nutriente so, na coluna "por 100"
    static DadosProduto Produto(string base100, string chave, string unidade, float? por100)
    {
        var dados = new DadosProduto { base100 = base100 };
        dados.nutrientes.Add(new DadosProduto.Nutriente { chave = chave, unidade = unidade, por100 = por100 });
        return dados;
    }

    static RotulagemFrontal.Estado Estado(DadosProduto dados, string chave)
    {
        foreach (var item in RotulagemFrontal.Avaliar(dados).itens)
            if (item.chave == chave) return item.estado;
        Assert.Fail("sem item " + chave);
        return RotulagemFrontal.Estado.SemDado;
    }

    // ---------- Produtos de teste ----------

    [Test]
    public void SpriteEAltoEmAcucarAdicionado()
    {
        // 10 g por 100 ml, acima dos 7,5 g dos liquidos
        var lupa = Avaliar("78939745");
        CollectionAssert.AreEqual(new[] { AcucarAdicionado }, lupa.Altos());
        CollectionAssert.IsEmpty(lupa.FaltaDado());
        Assert.IsNull(lupa.isencao);
    }

    [Test]
    public void MaggiProntoFicaAbaixoDoLimiteDeSodio()
    {
        // RDC 429, art. 19: alimento que precisa de preparo e avaliado pronto (298 mg por 100 ml, limite 300)
        var lupa = Avaliar("7891000538500");
        CollectionAssert.IsEmpty(lupa.Altos());
        CollectionAssert.IsEmpty(lupa.FaltaDado());
    }

    [Test]
    public void LeiteNaoPodeTerLupa()
    {
        var lupa = Avaliar("7898215157403");
        Assert.IsNotNull(lupa.isencao);
        CollectionAssert.IsEmpty(lupa.Altos());
        CollectionAssert.IsEmpty(lupa.itens);
        Assert.AreEqual("A lupa não se aplica a leites (IN 75/2020, Anexo XVI, item 7).", RotulagemFrontal.TextoIsencao(lupa));
    }

    [Test]
    public void FantaNaoDeclaraAcucarAdicionado()
    {
        // Rotulo antigo (RDC 360): sem acucar adicionado; sodio e gordura saturada saem da porcao
        var lupa = Avaliar("78938571");
        CollectionAssert.AreEqual(new[] { AcucarAdicionado }, lupa.FaltaDado());
        CollectionAssert.IsEmpty(lupa.Altos());
        Assert.AreEqual("Sem dado de açúcar adicionado neste rótulo.", RotulagemFrontal.TextoFaltaDado(lupa));
        Assert.IsNull(RotulagemFrontal.TextoIsencao(lupa));
    }

    // ---------- Limites ----------

    [Test]
    public void NoLimiteJaEAlto()
    {
        Assert.AreEqual(RotulagemFrontal.Estado.Alto, Estado(Produto("100 ml", "acucaresAdicionados", "g", 7.5f), "acucaresAdicionados"));
        Assert.AreEqual(RotulagemFrontal.Estado.Abaixo, Estado(Produto("100 ml", "acucaresAdicionados", "g", 7.4f), "acucaresAdicionados"));
        Assert.AreEqual(RotulagemFrontal.Estado.Alto, Estado(Produto("100 g", "acucaresAdicionados", "g", 15f), "acucaresAdicionados"));
        Assert.AreEqual(RotulagemFrontal.Estado.Abaixo, Estado(Produto("100 g", "acucaresAdicionados", "g", 14.9f), "acucaresAdicionados"));
        Assert.AreEqual(RotulagemFrontal.Estado.Alto, Estado(Produto("100 ml", "gordurasSaturadas", "g", 3f), "gordurasSaturadas"));
        Assert.AreEqual(RotulagemFrontal.Estado.Abaixo, Estado(Produto("100 g", "gordurasSaturadas", "g", 5.9f), "gordurasSaturadas"));
        Assert.AreEqual(RotulagemFrontal.Estado.Alto, Estado(Produto("100 g", "sodio", "mg", 600f), "sodio"));
        Assert.AreEqual(RotulagemFrontal.Estado.Abaixo, Estado(Produto("100 ml", "sodio", "mg", 299f), "sodio"));
    }

    [Test]
    public void SodioEmGramasEConvertido()
    {
        Assert.AreEqual(RotulagemFrontal.Estado.Alto, Estado(Produto("100 g", "sodio", "g", 0.6f), "sodio"));
    }

    [Test]
    public void SemValorFicaSemDadoENuncaAbaixo()
    {
        var dados = Produto("100 g", "sodio", "mg", null);
        Assert.AreEqual(RotulagemFrontal.Estado.SemDado, Estado(dados, "sodio"));
        Assert.AreEqual(RotulagemFrontal.Estado.SemDado, Estado(dados, "acucaresAdicionados"), "nutriente ausente da tabela");
    }

    [Test]
    public void VariosAltosEFaltasViramUmaFraseSo()
    {
        Assert.AreEqual("açúcar adicionado", RotulagemFrontal.Juntar(new[] { "açúcar adicionado" }));
        Assert.AreEqual("açúcar adicionado e sódio", RotulagemFrontal.Juntar(new[] { "açúcar adicionado", "sódio" }));
        Assert.AreEqual("açúcar adicionado, gordura saturada e sódio",
            RotulagemFrontal.Juntar(new[] { "açúcar adicionado", "gordura saturada", "sódio" }));

        var dados = new DadosProduto { base100 = "100 g" };
        dados.nutrientes.Add(new DadosProduto.Nutriente { chave = "gordurasSaturadas", unidade = "g", por100 = 1f });
        Assert.AreEqual("Sem dado de açúcar adicionado e sódio neste rótulo.",
            RotulagemFrontal.TextoFaltaDado(RotulagemFrontal.Avaliar(dados)));
    }
}
