using NUnit.Framework;

// Testes do comando que faz o Editor do Windows falar. O texto (com nomes de produtos que vem do Open Food Facts,
// editavel por qualquer pessoa) vai por variavel de ambiente e nunca entra no comando do PowerShell.
public class TestesLeitorVoz
{
    [Test]
    public void ComandoLeOTextoDaVariavelDeAmbiente()
    {
        StringAssert.Contains("$v.Speak($env:" + LeitorVoz.VariavelTexto + ")", LeitorVoz.ComandoPowerShell);
        StringAssert.DoesNotContain("\"", LeitorVoz.ComandoPowerShell, "aspas duplas fechariam o argumento do processo");
    }

    [Test]
    public void PrefereUmaVozEmPortugues()
    {
        StringAssert.Contains("'pt-BR'", LeitorVoz.ComandoPowerShell);
    }

#if UNITY_EDITOR_WIN
    [Test]
    public void TextoComAspasNaoEntraNoComando()
    {
        // O PowerShell trata as aspas tipograficas como aspas simples: dentro do comando, este nome rodaria o calc
        const string nome = "x’); Start-Process calc; (’y \"z\" 'w'";
        var info = LeitorVoz.InfoProcessoVoz(nome);

        StringAssert.DoesNotContain("calc", info.Arguments);
        Assert.AreEqual(nome, info.EnvironmentVariables[LeitorVoz.VariavelTexto]);
        Assert.IsFalse(info.UseShellExecute);
    }
#endif
}
