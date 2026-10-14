// Segura a tabela por um instante quando o rotulo sai do rastreio, para ela nao piscar. No diario de 14/10/2026,
// 31 das 62 saidas de um rotulo voltaram em ate 0,5 s (e so mais 5 entre 0,5 e 1 s): eram quedas do rastreio,
// nao o rotulo saindo da camera. Se o rotulo nao volta dentro da espera, a tabela some.
public class SegurarRastreio
{
    public enum Acao { Nenhuma, Mostrar, Esconder }

    readonly float espera;
    bool perdaPendente;
    float perdidoEm;

    public SegurarRastreio(float espera)
    {
        this.espera = espera;
    }

    public bool Visivel { get; private set; }

    // Quedas que voltaram dentro da espera desde que a tabela apareceu (vale ate a proxima vez que ela aparecer)
    public int QuedasCurtas { get; private set; }

    public Acao Encontrado(float agora)
    {
        if (perdaPendente)
        {
            perdaPendente = false;
            QuedasCurtas++;
            return Acao.Nenhuma;
        }
        if (Visivel) return Acao.Nenhuma;
        Visivel = true;
        QuedasCurtas = 0;
        return Acao.Mostrar;
    }

    // A espera conta da primeira perda; um aviso repetido nao a reinicia
    public void Perdido(float agora)
    {
        if (!Visivel || perdaPendente) return;
        perdaPendente = true;
        perdidoEm = agora;
    }

    // Chamado a cada quadro: diz quando a espera acabou sem o rotulo voltar
    public Acao Atualizar(float agora)
    {
        if (!perdaPendente || agora - perdidoEm < espera) return Acao.Nenhuma;
        perdaPendente = false;
        Visivel = false;
        return Acao.Esconder;
    }
}
