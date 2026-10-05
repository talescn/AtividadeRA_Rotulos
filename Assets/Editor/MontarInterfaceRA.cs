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

        // ---------- Modo leitura: tabela em 2D, em tela cheia ----------
        var painel = CriarImagem("PainelLeitura", canvasGO.transform, CorFundoLeitura, null);
        Ancorar(painel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        ui.painelLeitura = painel.gameObject;

        var areaLeitura = CriarAreaSegura("AreaSeguraLeitura", painel.transform);

        // O visor vem primeiro para que titulo e botoes fiquem por cima da tabela ampliada
        var visor = new GameObject("VisorLeitura", typeof(RectTransform)).GetComponent<RectTransform>();
        visor.SetParent(areaLeitura, false);
        Ancorar(visor, Vector2.zero, Vector2.one, new Vector2(0f, 140f), new Vector2(0f, -170f));
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

        painel.gameObject.SetActive(false);
        EditorUtility.SetDirty(ui);
        return ui;
    }

    static RectTransform CriarAreaSegura(string nome, Transform pai)
    {
        var area = new GameObject(nome, typeof(RectTransform), typeof(AjustarAreaSegura)).GetComponent<RectTransform>();
        area.SetParent(pai, false);
        Ancorar(area, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return area;
    }

    static Image CriarImagem(string nome, Transform pai, Color cor, Sprite sprite)
    {
        var imagem = new GameObject(nome, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        imagem.transform.SetParent(pai, false);
        imagem.sprite = sprite;
        imagem.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        imagem.color = cor;
        return imagem;
    }

    static TextMeshProUGUI CriarTexto(string nome, Transform pai, string conteudo, float tamanho, Color cor, TextAlignmentOptions alinhamento)
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

    static Button CriarBotao(string nome, Transform pai, TMP_DefaultControls.Resources recursos, string rotulo)
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

    static void Ancorar(RectTransform rt, Vector2 ancoraMin, Vector2 ancoraMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rt.anchorMin = ancoraMin;
        rt.anchorMax = ancoraMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
    }
}
