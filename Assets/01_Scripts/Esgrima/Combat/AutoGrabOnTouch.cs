using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Esgrima.Combat
{
    /// <summary>
    /// Permite que la espada se auto-equipe inmediatamente en la mano del jugador
    /// en cuanto este la toca o se acerca con su interactor (estilo arcade del template).
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class AutoGrabOnTouch : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Si es true, la espada se equipará automáticamente apenas la mano toque el collider o entre en su rango.")]
        [SerializeField] private bool autoSnapOnTouch = true;

        [Tooltip("Si es true, no permitirá que el jugador suelte accidentalmente la espada durante el combate intenso.")]
        [SerializeField] private bool retainGripInCombat = false;

        private XRGrabInteractable grabInteractable;
        private XRInteractionManager interactionManager;

        private void Awake()
        {
            grabInteractable = GetComponent<XRGrabInteractable>();
        }

        private void Start()
        {
            if (interactionManager == null)
            {
                interactionManager = Object.FindFirstObjectByType<XRInteractionManager>();
            }

            if (grabInteractable != null)
            {
                grabInteractable.hoverEntered.AddListener(OnHoverEntered);
            }
        }

        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            }
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (!autoSnapOnTouch || grabInteractable == null || grabInteractable.isSelected)
                return;

            if (args.interactorObject is IXRSelectInteractor selectInteractor)
            {
                // Auto-asignar a la mano que lo ha tocado / sobrevolado
                EquipToHand(selectInteractor);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!autoSnapOnTouch || grabInteractable == null || grabInteractable.isSelected)
                return;

            // Buscar si el objeto que colisionó es una mano o interactor de XR
            IXRSelectInteractor interactor = other.GetComponentInParent<IXRSelectInteractor>();
            if (interactor != null)
            {
                EquipToHand(interactor);
            }
        }

        public void EquipToHand(IXRSelectInteractor interactor)
        {
            if (interactionManager == null)
            {
                interactionManager = Object.FindFirstObjectByType<XRInteractionManager>();
            }

            if (interactionManager != null && interactor != null && grabInteractable != null && !grabInteractable.isSelected)
            {
                interactionManager.SelectEnter(interactor, grabInteractable);
                Debug.Log($"[AutoGrabOnTouch] Espada asignada automáticamente a la mano: {interactor.transform.name}");
            }
        }
    }
}
