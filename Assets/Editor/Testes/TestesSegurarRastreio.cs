using NUnit.Framework;

// Testes da espera antes de esconder a tabela quando o rotulo sai do rastreio. No diario de 14/10/2026, metade das
// saidas voltou em ate 0,5 s: a tabela piscava sem o rotulo ter saido da camera de verdade.
public class TestesSegurarRastreio
{
    const float Espera = 0.5f;

    [Test]
    public void PrimeiroEncontroMostraATabela()
    {
        var segurar = new SegurarRastreio(Espera);
        Assert.IsFalse(segurar.Visivel);
        Assert.AreEqual(SegurarRastreio.Acao.Mostrar, segurar.Encontrado(0f));
        Assert.IsTrue(segurar.Visivel);
    }

    [Test]
    public void QuedaCurtaNaoEscondeEEContada()
    {
        var segurar = new SegurarRastreio(Espera);
        segurar.Encontrado(0f);
        segurar.Perdido(1f);
        Assert.AreEqual(SegurarRastreio.Acao.Nenhuma, segurar.Atualizar(1.3f));
        Assert.AreEqual(SegurarRastreio.Acao.Nenhuma, segurar.Encontrado(1.4f), "voltou dentro da espera");
        Assert.AreEqual(SegurarRastreio.Acao.Nenhuma, segurar.Atualizar(2f));
        Assert.IsTrue(segurar.Visivel);
        Assert.AreEqual(1, segurar.QuedasCurtas);
    }

    [Test]
    public void PerdaLongaEscondeDepoisDaEspera()
    {
        var segurar = new SegurarRastreio(Espera);
        segurar.Encontrado(0f);
        segurar.Perdido(1f);
        segurar.Encontrado(1.2f);
        segurar.Perdido(2f);
        Assert.AreEqual(SegurarRastreio.Acao.Nenhuma, segurar.Atualizar(2.49f));
        Assert.AreEqual(SegurarRastreio.Acao.Esconder, segurar.Atualizar(2.5f));
        Assert.IsFalse(segurar.Visivel);
        Assert.AreEqual(1, segurar.QuedasCurtas, "a contagem vale ate a tabela sumir, para o diario");
        Assert.AreEqual(SegurarRastreio.Acao.Nenhuma, segurar.Atualizar(3f), "esconde uma vez so");

        Assert.AreEqual(SegurarRastreio.Acao.Mostrar, segurar.Encontrado(4f));
        Assert.AreEqual(0, segurar.QuedasCurtas, "a contagem recomeca quando a tabela aparece de novo");
    }

    [Test]
    public void PerdaSemTerMostradoNaoFazNada()
    {
        var segurar = new SegurarRastreio(Espera);
        segurar.Perdido(0f);
        Assert.AreEqual(SegurarRastreio.Acao.Nenhuma, segurar.Atualizar(1f));
    }

    [Test]
    public void EncontradoDuasVezesMostraUmaVez()
    {
        var segurar = new SegurarRastreio(Espera);
        Assert.AreEqual(SegurarRastreio.Acao.Mostrar, segurar.Encontrado(0f));
        Assert.AreEqual(SegurarRastreio.Acao.Nenhuma, segurar.Encontrado(0.1f));
        Assert.AreEqual(0, segurar.QuedasCurtas);
    }

    [Test]
    public void PerdidoRepetidoNaoReiniciaAEspera()
    {
        var segurar = new SegurarRastreio(Espera);
        segurar.Encontrado(0f);
        segurar.Perdido(1f);
        segurar.Perdido(1.3f);
        Assert.AreEqual(SegurarRastreio.Acao.Esconder, segurar.Atualizar(1.5f));
    }
}
