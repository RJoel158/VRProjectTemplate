using UnityEngine;

namespace Tiro.Data
{
    [CreateAssetMenu(fileName = "NewShootingRangeConfig", menuName = "VR Sports/Tiro/Range Config")]
    public class ShootingRangeConfigSO : ScriptableObject
    {
        [Header("Series Configuration")]
        [Tooltip("Número de disparos por serie olímpica reglamentaria.")]
        public int shotsPerSeries = 10;

        [Tooltip("Tiempo límite en segundos para completar la serie (0 = sin límite).")]
        public float timeLimitSeconds = 90f;

        [Header("Medal Thresholds (Puntos sobre 100)")]
        public int goldMedalScore = 90;
        public int silverMedalScore = 75;
        public int bronzeMedalScore = 60;

        [Header("Distances Available")]
        public float nearDistance = 10f;
        public float midDistance = 25f;
        public float farDistance = 50f;
    }
}
