using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Esgrima.Data;

namespace Esgrima.Combat
{
    /// <summary>
    /// Maneja el comportamiento físico, detección de velocidad de corte,
    /// choques (bloqueo/parry) e impactos de la espada de esgrima en VR.
    /// </summary>
    public class VRSword : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SwordDataSO swordData;
        [SerializeField] private bool isPlayerSword = true;

        [Header("Tracking References")]
        [Tooltip("Punto de referencia en la punta de la espada para calcular la velocidad del golpe.")]
        [SerializeField] private Transform bladeTip;
        [SerializeField] private Transform bladeBase;

        [Header("Components")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private XRGrabInteractable grabInteractable;

        [Header("Hit Cooldown")]
        [SerializeField] private float hitCooldown = 0.35f;

        private Vector3 lastTipPosition;
        private float currentBladeSpeed;
        private float lastHitTimestamp;

        public bool IsPlayerSword => isPlayerSword;
        public SwordDataSO Data => swordData;
        public float CurrentBladeSpeed => currentBladeSpeed;

        public event Action<Vector3> OnSwordClash;
        public event Action<FighterHitbox, Vector3> OnHitLanded;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (grabInteractable == null) grabInteractable = GetComponent<XRGrabInteractable>();

            if (bladeTip == null)
            {
                bladeTip = transform;
            }
        }

        private void Start()
        {
            lastTipPosition = bladeTip.position;
        }

        private void Update()
        {
            // Medir la velocidad del corte en metros por segundo
            float dt = Time.deltaTime;
            if (dt > 0.0001f)
            {
                Vector3 currentPos = bladeTip.position;
                currentBladeSpeed = (currentPos - lastTipPosition).magnitude / dt;
                lastTipPosition = currentPos;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleCollision(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision.collider);
        }

        private void HandleCollision(Collider other)
        {
            // Ignorar colisiones consigo mismo o su propia estructura
            if (other.transform.IsChildOf(transform.root)) return;

            // 1. Caso: Choque Espada contra Espada (Bloqueo / Parry estilo Wii Sports)
            VRSword otherSword = other.GetComponentInParent<VRSword>();
            if (otherSword != null && otherSword != this)
            {
                // Solo procesar si son de bandos contrarios
                if (otherSword.IsPlayerSword != this.isPlayerSword)
                {
                    ResolveSwordClash(otherSword);
                }
                return;
            }

            // 2. Caso: Impacto en el cuerpo del luchador (Hitbox)
            FighterHitbox hitbox = other.GetComponentInParent<FighterHitbox>();
            if (hitbox != null)
            {
                // No golpearse a uno mismo
                if (hitbox.IsPlayer == this.isPlayerSword) return;

                ResolveFighterHit(hitbox);
            }
        }

        private void ResolveSwordClash(VRSword otherSword)
        {
            // Evitar spam de choques en el mismo fotograma
            if (Time.time - lastHitTimestamp < 0.15f) return;
            lastHitTimestamp = Time.time;

            Vector3 clashPoint = (bladeTip.position + otherSword.bladeTip.position) * 0.5f;

            // Sonido metálico de choque
            if (swordData != null && swordData.clashSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(swordData.clashSound);
            }

            // Feedback háptico en el mando VR
            TriggerHaptics();

            OnSwordClash?.Invoke(clashPoint);
        }

        private void ResolveFighterHit(FighterHitbox targetHitbox)
        {
            // Cooldown de golpe para evitar registrar múltiples hits en una misma pasada
            if (Time.time - lastHitTimestamp < hitCooldown) return;

            // Comprobar si el corte supera la velocidad mínima requerida
            float minSpeed = (swordData != null) ? swordData.minSwingVelocity : 0.8f;
            if (currentBladeSpeed < minSpeed)
            {
                // Toque demasiado suave/lento, no cuenta como estocada
                return;
            }

            lastHitTimestamp = Time.time;

            // Dirección del empuje: hacia adelante según la dirección de la espada o el golpe
            Vector3 pushDirection = (targetHitbox.transform.position - transform.position);
            pushDirection.y = 0;
            pushDirection.Normalize();

            float knockback = (swordData != null) ? swordData.knockbackImpulse : 2.5f;
            AudioClip hitAudio = (swordData != null) ? swordData.fleshHitSound : null;

            targetHitbox.TakeHit(bladeTip.position, pushDirection, knockback, hitAudio);

            // Vibración fuerte de impacto en el control de Oculus Quest
            TriggerHaptics();

            OnHitLanded?.Invoke(targetHitbox, bladeTip.position);
        }

        public void TriggerHaptics()
        {
            if (!isPlayerSword || swordData == null) return;

            // Si está agarrada mediante XRI Grab Interactable, enviar impulso al interactor activo
            if (grabInteractable != null && grabInteractable.isSelected)
            {
                foreach (var interactor in grabInteractable.interactorsSelecting)
                {
                    if (interactor is XRBaseInputInteractor inputInteractor)
                    {
                        inputInteractor.SendHapticImpulse(swordData.hapticIntensity, swordData.hapticDurationSeconds);
                    }
                }
            }
        }
    }
}
