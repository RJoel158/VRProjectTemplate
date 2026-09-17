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

        [Header("Poses: Estocada Frontal")]
        [SerializeField] private Vector3 thrustWindupPos = new Vector3(0.28f, 1.25f, 0.15f);
        [SerializeField] private Vector3 thrustWindupRot = new Vector3(15f, -5f, 0f);
        [SerializeField] private Vector3 thrustAttackPos = new Vector3(0.0f, 1.2f, 1.25f);
        [SerializeField] private Vector3 thrustAttackRot = new Vector3(5f, 0f, 0f);

        [Header("Poses: Corte Horizontal Derecha -> Izquierda")]
        [SerializeField] private Vector3 sweepRightWindupPos = new Vector3(0.48f, 1.25f, 0.25f);
        [SerializeField] private Vector3 sweepRightWindupRot = new Vector3(15f, 75f, 0f);
        [SerializeField] private Vector3 sweepRightAttackPos = new Vector3(-0.45f, 1.2f, 0.95f);
        [SerializeField] private Vector3 sweepRightAttackRot = new Vector3(15f, -70f, 0f);

        [Header("Poses: Corte Horizontal Izquierda -> Derecha")]
        [SerializeField] private Vector3 sweepLeftWindupPos = new Vector3(-0.45f, 1.25f, 0.3f);
        [SerializeField] private Vector3 sweepLeftWindupRot = new Vector3(15f, -75f, 0f);
        [SerializeField] private Vector3 sweepLeftAttackPos = new Vector3(0.48f, 1.2f, 0.95f);
        [SerializeField] private Vector3 sweepLeftAttackRot = new Vector3(15f, 70f, 0f);

        [Header("Poses: Corte Descendente Vertical")]
        [SerializeField] private Vector3 overheadWindupPos = new Vector3(0.1f, 1.75f, 0.05f);
        [SerializeField] private Vector3 overheadWindupRot = new Vector3(85f, 0f, 0f);
        [SerializeField] private Vector3 overheadAttackPos = new Vector3(0.05f, 0.8f, 1.05f);
        [SerializeField] private Vector3 overheadAttackRot = new Vector3(-35f, 0f, 0f);

        [Header("Poses: Corte Ascendente Derecho (Gancho Inferior)")]
        [SerializeField] private Vector3 risingRightWindupPos = new Vector3(0.35f, 0.65f, 0.3f);
        [SerializeField] private Vector3 risingRightWindupRot = new Vector3(-50f, 30f, 0f);
        [SerializeField] private Vector3 risingRightAttackPos = new Vector3(-0.1f, 1.55f, 1.05f);
        [SerializeField] private Vector3 risingRightAttackRot = new Vector3(65f, -25f, 0f);

        [Header("Poses: Corte Ascendente Izquierdo")]
        [SerializeField] private Vector3 risingLeftWindupPos = new Vector3(-0.35f, 0.65f, 0.3f);
        [SerializeField] private Vector3 risingLeftWindupRot = new Vector3(-50f, -30f, 0f);
        [SerializeField] private Vector3 risingLeftAttackPos = new Vector3(0.15f, 1.55f, 1.05f);
        [SerializeField] private Vector3 risingLeftAttackRot = new Vector3(65f, 25f, 0f);

        [Header("Poses: Corte Diagonal Descendente Derecho (Oblicuo)")]
        [SerializeField] private Vector3 diagRightWindupPos = new Vector3(0.42f, 1.6f, 0.15f);
        [SerializeField] private Vector3 diagRightWindupRot = new Vector3(70f, 50f, 0f);
        [SerializeField] private Vector3 diagRightAttackPos = new Vector3(-0.38f, 0.85f, 1.0f);
        [SerializeField] private Vector3 diagRightAttackRot = new Vector3(-25f, -45f, 0f);

        [Header("Poses: Corte Diagonal Descendente Izquierdo (Oblicuo)")]
        [SerializeField] private Vector3 diagLeftWindupPos = new Vector3(-0.42f, 1.6f, 0.15f);
        [SerializeField] private Vector3 diagLeftWindupRot = new Vector3(70f, -50f, 0f);
        [SerializeField] private Vector3 diagLeftAttackPos = new Vector3(0.38f, 0.85f, 1.0f);
        [SerializeField] private Vector3 diagLeftAttackRot = new Vector3(-25f, 45f, 0f);

        private FencerState currentState = FencerState.Idle;
        private Coroutine behaviorRoutine;
        private Coroutine stunRoutine;
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
            if (currentState == FencerState.Down || currentState == FencerState.Idle) return;

            // Mantener referencia al jugador actualizada dinámicamente
            if (targetPlayer == null && Camera.main != null)
            {
                targetPlayer = Camera.main.transform;
            }

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

            // 3. Delimitación de seguridad del Ring para el paso voluntario (la IA no camina fuera del ring por sí sola)
            Vector3 currentPos = transform.position;
            Vector2 hPos = new Vector2(currentPos.x, currentPos.z);
            float safeLimit = config != null ? config.maxSafeArenaRadius : 3.6f;

            // Si su paso lo acerca al borde, redirigir su caminata hacia el centro (0, 0)
            if (hPos.magnitude > safeLimit * 0.85f)
            {
                Vector3 toCenter = -new Vector3(currentPos.x, 0, currentPos.z).normalized;
                moveDir = Vector3.Lerp(moveDir, toCenter * baseSpeed, 0.85f);
            }

            // Aplicar traslación horizontal de los pasos del rival
            // (NO recortar transform.position para permitir que los impactos del jugador lo empujen fuera del ring)
            transform.position += moveDir * Time.deltaTime;
        }

        public void StartAI()
        {
            StopAllCoroutines();
            behaviorRoutine = null;
            stunRoutine = null;
            currentState = FencerState.Guard;
            behaviorRoutine = StartCoroutine(AIBehaviorLoop());
        }

        public void StopAI()
        {
            StopAllCoroutines();
            behaviorRoutine = null;
            stunRoutine = null;
            currentState = FencerState.Idle;
        }

        public void ResetFencer()
        {
            StopAllCoroutines();
            behaviorRoutine = null;
            stunRoutine = null;
            currentState = FencerState.Guard;
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

                // 2. Selección variada de ataque entre 9 opciones de combate
                int attackRoll = Random.Range(0, 9);
                switch (attackRoll)
                {
                    case 0:
                        yield return ExecuteHorizontalSweepRightToLeft();
                        break;
                    case 1:
                        yield return ExecuteHorizontalSweepLeftToRight();
                        break;
                    case 2:
                        yield return ExecuteOverheadChop();
                        break;
                    case 3:
                        yield return ExecuteRisingSlashRight();
                        break;
                    case 4:
                        yield return ExecuteRisingSlashLeft();
                        break;
                    case 5:
                        yield return ExecuteDiagonalChopRight();
                        break;
                    case 6:
                        yield return ExecuteDiagonalChopLeft();
                        break;
                    case 7:
                        yield return ExecuteQuickThrust();
                        break;
                    case 8:
                        yield return ExecuteDoubleCombo();
                        break;
                }

                // Pausa post-acción
                yield return new WaitForSeconds(0.4f);
            }
        }

        // 1. Estocada Frontal Rápida
        private IEnumerator ExecuteQuickThrust()
        {
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration : 0.7f;
            yield return AnimateArm(thrustWindupPos, thrustWindupRot, windup);

            currentState = FencerState.Attack;
            yield return AnimateArm(thrustAttackPos, thrustAttackRot, 0.18f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.25f);
        }

        // 2. Corte Horizontal Derecha -> Izquierda (Forehand)
        private IEnumerator ExecuteHorizontalSweepRightToLeft()
        {
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration * 1.05f : 0.75f;
            yield return AnimateArm(sweepRightWindupPos, sweepRightWindupRot, windup);

            currentState = FencerState.Attack;
            yield return AnimateArm(sweepRightAttackPos, sweepRightAttackRot, 0.20f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.3f);
        }

        // 3. Corte Horizontal Izquierda -> Derecha (Backhand)
        private IEnumerator ExecuteHorizontalSweepLeftToRight()
        {
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration * 1.05f : 0.75f;
            yield return AnimateArm(sweepLeftWindupPos, sweepLeftWindupRot, windup);

            currentState = FencerState.Attack;
            yield return AnimateArm(sweepLeftAttackPos, sweepLeftAttackRot, 0.20f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.3f);
        }

        // 4. Corte Descendente Vertical (Overhead Slam)
        private IEnumerator ExecuteOverheadChop()
        {
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration * 1.15f : 0.85f;
            yield return AnimateArm(overheadWindupPos, overheadWindupRot, windup);

            currentState = FencerState.Attack;
            yield return AnimateArm(overheadAttackPos, overheadAttackRot, 0.18f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.35f);
        }

        // 5. Corte Ascendente Derecho (Uppercut / Gancho inferior derecho)
        private IEnumerator ExecuteRisingSlashRight()
        {
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration : 0.7f;
            yield return AnimateArm(risingRightWindupPos, risingRightWindupRot, windup);

            currentState = FencerState.Attack;
            yield return AnimateArm(risingRightAttackPos, risingRightAttackRot, 0.18f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.3f);
        }

        // 6. Corte Ascendente Izquierdo (Uppercut / Gancho inferior izquierdo)
        private IEnumerator ExecuteRisingSlashLeft()
        {
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration : 0.7f;
            yield return AnimateArm(risingLeftWindupPos, risingLeftWindupRot, windup);

            currentState = FencerState.Attack;
            yield return AnimateArm(risingLeftAttackPos, risingLeftAttackRot, 0.18f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.3f);
        }

        // 7. Corte Oblicuo / Diagonal Descendente Derecho (Top-Right a Bottom-Left)
        private IEnumerator ExecuteDiagonalChopRight()
        {
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration * 1.1f : 0.8f;
            yield return AnimateArm(diagRightWindupPos, diagRightWindupRot, windup);

            currentState = FencerState.Attack;
            yield return AnimateArm(diagRightAttackPos, diagRightAttackRot, 0.20f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.3f);
        }

        // 8. Corte Oblicuo / Diagonal Descendente Izquierdo (Top-Left a Bottom-Right)
        private IEnumerator ExecuteDiagonalChopLeft()
        {
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration * 1.1f : 0.8f;
            yield return AnimateArm(diagLeftWindupPos, diagLeftWindupRot, windup);

            currentState = FencerState.Attack;
            yield return AnimateArm(diagLeftAttackPos, diagLeftAttackRot, 0.20f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.3f);
        }

        // 9. Combo Dinámico de 2 Golpes
        private IEnumerator ExecuteDoubleCombo()
        {
            // Golpe 1: Tajo horizontal o diagonal
            bool startRight = Random.value > 0.5f;
            Vector3 wPos = startRight ? sweepRightWindupPos : sweepLeftWindupPos;
            Vector3 wRot = startRight ? sweepRightWindupRot : sweepLeftWindupRot;
            Vector3 aPos = startRight ? sweepRightAttackPos : sweepLeftAttackPos;
            Vector3 aRot = startRight ? sweepRightAttackRot : sweepLeftAttackRot;

            currentState = FencerState.Windup;
            yield return AnimateArm(wPos, wRot, 0.55f);

            currentState = FencerState.Attack;
            yield return AnimateArm(aPos, aRot, 0.18f);

            // Breve intervalo entre golpes
            yield return new WaitForSeconds(0.12f);

            // Golpe 2: Estocada directa o corte ascendente
            if (Random.value > 0.5f)
            {
                currentState = FencerState.Windup;
                yield return AnimateArm(thrustWindupPos, thrustWindupRot, 0.35f);

                currentState = FencerState.Attack;
                yield return AnimateArm(thrustAttackPos, thrustAttackRot, 0.16f);
            }
            else
            {
                currentState = FencerState.Windup;
                yield return AnimateArm(risingRightWindupPos, risingRightWindupRot, 0.35f);

                currentState = FencerState.Attack;
                yield return AnimateArm(risingRightAttackPos, risingRightAttackRot, 0.18f);
            }

            // Regreso a guardia
            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.3f);
        }

        private void HandleSwordClashed(Vector3 clashPoint)
        {
            // Si el jugador intercepta el golpe de la IA con su espada -> ¡BLOQUEO Y PARRIED!
            if (currentState == FencerState.Attack || currentState == FencerState.Windup)
            {
                if (behaviorRoutine != null) StopCoroutine(behaviorRoutine);
                if (stunRoutine != null) StopCoroutine(stunRoutine);
                stunRoutine = StartCoroutine(StunRoutine());
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

            // Si no fue derrotado ni el manager detuvo el combate, reanudar
            if (currentState != FencerState.Down && currentState != FencerState.Idle)
            {
                yield return AnimateArm(guardLocalPos, guardLocalRot, 0.25f);
                currentState = FencerState.Guard;
                behaviorRoutine = StartCoroutine(AIBehaviorLoop());
            }
            stunRoutine = null;
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
