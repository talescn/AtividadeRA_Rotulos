using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Confere se a cena montada pelo menu tem tudo o que a RA, o zoom e o modo leitura precisam.
public class TestesCenaAtividade
{
    const string CenaAtividade = "Assets/Scenes/AtividadeRA_Rotulos.unity";

    Scene cena;
    bool fecharNoFim;

    [OneTimeSetUp]
    public void AbrirCena()
    {
        // Se a cena ja estiver aberta no Editor, usa a aberta e nao fecha no fim
        cena = SceneManager.GetSceneByPath(CenaAtividade);
        fecharNoFim = !cena.isLoaded;
        if (fecharNoFim)
            cena = EditorSceneManager.OpenScene(CenaAtividade, OpenSceneMode.Additive);
    }

    [OneTimeTearDown]
    public void FecharCena()
    {
        if (fecharNoFim) EditorSceneManager.CloseScene(cena, true);
    }

    T Buscar<T>() where T : Component
    {
        foreach (var raiz in cena.GetRootGameObjects())
        {
            var componente = raiz.GetComponentInChildren<T>(true);
            if (componente != null) return componente;
        }
        return null;
    }

    [Test]
    public void TemACameraDoVuforia()
    {
        Assert.IsNotNull(Buscar<Vuforia.VuforiaBehaviour>(), "falta a ARCamera do Vuforia");
    }

    [Test]
    public void TemOsTresProdutosComFotosELarguras()
    {
        var gerenciador = Buscar<GerenciadorRotulosRA>();
        Assert.IsNotNull(gerenciador);
        Assert.AreEqual(3, gerenciador.produtos.Count);
        foreach (var p in gerenciador.produtos)
        {
            Assert.IsNotNull(p.rotulo, p.nome + ": sem foto do rotulo");
            Assert.IsNotNull(p.tabelaNutricional, p.nome + ": sem foto da tabela");
            Assert.Greater(p.larguraRotuloMetros, 0f, p.nome);
            Assert.Greater(p.larguraTabelaMetros, 0f, p.nome);
        }
    }

    [Test]
    public void ProdutosComGtinTemJsonLocal()
    {
        var gerenciador = Buscar<GerenciadorRotulosRA>();
        int comGtin = 0;
        foreach (var p in gerenciador.produtos)
        {
            if (string.IsNullOrEmpty(p.gtin)) continue;
            comGtin++;
            Assert.IsNotNull(RepositorioProdutos.CarregarLocal(p.gtin), $"{p.nome}: falta Resources/Produtos/{p.gtin}.json");
        }
        // Sprite e Ninho; o Maggi espera o GTIN conferido na embalagem
        Assert.GreaterOrEqual(comGtin, 2);
    }

    [Test]
    public void EventSystemUsaOInputSystemNovo()
    {
        // O projeto so tem o Input System novo: o StandaloneInputModule daria erro
        var eventSystem = Buscar<EventSystem>();
        Assert.IsNotNull(eventSystem, "falta o EventSystem");
        Assert.IsNotNull(eventSystem.GetComponent<InputSystemUIInputModule>());
        Assert.IsNull(eventSystem.GetComponent<StandaloneInputModule>());
    }

    [Test]
    public void InterfaceEstaLigadaAoGerenciador()
    {
        var ui = Buscar<InterfaceRotulosRA>();
        Assert.IsNotNull(ui, "falta o InterfaceRotulosRA");
        Assert.AreSame(Buscar<GerenciadorRotulosRA>(), ui.gerenciador);
        Assert.IsNotNull(ui.controleZoom);
        Assert.IsNotNull(ui.textoZoom);
        Assert.IsNotNull(ui.botaoLer);
        Assert.IsNotNull(ui.painelLeitura);
        Assert.IsNotNull(ui.visorLeitura);
        Assert.IsNotNull(ui.tabelaLeitura);
        Assert.IsNotNull(ui.tituloLeitura);
        Assert.IsNotNull(ui.botaoFechar);
        Assert.IsTrue(ui.tabelaLeitura.transform.IsChildOf(ui.visorLeitura), "a tabela da leitura precisa ficar dentro do visor");
        Assert.IsNotNull(ui.GetComponentInParent<Canvas>());
    }

    [Test]
    public void ControleDeZoomVaiDeMeioATres()
    {
        var controle = Buscar<InterfaceRotulosRA>().controleZoom;
        Assert.AreEqual(CalculosGestos.ZoomMinimo, controle.minValue);
        Assert.AreEqual(CalculosGestos.ZoomMaximo, controle.maxValue);
        Assert.AreEqual(1f, controle.value);
    }

    [Test]
    public void ModoLeituraEBotaoLerComecamEscondidos()
    {
        var ui = Buscar<InterfaceRotulosRA>();
        Assert.IsFalse(ui.painelLeitura.activeSelf);
        Assert.IsFalse(ui.botaoLer.gameObject.activeSelf);
    }

    [Test]
    public void LeitorDeCodigoDeBarrasEstaLigadoAInterface()
    {
        var ui = Buscar<InterfaceRotulosRA>();
        Assert.IsNotNull(ui.leitorCodigo, "falta o LeitorCodigoBarras");
        Assert.IsNotNull(ui.faixaAviso);
        Assert.IsNotNull(ui.textoAviso);
        Assert.IsTrue(ui.textoAviso.transform.IsChildOf(ui.faixaAviso.transform));
        Assert.IsFalse(ui.faixaAviso.activeSelf, "a faixa so aparece quando ha dica ou aviso");
    }
}
