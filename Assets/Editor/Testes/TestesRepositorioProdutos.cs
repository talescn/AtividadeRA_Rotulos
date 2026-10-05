using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

// Testes da busca de dados: JSON local, cache no aparelho, conversao da resposta do Open Food Facts
// e limite de consultas. Nenhum teste usa a rede.
public class TestesRepositorioProdutos
{
    string pastaCache;

    [SetUp]
    public void CriarPastaCache()
    {
        pastaCache = Path.Combine(Path.GetTempPath(), "TestesRotulosRA_" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void ApagarPastaCache()
    {
        if (Directory.Exists(pastaCache)) Directory.Delete(pastaCache, true);
    }

    [Test]
    public void LeOJsonLocalPeloGtin()
    {
        var d = new RepositorioProdutos(pastaCache).BuscarSemRede("7898215157403");
        Assert.IsNotNull(d);
        StringAssert.Contains("Ninho", d.nome);
        Assert.AreEqual(DadosProduto.FonteRotulo, d.fonte);
        Assert.AreEqual(63f, d.Buscar("sodio").por100);
        Assert.IsNull(d.Buscar("acucaresTotais").vd, "o rotulo nao traz %VD de acucares totais");
    }

    [Test]
    public void GtinSemJsonNemCacheNaoTemDado()
    {
        var repositorio = new RepositorioProdutos(pastaCache);
        Assert.IsNull(repositorio.BuscarSemRede("7891000000001"));
        Assert.IsNull(repositorio.BuscarSemRede(""));
        Assert.IsNull(repositorio.BuscarSemRede(null));
    }

    [Test]
    public void JsonLocalVemAntesDoCache()
    {
        // Um cache antigo com o mesmo GTIN nao pode esconder a transcricao do rotulo
        var doOpenFoodFacts = RepositorioProdutos.ConverterOpenFoodFacts(RespostaNinho());
        var repositorio = new RepositorioProdutos(pastaCache);
        repositorio.SalvarCache(doOpenFoodFacts);

        Assert.AreEqual(DadosProduto.FonteRotulo, repositorio.BuscarSemRede("7898215157403").fonte);
    }

    [Test]
    public void CacheGuardaEDevolveOMesmoProduto()
    {
        var original = RepositorioProdutos.ConverterOpenFoodFacts(RespostaNinho());
        // GTIN sem JSON local, para a busca chegar ao cache
        original.gtin = "7891000000001";
        new RepositorioProdutos(pastaCache).SalvarCache(original);

        // Outro repositorio, como se o app tivesse sido aberto de novo
        var lido = new RepositorioProdutos(pastaCache).BuscarSemRede(original.gtin);
        Assert.IsNotNull(lido);
        Assert.AreEqual(original.nome, lido.nome);
        Assert.AreEqual(original.conferidoEm, lido.conferidoEm);
        Assert.AreEqual(original.nutrientes.Count, lido.nutrientes.Count);
        Assert.AreEqual(63f, lido.Buscar("sodio").por100);
    }

    [Test]
    public void CacheQuebradoNaoDerrubaOApp()
    {
        Directory.CreateDirectory(pastaCache);
        File.WriteAllText(Path.Combine(pastaCache, "7891000000001.json"), "{ isto nao e json");

        LogAssert.Expect(LogType.Warning, new Regex(@"\[RA\] Cache invalido para 7891000000001"));
        Assert.IsNull(new RepositorioProdutos(pastaCache).BuscarSemRede("7891000000001"));
    }

    [Test]
    public void ConverteARespostaDoOpenFoodFacts()
    {
        var d = RepositorioProdutos.ConverterOpenFoodFacts(RespostaNinho());

        Assert.AreEqual("7898215157403", d.gtin);
        Assert.AreEqual("Leite Ninho Integral", d.nome, "usa o nome em portugues quando existe");
        Assert.AreEqual("Ninho, Nestlé", d.marca);
        Assert.AreEqual(DadosProduto.FonteOpenFoodFacts, d.fonte);
        Assert.AreEqual("2026-04-22", d.conferidoEm);
        Assert.AreEqual(200f, d.porcao.quantidade);
        Assert.AreEqual("g", d.porcao.unidade);
        Assert.AreEqual(59f, d.Buscar("energia").por100);
        // O Open Food Facts guarda massas em gramas
        Assert.AreEqual(63f, d.Buscar("sodio").por100, "sodio em mg");
        Assert.AreEqual(150f, d.Buscar("calcio").por100, "calcio em mg");
        Assert.AreEqual(120f, d.Buscar("vitaminaA").por100, "vitamina A em µg");
        Assert.AreEqual(126f, d.Buscar("sodio").porPorcao);
        Assert.IsNull(d.Buscar("sodio").vd, "a base nao tem %VD");
        Assert.AreEqual(1, d.Buscar("acucaresTotais").nivel);
        StringAssert.StartsWith("Leite integral", d.ingredientes);
    }

    [Test]
    public void RespostaSemTabelaNaoServe()
    {
        // Como o Sprite 200 ml hoje: tem nome e ingredientes, mas nenhum nutriente
        string json = "{\"code\":\"78939745\",\"status\":1,\"product\":{\"product_name\":\"Refrig. Sprite 200ml\"," +
                      "\"ingredients_text_pt\":\"ÁGUA GASEIFICADA, AÇÚCAR\",\"nutriments\":{}}}";
        Assert.IsNull(RepositorioProdutos.ConverterOpenFoodFacts(json));
    }

    [Test]
    public void ProdutoForaDaBaseDevolveNull()
    {
        string json = "{\"code\":\"7891000000001\",\"status\":0,\"status_verbose\":\"product not found\"}";
        Assert.IsNull(RepositorioProdutos.ConverterOpenFoodFacts(json));
    }

    [Test]
    public void NumeroComoTextoTambemServe()
    {
        string json = "{\"code\":\"78939745\",\"status\":1,\"product\":{\"serving_size\":\"200 ml\",\"serving_quantity\":\"200\"," +
                      "\"nutriments\":{\"energy-kcal_100g\":\"41\",\"sodium_100g\":\"0.0076\"}}}";
        var d = RepositorioProdutos.ConverterOpenFoodFacts(json);
        Assert.AreEqual(41f, d.Buscar("energia").por100);
        Assert.AreEqual(7.6f, d.Buscar("sodio").por100);
        Assert.AreEqual("ml", d.porcao.unidade);
        Assert.AreEqual("100 ml", d.base100);
    }

    [Test]
    public void UserAgentSegueOFormatoPedidoPeloOpenFoodFacts()
    {
        StringAssert.IsMatch(@"^AtividadeRA_Rotulos/\S+ \(https://github\.com/talescn/AtividadeRA_Rotulos\)$", RepositorioProdutos.UserAgent);
    }

    [Test]
    public void LimiteDeQuinzeConsultasPorMinuto()
    {
        var limite = new LimiteRequisicoes(15, 60f);
        for (int i = 0; i < 15; i++)
            Assert.IsTrue(limite.TentarRegistrar(i), "consulta " + (i + 1));
        Assert.IsFalse(limite.TentarRegistrar(20f), "a 16a consulta no mesmo minuto espera");
        // 60 s depois da primeira, abre uma vaga
        Assert.IsTrue(limite.TentarRegistrar(60.5f));
        Assert.IsFalse(limite.TentarRegistrar(60.6f));
    }

    // No formato da API v2, com os valores do Ninho Integral que o Open Food Facts devolveu em 05/10/2026 (resumida)
    static string RespostaNinho()
    {
        long modificadoEm = new DateTimeOffset(2026, 4, 22, 12, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
        return "{\"code\":\"7898215157403\",\"status\":1,\"status_verbose\":\"product found\",\"product\":{" +
               "\"product_name\":\"Ninho\",\"product_name_pt\":\"Leite Ninho Integral\",\"brands\":\"Ninho, Nestlé\"," +
               "\"serving_size\":\"200 g\",\"serving_quantity\":200,\"last_modified_t\":" + modificadoEm + "," +
               "\"ingredients_text_pt\":\"Leite integral, minerais [cálcio (fosfato tricálcico)]\"," +
               "\"nutriments\":{\"energy-kcal_100g\":59,\"energy-kcal_serving\":118,\"carbohydrates_100g\":4.4," +
               "\"sugars_100g\":4.4,\"proteins_100g\":3.1,\"fat_100g\":3.2,\"saturated-fat_100g\":2," +
               "\"sodium_100g\":0.063,\"sodium_serving\":0.126,\"calcium_100g\":0.15,\"vitamin-a_100g\":0.00012}}}";
    }
}
