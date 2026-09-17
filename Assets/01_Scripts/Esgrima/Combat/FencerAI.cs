using System.Collections;
using UnityEngine;
using Esgrima.Data;
using Esgrima.Core;

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
    /// Inteligencia Artificial dinámica para el rival de esgrima.
    /// Incluye desplazamiento inteligente y seguro por la arena (sin riesgo de caerse),
    /// combos variados de ataque y sistema de Bloqueo / Parry con aturdimiento y contraataque.
    /// </summary>
    public class FencerAI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private FighterConfigSO config;
        [SerializeField] private Transform targetPlayer;

        [Header("Sword Setup")]
        [SerializeField] private VRSword aiSword;
        [SerializeField] private Transform swordArmPivot;

        [Header("Poses Base")]
        [SerializeField] private Vector3 guardLocalPos = new Vector3(0.25f, 1.2f, 0.5f);
        [SerializeField] private Vector3 guardLocalRot = new Vector3(25f, -10f, 0f);

        [Header("Poses de Ataque")]
        [SerializeField] private Vector3 thrustWindupPos = new Vector3(0.3f, 1.35f, 0.2f);
        [SerializeField] private Vector3 thrustWindupRot = new Vector3(45f, 10f, 0f);
        [SerializeField] private Vector3 thrustAttackPos = new Vector3(0.0f, 1.15f, 1.15f);
        [SerializeField] private Vector3 thrustAttackRot = new Vector3(5f, 0f, 0f);

        [SerializeField] private Vector3 sweepWindupPos = new Vector3(-0.35f, 1.25f, 0.35f);
        [SerializeField] private Vector3 sweepWindupRot = new Vector3(15f, -65f, 0f);
        [SerializeField] private Vector3 sweepAttackPos = new Vector3(0.45f, 1.2f, 0.95f);
        [SerializeField] private Vector3 sweepAttackRot = new Vector3(20f, 60f, 0f);

        [SerializeField] private Vector3 overheadWindupPos = new Vector3(0.15f, 1.7f, 0.05f);
        [SerializeField] private Vector3 overheadWindupRot = new Vector3(80f, 0f, 0f);
        [SerializeField] private Vector3 overheadAttackPos = new Vector3(0.05f, 0.85f, 1.05f);
        [SerializeField] private Vector3 overheadAttackRot = new Vector3(-35f, 0f, 0f);

        private FencerState currentState = FencerState.Idle;
        private Coroutine behaviorRoutine;
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        // Variables de desplazamiento
        private float strafeTimer = 0f;
        private float currentStrafeSign = 1f;

        public FencerState CurrentState => currentState;
        public bool IsStunned => currentState == FencerState.Staggered;

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
            if (currentState == FencerState.Down) return;

            // Orientarse hacia el jugador
            if (targetPlayer != null)
            {
                Vector3 lookTarget = targetPlayer.position;
                lookTarget.y = transform.position.y;
                Vector3 dir = lookTarget - transform.position;
                if (dir.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 6f);
                }
            }

            // Desplazamiento táctico seguro solo en fase de guardia
            if (currentState == FencerState.Guard)
            {
                UpdateArenaFootwork();
            }
        }

        /// <summary>
        /// Mueve al rival con pasos de esgrima manteniendo distancia y rodeando al jugador,
        /// con delimitación matemática estricta para garantizar que JAMÁS caiga fuera del ring por movimiento propio.
        /// </summary>
        private void UpdateArenaFootwork()
        {
            if (targetPlayer == null) return;

            Vector3 toPlayer = targetPlayer.position - transform.position;
            toPlayer.y = 0;
            float dist = toPlayer.magnitude;
            Vector3 forwardDir = toPlayer.normalized;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir);

            Vector3 moveDir = Vector3.zero;
            float baseSpeed = config != null ? config.moveSpeed : 1.2f;
            float strafeSpd = config != null ? config.strafeSpeed : 0.8f;

            // 1. Control de distancia de combate (óptima: 1.5m a 2.4m)
            if (dist > 2.4f)
            {
                moveDir += forwardDir * baseSpeed;
            }
            else if (dist < 1.4f)
            {
                moveDir -= forwardDir * (baseSpeed * 0.9f);
            }

            // 2. Movimiento lateral (Strafe) alternando lados
            strafeTimer -= Time.deltaTime;
            if (strafeTimer <= 0f)
            {
                currentStrafeSign = Random.value > 0.5f ? 1f : -1f;
                strafeTimer = Random.Range(1.6f, 3.2f);
            }
            moveDir += rightDir * (currentStrafeSign * strafeSpd);

            // 3. Delimitación de seguridad del Ring (evita caídas al vacío)
            Vector3 currentPos = transform.position;
            Vector2 hPos = new Vector2(currentPos.x, currentPos.z);
            float safeLimit = config != null ? config.maxSafeArenaRadius : 3.6f;

            // Si se acerca a la zona de peligro del borde, redirigir hacia el centro (0, 0)
            if (hPos.magnitude > safeLimit * 0.8f)
            {
                Vector3 toCenter = -new Vector3(currentPos.x, 0, currentPos.z).normalized;
                moveDir = Vector3.Lerp(moveDir, toCenter * baseSpeed, 0.8f);
            }

            // Aplicar traslación horizontal
            transform.position += moveDir * Time.deltaTime;

            // Bloqueo duro e inviolable contra la caída por paso en falso
            Vector3 clampedPos = transform.position;
            Vector2 clampedH = new Vector2(clampedPos.x, clampedPos.z);
            if (clampedH.magnitude > safeLimit)
            {
                clampedH = clampedH.normalized * safeLimit;
                clampedPos.x = clampedH.x;
                clampedPos.z = clampedH.y;
                transform.position = clampedPos;
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
                // 1. Fase de Guardia y acecho
                currentState = FencerState.Guard;
                yield return AnimateArm(guardLocalPos, guardLocalRot, 0.25f);

                float waitTime = Random.Range(
                    config != null ? config.attackIntervalMin : 2.0f,
                    config != null ? config.attackIntervalMax : 3.5f
                );
                yield return new WaitForSeconds(waitTime);

                // 2. Selección de ataque (Estocada simple, Tajo lateral o Combo de 2 golpes)
                float roll = Random.value;
                float comboProb = config != null ? config.comboChance : 0.45f;

                if (roll < comboProb)
                {
                    yield return ExecuteDoubleCombo();
                }
                else if (roll < comboProb + 0.3f)
                {
                    yield return ExecuteHorizontalSweep();
                }
                else if (roll < comboProb + 0.55f)
                {
                    yield return ExecuteOverheadChop();
                }
                else
                {
                    yield return ExecuteQuickThrust();
                }

                // Pausa post-acción
                yield return new WaitForSeconds(0.4f);
            }
        }

        private IEnumerator ExecuteQuickThrust()
        {
            // Telegrafiado
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration : 0.7f;
            yield return AnimateArm(thrustWindupPos, thrustWindupRot, windup);

            // Ataque
            currentState = FencerState.Attack;
            yield return AnimateArm(thrustAttackPos, thrustAttackRot, 0.18f);

            // Recuperación a guardia
            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.25f);
        }

        private IEnumerator ExecuteHorizontalSweep()
        {
            // Telegrafiado lateral amplio
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration * 1.1f : 0.8f;
            yield return AnimateArm(sweepWindupPos, sweepWindupRot, windup);

            // Tajo barriendo horizontalmente
            currentState = FencerState.Attack;
            yield return AnimateArm(sweepAttackPos, sweepAttackRot, 0.22f);

            // Recuperación
            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.3f);
        }

        private IEnumerator ExecuteOverheadChop()
        {
            // Telegrafiado vertical hacia arriba
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration * 1.2f : 0.9f;
            yield return AnimateArm(overheadWindupPos, overheadWindupRot, windup);

            // Golpe descendente pesado
            currentState = FencerState.Attack;
            yield return AnimateArm(overheadAttackPos, overheadAttackRot, 0.20f);

            // Recuperación
            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.35f);
        }

        private IEnumerator ExecuteDoubleCombo()
        {
            // GOLPE 1: Tajo lateral rápido
            currentState = FencerState.Windup;
            yield return AnimateArm(sweepWindupPos, sweepWindupRot, 0.55f);

            currentState = FencerState.Attack;
            yield return AnimateArm(sweepAttackPos, sweepAttackRot, 0.18f);

            // Breve transición rápida entre golpes del combo
            yield return new WaitForSeconds(0.12f);

            // GOLPE 2: Estocada directa a fondo
            currentState = FencerState.Windup;
            yield return AnimateArm(thrustWindupPos, thrustWindupRot, 0.35f);

            currentState = FencerState.Attack;
            yield return AnimateArm(thrustAttackPos, thrustAttackRot, 0.16f);

            // Regreso a guardia
            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.3f);
        }

        private void HandleSwordClashed(Vector3 clashPoint)
        {
            // Si el jugador intercepta el golpe de la IA con su espada -> ¡BLOQUEO Y PARRIED!
            if (currentState == FencerState.Attack || currentState == FencerState.Windup)
            {
                if (behaviorRoutine != null) StopCoroutine(behaviorRoutine);
                StartCoroutine(StunRoutine());
            }
        }

        /// <summary>
        /// Ventana de vulnerabilidad tras ser bloqueado: el rival queda aturdido con la guardia abierta,
        /// permitiendo al jugador infligir un contragolpe crítico con empuje multiplicado hacia el abismo.
        /// </summary>
        private IEnumerator StunRoutine()
        {
            currentState = FencerState.Staggered;

            if (EsgrimaMatchManager.Instance != null)
            {
                EsgrimaMatchManager.Instance.AnnounceCombatBanner("¡BLOQUEO EXITOSO! ¡CONTRAATACA!");
            }

            // Animación de choque: el brazo y sable son repelidos hacia atrás
            Vector3 stunnedArmPos = new Vector3(0.45f, 0.9f, 0.05f);
            Vector3 stunnedArmRot = new Vector3(-25f, 45f, 0f);
            yield return AnimateArm(stunnedArmPos, stunnedArmRot, 0.1f);

            float staggerTime = config != null ? config.staggerDuration : 1.4f;
            yield return new WaitForSeconds(staggerTime);

            // Si no fue derrotado, reanudar combate
            if (currentState != FencerState.Down)
            {
                behaviorRoutine = StartCoroutine(AIBehaviorLoop());
            }
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
