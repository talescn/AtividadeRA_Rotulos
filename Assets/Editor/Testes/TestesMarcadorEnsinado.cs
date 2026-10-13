using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

// Testes do "Ensinar rotulo" sem camera: operacoes na imagem em cinza, nota do marcador (com a foto real do
// rotulo do Maggi), retangulo de recorte e o catalogo salvo no aparelho (aqui, numa pasta temporaria).
public class TestesMarcadorEnsinado
{
    const string FotoMaggi = "Assets/Fotos/rotulo3.jpg";

    string pasta;

    [SetUp]
    public void CriarPasta()
    {
        pasta = Path.Combine(Path.GetTempPath(), "TestesMarcadores_" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void ApagarPasta()
    {
        if (Directory.Exists(pasta)) Directory.Delete(pasta, true);
    }

    // ---------- Imagem em cinza ----------

    [Test]
    public void GirarHorarioPoeAColunaDaEsquerdaNoTopo()
    {
        // 1 2 3        4 1
        // 4 5 6   ->   5 2
        //              6 3
        byte[] imagem = { 1, 2, 3, 4, 5, 6 };
        CollectionAssert.AreEqual(new byte[] { 4, 1, 5, 2, 6, 3 }, ImagemCinza.GirarHorario(imagem, 3, 2));
    }

    [Test]
    public void RecortarCopiaSoARegiao()
    {
        byte[] imagem = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        CollectionAssert.AreEqual(new byte[] { 5, 6, 8, 9 }, ImagemCinza.Recortar(imagem, 3, new RectInt(1, 1, 2, 2)));
    }

    [Test]
    public void ReduzirFazAMediaENaoAmplia()
    {
        // Colunas alternadas 0 e 200: cada bloco 2x2 tem media 100
        var imagem = new byte[640 * 400];
        for (int i = 0; i < imagem.Length; i++) imagem[i] = (byte)(i % 2 * 200);
        byte[] reduzida = ImagemCinza.Reduzir(imagem, 640, 400, 320, out int w, out int h);
        Assert.AreEqual(320, w);
        Assert.AreEqual(200, h);
        Assert.AreEqual(100, reduzida[0]);
        Assert.AreSame(imagem, ImagemCinza.Reduzir(imagem, 640, 400, 1000, out w, out h));
    }

    [Test]
    public void ParaTexturaPoeALinhaDeCimaNoTopoDaTextura()
    {
        // A textura do Unity comeca pela linha de baixo: o pixel de cima do byte[] vai para y = altura - 1
        var textura = ImagemCinza.ParaTextura(new byte[] { 10, 20, 30, 40 }, 2, 2);
        try
        {
            Assert.AreEqual(10, textura.GetPixel(0, 1).r * 255f, 0.5f);
            Assert.AreEqual(30, textura.GetPixel(0, 0).r * 255f, 0.5f);
            Assert.IsTrue(textura.isReadable, "o Vuforia le os pixels da textura");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(textura);
        }
    }

    [Test]
    public void NitidezCaiComBorrao()
    {
        byte[] nitida = Blocos(200, 200);
        byte[] borrada = Borrar(nitida, 200, 200, 4);
        Assert.Greater(ImagemCinza.Nitidez(nitida, 200, 200, 200), 10f * ImagemCinza.Nitidez(borrada, 200, 200, 200));
    }

    // ---------- Nota do marcador ----------

    [Test]
    public void ImagemLisaNaoServe()
    {
        var lisa = new byte[400 * 300];
        for (int i = 0; i < lisa.Length; i++) lisa[i] = 128;
        var a = QualidadeMarcador.Avaliar(lisa, 400, 300);
        Assert.AreEqual(0, a.estrelas);
        StringAssert.Contains("lisa", a.motivo);
    }

    [Test]
    public void RotuloDoMaggiTemNotaAlta()
    {
        byte[] cinza = CarregarFoto(FotoMaggi, out int w, out int h);
        var a = QualidadeMarcador.Avaliar(cinza, w, h);
        Assert.GreaterOrEqual(a.estrelas, 4, $"{a.pontos} pontos, densidade {a.densidade}, cobertura {a.cobertura}");
    }

    [Test]
    public void RotuloBorradoPerdeANota()
    {
        byte[] menor = FotoMaggiReduzida(out int w, out int h);
        int nitida = QualidadeMarcador.Avaliar(menor, w, h).estrelas;
        var borrada = QualidadeMarcador.Avaliar(Borrar(menor, w, h, 3), w, h);
        Assert.LessOrEqual(borrada.estrelas, 1, $"{borrada.pontos} pontos");
        Assert.Less(borrada.estrelas, nitida);
        Assert.IsNotNull(borrada.motivo);
    }

    [Test]
    public void ReflexoTiraEstrelaEExplicaOMotivo()
    {
        byte[] menor = FotoMaggiReduzida(out int w, out int h);
        int semReflexo = QualidadeMarcador.Avaliar(menor, w, h).estrelas;
        // Um quinto da imagem estourado de branco, como o brilho de uma lampada no rotulo
        var comReflexo = (byte[])menor.Clone();
        for (int i = 0; i < w * (h / 5); i++) comReflexo[i] = 255;
        var a = QualidadeMarcador.Avaliar(comReflexo, w, h);
        Assert.Less(a.estrelas, semReflexo);
        StringAssert.Contains("Reflexo", a.motivo);
    }

    [Test]
    public void RotuloPequenoNaImagemPedeParaAproximar()
    {
        byte[] cinza = CarregarFoto(FotoMaggi, out int w, out int h);
        byte[] pequeno = ImagemCinza.Reduzir(cinza, w, h, 150, out int pw, out int ph);
        StringAssert.Contains("aproxime", QualidadeMarcador.Avaliar(pequeno, pw, ph).motivo);
    }

    // ---------- Retangulo de recorte ----------

    [Test]
    public void CantoNaoSaiDaFotoNemCruzaOOutroLado()
    {
        var r = new Rect(0.2f, 0.2f, 0.5f, 0.5f);
        Rect maior = CalculosRecorte.MoverCanto(r, 2, new Vector2(1f, 1f));
        Assert.AreEqual(1f, maior.xMax, 1e-5f);
        Assert.AreEqual(1f, maior.yMax, 1e-5f);
        Assert.AreEqual(0.2f, maior.xMin, 1e-5f);

        Rect cruzado = CalculosRecorte.MoverCanto(r, 0, new Vector2(1f, 1f));
        Assert.AreEqual(r.xMax - CalculosRecorte.LadoMinimo, cruzado.xMin, 1e-5f);
        Assert.AreEqual(r.yMax - CalculosRecorte.LadoMinimo, cruzado.yMin, 1e-5f);
    }

    [Test]
    public void MoverNaoDeixaORetanguloSairDaFoto()
    {
        Rect r = CalculosRecorte.Mover(new Rect(0.5f, 0.5f, 0.4f, 0.3f), new Vector2(1f, -1f));
        Assert.AreEqual(0.6f, r.x, 1e-5f);
        Assert.AreEqual(0f, r.y, 1e-5f);
        Assert.AreEqual(0.4f, r.width, 1e-5f);
    }

    [Test]
    public void ParaPixelsContaAsLinhasDeCimaParaBaixo()
    {
        // Metade de cima da foto na UI (y de 0,5 a 1) = linhas 0 a 49 do byte[]
        Assert.AreEqual(new RectInt(50, 0, 100, 50), CalculosRecorte.ParaPixels(new Rect(0.25f, 0.5f, 0.5f, 0.5f), 200, 100));
    }

    // ---------- Catalogo ----------

    [Test]
    public void CatalogoGuardaEReabreOsMarcadores()
    {
        var catalogo = new CatalogoMarcadores(pasta);
        var m = catalogo.Adicionar("78938571", new byte[] { 1, 2, 3 }, 0.08f, 4, new DateTime(2026, 10, 13, 10, 15, 0, 123), null);
        Assert.AreEqual("78938571_20261013_101500_123.png", m.arquivo);
        Assert.AreEqual("2026-10-13T10:15:00", m.criadoEm);

        var reaberto = new CatalogoMarcadores(pasta);
        Assert.AreEqual(1, reaberto.Marcadores.Count);
        Assert.AreEqual(4, reaberto.Marcadores[0].estrelas);
        Assert.AreEqual(0.08f, reaberto.Marcadores[0].larguraMetros, 1e-6f);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, reaberto.LerImagem(reaberto.Marcadores[0]));
    }

    [Test]
    public void CatalogoTiraAVistaMaisAntigaAlemDoLimite()
    {
        var catalogo = new CatalogoMarcadores(pasta);
        var removidos = new List<CatalogoMarcadores.Marcador>();
        var inicio = new DateTime(2026, 10, 13, 10, 0, 0);
        for (int i = 0; i <= CatalogoMarcadores.VistasPorProduto; i++)
            catalogo.Adicionar("78938571", new[] { (byte)i }, 0.08f, 3, inicio.AddMinutes(i), removidos);
        catalogo.Adicionar("7898215157403", new byte[] { 9 }, 0.08f, 5, inicio, removidos);

        Assert.AreEqual(CatalogoMarcadores.VistasPorProduto, catalogo.ContarVistas("78938571"));
        Assert.AreEqual(1, catalogo.ContarVistas("7898215157403"), "o limite e por produto");
        Assert.AreEqual(1, removidos.Count);
        Assert.AreEqual(CatalogoMarcadores.NomeArquivo("78938571", inicio), removidos[0].arquivo);
        Assert.IsFalse(File.Exists(Path.Combine(pasta, removidos[0].arquivo)), "o PNG da vista removida sai da pasta");
    }

    [Test]
    public void CatalogoRecusaGtinInvalidoEAguentaIndiceEstragado()
    {
        var catalogo = new CatalogoMarcadores(pasta);
        Assert.Throws<ArgumentException>(() => catalogo.Adicionar("12345", new byte[] { 1 }, 0.08f, 3, DateTime.Now, null));

        Directory.CreateDirectory(pasta);
        File.WriteAllText(Path.Combine(pasta, "marcadores.json"), "{ isto nao e json");
        LogAssert.Expect(LogType.Warning, new Regex("ilegivel"));
        Assert.AreEqual(0, new CatalogoMarcadores(pasta).Marcadores.Count);
    }

    [Test]
    public void CatalogoIgnoraEntradasSemArquivoOuComGtinEstragado()
    {
        Directory.CreateDirectory(pasta);
        File.WriteAllText(Path.Combine(pasta, "marcadores.json"),
            "[ null, { \"gtin\": \"78938571\", \"arquivo\": null }, { \"gtin\": \"123\", \"arquivo\": \"a.png\" }," +
            "  { \"gtin\": \"78938571\", \"arquivo\": \"78938571_20261013_101500_123.png\", \"larguraMetros\": 0.08 } ]");
        var catalogo = new CatalogoMarcadores(pasta);
        Assert.AreEqual(1, catalogo.Marcadores.Count);
        Assert.AreEqual("78938571_20261013_101500_123.png", catalogo.Marcadores[0].arquivo);
    }

    // ---------- Apoio ----------

    // Foto importada pelo menu da cena (Read/Write ligado, sem compressao), em cinza e de cima para baixo
    static byte[] CarregarFoto(string caminho, out int largura, out int altura)
    {
        var textura = AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
        Assert.IsNotNull(textura, caminho);
        largura = textura.width;
        altura = textura.height;
        Color32[] cores = textura.GetPixels32();
        var cinza = new byte[largura * altura];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
            {
                Color32 c = cores[(altura - 1 - y) * largura + x];
                cinza[y * largura + x] = (byte)((c.r * 299 + c.g * 587 + c.b * 114) / 1000);
            }
        return cinza;
    }

    // Na escala em que a nota e calculada: borrar ou estourar aqui equivale a fazer isso na foto inteira
    static byte[] FotoMaggiReduzida(out int largura, out int altura)
    {
        byte[] cinza = CarregarFoto(FotoMaggi, out int w, out int h);
        return ImagemCinza.Reduzir(cinza, w, h, QualidadeMarcador.LadoAnalise, out largura, out altura);
    }

    // Quadradinhos de 4 px com tons sorteados (sempre os mesmos): muita borda nitida
    static byte[] Blocos(int largura, int altura)
    {
        var sorteio = new System.Random(7);
        var tons = new byte[(largura / 4 + 1) * (altura / 4 + 1)];
        sorteio.NextBytes(tons);
        var imagem = new byte[largura * altura];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
                imagem[y * largura + x] = tons[(y / 4) * (largura / 4 + 1) + x / 4];
        return imagem;
    }

    // Borrao de caixa (media dos vizinhos ate raio pixels na linha e na coluna), passado duas vezes
    static byte[] Borrar(byte[] imagem, int largura, int altura, int raio)
    {
        byte[] r = imagem;
        for (int passada = 0; passada < 2; passada++)
        {
            r = BorrarNaDirecao(r, largura, altura, raio, 1, 0);
            r = BorrarNaDirecao(r, largura, altura, raio, 0, 1);
        }
        return r;
    }

    static byte[] BorrarNaDirecao(byte[] imagem, int largura, int altura, int raio, int dx, int dy)
    {
        var saida = new byte[imagem.Length];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
            {
                int soma = 0, n = 0;
                for (int k = -raio; k <= raio; k++)
                {
                    int xx = x + k * dx, yy = y + k * dy;
                    if (xx < 0 || yy < 0 || xx >= largura || yy >= altura) continue;
                    soma += imagem[yy * largura + xx];
                    n++;
                }
                saida[y * largura + x] = (byte)(soma / n);
            }
        return saida;
    }
}
