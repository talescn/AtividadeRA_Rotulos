using TMPro;
using UnityEngine;
using UnityEngine.UI;

// "Ensinar" o rotulo de um produto lido pelo codigo de barras: o usuario aponta para o rotulo e captura; a
// foto mais nitida aparece congelada com um retangulo ajustavel e a nota da imagem; o pedaco escolhido vira
// um marcador do Vuforia e a tabela passa a aparecer em RA sobre o rotulo. Cada produto pode ganhar mais de
// uma vista (frente e lados de uma garrafa), o que compensa o rotulo curvo.
public class EnsinarRotuloUI : MonoBehaviour
{
    public GerenciadorRotulosRA gerenciador;
    public CapturaRotulo captura;

    [Header("Captura")]
    public GameObject painelCaptura;
    public TMP_Text textoCaptura;
    public Button botaoCapturar;
    public Button botaoConcluir;

    [Header("Recorte")]
    public GameObject painelRecorte;
    [Tooltip("Area em que a foto congelada cabe inteira.")]
    public RectTransform visorRecorte;
    public RawImage fotoRecorte;
    [Tooltip("Retangulo ajustavel, filho da foto: as ancoras dele sao o recorte.")]
    public RectTransform molduraRecorte;
    public TMP_Text textoNota;
    public Button botaoCriar;
    public Button botaoRefazer;

    [Tooltip("Menor nota, em estrelas, para aceitar o marcador.")]
    public int estrelasMinimas = 2;

    // A nota e recalculada no maximo a cada tanto enquanto o retangulo e arrastado
    const float IntervaloNota = 0.15f;
    static readonly string[] NomesNota = { "inútil", "ruim", "fraca", "razoável", "boa", "ótima" };

    public bool Aberto => painelCaptura.activeSelf || painelRecorte.activeSelf;
    public bool EmRecorte => painelRecorte.activeSelf;

    DadosProduto produto;
    // Foto escolhida pela captura, em cinza e na orientacao da tela
    byte[] foto;
    int fotoLargura, fotoAltura;
    Texture2D previa;
    Vector2 visorMedido;
    Rect recorte;
    QualidadeMarcador.Avaliacao avaliacao;
    bool notaDesatualizada;
    float proximaNota;

    void Awake()
    {
        botaoCapturar.onClick.AddListener(Capturar);
        botaoConcluir.onClick.AddListener(Fechar);
        botaoCriar.onClick.AddListener(Criar);
        botaoRefazer.onClick.AddListener(() => MostrarCaptura(null));
    }

    // Comeca a ensinar o rotulo de um produto ja identificado
    public void Abrir(DadosProduto dados)
    {
        if (dados == null || !DadosProduto.GtinValido(dados.gtin)) return;
        produto = dados;
        MostrarCaptura(null);
    }

    public void Fechar()
    {
        captura.Cancelar();
        painelCaptura.SetActive(false);
        painelRecorte.SetActive(false);
        LiberarFoto();
        produto = null;
    }

    void MostrarCaptura(string aviso)
    {
        LiberarFoto();
        painelRecorte.SetActive(false);
        painelCaptura.SetActive(true);
        botaoCapturar.interactable = true;

        int vistas = gerenciador.VistasEnsinadas(produto.gtin);
        string texto = vistas == 0
            ? $"Aponte para o rótulo de <b>{produto.nome}</b>: de frente, bem perto e sem reflexo. Toque na tela para focar."
            : $"{vistas} de {CatalogoMarcadores.VistasPorProduto} vistas salvas. Em garrafa, capture também os lados do rótulo.";
        textoCaptura.text = aviso != null ? aviso + "\n" + texto : texto;
        botaoConcluir.GetComponentInChildren<TMP_Text>().text = vistas > 0 ? "Concluir" : "Cancelar";
    }

    void Capturar()
    {
        if (captura.Capturando) return;
        botaoCapturar.interactable = false;
        textoCaptura.text = "Segure firme...";
        captura.Iniciar(AoCapturar);
    }

    void AoCapturar(byte[] cinza, int largura, int altura)
    {
        // Fechado enquanto capturava
        if (produto == null) return;
        if (cinza == null)
        {
            MostrarCaptura("A câmera não entregou imagem. Tente de novo.");
            return;
        }
        foto = cinza;
        fotoLargura = largura;
        fotoAltura = altura;
        MostrarRecorte();
    }

    void MostrarRecorte()
    {
        painelCaptura.SetActive(false);
        painelRecorte.SetActive(true);

        // Na tela basta metade da resolucao; o marcador sai da foto inteira
        byte[] reduzida = ImagemCinza.Reduzir(foto, fotoLargura, fotoAltura, Mathf.Max(fotoLargura, fotoAltura) / 2,
                                              out int w, out int h);
        previa = ImagemCinza.ParaTextura(reduzida, w, h);
        fotoRecorte.texture = previa;
        // O tamanho da foto sai no Update: a area segura so ajusta o visor depois que o painel aparece
        visorMedido = Vector2.zero;
        AjustarFoto();

        recorte = CalculosRecorte.Inicial;
        AtualizarMoldura();
    }

    // A foto cabe inteira no visor, mantendo a proporcao; refeito quando o visor muda de tamanho
    void AjustarFoto()
    {
        Vector2 visor = visorRecorte.rect.size;
        if (visor == visorMedido) return;
        visorMedido = visor;
        fotoRecorte.rectTransform.sizeDelta = CalculosGestos.TamanhoAjustado(new Vector2(fotoLargura, fotoAltura), visor);
    }

    // Chamado pelas alcas; o deslocamento vem em pixels da tela
    public void Arrastar(int canto, Vector2 deltaTela)
    {
        if (!EmRecorte) return;
        Vector2 tamanho = fotoRecorte.rectTransform.rect.size * fotoRecorte.canvas.scaleFactor;
        if (tamanho.x <= 0f || tamanho.y <= 0f) return;
        var delta = new Vector2(deltaTela.x / tamanho.x, deltaTela.y / tamanho.y);
        recorte = canto < 0 ? CalculosRecorte.Mover(recorte, delta) : CalculosRecorte.MoverCanto(recorte, canto, delta);
        AtualizarMoldura();
    }

    void AtualizarMoldura()
    {
        molduraRecorte.anchorMin = recorte.min;
        molduraRecorte.anchorMax = recorte.max;
        molduraRecorte.offsetMin = molduraRecorte.offsetMax = Vector2.zero;
        notaDesatualizada = true;
    }

    void Update()
    {
        if (!EmRecorte) return;
        AjustarFoto();
        if (!notaDesatualizada || Time.unscaledTime < proximaNota) return;
        proximaNota = Time.unscaledTime + IntervaloNota;
        AtualizarNota();
    }

    void AtualizarNota()
    {
        notaDesatualizada = false;
        RectInt px = CalculosRecorte.ParaPixels(recorte, fotoLargura, fotoAltura);
        avaliacao = QualidadeMarcador.Avaliar(ImagemCinza.Recortar(foto, fotoLargura, px), px.width, px.height);

        bool aceita = avaliacao.estrelas >= estrelasMinimas;
        string cor = avaliacao.estrelas >= 4 ? "#7CFC7C" : aceita ? "#FFD84D" : "#FF7A6B";
        textoNota.text = $"Qualidade do marcador: <color={cor}><b>{avaliacao.estrelas} de 5, {NomesNota[avaliacao.estrelas]}</b></color>\n" +
                         (avaliacao.motivo ?? "Deixe só o rótulo dentro do retângulo, sem a parte transparente.");
        botaoCriar.interactable = aceita;
    }

    void Criar()
    {
        if (foto == null) return;
        // O ultimo arrasto pode nao ter sido avaliado ainda
        if (notaDesatualizada) AtualizarNota();
        if (avaliacao.estrelas < estrelasMinimas) return;

        RectInt px = CalculosRecorte.ParaPixels(recorte, fotoLargura, fotoAltura);
        Texture2D rotulo = ImagemCinza.ParaTextura(ImagemCinza.Recortar(foto, fotoLargura, px), px.width, px.height);
        // O gerenciador fica com a textura se o marcador for criado
        bool criado = gerenciador.EnsinarMarcador(produto, rotulo, avaliacao.estrelas);
        if (!criado) Destroy(rotulo);
        MostrarCaptura(criado ? "Marcador criado! Aponte para o rótulo: a tabela aparece sobre ele."
                              : "Não foi possível criar o marcador. Tente de novo.");
    }

    void LiberarFoto()
    {
        foto = null;
        fotoRecorte.texture = null;
        if (previa != null) Destroy(previa);
        previa = null;
    }
}
