using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.InputSystem;
using TMPro;
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

        [Header("Ammo OLED Display (Contador Digital en la Pistola)")]
        [Tooltip("Texto digital en la cara trasera de la corredera que muestra las balas restantes.")]
        [SerializeField] private TextMeshPro ammoCounterText;
        [Tooltip("Barra de puntos luminosos o instrucción de recarga.")]
        [SerializeField] private TextMeshPro ammoBarText;
        [SerializeField] private Transform ammoDisplayRoot;

        [Header("FX & Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private ParticleSystem muzzleFlashParticles;
        [SerializeField] private LineRenderer bulletTracerPrefab;
        [SerializeField] private GameObject bulletHolePrefab;

        [Header("Direct VR Hand Binding")]
        [Tooltip("Si es true, la pistola se vincula automáticamente al mando derecho. Si lo desmarcas (false), puedes moverla y emparentarla 100% a mano.")]
        [SerializeField] private bool bindToRightControllerOnStart = true;
        [Tooltip("Mando o mano específica. Si lo dejas vacío, busca automáticamente el Right Controller.")]
        [SerializeField] private Transform manualHandTarget;
        [Tooltip("Offset de posición local respecto al mando/mano (para moverla adelante/atrás, arriba/abajo).")]
        [SerializeField] private Vector3 gripOffset = new Vector3(0f, -0.025f, 0.08f);
        [Tooltip("Rotación Euler local respecto al mando/mano (0,0,0 apunta recto al frente).")]
        [SerializeField] private Vector3 gripEulerAngles = Vector3.zero;
        [Tooltip("Permite mover los valores de Offset y Rotación en el Inspector durante Play Mode y ver el resultado en tiempo real.")]
        [SerializeField] private bool liveCalibrationInPlayMode = true;

        [Header("Input Actions (Opcional - se complementa con lectura directa de hardware XR)")]
        [SerializeField] private InputActionProperty fireAction;
        [SerializeField] private InputActionProperty reloadAction;

        private int currentAmmo;
        private float lastFireTimestamp = -10f;
        private bool isReloading = false;
        private bool wasTriggerPulled = false;
        private bool wasReloadBtnPressed = false;
        private bool gestureArmed = true;
        private Vector3 lastHandPos;
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
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            // Configurar AudioSource para entrega estéreo directa, nítida y a máximo volumen al jugador
            audioSource.spatialBlend = 0f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 100f;
            audioSource.maxDistance = 1000f;
            audioSource.volume = 1f;
            audioSource.playOnAwake = false;
            audioSource.mute = false;

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

            lastHandPos = transform.position;
            EnsureAmmoDisplayCreated();
            UpdateAmmoDisplay();

            OnAmmoChanged?.Invoke(currentAmmo, MaxAmmo);
        }

        private void LateUpdate()
        {
            if (bindToRightControllerOnStart)
            {
                Transform target = manualHandTarget != null ? manualHandTarget : boundHandTarget;
                if (target == null || transform.parent != target)
                {
                    BindToRightHand();
                }
                else
                {
                    // Bloqueo firme de posición y rotación relativa al mando para evitar desorientación o giros anómalos
                    transform.localPosition = gripOffset;
                    transform.localRotation = Quaternion.Euler(gripEulerAngles);
                }
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
            boundHandTarget = manualHandTarget != null ? manualHandTarget : FindRightHandTransform();

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
            // 1. Prioridad Máxima: Buscar coincidencia exacta por nombre de mando físico (evita estabilizadores u orígenes de teletransporte)
            var allGos = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allGos)
            {
                string n = go.name.Trim();
                if (n.Equals("Right Controller", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("RightHand Controller", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("Right Hand", StringComparison.OrdinalIgnoreCase) ||
                    n.Equals("RightHand", StringComparison.OrdinalIgnoreCase))
                {
                    return go.transform;
                }
            }

            // 2. Buscar GameObject con TrackedPoseDriver cuyo nombre corresponda al mando derecho físico
            var poseDrivers = FindObjectsByType<UnityEngine.InputSystem.XR.TrackedPoseDriver>(FindObjectsSortMode.None);
            foreach (var pd in poseDrivers)
            {
                string n = pd.gameObject.name.ToLower();
                if (n.Contains("right") && !n.Contains("stabiliz") && !n.Contains("attach") &&
                    !n.Contains("origin") && !n.Contains("teleport") && !n.Contains("ray"))
                {
                    return pd.transform;
                }
            }

            // 3. Buscar controladores XRBaseController estándar
            var controllers = FindObjectsByType<UnityEngine.XR.Interaction.Toolkit.XRBaseController>(FindObjectsSortMode.None);
            foreach (var c in controllers)
            {
                string n = c.name.ToLower();
                if (n.Contains("right") && !n.Contains("stabiliz") && !n.Contains("attach") &&
                    !n.Contains("teleport") && !n.Contains("ray"))
                {
                    return c.transform;
                }
            }

            // 4. Buscar en XROrigin CameraFloorOffset filtrando estrictamente estabilizadores, rayos y teletransporte
            var origin = FindAnyObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (origin != null && origin.CameraFloorOffsetObject != null)
            {
                var children = origin.CameraFloorOffsetObject.GetComponentsInChildren<Transform>(true);
                foreach (var t in children)
                {
                    string lower = t.name.ToLower();
                    if (lower.Contains("right") && (lower.Contains("controller") || lower.Contains("hand")))
                    {
                        if (!lower.Contains("stabiliz") && !lower.Contains("attach") && !lower.Contains("origin") &&
                            !lower.Contains("ray") && !lower.Contains("poke") && !lower.Contains("teleport") &&
                            !lower.Contains("visual") && !lower.Contains("interactor") && !lower.Contains("turn") &&
                            !lower.Contains("move"))
                        {
                            return t;
                        }
                    }
                }
            }

            // Fallback seguro de jerarquía
            for (int i = 0; i < allGos.Length; i++)
            {
                string n = allGos[i].name.ToLower();
                if (n.Contains("right") && (n.Contains("hand") || n.Contains("controller")))
                {
                    if (!n.Contains("stabiliz") && !n.Contains("attach") && !n.Contains("origin") &&
                        !n.Contains("ray") && !n.Contains("poke") && !n.Contains("visual") &&
                        !n.Contains("callout") && !n.Contains("mesh") && !n.Contains("interactor") &&
                        !n.Contains("teleport") && !n.Contains("turn") && !n.Contains("move"))
                    {
                        return allGos[i].transform;
                    }
                }
            }

            return null;
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
        /// Recarga táctica olímpica al hacer el movimiento de la mano hacia abajo (estilo Pistol Whip):
        /// 1) Inclinando el arma hacia abajo (> 22° respecto al horizonte, forward.y < -0.38f)
        /// 2) O realizando un movimiento/sacudida dinámico rápido hacia abajo con la mano.
        /// </summary>
        private void CheckGestureReload()
        {
            if (isReloading) return;

            // Si el arma ya tiene el cargador lleno, rearmar el gesto al volver a apuntar al frente
            if (currentAmmo >= MaxAmmo)
            {
                if (transform.forward.y > -0.22f)
                {
                    gestureArmed = true;
                }
                lastHandPos = transform.position;
                return;
            }

            // Detección 1: Inclinación cómoda y ergonómica hacia abajo
            bool isPointingDown = transform.forward.y < -0.38f;

            // Detección 2: Movimiento rápido hacia abajo (downward flick de la muñeca/mano)
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float downVelocity = (lastHandPos.y - transform.position.y) / dt;
            bool isFlickingDown = (downVelocity > 0.75f) && (transform.forward.y < -0.12f);

            if ((isPointingDown || isFlickingDown) && gestureArmed)
            {
                gestureArmed = false;
                TryReload();
            }
            else if (transform.forward.y > -0.22f)
            {
                // Se rearma automáticamente cuando el jugador regresa el arma a posición de encare
                gestureArmed = true;
            }

            lastHandPos = transform.position;
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
            UpdateAmmoDisplay();
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

                // 1. Dianas reactivas en pared dinámica
                DynamicTargetNode wallNode = hit.collider.GetComponentInParent<DynamicTargetNode>();
                if (wallNode != null)
                {
                    hitTarget = true;
                    var gallery = wallNode.GetComponentInParent<DynamicWallTargetGallery>();
                    if (gallery != null) gallery.RegisterNodeHit(wallNode, hit.point);
                }

                // 2. Platos voladores (Clay Pigeon)
                ClayPigeon clay = hit.collider.GetComponentInParent<ClayPigeon>();
                if (clay != null)
                {
                    hitTarget = true;
                    clay.RegisterShotHit(hit.point, direction);
                }

                // 3. Dianas olímpicas concéntricas fijas
                TargetBoard targetBoard = hit.collider.GetComponentInParent<TargetBoard>();
                if (targetBoard != null)
                {
                    hitTarget = true;
                    targetBoard.RegisterBulletHit(hit.point, hit.normal);
                }
                else
                {
                    // 4. Botones interactivos en el panel de menú
                    var selector = hit.collider.GetComponentInParent<Tiro.UI.DisciplineSelectorPanel>();
                    if (selector != null)
                    {
                        hitTarget = true;
                        selector.HandleShotOnButton(hit.collider.name.ToLower());
                    }
                    else if (!hitTarget)
                    {
                        SpawnImpactEffect(hit.point, hit.normal);
                    }
                }
            }

            // 2. Trazador de bala
            StartCoroutine(RenderBulletTracer(origin, targetHitPoint));

            // 3. Fogonazo y Audio
            if (muzzleFlashParticles != null)
            {
                muzzleFlashParticles.Play();
            }

            ShootingAudioManager.PlayGunshot(Core.ShootingDiscipline.DynamicPistolWall, pistolData != null ? pistolData.gunshotSound : null);

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
            UpdateAmmoDisplay();

            ShootingAudioManager.PlayReload(pistolData != null ? pistolData.reloadSound : null);

            // Breve vibración de recarga
            SendHaptic(0.4f, 0.08f);

            yield return new WaitForSeconds(0.55f);

            currentAmmo = MaxAmmo;
            isReloading = false;
            UpdateAmmoDisplay();
            OnAmmoChanged?.Invoke(currentAmmo, MaxAmmo);
            OnPistolReloaded?.Invoke();

            SendHaptic(0.6f, 0.12f);
        }

        private void PlayDryFireSound()
        {
            ShootingAudioManager.PlayDryFire(pistolData != null ? pistolData.dryFireSound : null);
            SendHaptic(0.25f, 0.05f);
        }

        public void PlayWeaponSound(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            ShootingAudioManager.PlaySound(clip, volume);
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

        /// <summary>
        /// Crea una pantalla OLED micro-compacta en el lateral izquierdo de la pistola
        /// para verificar la munición sin obstruir la línea de las miras de hierro.
        /// </summary>
        private void EnsureAmmoDisplayCreated()
        {
            if (ammoCounterText != null) return;

            // Destruir display previo si existía en el alza trasera
            var oldDisplay = transform.Find("Ammo_OLED_Display");
            if (oldDisplay != null) DestroyImmediate(oldDisplay.gameObject);
            if (slideTransform != null)
            {
                var oldSlideDisplay = slideTransform.Find("Ammo_OLED_Display");
                if (oldSlideDisplay != null) DestroyImmediate(oldSlideDisplay.gameObject);
                var oldAmmo = slideTransform.Find("AmmoDisplay");
                if (oldAmmo != null) DestroyImmediate(oldAmmo.gameObject);
            }

            // Crear en el lateral izquierdo de la pistola (emparentado al armazón raíz)
            GameObject oledObj = new GameObject("Ammo_OLED_Display");
            oledObj.transform.SetParent(transform, false);
            // Lateral izquierdo del armazón (x = -0.018m, y = 0.035m, z = 0.03m)
            oledObj.transform.localPosition = new Vector3(-0.019f, 0.035f, 0.03f);
            oledObj.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Orientado hacia el lateral izquierdo

            ammoDisplayRoot = oledObj.transform;

            // Marco / Fondo OLED oscuro
            GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.name = "OLED_Bezel";
            bg.transform.SetParent(oledObj.transform, false);
            bg.transform.localPosition = Vector3.zero;
            bg.transform.localRotation = Quaternion.identity;
            bg.transform.localScale = new Vector3(0.038f, 0.016f, 0.002f);
            var col = bg.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            var r = bg.GetComponent<Renderer>();
            if (r != null)
            {
                r.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                r.material.color = new Color(0.04f, 0.05f, 0.07f, 1f);
            }

            // Texto de munición en el display
            GameObject numObj = new GameObject("Ammo_Text");
            numObj.transform.SetParent(oledObj.transform, false);
            numObj.transform.localPosition = new Vector3(0f, 0f, -0.002f);
            numObj.transform.localRotation = Quaternion.identity;
            numObj.transform.localScale = Vector3.one * 0.01f;

            ammoCounterText = numObj.AddComponent<TextMeshPro>();
            ammoCounterText.alignment = TextAlignmentOptions.Center;
            ammoCounterText.fontSize = 3.2f;
            ammoCounterText.fontStyle = FontStyles.Bold;
            ammoCounterText.color = new Color(0f, 1f, 0.85f, 1f);
            ammoCounterText.text = $"{currentAmmo} / {MaxAmmo}";
        }

        private void UpdateAmmoDisplay()
        {
            if (ammoCounterText == null) return;

            if (isReloading)
            {
                ammoCounterText.text = "RECARGANDO...";
                ammoCounterText.color = new Color(1f, 0.85f, 0.1f);
                return;
            }

            if (currentAmmo > 3)
            {
                ammoCounterText.text = $"{currentAmmo} / {MaxAmmo}";
                ammoCounterText.color = new Color(0f, 1f, 0.85f); // Cyan menta cyber
            }
            else if (currentAmmo > 0)
            {
                ammoCounterText.text = $"{currentAmmo} / {MaxAmmo} !";
                ammoCounterText.color = new Color(1f, 0.6f, 0f); // Naranja ámbar
            }
            else
            {
                ammoCounterText.text = "0 - RECARGA ▼";
                ammoCounterText.color = new Color(1f, 0.2f, 0.2f); // Rojo alerta
            }
        }

        private string GetBulletDots(int current, int max)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < max; i++)
            {
                if (i < current) sb.Append("●");
                else sb.Append("○");
            }
            return sb.ToString();
        }
    }
}
