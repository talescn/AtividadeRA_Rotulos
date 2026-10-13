using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Vuforia;

// Atividade 01 - Realidade Aumentada Marker Based
// Cria em tempo de execucao um Image Target para cada rotulo (os 3 produtos da cena e os rotulos ensinados
// no app, guardados no CatalogoMarcadores) e, quando o rotulo e reconhecido, mostra a tabela nutricional na frente dele.
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
        // Nome do marcador no Vuforia ("Rotulo_..." dos produtos da cena, "Ensinado_..." dos ensinados no app)
        public string nomeMarcador;
        public Transform target;
        public Transform tabela;
        public Vector3 escalaBase;
        public DadosProduto dados;
        public bool rastreado;
    }

    public List<Produto> produtos = new List<Produto>();

    [Tooltip("Distancia (m) da tabela a frente da superficie do rotulo.")]
    public float distanciaFrente = 0.01f;

    [Header("Rotulos ensinados no app")]
    [Tooltip("Largura usada para o rotulo ensinado, em metros. Nao precisa ser a real: a tabela e medida em proporcao a ela.")]
    public float larguraRotuloEnsinado = 0.08f;
    [Tooltip("Largura da tabela de um rotulo ensinado, em vezes a largura do rotulo.")]
    public float fatorTabelaEnsinada = 1f;

    // Avisado sempre que um produto entra ou sai da camera
    public event System.Action AoMudarRastreamento;

    public float Zoom { get; private set; } = 1f;

    readonly List<ProdutoEmCena> emCena = new List<ProdutoEmCena>();
    // Produtos visiveis agora, do mais antigo para o mais recente
    readonly List<ProdutoEmCena> rastreados = new List<ProdutoEmCena>();
    RepositorioProdutos repositorio;
    CatalogoMarcadores catalogo;

    // Ultimo produto que entrou na camera e ainda esta visivel (null se nenhum)
    public Produto ProdutoEmFoco => rastreados.Count > 0 ? rastreados[rastreados.Count - 1].produto : null;

    // Usado tambem pela interface, para os produtos lidos pelo codigo de barras
    public RepositorioProdutos Repositorio => repositorio ??= RepositorioProdutos.Padrao();

    // Rotulos ensinados no app, guardados no aparelho
    public CatalogoMarcadores Catalogo => catalogo ??= CatalogoMarcadores.Padrao();

    // Todos os marcadores ja foram criados. O leitor de codigo de barras so mexe na camera depois disso:
    // pedir a imagem da camera enquanto o Vuforia cria os marcadores derrubou o app uma vez no celular.
    public bool Pronto { get; private set; }

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

            var item = CriarMarcador(p, dados, "Rotulo_" + p.nome);
            // Sem dado local: tenta o Open Food Facts; se nao vier nada, fica a foto
            if (item != null && dados == null && !string.IsNullOrEmpty(p.gtin))
                StartCoroutine(repositorio.BuscarOpenFoodFacts(p.gtin, (recebidos, motivo) => AoReceberDados(item, recebidos)));
        }

        // Rotulos ensinados no app em outras vezes. Um marcador estragado nao pode impedir o resto:
        // sem Pronto, o leitor de codigo de barras nunca liga.
        foreach (var marcador in Catalogo.Marcadores)
        {
            try
            {
                CarregarMarcadorEnsinado(marcador);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RA] Marcador ensinado {marcador.arquivo} ficou de fora: {e.Message}");
            }
        }
        Pronto = true;
    }

    ProdutoEmCena CriarMarcador(Produto p, DadosProduto dados, string nomeMarcador)
    {
        // As fotos da cena chegam em RGB e sao copiadas; um rotulo ensinado ja vem em RGBA32 legivel
        Texture2D imagemMarcador = p.rotulo.format == TextureFormat.RGBA32 && p.rotulo.isReadable
            ? p.rotulo : CopiarRGBA32(p.rotulo);
        ImageTargetBehaviour target = VuforiaBehaviour.Instance.ObserverFactory
            .CreateImageTarget(imagemMarcador, p.larguraRotuloMetros, nomeMarcador);

        if (target == null)
        {
            Debug.LogError($"[RA] Nao foi possivel criar o Image Target de '{p.nome}' ({nomeMarcador}).");
            return null;
        }

        target.gameObject.name = "ImageTarget_" + nomeMarcador;
        // Mostra/esconde os filhos automaticamente quando o rotulo e encontrado/perdido
        var handler = target.gameObject.AddComponent<DefaultObserverEventHandler>();
        // So mostra a tabela enquanto o rotulo esta realmente visivel (evita tabela "fantasma")
        handler.StatusFilter = DefaultObserverEventHandler.TrackingStatusFilter.Tracked;

        var item = new ProdutoEmCena { produto = p, nomeMarcador = nomeMarcador, target = target.transform };
        emCena.Add(item);
        DefinirTabela(item, dados);

        // Os mesmos eventos que ligam e desligam a tabela dizem a interface o que esta visivel
        handler.OnTargetFound.AddListener(() => MarcarRastreado(item, true));
        handler.OnTargetLost.AddListener(() => MarcarRastreado(item, false));
        Debug.Log($"[RA] Marcador criado: {nomeMarcador} ({(dados != null ? "tabela dos dados" : "foto da tabela")})");
        return item;
    }

    // ---------- Rotulos ensinados no app ----------

    public int VistasEnsinadas(string gtin)
    {
        return Catalogo.ContarVistas(gtin);
    }

    // Cria na hora o marcador de um rotulo capturado pela camera e o guarda no aparelho. Fica com a textura.
    // As vistas mais antigas do mesmo produto, alem do limite do catalogo, saem da cena.
    public bool EnsinarMarcador(DadosProduto dados, Texture2D imagem, int estrelas)
    {
        if (!VuforiaApplication.Instance.IsRunning || dados == null || !dados.TemTabela) return false;
        DateTime agora = DateTime.Now;
        string arquivo = CatalogoMarcadores.NomeArquivo(dados.gtin, agora);
        if (CriarMarcadorEnsinado(arquivo, larguraRotuloEnsinado, dados, imagem) == null) return false;

        var removidos = new List<CatalogoMarcadores.Marcador>();
        try
        {
            Catalogo.Adicionar(dados.gtin, imagem.EncodeToPNG(), larguraRotuloEnsinado, estrelas, agora, removidos);
        }
        catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
        {
            // O marcador funciona agora; so nao volta na proxima vez que o app abrir
            Debug.LogWarning($"[RA] Marcador de {dados.gtin} criado, mas nao foi salvo no aparelho: {e.Message}");
        }
        finally
        {
            // A imagem das vistas antigas ja saiu da pasta, mesmo que o indice nao tenha sido salvo
            foreach (var antiga in removidos) RemoverMarcador(NomeEnsinado(antiga.arquivo));
        }
        return true;
    }

    void CarregarMarcadorEnsinado(CatalogoMarcadores.Marcador marcador)
    {
        byte[] png = Catalogo.LerImagem(marcador);
        DadosProduto dados = repositorio.BuscarSemRede(marcador.gtin);
        if (png == null || dados == null || !dados.TemTabela)
        {
            Debug.LogWarning($"[RA] Marcador ensinado {marcador.arquivo} ficou de fora: " +
                             (png == null ? "imagem nao encontrada." : "o produto nao tem tabela no aparelho."));
            return;
        }
        var imagem = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!imagem.LoadImage(png))
        {
            Debug.LogWarning($"[RA] Imagem do marcador ensinado {marcador.arquivo} ilegivel.");
            Destroy(imagem);
            return;
        }
        if (CriarMarcadorEnsinado(marcador.arquivo, marcador.larguraMetros, dados, imagem) == null) Destroy(imagem);
    }

    // A tabela tem a largura do rotulo vezes fatorTabelaEnsinada, para ficar na mesma proporcao em qualquer rotulo
    ProdutoEmCena CriarMarcadorEnsinado(string arquivo, float largura, DadosProduto dados, Texture2D imagem)
    {
        var p = new Produto
        {
            nome = dados.nome,
            gtin = dados.gtin,
            rotulo = imagem,
            larguraRotuloMetros = largura,
            larguraTabelaMetros = largura * fatorTabelaEnsinada
        };
        return CriarMarcador(p, dados, NomeEnsinado(arquivo));
    }

    static string NomeEnsinado(string arquivo)
    {
        return "Ensinado_" + Path.GetFileNameWithoutExtension(arquivo);
    }

    void RemoverMarcador(string nomeMarcador)
    {
        var item = emCena.Find(i => i.nomeMarcador == nomeMarcador);
        if (item == null) return;
        emCena.Remove(item);
        if (rastreados.Remove(item)) AoMudarRastreamento?.Invoke();
        Destroy(item.target.gameObject);
        Destroy(item.produto.rotulo);
        Debug.Log("[RA] Marcador removido (vista mais antiga do produto): " + nomeMarcador);
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
        // Evento atrasado de um marcador que acabou de ser removido
        if (!emCena.Contains(item)) return;
        // No log, para medir se o rotulo fica estavel ou entra e sai da camera
        Debug.Log($"[RA] Rotulo {(rastreado ? "na camera" : "saiu da camera")}: {item.nomeMarcador}");
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
