using UnityEngine;

/// <summary>
/// Sincroniza el cuerpo 3D del avatar con la cámara del jugador (VR / FPS):
/// 1. Mantiene el cuerpo posicionado en el suelo justo debajo del visor/cámara.
/// 2. Rota el cuerpo con el giro horizontal (Yaw) de la cabeza, para que los brazos y el arma apunten a donde miras.
/// 3. Oculta la cabeza del modelo para evitar que los polígonos de la cara o el pelo tapen la vista en primera persona.
/// </summary>
public class FirstPersonVRBody : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Cámara del jugador (Main Camera)")]
    public Transform cameraTransform;

    [Tooltip("Hueso de la cabeza para ocultar en primera persona")]
    public Transform headBone;

    [Header("Ajustes de Posicionamiento")]
    [Tooltip("Offset respecto a la proyección vertical de la cámara")]
    public Vector3 offset = new Vector3(0f, 0f, -0.05f);

    [Tooltip("Velocidad de seguimiento de rotación del cuerpo")]
    public float turnSmoothSpeed = 15f;

    private void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Ocultar la cabeza en primera persona para que no obstruya los ojos de la cámara
        if (headBone != null)
        {
            headBone.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        }
        else
        {
            // Intentar buscar el hueso de la cabeza automáticamente
            Transform head = FindBone(transform, "Head");
            if (head != null)
            {
                headBone = head;
                headBone.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            }
        }
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Mantener la posición del avatar bajo la cámara con la base en el piso del XR Origin
        Vector3 groundPos = cameraTransform.position;
        if (transform.parent != null)
        {
            groundPos.y = transform.parent.position.y;
        }
        else
        {
            groundPos.y = 0f;
        }

        // Aplicar offset según la orientación del cuerpo
        Vector3 forwardFlat = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        if (forwardFlat.sqrMagnitude > 0.001f)
        {
            transform.position = groundPos + (forwardFlat * offset.z);
            
            // Rotación suave del cuerpo hacia donde mira la cámara
            Quaternion targetRot = Quaternion.LookRotation(forwardFlat, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSmoothSpeed);
        }
        else
        {
            transform.position = groundPos;
        }
    }

    private Transform FindBone(Transform current, string boneName)
    {
        if (current.name.IndexOf(boneName, System.StringComparison.OrdinalIgnoreCase) >= 0)
            return current;

        for (int i = 0; i < current.childCount; i++)
        {
            Transform found = FindBone(current.GetChild(i), boneName);
            if (found != null) return found;
        }
        return null;
    }
}
