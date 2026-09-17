using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Esgrima.Combat
{
    /// <summary>
    /// Garantiza que el sable se acople firmemente en la mano del jugador
    /// con la postura, ángulo y agarre de un sable de esgrima real,
    /// eliminando cualquier desalineación o flotación vertical.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class SwordGripHandler : MonoBehaviour
    {
        [Header("Grip Alignment")]
        [Tooltip("Punto de anclaje exacto en la empuñadura del sable.")]
        [SerializeField] private Transform gripAttachPoint;

        [Tooltip("Rotación angular en grados para orientar la hoja hacia adelante de la mano.")]
        [SerializeField] private Vector3 gripEulerRotation = new Vector3(75f, 0f, 0f);

        [Tooltip("Desplazamiento local de la empuñadura respecto a la palma del mando.")]
        [SerializeField] private Vector3 gripLocalOffset = new Vector3(0f, -0.03f, 0.04f);

        private XRGrabInteractable grabInteractable;
        private Rigidbody rb;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();

            SetupGripAttachPoint();
            ConfigureInteractable();
        }

        private void SetupGripAttachPoint()
        {
            if (gripAttachPoint == null)
            {
                Transform existing = transform.Find("Grip_Attach_Point");
                if (existing != null)
                {
                    gripAttachPoint = existing;
                }
                else
                {
                    GameObject go = new GameObject("Grip_Attach_Point");
                    go.transform.SetParent(transform, false);
                    gripAttachPoint = go.transform;
                }
            }

            gripAttachPoint.localPosition = gripLocalOffset;
            gripAttachPoint.localRotation = Quaternion.Euler(gripEulerRotation);
        }

        private void ConfigureInteractable()
        {
            if (grabInteractable == null) return;

            // Obligar al XRGrabInteractable a usar este punto de anclaje exclusivo
            grabInteractable.attachTransform = gripAttachPoint;
            grabInteractable.useDynamicAttach = false;
            grabInteractable.matchAttachPosition = true;
            grabInteractable.matchAttachRotation = true;
            grabInteractable.snapToColliderVolume = false;
            grabInteractable.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grabInteractable.throwOnDetach = false;

            grabInteractable.selectEntered.AddListener(OnSwordSelected);
            grabInteractable.selectExited.AddListener(OnSwordReleased);
        }

        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnSwordSelected);
                grabInteractable.selectExited.RemoveListener(OnSwordReleased);
            }
        }

        private void OnSwordSelected(SelectEnterEventArgs args)
        {
            // Resetear cualquier desfase que el Ray Interactor pudiera haber acumulado
            if (args.interactorObject is XRBaseInteractor interactor)
            {
                Transform interactorAttach = interactor.GetAttachTransform(grabInteractable);
                if (interactorAttach != null)
                {
                    interactorAttach.localPosition = Vector3.zero;
                    interactorAttach.localRotation = Quaternion.identity;
                }

                if (interactor is XRBaseInputInteractor inputInteractor)
                {
                    inputInteractor.SendHapticImpulse(0.5f, 0.1f);
                }
            }

            if (rb != null)
            {
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void OnSwordReleased(SelectExitEventArgs args)
        {
            if (rb != null)
            {
                // Al soltarla, evitar que salga volando erráticamente
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
            }
        }

        /// <summary>
        /// Permite calibrar la inclinación de la hoja en tiempo real si el usuario lo requiere.
        /// </summary>
        public void SetGripAngle(float pitchDegrees)
        {
            gripEulerRotation.x = pitchDegrees;
            if (gripAttachPoint != null)
            {
                gripAttachPoint.localRotation = Quaternion.Euler(gripEulerRotation);
            }
        }
    }
}
