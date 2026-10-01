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

    public List<Produto> produtos = new List<Produto>();

    [Tooltip("Distancia (m) da tabela a frente da superficie do rotulo.")]
    public float distanciaFrente = 0.01f;

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

            CriarTabela(target.transform, p);
            Debug.Log($"[RA] Marcador criado: {p.nome}");
        }
    }

    void CriarTabela(Transform pai, Produto p)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "TabelaNutricional_" + p.nome;
        Destroy(quad.GetComponent<Collider>());

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
    }

    static Texture2D CopiarRGBA32(Texture2D origem)
    {
        var copia = new Texture2D(origem.width, origem.height, TextureFormat.RGBA32, false);
        copia.SetPixels32(origem.GetPixels32());
        copia.Apply();
        return copia;
    }
}
