using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.InputSystem;
using TMPro;
using Tiro.Data;
using Tiro.Targets;

namespace Tiro.Weapons
{
    /// <summary>
    /// Controla la Escopeta Olímpica Superpuesta (Over-Under Shotgun) para Tiro al Plato (Skeet / Trap).
    /// Dispara 2 cartuchos de perdigones con dispersión cónica balística,
    /// potente retroceso háptico, sonido de detonación y recarga por quiebre de cañón hacia abajo.
    /// </summary>
    public class OlympicShotgun : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PistolDataSO shotgunData;

        [Header("Double Barrel & Sight")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private Transform frontBeadSight;

        [Header("Pellet Ballistics")]
        [SerializeField] private int pelletCount = 12;
        [SerializeField] private float spreadAngleDegrees = 3.5f;
        [SerializeField] private float maxRange = 90f;

        [Header("Ammo (2 Shells)")]
        [SerializeField] private TextMeshPro ammoText;
        private int currentShells = 2;
        private const int MaxShells = 2;

        [Header("Audio & FX")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private ParticleSystem muzzleFlash;

        [Header("Hand Grip")]
        [SerializeField] private Vector3 gripOffset = new Vector3(0f, -0.05f, 0.12f);
        [SerializeField] private Vector3 gripEulerAngles = Vector3.zero;

        private bool isReloading = false;
        private float lastFireTime = -10f;
        private bool wasTriggerPulled = false;
        private bool gestureArmed = true;
        private Vector3 lastHandPos;
        private Transform boundHand;

        public int CurrentShells => currentShells;
        public int MaxAmmo => MaxShells;

        public event Action<int, int> OnAmmoChanged;
        public event Action OnShotgunFired;
        public event Action OnShotgunReloaded;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            currentShells = MaxShells;
        }

        private void OnEnable()
        {
            BindToRightHand();
        }

        private void Start()
        {
            BindToRightHand();
            lastHandPos = transform.position;
            UpdateDisplay();
            OnAmmoChanged?.Invoke(currentShells, MaxShells);
        }

        private void LateUpdate()
        {
            if (boundHand == null || transform.parent != boundHand)
            {
                BindToRightHand();
            }
        }

        public void BindToRightHand()
        {
            boundHand = FindRightHandTransform();

            if (boundHand != null)
            {
                transform.SetParent(boundHand, false);
                transform.localPosition = gripOffset;
                transform.localRotation = Quaternion.Euler(gripEulerAngles);

                var rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
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
                    return it.transform;
                }
                if (it.transform.parent != null && it.transform.parent.name.ToLower().Contains("right"))
                {
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

            // 1. Hardware VR
            var rightDevice = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.RightHand);
            if (rightDevice.isValid)
            {
                bool trigger = false;
                if (rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out bool btn) && btn) trigger = true;
                else if (rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out float val) && val > 0.45f) trigger = true;

                if (trigger && !wasTriggerPulled) fireRequested = true;
                wasTriggerPulled = trigger;

                if (rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out bool aBtn) && aBtn) TryReload();
            }

            // 2. Teclado / Ratón
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) fireRequested = true;
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) fireRequested = true;
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) TryReload();

            if (fireRequested) TryFire();
        }

        private void CheckGestureReload()
        {
            if (isReloading) return;

            if (currentShells >= MaxShells)
            {
                if (transform.forward.y > -0.22f) gestureArmed = true;
                lastHandPos = transform.position;
                return;
            }

            bool pointingDown = transform.forward.y < -0.38f;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float downVel = (lastHandPos.y - transform.position.y) / dt;
            bool flickDown = downVel > 0.75f && transform.forward.y < -0.12f;

            if ((pointingDown || flickDown) && gestureArmed)
            {
                gestureArmed = false;
                TryReload();
            }
            else if (transform.forward.y > -0.22f)
            {
                gestureArmed = true;
            }

            lastHandPos = transform.position;
        }

        public void TryFire()
        {
            if (isReloading) return;
            if (Time.time - lastFireTime < 0.22f) return;

            if (currentShells <= 0)
            {
                if (audioSource != null && shotgunData != null && shotgunData.dryFireSound != null)
                {
                    audioSource.PlayOneShot(shotgunData.dryFireSound);
                }
                return;
            }

            lastFireTime = Time.time;
            currentShells--;
            UpdateDisplay();
            OnAmmoChanged?.Invoke(currentShells, MaxShells);

            ExecuteShot();
        }

        private void ExecuteShot()
        {
            Vector3 origin = muzzlePoint != null ? muzzlePoint.position : transform.position;
            Vector3 baseDir = muzzlePoint != null ? muzzlePoint.forward : transform.forward;

            if (frontBeadSight != null)
            {
                baseDir = (frontBeadSight.position - origin).normalized;
                if (baseDir == Vector3.zero) baseDir = transform.forward;
            }

            // Disparo cónico de perdigones
            for (int i = 0; i < pelletCount; i++)
            {
                // Dispersión aleatoria dentro del cono
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * Mathf.Tan(spreadAngleDegrees * Mathf.Deg2Rad);
                Vector3 pelletDir = (baseDir + transform.right * randomCircle.x + transform.up * randomCircle.y).normalized;

                Vector3 hitPoint = origin + pelletDir * maxRange;

                if (Physics.Raycast(origin, pelletDir, out RaycastHit hit, maxRange, ~0, QueryTriggerInteraction.Collide))
                {
                    hitPoint = hit.point;

                    // 1. Impacto a plato volador (Clay Pigeon)
                    var clay = hit.collider.GetComponentInParent<ClayPigeon>();
                    if (clay != null)
                    {
                        clay.RegisterShotHit(hit.point, pelletDir);
                    }

                    // 2. Impacto a diana convencional
                    var target = hit.collider.GetComponentInParent<TargetBoard>();
                    if (target != null)
                    {
                        target.RegisterBulletHit(hit.point, hit.normal);
                    }

                    // 3. Impacto a galería reactiva de pared
                    var wallNode = hit.collider.GetComponentInParent<DynamicTargetNode>();
                    if (wallNode != null)
                    {
                        var gallery = wallNode.GetComponentInParent<DynamicWallTargetGallery>();
                        if (gallery != null) gallery.RegisterNodeHit(wallNode, hit.point);
                    }

                    // 4. Impacto a botones del panel de menú
                    var selector = hit.collider.GetComponentInParent<Tiro.UI.DisciplineSelectorPanel>();
                    if (selector != null)
                    {
                        selector.HandleShotOnButton(hit.collider.name.ToLower());
                    }
                }

                if (i % 2 == 0) // Renderizar trazadores para la mitad de perdigones para optimizar
                {
                    StartCoroutine(RenderPelletTracer(origin, hitPoint));
                }
            }

            if (muzzleFlash != null) muzzleFlash.Play();
            if (audioSource != null && shotgunData != null && shotgunData.gunshotSound != null)
            {
                audioSource.PlayOneShot(shotgunData.gunshotSound);
            }

            TriggerHeavyHaptics();
            OnShotgunFired?.Invoke();
        }

        private IEnumerator RenderPelletTracer(Vector3 start, Vector3 end)
        {
            GameObject tracer = new GameObject("PelletTracer");
            var lr = tracer.AddComponent<LineRenderer>();
            lr.startWidth = 0.018f;
            lr.endWidth = 0.006f;
            lr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lr.material.color = new Color(1f, 0.7f, 0.2f, 1f); // Trazador ámbar fuego
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            yield return new WaitForSeconds(0.04f);
            Destroy(tracer);
        }

        public void TryReload()
        {
            if (isReloading || currentShells >= MaxShells) return;
            StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            isReloading = true;
            UpdateDisplay();

            if (audioSource != null && shotgunData != null && shotgunData.reloadSound != null)
            {
                audioSource.PlayOneShot(shotgunData.reloadSound);
            }

            yield return new WaitForSeconds(0.6f);

            currentShells = MaxShells;
            isReloading = false;
            UpdateDisplay();
            OnAmmoChanged?.Invoke(currentShells, MaxShells);
            OnShotgunReloaded?.Invoke();
        }

        private void UpdateDisplay()
        {
            if (ammoText == null) return;
            if (isReloading)
            {
                ammoText.text = "RELOAD...";
                ammoText.color = Color.yellow;
            }
            else
            {
                ammoText.text = $"{currentShells}/{MaxShells}";
                ammoText.color = currentShells > 0 ? Color.green : Color.red;
            }
        }

        private void TriggerHeavyHaptics()
        {
            var interactors = FindObjectsByType<XRBaseInputInteractor>(FindObjectsSortMode.None);
            foreach (var it in interactors)
            {
                if (it.name.ToLower().Contains("right"))
                {
                    it.SendHapticImpulse(1.0f, 0.18f); // Vibración máxima para escopeta
                }
            }
        }
    }
}
