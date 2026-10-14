using UnityEngine;

// Nota de 0 a 5 estrelas para uma imagem candidata a marcador, no espirito da nota do Target Manager do
// Vuforia (que o Vuforia nao da em tempo de execucao): conta pontos de interesse (cantos FAST-9, o tipo de
// detalhe que o rastreamento procura), ve se eles cobrem a imagem toda e desconta reflexo, escuridao e
// recorte pequeno. Limites calibrados com as fotos dos rotulos do projeto e com a faixa da Fanta vista pela camera.
public static class QualidadeMarcador
{
    public struct Avaliacao
    {
        public int estrelas;
        public int pontos;
        // Pontos por mil pixels, na escala da analise
        public float densidade;
        // Fracao das celulas da grade 6x6 com pelo menos 2 pontos
        public float cobertura;
        // Fracao de pixels quase brancos (reflexo) e quase pretos
        public float estourado;
        public float escuro;
        // O que mais atrapalha, para mostrar na tela (null se nada)
        public string motivo;
    }

    // O maior lado e reduzido para isto antes da analise, para a nota nao depender do tamanho do recorte
    public const int LadoAnalise = 320;
    // Recorte menor que isto, em pixels da camera, tem pouco detalhe para o Vuforia
    public const int LadoMinimo = 200;

    const int LimiarFast = 20;
    const int Grade = 6;
    const float LimiteEstourado = 0.03f;
    const float LimiteEscuro = 0.25f;
    // Menos pontos que isto: a imagem esta borrada ou e lisa
    const int PontosMinimos = 30;

    // Circulo de raio 3 em volta do pixel, na ordem do teste FAST
    static readonly int[] CirculoX = { 0, 1, 2, 3, 3, 3, 2, 1, 0, -1, -2, -3, -3, -3, -2, -1 };
    static readonly int[] CirculoY = { -3, -3, -2, -1, 0, 1, 2, 3, 3, 3, 2, 1, 0, -1, -2, -3 };

    public static Avaliacao Avaliar(byte[] cinza, int largura, int altura)
    {
        byte[] img = ImagemCinza.Reduzir(cinza, largura, altura, LadoAnalise, out int w, out int h);
        var a = new Avaliacao();

        var porCelula = new int[Grade * Grade];
        a.pontos = ContarCantos(img, w, h, porCelula);
        a.densidade = a.pontos / (w * h / 1000f);
        int cobertas = 0;
        foreach (int n in porCelula) if (n >= 2) cobertas++;
        a.cobertura = cobertas / (float)porCelula.Length;

        int estourados = 0, escuros = 0;
        foreach (byte b in img)
        {
            if (b >= 250) estourados++;
            else if (b <= 10) escuros++;
        }
        a.estourado = estourados / (float)img.Length;
        a.escuro = escuros / (float)img.Length;

        a.estrelas = EstrelasPeloDetalhe(a.densidade, a.cobertura);
        if (a.estrelas < 3)
            a.motivo = a.pontos < PontosMinimos ? "Imagem borrada ou lisa: segure firme e de frente"
                                                : "Pouco detalhe: inclua o logotipo e os textos do rótulo";
        // Cada problema abaixo tira uma estrela; o ultimo encontrado e o que aparece na tela
        if (a.estourado > LimiteEstourado)
            Descontar(ref a, "Reflexo no rótulo: incline um pouco ou afaste a luz");
        if (a.escuro > LimiteEscuro)
            Descontar(ref a, "Pouca luz: use a lanterna ou um lugar mais claro");
        if (Mathf.Max(largura, altura) < LadoMinimo)
            Descontar(ref a, "Rótulo pequeno na imagem: aproxime a câmera");
        return a;
    }

    static void Descontar(ref Avaliacao a, string motivo)
    {
        a.estrelas = Mathf.Max(0, a.estrelas - 1);
        a.motivo = motivo;
    }

    static int EstrelasPeloDetalhe(float densidade, float cobertura)
    {
        if (densidade >= 12f && cobertura >= 0.85f) return 5;
        if (densidade >= 8f && cobertura >= 0.7f) return 4;
        if (densidade >= 5f && cobertura >= 0.5f) return 3;
        if (densidade >= 2.5f && cobertura >= 0.3f) return 2;
        if (densidade >= 1f) return 1;
        return 0;
    }

    // Cantos FAST-9: 9 pixels seguidos do circulo todos mais claros (ou todos mais escuros) que o centro.
    // So conta o canto mais forte de cada vizinhanca 3x3 e soma quantos caem em cada celula da grade.
    static int ContarCantos(byte[] img, int w, int h, int[] porCelula)
    {
        var deslocamentos = new int[16];
        for (int k = 0; k < 16; k++) deslocamentos[k] = CirculoY[k] * w + CirculoX[k];

        var forca = new int[w * h];
        for (int y = 3; y < h - 3; y++)
            for (int x = 3; x < w - 3; x++)
                forca[y * w + x] = ForcaFast(img, y * w + x, deslocamentos);

        int total = 0;
        for (int y = 3; y < h - 3; y++)
            for (int x = 3; x < w - 3; x++)
            {
                int i = y * w + x;
                if (forca[i] == 0 || !MaiorDaVizinhanca(forca, i, w)) continue;
                total++;
                porCelula[Mathf.Min(y * Grade / h, Grade - 1) * Grade + Mathf.Min(x * Grade / w, Grade - 1)]++;
            }
        return total;
    }

    // 0 se nao e canto; senao, o quanto o circulo se afasta do centro (para escolher o mais forte)
    static int ForcaFast(byte[] img, int i, int[] deslocamentos)
    {
        int claro = img[i] + LimiarFast, escuro = img[i] - LimiarFast;
        int seguidosClaros = 0, seguidosEscuros = 0, maxClaros = 0, maxEscuros = 0, soma = 0;
        // Da uma volta e meia no circulo para achar sequencias que passam do fim para o comeco
        for (int k = 0; k < 24; k++)
        {
            int v = img[i + deslocamentos[k & 15]];
            if (v > claro) { seguidosClaros++; if (k < 16) soma += v - claro; }
            else seguidosClaros = 0;
            if (v < escuro) { seguidosEscuros++; if (k < 16) soma += escuro - v; }
            else seguidosEscuros = 0;
            maxClaros = Mathf.Max(maxClaros, seguidosClaros);
            maxEscuros = Mathf.Max(maxEscuros, seguidosEscuros);
        }
        return maxClaros >= 9 || maxEscuros >= 9 ? soma : 0;
    }

    static bool MaiorDaVizinhanca(int[] forca, int i, int w)
    {
        int f = forca[i];
        for (int dy = -1; dy <= 1; dy++)
            for (int dx = -1; dx <= 1; dx++)
                if (forca[i + dy * w + dx] > f) return false;
        return true;
    }
}
