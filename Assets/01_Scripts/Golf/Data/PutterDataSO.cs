using System;
using UnityEngine;

namespace Golf.Data
{
    /// <summary>
    /// Configuracion del palo de golf (Putter).
    /// Define el multiplicador de potencia, masa del cabezal y feedback haptico al conectar con la bola.
    /// </summary>
    [CreateAssetMenu(fileName = "PutterConfig", menuName = "VR Sports/Golf/Putter Config")]
    public class PutterDataSO : ScriptableObject
    {
        [Header("Putter Geometry")]
        [Tooltip("Longitud promedio del shaft del putter en metros.")]
        [SerializeField] private float shaftLength = 0.88f;
        [Tooltip("Masa simulada del cabezal del putter en kg.")]
        [SerializeField] private float headMass = 0.35f;

        [Header("Impact Mechanics")]
        [Tooltip("Multiplicador de velocidad transferida de la cabeza del putter a la pelota.")]
        [SerializeField] private float impulseMultiplier = 1.35f;
        [Tooltip("Velocidad maxima que se puede imprimir a la bola para evitar lanzamientos fuera de control.")]
        [SerializeField] private float maxBallVelocity = 14f;
        [Tooltip("Velocidad minima del swing para registrar un tiro intencional (filtro de roces involuntarios).")]
        [SerializeField] private float minImpactVelocity = 0.12f;

        [Header("Haptic Feedback (XR)")]
        [Tooltip("Intensidad minima de vibracion en el mando Oculus.")]
        [Range(0f, 1f)]
        [SerializeField] private float minHapticAmplitude = 0.25f;
        [Tooltip("Intensidad maxima de vibracion para golpes potentes.")]
        [Range(0f, 1f)]
        [SerializeField] private float maxHapticAmplitude = 0.85f;
        [Tooltip("Duracion en segundos del pulso haptico.")]
        [SerializeField] private float hapticDuration = 0.08f;

        [Header("Aim Line")]
        [Tooltip("Permitir proyeccion de linea tenue de punteria cuando el putter esta cerca de la bola.")]
        [SerializeField] private bool showAimLine = true;
        [SerializeField] private float aimLineDistance = 2.5f;

        public float ShaftLength => shaftLength;
        public float HeadMass => headMass;
        public float ImpulseMultiplier => impulseMultiplier;
        public float MaxBallVelocity => maxBallVelocity;
        public float MinImpactVelocity => minImpactVelocity;

        public float MinHapticAmplitude => minHapticAmplitude;
        public float MaxHapticAmplitude => maxHapticAmplitude;
        public float HapticDuration => hapticDuration;

        public bool ShowAimLine => showAimLine;
        public float AimLineDistance => aimLineDistance;
    }
}
