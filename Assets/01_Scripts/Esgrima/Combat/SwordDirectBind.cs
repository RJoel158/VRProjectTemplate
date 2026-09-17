using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity.XR.CoreUtils;

namespace Esgrima.Combat
{
    /// <summary>
    /// Vincula la espada directamente al mando derecho (Oculus Right Controller)
    /// estilo Wii Sports Resort. Garantiza orientación frontal perfecta, cero temblores
    /// y respuesta 1:1 absoluta sin depender de físicas erráticas.
    /// </summary>
    [RequireComponent(typeof(VRSword))]
    [RequireComponent(typeof(Rigidbody))]
    public class SwordDirectBind : MonoBehaviour
    {
        [Header("Grip Positioning")]
        [Tooltip("Posición relativa de la empuñadura dentro de la palma del mando.")]
        [SerializeField] private Vector3 localGripPosition = new Vector3(0.01f, -0.04f, 0.08f);

        [Tooltip("Vector de inclinación de la hoja: hacia adelante (+Z) con ligero ángulo hacia arriba (+Y).")]
        [SerializeField] private Vector3 bladeForwardAim = new Vector3(0f, 0.22f, 0.97f);

        [Header("Target Hand Reference (Opcional, se auto-detecta si está vacío)")]
        [SerializeField] private Transform rightHandTarget;

        private Rigidbody rb;
        private Collider swordCollider;
        private XRGrabInteractable grabInteractable;
        private bool isBound = false;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            swordCollider = GetComponent<Collider>();
            grabInteractable = GetComponent<XRGrabInteractable>();

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (swordCollider != null)
            {
                swordCollider.isTrigger = true;
            }

            // Si tiene XRGrabInteractable, configurarlo para agarre fijo permanente (sticky)
            if (grabInteractable != null)
            {
                grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
                grabInteractable.throwOnDetach = false;
                grabInteractable.trackPosition = true;
                grabInteractable.trackRotation = true;
                grabInteractable.selectEntered.AddListener(OnGrabSelected);
            }
        }

        private void Start()
        {
            BindToRightHand();
        }

        private void LateUpdate()
        {
            // Re-vincular si aún no se ha enlazado o si el mando se inicializó después del frame 1
            if (!isBound || transform.parent == null)
            {
                BindToRightHand();
            }
            else if (rightHandTarget != null && transform.parent == rightHandTarget)
            {
                // Mantener fijación firme en mano
                transform.localPosition = localGripPosition;
                Vector3 targetAim = bladeForwardAim.sqrMagnitude > 0.001f ? bladeForwardAim.normalized : Vector3.forward;
                transform.localRotation = Quaternion.FromToRotation(Vector3.up, targetAim);
            }
        }

        private void OnGrabSelected(SelectEnterEventArgs args)
        {
            if (args.interactorObject != null)
            {
                rightHandTarget = args.interactorObject.transform;
                BindToRightHand();
            }
        }

        /// <summary>
        /// Localiza el mando derecho del XR Origin y emparenta la espada directamente a él.
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

                // Orientar matemáticamente la hoja (Vector3.up en el modelo) hacia adelante del mando
                Vector3 targetAim = bladeForwardAim.sqrMagnitude > 0.001f ? bladeForwardAim.normalized : Vector3.forward;
                transform.localRotation = Quaternion.FromToRotation(Vector3.up, targetAim);

                isBound = true;

                // Ignorar colisiones con el cuerpo del jugador para máxima fluidez
                Collider[] playerCols = rightHandTarget.root.GetComponentsInChildren<Collider>();
                if (swordCollider != null)
                {
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
            // 1. Buscar controladores XR por tipo
            var controllers = Object.FindObjectsByType<XRBaseController>(FindObjectsSortMode.None);
            foreach (var c in controllers)
            {
                string n = c.name.ToLower();
                if (n.Contains("right"))
                {
                    return c.transform;
                }
            }

            // 2. Buscar dentro de la jerarquía de XROrigin CameraFloorOffset
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

            // 3. Buscar interactores con "right" en el nombre o en su padre
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

            // 4. Buscar por nombre exacto en toda la jerarquía de la escena
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

        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrabSelected);
            }
        }
    }
}
