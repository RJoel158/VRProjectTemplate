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
            lastRestPosition = transform.position;
            lastRestRotation = transform.rotation;
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

            float speed = rb.linearVelocity.magnitude;

            // Comprobar si la bola esta en reposo
            if (currentState == BallState.Rolling || currentState == BallState.Airborne)
            {
                float threshold = config != null ? config.SleepVelocityThreshold : 0.05f;

                if (speed <= threshold)
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

            // Comprobar limite de caida al vacio / agua
            if (transform.position.y < -5f && currentState != BallState.OutOfBounds)
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

            rb.isKinematic = false;
            rb.linearVelocity = velocity;
            currentState = BallState.Rolling;
            restCheckTimer = 0f;

            if (trailRenderer != null) trailRenderer.Clear();

            OnBallHit?.Invoke(this, velocity);
        }

        public void SetAtRest()
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            currentState = BallState.AtRest;
            restCheckTimer = 0f;
            lastRestPosition = transform.position;
            lastRestRotation = transform.rotation;

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
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;

            yield return new WaitForSeconds(0.8f);

            // Recolocar en la posicion previa con una leve elevacion para evitar atascos
            transform.position = lastRestPosition + Vector3.up * 0.02f;
            transform.rotation = lastRestRotation;

            rb.isKinematic = false;
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
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = newPosition;
            transform.rotation = newRotation;
            lastRestPosition = newPosition;
            lastRestRotation = newRotation;
            currentState = BallState.AtRest;
            restCheckTimer = 0f;

            if (trailRenderer != null) trailRenderer.Clear();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (isRespawning || currentState == BallState.InCup) return;

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
