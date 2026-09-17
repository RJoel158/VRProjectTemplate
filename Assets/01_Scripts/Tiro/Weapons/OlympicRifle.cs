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
    /// Controla el Rifle de Precisión Deportivo Olímpico en VR.
    /// Diseñado para disparos a larga distancia (10m, 25m, 50m) con mira de diópter olímpico,
    /// balística de alta estabilidad, retroceso amortiguado y recarga fluida.
    /// </summary>
    public class OlympicRifle : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PistolDataSO rifleData;

        [Header("Precision Sights & Barrel")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private Transform frontSight;
        [SerializeField] private Transform rearDiopterSight;

        [Header("Ammo & Display")]
        [SerializeField] private TextMeshPro ammoText;
        [SerializeField] private int maxAmmo = 10;
        private int currentAmmo;

        [Header("Audio & FX")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private ParticleSystem muzzleFlash;

        [Header("Hand Grip")]
        [SerializeField] private Vector3 gripOffset = new Vector3(0f, -0.05f, 0.15f);
        [SerializeField] private Vector3 gripEulerAngles = Vector3.zero;

        private bool isReloading = false;
        private float lastFireTime = -10f;
        private bool wasTriggerPulled = false;
        private bool gestureArmed = true;
        private Vector3 lastHandPos;
        private Transform boundHand;

        public int CurrentAmmo => currentAmmo;
        public int MaxAmmo => maxAmmo;

        public event Action<int, int> OnAmmoChanged;
        public event Action OnRifleFired;
        public event Action OnRifleReloaded;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            currentAmmo = maxAmmo;
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
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
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

            if (currentAmmo >= maxAmmo)
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
            if (Time.time - lastFireTime < 0.25f) return;

            if (currentAmmo <= 0)
            {
                if (audioSource != null && rifleData != null && rifleData.dryFireSound != null)
                {
                    audioSource.PlayOneShot(rifleData.dryFireSound);
                }
                return;
            }

            lastFireTime = Time.time;
            currentAmmo--;
            UpdateDisplay();
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);

            ExecuteShot();
        }

        private void ExecuteShot()
        {
            Vector3 origin = muzzlePoint != null ? muzzlePoint.position : transform.position;
            Vector3 direction = muzzlePoint != null ? muzzlePoint.forward : transform.forward;

            if (rearDiopterSight != null && frontSight != null)
            {
                direction = (frontSight.position - rearDiopterSight.position).normalized;
            }

            float range = 120f;
            Vector3 hitPoint = origin + direction * range;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Collide))
            {
                hitPoint = hit.point;

                // 1. Dianas olímpicas fijas
                var target = hit.collider.GetComponentInParent<TargetBoard>();
                if (target != null)
                {
                    target.RegisterBulletHit(hit.point, hit.normal);
                }

                // 2. Galería reactiva de pared
                var wallNode = hit.collider.GetComponentInParent<DynamicTargetNode>();
                if (wallNode != null)
                {
                    var gallery = wallNode.GetComponentInParent<DynamicWallTargetGallery>();
                    if (gallery != null) gallery.RegisterNodeHit(wallNode, hit.point);
                }

                // 3. Platos voladores
                var clay = hit.collider.GetComponentInParent<ClayPigeon>();
                if (clay != null)
                {
                    clay.RegisterShotHit(hit.point, direction);
                }

                // 4. Botones interactivos en panel de menú
                var selector = hit.collider.GetComponentInParent<Tiro.UI.DisciplineSelectorPanel>();
                if (selector != null)
                {
                    selector.HandleShotOnButton(hit.collider.name.ToLower());
                }
            }

            StartCoroutine(RenderTracer(origin, hitPoint));

            if (muzzleFlash != null) muzzleFlash.Play();
            if (audioSource != null && rifleData != null && rifleData.gunshotSound != null)
            {
                audioSource.PlayOneShot(rifleData.gunshotSound);
            }

            TriggerHaptics();
            OnRifleFired?.Invoke();
        }

        private IEnumerator RenderTracer(Vector3 start, Vector3 end)
        {
            GameObject tracer = new GameObject("RifleTracer");
            var lr = tracer.AddComponent<LineRenderer>();
            lr.startWidth = 0.015f;
            lr.endWidth = 0.008f;
            lr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lr.material.color = new Color(0.4f, 0.9f, 1f, 1f); // Trazador azul olímpico
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            yield return new WaitForSeconds(0.05f);
            Destroy(tracer);
        }

        public void TryReload()
        {
            if (isReloading || currentAmmo >= maxAmmo) return;
            StartCoroutine(ReloadRoutine());
        }

        private IEnumerator ReloadRoutine()
        {
            isReloading = true;
            UpdateDisplay();

            if (audioSource != null && rifleData != null && rifleData.reloadSound != null)
            {
                audioSource.PlayOneShot(rifleData.reloadSound);
            }

            yield return new WaitForSeconds(0.6f);

            currentAmmo = maxAmmo;
            isReloading = false;
            UpdateDisplay();
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
            OnRifleReloaded?.Invoke();
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
                ammoText.text = $"{currentAmmo}/{maxAmmo}";
                ammoText.color = currentAmmo > 3 ? Color.cyan : (currentAmmo > 0 ? Color.yellow : Color.red);
            }
        }

        private void TriggerHaptics()
        {
            var interactors = FindObjectsByType<XRBaseInputInteractor>(FindObjectsSortMode.None);
            foreach (var it in interactors)
            {
                if (it.name.ToLower().Contains("right"))
                {
                    it.SendHapticImpulse(0.85f, 0.12f);
                }
            }
        }
    }
}
