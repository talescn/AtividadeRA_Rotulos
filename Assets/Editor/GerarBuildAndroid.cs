using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

// Configura o projeto para Android e gera o APK em Builds/ (pasta fora do Git).
// Pela linha de comando: -executeMethod GerarBuildAndroid.GerarBuildBatch
public static class GerarBuildAndroid
{
    public const string Pacote = "com.talescn.rotulosra";
    // Vuforia 11.4: Android 10.0+ (API 29), segundo a pagina "Supported Versions" da documentacao
    public const AndroidSdkVersions ApiMinima = AndroidSdkVersions.AndroidApiLevel29;
    public const string CaminhoApk = "Builds/AtividadeRA_Rotulos.apk";
    const string CenaAtividade = "Assets/Scenes/AtividadeRA_Rotulos.unity";

    [MenuItem("Atividade RA/Gerar build Android")]
    public static void GerarBuild()
    {
        if (Gerar())
            EditorUtility.RevealInFinder(CaminhoApk);
    }

    public static void GerarBuildBatch()
    {
        if (!Gerar())
            EditorApplication.Exit(1);
    }

    // Deixa salvas no ProjectSettings as opcoes de Android que o app precisa
    public static void AplicarConfiguracoes()
    {
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, Pacote);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = ApiMinima;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        // O Vuforia suporta OpenGL ES 3; o Vulkan e so experimental no Unity
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        // Activity classica: e o ponto de entrada que o Vuforia sempre suportou
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
        AssetDatabase.SaveAssets();
    }

    static bool Gerar()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            Debug.LogError("[RA] Android Build Support nao esta instalado. Instale pelo Unity Hub " +
                           "(Installs > Add modules > Android Build Support, com OpenJDK e Android SDK & NDK Tools).");
            return false;
        }

        AplicarConfiguracoes();
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        // APK para instalar direto no celular (o AAB e so para a Play Store)
        EditorUserBuildSettings.buildAppBundle = false;

        Directory.CreateDirectory(Path.GetDirectoryName(CaminhoApk));
        var opcoes = new BuildPlayerOptions
        {
            scenes = new[] { CenaAtividade },
            locationPathName = CaminhoApk,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None
        };

        BuildReport relatorio = BuildPipeline.BuildPlayer(opcoes);
        BuildSummary resumo = relatorio.summary;
        if (resumo.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[RA] Build Android falhou: {resumo.result}, {resumo.totalErrors} erro(s).");
            return false;
        }

        // O totalSize do relatorio nao e o tamanho do APK: mede o arquivo gerado
        long bytesApk = new FileInfo(CaminhoApk).Length;
        Debug.Log($"[RA] APK gerado: {CaminhoApk} ({bytesApk / (1024f * 1024f):0.0} MB)");
        return true;
    }
}
