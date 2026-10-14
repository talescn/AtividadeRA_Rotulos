using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Cria na cena a interface do zoom e do modo leitura e liga tudo ao InterfaceRotulosRA.
// Chamado pelo menu "Atividade RA > Montar cena dos rotulos". Os textos sao TextMeshPro.
public static class MontarInterfaceRA
{
    // Resolucao de referencia: celular em retrato
    static readonly Vector2 Referencia = new Vector2(1080, 1920);
    static readonly Color CorBarra = new Color(0f, 0f, 0f, 0.55f);
    static readonly Color CorFundoLeitura = new Color(0.04f, 0.04f, 0.04f, 0.94f);
    static readonly Color CorTextoClaro = new Color(0.95f, 0.95f, 0.95f);
    static readonly Color CorTextoBotao = new Color(0.12f, 0.12f, 0.12f);

    public static InterfaceRotulosRA Montar(GerenciadorRotulosRA gerenciador)
    {
        var sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var recursos = new DefaultControls.Resources
        {
            standard = sprite,
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd")
        };
        var recursosTMP = new TMP_DefaultControls.Resources { standard = sprite };

        // EventSystem com o modulo do Input System novo (o projeto nao usa o Input Manager antigo)
        if (Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasGO = new GameObject("InterfaceRA", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var escalador = canvasGO.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = Referencia;
        escalador.matchWidthOrHeight = 0.5f;

        var ui = canvasGO.AddComponent<InterfaceRotulosRA>();
        ui.gerenciador = gerenciador;

        // ---------- Tela de RA: barra de zoom e botao "Ler" ----------
        var areaRA = CriarAreaSegura("AreaSeguraRA", canvasGO.transform);

        var barra = CriarImagem("BarraZoom", areaRA, CorBarra, recursos.standard);
        Ancorar(barra.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(40f, 40f), new Vector2(-40f, 180f));

        var rotuloZoom = CriarTexto("RotuloZoom", barra.transform, "Zoom", 44, CorTextoClaro, TextAlignmentOptions.Left);
        Ancorar(rotuloZoom.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(30f, 0f), new Vector2(180f, 0f));

        ui.textoZoom = CriarTexto("ValorZoom", barra.transform, "1,0x", 44, CorTextoClaro, TextAlignmentOptions.Right);
        Ancorar(ui.textoZoom.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-180f, 0f), new Vector2(-30f, 0f));

        var slider = DefaultControls.CreateSlider(recursos);
        slider.name = "ControleZoom";
        slider.transform.SetParent(barra.transform, false);
        Ancorar((RectTransform)slider.transform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(200f, -35f), new Vector2(-200f, 35f));
        // Pegador redondo e grande, facil de arrastar com o dedo
        var areaPegador = (RectTransform)slider.transform.Find("Handle Slide Area");
        areaPegador.offsetMin = new Vector2(35f, 0f);
        areaPegador.offsetMax = new Vector2(-35f, 0f);
        ((RectTransform)areaPegador.Find("Handle")).sizeDelta = new Vector2(70f, 0f);
        ui.controleZoom = slider.GetComponent<Slider>();
        ui.controleZoom.minValue = CalculosGestos.ZoomMinimo;
        ui.controleZoom.maxValue = CalculosGestos.ZoomMaximo;
        ui.controleZoom.value = 1f;

        ui.botaoLer = CriarBotao("BotaoLer", areaRA, recursosTMP, "Ler");
        var rtLer = (RectTransform)ui.botaoLer.transform;
        rtLer.anchorMin = rtLer.anchorMax = new Vector2(0.5f, 0f);
        rtLer.pivot = new Vector2(0.5f, 0f);
        rtLer.sizeDelta = new Vector2(360f, 130f);
        rtLer.anchoredPosition = new Vector2(0f, 210f);
        // So aparece quando ha um produto rastreado
        ui.botaoLer.gameObject.SetActive(false);

        // Faixa no topo: dica de para onde apontar e avisos da leitura do codigo de barras
        var faixa = CriarImagem("FaixaAviso", areaRA, CorBarra, recursos.standard);
        faixa.raycastTarget = false;
        Ancorar(faixa.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -160f), new Vector2(-40f, -40f));
        ui.faixaAviso = faixa.gameObject;
        ui.textoAviso = CriarTexto("TextoAviso", faixa.transform, "", 38, CorTextoClaro, TextAlignmentOptions.Center);
        Ancorar(ui.textoAviso.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 0f), new Vector2(-24f, 0f));
        ui.textoAviso.textWrappingMode = TextWrappingModes.Normal;
        faixa.gameObject.SetActive(false);

        // Le o codigo de barras na imagem da camera quando nenhum rotulo esta visivel
        ui.leitorCodigo = canvasGO.AddComponent<LeitorCodigoBarras>();

        // Foco da camera e lanterna; o botao so aparece depois que o Vuforia diz que ha lanterna
        ui.controleCamera = canvasGO.AddComponent<ControleCamera>();
        ui.botaoLanterna = CriarBotao("BotaoLanterna", areaRA, recursosTMP, "Lanterna");
        ui.botaoLanterna.GetComponentInChildren<TextMeshProUGUI>().fontSize = 44;
        var rtLanterna = (RectTransform)ui.botaoLanterna.transform;
        rtLanterna.anchorMin = rtLanterna.anchorMax = rtLanterna.pivot = new Vector2(1f, 1f);
        rtLanterna.sizeDelta = new Vector2(300f, 110f);
        rtLanterna.anchoredPosition = new Vector2(-40f, -190f);
        ui.botaoLanterna.gameObject.SetActive(false);

        // ---------- Modo leitura: tabela em 2D, em tela cheia ----------
        var painel = CriarImagem("PainelLeitura", canvasGO.transform, CorFundoLeitura, null);
        Ancorar(painel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        ui.painelLeitura = painel.gameObject;

        var areaLeitura = CriarAreaSegura("AreaSeguraLeitura", painel.transform);

        // O visor vem primeiro para que titulo e botoes fiquem por cima da tabela ampliada
        var visor = new GameObject("VisorLeitura", typeof(RectTransform)).GetComponent<RectTransform>();
        visor.SetParent(areaLeitura, false);
        // Embaixo ficam o painel da quantidade e os botoes Ensinar e Ouvir
        Ancorar(visor, Vector2.zero, Vector2.one, new Vector2(0f, 560f), new Vector2(0f, -170f));
        ui.visorLeitura = visor;

        // Foto da tabela: so aparece quando o produto nao tem dados
        var tabela = new GameObject("TabelaLeitura", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        tabela.transform.SetParent(visor, false);
        tabela.raycastTarget = false;
        tabela.rectTransform.sizeDelta = new Vector2(800f, 800f);
        ui.tabelaLeitura = tabela;

        ui.tituloLeitura = CriarTexto("TituloLeitura", areaLeitura, "", 46, CorTextoClaro, TextAlignmentOptions.Left);
        Ancorar(ui.tituloLeitura.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(40f, -150f), new Vector2(-320f, -30f));
        ui.tituloLeitura.textWrappingMode = TextWrappingModes.Normal;
        ui.tituloLeitura.overflowMode = TextOverflowModes.Ellipsis;

        ui.botaoFechar = CriarBotao("BotaoFechar", areaLeitura, recursosTMP, "Fechar");
        var rtFechar = (RectTransform)ui.botaoFechar.transform;
        rtFechar.anchorMin = rtFechar.anchorMax = rtFechar.pivot = new Vector2(1f, 1f);
        rtFechar.sizeDelta = new Vector2(260f, 120f);
        rtFechar.anchoredPosition = new Vector2(-30f, -30f);

        var dica = CriarTexto("DicaLeitura", areaLeitura, "Pinça ou roda do mouse: zoom   ·   Arraste: mover",
            34, new Color(0.75f, 0.75f, 0.75f), TextAlignmentOptions.Center);
        Ancorar(dica.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(40f, 30f), new Vector2(-40f, 120f));

        // Transforma o rotulo do produto aberto num marcador; so aparece para produto com dados e GTIN
        ui.botaoEnsinar = CriarBotao("BotaoEnsinar", areaLeitura, recursosTMP, "Ensinar rótulo");
        Ancorar((RectTransform)ui.botaoEnsinar.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-510f, 140f), new Vector2(-15f, 250f));
        ui.botaoEnsinar.gameObject.SetActive(false);

        // Quantidade, carboidratos e botao Ouvir; a voz fica no Canvas, que nunca desliga
        ui.painelConsumo = CriarPainelConsumo(areaLeitura, recursos.standard, recursosTMP, canvasGO.AddComponent<LeitorVoz>());

        painel.gameObject.SetActive(false);

        // ---------- Ensinar rotulo: telas de captura e de recorte ----------
        MontarEnsinoRA.Montar(ui, canvasGO, sprite, recursosTMP);

        EditorUtility.SetDirty(ui);
        return ui;
    }

    // Painel do modo leitura com a quantidade ([-] 200 ml [+]), os carboidratos nela e o botao Ouvir. A raiz cobre
    // a area segura inteira, sem imagem, para os filhos se ancorarem como os outros botoes do modo leitura.
    static PainelConsumoUI CriarPainelConsumo(Transform areaLeitura, Sprite sprite, TMP_DefaultControls.Resources recursosTMP,
        LeitorVoz voz)
    {
        var raiz = new GameObject("PainelConsumo", typeof(RectTransform)).GetComponent<RectTransform>();
        raiz.SetParent(areaLeitura, false);
        Ancorar(raiz, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var painel = raiz.gameObject.AddComponent<PainelConsumoUI>();
        painel.voz = voz;

        var barra = CriarImagem("BarraConsumo", raiz, CorBarra, sprite);
        Ancorar(barra.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(40f, 270f), new Vector2(-40f, 540f));

        painel.botaoMenos = CriarBotaoQuadrado("BotaoMenos", barra.transform, recursosTMP, "-", new Vector2(0f, 1f), new Vector2(20f, -15f));
        painel.botaoMais = CriarBotaoQuadrado("BotaoMais", barra.transform, recursosTMP, "+", new Vector2(1f, 1f), new Vector2(-20f, -15f));

        painel.textoQuantidade = CriarTexto("TextoQuantidade", barra.transform, "", 44, CorTextoClaro, TextAlignmentOptions.Center);
        Ancorar(painel.textoQuantidade.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(170f, -125f), new Vector2(-170f, -15f));

        painel.textoResultado = CriarTexto("TextoResultado", barra.transform, "", 36, CorTextoClaro, TextAlignmentOptions.Center);
        Ancorar(painel.textoResultado.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 60f), new Vector2(-24f, 140f));
        painel.textoResultado.textWrappingMode = TextWrappingModes.Normal;

        var aviso = CriarTexto("AvisoInsulina", barra.transform, "Só informa gramas: não calcula dose de insulina.",
            28, new Color(0.75f, 0.75f, 0.75f), TextAlignmentOptions.Center);
        Ancorar(aviso.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(24f, 12f), new Vector2(-24f, 58f));

        painel.botaoOuvir = CriarBotao("BotaoOuvir", raiz, recursosTMP, "Ouvir");
        Ancorar((RectTransform)painel.botaoOuvir.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(15f, 140f), new Vector2(510f, 250f));
        painel.textoOuvir = painel.botaoOuvir.GetComponentInChildren<TextMeshProUGUI>();

        // So aparece quando a leitura abre um produto com dados
        raiz.gameObject.SetActive(false);
        return painel;
    }

    static Button CriarBotaoQuadrado(string nome, Transform pai, TMP_DefaultControls.Resources recursos, string rotulo,
        Vector2 canto, Vector2 posicao)
    {
        var botao = CriarBotao(nome, pai, recursos, rotulo);
        botao.GetComponentInChildren<TextMeshProUGUI>().fontSize = 64;
        var rt = (RectTransform)botao.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = canto;
        rt.sizeDelta = new Vector2(130f, 110f);
        rt.anchoredPosition = posicao;
        return botao;
    }

    internal static RectTransform CriarAreaSegura(string nome, Transform pai)
    {
        var area = new GameObject(nome, typeof(RectTransform), typeof(AjustarAreaSegura)).GetComponent<RectTransform>();
        area.SetParent(pai, false);
        Ancorar(area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return area;
    }

    internal static Image CriarImagem(string nome, Transform pai, Color cor, Sprite sprite)
    {
        var imagem = new GameObject(nome, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        imagem.transform.SetParent(pai, false);
        imagem.sprite = sprite;
        imagem.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        imagem.color = cor;
        return imagem;
    }

    internal static TextMeshProUGUI CriarTexto(string nome, Transform pai, string conteudo, float tamanho, Color cor, TextAlignmentOptions alinhamento)
    {
        var texto = new GameObject(nome, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        texto.transform.SetParent(pai, false);
        texto.text = conteudo;
        texto.fontSize = tamanho;
        texto.color = cor;
        texto.alignment = alinhamento;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.raycastTarget = false;
        return texto;
    }

    internal static Button CriarBotao(string nome, Transform pai, TMP_DefaultControls.Resources recursos, string rotulo)
    {
        var go = TMP_DefaultControls.CreateButton(recursos);
        go.name = nome;
        go.transform.SetParent(pai, false);
        var texto = go.GetComponentInChildren<TextMeshProUGUI>();
        texto.text = rotulo;
        texto.fontSize = 52;
        texto.fontStyle = FontStyles.Bold;
        texto.color = CorTextoBotao;
        texto.raycastTarget = false;
        return go.GetComponent<Button>();
    }

    internal static void Ancorar(RectTransform rt, Vector2 ancoraMin, Vector2 ancoraMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = ancoraMin;
        rt.anchorMax = ancoraMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
