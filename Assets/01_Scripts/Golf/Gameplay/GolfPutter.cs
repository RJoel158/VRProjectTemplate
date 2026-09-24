using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Golf.Data;
using Golf.Audio;

namespace Golf.Gameplay
{
    /// <summary>
    /// Controlador del palo de minigolf (Putter) en Realidad Virtual.
    /// Calcula la velocidad angular y lineal del cabezal, transmite el impulso a la bola con fisica estable,
    /// emite pulsos hapticos al mando de Oculus Quest y proyecta una linea de punteria suave.
    /// </summary>
    public class GolfPutter : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PutterDataSO config;

        [Header("Putter Tracking Points")]
        [Tooltip("Pivote del cabezal del putter que golpea la bola.")]
        [SerializeField] private Transform clubHead;
        [Tooltip("Cara delantera plana del putter para calcular la direccion normal del golpe.")]
        [SerializeField] private Transform clubFaceForward;

        [Header("Components")]
        [SerializeField] private XRGrabInteractable grabInteractable;
        [SerializeField] private LineRenderer aimLineRenderer;

        [Header("Target Ball Reference")]
        [SerializeField] private GolfBall targetBall;

        // Historial de velocidades para promedio movil suave (elimina jitter de tracking VR)
        private const int VelocityBufferSize = 6;
        private Queue<Vector3> headVelocityHistory = new Queue<Vector3>();
        private Vector3 lastHeadPosition;
        private float lastHitTimestamp = 0f;
        private const float HitCooldown = 0.25f;

        private IXRSelectInteractor activeInteractor;

        public PutterDataSO Config => config;
        public Transform ClubHead => clubHead;

        private void Awake()
        {
            if (clubHead == null) clubHead = transform;
            if (clubFaceForward == null) clubFaceForward = clubHead;
            if (grabInteractable == null) grabInteractable = GetComponent<XRGrabInteractable>();

            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.AddListener(OnGrab);
                grabInteractable.selectExited.AddListener(OnRelease);
            }

            SetupAimLine();
        }

        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.selectEntered.RemoveListener(OnGrab);
                grabInteractable.selectExited.RemoveListener(OnRelease);
            }
        }

        private void SetupAimLine()
        {
            if (aimLineRenderer == null)
            {
                aimLineRenderer = GetComponent<LineRenderer>();
            }

            if (aimLineRenderer != null)
            {
                aimLineRenderer.positionCount = 2;
                aimLineRenderer.startWidth = 0.008f;
                aimLineRenderer.endWidth = 0.002f;
                aimLineRenderer.useWorldSpace = true;
                aimLineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                aimLineRenderer.startColor = new Color(0.1f, 0.9f, 1f, 0.6f);
                aimLineRenderer.endColor = new Color(0.1f, 0.9f, 1f, 0f);
                aimLineRenderer.enabled = false;
            }
        }

        private void Start()
        {
            lastHeadPosition = clubHead.position;
            if (targetBall == null) targetBall = FindAnyObjectByType<GolfBall>();
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt > 0.0001f)
            {
                Vector3 currentPos = clubHead.position;
                Vector3 currentVel = (currentPos - lastHeadPosition) / dt;
                lastHeadPosition = currentPos;

                headVelocityHistory.Enqueue(currentVel);
                if (headVelocityHistory.Count > VelocityBufferSize)
                {
                    headVelocityHistory.Dequeue();
                }
            }

            UpdateAimLine();
        }

        private Vector3 GetSmoothedHeadVelocity()
        {
            if (headVelocityHistory.Count == 0) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            foreach (var v in headVelocityHistory)
            {
                sum += v;
            }
            return sum / headVelocityHistory.Count;
        }

        private void UpdateAimLine()
        {
            if (aimLineRenderer == null || config == null || !config.ShowAimLine) return;

            if (targetBall == null || !targetBall.IsAtRest)
            {
                aimLineRenderer.enabled = false;
                return;
            }

            float distToBall = Vector3.Distance(clubHead.position, targetBall.transform.position);
            if (distToBall < 0.45f)
            {
                // Proyectar linea en el plano del suelo segun la orientacion de la cara del palo
                Vector3 faceDir = clubFaceForward.forward;
                faceDir.y = 0f;
                if (faceDir.sqrMagnitude > 0.001f)
                {
                    faceDir.Normalize();
                    Vector3 start = targetBall.transform.position;
                    Vector3 end = start + faceDir * config.AimLineDistance;

                    aimLineRenderer.enabled = true;
                    aimLineRenderer.SetPosition(0, start);
                    aimLineRenderer.SetPosition(1, end);
                    return;
                }
            }

            aimLineRenderer.enabled = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleBallImpact(other);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleBallImpact(collision.collider);
        }

        private void HandleBallImpact(Collider other)
        {
            if (Time.time - lastHitTimestamp < HitCooldown) return;

            GolfBall ball = other.GetComponent<GolfBall>();
            if (ball == null) ball = other.GetComponentInParent<GolfBall>();
            if (ball == null) return;

            Vector3 smoothedVel = GetSmoothedHeadVelocity();
            float swingSpeed = smoothedVel.magnitude;

            float minSpeed = config != null ? config.MinImpactVelocity : 0.12f;
            if (swingSpeed < minSpeed) return;

            lastHitTimestamp = Time.time;

            // Determinar direccion del tiro (combinacion de velocidad del swing y orientacion de la cara del putter)
            Vector3 swingDir = smoothedVel.normalized;
            Vector3 faceDir = clubFaceForward.forward;

            // La fuerza del impacto se proyecta mayoritariamente hacia adelante en el plano horizontal
            Vector3 hitDirection = Vector3.Lerp(faceDir, swingDir, 0.4f);
            hitDirection.y = Mathf.Clamp(hitDirection.y, 0f, 0.25f); // Pequena elevacion natural si se desea
            hitDirection.Normalize();

            float multiplier = config != null ? config.ImpulseMultiplier : 1.35f;
            float maxVel = config != null ? config.MaxBallVelocity : 14f;
            float targetSpeed = Mathf.Clamp(swingSpeed * multiplier, 0.3f, maxVel);

            Vector3 finalVelocity = hitDirection * targetSpeed;
            ball.ApplyPutterHit(finalVelocity);

            // Feedback Sonoro
            float normalizedSpeed = Mathf.Clamp01(targetSpeed / maxVel);
            GolfAudioManager.PlayPutterHit(normalizedSpeed);

            // Feedback Haptico en mandos Oculus Quest
            SendHapticFeedback(normalizedSpeed);

            if (aimLineRenderer != null) aimLineRenderer.enabled = false;
        }

        private void SendHapticFeedback(float normalizedPower)
        {
            if (activeInteractor is XRBaseInputInteractor baseInputInteractor)
            {
                float minAmp = config != null ? config.MinHapticAmplitude : 0.25f;
                float maxAmp = config != null ? config.MaxHapticAmplitude : 0.85f;
                float duration = config != null ? config.HapticDuration : 0.08f;
                float amplitude = Mathf.Lerp(minAmp, maxAmp, normalizedPower);

                baseInputInteractor.SendHapticImpulse(amplitude, duration);
            }
        }

        private void OnGrab(SelectEnterEventArgs args)
        {
            activeInteractor = args.interactorObject;
        }

        private void OnRelease(SelectExitEventArgs args)
        {
            activeInteractor = null;
        }

        public void SetTargetBall(GolfBall ball)
        {
            targetBall = ball;
        }
    }
}
