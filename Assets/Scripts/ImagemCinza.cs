using UnityEngine;

// Operacoes numa imagem em tons de cinza guardada como byte[] (um byte por pixel, linha apos linha, sem
// preenchimento). Usadas para transformar o quadro da camera no marcador de um rotulo.
public static class ImagemCinza
{
    // Gira 90 graus no sentido horario: a imagem da camera chega deitada e passa a ficar como a tela em
    // retrato. A largura da nova imagem e a altura da original.
    public static byte[] GirarHorario(byte[] pixels, int largura, int altura)
    {
        var girada = new byte[largura * altura];
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
                girada[x * altura + (altura - 1 - y)] = pixels[y * largura + x];
        return girada;
    }

    // Copia so a regiao pedida (ja limitada a imagem)
    public static byte[] Recortar(byte[] pixels, int largura, RectInt regiao)
    {
        var recorte = new byte[regiao.width * regiao.height];
        for (int y = 0; y < regiao.height; y++)
            System.Buffer.BlockCopy(pixels, (regiao.y + y) * largura + regiao.x, recorte, y * regiao.width, regiao.width);
        return recorte;
    }

    // Reduz ate o maior lado ficar com maiorLado pixels (nao amplia). Cada pixel novo e a media do bloco
    // de pixels originais que ele cobre, para nao perder nem inventar detalhe.
    public static byte[] Reduzir(byte[] pixels, int largura, int altura, int maiorLado, out int novaLargura, out int novaAltura)
    {
        float escala = Mathf.Min(1f, maiorLado / (float)Mathf.Max(largura, altura));
        novaLargura = Mathf.Max(1, Mathf.RoundToInt(largura * escala));
        novaAltura = Mathf.Max(1, Mathf.RoundToInt(altura * escala));
        if (novaLargura == largura && novaAltura == altura) return pixels;

        var reduzida = new byte[novaLargura * novaAltura];
        float passoX = largura / (float)novaLargura, passoY = altura / (float)novaAltura;
        for (int y = 0; y < novaAltura; y++)
        {
            int y0 = (int)(y * passoY), y1 = Mathf.Max(y0 + 1, (int)((y + 1) * passoY));
            for (int x = 0; x < novaLargura; x++)
            {
                int x0 = (int)(x * passoX), x1 = Mathf.Max(x0 + 1, (int)((x + 1) * passoX));
                int soma = 0;
                for (int yy = y0; yy < y1; yy++)
                    for (int xx = x0; xx < x1; xx++) soma += pixels[yy * largura + xx];
                reduzida[y * novaLargura + x] = (byte)(soma / ((y1 - y0) * (x1 - x0)));
            }
        }
        return reduzida;
    }

    // Variancia do laplaciano na metade central da imagem, de passo em passo pixels: quanto maior,
    // mais nitida. Serve para escolher o quadro menos borrado. stride: bytes entre o inicio de duas linhas.
    public static float Nitidez(byte[] pixels, int largura, int altura, int stride, int passo = 2)
    {
        double soma = 0, somaQuadrados = 0;
        int n = 0;
        for (int y = Mathf.Max(1, altura / 4); y < Mathf.Min(altura - 1, altura * 3 / 4); y += passo)
            for (int x = Mathf.Max(1, largura / 4); x < Mathf.Min(largura - 1, largura * 3 / 4); x += passo)
            {
                int i = y * stride + x;
                int lap = 4 * pixels[i] - pixels[i - 1] - pixels[i + 1] - pixels[i - stride] - pixels[i + stride];
                soma += lap;
                somaQuadrados += lap * lap;
                n++;
            }
        if (n == 0) return 0f;
        double media = soma / n;
        return (float)(somaQuadrados / n - media * media);
    }

    // Converte para uma textura RGBA32 legivel (o Vuforia cria o marcador a partir dela)
    public static Texture2D ParaTextura(byte[] pixels, int largura, int altura)
    {
        var cores = new Color32[largura * altura];
        // A textura do Unity comeca pela linha de baixo
        for (int y = 0; y < altura; y++)
            for (int x = 0; x < largura; x++)
            {
                byte c = pixels[(altura - 1 - y) * largura + x];
                cores[y * largura + x] = new Color32(c, c, c, 255);
            }
        var textura = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
        textura.SetPixels32(cores);
        textura.Apply();
        return textura;
    }
}
