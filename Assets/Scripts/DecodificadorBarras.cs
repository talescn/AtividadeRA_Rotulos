using System.Collections.Generic;
using ZXing;
using ZXing.Common;

// Le o codigo de barras de produtos (EAN-13, EAN-8 e UPC-A) numa imagem em tons de cinza ou RGB.
// Nao usa nada do Unity: roda fora da thread principal e nos testes de EditMode.
public static class DecodificadorBarras
{
    static readonly List<BarcodeFormat> Formatos = new List<BarcodeFormat>
    {
        BarcodeFormat.EAN_13, BarcodeFormat.EAN_8, BarcodeFormat.UPC_A
    };

    // O leitor do ZXing nao pode ser usado por duas threads ao mesmo tempo: um por thread
    [System.ThreadStatic] static BarcodeReaderGeneric leitor;

    // Sem AutoRotate: ele tentaria 4 rotacoes em cada quadro sem codigo, e para codigo de barras so
    // importam duas (deitado e em pe). Com TryHarder, o leitor 1D ja tenta a outra orientacao sozinho.
    static BarcodeReaderGeneric Leitor => leitor ??= new BarcodeReaderGeneric
    {
        AutoRotate = false,
        Options = new DecodingOptions { PossibleFormats = Formatos, TryHarder = true }
    };

    // bytesPorPixel: 1 (cinza) ou 3 (RGB). Devolve o GTIN lido (com digito verificador conferido) ou null.
    public static string Decodificar(byte[] pixels, int largura, int altura, int stride, int bytesPorPixel)
    {
        if (pixels == null || largura <= 0 || altura <= 0) return null;

        byte[] compactos = Compactar(pixels, largura, altura, stride, bytesPorPixel);
        var formato = bytesPorPixel == 1 ? RGBLuminanceSource.BitmapFormat.Gray8 : RGBLuminanceSource.BitmapFormat.RGB24;
        var fonte = new RGBLuminanceSource(compactos, largura, altura, formato);
        // Com o celular em retrato, o codigo na horizontal da tela chega deitado na imagem da camera:
        // a orientacao girada vem primeiro. Se falhar, o proprio leitor gira mais 90 graus (180 da girada,
        // o que para codigo de barras vale o mesmo que a imagem original, porque as linhas sao lidas nos dois sentidos)
        var resultado = Leitor.Decode(fonte.rotateCounterClockwise());
        if (resultado == null) return null;
        return DadosProduto.GtinValido(resultado.Text) ? resultado.Text : null;
    }

    // Tira o preenchimento do fim de cada linha (stride maior que largura x bytes por pixel)
    static byte[] Compactar(byte[] pixels, int largura, int altura, int stride, int bytesPorPixel)
    {
        int linha = largura * bytesPorPixel;
        if (stride <= linha) return pixels;
        var saida = new byte[linha * altura];
        for (int y = 0; y < altura; y++)
            System.Buffer.BlockCopy(pixels, y * stride, saida, y * linha, linha);
        return saida;
    }
}
