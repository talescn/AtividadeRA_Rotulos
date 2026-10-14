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

    [Header("Codigo de barras")]
    public LeitorCodigoBarras leitorCodigo;
    [Tooltip("Faixa no topo da tela com a dica e os avisos da leitura do codigo de barras.")]
    public GameObject faixaAviso;
    public TMP_Text textoAviso;

    [Header("Camera")]
    public ControleCamera controleCamera;
    [Tooltip("Liga e apaga a lanterna; so aparece se a camera tiver lanterna.")]
    public Button botaoLanterna;

    [Header("Ensinar rotulo")]
    public EnsinarRotuloUI ensinar;
    [Tooltip("No modo leitura: transforma o rotulo do produto aberto num marcador de RA.")]
    public Button botaoEnsinar;

    [Header("Quantidade e voz")]
    [Tooltip("No modo leitura: quantidade que a pessoa vai consumir, carboidratos nela e o botao Ouvir.")]
    public PainelConsumoUI painelConsumo;

    const string Dica = "Aponte para o rótulo ou para o código de barras";
    static readonly Color CorLanternaApagada = Color.white;
    static readonly Color CorLanternaLigada = new Color(1f, 0.85f, 0.2f);
    string aviso;
    float avisoAte;
    bool buscandoCodigo;

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

    // Dados da tabela aberta no modo leitura (null quando e a foto da tabela)
    DadosProduto dadosEmLeitura;

    bool EmLeitura => painelLeitura.activeSelf;
    bool Ensinando => ensinar != null && ensinar.Aberto;

    void Awake()
    {
        controleZoom.minValue = CalculosGestos.ZoomMinimo;
        controleZoom.maxValue = CalculosGestos.ZoomMaximo;
        controleZoom.onValueChanged.AddListener(AoMudarControleZoom);
        botaoLer.onClick.AddListener(AbrirLeituraDoProdutoEmFoco);
        botaoFechar.onClick.AddListener(FecharLeitura);
        if (botaoLanterna != null && controleCamera != null)
            botaoLanterna.onClick.AddListener(controleCamera.AlternarLanterna);
        if (botaoEnsinar != null) botaoEnsinar.onClick.AddListener(EnsinarRotuloEmLeitura);
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        gerenciador.AoMudarRastreamento += AtualizarBotaoLer;
        if (leitorCodigo != null) leitorCodigo.AoLerCodigo += AoLerCodigo;
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        gerenciador.AoMudarRastreamento -= AtualizarBotaoLer;
        if (leitorCodigo != null) leitorCodigo.AoLerCodigo -= AoLerCodigo;
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
        // Enquanto ensina um rotulo, a tela e da captura e do recorte
        else if (!Ensinando)
            GestosRA(toques);

        // Esquece os dedos que sairam da tela neste quadro
        foreach (var t in toques)
            if (t.phase == FaseToque.Ended || t.phase == FaseToque.Canceled)
                toqueComecouNaUI.Remove(t.touchId);
        if (toques.Count == 0) houveMultiToque = false;

        // O codigo de barras so e procurado depois de criados os marcadores e quando nenhum rotulo esta na camera
        bool procurando = gerenciador.Pronto && !EmLeitura && !Ensinando && !buscandoCodigo && gerenciador.ProdutoEmFoco == null;
        if (leitorCodigo != null) leitorCodigo.Ativo = procurando;
        AtualizarAviso(procurando);
        AtualizarBotaoLer();
        AtualizarBotaoLanterna();
        // A barra de zoom sai do caminho dos botoes da captura
        var barraZoom = controleZoom.transform.parent.gameObject;
        if (barraZoom.activeSelf == Ensinando) barraZoom.SetActive(!Ensinando);
    }

    // Some no modo leitura, no recorte do rotulo e quando a camera nao tem lanterna; amarelo enquanto esta ligada
    void AtualizarBotaoLanterna()
    {
        if (botaoLanterna == null || controleCamera == null) return;
        bool mostrar = !EmLeitura && !(Ensinando && ensinar.EmRecorte) && controleCamera.LanternaDisponivel;
        if (botaoLanterna.gameObject.activeSelf != mostrar) botaoLanterna.gameObject.SetActive(mostrar);
        botaoLanterna.image.color = controleCamera.LanternaLigada ? CorLanternaLigada : CorLanternaApagada;
    }

    // ---------- Codigo de barras ----------

    void AoLerCodigo(string gtin)
    {
        // JSON local ou cache: abre na hora
        DadosProduto dados = gerenciador.Repositorio.BuscarSemRede(gtin);
        if (dados != null && dados.TemTabela)
        {
            Debug.Log($"[RA] Dados de {gtin}: no aparelho (fonte {dados.fonte})");
            AbrirLeitura(dados);
            return;
        }

        buscandoCodigo = true;
        MostrarAviso($"Código {gtin}: buscando no Open Food Facts...", 20f);
        StartCoroutine(gerenciador.Repositorio.BuscarOpenFoodFacts(gtin, (recebidos, motivo) =>
        {
            buscandoCodigo = false;
            Debug.Log($"[RA] Dados de {gtin}: " + (recebidos != null ? "Open Food Facts" : "sem tabela (" + motivo + ")"));
            if (recebidos != null)
            {
                MostrarAviso(null, 0f);
                AbrirLeitura(recebidos);
            }
            else
            {
                MostrarAviso($"Código {gtin}: {motivo}.", 6f);
            }
        }));
    }

    void MostrarAviso(string texto, float segundos)
    {
        aviso = texto;
        avisoAte = Time.unscaledTime + segundos;
    }

    // Faixa do topo: um aviso recente ou, enquanto procura, a dica de para onde apontar
    void AtualizarAviso(bool procurando)
    {
        if (faixaAviso == null) return;
        string texto = !string.IsNullOrEmpty(aviso) && Time.unscaledTime < avisoAte ? aviso : procurando ? Dica : null;
        bool mostrar = texto != null && !EmLeitura;
        if (faixaAviso.activeSelf != mostrar) faixaAviso.SetActive(mostrar);
        if (mostrar && textoAviso.text != texto) textoAviso.text = texto;
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
        var produto = Physics.Raycast(cam.ScreenPointToRay(posicaoTela), out RaycastHit acerto)
            ? gerenciador.ProdutoDaTabela(acerto.transform) : null;

        if (produto != null)
            AbrirLeitura(produto);
        // Toque fora da tabela, enquanto nenhum rotulo esta na camera: foca naquele ponto (ajuda a ler o
        // codigo de barras de perto). Com um rotulo rastreado, mexer no foco poderia atrapalhar o rastreamento.
        else if (controleCamera != null && gerenciador.ProdutoEmFoco == null)
            controleCamera.FocarEm(posicaoTela);
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
        bool mostrar = !EmLeitura && !Ensinando && gerenciador.ProdutoEmFoco != null;
        if (botaoLer.gameObject.activeSelf != mostrar) botaoLer.gameObject.SetActive(mostrar);
    }

    // Fecha a leitura e comeca a ensinar o rotulo do produto que estava aberto
    void EnsinarRotuloEmLeitura()
    {
        var dados = dadosEmLeitura;
        if (dados == null || ensinar == null) return;
        FecharLeitura();
        ensinar.Abrir(dados);
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
        if (dados != null)
        {
            AbrirLeitura(dados);
            return;
        }
        if (produto.tabelaNutricional == null) return;

        // Sem dados: mostra a foto, no tamanho da imagem (o ajuste a tela vem da escala)
        LimparTabelaDados();
        DefinirDadosEmLeitura(null);
        tabelaLeitura.gameObject.SetActive(true);
        tabelaLeitura.texture = produto.tabelaNutricional;
        tabelaLeitura.rectTransform.sizeDelta = new Vector2(produto.tabelaNutricional.width, produto.tabelaNutricional.height);
        MostrarLeitura(tabelaLeitura.rectTransform, produto.nome);
    }

    // Tabela desenhada a partir dos dados: de um produto da cena ou de um codigo de barras lido
    public void AbrirLeitura(DadosProduto dados)
    {
        if (dados == null) return;
        LimparTabelaDados();
        DefinirDadosEmLeitura(dados);
        tabelaLeitura.gameObject.SetActive(false);
        tabelaDadosLeitura = TabelaNutricionalUI.Montar(visorLeitura, dados);
        MostrarLeitura(tabelaDadosLeitura, dados.nome);
    }

    // So da para ensinar o rotulo de um produto com dados e GTIN valido
    void DefinirDadosEmLeitura(DadosProduto dados)
    {
        dadosEmLeitura = dados;
        if (botaoEnsinar != null)
            botaoEnsinar.gameObject.SetActive(ensinar != null && dados != null && DadosProduto.GtinValido(dados.gtin));
        // Sem dados (so a foto da tabela) o painel some; ao fechar a leitura, a voz para junto
        if (painelConsumo != null)
        {
            if (dados != null) painelConsumo.Mostrar(dados);
            else painelConsumo.Esconder();
        }
    }

    void MostrarLeitura(RectTransform conteudo, string titulo)
    {
        conteudoLeitura = conteudo;
        tituloLeitura.text = titulo;
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
        // No diario de testes, o intervalo ate o proximo codigo lido mede quanto a leitura demora
        if (EmLeitura) Debug.Log("[RA] Leitura fechada");
        painelLeitura.SetActive(false);
        LimparTabelaDados();
        DefinirDadosEmLeitura(null);
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
