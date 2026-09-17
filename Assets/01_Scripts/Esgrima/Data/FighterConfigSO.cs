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

        [Header("Arena Footwork & Movement")]
        [Tooltip("Velocidad de desplazamiento por la arena (m/s).")]
        public float moveSpeed = 1.2f;

        [Tooltip("Velocidad de desplazamiento lateral (strafing).")]
        public float strafeSpeed = 0.8f;

        [Tooltip("Radio máximo seguro desde el centro del ring (el ring mide 5.0m).")]
        public float maxSafeArenaRadius = 3.6f;

        [Header("Combos & Parry Vulnerability")]
        [Tooltip("Probabilidad de encadenar combos de múltiples estocadas.")]
        [Range(0f, 1f)]
        public float comboChance = 0.45f;

        [Tooltip("Multiplicador de retroceso cuando el rival recibe un contragolpe durante el aturdimiento de parry.")]
        public float parryVulnerabilityMultiplier = 2.8f;
    }
}
