using UnityEngine;

// Contas do zoom, da pinca e do modo leitura.
// Ficam fora dos MonoBehaviours para poderem ser testadas no EditMode.
public static class CalculosGestos
{
    // Zoom da tabela em RA (controle deslizante e pinca)
    public const float ZoomMinimo = 0.5f;
    public const float ZoomMaximo = 3f;

    // Zoom da tabela no modo leitura (1 = tabela inteira na tela)
    public const float ZoomLeituraMinimo = 1f;
    public const float ZoomLeituraMaximo = 5f;

    // Cada clique da roda do mouse aumenta ou diminui 10%
    const float PassoRoda = 1.1f;

    // Toque mais longo que isso ja e arrastar, nao tocar
    const float DuracaoMaximaToque = 0.5f;

    public static float LimitarZoom(float zoom)
    {
        return Mathf.Clamp(zoom, ZoomMinimo, ZoomMaximo);
    }

    public static float LimitarZoomLeitura(float zoom)
    {
        return Mathf.Clamp(zoom, ZoomLeituraMinimo, ZoomLeituraMaximo);
    }

    // Quanto a distancia entre os dois dedos mudou desde o quadro anterior
    public static float FatorPinca(float distanciaAnterior, float distanciaAtual)
    {
        if (distanciaAnterior < 1f || distanciaAtual < 1f) return 1f;
        return distanciaAtual / distanciaAnterior;
    }

    // So o sentido da rolagem importa: a escala do valor muda conforme a plataforma
    public static float FatorRoda(float rolagem)
    {
        if (rolagem > 0f) return PassoRoda;
        if (rolagem < 0f) return 1f / PassoRoda;
        return 1f;
    }

    // Aplica o zoom so na largura e na altura do quad, mantendo a proporcao da tabela
    public static Vector3 EscalaTabela(Vector3 escalaBase, float zoom)
    {
        return new Vector3(escalaBase.x * zoom, escalaBase.y * zoom, escalaBase.z);
    }

    // Maior tamanho com a proporcao da imagem que cabe na area
    public static Vector2 TamanhoAjustado(Vector2 tamanhoImagem, Vector2 area)
    {
        if (tamanhoImagem.x <= 0f || tamanhoImagem.y <= 0f) return Vector2.zero;
        float escala = Mathf.Min(area.x / tamanhoImagem.x, area.y / tamanhoImagem.y);
        return tamanhoImagem * escala;
    }

    // Zoom em torno de um ponto: o que estava sob os dedos continua sob os dedos
    public static Vector2 ZoomEmTornoDoPonto(Vector2 posicao, Vector2 ponto, float fator)
    {
        return ponto + (posicao - ponto) * fator;
    }

    // Impede que a tabela saia da tela: so da para arrastar o que sobra alem do visor
    public static Vector2 LimitarDeslocamento(Vector2 posicao, Vector2 tamanhoConteudo, Vector2 tamanhoVisor)
    {
        float folgaX = Mathf.Max(0f, (tamanhoConteudo.x - tamanhoVisor.x) / 2f);
        float folgaY = Mathf.Max(0f, (tamanhoConteudo.y - tamanhoVisor.y) / 2f);
        return new Vector2(Mathf.Clamp(posicao.x, -folgaX, folgaX), Mathf.Clamp(posicao.y, -folgaY, folgaY));
    }

    // Um toque curto e quase parado conta como "tocar"
    public static bool EhToque(Vector2 inicio, Vector2 fim, float duracao, float limiarPixels)
    {
        return duracao <= DuracaoMaximaToque && (fim - inicio).sqrMagnitude <= limiarPixels * limiarPixels;
    }
}
