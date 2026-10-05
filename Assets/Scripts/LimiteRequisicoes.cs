using System.Collections.Generic;

// Janela deslizante: no maximo N requisicoes a cada X segundos.
// O Open Food Facts limita a leitura de produtos a 15 por minuto por IP.
public class LimiteRequisicoes
{
    readonly int maximo;
    readonly float janelaSegundos;
    readonly Queue<float> instantes = new Queue<float>();

    public LimiteRequisicoes(int maximo, float janelaSegundos)
    {
        this.maximo = maximo;
        this.janelaSegundos = janelaSegundos;
    }

    // Registra e devolve true se ainda cabe uma requisicao agora; senao devolve false
    public bool TentarRegistrar(float agora)
    {
        while (instantes.Count > 0 && agora - instantes.Peek() >= janelaSegundos)
            instantes.Dequeue();

        if (instantes.Count >= maximo) return false;
        instantes.Enqueue(agora);
        return true;
    }
}
