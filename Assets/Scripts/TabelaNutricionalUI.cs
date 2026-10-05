using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Desenha a tabela nutricional a partir dos dados, no modelo da RDC 429/2020:
// titulo, porcao, colunas "100 ml" / porcao / %VD e a data em que os dados foram conferidos.
// Serve para a RA (Canvas no espaco, sobre o rotulo) e para o modo leitura (Canvas da tela).
// O texto e TextMeshPro, entao fica nitido em qualquer zoom.
public static class TabelaNutricionalUI
{
    // A tabela tem sempre esta largura, em unidades do Canvas; a altura depende do numero de linhas
    public const float Largura = 1000f;
    const float Borda = 6f;
    const float Margem = 30f;
    const float AlturaLinha = 54f;
    const float RecuoPorNivel = 34f;
    const float TamanhoTexto = 36f;
    // Colunas: nome | por 100 | porcao | %VD, em fracoes da largura util
    static readonly float[] Colunas = { 0.49f, 0.18f, 0.18f, 0.15f };

    static readonly Color CorTexto = new Color(0.08f, 0.08f, 0.08f);
    static readonly Color CorSecundaria = new Color(0.35f, 0.35f, 0.35f);

    public static RectTransform Montar(Transform pai, DadosProduto dados)
    {
        // Retangulo preto por tras faz a borda; o fundo branco fica por cima, recuado
        var raiz = Retangulo("TabelaNutricional", pai, CorTexto);
        raiz.anchorMin = raiz.anchorMax = raiz.pivot = new Vector2(0.5f, 0.5f);
        var fundo = Retangulo("Fundo", raiz, Color.white);
        fundo.anchorMin = Vector2.zero;
        fundo.anchorMax = Vector2.one;
        fundo.offsetMin = new Vector2(Borda, Borda);
        fundo.offsetMax = new Vector2(-Borda, -Borda);

        float util = Largura - 2f * Margem;
        float y = Margem;

        y = Texto(raiz, "INFORMAÇÃO NUTRICIONAL", Margem, y, util, 60f, 54f, FontStyles.Bold, TextAlignmentOptions.Left);
        y = Traco(raiz, y, 2f);
        if (dados.porcoesPorEmbalagem.HasValue)
            y = Texto(raiz, "Porções por embalagem: " + dados.porcoesPorEmbalagem.Value, Margem, y, util, AlturaLinha, TamanhoTexto);
        y = Texto(raiz, LinhaPorcao(dados), Margem, y, util, AlturaLinha, TamanhoTexto);
        y = Traco(raiz, y, 12f);

        // Cabecalho: "**" marca quando a coluna "por 100" e do alimento pronto (como no Maggi)
        string cabecalho100 = (dados.base100 ?? "100 g") + (string.IsNullOrEmpty(dados.observacaoBase100) ? "" : "**");
        y = LinhaColunas(raiz, y, "", cabecalho100, RotuloPorcao(dados.porcao), "%VD*", FontStyles.Bold, 0);
        y = Traco(raiz, y, 2f);

        foreach (var n in dados.nutrientes)
        {
            string nome = n.nome + " (" + n.unidade + ")";
            y = LinhaColunas(raiz, y, nome, FormatarNumero(n.por100), FormatarNumero(n.porPorcao), FormatarNumero(n.vd),
                FontStyles.Normal, n.nivel);
            y = Traco(raiz, y, 1f);
        }
        y = Traco(raiz, y, 6f);

        y = Texto(raiz, "*Percentual de valores diários fornecidos pela porção.", Margem, y, util, 42f, 27f);
        if (!string.IsNullOrEmpty(dados.observacaoBase100))
            y = Texto(raiz, "**" + dados.observacaoBase100, Margem, y, util, 42f, 27f);
        y = Texto(raiz, LinhaConferido(dados), Margem, y + 8f, util, 44f, 28f, FontStyles.Italic, TextAlignmentOptions.Left, CorSecundaria);

        raiz.sizeDelta = new Vector2(Largura, y + Margem);
        return raiz;
    }

    // ---------- Textos (publicos para os testes) ----------

    // Como no rotulo: virgula decimal e sem zeros sobrando ("4,4", "120", "0"); valor ausente fica em branco
    public static string FormatarNumero(float? valor)
    {
        if (!valor.HasValue) return "";
        return Math.Round(valor.Value, 2).ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');
    }

    // "2026-10-05" -> "05/10/2026"
    public static string FormatarData(string dataIso)
    {
        if (DateTime.TryParseExact(dataIso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime data))
            return data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        return dataIso ?? "";
    }

    // "200 ml", "17 g"
    public static string RotuloPorcao(DadosProduto.Porcao porcao)
    {
        if (porcao == null || !porcao.quantidade.HasValue) return "Porção";
        return FormatarNumero(porcao.quantidade) + " " + porcao.unidade;
    }

    // "Porção: 200 ml (1 copo)"
    public static string LinhaPorcao(DadosProduto dados)
    {
        if (dados.porcao == null) return "Porção: não informada";
        string medida = string.IsNullOrEmpty(dados.porcao.descricao) ? "" : " (" + dados.porcao.descricao + ")";
        return "Porção: " + RotuloPorcao(dados.porcao) + medida;
    }

    // "Conferido em 05/10/2026 · fonte: rótulo"
    public static string LinhaConferido(DadosProduto dados)
    {
        string fonte = dados.fonte == DadosProduto.FonteOpenFoodFacts ? "Open Food Facts" : "rótulo";
        return "Conferido em " + FormatarData(dados.conferidoEm) + " · fonte: " + fonte;
    }

    // ---------- Montagem ----------

    static float LinhaColunas(RectTransform raiz, float y, string nome, string c1, string c2, string c3, FontStyles estilo, int nivel)
    {
        float util = Largura - 2f * Margem;
        float x = Margem;
        float recuo = RecuoPorNivel * nivel;
        Texto(raiz, nome, x + recuo, y, util * Colunas[0] - recuo, AlturaLinha, TamanhoTexto, estilo);
        x += util * Colunas[0];
        Texto(raiz, c1, x, y, util * Colunas[1], AlturaLinha, TamanhoTexto, estilo, TextAlignmentOptions.Right);
        x += util * Colunas[1];
        Texto(raiz, c2, x, y, util * Colunas[2], AlturaLinha, TamanhoTexto, estilo, TextAlignmentOptions.Right);
        x += util * Colunas[2];
        Texto(raiz, c3, x, y, util * Colunas[3], AlturaLinha, TamanhoTexto, estilo, TextAlignmentOptions.Right);
        return y + AlturaLinha;
    }

    // Devolve o y logo abaixo do texto (y cresce para baixo, a partir do topo da tabela)
    static float Texto(RectTransform raiz, string conteudo, float x, float y, float largura, float altura, float tamanho,
        FontStyles estilo = FontStyles.Normal, TextAlignmentOptions alinhamento = TextAlignmentOptions.Left, Color? cor = null)
    {
        var texto = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        Posicionar(texto.rectTransform, raiz, x, y, largura, altura);
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.fontStyle = estilo;
        // Left e Right do TMP ja centralizam na vertical
        texto.alignment = alinhamento;
        texto.color = cor ?? CorTexto;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.overflowMode = TextOverflowModes.Overflow;
        texto.raycastTarget = false;
        return y + altura;
    }

    static float Traco(RectTransform raiz, float y, float espessura)
    {
        var traco = Retangulo("Traco", raiz, CorTexto);
        Posicionar(traco, raiz, Margem, y, Largura - 2f * Margem, espessura);
        return y + espessura;
    }

    static RectTransform Retangulo(string nome, Transform pai, Color cor)
    {
        var imagem = new GameObject(nome, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        imagem.transform.SetParent(pai, false);
        imagem.color = cor;
        imagem.raycastTarget = false;
        return imagem.rectTransform;
    }

    // Ancora no canto superior esquerdo da tabela
    static void Posicionar(RectTransform rt, RectTransform raiz, float x, float y, float largura, float altura)
    {
        rt.SetParent(raiz, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, -y);
        rt.sizeDelta = new Vector2(largura, altura);
    }
}
