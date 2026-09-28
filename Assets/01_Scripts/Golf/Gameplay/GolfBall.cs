using System;
using System.Collections;
using UnityEngine;
using Golf.Data;
using Golf.Audio;

namespace Golf.Gameplay
{
    public enum BallState
    {
        AtRest,
        Rolling,
        Airborne,
        OutOfBounds,
        InCup
    }

    /// <summary>
    /// Controla la fisica, estados y comportamiento de la bola de minigolf.
    /// Registra la posicion del ultimo tiro para recolocacion en caso de fuera de pista (Out of Bounds),
    /// reproduce audio de rebote y rodamiento, y avisa cuando la bola vuelve al reposo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class GolfBall : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GolfBallSO config;

        [Header("Components")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private SphereCollider sphereCollider;
        [SerializeField] private TrailRenderer trailRenderer;

        private BallState currentState = BallState.AtRest;
        private Vector3 lastRestPosition;
        private Quaternion lastRestRotation;
        private float restCheckTimer = 0f;
        private const float MinRestDuration = 0.45f;
        private bool isRespawning = false;

        public BallState CurrentState => currentState;
        public Vector3 LastRestPosition => lastRestPosition;
        public bool IsAtRest => currentState == BallState.AtRest;
        public Rigidbody RigidbodyComponent => rb;

        public event Action<GolfBall, Vector3> OnBallHit; // (ball, hitVelocity)
        public event Action<GolfBall, Vector3> OnBallStopped; // (ball, restPosition)
        public event Action<GolfBall> OnBallOutOfBounds;
        public event Action<GolfBall> OnBallInCup;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (sphereCollider == null) sphereCollider = GetComponent<SphereCollider>();
            if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();

            ApplyConfig();

            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }

            lastRestPosition = transform.position;
            lastRestRotation = transform.rotation;
            currentState = BallState.AtRest;
        }

        public void ApplyConfig()
        {
            if (config != null && rb != null)
            {
                rb.mass = config.Mass;
                rb.angularDamping = config.AngularDrag;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                PhysicsMaterial mat = new PhysicsMaterial("GolfBallPhysMat")
                {
                    dynamicFriction = config.GrassDynamicFriction,
                    staticFriction = config.GrassStaticFriction,
                    bounciness = config.Bounciness,
                    frictionCombine = PhysicsMaterialCombine.Multiply,
                    bounceCombine = PhysicsMaterialCombine.Maximum
                };
                sphereCollider.material = mat;

                if (trailRenderer != null)
                {
                    trailRenderer.enabled = config.EnableTrail;
                    trailRenderer.startColor = config.TrailColor;
                    trailRenderer.endColor = new Color(config.TrailColor.r, config.TrailColor.g, config.TrailColor.b, 0f);
                    trailRenderer.time = config.TrailTime;
                }
            }
        }

        private void FixedUpdate()
        {
            if (isRespawning || currentState == BallState.InCup) return;

            // Comprobar si la bola esta en rodamiento
            if (currentState == BallState.Rolling)
            {
                float speed = rb.linearVelocity.magnitude;

                // Detectar si la bola está tocando el suelo mediante raycast hacia abajo
                bool onGround = Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, 0.038f, ~0, QueryTriggerInteraction.Ignore);

                if (onGround)
                {
                    // Friccion de rodamiento auténtica sobre césped sintético (deceleración ~1.35 m/s^2)
                    float decelRate = config != null ? (config.GrassDynamicFriction * 3.8f) : 1.35f;
                    float frictionDecel = decelRate * Time.fixedDeltaTime;

                    Vector3 currentLinear = rb.linearVelocity;
                    Vector3 horiz = new Vector3(currentLinear.x, 0f, currentLinear.z);
                    Vector3 slowedHoriz = Vector3.MoveTowards(horiz, Vector3.zero, frictionDecel);
                    rb.linearVelocity = new Vector3(slowedHoriz.x, currentLinear.y, slowedHoriz.z);

                    // Amortiguación angular para rodamiento suave y realista
                    rb.angularVelocity = Vector3.MoveTowards(rb.angularVelocity, Vector3.zero, 3.0f * Time.fixedDeltaTime);

                    float threshold = config != null ? config.SleepVelocityThreshold : 0.04f;
                    float slopeAngle = Vector3.Angle(groundHit.normal, Vector3.up);

                    // Solo entrar en reposo si la velocidad es muy baja y el terreno no tiene pendiente marcada (> 5 grados)
                    if (speed <= threshold && slopeAngle < 5f)
                    {
                        restCheckTimer += Time.fixedDeltaTime;
                        if (restCheckTimer >= MinRestDuration)
                        {
                            SetAtRest();
                        }
                    }
                    else
                    {
                        restCheckTimer = 0f;
                    }
                }
                else
                {
                    // En el aire (saltos o baches), la gravedad física pura actúa normalmente
                    restCheckTimer = 0f;
                }
            }

            // Comprobar limite de caida al vacio / agua
            if (transform.position.y < -1f && currentState != BallState.OutOfBounds && currentState != BallState.InCup)
            {
                TriggerOutOfBounds();
            }
        }

        /// <summary>
        /// Aplica el impulso del putter a la pelota e inicia el movimiento.
        /// </summary>
        public void ApplyPutterHit(Vector3 velocity)
        {
            if (isRespawning || currentState == BallState.InCup) return;

            // Guardar posicion previa al golpe para penalizaciones de fuera de pista
            lastRestPosition = transform.position;
            lastRestRotation = transform.rotation;

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = velocity;

                // Transmitir velocidad angular inmediata para inicio de rodamiento puro
                Vector3 rollAxis = Vector3.Cross(Vector3.up, velocity.normalized);
                float ballRadius = sphereCollider != null ? sphereCollider.radius * transform.lossyScale.x : 0.0225f;
                if (ballRadius > 0.001f)
                {
                    rb.angularVelocity = rollAxis * (velocity.magnitude / ballRadius);
                }
            }
            currentState = BallState.Rolling;
            restCheckTimer = 0f;

            if (trailRenderer != null) trailRenderer.Clear();

            Debug.Log($"[GolfBall] Tiro recibido: velocidad = {velocity.magnitude:F2} m/s, dir = {velocity.normalized}");
            OnBallHit?.Invoke(this, velocity);
        }

        public void SetAtRest()
        {
            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }
            currentState = BallState.AtRest;
            restCheckTimer = 0f;
            lastRestPosition = transform.position;
            lastRestRotation = transform.rotation;

            Debug.Log($"[GolfBall] Pelota detenida en reposo en: {transform.position}");
            OnBallStopped?.Invoke(this, transform.position);
        }

        public void TriggerOutOfBounds()
        {
            if (isRespawning || currentState == BallState.OutOfBounds || currentState == BallState.InCup) return;

            currentState = BallState.OutOfBounds;
            GolfAudioManager.PlayOutOfBounds();
            OnBallOutOfBounds?.Invoke(this);

            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            isRespawning = true;
            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }

            yield return new WaitForSeconds(0.8f);

            // Recolocar en la posicion previa con una leve elevacion para evitar atascos
            transform.position = lastRestPosition + Vector3.up * 0.02f;
            transform.rotation = lastRestRotation;

            currentState = BallState.AtRest;
            isRespawning = false;
            restCheckTimer = 0f;

            if (trailRenderer != null) trailRenderer.Clear();

            OnBallStopped?.Invoke(this, transform.position);
        }

        public void TriggerHoleInCup(Vector3 cupCenter)
        {
            if (currentState == BallState.InCup) return;

            currentState = BallState.InCup;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;

            StartCoroutine(AnimateDropIntoCup(cupCenter));
            OnBallInCup?.Invoke(this);
        }

        private IEnumerator AnimateDropIntoCup(Vector3 cupCenter)
        {
            Vector3 start = transform.position;
            Vector3 target = cupCenter + Vector3.down * 0.045f;
            float elapsed = 0f;
            float duration = 0.22f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }

            transform.position = target;
        }

        public void ResetToPosition(Vector3 newPosition, Quaternion newRotation)
        {
            StopAllCoroutines();
            isRespawning = false;
            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }
            transform.position = newPosition;
            transform.rotation = newRotation;
            lastRestPosition = newPosition;
            lastRestRotation = newRotation;
            currentState = BallState.AtRest;
            restCheckTimer = 0f;

            if (trailRenderer != null) trailRenderer.Clear();
            Debug.Log($"[GolfBall] Bola posicionada y fijada en reposo en {newPosition}");
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isRespawning || currentState == BallState.InCup) return;

            string cName = collision.gameObject.name.ToLower();
            string cTag = collision.gameObject.tag;

            // Detección de caída fuera de la pista (patio exterior, agua o zona no delimitada)
            if (cName.Contains("patio") || cName.Contains("ocean") || cName.Contains("water") || cTag == "OutOfBounds" || cTag == "Floor")
            {
                if (transform.position.y < -0.10f)
                {
                    TriggerOutOfBounds();
                    return;
                }
            }

            // Deteccion de rebote contra maderas o bordes
            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed > 0.4f)
            {
                if (collision.gameObject.CompareTag("WoodBumper") || collision.gameObject.name.Contains("Wood") || collision.gameObject.name.Contains("Bumper"))
                {
                    GolfAudioManager.PlayWoodBounce(impactSpeed);
                }
            }
        }
    }
}
