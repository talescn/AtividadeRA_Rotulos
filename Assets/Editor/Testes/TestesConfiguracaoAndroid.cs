using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.Rendering;

// Confere as opcoes de Android que o GerarBuildAndroid deixa salvas no ProjectSettings.
public class TestesConfiguracaoAndroid
{
    [Test]
    public void PacoteEDoProjeto()
    {
        // O template deixava com.UnityTechnologies.com.unity.template.urpblank
        Assert.AreEqual("com.talescn.rotulosra", PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
    }

    [Test]
    public void UsaIl2cppEArm64()
    {
        Assert.AreEqual(ScriptingImplementation.IL2CPP, PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android));
        Assert.AreEqual(AndroidArchitecture.ARM64, PlayerSettings.Android.targetArchitectures);
    }

    [Test]
    public void ApiMinimaEDoAndroid10()
    {
        // Vuforia 11.4 suporta Android 10.0+ (API 29)
        Assert.AreEqual(AndroidSdkVersions.AndroidApiLevel29, PlayerSettings.Android.minSdkVersion);
    }

    [Test]
    public void AbreEmRetrato()
    {
        Assert.AreEqual(UIOrientation.Portrait, PlayerSettings.defaultInterfaceOrientation);
    }

    [Test]
    public void UsaSoOpenGLES3()
    {
        Assert.IsFalse(PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.Android));
        CollectionAssert.AreEqual(new[] { GraphicsDeviceType.OpenGLES3 }, PlayerSettings.GetGraphicsAPIs(BuildTarget.Android));
    }

    [Test]
    public void UsaActivityClassica()
    {
        Assert.AreEqual(AndroidApplicationEntry.Activity, PlayerSettings.Android.applicationEntry);
    }
}
