using System;
using System.Collections.Generic;
using UnityEngine;

namespace Golf.Data
{
    /// <summary>
    /// ScriptableObject de arquitectura para el estado y progreso en tiempo real de la partida de Golf.
    /// Permite inspeccionar en vivo desde el Inspector de Unity:
    /// - Hoyo actual y nombre
    /// - Cantidad de golpes en el hoyo actual
    /// - Golpes acumulados en el circuito
    /// - Última posición y velocidad de golpe
    /// - Desglose de puntuación por hoyo (Par, Birdie, Bogey)
    /// </summary>
    [CreateAssetMenu(fileName = "GolfProgressData", menuName = "VR Sports/Golf/Session Progress")]
    public class GolfProgressSO : ScriptableObject
    {
        [Header("Progreso Actual del Circuito")]
        [Tooltip("Índice del hoyo actual (0 a N-1).")]
        [SerializeField] private int currentHoleIndex = 0;
        [Tooltip("Nombre descriptivo del hoyo en juego.")]
        [SerializeField] private string currentHoleName = "Hoyo 1";
        [Tooltip("Par del hoyo actual.")]
        [SerializeField] private int currentHolePar = 2;

        [Header("Golpes")]
        [Tooltip("Golpes ejecutados en el hoyo actual.")]
        [SerializeField] private int strokesCurrentHole = 0;
        [Tooltip("Total de golpes acumulados en todo el circuito.")]
        [SerializeField] private int totalCourseStrokes = 0;

        [Header("Último Golpe Realizado")]
        [Tooltip("Posición en el mundo desde donde se ejecutó el último golpe.")]
        [SerializeField] private Vector3 lastShotPosition = Vector3.zero;
        [Tooltip("Vector de velocidad y dirección del último impacto.")]
        [SerializeField] private Vector3 lastShotVelocity = Vector3.zero;
        [Tooltip("Fuerza en m/s del último tiro.")]
        [SerializeField] private float lastShotSpeed = 0f;
        [Tooltip("Hora o timestamp del último tiro.")]
        [SerializeField] private string lastShotTime = "";

        [Header("Historial de Hoyos Completados")]
        [SerializeField] private List<int> scoresPerHole = new List<int>();
        [SerializeField] private bool isCourseCompleted = false;
        [SerializeField] private int bestScoreRecord = 999;

        // Getters públicos para UI y sistemas externos
        public int CurrentHoleIndex => currentHoleIndex;
        public string CurrentHoleName => currentHoleName;
        public int CurrentHolePar => currentHolePar;
        public int StrokesCurrentHole => strokesCurrentHole;
        public int TotalCourseStrokes => totalCourseStrokes;
        public Vector3 LastShotPosition => lastShotPosition;
        public Vector3 LastShotVelocity => lastShotVelocity;
        public float LastShotSpeed => lastShotSpeed;
        public IReadOnlyList<int> ScoresPerHole => scoresPerHole;
        public bool IsCourseCompleted => isCourseCompleted;
        public int BestScoreRecord => bestScoreRecord;

        public event Action OnProgressUpdated;

        /// <summary>
        /// Reinicia la sesión al iniciar un nuevo circuito completo.
        /// </summary>
        public void ResetSession(int startingHoleIndex, string holeName, int par)
        {
            currentHoleIndex = startingHoleIndex;
            currentHoleName = holeName;
            currentHolePar = par;
            strokesCurrentHole = 0;
            totalCourseStrokes = 0;
            lastShotPosition = Vector3.zero;
            lastShotVelocity = Vector3.zero;
            lastShotSpeed = 0f;
            lastShotTime = "";
            scoresPerHole.Clear();
            isCourseCompleted = false;

            OnProgressUpdated?.Invoke();
        }

        /// <summary>
        /// Configura el nuevo hoyo activo.
        /// </summary>
        public void StartHole(int index, string holeName, int par)
        {
            currentHoleIndex = index;
            currentHoleName = holeName;
            currentHolePar = par;
            strokesCurrentHole = 0;

            OnProgressUpdated?.Invoke();
        }

        /// <summary>
        /// Registra un golpe ejecutado con la pelota.
        /// </summary>
        public void RecordStroke(Vector3 shotPosition, Vector3 shotVelocity)
        {
            strokesCurrentHole++;
            totalCourseStrokes++;
            lastShotPosition = shotPosition;
            lastShotVelocity = shotVelocity;
            lastShotSpeed = shotVelocity.magnitude;
            lastShotTime = DateTime.Now.ToString("HH:mm:ss");

            OnProgressUpdated?.Invoke();
        }

        /// <summary>
        /// Registra una penalización por fuera de límites (Out of Bounds).
        /// </summary>
        public void RecordPenalty(int penaltyStrokes)
        {
            strokesCurrentHole += penaltyStrokes;
            totalCourseStrokes += penaltyStrokes;

            OnProgressUpdated?.Invoke();
        }

        /// <summary>
        /// Registra la finalización del hoyo actual.
        /// </summary>
        public void CompleteHole(int strokes)
        {
            scoresPerHole.Add(strokes);
            OnProgressUpdated?.Invoke();
        }

        /// <summary>
        /// Registra la finalización del circuito completo.
        /// </summary>
        public void CompleteCourse(int bestScore)
        {
            isCourseCompleted = true;
            bestScoreRecord = bestScore;
            OnProgressUpdated?.Invoke();
        }
    }
}
