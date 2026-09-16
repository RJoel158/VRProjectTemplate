using System;
using System.Collections;
using UnityEngine;

namespace Esgrima.Combat
{
    /// <summary>
    /// Gestiona el retroceso físico (knockback) gradual de un luchador al recibir un impacto de espada.
    /// Funciona con Rigidbody, CharacterController o directamente por interpolación de Transform.
    /// </summary>
    public class KnockbackController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private CharacterController characterController;
        [Tooltip("Transform que será desplazado por el retroceso. Si es nulo, desplaza este mismo GameObject.")]
        [SerializeField] private Transform targetTransformToMove;

        [Header("Settings")]
        [Tooltip("Duración en segundos del desplazamiento de retroceso.")]
        [SerializeField] private float knockbackDuration = 0.25f;

        [Tooltip("Curva de desaceleración del impacto.")]
        [SerializeField] private AnimationCurve knockbackCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        private Coroutine activeKnockbackCoroutine;

        public event Action<Vector3, float> OnKnockbackApplied;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (characterController == null) characterController = GetComponent<CharacterController>();
        }

        /// <summary>
        /// Aplica un retroceso en la dirección indicada con la fuerza dada.
        /// </summary>
        /// <param name="direction">Dirección del empuje (generalmente horizontal).</param>
        /// <param name="force">Magnitud del empuje.</param>
        public void ApplyKnockback(Vector3 direction, float force)
        {
            // Asegurar que el empuje sea predominantemente horizontal para mantener a los luchadores en la arena
            direction.y = 0;
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = -transform.forward;
            }
            direction.Normalize();

            if (activeKnockbackCoroutine != null)
            {
                StopCoroutine(activeKnockbackCoroutine);
            }

            activeKnockbackCoroutine = StartCoroutine(PerformKnockbackRoutine(direction, force));
            OnKnockbackApplied?.Invoke(direction, force);
        }

        private IEnumerator PerformKnockbackRoutine(Vector3 direction, float totalDistance)
        {
            float elapsed = 0f;
            Vector3 previousDisplacement = Vector3.zero;

            while (elapsed < knockbackDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / knockbackDuration);
                float curveValue = knockbackCurve.Evaluate(t);

                // Desplazamiento acumulado por paso
                float stepSpeed = (totalDistance / knockbackDuration) * curveValue * Time.deltaTime;
                Vector3 stepVector = direction * stepSpeed;

                if (characterController != null && characterController.enabled)
                {
                    characterController.Move(stepVector);
                }
                else if (rb != null && !rb.isKinematic)
                {
                    rb.MovePosition(rb.position + stepVector);
                }
                else
                {
                    Transform targetTrans = targetTransformToMove != null ? targetTransformToMove : transform;
                    targetTrans.position += stepVector;
                }

                yield return null;
            }

            activeKnockbackCoroutine = null;
        }

        /// <summary>
        /// Cancela cualquier knockback activo (por ejemplo al reiniciar la ronda).
        /// </summary>
        public void ResetKnockback()
        {
            if (activeKnockbackCoroutine != null)
            {
                StopCoroutine(activeKnockbackCoroutine);
                activeKnockbackCoroutine = null;
            }
        }
    }
}
