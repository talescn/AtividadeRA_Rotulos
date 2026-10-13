using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Cria as telas do "Ensinar rotulo" (captura e recorte) e liga tudo ao EnsinarRotuloUI.
// Chamado pelo MontarInterfaceRA depois de montar a tela de RA e o modo leitura.
public static class MontarEnsinoRA
{
    static readonly Color CorFaixa = new Color(0f, 0f, 0f, 0.6f);
    static readonly Color CorFundoRecorte = new Color(0.04f, 0.04f, 0.04f, 0.97f);
    static readonly Color CorMira = new Color(1f, 1f, 1f, 0.8f);
    static readonly Color CorMoldura = new Color(1f, 0.85f, 0.2f);
    // O meio do recorte fica levemente claro, para ver o que vai virar marcador
    static readonly Color CorMeioRecorte = new Color(1f, 1f, 1f, 0.08f);
    static readonly Color CorTextoClaro = new Color(0.95f, 0.95f, 0.95f);
    const float EspessuraMoldura = 6f;
    // Alca grande o bastante para o dedo
    const float LadoAlca = 90f;

    public static EnsinarRotuloUI Montar(InterfaceRotulosRA ui, GameObject canvasGO, Sprite sprite,
                                         TMP_DefaultControls.Resources recursosTMP)
    {
        var ensinar = canvasGO.AddComponent<EnsinarRotuloUI>();
        ensinar.gerenciador = ui.gerenciador;
        ensinar.captura = canvasGO.AddComponent<CapturaRotulo>();
        MontarCaptura(ensinar, ui.controleCamera, canvasGO.transform, sprite, recursosTMP);
        MontarRecorte(ensinar, canvasGO.transform, sprite, recursosTMP);
        ui.ensinar = ensinar;
        EditorUtility.SetDirty(ensinar);
        return ensinar;
    }

    // Fica por baixo da tela de RA, para a lanterna continuar clicavel. O fundo transparente foca a camera
    // no ponto tocado; a mira so mostra onde por o rotulo (o recorte de verdade e feito depois, na foto).
    static void MontarCaptura(EnsinarRotuloUI ensinar, ControleCamera controleCamera, Transform canvas, Sprite sprite,
                              TMP_DefaultControls.Resources recursosTMP)
    {
        var painel = MontarInterfaceRA.CriarImagem("PainelCaptura", canvas, Color.clear, null);
        MontarInterfaceRA.Ancorar(painel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        painel.transform.SetSiblingIndex(0);
        painel.gameObject.AddComponent<ToqueParaFocar>().controleCamera = controleCamera;
        ensinar.painelCaptura = painel.gameObject;

        var area = MontarInterfaceRA.CriarAreaSegura("AreaSeguraCaptura", painel.transform);

        var mira = new GameObject("Mira", typeof(RectTransform)).GetComponent<RectTransform>();
        mira.SetParent(area, false);
        MontarInterfaceRA.Ancorar(mira, new Vector2(0.08f, 0.4f), new Vector2(0.92f, 0.62f), Vector2.zero, Vector2.zero);
        CriarMoldura(mira, CorMira);

        var faixa = MontarInterfaceRA.CriarImagem("FaixaCaptura", area, CorFaixa, sprite);
        faixa.raycastTarget = false;
        MontarInterfaceRA.Ancorar(faixa.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(40f, 200f), new Vector2(-40f, 420f));
        ensinar.textoCaptura = CriarTextoLongo("TextoCaptura", faixa.transform);

        ensinar.botaoConcluir = CriarBotaoMetade("BotaoConcluirEnsino", area, recursosTMP, "Cancelar", false);
        ensinar.botaoCapturar = CriarBotaoMetade("BotaoCapturar", area, recursosTMP, "Capturar", true);

        painel.gameObject.SetActive(false);
    }

    // Por cima da tela de RA: a foto congelada, o retangulo com quatro alcas, a nota e os botoes
    static void MontarRecorte(EnsinarRotuloUI ensinar, Transform canvas, Sprite sprite,
                              TMP_DefaultControls.Resources recursosTMP)
    {
        var painel = MontarInterfaceRA.CriarImagem("PainelRecorte", canvas, CorFundoRecorte, null);
        MontarInterfaceRA.Ancorar(painel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        ensinar.painelRecorte = painel.gameObject;

        var area = MontarInterfaceRA.CriarAreaSegura("AreaSeguraRecorte", painel.transform);

        var titulo = MontarInterfaceRA.CriarTexto("TituloRecorte", area, "Ajuste o retângulo só no rótulo", 46,
            CorTextoClaro, TextAlignmentOptions.Center);
        MontarInterfaceRA.Ancorar(titulo.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(40f, -150f), new Vector2(-40f, -40f));

        var visor = new GameObject("VisorRecorte", typeof(RectTransform)).GetComponent<RectTransform>();
        visor.SetParent(area, false);
        MontarInterfaceRA.Ancorar(visor, Vector2.zero, Vector2.one, new Vector2(40f, 440f), new Vector2(-40f, -170f));
        ensinar.visorRecorte = visor;

        // O tamanho da foto e definido quando ela chega, para caber inteira no visor
        var foto = new GameObject("FotoRecorte", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        foto.transform.SetParent(visor, false);
        foto.raycastTarget = false;
        foto.rectTransform.sizeDelta = new Vector2(600f, 1000f);
        ensinar.fotoRecorte = foto;

        // As ancoras da moldura sao o recorte; arrastar o meio move o retangulo inteiro
        var moldura = MontarInterfaceRA.CriarImagem("MolduraRecorte", foto.transform, CorMeioRecorte, null);
        Rect inicial = CalculosRecorte.Inicial;
        MontarInterfaceRA.Ancorar(moldura.rectTransform, inicial.min, inicial.max, Vector2.zero, Vector2.zero);
        AdicionarAlca(moldura.gameObject, ensinar, -1);
        CriarMoldura(moldura.rectTransform, CorMoldura);
        ensinar.molduraRecorte = moldura.rectTransform;

        // Na ordem dos cantos do CalculosRecorte: inferior esquerdo, superior esquerdo, superior direito, inferior direito.
        // As alcas ficam por fora dos cantos, para o meio de um retangulo pequeno continuar livre para arrastar.
        Vector2[] cantos = { Vector2.zero, new Vector2(0f, 1f), Vector2.one, new Vector2(1f, 0f) };
        for (int i = 0; i < cantos.Length; i++)
        {
            var alca = MontarInterfaceRA.CriarImagem("Alca" + i, moldura.transform, CorMoldura, sprite);
            alca.rectTransform.anchorMin = alca.rectTransform.anchorMax = cantos[i];
            alca.rectTransform.pivot = Vector2.one - cantos[i];
            alca.rectTransform.sizeDelta = new Vector2(LadoAlca, LadoAlca);
            alca.rectTransform.anchoredPosition = Vector2.zero;
            AdicionarAlca(alca.gameObject, ensinar, i);
        }

        var faixaNota = MontarInterfaceRA.CriarImagem("FaixaNota", area, CorFaixa, sprite);
        faixaNota.raycastTarget = false;
        MontarInterfaceRA.Ancorar(faixaNota.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(40f, 200f), new Vector2(-40f, 420f));
        ensinar.textoNota = CriarTextoLongo("TextoNota", faixaNota.transform);

        ensinar.botaoRefazer = CriarBotaoMetade("BotaoRefazer", area, recursosTMP, "Refazer", false);
        ensinar.botaoCriar = CriarBotaoMetade("BotaoCriarMarcador", area, recursosTMP, "Criar marcador", true);

        painel.gameObject.SetActive(false);
    }

    static void AdicionarAlca(GameObject alvo, EnsinarRotuloUI ensinar, int canto)
    {
        var alca = alvo.AddComponent<AlcaRecorte>();
        alca.ensinar = ensinar;
        alca.canto = canto;
    }

    // Quatro barras finas por dentro do retangulo, que nao recebem toque
    static void CriarMoldura(RectTransform alvo, Color cor)
    {
        CriarBarra(alvo, "BordaBaixo", Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, EspessuraMoldura), cor);
        CriarBarra(alvo, "BordaCima", new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -EspessuraMoldura), Vector2.zero, cor);
        CriarBarra(alvo, "BordaEsquerda", Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(EspessuraMoldura, 0f), cor);
        CriarBarra(alvo, "BordaDireita", new Vector2(1f, 0f), Vector2.one, new Vector2(-EspessuraMoldura, 0f), Vector2.zero, cor);
    }

    static void CriarBarra(RectTransform pai, string nome, Vector2 ancoraMin, Vector2 ancoraMax, Vector2 offsetMin,
                           Vector2 offsetMax, Color cor)
    {
        var barra = MontarInterfaceRA.CriarImagem(nome, pai, cor, null);
        barra.raycastTarget = false;
        MontarInterfaceRA.Ancorar(barra.rectTransform, ancoraMin, ancoraMax, offsetMin, offsetMax);
    }

    // Texto de varias linhas que ocupa a faixa inteira
    static TextMeshProUGUI CriarTextoLongo(string nome, Transform faixa)
    {
        var texto = MontarInterfaceRA.CriarTexto(nome, faixa, "", 38, CorTextoClaro, TextAlignmentOptions.Center);
        MontarInterfaceRA.Ancorar(texto.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 0f), new Vector2(-24f, 0f));
        texto.textWrappingMode = TextWrappingModes.Normal;
        return texto;
    }

    // Botao na metade esquerda ou direita da parte de baixo da tela
    static Button CriarBotaoMetade(string nome, RectTransform area, TMP_DefaultControls.Resources recursosTMP,
                                   string rotulo, bool direita)
    {
        var botao = MontarInterfaceRA.CriarBotao(nome, area, recursosTMP, rotulo);
        botao.GetComponentInChildren<TextMeshProUGUI>().fontSize = 46;
        if (direita)
            MontarInterfaceRA.Ancorar((RectTransform)botao.transform, new Vector2(0.5f, 0f), new Vector2(1f, 0f),
                new Vector2(15f, 40f), new Vector2(-40f, 170f));
        else
            MontarInterfaceRA.Ancorar((RectTransform)botao.transform, Vector2.zero, new Vector2(0.5f, 0f),
                new Vector2(40f, 40f), new Vector2(-15f, 170f));
        return botao;
    }
}
