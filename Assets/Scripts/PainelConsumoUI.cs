using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Painel do modo leitura: a quantidade que a pessoa vai consumir ([-] 200 ml [+]), os carboidratos nela e o
// botao Ouvir, que le o resumo do produto. So aparece para produto com dados. Nao calcula dose de insulina.
// Criado na cena pelo MontarInterfaceRA; o InterfaceRotulosRA chama Mostrar e Esconder.
public class PainelConsumoUI : MonoBehaviour
{
    public Button botaoMenos;
    public Button botaoMais;
    public TMP_Text textoQuantidade;
    public TMP_Text textoResultado;
    public Button botaoOuvir;
    public TMP_Text textoOuvir;
    [Tooltip("Fica fora do painel, no Canvas, para nao ser desligada junto com ele.")]
    public LeitorVoz voz;

    const string RotuloOuvir = "Ouvir";
    const string RotuloParar = "Parar";
    const string RotuloSemVoz = "Sem voz";
    // Quanto tempo o botao mostra "Sem voz" quando o aparelho nao consegue falar
    const float TempoSemVoz = 2.5f;

    DadosProduto dados;
    float quantidade;
    string unidade;
    float semVozAte;

    void Awake()
    {
        botaoMenos.onClick.AddListener(() => Mudar(-1));
        botaoMais.onClick.AddListener(() => Mudar(1));
        botaoOuvir.onClick.AddListener(AlternarVoz);
    }

    void Update()
    {
        string rotulo = voz != null && voz.Falando ? RotuloParar
            : Time.unscaledTime < semVozAte ? RotuloSemVoz : RotuloOuvir;
        if (textoOuvir.text != rotulo) textoOuvir.text = rotulo;
    }

    // Abre o painel com o produto da leitura, na quantidade da porcao; sem dados (so a foto da tabela) ele some
    public void Mostrar(DadosProduto produto)
    {
        if (produto == null)
        {
            Esconder();
            return;
        }
        dados = produto;
        unidade = CalculosPorcao.Unidade(produto);
        quantidade = CalculosPorcao.QuantidadeInicial(produto);
        gameObject.SetActive(true);
        Atualizar();
    }

    public void Esconder()
    {
        if (voz != null) voz.Parar();
        dados = null;
        gameObject.SetActive(false);
    }

    void Mudar(int sentido)
    {
        if (dados == null) return;
        quantidade = CalculosPorcao.Limitar(quantidade + sentido * CalculosPorcao.Passo(unidade));
        Atualizar();
    }

    void Atualizar()
    {
        textoQuantidade.text = "Vou consumir: " + CalculosPorcao.TextoQuantidade(quantidade, unidade);
        textoResultado.text = CalculosPorcao.TextoResultado(dados, quantidade);
    }

    void AlternarVoz()
    {
        if (dados == null || voz == null) return;
        if (voz.Falando) voz.Parar();
        else if (!voz.Falar(ResumoFalado.Montar(dados, quantidade))) semVozAte = Time.unscaledTime + TempoSemVoz;
    }
}
