using UnityEngine;

namespace Tiro.Data
{
    [CreateAssetMenu(fileName = "NewPistolData", menuName = "VR Sports/Tiro/Pistol Data")]
    public class PistolDataSO : ScriptableObject
    {
        [Header("Weapon Identity")]
        public string pistolName = "Pistola Olímpica 9mm";

        [Header("Ballistics & Fire Rate")]
        [Tooltip("Tiempo mínimo entre disparos en segundos.")]
        public float fireCooldown = 0.18f;

        [Tooltip("Capacidad del cargador (disparos por serie reglamentaria olímpica).")]
        public int magazineCapacity = 10;

        [Tooltip("Alcance máximo del proyectil en metros.")]
        public float maxRange = 100f;

        [Header("Haptics & Recoil")]
        [Tooltip("Intensidad de la vibración háptica al disparar (0 a 1).")]
        [Range(0.1f, 1f)]
        public float hapticIntensity = 0.9f;

        [Tooltip("Duración de la vibración háptica en segundos.")]
        public float hapticDurationSeconds = 0.12f;

        [Tooltip("Distancia en metros que retrocede visualmente la corredera.")]
        public float slideRecoilDistance = 0.035f;

        [Tooltip("Ángulo en grados que la pistola cabecea hacia arriba por retroceso.")]
        public float muzzleClimbDegrees = 3.5f;

        [Header("Audio")]
        public AudioClip gunshotSound;
        public AudioClip dryFireSound;
        public AudioClip reloadSound;
    }
}
