using System.IO;
using UnityEditor;
using UnityEngine;

// Gera um PNG de cada tabela desenhada a partir dos dados (em Builds/Previas, fora do Git),
// para conferir o layout sem camera nem celular.
// Pela linha de comando (sem -nographics): -executeMethod GerarPreviaTabelas.Gerar -quit
public static class GerarPreviaTabelas
{
    const string Pasta = "Builds/Previas";
    const int Folga = 40;

    [MenuItem("Atividade RA/Gerar previa das tabelas")]
    public static void Gerar()
    {
        Directory.CreateDirectory(Pasta);
        foreach (var arquivo in Resources.LoadAll<TextAsset>(RepositorioProdutos.PastaRecursos))
        {
            string caminho = Path.Combine(Pasta, arquivo.name + ".png");
            Salvar(RepositorioProdutos.Ler(arquivo.text), caminho);
            Debug.Log("[RA] Previa gerada: " + caminho);
        }
    }

    static void Salvar(DadosProduto dados, string caminho)
    {
        // Camera e Canvas temporarios; nada fica na cena
        var camera = new GameObject("PreviaCamera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
        camera.orthographic = true;

        var canvas = new GameObject("PreviaCanvas", typeof(Canvas)).GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;
        var tabela = TabelaNutricionalUI.Montar(canvas.transform, dados);

        int largura = Mathf.CeilToInt(tabela.sizeDelta.x) + 2 * Folga;
        int altura = Mathf.CeilToInt(tabela.sizeDelta.y) + 2 * Folga;
        var alvo = new RenderTexture(largura, altura, 24);
        camera.targetTexture = alvo;
        Canvas.ForceUpdateCanvases();
        camera.Render();

        var anterior = RenderTexture.active;
        RenderTexture.active = alvo;
        var imagem = new Texture2D(largura, altura, TextureFormat.RGB24, false);
        imagem.ReadPixels(new Rect(0, 0, largura, altura), 0, 0);
        imagem.Apply();
        RenderTexture.active = anterior;
        File.WriteAllBytes(caminho, imagem.EncodeToPNG());

        camera.targetTexture = null;
        Object.DestroyImmediate(imagem);
        Object.DestroyImmediate(alvo);
        Object.DestroyImmediate(canvas.gameObject);
        Object.DestroyImmediate(camera.gameObject);
    }
}
