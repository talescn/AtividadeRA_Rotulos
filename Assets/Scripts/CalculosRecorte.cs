using UnityEngine;

// Contas do retangulo que escolhe o pedaco da foto que vira marcador. O retangulo fica em coordenadas
// da foto de 0 a 1, com a origem embaixo a esquerda, como as ancoras da UI do Unity.
public static class CalculosRecorte
{
    // Menor lado do retangulo, em fracao da foto
    public const float LadoMinimo = 0.08f;

    // Comeco: uma faixa larga no meio da foto, o formato mais comum de rotulo de garrafa
    public static readonly Rect Inicial = new Rect(0.12f, 0.36f, 0.76f, 0.28f);

    // canto: 0 = inferior esquerdo, 1 = superior esquerdo, 2 = superior direito, 3 = inferior direito
    public static Rect MoverCanto(Rect r, int canto, Vector2 delta)
    {
        float xMin = r.xMin, xMax = r.xMax, yMin = r.yMin, yMax = r.yMax;
        if (canto == 0 || canto == 1) xMin = Mathf.Clamp(xMin + delta.x, 0f, xMax - LadoMinimo);
        else xMax = Mathf.Clamp(xMax + delta.x, xMin + LadoMinimo, 1f);
        if (canto == 0 || canto == 3) yMin = Mathf.Clamp(yMin + delta.y, 0f, yMax - LadoMinimo);
        else yMax = Mathf.Clamp(yMax + delta.y, yMin + LadoMinimo, 1f);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    // Arrasta o retangulo inteiro sem deixar sair da foto
    public static Rect Mover(Rect r, Vector2 delta)
    {
        r.x = Mathf.Clamp(r.x + delta.x, 0f, 1f - r.width);
        r.y = Mathf.Clamp(r.y + delta.y, 0f, 1f - r.height);
        return r;
    }

    // Em pixels da foto, com a origem no canto superior esquerdo (como no byte[] da imagem)
    public static RectInt ParaPixels(Rect r, int largura, int altura)
    {
        int x0 = Mathf.Clamp(Mathf.RoundToInt(r.xMin * largura), 0, largura - 1);
        int x1 = Mathf.Clamp(Mathf.RoundToInt(r.xMax * largura), x0 + 1, largura);
        int topo = Mathf.Clamp(Mathf.RoundToInt((1f - r.yMax) * altura), 0, altura - 1);
        int baixo = Mathf.Clamp(Mathf.RoundToInt((1f - r.yMin) * altura), topo + 1, altura);
        return new RectInt(x0, topo, x1 - x0, baixo - topo);
    }
}
