using NUnit.Framework;

// Testes de qual tabela aparece quando varias vistas ensinadas do mesmo produto estao na camera ao mesmo tempo:
// so a que entrou primeiro, para nao haver duas tabelas sobre o mesmo rotulo.
public class TestesCalculosRastreio
{
    [Test]
    public void SoAPrimeiraVistaDeCadaProdutoMostraATabela()
    {
        // Na ordem em que entraram na camera
        var gtins = new[] { "7898416527548", "78939745", "7898416527548", "7891150072053", "78939745" };
        CollectionAssert.AreEqual(new[] { true, true, false, true, false }, CalculosRastreio.QuaisMostrar(gtins));
    }

    [Test]
    public void MarcadorSemGtinContaSozinho()
    {
        CollectionAssert.AreEqual(new[] { true, true, true }, CalculosRastreio.QuaisMostrar(new string[] { null, null, "78939745" }));
    }

    [Test]
    public void NadaNaCameraNaoMostraNada()
    {
        CollectionAssert.IsEmpty(CalculosRastreio.QuaisMostrar(new string[0]));
    }
}
