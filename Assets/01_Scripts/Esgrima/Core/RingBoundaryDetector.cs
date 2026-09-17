using System;
using UnityEngine;
using Esgrima.Data;

namespace Esgrima.Core
{
    /// <summary>
    /// Monitorea continuamente si un luchador traspasa el borde de la plataforma circular
    /// o cae por debajo del umbral de altura (ring out al agua/abismo).
    /// Detecta la posición real del jugador en VR (Headset) y de la IA.
    /// </summary>
    public class RingBoundaryDetector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EsgrimaMatchConfigSO matchConfig;
        [SerializeField] private Transform ringCenter;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Transform aiTransform;

        public event Action<bool> OnFighterFellOutOfBounds; // true = Player cayó, false = AI cayó

        private bool roundActive = true;

        public void SetRoundActive(bool active)
        {
            roundActive = active;
        }

        private void Update()
        {
            if (!roundActive) return;

            Vector3 center = ringCenter != null ? ringCenter.position : Vector3.zero;
            float radius = matchConfig != null ? matchConfig.ringRadius : 5.0f;
            // Umbral de caída: a partir del 94% del radio ya se considera ring out inevitable
            float outOfBoundsDistance = radius * 0.94f;
            float fallThreshold = matchConfig != null ? matchConfig.fallYThreshold : -0.6f;

            // 1. Chequeo del Jugador (usando visor físico VR Camera o playerTransform)
            Vector3 playerPos = (Camera.main != null) ? Camera.main.transform.position : (playerTransform != null ? playerTransform.position : Vector3.zero);
            float distPlayer = Vector3.Distance(new Vector3(playerPos.x, 0, playerPos.z), new Vector3(center.x, 0, center.z));

            if (distPlayer > outOfBoundsDistance || playerPos.y < fallThreshold)
            {
                roundActive = false;
                OnFighterFellOutOfBounds?.Invoke(true);
                return;
            }

            // 2. Chequeo de la IA
            if (aiTransform != null)
            {
                float distAI = Vector3.Distance(new Vector3(aiTransform.position.x, 0, aiTransform.position.z), new Vector3(center.x, 0, center.z));

                // Si la IA fue empujada fuera de la plataforma, hacerla caer al agua físicamente
                if (distAI > outOfBoundsDistance)
                {
                    aiTransform.position += Vector3.down * 5.0f * Time.deltaTime;
                }

                if (distAI > outOfBoundsDistance || aiTransform.position.y < fallThreshold)
                {
                    roundActive = false;
                    OnFighterFellOutOfBounds?.Invoke(false);
                    return;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = ringCenter != null ? ringCenter.position : transform.position;
            float radius = matchConfig != null ? matchConfig.ringRadius : 5.0f;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center, radius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, radius * 0.94f);
        }
    }
}
