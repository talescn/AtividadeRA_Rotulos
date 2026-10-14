using System.IO;
using System.Text;
using System.Xml;
using UnityEditor.Android;

// Depois que a Unity gera o projeto Gradle do Android, acrescenta ao manifesto a consulta ao servico de voz.
// Desde o Android 11 um app so enxerga o mecanismo de TextToSpeech se declarar a acao TTS_SERVICE em <queries>;
// sem isso o LeitorVoz nao consegue iniciar a voz no celular.
public class AjustarManifestoAndroid : IPostGenerateGradleAndroidProject
{
    public const string AcaoVoz = "android.intent.action.TTS_SERVICE";
    const string NamespaceAndroid = "http://schemas.android.com/apk/res/android";

    public int callbackOrder => 0;

    // caminho: a pasta do modulo unityLibrary do projeto Gradle
    public void OnPostGenerateGradleAndroidProject(string caminho)
    {
        string manifesto = Path.Combine(caminho, "src", "main", "AndroidManifest.xml");
        File.WriteAllText(manifesto, AdicionarConsultaVoz(File.ReadAllText(manifesto)), new UTF8Encoding(false));
        UnityEngine.Debug.Log("[RA] Manifesto do Android com a consulta ao servico de voz: " + manifesto);
    }

    // Acrescenta <queries><intent><action android:name="android.intent.action.TTS_SERVICE" /></intent></queries>,
    // uma vez so
    public static string AdicionarConsultaVoz(string manifesto)
    {
        var doc = new XmlDocument();
        doc.LoadXml(manifesto);
        foreach (XmlElement acao in doc.GetElementsByTagName("action"))
            if (acao.GetAttribute("name", NamespaceAndroid) == AcaoVoz && acao.ParentNode?.ParentNode?.Name == "queries")
                return manifesto;

        var raiz = doc.DocumentElement;
        var consultas = raiz["queries"] ?? (XmlElement)raiz.AppendChild(doc.CreateElement("queries"));
        var intencao = consultas.AppendChild(doc.CreateElement("intent"));
        var acaoVoz = doc.CreateElement("action");
        var nome = doc.CreateAttribute("android", "name", NamespaceAndroid);
        nome.Value = AcaoVoz;
        acaoVoz.Attributes.Append(nome);
        intencao.AppendChild(acaoVoz);
        // OuterXml mantem a declaracao original (utf-8)
        return doc.OuterXml;
    }
}
