using NUnit.Framework;
using UnityEngine;

// Testes de EditMode das contas do zoom, da pinca e do modo leitura.
public class TestesCalculosGestos
{
    [Test]
    public void ZoomFicaEntreMeioETres()
    {
        Assert.AreEqual(0.5f, CalculosGestos.LimitarZoom(0.1f));
        Assert.AreEqual(3f, CalculosGestos.LimitarZoom(10f));
        Assert.AreEqual(1.7f, CalculosGestos.LimitarZoom(1.7f), 1e-6f);
    }

    [Test]
    public void EscalaDaTabelaMantemAProporcao()
    {
        var escalaBase = new Vector3(0.16f, 0.045f, 1f);
        var escala = CalculosGestos.EscalaTabela(escalaBase, 2.5f);
        Assert.AreEqual(escalaBase.x / escalaBase.y, escala.x / escala.y, 1e-5f);
        Assert.AreEqual(0.4f, escala.x, 1e-5f);
        Assert.AreEqual(1f, escala.z, "o quad nao ganha espessura");
    }

    [Test]
    public void PincaAfastandoOsDedosAumenta()
    {
        Assert.AreEqual(2f, CalculosGestos.FatorPinca(100f, 200f), 1e-6f);
        Assert.AreEqual(0.5f, CalculosGestos.FatorPinca(200f, 100f), 1e-6f);
        // Dedos colados: nao divide por zero
        Assert.AreEqual(1f, CalculosGestos.FatorPinca(0f, 50f));
    }

    [Test]
    public void RodaDoMouseUsaSoOSentido()
    {
        // No Windows a roda pode vir como 120 ou como 1, conforme a configuracao do Input System
        Assert.AreEqual(CalculosGestos.FatorRoda(1f), CalculosGestos.FatorRoda(120f));
        Assert.Greater(CalculosGestos.FatorRoda(1f), 1f);
        Assert.Less(CalculosGestos.FatorRoda(-1f), 1f);
        Assert.AreEqual(1f, CalculosGestos.FatorRoda(0f));
    }

    [Test]
    public void TabelaCabeNaTelaSemDistorcer()
    {
        // Foto da tabela do Sprite (1298 x 364) num visor em retrato: limitada pela largura
        var tamanho = CalculosGestos.TamanhoAjustado(new Vector2(1298f, 364f), new Vector2(1000f, 1600f));
        Assert.AreEqual(1000f, tamanho.x, 0.01f);
        Assert.AreEqual(1298f / 364f, tamanho.x / tamanho.y, 1e-3f);

        // Foto da tabela do Ninho (1022 x 1271) num visor em paisagem: limitada pela altura
        tamanho = CalculosGestos.TamanhoAjustado(new Vector2(1022f, 1271f), new Vector2(1600f, 800f));
        Assert.AreEqual(800f, tamanho.y, 0.01f);
        Assert.AreEqual(1022f / 1271f, tamanho.x / tamanho.y, 1e-3f);
    }

    [Test]
    public void PontoSobOsDedosNaoSaiDoLugarNoZoom()
    {
        var posicaoTabela = new Vector2(30f, -20f);
        var dedos = new Vector2(100f, 50f);
        // Ponto da tabela que esta sob os dedos, medido a partir do centro dela (escala 1)
        Vector2 pontoNaTabela = dedos - posicaoTabela;

        var nova = CalculosGestos.ZoomEmTornoDoPonto(posicaoTabela, dedos, 2f);

        // Com o dobro da escala, o mesmo ponto da tabela continua sob os dedos
        Assert.Less(Vector2.Distance(dedos, nova + pontoNaTabela * 2f), 1e-4f);
    }

    [Test]
    public void TabelaPequenaFicaCentralizadaETabelaGrandeSoAteABorda()
    {
        var visor = new Vector2(1000f, 1600f);
        Assert.AreEqual(Vector2.zero,
            CalculosGestos.LimitarDeslocamento(new Vector2(300f, 300f), new Vector2(800f, 600f), visor));

        // Tabela de 2000 x 2400: sobram 500 de cada lado na horizontal e 400 na vertical
        var limitada = CalculosGestos.LimitarDeslocamento(new Vector2(900f, -900f), new Vector2(2000f, 2400f), visor);
        Assert.AreEqual(new Vector2(500f, -400f), limitada);
    }

    [Test]
    public void ZoomDaLeituraNaoDeixaATabelaMenorQueATela()
    {
        Assert.AreEqual(1f, CalculosGestos.LimitarZoomLeitura(0.3f));
        Assert.AreEqual(5f, CalculosGestos.LimitarZoomLeitura(9f));
    }

    [Test]
    public void ToqueCurtoEParadoContaComoToque()
    {
        Assert.IsTrue(CalculosGestos.EhToque(Vector2.zero, new Vector2(5f, 5f), 0.2f, 30f));
        Assert.IsFalse(CalculosGestos.EhToque(Vector2.zero, new Vector2(100f, 0f), 0.2f, 30f), "arrastou");
        Assert.IsFalse(CalculosGestos.EhToque(Vector2.zero, Vector2.zero, 1.5f, 30f), "segurou demais");
    }
}
