using System;
using UnityEngine;

namespace Tiro.Targets
{
    /// <summary>
    /// Plato de tiro al plato (Clay Pigeon) naranja de alta visibilidad.
    /// Vuela con física balística por el cielo y estalla en fragmentos cerámicos al recibir perdigonadas.
    /// </summary>
    public class ClayPigeon : MonoBehaviour
    {
        [Header("State")]
        [SerializeField] private int pointValue = 10;
        [SerializeField] private float lifetimeSeconds = 6f;

        [Header("Frisbee Aerodynamics & Glide")]
        [SerializeField] private float liftRatio = 0.78f; // Contrarresta el 78% de la gravedad para un planeo prolongado tipo frisbee
        [SerializeField] private float spinSpeed = 600f;  // Giro estabilizador giroscópico visual
        [SerializeField] private float airDrag = 0.04f;   // Resistencia aerodinámica suave

        private Rigidbody rb;
        private bool isBroken = false;

        public event Action<ClayPigeon, int, Vector3> OnClayBroken; // (clay, points, hitPoint)

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            Destroy(gameObject, lifetimeSeconds);
        }

        private void FixedUpdate()
        {
            if (rb == null || isBroken) return;

            Vector3 horizVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float speed = horizVel.magnitude;

            if (speed > 0.5f)
            {
                // 1. Sustentación aerodinámica de planeador / frisbee:
                // Genera empuje vertical proporcional a la velocidad horizontal que amortigua la caída por gravedad
                float maxLift = Mathf.Abs(Physics.gravity.y) * liftRatio;
                float currentLift = Mathf.Clamp(speed * 0.82f, 0f, maxLift);
                rb.AddForce(Vector3.up * currentLift, ForceMode.Acceleration);

                // 2. Fricción aerodinámica suave en el plano horizontal para una deceleración progresiva
                rb.AddForce(-horizVel.normalized * (speed * airDrag), ForceMode.Acceleration);

                // 3. Suave orientación aerodinámica inclinada hacia la trayectoria
                Vector3 forwardFlat = horizVel.normalized;
                Vector3 tiltAxis = Vector3.Cross(Vector3.up, forwardFlat);
                Quaternion baseRotation = Quaternion.AngleAxis(-5f, tiltAxis);
                transform.rotation = Quaternion.Slerp(transform.rotation, baseRotation, 4f * Time.fixedDeltaTime);
            }

            // Giro rápido giroscópico sobre el eje normal del disco
            transform.Rotate(Vector3.up, spinSpeed * Time.fixedDeltaTime, Space.Self);
        }

        public void RegisterShotHit(Vector3 hitPoint, Vector3 hitDirection)
        {
            if (isBroken) return;
            isBroken = true;

            // 1. Sonido de estallido cerámico en el aire y al oído del jugador
            AudioClip shatterClip = Tiro.Audio.ShootingSoundFX.GetClayShatterSound();
            AudioSource.PlayClipAtPoint(shatterClip, hitPoint, 1f);
            if (Camera.main != null) AudioSource.PlayClipAtPoint(shatterClip, Camera.main.transform.position, 0.75f);

            // 2. Efecto de fragmentos cerámicos
            SpawnShatterParticles(hitPoint, hitDirection);

            // 3. Notificar puntos
            OnClayBroken?.Invoke(this, pointValue, hitPoint);

            // 4. Destruir el plato
            Destroy(gameObject);
        }

        private void SpawnShatterParticles(Vector3 point, Vector3 direction)
        {
            GameObject debrisObj = new GameObject("ClayDebris");
            debrisObj.transform.position = point;

            // Fragmentos físicos procedurales
            for (int i = 0; i < 6; i++)
            {
                GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "ClayShard";
                shard.transform.position = point + UnityEngine.Random.insideUnitSphere * 0.05f;
                shard.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.04f, 0.09f);

                var r = shard.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    r.material.color = new Color(1f, 0.4f, 0.05f); // Naranja cerámico
                }

                var col = shard.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var shardRb = shard.AddComponent<Rigidbody>();
                shardRb.mass = 0.05f;
                shardRb.linearVelocity = (direction + UnityEngine.Random.insideUnitSphere).normalized * UnityEngine.Random.Range(3f, 7f);

                Destroy(shard, 1.5f);
            }

            Destroy(debrisObj, 2f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            // Si cae al suelo sin ser impactado
            if (!isBroken)
            {
                Destroy(gameObject, 0.2f);
            }
        }
    }
}
