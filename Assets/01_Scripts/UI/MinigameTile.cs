using UnityEngine;
using UnityEngine.UI;


[ExecuteAlways]
public class MinigameTile : MonoBehaviour
{
    [Header("El deporte que representa este tile")]
    public MinigameData data;

    [Header("Referencias visuales (arrastra los hijos del boton aca)")]
    public Image artworkImage; // La Image hija que ocupa todo el tile
    public Text label;         // El Text hijo con el nombre (opcional)

    void Start()
    {
        ApplyVisuals();
    }

    void OnValidate()
    {
        ApplyVisuals();
    }

    public void ApplyVisuals()
    {
        if (data == null) return;

        if (artworkImage != null && data.icon != null)
            artworkImage.sprite = data.icon;

        if (label != null)
            label.text = data.sportName;
    }
}