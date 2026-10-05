using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;
using Toque = UnityEngine.InputSystem.EnhancedTouch.Touch;
using FaseToque = UnityEngine.InputSystem.TouchPhase;

// Interface da tabela em RA: zoom (controle deslizante, pinca e roda do mouse),
// botao "Ler" e modo leitura (tabela em 2D, em tela cheia).
// No Editor, o clique e o arrastar do mouse fazem o papel do toque.
public class InterfaceRotulosRA : MonoBehaviour
{
    public GerenciadorRotulosRA gerenciador;

    [Header("Tela de RA")]
    public Slider controleZoom;
    public TMP_Text textoZoom;
    public Button botaoLer;

    [Header("Modo leitura")]
    public GameObject painelLeitura;
    [Tooltip("Area em que a tabela pode ser movida; o centro dela e o centro da tabela com zoom 1.")]
    public RectTransform visorLeitura;
    [Tooltip("Foto da tabela, usada so quando o produto nao tem dados.")]
    public RawImage tabelaLeitura;
    public TMP_Text tituloLeitura;
    public Button botaoFechar;

    [Tooltip("Espaco livre em volta da tabela no modo leitura, em unidades do Canvas.")]
    public float margemLeitura = 40f;

    readonly List<RaycastResult> resultadosUI = new List<RaycastResult>();
    // Se cada dedo comecou em cima de um controle (nesse caso o gesto e da UI, nao da tabela)
    readonly Dictionary<int, bool> toqueComecouNaUI = new Dictionary<int, bool>();

    float distanciaPinca = -1f;
    Vector2 meioPinca;
    bool houveMultiToque;
    int dedoArrastando = -1;
    Vector2 posicaoArrasto;

    Vector2 inicioClique;
    float tempoClique;
    bool cliqueNaUI;

    // O que esta no modo leitura: a tabela desenhada dos dados ou a foto. Os gestos mexem nele.
    RectTransform conteudoLeitura;
    // Tabela desenhada dos dados (destruida ao fechar)
    RectTransform tabelaDadosLeitura;
    // Escala que faz o conteudo caber inteiro no visor; o zoom da leitura multiplica esta escala
    float escalaAjuste = 1f;
    float escalaLeitura = 1f;

    bool EmLeitura => painelLeitura.activeSelf;

    void Awake()
    {
        controleZoom.minValue = CalculosGestos.ZoomMinimo;
        controleZoom.maxValue = CalculosGestos.ZoomMaximo;
        controleZoom.onValueChanged.AddListener(AoMudarControleZoom);
        botaoLer.onClick.AddListener(AbrirLeituraDoProdutoEmFoco);
        botaoFechar.onClick.AddListener(FecharLeitura);
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        gerenciador.AoMudarRastreamento += AtualizarBotaoLer;
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        gerenciador.AoMudarRastreamento -= AtualizarBotaoLer;
    }

    void Start()
    {
        painelLeitura.SetActive(false);
        controleZoom.SetValueWithoutNotify(gerenciador.Zoom);
        AtualizarTextoZoom();
        AtualizarBotaoLer();
    }

    void Update()
    {
        var toques = Toque.activeTouches;
        foreach (var t in toques)
            if (t.phase == FaseToque.Began)
                toqueComecouNaUI[t.touchId] = SobreUI(t.screenPosition);

        if (EmLeitura)
            GestosLeitura(toques);
        else
            GestosRA(toques);

        // Esquece os dedos que sairam da tela neste quadro
        foreach (var t in toques)
            if (t.phase == FaseToque.Ended || t.phase == FaseToque.Canceled)
                toqueComecouNaUI.Remove(t.touchId);
        if (toques.Count == 0) houveMultiToque = false;
    }

    // ---------- Tela de RA ----------

    void GestosRA(ReadOnlyArray<Toque> toques)
    {
        if (toques.Count >= 2)
        {
            houveMultiToque = true;
            // A pinca so vale se os dois dedos comecaram fora dos controles
            if (!ComecouNaUI(toques[0]) && !ComecouNaUI(toques[1]))
            {
                float distancia = Vector2.Distance(toques[0].screenPosition, toques[1].screenPosition);
                if (distanciaPinca > 0f)
                    AplicarZoom(gerenciador.Zoom * CalculosGestos.FatorPinca(distanciaPinca, distancia));
                distanciaPinca = distancia;
            }
        }
        else
        {
            distanciaPinca = -1f;
        }

        // Tocar na tabela abre o modo leitura
        if (toques.Count == 1 && toques[0].phase == FaseToque.Ended && !houveMultiToque && !ComecouNaUI(toques[0]))
        {
            var t = toques[0];
            if (CalculosGestos.EhToque(t.startScreenPosition, t.screenPosition, (float)(t.time - t.startTime), LimiarToque()))
                TocarEm(t.screenPosition);
        }

        var mouse = Mouse.current;
        if (mouse == null) return;

        float rolagem = mouse.scroll.ReadValue().y;
        if (rolagem != 0f && !SobreUI(mouse.position.ReadValue()))
            AplicarZoom(gerenciador.Zoom * CalculosGestos.FatorRoda(rolagem));

        if (mouse.leftButton.wasPressedThisFrame)
        {
            inicioClique = mouse.position.ReadValue();
            tempoClique = Time.unscaledTime;
            cliqueNaUI = SobreUI(inicioClique);
        }
        if (mouse.leftButton.wasReleasedThisFrame && !cliqueNaUI)
        {
            Vector2 fim = mouse.position.ReadValue();
            if (CalculosGestos.EhToque(inicioClique, fim, Time.unscaledTime - tempoClique, LimiarToque()))
                TocarEm(fim);
        }
    }

    void TocarEm(Vector2 posicaoTela)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        if (!Physics.Raycast(cam.ScreenPointToRay(posicaoTela), out RaycastHit acerto)) return;

        var produto = gerenciador.ProdutoDaTabela(acerto.transform);
        if (produto != null) AbrirLeitura(produto);
    }

    void AoMudarControleZoom(float valor)
    {
        gerenciador.DefinirZoom(valor);
        AtualizarTextoZoom();
    }

    void AplicarZoom(float zoom)
    {
        gerenciador.DefinirZoom(zoom);
        controleZoom.SetValueWithoutNotify(gerenciador.Zoom);
        AtualizarTextoZoom();
    }

    void AtualizarTextoZoom()
    {
        textoZoom.text = gerenciador.Zoom.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',') + "x";
    }

    void AtualizarBotaoLer()
    {
        botaoLer.gameObject.SetActive(!EmLeitura && gerenciador.ProdutoEmFoco != null);
    }

    // ---------- Modo leitura ----------

    void AbrirLeituraDoProdutoEmFoco()
    {
        AbrirLeitura(gerenciador.ProdutoEmFoco);
    }

    public void AbrirLeitura(GerenciadorRotulosRA.Produto produto)
    {
        if (produto == null) return;
        DadosProduto dados = gerenciador.DadosDe(produto);
        if (dados == null && produto.tabelaNutricional == null) return;

        LimparTabelaDados();
        if (dados != null)
        {
            // Mesma tabela da RA, desenhada a partir dos dados; a foto fica escondida
            tabelaLeitura.gameObject.SetActive(false);
            tabelaDadosLeitura = TabelaNutricionalUI.Montar(visorLeitura, dados);
            conteudoLeitura = tabelaDadosLeitura;
            tituloLeitura.text = dados.nome;
        }
        else
        {
            // Sem dados: mostra a foto, no tamanho da imagem (o ajuste a tela vem da escala)
            tabelaLeitura.gameObject.SetActive(true);
            tabelaLeitura.texture = produto.tabelaNutricional;
            tabelaLeitura.rectTransform.sizeDelta = new Vector2(produto.tabelaNutricional.width, produto.tabelaNutricional.height);
            conteudoLeitura = tabelaLeitura.rectTransform;
            tituloLeitura.text = produto.nome;
        }

        painelLeitura.SetActive(true);
        AtualizarBotaoLer();

        // Comeca com a tabela inteira na tela, centralizada
        var area = visorLeitura.rect.size - Vector2.one * (2f * margemLeitura);
        Vector2 tamanho = conteudoLeitura.sizeDelta;
        escalaAjuste = CalculosGestos.TamanhoAjustado(tamanho, area).x / tamanho.x;
        escalaLeitura = 1f;
        conteudoLeitura.anchoredPosition = Vector2.zero;
        conteudoLeitura.localScale = Vector3.one * escalaAjuste;
        distanciaPinca = -1f;
        dedoArrastando = -1;
    }

    public void FecharLeitura()
    {
        painelLeitura.SetActive(false);
        LimparTabelaDados();
        distanciaPinca = -1f;
        dedoArrastando = -1;
        AtualizarBotaoLer();
    }

    void LimparTabelaDados()
    {
        if (tabelaDadosLeitura != null) Destroy(tabelaDadosLeitura.gameObject);
        tabelaDadosLeitura = null;
    }

    void GestosLeitura(ReadOnlyArray<Toque> toques)
    {
        var rt = conteudoLeitura;
        if (rt == null) return;
        Vector2 posicao = rt.anchoredPosition;

        if (toques.Count >= 2)
        {
            // Pinca: zoom em torno do ponto entre os dedos, que tambem arrasta a tabela
            Vector2 meio = (toques[0].screenPosition + toques[1].screenPosition) / 2f;
            float distancia = Vector2.Distance(toques[0].screenPosition, toques[1].screenPosition);
            if (distanciaPinca > 0f)
            {
                float novaEscala = CalculosGestos.LimitarZoomLeitura(escalaLeitura * CalculosGestos.FatorPinca(distanciaPinca, distancia));
                Vector2 meioAnterior = ParaVisor(meioPinca);
                posicao = CalculosGestos.ZoomEmTornoDoPonto(posicao, meioAnterior, novaEscala / escalaLeitura)
                          + (ParaVisor(meio) - meioAnterior);
                escalaLeitura = novaEscala;
            }
            distanciaPinca = distancia;
            meioPinca = meio;
            dedoArrastando = -1;
        }
        else
        {
            distanciaPinca = -1f;
            if (toques.Count == 1 && !ComecouNaUIdeControle(toques[0]))
            {
                // Um dedo: arrasta a tabela
                var t = toques[0];
                if (dedoArrastando == t.touchId)
                    posicao += ParaVisor(t.screenPosition) - ParaVisor(posicaoArrasto);
                dedoArrastando = t.touchId;
                posicaoArrasto = t.screenPosition;
            }
            else
            {
                dedoArrastando = -1;
            }
        }

        var mouse = Mouse.current;
        if (mouse != null && toques.Count == 0)
        {
            Vector2 ponteiro = mouse.position.ReadValue();
            float rolagem = mouse.scroll.ReadValue().y;
            if (rolagem != 0f)
            {
                float novaEscala = CalculosGestos.LimitarZoomLeitura(escalaLeitura * CalculosGestos.FatorRoda(rolagem));
                posicao = CalculosGestos.ZoomEmTornoDoPonto(posicao, ParaVisor(ponteiro), novaEscala / escalaLeitura);
                escalaLeitura = novaEscala;
            }

            if (mouse.leftButton.wasPressedThisFrame)
                cliqueNaUI = SobreControle(ponteiro);
            if (mouse.leftButton.isPressed && !cliqueNaUI)
                posicao += ParaVisor(ponteiro) - ParaVisor(ponteiro - mouse.delta.ReadValue());
        }

        float escala = escalaAjuste * escalaLeitura;
        rt.localScale = Vector3.one * escala;
        rt.anchoredPosition = CalculosGestos.LimitarDeslocamento(posicao, rt.sizeDelta * escala, visorLeitura.rect.size);
    }

    // Converte um ponto da tela para as coordenadas do visor (origem no centro)
    Vector2 ParaVisor(Vector2 posicaoTela)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(visorLeitura, posicaoTela, null, out Vector2 local);
        return local;
    }

    // ---------- Apoio ----------

    // 3% do lado menor da tela: tolerancia para o dedo tremer e ainda contar como toque
    static float LimiarToque()
    {
        return Mathf.Min(Screen.width, Screen.height) * 0.03f;
    }

    bool ComecouNaUI(Toque t)
    {
        return toqueComecouNaUI.TryGetValue(t.touchId, out bool naUI) && naUI;
    }

    // No modo leitura o painel inteiro e UI; so os botoes contam como "controle"
    bool ComecouNaUIdeControle(Toque t)
    {
        return ComecouNaUI(t) && SobreControle(t.startScreenPosition);
    }

    bool SobreUI(Vector2 posicaoTela)
    {
        return RaycastUI(posicaoTela).Count > 0;
    }

    bool SobreControle(Vector2 posicaoTela)
    {
        foreach (var r in RaycastUI(posicaoTela))
            if (r.gameObject.GetComponentInParent<Selectable>() != null) return true;
        return false;
    }

    List<RaycastResult> RaycastUI(Vector2 posicaoTela)
    {
        resultadosUI.Clear();
        if (EventSystem.current == null) return resultadosUI;
        var dados = new PointerEventData(EventSystem.current) { position = posicaoTela };
        EventSystem.current.RaycastAll(dados, resultadosUI);
        return resultadosUI;
    }
}
