using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Script temporal de diagnóstico. Poné esto en un botón para ver en la
/// Console si el rayo del control lo está tocando (hover) y si el click
/// realmente se registra. Sacalo después de resolver el problema.
/// </summary>
public class ButtonDebugLogger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log($"[ButtonDebug] HOVER ENTER en '{gameObject.name}'");
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log($"[ButtonDebug] HOVER EXIT en '{gameObject.name}'");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log($"[ButtonDebug] POINTER DOWN en '{gameObject.name}'");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log($"[ButtonDebug] POINTER UP en '{gameObject.name}'");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[ButtonDebug] CLICK detectado en '{gameObject.name}'");
    }
}