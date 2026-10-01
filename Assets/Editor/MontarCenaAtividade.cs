using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Configura automaticamente a cena da Atividade 01 (3 rotulos -> 3 tabelas nutricionais).
[InitializeOnLoad]
public static class MontarCenaAtividade
{
    const string PastaFotos = "Assets/Fotos";
    const string CenaAtividade = "Assets/Scenes/AtividadeRA_Rotulos.unity";

    static readonly string[] Nomes = { "Sprite", "LeiteNinho", "CremeCebolaMaggi" };
    static readonly float[] Larguras = { 0.06f, 0.095f, 0.115f };
    static readonly float[] LargurasTabela = { 0.16f, 0.09f, 0.11f };

    static MontarCenaAtividade()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(CenaAtividade) && File.Exists(PastaFotos + "/rotulo1.jpg"))
                Montar();
        };
    }

    [MenuItem("Atividade RA/Montar cena dos rotulos")]
    public static void Montar()
    {
        AssetDatabase.Refresh();
        for (int i = 1; i <= 3; i++)
        {
            AjustarImportacao($"{PastaFotos}/rotulo{i}.jpg");
            AjustarImportacao($"{PastaFotos}/tabela{i}.jpg");
        }

        var cena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // Troca a Main Camera padrao pela ARCamera do Vuforia
        var mainCam = GameObject.Find("Main Camera");
        if (mainCam != null) Object.DestroyImmediate(mainCam);
        if (!EditorApplication.ExecuteMenuItem("GameObject/Vuforia Engine/AR Camera"))
            Debug.LogError("[RA] Nao encontrei o menu GameObject/Vuforia Engine/AR Camera. O Vuforia esta instalado?");

        var ger = Object.FindAnyObjectByType<GerenciadorRotulosRA>();
        if (ger == null)
            ger = new GameObject("GerenciadorRotulosRA").AddComponent<GerenciadorRotulosRA>();

        ger.produtos.Clear();
        for (int i = 0; i < 3; i++)
        {
            ger.produtos.Add(new GerenciadorRotulosRA.Produto
            {
                nome = Nomes[i],
                rotulo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{PastaFotos}/rotulo{i + 1}.jpg"),
                tabelaNutricional = AssetDatabase.LoadAssetAtPath<Texture2D>($"{PastaFotos}/tabela{i + 1}.jpg"),
                larguraRotuloMetros = Larguras[i],
                larguraTabelaMetros = LargurasTabela[i]
            });
        }
        EditorUtility.SetDirty(ger);

        EditorSceneManager.MarkSceneDirty(cena);
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(cena, CenaAtividade);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(CenaAtividade, true) };
        Debug.Log("[RA] Cena da atividade montada: " + CenaAtividade);
    }

    static void AjustarImportacao(string caminho)
    {
        var imp = AssetImporter.GetAtPath(caminho) as TextureImporter;
        if (imp == null) { Debug.LogWarning("[RA] Imagem nao encontrada: " + caminho); return; }
        imp.textureType = TextureImporterType.Default;
        imp.isReadable = true;
        imp.mipmapEnabled = false;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.maxTextureSize = 2048;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.SaveAndReimport();
    }
}
