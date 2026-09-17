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
            Vector3 origin = launchPoint != null ? launchPoint.position : transform.position;

            // Construir modelo procedural del plato (disco naranja de 22cm)
            GameObject clayObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            clayObj.name = $"ClayPigeon_{claysLaunched + 1}";
            clayObj.transform.position = origin;
            clayObj.transform.localScale = new Vector3(0.24f, 0.025f, 0.24f);

            var col = clayObj.GetComponent<Collider>();
            if (col == null) col = clayObj.AddComponent<MeshCollider>();

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
            rb.mass = 0.12f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Ángulo de lanzamiento parabólico
            float elevation = UnityEngine.Random.Range(minElevationAngle, maxElevationAngle);
            float azimuth = UnityEngine.Random.Range(minAzimuthAngle, maxAzimuthAngle);
            Quaternion launchRot = Quaternion.Euler(-elevation, azimuth, 0f);
            Vector3 launchVelocity = (launchRot * Vector3.forward) * UnityEngine.Random.Range(minLaunchSpeed, maxLaunchSpeed);

            rb.linearVelocity = launchVelocity;
            rb.angularVelocity = new Vector3(0f, 35f, 0f); // Giro estabilizador tipo frisbee

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
