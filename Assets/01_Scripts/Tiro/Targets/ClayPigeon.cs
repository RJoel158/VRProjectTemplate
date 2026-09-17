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

        public void RegisterShotHit(Vector3 hitPoint, Vector3 hitDirection)
        {
            if (isBroken) return;
            isBroken = true;

            // 1. Efecto de fragmentos cerámicos
            SpawnShatterParticles(hitPoint, hitDirection);

            // 2. Notificar puntos
            OnClayBroken?.Invoke(this, pointValue, hitPoint);

            // 3. Destruir el plato
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
