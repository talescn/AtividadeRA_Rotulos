using System;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;

// Confere os JSONs de Resources/Produtos: campos obrigatorios, GTIN valido e o valor energetico
// batendo com os macronutrientes (4 kcal por grama de carboidrato e de proteina, 9 de gordura).
public class TestesDadosProdutos
{
    // O rotulo arredonda cada valor; 15% de diferenca cobre esses arredondamentos
    const float ToleranciaEnergia = 0.15f;

    static TextAsset[] Arquivos()
    {
        return Resources.LoadAll<TextAsset>(RepositorioProdutos.PastaRecursos);
    }

    [Test]
    public void ExistemOsJsonsDoSpriteEDoNinho()
    {
        Assert.IsNotNull(RepositorioProdutos.CarregarLocal("78939745"), "Sprite 200 ml");
        Assert.IsNotNull(RepositorioProdutos.CarregarLocal("7898215157403"), "Ninho Integral 1 L");
    }

    [Test]
    public void CadaJsonTemOsCamposObrigatorios()
    {
        Assert.Greater(Arquivos().Length, 0);
        foreach (var arquivo in Arquivos())
        {
            var d = RepositorioProdutos.Ler(arquivo.text);
            string nome = arquivo.name;
            Assert.AreEqual(nome, d.gtin, "o nome do arquivo precisa ser o GTIN");
            Assert.IsNotEmpty(d.nome, nome);
            Assert.IsNotEmpty(d.marca, nome);
            Assert.IsTrue(DateTime.TryParseExact(d.conferidoEm, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
                nome + ": conferidoEm deve ser AAAA-MM-DD");
            CollectionAssert.Contains(new[] { DadosProduto.FonteRotulo, DadosProduto.FonteOpenFoodFacts }, d.fonte, nome);
            Assert.IsNotNull(d.porcao, nome + ": sem porcao");
            Assert.Greater(d.porcao.quantidade ?? 0f, 0f, nome + ": porcao sem quantidade");
            CollectionAssert.Contains(new[] { "g", "ml" }, d.porcao.unidade, nome);
            Assert.IsNotEmpty(d.base100, nome);
            Assert.IsTrue(d.TemTabela, nome + ": sem valor energetico");
            foreach (var n in d.nutrientes)
            {
                Assert.IsNotEmpty(n.chave, nome);
                Assert.IsNotEmpty(n.nome, nome);
                CollectionAssert.Contains(new[] { "kcal", "g", "mg", "µg" }, n.unidade, nome + ": " + n.nome);
            }
        }
    }

    [Test]
    public void GtinTemDigitoVerificadorValido()
    {
        foreach (var arquivo in Arquivos())
            Assert.IsTrue(DadosProduto.GtinValido(arquivo.name), arquivo.name);

        Assert.IsFalse(DadosProduto.GtinValido("7898215157404"), "digito verificador errado");
        Assert.IsFalse(DadosProduto.GtinValido("78982151574"), "tamanho invalido");
        Assert.IsFalse(DadosProduto.GtinValido(null));
    }

    [Test]
    public void ValorEnergeticoBateComOsMacronutrientes()
    {
        foreach (var arquivo in Arquivos())
        {
            var d = RepositorioProdutos.Ler(arquivo.text);
            VerificarEnergia(d, n => n.por100, d.base100);
            VerificarEnergia(d, n => n.porPorcao, "porcao");
        }
    }

    static void VerificarEnergia(DadosProduto d, Func<DadosProduto.Nutriente, float?> coluna, string nomeColuna)
    {
        float? kcal = Valor(d, "energia", coluna);
        float? carboidratos = Valor(d, "carboidratos", coluna);
        float? proteinas = Valor(d, "proteinas", coluna);
        float? gorduras = Valor(d, "gordurasTotais", coluna);
        Assert.IsTrue(kcal.HasValue && carboidratos.HasValue && proteinas.HasValue && gorduras.HasValue,
            $"{d.nome} ({nomeColuna}): falta energia ou macronutriente");

        float calculado = 4f * carboidratos.Value + 4f * proteinas.Value + 9f * gorduras.Value;
        float diferenca = Mathf.Abs(kcal.Value - calculado) / kcal.Value;
        Assert.LessOrEqual(diferenca, ToleranciaEnergia,
            $"{d.nome} ({nomeColuna}): {kcal} kcal no rotulo e {calculado:0.0} kcal pelos macronutrientes");
    }

    static float? Valor(DadosProduto d, string chave, Func<DadosProduto.Nutriente, float?> coluna)
    {
        var n = d.Buscar(chave);
        return n != null ? coluna(n) : null;
    }
}
