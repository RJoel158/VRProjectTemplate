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
    public enum ShootingDiscipline
    {
        OlympicRifleDistance = 0,   // 1. Rifle de Precisión (10m, 25m, 50m)
        DynamicPistolWall = 1,      // 2. Pistola Rápida Dinámica (Pared cercana)
        ClayPigeonShotgun = 2       // 3. Tiro al Plato (Escopeta deportiva)
    }

    /// <summary>
    /// Árbitro y gestor central del polígono de tiro deportivo olímpico.
    /// Administra las 3 disciplinas deportivas (Rifle a distancia, Pistola en pared dinámica, y Tiro al plato),
    /// el equipamiento activo en mano, tiempos, medallas y persistencia en disco.
    /// </summary>
    public class ShootingRangeManager : MonoBehaviour
    {
        public static ShootingRangeManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private ShootingRangeConfigSO config;

        [Header("Disciplines & Equipment")]
        [SerializeField] private ShootingDiscipline activeDiscipline = ShootingDiscipline.OlympicRifleDistance;
        [SerializeField] private OlympicRifle rifle;
        [SerializeField] private OlympicPistol pistol;
        [SerializeField] private OlympicShotgun shotgun;

        [Header("Target Systems")]
        [SerializeField] private GameObject distanceTargetsRoot;
        [SerializeField] private List<TargetBoard> targets = new List<TargetBoard>();
        [SerializeField] private DynamicWallTargetGallery wallGallery;
        [SerializeField] private ClayPigeonLauncher clayLauncher;

        [Header("Runtime State")]
        [SerializeField] private int currentSeriesIndex = 1;
        [SerializeField] private int shotsFiredInSeries = 0;
        [SerializeField] private int currentSeriesScore = 0;
        [SerializeField] private int bullseyesInSeries = 0;
        [SerializeField] private float timeRemaining = 90f;
        [SerializeField] private bool seriesActive = false;
        [SerializeField] private int currentTargetIndex = 0;

        private ShootingSaveData currentSaveData;

        public ShootingDiscipline ActiveDiscipline => activeDiscipline;
        public int CurrentSeriesIndex => currentSeriesIndex;
        public int ShotsFiredInSeries => shotsFiredInSeries;
        public int CurrentSeriesScore => currentSeriesScore;
        public int BullseyesInSeries => bullseyesInSeries;
        public float TimeRemaining => timeRemaining;
        public bool IsSeriesActive => seriesActive;
        public ShootingSaveData SaveData => currentSaveData;
        public ShootingRangeConfigSO Config => config;

        public event Action<ShootingDiscipline> OnDisciplineChanged;
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

            // Garantizar que únicamente la cámara VR en primera persona de XR Origin esté activa
            // (Destruye cualquier cámara estándar externa que cause vista lejana o conflicto de 2 AudioListeners)
            CleanupRogueCamerasAndListeners();

            currentSaveData = ShootingSaveSystem.Load();
        }

        private void CleanupRogueCamerasAndListeners()
        {
            var cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in cameras)
            {
                if (cam.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() == null)
                {
                    Destroy(cam.gameObject);
                }
            }

            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (var l in listeners)
            {
                if (l.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() == null)
                {
                    Destroy(l);
                }
            }
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
                if (t != null) t.OnHitScored += HandleTargetHit;
            }

            // Auto-detectar armas
            if (rifle == null) rifle = FindAnyObjectByType<OlympicRifle>();
            if (pistol == null) pistol = FindAnyObjectByType<OlympicPistol>();
            if (shotgun == null) shotgun = FindAnyObjectByType<OlympicShotgun>();

            // Auto-detectar dianas dinámicas
            if (wallGallery == null) wallGallery = FindAnyObjectByType<DynamicWallTargetGallery>();
            if (clayLauncher == null) clayLauncher = FindAnyObjectByType<ClayPigeonLauncher>();

            // Conectar eventos de dianas dinámicas
            if (wallGallery != null)
            {
                wallGallery.OnTargetHit += HandleWallTargetHit;
                wallGallery.OnRoundFinished += HandleWallRoundFinished;
                wallGallery.OnTimerTick += (sec) => OnTimerUpdated?.Invoke(sec);
            }

            if (clayLauncher != null)
            {
                clayLauncher.OnClayScored += HandleClayScored;
                clayLauncher.OnClayRoundFinished += HandleClayRoundFinished;
            }

            // Iniciar en la disciplina inicial
            SelectDiscipline(activeDiscipline);
        }

        private void OnDestroy()
        {
            foreach (var t in targets)
            {
                if (t != null) t.OnHitScored -= HandleTargetHit;
            }

            if (wallGallery != null)
            {
                wallGallery.OnTargetHit -= HandleWallTargetHit;
                wallGallery.OnRoundFinished -= HandleWallRoundFinished;
            }

            if (clayLauncher != null)
            {
                clayLauncher.OnClayScored -= HandleClayScored;
                clayLauncher.OnClayRoundFinished -= HandleClayRoundFinished;
            }
        }

        private void Update()
        {
            if (!seriesActive) return;

            // Para la disciplina de rifle a distancia manejamos el temporizador aquí
            if (activeDiscipline == ShootingDiscipline.OlympicRifleDistance)
            {
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
        }

        /// <summary>
        /// Cambia la disciplina activa de tiro (Rifle, Pistola en pared, o Tiro al plato).
        /// </summary>
        public void SelectDiscipline(ShootingDiscipline discipline)
        {
            activeDiscipline = discipline;

            // 1. Activar arma correspondiente y desactivar las otras
            if (rifle != null) rifle.gameObject.SetActive(discipline == ShootingDiscipline.OlympicRifleDistance);
            if (pistol != null) pistol.gameObject.SetActive(discipline == ShootingDiscipline.DynamicPistolWall);
            if (shotgun != null) shotgun.gameObject.SetActive(discipline == ShootingDiscipline.ClayPigeonShotgun);

            // Re-vincular arma activa a la mano del jugador
            if (discipline == ShootingDiscipline.OlympicRifleDistance && rifle != null) rifle.BindToRightHand();
            if (discipline == ShootingDiscipline.DynamicPistolWall && pistol != null) pistol.BindToRightHand();
            if (discipline == ShootingDiscipline.ClayPigeonShotgun && shotgun != null) shotgun.BindToRightHand();

            // 2. Activar/Desactivar sistemas de dianas correspondientes
            if (distanceTargetsRoot != null)
            {
                distanceTargetsRoot.SetActive(discipline == ShootingDiscipline.OlympicRifleDistance);
            }

            if (wallGallery != null)
            {
                wallGallery.gameObject.SetActive(discipline == ShootingDiscipline.DynamicPistolWall);
                if (discipline != ShootingDiscipline.DynamicPistolWall) wallGallery.StopGalleryRound();
            }

            if (clayLauncher != null)
            {
                clayLauncher.gameObject.SetActive(discipline == ShootingDiscipline.ClayPigeonShotgun);
                if (discipline != ShootingDiscipline.ClayPigeonShotgun) clayLauncher.StopClayRound();
            }

            // 3. Iniciar serie/ronda en la nueva disciplina
            StartNewDisciplineRound();

            OnDisciplineChanged?.Invoke(activeDiscipline);
        }

        public void StartNewDisciplineRound()
        {
            currentSeriesScore = 0;
            shotsFiredInSeries = 0;
            bullseyesInSeries = 0;
            seriesActive = true;

            switch (activeDiscipline)
            {
                case ShootingDiscipline.OlympicRifleDistance:
                    StartNewSeries(currentTargetIndex);
                    break;

                case ShootingDiscipline.DynamicPistolWall:
                    if (pistol != null) pistol.TryReload();
                    timeRemaining = 60f;
                    OnScoreUpdated?.Invoke(0, 0, 60);
                    OnSeriesStarted?.Invoke(currentSeriesIndex, 8f);
                    if (wallGallery != null) wallGallery.StartGalleryRound();
                    break;

                case ShootingDiscipline.ClayPigeonShotgun:
                    if (shotgun != null) shotgun.TryReload();
                    timeRemaining = 45f;
                    OnScoreUpdated?.Invoke(0, 0, 10);
                    OnSeriesStarted?.Invoke(currentSeriesIndex, 20f);
                    if (clayLauncher != null) clayLauncher.StartClayRound();
                    break;
            }
        }

        public void StartNewSeries(int targetIdx)
        {
            currentTargetIndex = Mathf.Clamp(targetIdx, 0, Mathf.Max(0, targets.Count - 1));
            shotsFiredInSeries = 0;
            currentSeriesScore = 0;
            bullseyesInSeries = 0;
            timeRemaining = config != null ? config.timeLimitSeconds : 90f;
            seriesActive = true;

            foreach (var t in targets)
            {
                if (t != null) t.ResetTarget();
            }

            if (rifle != null) rifle.TryReload();

            float dist = targets.Count > currentTargetIndex && targets[currentTargetIndex] != null
                ? targets[currentTargetIndex].DistanceMeters
                : 10f;

            int maxShots = config != null ? config.shotsPerSeries : 10;
            OnScoreUpdated?.Invoke(currentSeriesScore, shotsFiredInSeries, maxShots);
            OnSeriesStarted?.Invoke(currentSeriesIndex, dist);
        }

        private void HandleTargetHit(TargetBoard target, int score, bool isBullseye, Vector3 hitPoint)
        {
            if (!seriesActive || activeDiscipline != ShootingDiscipline.OlympicRifleDistance) return;

            shotsFiredInSeries++;
            currentSeriesScore += score;
            if (isBullseye) bullseyesInSeries++;

            if (ScoreManager.Instance != null && score > 0)
            {
                ScoreManager.Instance.AddPoints(score * 10);
            }

            int maxShots = config != null ? config.shotsPerSeries : 10;
            OnScoreUpdated?.Invoke(currentSeriesScore, shotsFiredInSeries, maxShots);
            OnShotLanded?.Invoke(score, isBullseye, hitPoint);

            if (shotsFiredInSeries >= maxShots)
            {
                CompleteSeries(timeExpired: false);
            }
        }

        private void HandleWallTargetHit(int score, bool isBullseye, Vector3 hitPos)
        {
            if (!seriesActive || activeDiscipline != ShootingDiscipline.DynamicPistolWall) return;

            shotsFiredInSeries++;
            currentSeriesScore += score;
            if (isBullseye) bullseyesInSeries++;

            if (ScoreManager.Instance != null && score > 0)
            {
                ScoreManager.Instance.AddPoints(score * 10);
            }

            OnScoreUpdated?.Invoke(currentSeriesScore, shotsFiredInSeries, 60);
            OnShotLanded?.Invoke(score, isBullseye, hitPos);
        }

        private void HandleWallRoundFinished(int finalScore, int hits, int total)
        {
            if (activeDiscipline != ShootingDiscipline.DynamicPistolWall) return;
            CompleteSeries(timeExpired: true);
        }

        private void HandleClayScored(int points, Vector3 hitPoint)
        {
            if (!seriesActive || activeDiscipline != ShootingDiscipline.ClayPigeonShotgun) return;

            shotsFiredInSeries++;
            currentSeriesScore += points;
            bullseyesInSeries++;

            if (ScoreManager.Instance != null && points > 0)
            {
                ScoreManager.Instance.AddPoints(points * 10);
            }

            OnScoreUpdated?.Invoke(currentSeriesScore, shotsFiredInSeries, 10);
            OnShotLanded?.Invoke(points, true, hitPoint);
        }

        private void HandleClayRoundFinished(int hits, int total)
        {
            if (activeDiscipline != ShootingDiscipline.ClayPigeonShotgun) return;
            CompleteSeries(timeExpired: false);
        }

        [Header("Sequence Tournament Mode")]
        [SerializeField] private bool isSequenceMode = false;
        private int sequenceTotalScore = 0;
        private Coroutine sequenceTransitionRoutine;

        public bool IsSequenceMode => isSequenceMode;

        public void StartFullOlympicSequence()
        {
            if (sequenceTransitionRoutine != null)
            {
                StopCoroutine(sequenceTransitionRoutine);
                sequenceTransitionRoutine = null;
            }
            isSequenceMode = true;
            sequenceTotalScore = 0;
            SelectDiscipline(ShootingDiscipline.DynamicPistolWall);
        }

        private IEnumerator SequenceTransitionRoutine(ShootingDiscipline nextDiscipline, float delay)
        {
            yield return new WaitForSeconds(delay);
            SelectDiscipline(nextDiscipline);
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

            switch (activeDiscipline)
            {
                case ShootingDiscipline.OlympicRifleDistance:
                    if (currentSeriesScore > currentSaveData.rifleHighScore) currentSaveData.rifleHighScore = currentSeriesScore;
                    break;
                case ShootingDiscipline.DynamicPistolWall:
                    if (currentSeriesScore > currentSaveData.pistolHighScore) currentSaveData.pistolHighScore = currentSeriesScore;
                    break;
                case ShootingDiscipline.ClayPigeonShotgun:
                    if (currentSeriesScore > currentSaveData.clayHighScore) currentSaveData.clayHighScore = currentSeriesScore;
                    break;
            }

            ShootingSaveSystem.Save(currentSaveData);

            if (isSequenceMode)
            {
                sequenceTotalScore += currentSeriesScore;

                if (activeDiscipline == ShootingDiscipline.DynamicPistolWall)
                {
                    OnSeriesFinished?.Invoke($"¡FASE 1 (PISTOLA) COMPLETADA! +{currentSeriesScore} pts\nPreparando Rifle de Precisión en 3s...", sequenceTotalScore, "🏆 CIRCUITO OLÍMPICO");
                    sequenceTransitionRoutine = StartCoroutine(SequenceTransitionRoutine(ShootingDiscipline.OlympicRifleDistance, 3.5f));
                    return;
                }
                else if (activeDiscipline == ShootingDiscipline.OlympicRifleDistance)
                {
                    OnSeriesFinished?.Invoke($"¡FASE 2 (RIFLE) COMPLETADA! +{currentSeriesScore} pts\nPreparando Tiro al Plato en 3s...", sequenceTotalScore, "🏆 CIRCUITO OLÍMPICO");
                    sequenceTransitionRoutine = StartCoroutine(SequenceTransitionRoutine(ShootingDiscipline.ClayPigeonShotgun, 3.5f));
                    return;
                }
                else if (activeDiscipline == ShootingDiscipline.ClayPigeonShotgun)
                {
                    isSequenceMode = false;
                    string circuitMedal = sequenceTotalScore >= 200 ? "¡GRAN CAMPEÓN OLÍMPICO! 🥇 ORO" : (sequenceTotalScore >= 140 ? "¡SUBCAMPEÓN OLÍMPICO! 🥈 PLATA" : "¡BRONCE OLÍMPICO! 🥉");
                    OnSeriesFinished?.Invoke($"¡CIRCUITO OLÍMPICO COMPLETADO!\nPUNTUACIÓN COMBINADA: {sequenceTotalScore} pts", sequenceTotalScore, circuitMedal);
                    return;
                }
            }

            string disciplineName = activeDiscipline == ShootingDiscipline.OlympicRifleDistance ? "Rifle de Precisión"
                : (activeDiscipline == ShootingDiscipline.DynamicPistolWall ? "Pistola Rápida" : "Tiro al Plato");

            string msg = timeExpired
                ? $"¡Tiempo Agotado en {disciplineName}! Puntuación: {currentSeriesScore} pts."
                : $"¡Ronda de {disciplineName} Completada! Puntuación: {currentSeriesScore} pts.";

            OnSeriesFinished?.Invoke(msg, currentSeriesScore, medal);
        }
    }
}
