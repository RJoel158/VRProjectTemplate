using UnityEngine;

namespace Esgrima.Data
{
    [CreateAssetMenu(fileName = "NewSwordData", menuName = "VR Sports/Esgrima/Sword Data")]
    public class SwordDataSO : ScriptableObject
    {
        [Header("General Info")]
        [Tooltip("Nombre del modelo o tipo de espada.")]
        public string swordName = "Katana de Entrenamiento";

        [Header("Combat Mechanics")]
        [Tooltip("Fuerza con la que empuja al oponente al asestar un golpe válido.")]
        [Range(0.5f, 10f)]
        public float knockbackImpulse = 2.5f;

        [Tooltip("Daño o reducción de estabilidad que inflige por golpe.")]
        public int damage = 1;

        [Tooltip("Velocidad mínima de la hoja (m/s) para considerar el contacto como un corte válido y no un mero roce.")]
        [Range(0.2f, 5f)]
        public float minSwingVelocity = 1.0f;

        [Header("Haptics (Vibración Oculus Quest)")]
        [Range(0f, 1f)]
        public float hapticIntensity = 0.7f;

        [Range(0.02f, 0.5f)]
        public float hapticDurationSeconds = 0.12f;

        [Header("Audio Feedback")]
        public AudioClip clashSound; // Sonido al chocar espada con espada (Bloqueo/Parry)
        public AudioClip fleshHitSound; // Sonido al cortar/golpear al oponente
        public AudioClip swingWhooshSound; // Sonido de blandir la espada rápido
    }
}
