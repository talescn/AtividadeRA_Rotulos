using NUnit.Framework;

// Testes da medida de cada tentativa de leitura de codigo de barras, gravada no diario: o tempo vai do primeiro
// quadro em que um codigo foi decodificado ate a confirmacao, ou a tentativa acaba sem leitura.
public class TestesMedidaLeitura
{
    const string Sprite = "78939745";
    const string Ninho = "7898215157403";
    const string NinhoErrado = "7898215157404";

    [Test]
    public void ConfirmacaoDizOTempoDesdeOPrimeiroQuadroComCodigo()
    {
        var medida = new MedidaLeitura();
        medida.Registrar(null, 0f);
        medida.Registrar(Sprite, 1f);
        medida.Registrar(Sprite, 2.2f);

        Assert.AreEqual(Sprite + " em 1,2 s (2 quadros com codigo, outros codigos: nenhum)", medida.Confirmar(Sprite, 2.2f));
        Assert.IsFalse(medida.EmAndamento);
    }

    [Test]
    public void CodigoErradoApareceNaConfirmacao()
    {
        var medida = new MedidaLeitura();
        medida.Registrar(NinhoErrado, 0f);
        medida.Registrar(Ninho, 0.5f);
        medida.Registrar(Ninho, 0.8f);

        Assert.AreEqual(Ninho + " em 0,8 s (3 quadros com codigo, outros codigos: " + NinhoErrado + ")",
            medida.Confirmar(Ninho, 0.8f));
    }

    [Test]
    public void TentativaSemLeituraAcabaDepoisDoSilencio()
    {
        var medida = new MedidaLeitura(silencioFim: 10f);
        medida.Registrar(Ninho, 0f);
        medida.Registrar(NinhoErrado, 3f);

        Assert.IsNull(medida.VerificarSilencio(12.9f));
        Assert.AreEqual("2 quadros com codigo em 3 s (" + Ninho + ", " + NinhoErrado + ")", medida.VerificarSilencio(13f));
        Assert.IsFalse(medida.EmAndamento);
        Assert.IsNull(medida.VerificarSilencio(30f), "a tentativa ja acabou");
    }

    [Test]
    public void SemTentativaNaoHaNadaParaEncerrar()
    {
        var medida = new MedidaLeitura();
        Assert.IsNull(medida.Abandonar(5f));
        medida.Registrar(null, 1f);
        Assert.IsFalse(medida.EmAndamento, "quadro sem codigo nao comeca tentativa");
    }

    [Test]
    public void DepoisDaConfirmacaoComecaOutraTentativa()
    {
        var medida = new MedidaLeitura();
        medida.Registrar(Sprite, 0f);
        medida.Confirmar(Sprite, 0.3f);

        medida.Registrar(Ninho, 20f);
        Assert.AreEqual(Ninho + " em 0 s (1 quadro com codigo, outros codigos: nenhum)", medida.Confirmar(Ninho, 20f));
    }

    [Test]
    public void LeitorDesligadoEncerraATentativaSemLeitura()
    {
        // Um rotulo entrou na camera antes de o codigo ser confirmado
        var medida = new MedidaLeitura();
        medida.Registrar(Ninho, 1f);
        Assert.AreEqual("1 quadro com codigo em 0 s (" + Ninho + ")", medida.Abandonar(1.4f));
    }
}
