using UnityEngine;

// Pergunta ao Android o estado termico do aparelho (PowerManager.getCurrentThermalStatus, API 29, o minimo do app)
// e grava no diario quando ele muda. Criado pelo DiarioTestes, so no Android. Uma consulta a cada 10 s basta: o
// estado muda em minutos, e com o app em segundo plano a consulta para junto com ele.
public class MonitorTemperatura : MonoBehaviour
{
    const float IntervaloSegundos = 10f;

    readonly EstadoTermico estado = new EstadoTermico();
    AndroidJavaObject energia;
    float proximaMedida;

    public static void Criar()
    {
        var objeto = new GameObject("MonitorTemperatura");
        DontDestroyOnLoad(objeto);
        objeto.AddComponent<MonitorTemperatura>();
    }

    void Update()
    {
        if (Time.unscaledTime < proximaMedida) return;
        proximaMedida = Time.unscaledTime + IntervaloSegundos;
        try
        {
            string linha = estado.Registrar(GerenciadorEnergia().Call<int>("getCurrentThermalStatus"));
            if (linha != null) Debug.Log(linha);
        }
        catch (System.Exception e)
        {
            // Uma linha so e o monitor para: a medida nunca pode atrapalhar o app
            Debug.LogWarning(EstadoTermico.Indisponivel(e.Message));
            enabled = false;
        }
    }

    // O PowerManager do Android, pedido uma vez so
    AndroidJavaObject GerenciadorEnergia()
    {
        if (energia != null) return energia;
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            energia = activity.Call<AndroidJavaObject>("getSystemService", "power");
        if (energia == null) throw new System.InvalidOperationException("servico de energia ausente");
        return energia;
    }

    void OnDestroy()
    {
        energia?.Dispose();
    }
}
