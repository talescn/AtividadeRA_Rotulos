using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

// Testes do diario de testes (as linhas [RA] do log guardadas no aparelho para o teste no mercado),
// gravando numa pasta temporaria.
public class TestesDiario
{
    static readonly DateTime Instante = new DateTime(2026, 10, 14, 9, 5, 3, 42);

    string pasta;

    [SetUp]
    public void CriarPasta()
    {
        pasta = Path.Combine(Path.GetTempPath(), "TestesDiario_" + Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void ApagarPasta()
    {
        if (Directory.Exists(pasta)) Directory.Delete(pasta, true);
    }

    string[] LinhasDoDia()
    {
        return File.ReadAllLines(Path.Combine(pasta, DiarioTestes.NomeArquivo(Instante)));
    }

    // ---------- Formato ----------

    [Test]
    public void LinhaTemHoraTipoEMensagemSeparadosPorTab()
    {
        Assert.AreEqual("2026-10-14 09:05:03.042\tLog\t[RA] Codigo de barras lido: 78938571",
            DiarioTestes.FormatarLinha(Instante, LogType.Log, "[RA] Codigo de barras lido: 78938571", ""));
    }

    [Test]
    public void LinhaAchataQuebrasETabs()
    {
        Assert.AreEqual("2026-10-14 09:05:03.042\tWarning\t[RA] a / b / c d",
            DiarioTestes.FormatarLinha(Instante, LogType.Warning, "[RA] a\r\nb\nc\td\n", ""));
    }

    [Test]
    public void ExcecaoLevaAPrimeiraLinhaDaPilha()
    {
        string pilha = "\nGerenciadorRotulosRA.CriarTargets () (at Assets/Scripts/GerenciadorRotulosRA.cs:96)\nOutra.Linha ()\n";
        Assert.AreEqual("2026-10-14 09:05:03.042\tException\tNullReferenceException: x | " +
                        "GerenciadorRotulosRA.CriarTargets () (at Assets/Scripts/GerenciadorRotulosRA.cs:96)",
            DiarioTestes.FormatarLinha(Instante, LogType.Exception, "NullReferenceException: x", pilha));
    }

    [Test]
    public void LogComumNaoLevaAPilha()
    {
        Assert.AreEqual("2026-10-14 09:05:03.042\tLog\t[RA] Leitura fechada",
            DiarioTestes.FormatarLinha(Instante, LogType.Log, "[RA] Leitura fechada", "Pilha.Qualquer ()\n"));
    }

    [Test]
    public void UmArquivoPorDia()
    {
        Assert.AreEqual("diario_20261014.txt", DiarioTestes.NomeArquivo(Instante));
    }

    // ---------- O que entra ----------

    [Test]
    public void SoGuardaAsLinhasDoProjetoEOsErros()
    {
        Assert.IsTrue(DiarioTestes.DeveRegistrar("[RA] Leitura fechada", LogType.Log));
        Assert.IsTrue(DiarioTestes.DeveRegistrar("[RA] Cache invalido para 789", LogType.Warning));
        Assert.IsTrue(DiarioTestes.DeveRegistrar("NullReferenceException: x", LogType.Exception));
        Assert.IsTrue(DiarioTestes.DeveRegistrar("Erro de outro sistema", LogType.Error));
        Assert.IsFalse(DiarioTestes.DeveRegistrar("Vuforia: mensagem comum", LogType.Log));
        Assert.IsFalse(DiarioTestes.DeveRegistrar("Aviso de outro sistema", LogType.Warning));
        Assert.IsFalse(DiarioTestes.DeveRegistrar(null, LogType.Log));
    }

    // ---------- Arquivo ----------

    [Test]
    public void RegistrarAcrescentaAsLinhasNoArquivoDoDia()
    {
        var diario = new DiarioTestes(pasta);
        diario.Registrar("[RA] Codigo de barras lido: 78938571", "", LogType.Log, Instante);
        diario.Registrar("Mensagem de outro sistema", "", LogType.Log, Instante);
        diario.Registrar("[RA] Leitura fechada", "", LogType.Log, Instante.AddSeconds(5));

        string[] linhas = LinhasDoDia();
        Assert.AreEqual(2, linhas.Length);
        StringAssert.EndsWith("[RA] Codigo de barras lido: 78938571", linhas[0]);
        StringAssert.StartsWith("2026-10-14 09:05:08.042", linhas[1]);
    }

    [Test]
    public void ArquivoCheioParaDeCrescerEAvisaUmaVez()
    {
        // Cada linha tem uns 70 bytes: so a primeira cabe em 100
        var diario = new DiarioTestes(pasta, 100);
        for (int i = 0; i < 5; i++)
            diario.Registrar("[RA] Leitor: 15" + i + " quadros, 95 ms em media", "", LogType.Log, Instante);

        string[] linhas = LinhasDoDia();
        Assert.AreEqual(2, linhas.Length);
        StringAssert.EndsWith("[RA] Leitor: 150 quadros, 95 ms em media", linhas[0]);
        StringAssert.Contains("[RA] Diario cheio", linhas[1]);
        Assert.IsFalse(diario.Desligado);
    }

    [Test]
    public void LinhaRepetidaEmSeguidaViraUmaSoComAContagem()
    {
        // Um erro a cada quadro nao pode encher o arquivo do dia
        var diario = new DiarioTestes(pasta);
        for (int i = 0; i < 4; i++)
            diario.Registrar("Erro a cada quadro", "", LogType.Error, Instante);
        diario.Registrar("[RA] Leitura fechada", "", LogType.Log, Instante);

        string[] linhas = LinhasDoDia();
        Assert.AreEqual(3, linhas.Length);
        StringAssert.EndsWith("Erro a cada quadro", linhas[0]);
        StringAssert.EndsWith("[RA] A linha anterior se repetiu mais 3 vezes", linhas[1]);
        StringAssert.EndsWith("[RA] Leitura fechada", linhas[2]);
    }

    [Test]
    public void PastaApagadaComOAppAbertoVoltaASerCriada()
    {
        var diario = new DiarioTestes(pasta);
        diario.Registrar("[RA] Antes", "", LogType.Log, Instante);
        Directory.Delete(pasta, true);
        diario.Registrar("[RA] Depois", "", LogType.Log, Instante);

        Assert.IsFalse(diario.Desligado);
        string[] linhas = LinhasDoDia();
        Assert.AreEqual(1, linhas.Length);
        StringAssert.EndsWith("[RA] Depois", linhas[0]);
    }

    [Test]
    public void ErroDeDiscoRepetidoDesligaODiarioSemExcecao()
    {
        // Uma pasta "dentro" de um arquivo nao pode ser criada
        Directory.CreateDirectory(pasta);
        string arquivo = Path.Combine(pasta, "arquivo.txt");
        File.WriteAllText(arquivo, "x");
        var diario = new DiarioTestes(Path.Combine(arquivo, "Diario"));

        Assert.DoesNotThrow(() => diario.Registrar("[RA] Um", "", LogType.Log, Instante));
        Assert.IsFalse(diario.Desligado, "uma falha so pode ser passageira");
        diario.Registrar("[RA] Dois", "", LogType.Log, Instante);
        diario.Registrar("[RA] Tres", "", LogType.Log, Instante);
        Assert.IsTrue(diario.Desligado);
        Assert.IsNotEmpty(diario.MotivoDesligado);
    }
}
