using NUnit.Framework;

// Testes do comando que faz o Editor do Windows falar: o texto entra numa string do PowerShell, entao
// aspas no nome de um produto (vindo do Open Food Facts, por exemplo) nao podem sair dela.
public class TestesLeitorVoz
{
    [Test]
    public void AspasDoTextoNaoSaemDaStringDoPowerShell()
    {
        string comando = LeitorVoz.ComandoPowerShell("Biscoito \"Maria\" d'Ouro'; Remove-Item x");

        StringAssert.DoesNotContain("\"", comando, "aspas duplas fechariam o argumento do processo");
        StringAssert.Contains("$v.Speak('Biscoito Maria d''Ouro''; Remove-Item x')", comando);
    }

    [Test]
    public void QuebraDeLinhaViraEspaco()
    {
        StringAssert.Contains("$v.Speak('linha um linha dois')", LeitorVoz.ComandoPowerShell("linha um\nlinha dois"));
    }

    [Test]
    public void PrefereUmaVozEmPortugues()
    {
        StringAssert.Contains("'pt-BR'", LeitorVoz.ComandoPowerShell("Sprite"));
    }
}
