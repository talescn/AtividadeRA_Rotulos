using UnityEngine;
using UnityEngine.EventSystems;

// Alca do retangulo de recorte: arrastar um canto muda o tamanho; arrastar o meio move o retangulo inteiro
public class AlcaRecorte : MonoBehaviour, IDragHandler
{
    public EnsinarRotuloUI ensinar;

    [Tooltip("0 = inferior esquerdo, 1 = superior esquerdo, 2 = superior direito, 3 = inferior direito, -1 = mover tudo")]
    public int canto = -1;

    public void OnDrag(PointerEventData evento)
    {
        if (ensinar != null) ensinar.Arrastar(canto, evento.delta);
    }
}
