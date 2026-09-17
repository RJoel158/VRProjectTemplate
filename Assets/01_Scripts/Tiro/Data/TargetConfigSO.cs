using UnityEngine;

namespace Tiro.Data
{
    [CreateAssetMenu(fileName = "NewTargetConfig", menuName = "VR Sports/Tiro/Target Config")]
    public class TargetConfigSO : ScriptableObject
    {
        [Header("Target Identity")]
        public string targetName = "Diana Concéntrica Olímpica 50cm";

        [Header("Dimensions (Meters)")]
        [Tooltip("Radio total del círculo de la diana en metros (0.25m = 50cm de diámetro).")]
        public float totalRadius = 0.25f;

        [Tooltip("Radio del Bullseye central 'X' (10 puntos perfectos).")]
        public float bullseyeRadius = 0.025f;

        [Tooltip("Radio de la zona 10.")]
        public float ring10Radius = 0.05f;

        [Tooltip("Radio de la zona 9.")]
        public float ring9Radius = 0.075f;

        [Tooltip("Radio de la zona 8.")]
        public float ring8Radius = 0.10f;

        [Tooltip("Radio de la zona 7.")]
        public float ring7Radius = 0.125f;

        [Tooltip("Radio de la zona 6.")]
        public float ring6Radius = 0.15f;

        [Tooltip("Radio de la zona 5.")]
        public float ring5Radius = 0.175f;

        [Tooltip("Radio de la zona 4.")]
        public float ring4Radius = 0.20f;

        [Tooltip("Radio de la zona 3.")]
        public float ring3Radius = 0.225f;

        [Header("Feedback Audio")]
        public AudioClip bullseyeHitSound;
        public AudioClip standardHitSound;

        /// <summary>
        /// Evalúa la distancia euclídea desde el centro de la diana y devuelve la puntuación de 0 a 10.
        /// </summary>
        public int CalculateScore(float distanceFromCenter, out bool isBullseye)
        {
            isBullseye = false;

            if (distanceFromCenter <= bullseyeRadius)
            {
                isBullseye = true;
                return 10;
            }
            if (distanceFromCenter <= ring10Radius) return 10;
            if (distanceFromCenter <= ring9Radius) return 9;
            if (distanceFromCenter <= ring8Radius) return 8;
            if (distanceFromCenter <= ring7Radius) return 7;
            if (distanceFromCenter <= ring6Radius) return 6;
            if (distanceFromCenter <= ring5Radius) return 5;
            if (distanceFromCenter <= ring4Radius) return 4;
            if (distanceFromCenter <= ring3Radius) return 3;
            if (distanceFromCenter <= totalRadius) return 1;

            return 0; // Fuera de la diana (Miss)
        }
    }
}
