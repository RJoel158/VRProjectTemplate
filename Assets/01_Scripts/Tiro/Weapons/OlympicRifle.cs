using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.InputSystem;
using TMPro;
using Tiro.Data;
using Tiro.Targets;
using Tiro.Audio;

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
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.spatialBlend = 0f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 100f;
            audioSource.maxDistance = 1000f;
            audioSource.volume = 1f;
            audioSource.playOnAwake = false;
            audioSource.mute = false;

            currentAmmo = maxAmmo;
            SetupM1GarandSights();
        }

        private void OnEnable()
        {
            BindToRightHand();
        }

        private void Start()
        {
            BindToRightHand();
            SetupM1GarandSights();
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
            else
            {
                // Bloqueo firme de posición y rotación relativa al mando para evitar desorientación o giros anómalos
                transform.localPosition = gripOffset;
                transform.localRotation = Quaternion.Euler(gripEulerAngles);
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
            // 1. Prioridad Máxima: Buscar coincidencia exacta por nombre de mando físico
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
                ShootingAudioManager.PlayDryFire(rifleData != null ? rifleData.dryFireSound : null);
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

            ShootingAudioManager.PlayGunshot(Core.ShootingDiscipline.OlympicRifleDistance, rifleData != null ? rifleData.gunshotSound : null);

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

            ShootingAudioManager.PlayReload(rifleData != null ? rifleData.reloadSound : null);

            yield return new WaitForSeconds(0.6f);

            currentAmmo = maxAmmo;
            isReloading = false;
            UpdateDisplay();
            OnAmmoChanged?.Invoke(currentAmmo, maxAmmo);
            OnRifleReloaded?.Invoke();
        }

        public void PlayWeaponSound(AudioClip clip, float volume = 1f)
        {
            if (clip == null) return;
            ShootingAudioManager.PlaySound(clip, volume);
        }

        /// <summary>
        /// Configura el sistema de miras estilo M1 Garand / Ruger 10/22:
        /// - Alza trasera (Diopter Housing): anillo ahuecado (peep sight) con orificio abierto en el centro.
        /// - Mira delantera: poste fino ("piquito") con puntita óptica luminosa y aletas protectoras abiertas.
        /// - Pantalla de munición: compacta en el costado del recibidor, sin interferir en la línea de mira.
        /// </summary>
        public void SetupM1GarandSights()
        {
            // 1. Alza Trasera Ahuecada (M1 Garand Peep Sight)
            Transform diopter = null;
            foreach (var tr in GetComponentsInChildren<Transform>(true))
            {
                if (tr.name == "Diopter_Housing") { diopter = tr; break; }
            }

            if (diopter != null)
            {
                diopter.localPosition = new Vector3(0f, 0.065f, -0.08f);
                diopter.localRotation = Quaternion.identity;
                diopter.localScale = Vector3.one;

                MeshFilter mf = diopter.GetComponent<MeshFilter>();
                if (mf != null)
                {
                    // Orificio de 11mm de diámetro (5.5mm radio) para visión totalmente nítida y despejada
                    mf.sharedMesh = GeneratePeepApertureMesh(innerRadius: 0.0055f, outerRadius: 0.0135f, thickness: 0.0025f, segments: 28);
                }

                var col = diopter.GetComponent<Collider>();
                if (col != null) Destroy(col);

                MeshRenderer mr = diopter.GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterial == null)
                {
                    mr.material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.12f, 0.12f, 0.14f) };
                }

                // Vástago de sujeción al cajón
                Transform stem = diopter.Find("Peep_Stem");
                if (stem == null)
                {
                    GameObject stemObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    stemObj.name = "Peep_Stem";
                    stemObj.transform.SetParent(diopter, false);
                    stemObj.transform.localPosition = new Vector3(0f, -0.016f, 0f);
                    stemObj.transform.localScale = new Vector3(0.005f, 0.022f, 0.003f);
                    var stemCol = stemObj.GetComponent<Collider>();
                    if (stemCol != null) Destroy(stemCol);
                    if (mr != null) stemObj.GetComponent<Renderer>().material = mr.material;
                }
            }

            if (rearDiopterSight != null)
            {
                rearDiopterSight.localPosition = new Vector3(0f, 0.065f, -0.08f);
            }

            // 2. Desactivar túnel cilíndrico opaco
            foreach (var tr in GetComponentsInChildren<Transform>(true))
            {
                if (tr.name == "Front_Globe_Tunnel")
                {
                    tr.gameObject.SetActive(false);
                }
            }

            // 3. Mira delantera tipo M1 Garand / Ruger 10/22 (Poste central / "piquito" con puntita luminosa)
            Transform frontSightGroup = transform.Find("Garand_Front_Sight");
            if (frontSightGroup == null)
            {
                GameObject fsgObj = new GameObject("Garand_Front_Sight");
                fsgObj.transform.SetParent(transform, false);
                frontSightGroup = fsgObj.transform;
                frontSightGroup.localPosition = new Vector3(0f, 0f, 0.68f);

                Material gunMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.12f, 0.12f, 0.14f) };
                Material tipMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(1f, 0.45f, 0.05f) }; // Naranja flúor

                // Base sobre el cañón
                GameObject baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                baseBlock.name = "Sight_Base";
                baseBlock.transform.SetParent(frontSightGroup, false);
                baseBlock.transform.localPosition = new Vector3(0f, 0.042f, 0f);
                baseBlock.transform.localScale = new Vector3(0.018f, 0.008f, 0.012f);
                baseBlock.GetComponent<Renderer>().material = gunMat;
                Destroy(baseBlock.GetComponent<Collider>());

                // Poste central ("Piquito")
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = "Center_Post";
                post.transform.SetParent(frontSightGroup, false);
                post.transform.localPosition = new Vector3(0f, 0.054f, 0f);
                post.transform.localScale = new Vector3(0.0022f, 0.018f, 0.003f);
                post.GetComponent<Renderer>().material = gunMat;
                Destroy(post.GetComponent<Collider>());

                // Puntita brillante de puntería (Bead a y = 0.065f)
                GameObject tipBead = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tipBead.name = "Sight_Tip_Bead";
                tipBead.transform.SetParent(frontSightGroup, false);
                tipBead.transform.localPosition = new Vector3(0f, 0.0645f, 0f);
                tipBead.transform.localScale = new Vector3(0.003f, 0.003f, 0.0035f);
                tipBead.GetComponent<Renderer>().material = tipMat;
                Destroy(tipBead.GetComponent<Collider>());

                // Orejas protectoras abiertas M1 Garand (alas laterales)
                GameObject leftWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leftWing.name = "Left_Wing";
                leftWing.transform.SetParent(frontSightGroup, false);
                leftWing.transform.localPosition = new Vector3(-0.009f, 0.057f, 0f);
                leftWing.transform.localScale = new Vector3(0.0018f, 0.018f, 0.010f);
                leftWing.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);
                leftWing.GetComponent<Renderer>().material = gunMat;
                Destroy(leftWing.GetComponent<Collider>());

                GameObject rightWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rightWing.name = "Right_Wing";
                rightWing.transform.SetParent(frontSightGroup, false);
                rightWing.transform.localPosition = new Vector3(0.009f, 0.057f, 0f);
                rightWing.transform.localScale = new Vector3(0.0018f, 0.018f, 0.010f);
                rightWing.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
                rightWing.GetComponent<Renderer>().material = gunMat;
                Destroy(rightWing.GetComponent<Collider>());
            }

            if (frontSight != null)
            {
                frontSight.localPosition = new Vector3(0f, 0.065f, 0.68f);
            }

            // 4. Ubicar pantalla OLED lateral de munición sin obstruir miras
            if (ammoText != null)
            {
                ammoText.transform.SetParent(transform, true);
                ammoText.transform.localPosition = new Vector3(-0.024f, 0.035f, 0.05f);
                ammoText.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                ammoText.transform.localScale = Vector3.one;
                ammoText.fontSize = 0.22f;
                ammoText.alignment = TextAlignmentOptions.Center;
            }
        }

        private static Mesh GeneratePeepApertureMesh(float innerRadius, float outerRadius, float thickness, int segments)
        {
            Mesh mesh = new Mesh();
            mesh.name = "Procedural_M1Garand_PeepSight";

            int vCount = segments * 4;
            Vector3[] vertices = new Vector3[vCount];
            Vector3[] normals = new Vector3[vCount];
            Vector2[] uvs = new Vector2[vCount];

            float halfThick = thickness * 0.5f;

            for (int i = 0; i < segments; i++)
            {
                float angle = (float)i / segments * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                // Front Face (z = +halfThick)
                vertices[i] = new Vector3(cos * outerRadius, sin * outerRadius, halfThick);
                normals[i] = Vector3.forward;
                uvs[i] = new Vector2((cos + 1f) * 0.5f, (sin + 1f) * 0.5f);

                int ifIdx = segments + i;
                vertices[ifIdx] = new Vector3(cos * innerRadius, sin * innerRadius, halfThick);
                normals[ifIdx] = Vector3.forward;
                uvs[ifIdx] = new Vector2((cos * 0.5f + 1f) * 0.5f, (sin * 0.5f + 1f) * 0.5f);

                // Back Face (z = -halfThick)
                int obIdx = 2 * segments + i;
                vertices[obIdx] = new Vector3(cos * outerRadius, sin * outerRadius, -halfThick);
                normals[obIdx] = Vector3.back;
                uvs[obIdx] = new Vector2((cos + 1f) * 0.5f, (sin + 1f) * 0.5f);

                int ibIdx = 3 * segments + i;
                vertices[ibIdx] = new Vector3(cos * innerRadius, sin * innerRadius, -halfThick);
                normals[ibIdx] = Vector3.back;
                uvs[ibIdx] = new Vector2((cos * 0.5f + 1f) * 0.5f, (sin * 0.5f + 1f) * 0.5f);
            }

            int[] indices = new int[segments * 24];
            int idx = 0;

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;

                // Front quad (outer to inner)
                indices[idx++] = i;
                indices[idx++] = segments + i;
                indices[idx++] = next;

                indices[idx++] = next;
                indices[idx++] = segments + i;
                indices[idx++] = segments + next;

                // Back quad (inner to outer)
                indices[idx++] = 2 * segments + i;
                indices[idx++] = 2 * segments + next;
                indices[idx++] = 3 * segments + i;

                indices[idx++] = 2 * segments + next;
                indices[idx++] = 3 * segments + next;
                indices[idx++] = 3 * segments + i;

                // Outer rim
                indices[idx++] = i;
                indices[idx++] = next;
                indices[idx++] = 2 * segments + i;

                indices[idx++] = next;
                indices[idx++] = 2 * segments + next;
                indices[idx++] = 2 * segments + i;

                // Inner rim (apertura interior abierta)
                indices[idx++] = segments + i;
                indices[idx++] = 3 * segments + i;
                indices[idx++] = segments + next;

                indices[idx++] = segments + next;
                indices[idx++] = 3 * segments + i;
                indices[idx++] = 3 * segments + next;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.triangles = indices;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            return mesh;
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
