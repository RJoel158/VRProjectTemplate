using UnityEngine;

namespace Esgrima.Data
{
    [CreateAssetMenu(fileName = "NewFighterConfig", menuName = "VR Sports/Esgrima/Fighter Config")]
    public class FighterConfigSO : ScriptableObject
    {
        [Header("Fighter Identity")]
        public string fighterName = "Rival Novato";

        [Header("Defense & Balance")]
        [Tooltip("Multiplicador de retroceso. 1 = normal, menor = más pesado / resistente.")]
        [Range(0.2f, 2f)]
        public float knockbackMultiplier = 1f;

        [Tooltip("Golpes que resiste antes de perder el equilibrio o quedar aturdido.")]
        public int maxHitsAllowed = 3;

        [Header("AI Behavior")]
        [Tooltip("Tiempo mínimo en segundos entre ataques de la IA.")]
        public float attackIntervalMin = 2.5f;

        [Tooltip("Tiempo máximo en segundos entre ataques de la IA.")]
        public float attackIntervalMax = 4.5f;

        [Tooltip("Tiempo de preparación/telegrafeado antes de que el bot lance el golpe.")]
        public float windupDuration = 0.8f;

        [Tooltip("Tiempo que queda aturdido (stagger) si el jugador bloquea su golpe espada con espada.")]
        public float staggerDuration = 1.2f;

        [Tooltip("Probabilidad (0 a 1) de que el bot intente bloquear cuando ve venir la espada del jugador.")]
        [Range(0f, 1f)]
        public float blockChance = 0.35f;
    }
}
