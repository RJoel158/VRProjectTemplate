using System.Collections;
using UnityEngine;
using Esgrima.Data;

namespace Esgrima.Combat
{
    public enum FencerState
    {
        Idle,
        Guard,
        Windup,
        Attack,
        Staggered,
        Down
    }

    /// <summary>
    /// IA sencilla y reactiva para el contrincante de esgrima.
    /// Presenta ciclos claros de telegrafiado, ataque y vulnerabilidad (Stagger al ser bloqueado),
    /// emulando el comportamiento del rival en Wii Sports Resort.
    /// </summary>
    public class FencerAI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private FighterConfigSO config;
        [SerializeField] private Transform targetPlayer;

        [Header("Sword Setup")]
        [SerializeField] private VRSword aiSword;
        [SerializeField] private Transform swordArmPivot;

        [Header("Poses")]
        [SerializeField] private Vector3 guardLocalPos = new Vector3(0.25f, 1.2f, 0.5f);
        [SerializeField] private Vector3 guardLocalRot = new Vector3(25f, -10f, 0f);
        [SerializeField] private Vector3 windupLocalPos = new Vector3(0.35f, 1.5f, 0.1f);
        [SerializeField] private Vector3 windupLocalRot = new Vector3(60f, 15f, 0f);
        [SerializeField] private Vector3 attackLocalPos = new Vector3(0.0f, 1.1f, 1.1f);
        [SerializeField] private Vector3 attackLocalRot = new Vector3(0f, 0f, 0f);

        private FencerState currentState = FencerState.Idle;
        private Coroutine behaviorRoutine;
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        public FencerState CurrentState => currentState;

        private void Awake()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void Start()
        {
            if (targetPlayer == null && Camera.main != null)
            {
                targetPlayer = Camera.main.transform;
            }

            if (aiSword != null)
            {
                aiSword.OnSwordClash += HandleSwordClashed;
            }

            StartAI();
        }

        private void OnDestroy()
        {
            if (aiSword != null)
            {
                aiSword.OnSwordClash -= HandleSwordClashed;
            }
        }

        private void Update()
        {
            // Orientarse hacia el jugador mientras esté activo
            if (targetPlayer != null && currentState != FencerState.Down)
            {
                Vector3 lookTarget = targetPlayer.position;
                lookTarget.y = transform.position.y;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookTarget - transform.position), Time.deltaTime * 5f);
            }
        }

        public void StartAI()
        {
            if (behaviorRoutine != null) StopCoroutine(behaviorRoutine);
            currentState = FencerState.Guard;
            behaviorRoutine = StartCoroutine(AIBehaviorLoop());
        }

        public void StopAI()
        {
            if (behaviorRoutine != null)
            {
                StopCoroutine(behaviorRoutine);
                behaviorRoutine = null;
            }
            currentState = FencerState.Idle;
        }

        public void ResetFencer()
        {
            StopAI();
            transform.position = initialPosition;
            transform.rotation = initialRotation;
            if (swordArmPivot != null)
            {
                swordArmPivot.localPosition = guardLocalPos;
                swordArmPivot.localRotation = Quaternion.Euler(guardLocalRot);
            }
            StartAI();
        }

        private IEnumerator AIBehaviorLoop()
        {
            while (true)
            {
                // 1. Fase de Guardia (Espera)
                currentState = FencerState.Guard;
                yield return AnimateArm(guardLocalPos, guardLocalRot, 0.3f);

                float waitTime = Random.Range(
                    config != null ? config.attackIntervalMin : 2.0f,
                    config != null ? config.attackIntervalMax : 4.0f
                );
                yield return new WaitForSeconds(waitTime);

                // 2. Fase de Telegrafiado / Windup (El jugador ve venir el golpe para poder bloquear)
                currentState = FencerState.Windup;
                float windupTime = config != null ? config.windupDuration : 0.8f;
                yield return AnimateArm(windupLocalPos, windupLocalRot, windupTime);

                // 3. Fase de Ataque (Estocada veloz)
                currentState = FencerState.Attack;
                yield return AnimateArm(attackLocalPos, attackLocalRot, 0.18f);

                // Pausa breve tras el ataque
                yield return new WaitForSeconds(0.3f);
            }
        }

        private void HandleSwordClashed(Vector3 clashPoint)
        {
            // Si el jugador choca espadas durante el ataque de la IA -> Stagger / Aturdimiento
            if (currentState == FencerState.Attack || currentState == FencerState.Windup)
            {
                if (behaviorRoutine != null) StopCoroutine(behaviorRoutine);
                StartCoroutine(StaggerRoutine());
            }
        }

        private IEnumerator StaggerRoutine()
        {
            currentState = FencerState.Staggered;

            // Retroceso del brazo
            Vector3 recoilPos = windupLocalPos + Vector3.back * 0.2f;
            yield return AnimateArm(recoilPos, windupLocalRot, 0.1f);

            float staggerTime = config != null ? config.staggerDuration : 1.2f;
            yield return new WaitForSeconds(staggerTime);

            // Reanudar combate
            behaviorRoutine = StartCoroutine(AIBehaviorLoop());
        }

        private IEnumerator AnimateArm(Vector3 targetPos, Vector3 targetEulerRot, float duration)
        {
            if (swordArmPivot == null) yield break;

            Vector3 startPos = swordArmPivot.localPosition;
            Quaternion startRot = swordArmPivot.localRotation;
            Quaternion targetRot = Quaternion.Euler(targetEulerRot);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                swordArmPivot.localPosition = Vector3.Lerp(startPos, targetPos, t);
                swordArmPivot.localRotation = Quaternion.Slerp(startRot, targetRot, t);
                yield return null;
            }

            swordArmPivot.localPosition = targetPos;
            swordArmPivot.localRotation = targetRot;
        }
    }
}
