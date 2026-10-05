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
    // GTINs conferidos na embalagem. O do Maggi ainda falta: sem ele, o Maggi mostra a foto da tabela.
    static readonly string[] Gtins = { "78939745", "7898215157403", "" };
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
        if (!GarantirRecursosTMP())
        {
            Debug.LogWarning("[RA] Importando os recursos do TextMeshPro. Rode o menu de novo quando a importacao terminar.");
            return;
        }
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
                gtin = Gtins[i],
                rotulo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{PastaFotos}/rotulo{i + 1}.jpg"),
                tabelaNutricional = AssetDatabase.LoadAssetAtPath<Texture2D>($"{PastaFotos}/tabela{i + 1}.jpg"),
                larguraRotuloMetros = Larguras[i],
                larguraTabelaMetros = LargurasTabela[i]
            });
        }
        EditorUtility.SetDirty(ger);

        // Zoom, botao "Ler" e modo leitura
        MontarInterfaceRA.Montar(ger);

        EditorSceneManager.MarkSceneDirty(cena);
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(cena, CenaAtividade);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(CenaAtividade, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[RA] Cena da atividade montada: " + CenaAtividade);
    }

    // O TextMeshPro precisa dos recursos essenciais (fonte padrao e TMP Settings) dentro de Assets.
    // Devolve true se ja estao no projeto; senao comeca a importacao, que termina em segundo plano.
    public static bool GarantirRecursosTMP()
    {
        if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return true;
        var ugui = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.ugui/package.json");
        UnityEditor.AssetPackage.Package.Import(ugui.resolvedPath + "/Package Resources/TMP Essential Resources.unitypackage", false);
        return false;
    }

    // Pela linha de comando (sem -quit): importa os recursos do TextMeshPro e fecha o Unity quando terminar
    public static void ImportarRecursosTMPBatch()
    {
        AssetDatabase.importPackageCompleted += pacote =>
        {
            Debug.Log("[RA] Recursos essenciais do TextMeshPro importados.");
            EditorApplication.Exit(0);
        };
        AssetDatabase.importPackageFailed += (pacote, erro) =>
        {
            Debug.LogError("[RA] Falha ao importar os recursos do TextMeshPro: " + erro);
            EditorApplication.Exit(1);
        };
        if (GarantirRecursosTMP())
        {
            Debug.Log("[RA] Recursos essenciais do TextMeshPro ja estavam no projeto.");
            EditorApplication.Exit(0);
        }
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
