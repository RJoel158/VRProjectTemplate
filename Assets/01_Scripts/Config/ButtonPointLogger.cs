using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonPointerLogger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData e) => Debug.Log($"[POINTER] Encima de: {name}");
    public void OnPointerExit(PointerEventData e) => Debug.Log($"[POINTER] Saliste de: {name}");
}