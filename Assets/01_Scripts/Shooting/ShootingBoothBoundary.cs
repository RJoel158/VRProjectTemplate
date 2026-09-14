using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ShootingBoothBoundary : MonoBehaviour
{
    [Header("Configuración del Puesto")]
    public string playerTag = "Player";
    public MeshRenderer boothIndicatorRenderer;
    public Color normalColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
    public Color warningColor = new Color(0.9f, 0.2f, 0.2f, 0.5f);

    private Material indicatorMat;

    private void Awake()
    {
        if (boothIndicatorRenderer != null)
        {
            indicatorMat = boothIndicatorRenderer.material;
            indicatorMat.color = normalColor;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null)
        {
            if (indicatorMat != null) indicatorMat.color = warningColor;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) || other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null)
        {
            if (indicatorMat != null) indicatorMat.color = normalColor;
        }
    }
}
