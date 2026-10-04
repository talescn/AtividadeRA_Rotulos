using System.Collections.Generic;
using UnityEngine;
using Vuforia;

// Atividade 01 - Realidade Aumentada Marker Based
// Cria em tempo de execucao 3 Image Targets (um para cada rotulo de produto)
// e, quando o rotulo e reconhecido, mostra a tabela nutricional do produto na frente dele.
public class GerenciadorRotulosRA : MonoBehaviour
{
    [System.Serializable]
    public class Produto
    {
        public string nome = "Produto";
        [Tooltip("Foto do rotulo (marcador). Precisa estar com Read/Write habilitado.")]
        public Texture2D rotulo;
        [Tooltip("Foto da tabela nutricional do proprio produto.")]
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
        public Transform tabela;
        public Vector3 escalaBase;
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

    // Ultimo produto que entrou na camera e ainda esta visivel (null se nenhum)
    public Produto ProdutoEmFoco => rastreados.Count > 0 ? rastreados[rastreados.Count - 1].produto : null;

    void Start()
    {
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
            if (p.rotulo == null || p.tabelaNutricional == null)
            {
                Debug.LogWarning($"[RA] Produto '{p.nome}' sem imagem de rotulo ou tabela.");
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

            var item = new ProdutoEmCena { produto = p, tabela = CriarTabela(target.transform, p) };
            item.escalaBase = item.tabela.localScale;
            item.tabela.localScale = CalculosGestos.EscalaTabela(item.escalaBase, Zoom);
            emCena.Add(item);

            // Os mesmos eventos que ligam e desligam a tabela dizem a interface o que esta visivel
            handler.OnTargetFound.AddListener(() => MarcarRastreado(item, true));
            handler.OnTargetLost.AddListener(() => MarcarRastreado(item, false));
            Debug.Log($"[RA] Marcador criado: {p.nome}");
        }
    }

    Transform CriarTabela(Transform pai, Produto p)
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
            item.tabela.localScale = CalculosGestos.EscalaTabela(item.escalaBase, Zoom);
    }

    // Produto dono da tabela tocada (null se o objeto nao for uma tabela)
    public Produto ProdutoDaTabela(Transform tabela)
    {
        foreach (var item in emCena)
            if (item.tabela == tabela) return item.produto;
        return null;
    }

    void MarcarRastreado(ProdutoEmCena item, bool rastreado)
    {
        rastreados.Remove(item);
        if (rastreado) rastreados.Add(item);
        AoMudarRastreamento?.Invoke();
    }

    static Texture2D CopiarRGBA32(Texture2D origem)
    {
        var copia = new Texture2D(origem.width, origem.height, TextureFormat.RGBA32, false);
        copia.SetPixels32(origem.GetPixels32());
        copia.Apply();
        return copia;
    }
}
