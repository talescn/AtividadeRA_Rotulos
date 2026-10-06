using System.Collections.Generic;
using NUnit.Framework;
using ZXing;
using ZXing.Common;

// Testes do leitor de codigo de barras sem camera: as imagens sao geradas pelo proprio ZXing.
public class TestesCodigoBarras
{
    const int Largura = 640;
    const int Altura = 240;

    [Test]
    public void LeOEan13DoNinho()
    {
        var cinza = GerarCinza("7898215157403", BarcodeFormat.EAN_13, out int largura, out int altura);
        Assert.AreEqual("7898215157403", DecodificadorBarras.Decodificar(cinza, largura, altura, largura, 1));
    }

    [Test]
    public void LeOEan8DoSprite()
    {
        var cinza = GerarCinza("78939745", BarcodeFormat.EAN_8, out int largura, out int altura);
        Assert.AreEqual("78939745", DecodificadorBarras.Decodificar(cinza, largura, altura, largura, 1));
    }

    [Test]
    public void LeCodigoDeitadoComoNaImagemDaCamera()
    {
        // Com o celular em retrato, a imagem da camera chega girada 90 graus em relacao a tela
        var cinza = GerarCinza("7898215157403", BarcodeFormat.EAN_13, out int largura, out int altura);
        var girado = Girar90(cinza, largura, altura);
        Assert.AreEqual("7898215157403", DecodificadorBarras.Decodificar(girado, altura, largura, altura, 1));
    }

    [Test]
    public void LeImagemRgbComPreenchimentoNoFimDaLinha()
    {
        var cinza = GerarCinza("7898215157403", BarcodeFormat.EAN_13, out int largura, out int altura);
        int stride = largura * 3 + 16;
        var rgb = new byte[stride * altura];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
                for (int c = 0; c < 3; c++)
                    rgb[y * stride + x * 3 + c] = cinza[y * largura + x];

        Assert.AreEqual("7898215157403", DecodificadorBarras.Decodificar(rgb, largura, altura, stride, 3));
    }

    [Test]
    public void ImagemSemCodigoNaoDaLeitura()
    {
        var branco = new byte[Largura * Altura];
        for (int i = 0; i < branco.Length; i++) branco[i] = 255;
        Assert.IsNull(DecodificadorBarras.Decodificar(branco, Largura, Altura, Largura, 1));
        Assert.IsNull(DecodificadorBarras.Decodificar(null, Largura, Altura, Largura, 1));
    }

    [Test]
    public void ConfirmaSoComDuasLeiturasIguais()
    {
        var confirmacao = new LeituraEstavel(leiturasIguais: 2, janelaSegundos: 1.5f, silencioSegundos: 8f);
        Assert.IsNull(confirmacao.Registrar("7898215157403", 0f));
        Assert.AreEqual("7898215157403", confirmacao.Registrar("7898215157403", 0.3f));
    }

    [Test]
    public void QuadroSemCodigoEntreDuasLeiturasNaoAtrapalha()
    {
        var confirmacao = new LeituraEstavel(2, 1.5f, 8f);
        Assert.IsNull(confirmacao.Registrar("78939745", 0f));
        Assert.IsNull(confirmacao.Registrar(null, 0.3f));
        Assert.AreEqual("78939745", confirmacao.Registrar("78939745", 0.6f));
    }

    [Test]
    public void LeiturasDiferentesOuDistantesNaoConfirmam()
    {
        var confirmacao = new LeituraEstavel(2, 1.5f, 8f);
        Assert.IsNull(confirmacao.Registrar("7898215157403", 0f));
        Assert.IsNull(confirmacao.Registrar("7898215157404", 0.3f), "numero diferente comeca a contagem de novo");
        Assert.IsNull(confirmacao.Registrar("7898215157403", 0.6f));
        Assert.IsNull(confirmacao.Registrar("7898215157403", 5f), "longe demais da leitura anterior");
    }

    [Test]
    public void MesmoCodigoFicaEmSilencioDepoisDeAceito()
    {
        var confirmacao = new LeituraEstavel(2, 1.5f, 8f);
        confirmacao.Registrar("78939745", 0f);
        Assert.AreEqual("78939745", confirmacao.Registrar("78939745", 0.3f));

        confirmacao.Registrar("78939745", 1f);
        Assert.IsNull(confirmacao.Registrar("78939745", 1.3f), "acabou de ser aceito");

        confirmacao.Registrar("78939745", 9f);
        Assert.AreEqual("78939745", confirmacao.Registrar("78939745", 9.3f), "passado o silencio, vale de novo");
    }

    // Imagem em tons de cinza (0 = barra, 255 = fundo), como a camera entrega no formato GRAYSCALE
    static byte[] GerarCinza(string codigo, BarcodeFormat formato, out int largura, out int altura)
    {
        var dicas = new Dictionary<EncodeHintType, object> { { EncodeHintType.MARGIN, 20 } };
        BitMatrix matriz = new MultiFormatWriter().encode(codigo, formato, Largura, Altura, dicas);
        largura = matriz.Width;
        altura = matriz.Height;
        var pixels = new byte[largura * altura];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
                pixels[y * largura + x] = matriz[x, y] ? (byte)0 : (byte)255;
        return pixels;
    }

    static byte[] Girar90(byte[] pixels, int largura, int altura)
    {
        var girado = new byte[pixels.Length];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
                girado[x * altura + (altura - 1 - y)] = pixels[y * largura + x];
        return girado;
    }
}
