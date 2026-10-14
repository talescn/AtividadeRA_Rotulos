using UnityEngine;

// O DefaultObserverEventHandler do Vuforia (liga e desliga a tabela com o rastreio do rotulo), com uma espera antes
// de esconder: uma queda curta do rastreio nao faz a tabela piscar (SegurarRastreio). Os eventos OnTargetFound e
// OnTargetLost tambem so saem quando a tabela aparece ou some de verdade.
public class ObservadorRotulo : DefaultObserverEventHandler
{
    // Tirada do diario de 14/10/2026: metade das saidas do rastreio voltou em ate meio segundo
    const float EsperaPerda = 0.5f;

    readonly SegurarRastreio segurar = new SegurarRastreio(EsperaPerda);

    // Quedas curtas desde que a tabela apareceu, para o diario
    public int QuedasCurtas => segurar.QuedasCurtas;

    protected override void OnTrackingFound()
    {
        if (segurar.Encontrado(Time.unscaledTime) == SegurarRastreio.Acao.Mostrar) base.OnTrackingFound();
    }

    protected override void OnTrackingLost()
    {
        // Antes de aparecer pela primeira vez (o Vuforia esconde tudo no primeiro aviso) nao ha o que segurar
        if (!segurar.Visivel)
        {
            base.OnTrackingLost();
            return;
        }
        segurar.Perdido(Time.unscaledTime);
    }

    void Update()
    {
        if (segurar.Atualizar(Time.unscaledTime) == SegurarRastreio.Acao.Esconder) base.OnTrackingLost();
    }
}
