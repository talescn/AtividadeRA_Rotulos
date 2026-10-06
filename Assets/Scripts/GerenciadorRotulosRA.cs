using System.Collections.Generic;
using UnityEngine;
using Vuforia;

// Atividade 01 - Realidade Aumentada Marker Based
// Cria em tempo de execucao 3 Image Targets (um para cada rotulo de produto)
// e, quando o rotulo e reconhecido, mostra a tabela nutricional do produto na frente dele.
// A tabela e desenhada a partir dos dados do produto (por GTIN); a foto da tabela so e usada se nao houver dados.
public class GerenciadorRotulosRA : MonoBehaviour
{
    [System.Serializable]
    public class Produto
    {
        public string nome = "Produto";
        [Tooltip("Codigo de barras (GTIN/EAN). Os dados vem de Resources/Produtos/{gtin}.json, do cache ou do Open Food Facts.")]
        public string gtin;
        [Tooltip("Foto do rotulo (marcador). Precisa estar com Read/Write habilitado.")]
        public Texture2D rotulo;
        [Tooltip("Foto da tabela nutricional do proprio produto. So aparece se nao houver dados para o GTIN.")]
        public Texture2D tabelaNutricional;
        [Tooltip("Largura real do rotulo em metros (ex.: 0.10 = 10 cm).")]
        public float larguraRotuloMetros = 0.10f;
        [Tooltip("Largura da tabela nutricional exibida, em metros.")]
        public float larguraTabelaMetros = 0.12f;
    }

    // Estado de cada produto enquanto o app roda
    class ProdutoEmCena
    {
        public Produto produto;
        public Transform target;
        public Transform tabela;
        public Vector3 escalaBase;
        public DadosProduto dados;
        public bool rastreado;
    }

    public List<Produto> produtos = new List<Produto>();

    [Tooltip("Distancia (m) da tabela a frente da superficie do rotulo.")]
    public float distanciaFrente = 0.01f;

    // Avisado sempre que um produto entra ou sai da camera
    public event System.Action AoMudarRastreamento;

    public float Zoom { get; private set; } = 1f;

    readonly List<ProdutoEmCena> emCena = new List<ProdutoEmCena>();
    // Produtos visiveis agora, do mais antigo para o mais recente
    readonly List<ProdutoEmCena> rastreados = new List<ProdutoEmCena>();
    RepositorioProdutos repositorio;

    // Ultimo produto que entrou na camera e ainda esta visivel (null se nenhum)
    public Produto ProdutoEmFoco => rastreados.Count > 0 ? rastreados[rastreados.Count - 1].produto : null;

    // Usado tambem pela interface, para os produtos lidos pelo codigo de barras
    public RepositorioProdutos Repositorio => repositorio ??= RepositorioProdutos.Padrao();

    void Start()
    {
        repositorio = Repositorio;
        if (VuforiaApplication.Instance.IsRunning)
            CriarTargets();
        else
            VuforiaApplication.Instance.OnVuforiaStarted += CriarTargets;
    }

    void OnDestroy()
    {
        if (VuforiaApplication.Instance != null)
            VuforiaApplication.Instance.OnVuforiaStarted -= CriarTargets;
    }

    void CriarTargets()
    {
        VuforiaApplication.Instance.OnVuforiaStarted -= CriarTargets;

        foreach (var p in produtos)
        {
            // Dados locais ou do cache; sem rede, entao responde na hora
            DadosProduto dados = repositorio.BuscarSemRede(p.gtin);
            if (dados != null && !dados.TemTabela) dados = null;

            if (p.rotulo == null || (dados == null && p.tabelaNutricional == null && string.IsNullOrEmpty(p.gtin)))
            {
                Debug.LogWarning($"[RA] Produto '{p.nome}' sem foto do rotulo ou sem tabela (nem dados, nem foto, nem GTIN).");
                continue;
            }

            Texture2D imagemMarcador = CopiarRGBA32(p.rotulo);
            ImageTargetBehaviour target = VuforiaBehaviour.Instance.ObserverFactory
                .CreateImageTarget(imagemMarcador, p.larguraRotuloMetros, "Rotulo_" + p.nome);

            if (target == null)
            {
                Debug.LogError($"[RA] Nao foi possivel criar o Image Target de '{p.nome}'.");
                continue;
            }

            target.gameObject.name = "ImageTarget_" + p.nome;
            // Mostra/esconde os filhos automaticamente quando o rotulo e encontrado/perdido
            var handler = target.gameObject.AddComponent<DefaultObserverEventHandler>();
            // So mostra a tabela enquanto o rotulo esta realmente visivel (evita tabela "fantasma")
            handler.StatusFilter = DefaultObserverEventHandler.TrackingStatusFilter.Tracked;

            var item = new ProdutoEmCena { produto = p, target = target.transform };
            emCena.Add(item);
            DefinirTabela(item, dados);

            // Os mesmos eventos que ligam e desligam a tabela dizem a interface o que esta visivel
            handler.OnTargetFound.AddListener(() => MarcarRastreado(item, true));
            handler.OnTargetLost.AddListener(() => MarcarRastreado(item, false));
            Debug.Log($"[RA] Marcador criado: {p.nome} ({(dados != null ? "tabela dos dados" : "foto da tabela")})");

            // Sem dado local: tenta o Open Food Facts; se nao vier nada, fica a foto
            if (dados == null && !string.IsNullOrEmpty(p.gtin))
                StartCoroutine(repositorio.BuscarOpenFoodFacts(p.gtin, (recebidos, motivo) => AoReceberDados(item, recebidos)));
        }
    }

    void AoReceberDados(ProdutoEmCena item, DadosProduto dados)
    {
        if (dados == null) return;
        Debug.Log($"[RA] Dados do Open Food Facts para {item.produto.nome}");
        DefinirTabela(item, dados);
        // A tabela nova nasce depois do evento de rastreamento: liga ou desliga conforme o rotulo esta visivel
        DefinirVisivel(item.tabela, item.rastreado);
    }

    // Troca a tabela do target: desenhada a partir dos dados ou, sem dados, a foto
    void DefinirTabela(ProdutoEmCena item, DadosProduto dados)
    {
        if (item.tabela != null) Destroy(item.tabela.gameObject);
        item.dados = dados;
        item.tabela = dados != null ? CriarTabelaDados(item.target, item.produto, dados)
            : item.produto.tabelaNutricional != null ? CriarTabelaFoto(item.target, item.produto) : null;
        if (item.tabela == null) return;
        item.escalaBase = item.tabela.localScale;
        item.tabela.localScale = CalculosGestos.EscalaTabela(item.escalaBase, Zoom);
    }

    Transform CriarTabelaDados(Transform pai, Produto p, DadosProduto dados)
    {
        var go = new GameObject("TabelaNutricional_" + p.nome, typeof(RectTransform), typeof(Canvas));
        var rt = (RectTransform)go.transform;
        rt.SetParent(pai, false);
        go.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;

        var tabela = TabelaNutricionalUI.Montar(rt, dados);
        rt.sizeDelta = tabela.sizeDelta;
        // Mesmo plano e mesma orientacao do quad da foto
        rt.localRotation = Quaternion.Euler(90f, 0f, 0f);
        rt.localPosition = new Vector3(0f, distanciaFrente, 0f);
        float escala = p.larguraTabelaMetros / TabelaNutricionalUI.Largura;
        rt.localScale = new Vector3(escala, escala, escala);

        // O toque na tabela e detectado neste collider; o DefaultObserverEventHandler o liga e desliga junto com o Canvas
        go.AddComponent<BoxCollider>().size = new Vector3(tabela.sizeDelta.x, tabela.sizeDelta.y, 1f);
        return rt;
    }

    Transform CriarTabelaFoto(Transform pai, Produto p)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "TabelaNutricional_" + p.nome;
        // O MeshCollider do quad fica: e nele que o toque na tabela e detectado.
        // O DefaultObserverEventHandler desliga o collider junto com a tabela.

        quad.transform.SetParent(pai, false);
        // Plano do Image Target = XZ (normal +Y apontando para a camera).
        quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localPosition = new Vector3(0f, distanciaFrente, 0f);

        float largura = p.larguraTabelaMetros;
        float altura = largura * p.tabelaNutricional.height / (float)p.tabelaNutricional.width;
        quad.transform.localScale = new Vector3(largura, altura, 1f);

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        Material mat = new Material(shader);
        mat.mainTexture = p.tabelaNutricional;
        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", p.tabelaNutricional);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        quad.GetComponent<MeshRenderer>().material = mat;
        return quad.transform;
    }

    // Multiplica o tamanho de todas as tabelas em RA, mantendo a proporcao
    public void DefinirZoom(float zoom)
    {
        Zoom = CalculosGestos.LimitarZoom(zoom);
        foreach (var item in emCena)
            if (item.tabela != null)
                item.tabela.localScale = CalculosGestos.EscalaTabela(item.escalaBase, Zoom);
    }

    // Produto dono da tabela tocada (null se o objeto nao for uma tabela)
    public Produto ProdutoDaTabela(Transform tabela)
    {
        foreach (var item in emCena)
            if (item.tabela == tabela) return item.produto;
        return null;
    }

    // Dados usados na tabela do produto (null se ele esta mostrando a foto)
    public DadosProduto DadosDe(Produto produto)
    {
        foreach (var item in emCena)
            if (item.produto == produto) return item.dados;
        return null;
    }

    void MarcarRastreado(ProdutoEmCena item, bool rastreado)
    {
        item.rastreado = rastreado;
        rastreados.Remove(item);
        if (rastreado) rastreados.Add(item);
        AoMudarRastreamento?.Invoke();
    }

    // Faz o mesmo que o DefaultObserverEventHandler, para uma tabela criada depois do evento
    static void DefinirVisivel(Transform raiz, bool visivel)
    {
        if (raiz == null) return;
        foreach (var r in raiz.GetComponentsInChildren<Renderer>(true)) r.enabled = visivel;
        foreach (var c in raiz.GetComponentsInChildren<Collider>(true)) c.enabled = visivel;
        foreach (var c in raiz.GetComponentsInChildren<Canvas>(true)) c.enabled = visivel;
    }

    static Texture2D CopiarRGBA32(Texture2D origem)
    {
        var copia = new Texture2D(origem.width, origem.height, TextureFormat.RGBA32, false);
        copia.SetPixels32(origem.GetPixels32());
        copia.Apply();
        return copia;
    }
}
