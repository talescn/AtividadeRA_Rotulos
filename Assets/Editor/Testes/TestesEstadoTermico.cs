using NUnit.Framework;

// Testes da linha de temperatura do diario: o estado termico do Android em texto, gravado so quando muda.
public class TestesEstadoTermico
{
    [Test]
    public void DaNomeAosEstadosDoAndroid()
    {
        Assert.AreEqual("normal", EstadoTermico.Nome(0));
        Assert.AreEqual("leve", EstadoTermico.Nome(1));
        Assert.AreEqual("moderado", EstadoTermico.Nome(2));
        Assert.AreEqual("severo", EstadoTermico.Nome(3));
        Assert.AreEqual("critico", EstadoTermico.Nome(4));
        Assert.AreEqual("emergencia", EstadoTermico.Nome(5));
        Assert.AreEqual("desligando", EstadoTermico.Nome(6));
        Assert.AreEqual("desconhecido", EstadoTermico.Nome(7), "estado que uma versao nova do Android criar");
        Assert.AreEqual("desconhecido", EstadoTermico.Nome(-1));
    }

    [Test]
    public void PrimeiraMedidaViraLinha()
    {
        // Mesmo "normal" entra no diario: e a referencia para comparar a velocidade do leitor depois
        Assert.AreEqual("[RA] Temperatura: estado termico 0 (normal)", new EstadoTermico().Registrar(0));
    }

    [Test]
    public void SoGravaQuandoOEstadoMuda()
    {
        var temperatura = new EstadoTermico();
        temperatura.Registrar(2);
        Assert.IsNull(temperatura.Registrar(2), "mesmo estado");
        Assert.AreEqual("[RA] Temperatura: estado termico 3 (severo)", temperatura.Registrar(3), "esquentou");
        Assert.IsNull(temperatura.Registrar(3));
        Assert.AreEqual("[RA] Temperatura: estado termico 1 (leve)", temperatura.Registrar(1), "esfriou");
    }

    [Test]
    public void FalhaDoAparelhoViraLinhaDeIndisponivel()
    {
        Assert.AreEqual("[RA] Temperatura: indisponivel (servico de energia ausente)",
            EstadoTermico.Indisponivel("servico de energia ausente"));
    }
}
