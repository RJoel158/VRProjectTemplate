using System;
using UnityEngine;
using Esgrima.Data;

namespace Esgrima.Core
{
    /// <summary>
    /// Monitorea continuamente si un luchador traspasa el borde de la plataforma circular
    /// o cae por debajo del umbral de altura (ring out al agua/abismo).
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
            float radius = matchConfig != null ? matchConfig.ringRadius : 3.5f;
            float fallThreshold = matchConfig != null ? matchConfig.fallYThreshold : -0.8f;

            // 1. Chequeo del Jugador
            if (playerTransform != null)
            {
                float distPlayer = Vector3.Distance(new Vector3(playerTransform.position.x, 0, playerTransform.position.z),
                                                   new Vector3(center.x, 0, center.z));
                if (distPlayer > radius || playerTransform.position.y < fallThreshold)
                {
                    roundActive = false;
                    OnFighterFellOutOfBounds?.Invoke(true);
                    return;
                }
            }

            // 2. Chequeo de la IA
            if (aiTransform != null)
            {
                float distAI = Vector3.Distance(new Vector3(aiTransform.position.x, 0, aiTransform.position.z),
                                               new Vector3(center.x, 0, center.z));
                if (distAI > radius || aiTransform.position.y < fallThreshold)
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
            float radius = matchConfig != null ? matchConfig.ringRadius : 3.5f;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center, radius);
        }
    }
}
