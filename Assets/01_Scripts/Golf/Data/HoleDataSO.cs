using System;
using UnityEngine;

namespace Golf.Data
{
    /// <summary>
    /// Informacion y reglas especificas para un hoyo individual de minigolf.
    /// Justifica el uso de ScriptableObjects permitiendo modularizar y crear hoyos sin recompilar codigo.
    /// </summary>
    [CreateAssetMenu(fileName = "Hole_01", menuName = "VR Sports/Golf/Hole Data")]
    public class HoleDataSO : ScriptableObject
    {
        [Header("Hole Identification")]
        [SerializeField] private int holeNumber = 1;
        [SerializeField] private string holeName = "Hoyo 1: Curva Verde";
        [TextArea(2, 4)]
        [SerializeField] private string description = "Pista plana con curvas de madera. Ideal para calibrar la fuerza del tiro.";

        [Header("Rules & Scoring")]
        [Tooltip("Par sugerido para este hoyo (golpes esperados).")]
        [SerializeField] private int par = 2;
        [Tooltip("Maximo de golpes permitidos antes de forzar el paso al siguiente hoyo.")]
        [SerializeField] private int maxStrokes = 6;
        [Tooltip("Penalizacion de golpes al caer fuera de pista (Out of Bounds).")]
        [SerializeField] private int outOfBoundsPenalty = 1;

        [Header("World Coordinates / References (Configurables en Scene)")]
        [Tooltip("Radio del vaso del hoyo en metros.")]
        [SerializeField] private float cupRadius = 0.085f;
        [Tooltip("Velocidad maxima a la que la bola puede entrar al hoyo sin pasar de largo.")]
        [SerializeField] private float cupMaxEntrySpeed = 2.8f;

        public int HoleNumber => holeNumber;
        public string HoleName => holeName;
        public string Description => description;
        public int Par => par;
        public int MaxStrokes => maxStrokes;
        public int OutOfBoundsPenalty => outOfBoundsPenalty;
        public float CupRadius => cupRadius;
        public float CupMaxEntrySpeed => cupMaxEntrySpeed;
    }
}
