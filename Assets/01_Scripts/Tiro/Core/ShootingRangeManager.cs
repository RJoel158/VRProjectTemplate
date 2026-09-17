using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tiro.Data;
using Tiro.Targets;
using Tiro.Weapons;
using Tiro.Persistence;

namespace Tiro.Core
{
    /// <summary>
    /// Árbitro y gestor central del polígono de tiro deportivo olímpico.
    /// Administra las series de 10 disparos, tiempos, cambios de distancia (10m, 25m, 50m),
    /// cálculo de medallas olímpicas y persistencia en disco.
    /// </summary>
    public class ShootingRangeManager : MonoBehaviour
    {
        public static ShootingRangeManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private ShootingRangeConfigSO config;
        [SerializeField] private OlympicPistol pistol;
        [SerializeField] private List<TargetBoard> targets = new List<TargetBoard>();

        [Header("Runtime State")]
        [SerializeField] private int currentSeriesIndex = 1;
        [SerializeField] private int shotsFiredInSeries = 0;
        [SerializeField] private int currentSeriesScore = 0;
        [SerializeField] private int bullseyesInSeries = 0;
        [SerializeField] private float timeRemaining = 90f;
        [SerializeField] private bool seriesActive = false;
        [SerializeField] private int currentTargetIndex = 0;

        private ShootingSaveData currentSaveData;

        public int CurrentSeriesIndex => currentSeriesIndex;
        public int ShotsFiredInSeries => shotsFiredInSeries;
        public int CurrentSeriesScore => currentSeriesScore;
        public int BullseyesInSeries => bullseyesInSeries;
        public float TimeRemaining => timeRemaining;
        public bool IsSeriesActive => seriesActive;
        public ShootingSaveData SaveData => currentSaveData;
        public ShootingRangeConfigSO Config => config;

        public event Action<int, int, int> OnScoreUpdated; // (currentScore, shotsFired, maxShots)
        public event Action<int, bool, Vector3> OnShotLanded; // (score, isBullseye, hitPoint)
        public event Action<string, int, string> OnSeriesFinished; // (message, finalScore, medal)
        public event Action<int, float> OnSeriesStarted; // (seriesIndex, distance)
        public event Action<float> OnTimerUpdated; // (seconds)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            currentSaveData = ShootingSaveSystem.Load();
        }

        private void Start()
        {
            // Auto-detectar dianas si no se asignaron
            if (targets.Count == 0)
            {
                targets.AddRange(FindObjectsByType<TargetBoard>(FindObjectsSortMode.None));
            }

            foreach (var t in targets)
            {
                if (t != null)
                {
                    t.OnHitScored += HandleTargetHit;
                }
            }

            if (pistol == null)
            {
                pistol = FindAnyObjectByType<OlympicPistol>();
            }

            StartNewSeries(0);
        }

        private void OnDestroy()
        {
            foreach (var t in targets)
            {
                if (t != null)
                {
                    t.OnHitScored -= HandleTargetHit;
                }
            }
        }

        private void Update()
        {
            if (!seriesActive) return;

            float timeLimit = config != null ? config.timeLimitSeconds : 90f;
            if (timeLimit > 0f)
            {
                timeRemaining -= Time.deltaTime;
                OnTimerUpdated?.Invoke(Mathf.Max(0f, timeRemaining));

                if (timeRemaining <= 0f)
                {
                    CompleteSeries(timeExpired: true);
                }
            }
        }

        public void SelectTargetDistance(int index)
        {
            if (index < 0 || index >= targets.Count) return;
            currentTargetIndex = index;
            StartNewSeries(currentTargetIndex);
        }

        public void StartNewSeries(int targetIdx)
        {
            currentTargetIndex = Mathf.Clamp(targetIdx, 0, Mathf.Max(0, targets.Count - 1));
            shotsFiredInSeries = 0;
            currentSeriesScore = 0;
            bullseyesInSeries = 0;
            timeRemaining = config != null ? config.timeLimitSeconds : 90f;
            seriesActive = true;

            // Resetear dianas
            foreach (var t in targets)
            {
                if (t != null) t.ResetTarget();
            }

            // Recargar pistola
            if (pistol != null)
            {
                pistol.TryReload();
            }

            float dist = targets.Count > currentTargetIndex && targets[currentTargetIndex] != null
                ? targets[currentTargetIndex].DistanceMeters
                : 10f;

            int maxShots = config != null ? config.shotsPerSeries : 10;
            OnScoreUpdated?.Invoke(currentSeriesScore, shotsFiredInSeries, maxShots);
            OnSeriesStarted?.Invoke(currentSeriesIndex, dist);
        }

        private void HandleTargetHit(TargetBoard target, int score, bool isBullseye, Vector3 hitPoint)
        {
            if (!seriesActive) return;

            shotsFiredInSeries++;
            currentSeriesScore += score;
            if (isBullseye) bullseyesInSeries++;

            // Integración con ScoreManager general del proyecto
            if (ScoreManager.Instance != null && score > 0)
            {
                ScoreManager.Instance.AddPoints(score * 10);
            }

            int maxShots = config != null ? config.shotsPerSeries : 10;
            OnScoreUpdated?.Invoke(currentSeriesScore, shotsFiredInSeries, maxShots);
            OnShotLanded?.Invoke(score, isBullseye, hitPoint);

            // Comprobar si concluyó la serie reglamentaria
            if (shotsFiredInSeries >= maxShots)
            {
                CompleteSeries(timeExpired: false);
            }
        }

        private void CompleteSeries(bool timeExpired)
        {
            seriesActive = false;

            int gold = config != null ? config.goldMedalScore : 90;
            int silver = config != null ? config.silverMedalScore : 75;
            int bronze = config != null ? config.bronzeMedalScore : 60;

            string medal = "Sin Medalla";
            if (currentSeriesScore >= gold)
            {
                medal = "¡MEDALLA DE ORO OLÍMPICA! 🥇";
                if (currentSaveData != null) currentSaveData.goldMedals++;
            }
            else if (currentSeriesScore >= silver)
            {
                medal = "¡MEDALLA DE PLATA OLÍMPICA! 🥈";
                if (currentSaveData != null) currentSaveData.silverMedals++;
            }
            else if (currentSeriesScore >= bronze)
            {
                medal = "¡MEDALLA DE BRONCE OLÍMPICA! 🥉";
                if (currentSaveData != null) currentSaveData.bronzeMedals++;
            }

            // Actualizar datos persistentes
            if (currentSaveData == null) currentSaveData = new ShootingSaveData();
            currentSaveData.seriesCompleted++;
            currentSaveData.totalShotsFired += shotsFiredInSeries;
            currentSaveData.totalHitsOnTarget += (currentSeriesScore > 0 ? shotsFiredInSeries : 0);
            currentSaveData.totalBullseyes += bullseyesInSeries;

            if (currentSeriesScore > currentSaveData.highestScore)
            {
                currentSaveData.highestScore = currentSeriesScore;
            }

            float currentAccuracy = shotsFiredInSeries > 0 ? ((float)currentSeriesScore / (shotsFiredInSeries * 10f)) * 100f : 0f;
            if (currentAccuracy > currentSaveData.bestAccuracyPercentage)
            {
                currentSaveData.bestAccuracyPercentage = currentAccuracy;
            }

            ShootingSaveSystem.Save(currentSaveData);

            string msg = timeExpired
                ? $"¡Tiempo Agotado! Serie terminada con {currentSeriesScore} pts."
                : $"¡Serie Olímpica Completada! Puntuación final: {currentSeriesScore} pts.";

            OnSeriesFinished?.Invoke(msg, currentSeriesScore, medal);

            // Reiniciar siguiente serie tras 4 segundos de celebración
            StartCoroutine(AutoRestartNextSeriesRoutine());
        }

        private IEnumerator AutoRestartNextSeriesRoutine()
        {
            yield return new WaitForSeconds(4.5f);
            currentSeriesIndex++;
            StartNewSeries(currentTargetIndex);
        }
    }
}
