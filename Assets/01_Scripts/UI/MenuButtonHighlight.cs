using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[RequireComponent(typeof(Button))]
public class MenuButtonHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Referencias")]
    public GameObject borderHighlight; // Hijo con la Image del borde celeste

    [Header("Color del borde (celeste tipo Wii)")]
    public Color highlightColor = new Color(0.42f, 0.82f, 1f); // #6BD1FF aprox

    [Header("Animación")]
    public bool pulse = true;
    public float pulseSpeed = 2f;
    public float pulseScaleAmount = 0.03f;

    private Image borderImage;
    private Vector3 baseScale;
    private bool isHighlighted;

    void Awake()
    {
        if (borderHighlight != null)
        {
            borderImage = borderHighlight.GetComponent<Image>();
            if (borderImage != null)
                borderImage.color = highlightColor;

            baseScale = borderHighlight.transform.localScale;
            borderHighlight.SetActive(false);
        }
    }

    void Update()
    {
        if (isHighlighted && pulse && borderHighlight != null)
        {
            float scale = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseScaleAmount;
            borderHighlight.transform.localScale = baseScale * scale;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => SetHighlight(true);
    public void OnPointerExit(PointerEventData eventData) => SetHighlight(false);
    public void OnSelect(BaseEventData eventData) => SetHighlight(true);
    public void OnDeselect(BaseEventData eventData) => SetHighlight(false);

    private void SetHighlight(bool state)
    {
        if (state && !isHighlighted)
        {
            if (MainMenuManager.Instance != null)
            {
                MainMenuManager.Instance.PlayHoverSound();
            }
        }

        isHighlighted = state;
        if (borderHighlight != null)
        {
            borderHighlight.SetActive(state);
            if (!state) borderHighlight.transform.localScale = baseScale;
        }
    }
}