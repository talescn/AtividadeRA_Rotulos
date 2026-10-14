using UnityEngine;

// Mantem os controles dentro da area segura da tela (fora do entalhe da camera e das barras do sistema).
[RequireComponent(typeof(RectTransform))]
public class AjustarAreaSegura : MonoBehaviour
{
    Rect ultimaArea;
    Vector2Int ultimaTela;

    void Update()
    {
        var tela = new Vector2Int(Screen.width, Screen.height);
        if (tela.x <= 0 || tela.y <= 0) return;
        if (Screen.safeArea == ultimaArea && tela == ultimaTela) return;

        ultimaArea = Screen.safeArea;
        ultimaTela = tela;

        var rt = (RectTransform)transform;
        rt.anchorMin = new Vector2(ultimaArea.xMin / tela.x, ultimaArea.yMin / tela.y);
        rt.anchorMax = new Vector2(ultimaArea.xMax / tela.x, ultimaArea.yMax / tela.y);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
