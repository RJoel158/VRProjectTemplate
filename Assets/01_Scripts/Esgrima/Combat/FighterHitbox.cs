using System;
using UnityEngine;
using Esgrima.Data;
using Esgrima.Core;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Esgrima.Combat
{
    /// <summary>
    /// Componente que recibe los impactos de la espada enemiga en el cuerpo del luchador.
    /// Si es el jugador, sigue dinámicamente la posición del visor VR (Headset) y notifica al GameManager.
    /// </summary>
    public class FighterHitbox : MonoBehaviour
    {
        [Header("Fighter Identity")]
        [Tooltip("Indica si este cuerpo pertenece al jugador de VR (true) o a la IA/rival (false).")]
        [SerializeField] private bool isPlayer = false;

        [Header("Configuration")]
        [SerializeField] private FighterConfigSO fighterConfig;
        [SerializeField] private KnockbackController knockbackController;

        [Header("Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private ParticleSystem hitVFXPrefab;

        public bool IsPlayer => isPlayer;
        public FighterConfigSO Config => fighterConfig;

        public event Action<Vector3, float> OnHitTaken;

        private Transform vrCameraTransform;

        private void Awake()
        {
            if (knockbackController == null)
            {
                knockbackController = GetComponentInParent<KnockbackController>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        private void Start()
        {
            if (isPlayer && Camera.main != null)
            {
                vrCameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            // Si es el jugador de VR, mantener el collider centrado exactamente donde está la cabeza físicamente
            if (isPlayer && vrCameraTransform != null)
            {
                Vector3 headPos = vrCameraTransform.position;
                transform.position = new Vector3(headPos.x, headPos.y * 0.5f, headPos.z);
            }
        }

        public void SetConfig(FighterConfigSO config)
        {
            fighterConfig = config;
        }

        /// <summary>
        /// Llamado por VRSword cuando asesta un golpe válido.
        /// </summary>
        public void TakeHit(Vector3 hitPoint, Vector3 hitDirection, float baseKnockback, AudioClip hitAudio = null)
        {
            float multiplier = fighterConfig != null ? fighterConfig.knockbackMultiplier : 1.0f;
            float finalForce = baseKnockback * multiplier;

            // Reproducir sonido
            if (hitAudio != null && audioSource != null)
            {
                audioSource.PlayOneShot(hitAudio);
            }

            // Efecto visual si existe
            if (hitVFXPrefab != null)
            {
                Instantiate(hitVFXPrefab, hitPoint, Quaternion.LookRotation(hitDirection));
            }

            // Aplicar retroceso
            if (knockbackController != null)
            {
                knockbackController.ApplyKnockback(hitDirection, finalForce);
            }

            // Si es el jugador recibiendo el golpe, vibrar los mandos
            if (isPlayer)
            {
                TriggerHapticsOnPlayer();
            }

            // Notificar al MatchManager para registrar el golpe y actualizar marcador
            if (EsgrimaMatchManager.Instance != null)
            {
                EsgrimaMatchManager.Instance.RegisterFighterHit(isPlayer, finalForce);
            }

            OnHitTaken?.Invoke(hitPoint, finalForce);
        }

        private void TriggerHapticsOnPlayer()
        {
            var interactors = UnityEngine.Object.FindObjectsByType<XRBaseInputInteractor>(FindObjectsSortMode.None);
            foreach (var interactor in interactors)
            {
                interactor.SendHapticImpulse(0.7f, 0.2f);
            }
        }
    }
}
