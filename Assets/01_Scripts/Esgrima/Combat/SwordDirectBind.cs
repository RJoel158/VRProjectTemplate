using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

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
        [SerializeField] private Vector3 localGripPosition = new Vector3(0f, -0.035f, 0.08f);

        [Tooltip("Vector de inclinación de la hoja: hacia adelante (+Z) con ligero ángulo hacia arriba (+Y).")]
        [SerializeField] private Vector3 bladeForwardAim = new Vector3(0f, 0.18f, 0.98f);

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
            // Si por alguna razón perdió el parentesco o no se vinculó en Start, re-vincular
            if (!isBound || transform.parent == null)
            {
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
            // 1. Buscar interactores de entrada de la mano derecha
            XRBaseInputInteractor[] interactors = Object.FindObjectsByType<XRBaseInputInteractor>(FindObjectsSortMode.None);
            for (int i = 0; i < interactors.Length; i++)
            {
                string n = interactors[i].gameObject.name.ToLower();
                if (n.Contains("right") || n.Contains("derech"))
                {
                    return interactors[i].transform;
                }
            }

            // 2. Buscar por nombre en jerarquía
            GameObject[] allGos = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            for (int i = 0; i < allGos.Length; i++)
            {
                string n = allGos[i].name.ToLower();
                if (n.Contains("right controller") || n.Contains("righthand") || n.Contains("right_hand"))
                {
                    return allGos[i].transform;
                }
            }

            // 3. Fallback: Main Camera
            if (Camera.main != null)
            {
                return Camera.main.transform;
            }

            return null;
        }
    }
}
