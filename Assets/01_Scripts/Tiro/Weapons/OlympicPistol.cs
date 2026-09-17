using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.InputSystem;
using Tiro.Data;
using Tiro.Targets;

namespace Tiro.Weapons
{
    /// <summary>
    /// Controla la pistola deportiva olímpica en VR (estilo Pistol Whip con mira de hierro).
    /// Permite disparos de alta precisión con alineación exacta entre alza trasera y punto delantero,
    /// retroceso háptico, animación de corredera y recarga física o por botón.
    /// </summary>
    public class OlympicPistol : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PistolDataSO pistolData;

        [Header("Iron Sights & Ballistics")]
        [Tooltip("Punto de salida del proyectil en la punta del cañón.")]
        [SerializeField] private Transform muzzlePoint;
        [Tooltip("Punto de mira delantero (poste con fibra óptica).")]
        [SerializeField] private Transform frontSight;
        [Tooltip("Alza trasera (muesca con puntos fluorescentes).")]
        [SerializeField] private Transform rearSight;
        [Tooltip("Pieza de la corredera que retrocede en cada disparo.")]
        [SerializeField] private Transform slideTransform;

        [Header("FX & Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private ParticleSystem muzzleFlashParticles;
        [SerializeField] private LineRenderer bulletTracerPrefab;
        [SerializeField] private GameObject bulletHolePrefab;

        [Header("Direct VR Hand Binding")]
        [Tooltip("Si es true, la pistola se vincula automáticamente al mando derecho sin necesidad de recogerla del suelo.")]
        [SerializeField] private bool bindToRightControllerOnStart = true;
        [SerializeField] private Vector3 gripOffset = new Vector3(0f, -0.025f, 0.08f);
        [SerializeField] private Vector3 gripEulerAngles = Vector3.zero; // Apuntar 100% al frente (+Z)

        [Header("Input Actions (Opcional - se complementa con lectura directa de hardware XR)")]
        [SerializeField] private InputActionProperty fireAction;
        [SerializeField] private InputActionProperty reloadAction;

        private int currentAmmo;
        private float lastFireTimestamp = -10f;
        private bool isReloading = false;
        private bool wasTriggerPulled = false;
        private bool wasReloadBtnPressed = false;
        private Vector3 slideInitialLocalPos;
        private XRGrabInteractable grabInteractable;
        private XRBaseInputInteractor boundInteractor;
        private Transform boundHandTarget;

        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => pistolData != null ? pistolData.magazineCapacity : 10;
        public PistolDataSO Data => pistolData;

        public event Action<int, int> OnAmmoChanged; // (current, max)
        public event Action OnPistolFired;
        public event Action OnPistolReloaded;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            grabInteractable = GetComponent<XRGrabInteractable>();

            if (slideTransform != null)
            {
                slideInitialLocalPos = slideTransform.localPosition;
            }

            if (pistolData != null)
            {
                currentAmmo = pistolData.magazineCapacity;
            }
            else
            {
                currentAmmo = 10;
            }
        }

        private void OnEnable()
        {
            try
            {
                if (fireAction.action != null && !fireAction.action.enabled) fireAction.action.Enable();
                if (reloadAction.action != null && !reloadAction.action.enabled) reloadAction.action.Enable();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OlympicPistol] Error activando InputActions: {ex.Message}");
            }
        }

        private void OnDisable()
        {
            try
            {
                if (fireAction.action != null && fireAction.action.enabled) fireAction.action.Disable();
                if (reloadAction.action != null && reloadAction.action.enabled) reloadAction.action.Disable();
            }
            catch (Exception) { }
        }

        private void Start()
        {
            if (grabInteractable != null)
            {
                grabInteractable.activated.AddListener(OnGrabActivated);
            }

            if (bindToRightControllerOnStart)
            {
                BindToRightHand();
            }

            OnAmmoChanged?.Invoke(currentAmmo, MaxAmmo);
        }

        private void LateUpdate()
        {
            // Garantizar que la pistola permanezca vinculada al mando derecho
            if (bindToRightControllerOnStart && (boundHandTarget == null || transform.parent != boundHandTarget))
            {
                BindToRightHand();
            }
        }

        private void OnDestroy()
        {
            if (grabInteractable != null)
            {
                grabInteractable.activated.RemoveListener(OnGrabActivated);
            }
        }

        /// <summary>
        /// Localiza el mando derecho del jugador en la jerarquía XR y emparenta la pistola de forma precisa.
        /// </summary>
        public void BindToRightHand()
        {
            if (boundHandTarget == null)
            {
                boundHandTarget = FindRightHandTransform();
            }

            if (boundHandTarget != null)
            {
                transform.SetParent(boundHandTarget, false);
                transform.localPosition = gripOffset;
                transform.localRotation = Quaternion.Euler(gripEulerAngles);

                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                if (grabInteractable != null)
                {
                    grabInteractable.enabled = false;
                }
            }
        }

        private Transform FindRightHandTransform()
        {
            // 1. Buscar controladores XRBaseController
            var controllers = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.XRBaseController>(FindObjectsSortMode.None);
            foreach (var c in controllers)
            {
                string n = c.name.ToLower();
                if (n.Contains("right"))
                {
                    return c.transform;
                }
            }

            // 2. Buscar en XROrigin CameraFloorOffset
            var origin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
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

            // 3. Buscar interactores XR con "right"
            var interactors = FindObjectsByType<XRBaseInputInteractor>(FindObjectsSortMode.None);
            foreach (var it in interactors)
            {
                string n = it.name.ToLower();
                if (n.Contains("right"))
                {
                    boundInteractor = it;
                    return it.transform;
                }
                if (it.transform.parent != null && it.transform.parent.name.ToLower().Contains("right"))
                {
                    boundInteractor = it;
                    return it.transform.parent;
                }
            }

            // 4. Búsqueda exhaustiva por nombre en la jerarquía
            var allGos = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            Transform candidate = null;
            for (int i = 0; i < allGos.Length; i++)
            {
                string n = allGos[i].name.ToLower();
                if (n == "right hand" || n == "right controller" || n == "righthand controller" || n == "righthand")
                {
                    return allGos[i].transform;
                }
                if (n.Contains("right") && (n.Contains("hand") || n.Contains("controller")))
                {
                    if (!n.Contains("visual") && !n.Contains("callout") && !n.Contains("mesh"))
                    {
                        if (candidate == null) candidate = allGos[i].transform;
                    }
                }
            }

            return candidate;
        }

        private void Update()
        {
            HandleInput();
            CheckGestureReload();
        }

        private void HandleInput()
        {
            bool fireRequested = false;
            bool reloadRequested = false;

            // 1. Hardware Directo XR (Meta Quest / OpenXR Controllers)
            var rightDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (rightDevice.isValid)
            {
                bool triggerPressed = false;
                if (rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool btnVal) && btnVal)
                {
                    triggerPressed = true;
                }
                else if (rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float triggerAxis) && triggerAxis > 0.45f)
                {
                    triggerPressed = true;
                }

                if (triggerPressed && !wasTriggerPulled)
                {
                    fireRequested = true;
                }
                wasTriggerPulled = triggerPressed;

                // Botón A o B para recargar
                bool reloadBtn = false;
                if (rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool aVal) && aVal) reloadBtn = true;
                if (rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out bool bVal) && bVal) reloadBtn = true;

                if (reloadBtn && !wasReloadBtnPressed)
                {
                    reloadRequested = true;
                }
                wasReloadBtnPressed = reloadBtn;
            }

            // 2. Input System Action Properties (si están mapeadas)
            if (fireAction.action != null && fireAction.action.WasPressedThisFrame())
            {
                fireRequested = true;
            }
            if (reloadAction.action != null && reloadAction.action.WasPressedThisFrame())
            {
                reloadRequested = true;
            }

            // 3. Fallback Teclado / Ratón para depuración en Unity Editor y Simulator
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                fireRequested = true;
            }
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                fireRequested = true;
            }
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                reloadRequested = true;
            }

            // Ejecutar acciones solicitadas
            if (fireRequested)
            {
                TryFire();
            }
            if (reloadRequested)
            {
                TryReload();
            }
        }

        /// <summary>
        /// Recarga táctica olímpica al inclinar el cañón hacia abajo.
        /// </summary>
        private void CheckGestureReload()
        {
            if (isReloading || currentAmmo >= MaxAmmo) return;

            // Si el arma apunta hacia abajo (> 60° de inclinación negativa)
            if (transform.forward.y < -0.75f)
            {
                TryReload();
            }
        }

        private void OnGrabActivated(ActivateEventArgs args)
        {
            TryFire();
        }

        public void TryFire()
        {
            if (isReloading) return;

            float cooldown = pistolData != null ? pistolData.fireCooldown : 0.18f;
            if (Time.time - lastFireTimestamp < cooldown) return;

            if (currentAmmo <= 0)
            {
                PlayDryFireSound();
                return;
            }

            lastFireTimestamp = Time.time;
            currentAmmo--;
            OnAmmoChanged?.Invoke(currentAmmo, MaxAmmo);

            ExecuteShot();
        }

        private void ExecuteShot()
        {
            // 1. Origen y dirección: alineados con la mira de hierro
            Vector3 origin = muzzlePoint != null ? muzzlePoint.position : transform.position;
            Vector3 direction = muzzlePoint != null ? muzzlePoint.forward : transform.forward;

            if (rearSight != null && frontSight != null)
            {
                // La línea óptica perfecta va del alza al poste delantero
                direction = (frontSight.position - rearSight.position).normalized;
            }

            float range = pistolData != null ? pistolData.maxRange : 100f;
            Vector3 targetHitPoint = origin + direction * range;
            bool hitTarget = false;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Collide))
            {
                targetHitPoint = hit.point;

                // Chequear si impactó una diana olímpica
                TargetBoard targetBoard = hit.collider.GetComponentInParent<TargetBoard>();
                if (targetBoard != null)
                {
                    hitTarget = true;
                    targetBoard.RegisterBulletHit(hit.point, hit.normal);
                }
                else
                {
                    SpawnImpactEffect(hit.point, hit.normal);
                }
            }

            // 2. Trazador de bala
            StartCoroutine(RenderBulletTracer(origin, targetHitPoint));

            // 3. Fogonazo y Audio
            if (muzzleFlashParticles != null)
            {
                muzzleFlashParticles.Play();
            }

            if (pistolData != null && pistolData.gunshotSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(pistolData.gunshotSound);
            }

            // 4. Retroceso de corredera y cabeceo
            StartCoroutine(AnimateRecoil());

            // 5. Vibración háptica en el mando Oculus
            TriggerHaptics();

            OnPistolFired?.Invoke();
        }

        private IEnumerator RenderBulletTracer(Vector3 start, Vector3 end)
        {
            GameObject tracerObj = new GameObject("BulletTracer");
            LineRenderer lr = tracerObj.AddComponent<LineRenderer>();

            lr.startWidth = 0.012f;
            lr.endWidth = 0.006f;
            lr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lr.material.color = new Color(1f, 0.85f, 0.4f, 1f); // Amarillo dorado trazador
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            yield return new WaitForSeconds(0.04f);
            Destroy(tracerObj);
        }

        private void SpawnImpactEffect(Vector3 point, Vector3 normal)
        {
            if (bulletHolePrefab != null)
            {
                Instantiate(bulletHolePrefab, point + normal * 0.002f, Quaternion.LookRotation(normal));
            }
        }

        private IEnumerator AnimateRecoil()
        {
            if (slideTransform == null) yield break;

            float dist = pistolData != null ? pistolData.slideRecoilDistance : 0.035f;
            Vector3 backPos = slideInitialLocalPos - Vector3.forward * dist;

            // Retroceso rápido hacia atrás (0.03s)
            float elapsed = 0f;
            while (elapsed < 0.03f)
            {
                elapsed += Time.deltaTime;
                slideTransform.localPosition = Vector3.Lerp(slideInitialLocalPos, backPos, elapsed / 0.03f);
                yield return null;
            }

            // Recuperación hacia adelante (0.06s)
            elapsed = 0f;
            while (elapsed < 0.06f)
            {
                elapsed += Time.deltaTime;
                slideTransform.localPosition = Vector3.Lerp(backPos, slideInitialLocalPos, elapsed / 0.06f);
                yield return null;
            }

            slideTransform.localPosition = slideInitialLocalPos;
        }

        public void TryReload()
        {
            if (isReloading || currentAmmo >= MaxAmmo) return;
            StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            isReloading = true;

            if (pistolData != null && pistolData.reloadSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(pistolData.reloadSound);
            }

            // Breve vibración de recarga
            SendHaptic(0.4f, 0.08f);

            yield return new WaitForSeconds(0.65f);

            currentAmmo = MaxAmmo;
            isReloading = false;
            OnAmmoChanged?.Invoke(currentAmmo, MaxAmmo);
            OnPistolReloaded?.Invoke();

            SendHaptic(0.6f, 0.12f);
        }

        private void PlayDryFireSound()
        {
            if (pistolData != null && pistolData.dryFireSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(pistolData.dryFireSound);
            }
            SendHaptic(0.25f, 0.05f);
        }

        private void TriggerHaptics()
        {
            float intensity = pistolData != null ? pistolData.hapticIntensity : 0.9f;
            float duration = pistolData != null ? pistolData.hapticDurationSeconds : 0.12f;
            SendHaptic(intensity, duration);
        }

        private void SendHaptic(float intensity, float duration)
        {
            if (boundInteractor != null)
            {
                boundInteractor.SendHapticImpulse(intensity, duration);
                return;
            }

            if (grabInteractable != null && grabInteractable.isSelected)
            {
                foreach (var it in grabInteractable.interactorsSelecting)
                {
                    if (it is XRBaseInputInteractor inputIt)
                    {
                        inputIt.SendHapticImpulse(intensity, duration);
                    }
                }
                return;
            }

            var all = FindObjectsByType<XRBaseInputInteractor>(FindObjectsSortMode.None);
            foreach (var it in all)
            {
                string n = it.name.ToLower();
                if (n.Contains("right") || (it.transform.parent != null && it.transform.parent.name.ToLower().Contains("right")))
                {
                    it.SendHapticImpulse(intensity, duration);
                }
            }
        }
    }
}
