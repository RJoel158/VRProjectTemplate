using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tiro.Targets
{
    /// <summary>
    /// Máquina lanzadora de platos de tiro olímpico (Trap / Skeet Machine).
    /// Arroja platos en trayectorias parabólicas suaves y visibles por el cielo.
    /// </summary>
    public class ClayPigeonLauncher : MonoBehaviour
    {
        [Header("Launch Configuration")]
        [SerializeField] private Transform launchPoint;
        [SerializeField] private Material clayMaterial;
        [SerializeField] private bool launchLeftToRight = true;
        [SerializeField] private Vector3 leftBunkerOffset = new Vector3(-9.0f, 0.9f, 15f);
        [SerializeField] private float minLaunchSpeed = 16f;
        [SerializeField] private float maxLaunchSpeed = 22f;
        [SerializeField] private float minElevationAngle = 35f;
        [SerializeField] private float maxElevationAngle = 55f;
        [SerializeField] private float minAzimuthAngle = -25f;
        [SerializeField] private float maxAzimuthAngle = 25f;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip launchSound;

        [Header("Round State")]
        [SerializeField] private int claysPerRound = 10;
        [SerializeField] private float launchInterval = 3.5f;

        private Coroutine activeRoundRoutine;
        private int claysLaunched = 0;
        private int claysHit = 0;
        private bool isLauncherActive = false;

        public event Action<int, int> OnClayRoundProgress; // (launched, total)
        public event Action<int, Vector3> OnClayScored; // (points, hitPoint)
        public event Action<int, int> OnClayRoundFinished; // (hits, total)

        public bool IsRunning => isLauncherActive;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (launchPoint == null) launchPoint = transform;
        }

        public void StartClayRound()
        {
            StopClayRound();
            activeRoundRoutine = StartCoroutine(ClayRoundRoutine());
        }

        public void StopClayRound()
        {
            if (activeRoundRoutine != null)
            {
                StopCoroutine(activeRoundRoutine);
                activeRoundRoutine = null;
            }
            isLauncherActive = false;
        }

        private IEnumerator ClayRoundRoutine()
        {
            isLauncherActive = true;
            claysLaunched = 0;
            claysHit = 0;

            yield return new WaitForSeconds(1.0f);

            while (claysLaunched < claysPerRound && isLauncherActive)
            {
                LaunchSingleClay();
                claysLaunched++;
                OnClayRoundProgress?.Invoke(claysLaunched, claysPerRound);

                yield return new WaitForSeconds(launchInterval);
            }

            yield return new WaitForSeconds(3.0f); // Esperar que caiga el último plato
            isLauncherActive = false;
            OnClayRoundFinished?.Invoke(claysHit, claysPerRound);
        }

        public ClayPigeon LaunchSingleClay()
        {
            Vector3 origin;
            Vector3 launchVelocity;

            if (launchLeftToRight)
            {
                // Trayectoria fluida y elegante tipo frisbee planeando frente al tirador (de izquierda a derecha)
                origin = new Vector3(leftBunkerOffset.x, leftBunkerOffset.y, leftBunkerOffset.z);
                float vx = UnityEngine.Random.Range(10.0f, 12.5f); // Desplazamiento horizontal legible y suave
                float vy = UnityEngine.Random.Range(4.2f, 5.8f);   // Elevación inicial suave que permite un planeo majestuoso
                float vz = UnityEngine.Random.Range(1.0f, 2.5f);   // Ligero avance hacia el fondo del campo
                launchVelocity = new Vector3(vx, vy, vz);
            }
            else
            {
                origin = launchPoint != null ? launchPoint.position : transform.position;
                float elevation = UnityEngine.Random.Range(20f, 35f);
                float azimuth = UnityEngine.Random.Range(minAzimuthAngle, maxAzimuthAngle);
                Quaternion launchRot = Quaternion.Euler(-elevation, azimuth, 0f);
                launchVelocity = (launchRot * Vector3.forward) * UnityEngine.Random.Range(10.5f, 13.5f);
            }

            // Construir modelo procedural del plato (disco naranja fluorescente de 28cm con perfil frisbee)
            GameObject clayObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            clayObj.name = $"ClayPigeon_{claysLaunched + 1}";
            clayObj.transform.position = origin;
            clayObj.transform.localScale = new Vector3(0.28f, 0.025f, 0.28f);

            // Reemplazar collider por un BoxCollider de dimensiones óptimas para impacto consistente de perdigones
            var defaultCol = clayObj.GetComponent<Collider>();
            if (defaultCol != null) Object.Destroy(defaultCol);
            var hitCol = clayObj.AddComponent<BoxCollider>();
            hitCol.size = new Vector3(1.15f, 3.5f, 1.15f);

            var r = clayObj.GetComponent<Renderer>();
            if (r != null)
            {
                if (clayMaterial != null)
                {
                    r.material = clayMaterial;
                }
                else
                {
                    r.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    r.material.color = new Color(1f, 0.45f, 0.05f, 1f); // Naranja fluorescente
                }
            }

            var rb = clayObj.AddComponent<Rigidbody>();
            rb.mass = 0.11f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.linearVelocity = launchVelocity;

            var clay = clayObj.AddComponent<ClayPigeon>();
            clay.OnClayBroken += HandleClayBroken;

            if (audioSource != null && launchSound != null)
            {
                audioSource.PlayOneShot(launchSound);
            }

            return clay;
        }

        private void HandleClayBroken(ClayPigeon clay, int points, Vector3 hitPoint)
        {
            claysHit++;
            OnClayScored?.Invoke(points, hitPoint);
        }
    }
}
