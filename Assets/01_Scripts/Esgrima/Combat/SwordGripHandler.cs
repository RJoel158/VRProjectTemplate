using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Esgrima.Combat
{
    /// <summary>
    /// Fija el sable a la mano/mando de Oculus como un objeto completamente sólido (un palo),
    /// eliminando cualquier temblor, retraso o interferencia física con el cuerpo del jugador.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    [RequireComponent(typeof(Rigidbody))]
    public class SwordGripHandler : MonoBehaviour
    {
        [Header("Grip Alignment")]
        [Tooltip("Rotación angular en grados para orientar la hoja hacia adelante del puño.")]
        [SerializeField] private Vector3 gripEulerRotation = new Vector3(-80f, 0f, 0f);

        [Tooltip("Desplazamiento local de la empuñadura respecto a la palma del mando.")]
        [SerializeField] private Vector3 gripLocalOffset = new Vector3(0f, -0.03f, 0.05f);

        private XRGrabInteractable grabInteractable;
        private Rigidbody rb;
        private Collider swordCollider;
        private Transform gripAttachPoint;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
            rb = GetComponent<Rigidbody>();
            swordCollider = GetComponent<Collider>();

            SetupGripAttachPoint();
            ConfigureInteractable();
        }

        private void SetupGripAttachPoint()
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

            gripAttachPoint.localPosition = gripLocalOffset;
            gripAttachPoint.localRotation = Quaternion.Euler(gripEulerRotation);
        }

        private void ConfigureInteractable()
        {
            if (grabInteractable == null) return;

            grabInteractable.attachTransform = gripAttachPoint;
            grabInteractable.useDynamicAttach = false;
            grabInteractable.matchAttachPosition = true;
            grabInteractable.matchAttachRotation = true;
            grabInteractable.snapToColliderVolume = false;

            // Instantaneous para seguimiento 1:1 directo sin temblores ni retraso físico
            grabInteractable.movementType = XRBaseInteractable.MovementType.Instantaneous;
            grabInteractable.trackPosition = true;
            grabInteractable.trackRotation = true;
            grabInteractable.smoothPosition = false;
            grabInteractable.smoothRotation = false;
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
            // Bloqueo cinemático total para evitar que la física pelee con la mano
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Al estar en mano, el collider pasa a Trigger:
            // Sigue detectando golpes al rival y choques espada con espada (VRSword usa OnTriggerEnter),
            // pero NUNCA empujará la mano ni rebotará contra el cuerpo del jugador provocando temblores.
            if (swordCollider != null)
            {
                swordCollider.isTrigger = true;
            }

            // Ignorar colisiones con cualquier collider de la jerarquía del jugador
            Collider[] allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
            Transform interactorRoot = args.interactorObject != null ? args.interactorObject.transform.root : null;
            if (interactorRoot != null && swordCollider != null)
            {
                foreach (var col in allColliders)
                {
                    if (col != swordCollider && col.transform.IsChildOf(interactorRoot))
                    {
                        Physics.IgnoreCollision(swordCollider, col, true);
                    }
                }
            }

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
                    inputInteractor.SendHapticImpulse(0.5f, 0.08f);
                }
            }
        }

        private void OnSwordReleased(SelectExitEventArgs args)
        {
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Al soltarla, vuelve a sólido para que el rayo a distancia pueda volver a detectarla y agarrarla
            if (swordCollider != null)
            {
                swordCollider.isTrigger = false;
            }
        }
    }
}
