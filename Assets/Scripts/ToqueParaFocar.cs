using UnityEngine;
using UnityEngine.EventSystems;

// Fundo transparente da tela de captura: um toque pede foco da camera naquele ponto
public class ToqueParaFocar : MonoBehaviour, IPointerClickHandler
{
    public ControleCamera controleCamera;

    public void OnPointerClick(PointerEventData evento)
    {
        if (controleCamera != null) controleCamera.FocarEm(evento.position);
    }
}
