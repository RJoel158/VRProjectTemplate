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
    /// Inteligencia Artificial dinámica y de alto rendimiento para el rival de esgrima en VR (estilo Wii Sports Resort).
    /// Cuenta con:
    /// 1. Seguimiento activo 360° del visor VR del jugador (imposible flanquearlo al caminar alrededor).
    /// 2. Sistema reactivo de Bloqueo / Parry defensivo con contraataques relámpago (Ripostes).
    /// 3. Embestidas y estocadas con avance físico (Lunges) y pasos atrás de recuperación.
    /// 4. 10 variantes de ataque: estocadas, tajos en 4 direcciones, ascendentes, diagonales y combos.
    /// 5. Desplazamiento táctico y delimite seguro de arena para no caer al agua por movimiento propio.
    /// </summary>
    public class FencerAI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private FighterConfigSO config;
        [SerializeField] private Transform targetPlayer;
        [SerializeField] private VRSword playerSword;

        [Header("Sword Setup")]
        [SerializeField] private VRSword aiSword;
        [SerializeField] private Transform swordArmPivot;

        [Header("Poses Base y Guardia")]
        [SerializeField] private Vector3 guardLocalPos = new Vector3(0.25f, 1.2f, 0.45f);
        [SerializeField] private Vector3 guardLocalRot = new Vector3(25f, -10f, 0f);

        [Header("Poses: Bloqueo Defensivo (Parry)")]
        [SerializeField] private Vector3 highBlockPos = new Vector3(0.05f, 1.55f, 0.5f);
        [SerializeField] private Vector3 highBlockRot = new Vector3(15f, 0f, 85f);
        [SerializeField] private Vector3 leftBlockPos = new Vector3(-0.35f, 1.25f, 0.52f);
        [SerializeField] private Vector3 leftBlockRot = new Vector3(65f, -50f, 0f);
        [SerializeField] private Vector3 rightBlockPos = new Vector3(0.38f, 1.25f, 0.52f);
        [SerializeField] private Vector3 rightBlockRot = new Vector3(65f, 50f, 0f);

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

        [Header("Poses: Corte Diagonal Descendente Derecho")]
        [SerializeField] private Vector3 diagRightWindupPos = new Vector3(0.42f, 1.6f, 0.15f);
        [SerializeField] private Vector3 diagRightWindupRot = new Vector3(70f, 50f, 0f);
        [SerializeField] private Vector3 diagRightAttackPos = new Vector3(-0.38f, 0.85f, 1.0f);
        [SerializeField] private Vector3 diagRightAttackRot = new Vector3(-25f, -45f, 0f);

        [Header("Poses: Corte Diagonal Descendente Izquierdo")]
        [SerializeField] private Vector3 diagLeftWindupPos = new Vector3(-0.42f, 1.6f, 0.15f);
        [SerializeField] private Vector3 diagLeftWindupRot = new Vector3(70f, -50f, 0f);
        [SerializeField] private Vector3 diagLeftAttackPos = new Vector3(0.38f, 0.85f, 1.0f);
        [SerializeField] private Vector3 diagLeftAttackRot = new Vector3(-25f, 45f, 0f);

        private FencerState currentState = FencerState.Idle;
        private Coroutine behaviorRoutine;
        private Coroutine stunRoutine;
        private Coroutine defensiveRoutine;
        private Vector3 initialPosition;
        private Quaternion initialRotation;

        // Variables de desplazamiento y guardia reactiva
        private float strafeTimer = 0f;
        private float currentStrafeSign = 1f;
        private float blockCooldownTimer = 0f;

        public FencerState CurrentState => currentState;
        public bool IsStunned => currentState == FencerState.Staggered;

        private void Awake()
        {
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void Start()
        {
            FindPlayerReferences();

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

        private void FindPlayerReferences()
        {
            if (targetPlayer == null && Camera.main != null)
            {
                targetPlayer = Camera.main.transform;
            }

            if (playerSword == null)
            {
                var allSwords = FindObjectsByType<VRSword>(FindObjectsSortMode.None);
                foreach (var s in allSwords)
                {
                    if (s.IsPlayerSword)
                    {
                        playerSword = s;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene la posición física actual del jugador en el espacio del mundo real (HMD del visor VR).
        /// </summary>
        private Vector3 GetPlayerWorldPosition()
        {
            if (Camera.main != null)
            {
                return Camera.main.transform.position;
            }
            if (targetPlayer != null)
            {
                return targetPlayer.position;
            }
            return transform.position + transform.forward * 2f;
        }

        private void Update()
        {
            if (currentState == FencerState.Down || currentState == FencerState.Idle) return;

            if (blockCooldownTimer > 0f)
            {
                blockCooldownTimer -= Time.deltaTime;
            }

            // 1. Seguimiento activo e instantáneo de la orientación hacia el jugador
            UpdateRotationTracking();

            // 2. Comprobación de defensa reactiva cuando está en guardia
            if (currentState == FencerState.Guard)
            {
                CheckReactiveDefense();
                UpdateArenaFootwork();
            }
        }

        /// <summary>
        /// Rota rápidamente hacia el jugador para que nunca quede desorientado si el jugador camina a su alrededor.
        /// </summary>
        private void UpdateRotationTracking()
        {
            Vector3 playerPos = GetPlayerWorldPosition();
            Vector3 lookTarget = playerPos;
            lookTarget.y = transform.position.y;
            Vector3 dir = lookTarget - transform.position;

            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir);
                float angleDiff = Quaternion.Angle(transform.rotation, targetRot);

                // Si el jugador se ha movido mucho de lado (> 40°), girar de forma inmediata y atlética
                float turnSpeed = angleDiff > 40f ? 26f : (currentState == FencerState.Attack ? 16f : 18f);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
            }
        }

        /// <summary>
        /// Alinea de golpe la orientación del rival directamente hacia el pecho del jugador antes de atacar.
        /// </summary>
        private void SnapFacePlayer()
        {
            Vector3 playerPos = GetPlayerWorldPosition();
            playerPos.y = transform.position.y;
            Vector3 dir = playerPos - transform.position;
            if (dir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }

        /// <summary>
        /// Detecta si la espada del jugador se acerca rápidamente en trayectoria de ataque
        /// y levanta una guardia perpendicular para bloquearla al estilo Wii Sports Resort.
        /// </summary>
        private void CheckReactiveDefense()
        {
            if (playerSword == null || blockCooldownTimer > 0f) return;

            Vector3 swordPos = playerSword.transform.position;
            float distToSword = Vector3.Distance(transform.position, swordPos);
            float bladeSpeed = playerSword.CurrentBladeSpeed;

            // Si la espada del jugador entra en rango cercano (menos de 1.35m) a velocidad de golpe (> 0.4 m/s)
            if (distToSword < 1.35f && bladeSpeed > 0.4f)
            {
                float chance = config != null ? config.blockChance : 0.55f;
                if (Random.value < chance)
                {
                    blockCooldownTimer = 1.25f; // Evitar bloqueo infinito continuo
                    if (defensiveRoutine != null) StopCoroutine(defensiveRoutine);
                    defensiveRoutine = StartCoroutine(ExecuteDefensiveBlock(swordPos));
                }
            }
        }

        /// <summary>
        /// Eleva rápidamente la espada del rival para interceptar el golpe entrante.
        /// </summary>
        private IEnumerator ExecuteDefensiveBlock(Vector3 incomingBladePos)
        {
            Vector3 localIncoming = transform.InverseTransformPoint(incomingBladePos);

            Vector3 targetBlockPos;
            Vector3 targetBlockRot;

            // Bloqueo superior si el corte viene de arriba
            if (localIncoming.y > 1.4f)
            {
                targetBlockPos = highBlockPos;
                targetBlockRot = highBlockRot;
            }
            // Bloqueo izquierdo si viene por su izquierda
            else if (localIncoming.x < 0f)
            {
                targetBlockPos = leftBlockPos;
                targetBlockRot = leftBlockRot;
            }
            // Bloqueo derecho si viene por su derecha
            else
            {
                targetBlockPos = rightBlockPos;
                targetBlockRot = rightBlockRot;
            }

            // Movimiento defensivo rápido (0.12s)
            yield return AnimateArm(targetBlockPos, targetBlockRot, 0.12f);

            // Mantener guardia durante 0.4s
            yield return new WaitForSeconds(0.40f);

            // Volver suavemente a la guardia base si no fue aturdido
            if (currentState == FencerState.Guard)
            {
                yield return AnimateArm(guardLocalPos, guardLocalRot, 0.18f);
            }

            defensiveRoutine = null;
        }

        /// <summary>
        /// Mueve al rival con pasos tácticos de esgrima:
        /// se aproxima si el jugador retrocede, retrocede si el jugador invade su espacio,
        /// y se desplaza lateralmente sin cruzar el límite seguro de la plataforma.
        /// </summary>
        private void UpdateArenaFootwork()
        {
            Vector3 playerPos = GetPlayerWorldPosition();
            Vector3 toPlayer = playerPos - transform.position;
            toPlayer.y = 0;
            float dist = toPlayer.magnitude;
            Vector3 forwardDir = toPlayer.normalized;
            Vector3 rightDir = Vector3.Cross(Vector3.up, forwardDir);

            Vector3 moveDir = Vector3.zero;
            float baseSpeed = config != null ? config.moveSpeed : 1.8f;
            float strafeSpd = config != null ? config.strafeSpeed : 1.3f;

            // 1. Control dinámico de distancia de combate (óptima: 1.4m a 2.3m)
            if (dist > 2.3f)
            {
                moveDir += forwardDir * baseSpeed;
            }
            else if (dist < 1.35f)
            {
                moveDir -= forwardDir * (baseSpeed * 1.15f);
            }

            // 2. Movimiento lateral (Strafe) alternando lados
            strafeTimer -= Time.deltaTime;
            if (strafeTimer <= 0f)
            {
                currentStrafeSign = Random.value > 0.5f ? 1f : -1f;
                strafeTimer = Random.Range(1.2f, 2.5f);
            }
            moveDir += rightDir * (currentStrafeSign * strafeSpd);

            // 3. Delimitación de seguridad del Ring para el paso voluntario
            Vector3 currentPos = transform.position;
            Vector2 hPos = new Vector2(currentPos.x, currentPos.z);
            float safeLimit = config != null ? config.maxSafeArenaRadius : 3.8f;

            // Si su paso voluntario lo acerca al borde, redirigir hacia el centro
            if (hPos.magnitude > safeLimit * 0.85f)
            {
                Vector3 toCenter = -new Vector3(currentPos.x, 0, currentPos.z).normalized;
                moveDir = Vector3.Lerp(moveDir, toCenter * baseSpeed, 0.85f);
            }

            transform.position += moveDir * Time.deltaTime;
        }

        public void StartAI()
        {
            StopAllCoroutines();
            behaviorRoutine = null;
            stunRoutine = null;
            defensiveRoutine = null;
            currentState = FencerState.Guard;
            behaviorRoutine = StartCoroutine(AIBehaviorLoop());
        }

        public void StopAI()
        {
            StopAllCoroutines();
            behaviorRoutine = null;
            stunRoutine = null;
            defensiveRoutine = null;
            currentState = FencerState.Idle;
        }

        public void ResetFencer()
        {
            StopAllCoroutines();
            behaviorRoutine = null;
            stunRoutine = null;
            defensiveRoutine = null;
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
                yield return AnimateArm(guardLocalPos, guardLocalRot, 0.2f);

                float waitTime = Random.Range(
                    config != null ? config.attackIntervalMin : 1.2f,
                    config != null ? config.attackIntervalMax : 2.2f
                );
                yield return new WaitForSeconds(waitTime);

                // Asegurarse de que no fue interrumpido por aturdimiento o defensa
                if (currentState != FencerState.Guard) continue;

                // 2. Selección variada entre 10 patrones de ataque
                int attackRoll = Random.Range(0, 10);
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
                    case 9:
                        yield return ExecuteFeintThrust();
                        break;
                }

                // Pausa post-acción antes de la siguiente iniciativa
                yield return new WaitForSeconds(0.25f);
            }
        }

        /// <summary>
        /// Realiza una embestida / avance físico de esgrima (Lunge) hacia adelante al lanzar un ataque.
        /// </summary>
        private IEnumerator PerformLunge(float distance, float duration)
        {
            Vector3 startPos = transform.position;
            Vector3 targetPos = startPos + transform.forward * distance;

            // Mantenerse dentro de la plataforma segura
            float maxR = config != null ? config.maxSafeArenaRadius : 3.8f;
            if (new Vector2(targetPos.x, targetPos.z).magnitude > maxR)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
        }

        // 1. Estocada Frontal Rápida con lunge
        private IEnumerator ExecuteQuickThrust()
        {
            SnapFacePlayer();
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration : 0.45f;
            yield return AnimateArm(thrustWindupPos, thrustWindupRot, windup);

            SnapFacePlayer();
            currentState = FencerState.Attack;
            StartCoroutine(PerformLunge(0.40f, 0.16f));
            yield return AnimateArm(thrustAttackPos, thrustAttackRot, 0.16f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.22f);
        }

        // 2. Corte Horizontal Derecha -> Izquierda (Forehand)
        private IEnumerator ExecuteHorizontalSweepRightToLeft()
        {
            SnapFacePlayer();
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration * 1.05f : 0.48f;
            yield return AnimateArm(sweepRightWindupPos, sweepRightWindupRot, windup);

            SnapFacePlayer();
            currentState = FencerState.Attack;
            StartCoroutine(PerformLunge(0.35f, 0.18f));
            yield return AnimateArm(sweepRightAttackPos, sweepRightAttackRot, 0.18f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.25f);
        }

        // 3. Corte Horizontal Izquierda -> Derecha (Backhand)
        private IEnumerator ExecuteHorizontalSweepLeftToRight()
        {
            SnapFacePlayer();
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration * 1.05f : 0.48f;
            yield return AnimateArm(sweepLeftWindupPos, sweepLeftWindupRot, windup);

            SnapFacePlayer();
            currentState = FencerState.Attack;
            StartCoroutine(PerformLunge(0.35f, 0.18f));
            yield return AnimateArm(sweepLeftAttackPos, sweepLeftAttackRot, 0.18f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.25f);
        }

        // 4. Corte Descendente Vertical (Overhead Slam)
        private IEnumerator ExecuteOverheadChop()
        {
            SnapFacePlayer();
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration * 1.15f : 0.52f;
            yield return AnimateArm(overheadWindupPos, overheadWindupRot, windup);

            SnapFacePlayer();
            currentState = FencerState.Attack;
            StartCoroutine(PerformLunge(0.38f, 0.16f));
            yield return AnimateArm(overheadAttackPos, overheadAttackRot, 0.16f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.28f);
        }

        // 5. Corte Ascendente Derecho (Gancho Inferior)
        private IEnumerator ExecuteRisingSlashRight()
        {
            SnapFacePlayer();
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration : 0.45f;
            yield return AnimateArm(risingRightWindupPos, risingRightWindupRot, windup);

            SnapFacePlayer();
            currentState = FencerState.Attack;
            StartCoroutine(PerformLunge(0.32f, 0.16f));
            yield return AnimateArm(risingRightAttackPos, risingRightAttackRot, 0.16f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.24f);
        }

        // 6. Corte Ascendente Izquierdo
        private IEnumerator ExecuteRisingSlashLeft()
        {
            SnapFacePlayer();
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration : 0.45f;
            yield return AnimateArm(risingLeftWindupPos, risingLeftWindupRot, windup);

            SnapFacePlayer();
            currentState = FencerState.Attack;
            StartCoroutine(PerformLunge(0.32f, 0.16f));
            yield return AnimateArm(risingLeftAttackPos, risingLeftAttackRot, 0.16f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.24f);
        }

        // 7. Corte Oblicuo / Diagonal Descendente Derecho
        private IEnumerator ExecuteDiagonalChopRight()
        {
            SnapFacePlayer();
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration * 1.08f : 0.48f;
            yield return AnimateArm(diagRightWindupPos, diagRightWindupRot, windup);

            SnapFacePlayer();
            currentState = FencerState.Attack;
            StartCoroutine(PerformLunge(0.35f, 0.17f));
            yield return AnimateArm(diagRightAttackPos, diagRightAttackRot, 0.17f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.25f);
        }

        // 8. Corte Oblicuo / Diagonal Descendente Izquierdo
        private IEnumerator ExecuteDiagonalChopLeft()
        {
            SnapFacePlayer();
            currentState = FencerState.Windup;
            float windup = config != null ? config.windupDuration * 1.08f : 0.48f;
            yield return AnimateArm(diagLeftWindupPos, diagLeftWindupRot, windup);

            SnapFacePlayer();
            currentState = FencerState.Attack;
            StartCoroutine(PerformLunge(0.35f, 0.17f));
            yield return AnimateArm(diagLeftAttackPos, diagLeftAttackRot, 0.17f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.25f);
        }

        // 9. Combo Dinámico de 2 Golpes
        private IEnumerator ExecuteDoubleCombo()
        {
            SnapFacePlayer();
            bool startRight = Random.value > 0.5f;
            Vector3 wPos = startRight ? sweepRightWindupPos : sweepLeftWindupPos;
            Vector3 wRot = startRight ? sweepRightWindupRot : sweepLeftWindupRot;
            Vector3 aPos = startRight ? sweepRightAttackPos : sweepLeftAttackPos;
            Vector3 aRot = startRight ? sweepRightAttackRot : sweepLeftAttackRot;

            // Golpe 1
            currentState = FencerState.Windup;
            yield return AnimateArm(wPos, wRot, 0.40f);

            SnapFacePlayer();
            currentState = FencerState.Attack;
            StartCoroutine(PerformLunge(0.30f, 0.15f));
            yield return AnimateArm(aPos, aRot, 0.15f);

            yield return new WaitForSeconds(0.08f);

            // Golpe 2: Estocada o uppercut
            SnapFacePlayer();
            if (Random.value > 0.5f)
            {
                currentState = FencerState.Windup;
                yield return AnimateArm(thrustWindupPos, thrustWindupRot, 0.28f);

                SnapFacePlayer();
                currentState = FencerState.Attack;
                StartCoroutine(PerformLunge(0.32f, 0.14f));
                yield return AnimateArm(thrustAttackPos, thrustAttackRot, 0.14f);
            }
            else
            {
                currentState = FencerState.Windup;
                yield return AnimateArm(risingRightWindupPos, risingRightWindupRot, 0.28f);

                SnapFacePlayer();
                currentState = FencerState.Attack;
                StartCoroutine(PerformLunge(0.32f, 0.15f));
                yield return AnimateArm(risingRightAttackPos, risingRightAttackRot, 0.15f);
            }

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.25f);
        }

        // 10. Amago con estocada relámpago (Feint)
        private IEnumerator ExecuteFeintThrust()
        {
            SnapFacePlayer();
            currentState = FencerState.Windup;
            // Amago a la izquierda
            yield return AnimateArm(sweepLeftWindupPos, sweepLeftWindupRot, 0.22f);

            // Cambio brusco a estocada directa
            SnapFacePlayer();
            currentState = FencerState.Attack;
            StartCoroutine(PerformLunge(0.42f, 0.15f));
            yield return AnimateArm(thrustAttackPos, thrustAttackRot, 0.15f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.25f);
        }

        private void HandleSwordClashed(Vector3 clashPoint)
        {
            // Caso 1: El jugador interceptó el golpe del rival -> ¡PARRY DEL JUGADOR!
            if (currentState == FencerState.Attack || currentState == FencerState.Windup)
            {
                if (behaviorRoutine != null) StopCoroutine(behaviorRoutine);
                if (stunRoutine != null) StopCoroutine(stunRoutine);
                if (defensiveRoutine != null) StopCoroutine(defensiveRoutine);
                stunRoutine = StartCoroutine(StunRoutine());
            }
            // Caso 2: El rival estaba en guardia/bloqueo -> ¡EL RIVAL BLOQUEÓ AL JUGADOR!
            else if (currentState == FencerState.Guard)
            {
                if (EsgrimaMatchManager.Instance != null)
                {
                    EsgrimaMatchManager.Instance.AnnounceCombatBanner("¡EL RIVAL BLOQUEÓ TU ATAQUE!");
                }

                if (behaviorRoutine != null) StopCoroutine(behaviorRoutine);
                if (defensiveRoutine != null) StopCoroutine(defensiveRoutine);
                behaviorRoutine = StartCoroutine(RiposteRoutine());
            }
        }

        /// <summary>
        /// Contraataque inmediato (Riposte) del rival tras bloquear con éxito al jugador.
        /// </summary>
        private IEnumerator RiposteRoutine()
        {
            SnapFacePlayer();
            currentState = FencerState.Windup;
            yield return AnimateArm(thrustWindupPos, thrustWindupRot, 0.18f);

            SnapFacePlayer();
            currentState = FencerState.Attack;
            StartCoroutine(PerformLunge(0.35f, 0.14f));
            yield return AnimateArm(thrustAttackPos, thrustAttackRot, 0.14f);

            yield return AnimateArm(guardLocalPos, guardLocalRot, 0.22f);

            currentState = FencerState.Guard;
            behaviorRoutine = StartCoroutine(AIBehaviorLoop());
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

            // Animación de retroceso del brazo: sable repelido hacia atrás con pecho descubierto
            Vector3 stunnedArmPos = new Vector3(0.45f, 0.9f, 0.05f);
            Vector3 stunnedArmRot = new Vector3(-25f, 45f, 0f);
            yield return AnimateArm(stunnedArmPos, stunnedArmRot, 0.10f);

            float staggerTime = config != null ? config.staggerDuration : 1.3f;
            yield return new WaitForSeconds(staggerTime);

            // Reanudar combate si no terminó la ronda
            if (currentState != FencerState.Down && currentState != FencerState.Idle)
            {
                yield return AnimateArm(guardLocalPos, guardLocalRot, 0.22f);
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
