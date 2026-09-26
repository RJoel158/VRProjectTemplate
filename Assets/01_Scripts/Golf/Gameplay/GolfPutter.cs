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

        [Header("Direct Hand Binding")]
        [Tooltip("Vincula el putter directamente al mando derecho al estilo Wii Sports y Walkabout Golf.")]
        [SerializeField] private bool autoBindToRightHand = true;
        [SerializeField] private Vector3 localGripPosition = new Vector3(0f, -0.04f, 0.05f);
        [SerializeField] private Vector3 localGripEuler = new Vector3(-20f, 0f, 0f);
        [SerializeField] private Transform rightHandTarget;

        [Header("Dynamic Putter Length (Walkabout Mini Golf Style)")]
        [Tooltip("Longitud actual del palo en metros (desde el agarre hasta el cabezal).")]
        [SerializeField] private float currentLength = 1.02f;
        [SerializeField] private float minLength = 0.72f;
        [SerializeField] private float maxLength = 1.35f;
        [SerializeField] private float lengthAdjustSpeed = 0.40f;

        private float lastHapticTickLength = 1.02f;
        private const float HapticTickInterval = 0.025f; // Tick haptico cada 2.5 cm

        // Historial de velocidades para promedio movil suave (elimina jitter de tracking VR)
        private const int VelocityBufferSize = 6;
        private Queue<Vector3> headVelocityHistory = new Queue<Vector3>();
        private Vector3 lastHeadPosition;
        private float lastHitTimestamp = 0f;
        private const float HitCooldown = 0.25f;

        private IXRSelectInteractor activeInteractor;
        private XRBaseInputInteractor boundInputInteractor;
        private Rigidbody rb;
        private bool isBound = false;

        public PutterDataSO Config => config;
        public Transform ClubHead => clubHead;
        public float CurrentLength => currentLength;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            currentLength = PlayerPrefs.GetFloat("Golf_PutterLength", 1.02f);
            ApplyPutterLength(currentLength);
            ValidatePutterHierarchy();

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

        /// <summary>
        /// Ajusta dinamicamente la longitud del putter (vara y cabezal) en tiempo real.
        /// </summary>
        public void ApplyPutterLength(float length)
        {
            currentLength = Mathf.Clamp(length, minLength, maxLength);

            if (clubHead != null)
            {
                clubHead.localPosition = new Vector3(0f, -currentLength, 0.02f);
                clubHead.localScale = new Vector3(0.12f, 0.04f, 0.06f);

                var box = clubHead.GetComponent<BoxCollider>();
                if (box != null)
                {
                    box.isTrigger = true;
                    // Collider generoso en altura para garantizar contacto infalible con la pelota sin importar desniveles
                    box.size = new Vector3(1.35f, 2.4f, 1.4f);
                    box.center = new Vector3(0f, -0.15f, 0f);
                }
            }

            Transform shaft = transform.Find("Shaft");
            if (shaft != null)
            {
                shaft.localPosition = new Vector3(0f, -currentLength * 0.5f, 0f);
                shaft.localScale = new Vector3(0.014f, currentLength * 0.5f, 0.014f);
            }

            Transform grip = transform.Find("Grip");
            if (grip != null)
            {
                grip.localPosition = Vector3.zero;
                grip.localScale = new Vector3(0.024f, 0.10f, 0.024f);
            }
        }

        /// <summary>
        /// Corrige en tiempo de ejecucion cualquier jerarquia invertida previa y asegura la longitud correcta.
        /// </summary>
        public void ValidatePutterHierarchy()
        {
            ApplyPutterLength(currentLength > 0.5f ? currentLength : 1.02f);
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
            ValidatePutterHierarchy();
            lastHeadPosition = clubHead != null ? clubHead.position : transform.position;
            if (targetBall == null) targetBall = FindAnyObjectByType<GolfBall>();

            if (autoBindToRightHand)
            {
                BindToRightHand();
            }
        }

        private void LateUpdate()
        {
            if (autoBindToRightHand && (!isBound || transform.parent == null || transform.parent != rightHandTarget))
            {
                BindToRightHand();
            }
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
            HandleLengthAdjustment();
        }

        /// <summary>
        /// Permite al jugador alargar o acortar el putter en tiempo real mediante el joystick vertical (VR)
        /// o las flechas de teclado (Editor), guardando la preferencia automáticamente.
        /// </summary>
        private void HandleLengthAdjustment()
        {
            float verticalInput = 0f;

            // 1. Mando derecho (XR Controller Meta Quest)
            var rightDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (rightDevice.isValid && rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 rightStick))
            {
                if (Mathf.Abs(rightStick.y) > 0.18f)
                {
                    verticalInput = rightStick.y;
                }
            }

            // 2. Mando izquierdo (fallback)
            if (Mathf.Abs(verticalInput) < 0.18f)
            {
                var leftDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.LeftHand);
                if (leftDevice.isValid && leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out Vector2 leftStick))
                {
                    if (Mathf.Abs(leftStick.y) > 0.18f)
                    {
                        verticalInput = leftStick.y;
                    }
                }
            }

            // 3. Fallback Teclado PC / Unity Editor (Flechas Arriba/Abajo o W/S)
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed || UnityEngine.InputSystem.Keyboard.current.wKey.isPressed)
                {
                    verticalInput = 1f;
                }
                else if (UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed || UnityEngine.InputSystem.Keyboard.current.sKey.isPressed)
                {
                    verticalInput = -1f;
                }
            }
#endif

            if (Mathf.Abs(verticalInput) > 0.18f)
            {
                // Empujar la palanca hacia abajo alarga el palo hacia el suelo; empujar hacia arriba lo acorta
                // (o viceversa según intuición natural: stick hacia adelante/arriba = alargar hacia el suelo)
                float delta = verticalInput * lengthAdjustSpeed * Time.deltaTime;
                float newLength = Mathf.Clamp(currentLength + delta, minLength, maxLength);

                if (!Mathf.Approximately(newLength, currentLength))
                {
                    currentLength = newLength;
                    ApplyPutterLength(currentLength);

                    // Pulso háptico por cada intervalo de 2.5 cm para sentir el clic mecánico
                    if (Mathf.Abs(currentLength - lastHapticTickLength) >= HapticTickInterval)
                    {
                        lastHapticTickLength = currentLength;
                        TriggerAdjustmentHaptic();
                    }

                    // Guardar preferencia
                    PlayerPrefs.SetFloat("Golf_PutterLength", currentLength);
                }
            }
        }

        private void TriggerAdjustmentHaptic()
        {
            var rightDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (rightDevice.isValid)
            {
                rightDevice.SendHapticImpulse(0, 0.22f, 0.025f);
            }
            else if (boundInputInteractor != null)
            {
                boundInputInteractor.SendHapticImpulse(0.22f, 0.025f);
            }
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

        private void OnTriggerStay(Collider other)
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

            // 1. Obtener velocidad de tracking suavizada
            Vector3 smoothedVel = GetSmoothedHeadVelocity();
            float swingSpeed = smoothedVel.magnitude;

            // 2. Consultar velocidad física directa de hardware del mando XR (cero latencia)
            var rightDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (rightDevice.isValid)
            {
                if (rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceVelocity, out Vector3 handLinearVel) &&
                    rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceAngularVelocity, out Vector3 handAngVel))
                {
                    Vector3 r = clubHead.position - transform.position;
                    Vector3 hwHeadVel = handLinearVel + Vector3.Cross(handAngVel, r);
                    if (hwHeadVel.magnitude > swingSpeed)
                    {
                        swingSpeed = hwHeadVel.magnitude;
                        smoothedVel = hwHeadVel;
                    }
                }
            }

            float minSpeed = config != null ? config.MinImpactVelocity : 0.08f;
            // Si la velocidad por historico o hardware es baja, verificar si hay desplazamiento instantaneo del cabezal
            if (swingSpeed < minSpeed)
            {
                Vector3 headDelta = clubHead.position - lastHeadPosition;
                if (headDelta.magnitude > 0.0005f)
                {
                    swingSpeed = minSpeed * 1.5f;
                    smoothedVel = headDelta.normalized * swingSpeed;
                }
                else
                {
                    // Si el palo está empujando la pelota directamente con contacto
                    Vector3 pushDir = clubFaceForward != null ? clubFaceForward.forward : transform.forward;
                    pushDir.y = 0f;
                    swingSpeed = minSpeed;
                    smoothedVel = pushDir.normalized * swingSpeed;
                }
            }

            lastHitTimestamp = Time.time;

            // Determinar direccion del tiro (combinacion de velocidad del swing y orientacion de la cara del putter)
            Vector3 swingDir = smoothedVel.normalized;
            Vector3 faceDir = clubFaceForward != null ? clubFaceForward.forward : transform.forward;
            faceDir.y = 0f;
            if (faceDir.sqrMagnitude < 0.001f) faceDir = swingDir;
            faceDir.Normalize();

            // Si el swing va en direccion opuesta a la cara (golpe de reves), invertir cara
            if (Vector3.Dot(faceDir, swingDir) < 0f)
            {
                faceDir = -faceDir;
            }

            // La fuerza del impacto se proyecta mayoritariamente hacia adelante en el plano horizontal (75% cara, 25% swing)
            Vector3 hitDirection = Vector3.Lerp(faceDir, swingDir, 0.25f);
            hitDirection.y = 0f; // Mantener sobre el cesped
            if (hitDirection.sqrMagnitude < 0.001f) hitDirection = faceDir;
            hitDirection.Normalize();

            float multiplier = config != null ? config.ImpulseMultiplier : 1.35f;
            float maxVel = config != null ? config.MaxBallVelocity : 14f;
            // Clampear velocidad permitiendo toques sutiles para embocar de cerca
            float targetSpeed = Mathf.Clamp(swingSpeed * multiplier, 0.08f, maxVel);

            Vector3 finalVelocity = hitDirection * targetSpeed;
            Debug.Log($"[GolfPutter] Golpe aplicado a la pelota: velocidad={targetSpeed:F2} m/s, direccion={hitDirection}, largoPalo={currentLength:F2}m");
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
            float minAmp = config != null ? config.MinHapticAmplitude : 0.25f;
            float maxAmp = config != null ? config.MaxHapticAmplitude : 0.85f;
            float duration = config != null ? config.HapticDuration : 0.08f;
            float amplitude = Mathf.Lerp(minAmp, maxAmp, normalizedPower);

            if (boundInputInteractor != null)
            {
                boundInputInteractor.SendHapticImpulse(amplitude, duration);
            }
            else if (activeInteractor is XRBaseInputInteractor baseInputInteractor)
            {
                baseInputInteractor.SendHapticImpulse(amplitude, duration);
            }
        }

        private void BindToRightHand()
        {
            if (rightHandTarget == null)
            {
                rightHandTarget = FindRightHandTransform();
            }

            if (rightHandTarget != null)
            {
                ValidatePutterHierarchy();
                transform.SetParent(rightHandTarget, false);
                transform.localPosition = localGripPosition;
                transform.localRotation = Quaternion.Euler(localGripEuler);

                isBound = true;

                // Localizar el interactor del mando para vibracion haptica
                boundInputInteractor = rightHandTarget.GetComponent<XRBaseInputInteractor>();
                if (boundInputInteractor == null)
                {
                    boundInputInteractor = rightHandTarget.GetComponentInChildren<XRBaseInputInteractor>();
                }
                if (boundInputInteractor == null && rightHandTarget.parent != null)
                {
                    boundInputInteractor = rightHandTarget.parent.GetComponentInChildren<XRBaseInputInteractor>();
                }

                Debug.Log($"[GolfPutter] Putter vinculado exitosamente a la mano: {rightHandTarget.name}");
            }
        }

        private Transform FindRightHandTransform()
        {
            // 1. Buscar en controladores XR
            var controllers = Object.FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.XRBaseController>(FindObjectsSortMode.None);
            foreach (var c in controllers)
            {
                string n = c.name.ToLower();
                if (n.Contains("right"))
                {
                    return c.transform;
                }
            }

            // 2. Buscar en XROrigin CameraFloorOffset
            var origin = Object.FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null && origin.CameraFloorOffsetObject != null)
            {
                var children = origin.CameraFloorOffsetObject.GetComponentsInChildren<Transform>(true);
                foreach (var t in children)
                {
                    string lower = t.name.ToLower();
                    if (lower.Contains("right") && (lower.Contains("controller") || lower.Contains("hand")))
                    {
                        if (!lower.Contains("ray") && !lower.Contains("poke") && !lower.Contains("teleport") && !lower.Contains("visual"))
                        {
                            return t;
                        }
                    }
                }
            }

            // 3. Buscar interactores con "right"
            var interactors = Object.FindObjectsByType<XRBaseInputInteractor>(FindObjectsSortMode.None);
            foreach (var it in interactors)
            {
                string n = it.name.ToLower();
                if (n.Contains("right"))
                {
                    return it.transform;
                }
                if (it.transform.parent != null && it.transform.parent.name.ToLower().Contains("right"))
                {
                    return it.transform.parent;
                }
            }

            return null;
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
