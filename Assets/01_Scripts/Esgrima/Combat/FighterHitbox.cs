using System;
using UnityEngine;
using Esgrima.Data;

namespace Esgrima.Combat
{
    /// <summary>
    /// Componente que recibe los impactos de la espada enemiga en el cuerpo del luchador
    /// y delega el empuje a KnockbackController.
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

            OnHitTaken?.Invoke(hitPoint, finalForce);
        }
    }
}
