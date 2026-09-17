using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity.XR.CoreUtils;

namespace Esgrima.Combat
{
    /// <summary>
    /// Vincula la espada directamente al mando derecho (Oculus Right Controller)
    /// estilo Wii Sports Resort. Garantiza orientación frontal fija, cero temblores
    /// y respuesta 1:1 absoluta sin depender de físicas erráticas ni recálculos en LateUpdate.
    /// </summary>
    [RequireComponent(typeof(VRSword))]
    [RequireComponent(typeof(Rigidbody))]
    public class SwordDirectBind : MonoBehaviour
    {
        [Header("Grip Positioning")]
        [Tooltip("Posición relativa de la empuñadura dentro de la palma del mando.")]
        [SerializeField] private Vector3 localGripPosition = new Vector3(0f, -0.04f, 0.08f);

        [Tooltip("Rotación Euler para orientar la hoja hacia adelante (+Z) con ligero ángulo superior (+Y).")]
        [SerializeField] private Vector3 bladeEulerRotation = new Vector3(75f, 0f, 0f);

        [Header("Target Hand Reference (Opcional, se auto-detecta si está vacío)")]
        [SerializeField] private Transform rightHandTarget;

        private Rigidbody rb;
        private Collider swordCollider;
        private bool isBound = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            swordCollider = GetComponent<Collider>();

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (swordCollider != null)
            {
                swordCollider.isTrigger = true;
            }
        }

        private void Start()
        {
            BindToRightHand();
        }

        private void LateUpdate()
        {
            // Solo re-vincular si aún no se ha enlazado o se perdió el parentesco.
            // NO sobreescribir transform cada frame para no interferir con el Late-Latching de XR (elimina temblores).
            if (!isBound || transform.parent == null || transform.parent != rightHandTarget)
            {
                BindToRightHand();
            }
        }

        /// <summary>
        /// Localiza el mando derecho del XR Origin y emparenta la espada como hijo directo.
        /// Al ser hijo del mando, Unity sincroniza posición y rotación automáticamente al 100%.
        /// </summary>
        public void BindToRightHand()
        {
            if (rightHandTarget == null)
            {
                rightHandTarget = FindRightHandTransform();
            }

            if (rightHandTarget != null)
            {
                transform.SetParent(rightHandTarget, false);
                transform.localPosition = localGripPosition;
                transform.localRotation = Quaternion.Euler(bladeEulerRotation);

                isBound = true;

                // Ignorar colisiones con el cuerpo del jugador para evitar cualquier interferencia
                if (swordCollider != null)
                {
                    Collider[] playerCols = rightHandTarget.root.GetComponentsInChildren<Collider>();
                    for (int i = 0; i < playerCols.Length; i++)
                    {
                        if (playerCols[i] != swordCollider)
                        {
                            Physics.IgnoreCollision(swordCollider, playerCols[i], true);
                        }
                    }
                }
            }
        }

        private Transform FindRightHandTransform()
        {
            // 1. Buscar controladores XR
            var controllers = Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.XRBaseController>(FindObjectsSortMode.None);
            foreach (var c in controllers)
            {
                string n = c.name.ToLower();
                if (n.Contains("right"))
                {
                    return c.transform;
                }
            }

            // 2. Buscar en XROrigin CameraFloorOffset
            var origin = Object.FindAnyObjectByType<XROrigin>();
            if (origin != null && origin.CameraFloorOffsetObject != null)
            {
                var children = origin.CameraFloorOffsetObject.GetComponentsInChildren<Transform>(true);
                foreach (var t in children)
                {
                    string lower = t.name.ToLower();
                    if (lower.Contains("right") && (lower.Contains("controller") || lower.Contains("hand")))
                    {
                        if (!lower.Contains("ray") && !lower.Contains("poke") && !lower.Contains("teleport") && !lower.Contains("visual"))
                        {
                            return t;
                        }
                    }
                }
            }

            // 3. Buscar interactores con "right"
            var interactors = Object.FindObjectsByType<XRBaseInputInteractor>(FindObjectsSortMode.None);
            foreach (var it in interactors)
            {
                string n = it.name.ToLower();
                if (n.Contains("right"))
                {
                    return it.transform;
                }
                if (it.transform.parent != null && it.transform.parent.name.ToLower().Contains("right"))
                {
                    return it.transform.parent;
                }
            }

            // 4. Buscar por nombre en la jerarquía
            var allGos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            Transform candidate = null;
            for (int i = 0; i < allGos.Length; i++)
            {
                string n = allGos[i].name.ToLower();
                if (n == "right hand" || n == "right controller" || n == "righthand controller" || n == "righthand")
                {
                    return allGos[i].transform;
                }
                if (n.Contains("right") && (n.Contains("hand") || n.Contains("controller")))
                {
                    if (!n.Contains("visual") && !n.Contains("callout") && !n.Contains("mesh"))
                    {
                        if (candidate == null) candidate = allGos[i].transform;
                    }
                }
            }

            return candidate;
        }
    }
}
