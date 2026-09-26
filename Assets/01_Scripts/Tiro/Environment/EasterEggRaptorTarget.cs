using UnityEngine;

namespace Tiro.Environment
{
    /// <summary>
    /// Componente de objetivo Easter Egg para las aves rapaces en el cielo.
    /// Al ser impactada por el Rifle de Precision:
    /// - Explota en una nube de plumas low-poly.
    /// - Reproduce un sonidito comico arcade.
    /// - Otorga el doble de puntos (activable una sola vez por sesion).
    /// </summary>
    public class EasterEggRaptorTarget : MonoBehaviour
    {
        private Material featherMat;
        private bool isHit = false;

        public void Initialize(Material mat)
        {
            featherMat = mat;
        }

        public void OnShotByRifle(Vector3 hitPoint)
        {
            if (isHit) return;
            isHit = true;

            // 1. Sonidito cómico arcade
            Tiro.Audio.ShootingAudioManager.PlayEasterEggSound();

            // 2. Activar doble de puntos en ShootingRangeManager
            if (Tiro.Core.ShootingRangeManager.Instance != null)
            {
                Tiro.Core.ShootingRangeManager.Instance.TriggerEasterEggBonus();
            }

            // 3. Efecto de explosión de plumas
            SpawnFeatherExplosion(hitPoint);

            // 4. Destruir el ave
            Destroy(gameObject);
        }

        private void SpawnFeatherExplosion(Vector3 pos)
        {
            GameObject burstObj = new GameObject("Raptor_Feather_Burst");
            burstObj.transform.position = pos;

            for (int i = 0; i < 14; i++)
            {
                GameObject feather = GameObject.CreatePrimitive(PrimitiveType.Cube);
                feather.name = "Feather_Piece";
                feather.transform.SetParent(burstObj.transform, false);
                feather.transform.position = pos + Random.insideUnitSphere * 0.45f;
                feather.transform.localScale = new Vector3(0.14f, 0.025f, 0.25f);
                feather.transform.rotation = Random.rotation;

                Destroy(feather.GetComponent<Collider>());
                if (featherMat != null) feather.GetComponent<Renderer>().sharedMaterial = featherMat;

                var rb = feather.AddComponent<Rigidbody>();
                rb.mass = 0.02f;
                rb.linearDamping = 1.4f;
                rb.angularDamping = 2.2f;
                rb.linearVelocity = Random.insideUnitSphere * Random.Range(3.5f, 7.5f) + Vector3.up * 2f;
                rb.angularVelocity = Random.insideUnitSphere * 14f;
            }

            Destroy(burstObj, 3.5f);
        }
    }
}
