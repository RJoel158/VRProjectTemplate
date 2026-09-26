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

        [Header("Easter Egg State")]
        [SerializeField] private bool easterEggMultiplierActive = false;
        private bool easterEggTriggeredThisSession = false;

        public bool IsEasterEggMultiplierActive => easterEggMultiplierActive;

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
                if (cam.name != "Main Camera" && cam.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() == null)
                {
                    Destroy(cam.gameObject);
                }
            }

            // Localizar o crear el AudioListener en la cámara activa del jugador VR
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                var allCams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                foreach (var c in allCams)
                {
                    if (c.name.Contains("Main") || c.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null)
                    {
                        mainCam = c;
                        break;
                    }
                }
                if (mainCam == null && allCams.Length > 0) mainCam = allCams[0];
            }

            var listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            AudioListener keeper = null;

            if (mainCam != null)
            {
                keeper = mainCam.GetComponent<AudioListener>();
                if (keeper == null) keeper = mainCam.gameObject.AddComponent<AudioListener>();
                keeper.enabled = true;
            }

            foreach (var l in listeners)
            {
                if (keeper != null && l != keeper)
                {
                    Destroy(l);
                }
            }

            // Asegurar que el sistema global de audio ShootingAudioManager esté activo
            Tiro.Audio.ShootingAudioManager.EnsureAudioSystemActive();
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
                    t.OnTargetKnockedDown += HandleTargetKnockedDown;
                }
            }

            // Auto-detectar armas
            if (rifle == null) rifle = FindAnyObjectByType<OlympicRifle>();
            if (pistol == null) pistol = FindAnyObjectByType<OlympicPistol>();
            if (shotgun == null) shotgun = FindAnyObjectByType<OlympicShotgun>();

            // Auto-detectar dianas dinámicas
            if (wallGallery == null) wallGallery = FindAnyObjectByType<DynamicWallTargetGallery>();
            if (clayLauncher == null) clayLauncher = FindAnyObjectByType<ClayPigeonLauncher>();

            // Asegurar entorno inmersivo de campo abierto (turriles, neumaticos, bosque perimetral y rapaces)
            if (FindAnyObjectByType<Tiro.Environment.ShootingOutdoorEnvironment>() == null)
            {
                GameObject envObj = new GameObject("Shooting_Outdoor_Environment");
                envObj.AddComponent<Tiro.Environment.ShootingOutdoorEnvironment>();
            }

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
                if (t != null)
                {
                    t.OnHitScored -= HandleTargetHit;
                    t.OnTargetKnockedDown -= HandleTargetKnockedDown;
                }
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

            // 1. Activar arma correspondiente y desactivar las otras limpiamente
            if (rifle != null)
            {
                bool activate = discipline == ShootingDiscipline.OlympicRifleDistance;
                rifle.gameObject.SetActive(activate);
                if (activate) rifle.BindToRightHand();
            }
            if (pistol != null)
            {
                bool activate = discipline == ShootingDiscipline.DynamicPistolWall;
                pistol.gameObject.SetActive(activate);
                if (activate) pistol.BindToRightHand();
            }
            if (shotgun != null)
            {
                bool activate = discipline == ShootingDiscipline.ClayPigeonShotgun;
                shotgun.gameObject.SetActive(activate);
                if (activate) shotgun.BindToRightHand();
            }

            // 2. Mantener las dianas del polígono siempre visibles para que el usuario pueda dispararles
            if (distanceTargetsRoot != null)
            {
                distanceTargetsRoot.SetActive(true);
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
            if (ScoreManager.Instance != null && score > 0)
            {
                ScoreManager.Instance.AddPoints(score * 10);
            }

            if (!seriesActive || activeDiscipline != ShootingDiscipline.OlympicRifleDistance) return;

            shotsFiredInSeries++;
            int pts = easterEggMultiplierActive ? score * 2 : score;
            currentSeriesScore += pts;
            if (isBullseye) bullseyesInSeries++;

            int maxShots = config != null ? config.shotsPerSeries : 10;
            OnScoreUpdated?.Invoke(currentSeriesScore, shotsFiredInSeries, maxShots);
            OnShotLanded?.Invoke(score, isBullseye, hitPoint);

            if (shotsFiredInSeries >= maxShots)
            {
                CompleteSeries(timeExpired: false);
            }
        }

        private void HandleTargetKnockedDown(TargetBoard knockedTarget)
        {
            if (targets == null || targets.Count == 0) return;

            // Encontrar la posición de la diana abatida en la lista ordenada
            int index = targets.IndexOf(knockedTarget);
            if (index < 0) return;

            // La siguiente diana en la rotación progresiva (10m -> 25m -> 50m -> 10m...)
            int nextIndex = (index + 1) % targets.Count;

            // Si la siguiente diana ya estaba abatida, o si ninguna otra diana está en pie,
            // levantar la siguiente diana progresivamente
            if (targets[nextIndex] != null && targets[nextIndex].IsKnockedDown)
            {
                StartCoroutine(RaiseTargetDelayed(targets[nextIndex], 0.7f));
            }
            else
            {
                bool anyUp = false;
                for (int i = 0; i < targets.Count; i++)
                {
                    if (targets[i] != null && !targets[i].IsKnockedDown)
                    {
                        anyUp = true;
                        break;
                    }
                }

                if (!anyUp && targets[nextIndex] != null)
                {
                    StartCoroutine(RaiseTargetDelayed(targets[nextIndex], 0.7f));
                }
            }
        }

        private IEnumerator RaiseTargetDelayed(TargetBoard target, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            if (target != null && target.IsKnockedDown)
            {
                target.ResetTarget();
            }
        }

        private void HandleWallTargetHit(int score, bool isBullseye, Vector3 hitPos)
        {
            if (!seriesActive || activeDiscipline != ShootingDiscipline.DynamicPistolWall) return;

            shotsFiredInSeries++;
            int pts = easterEggMultiplierActive ? score * 2 : score;
            currentSeriesScore += pts;
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
            int pts = easterEggMultiplierActive ? points * 2 : points;
            currentSeriesScore += pts;
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
                medal = "¡MEDALLA DE ORO OLIMPICA!";
                if (currentSaveData != null) currentSaveData.goldMedals++;
            }
            else if (currentSeriesScore >= silver)
            {
                medal = "¡MEDALLA DE PLATA OLIMPICA!";
                if (currentSaveData != null) currentSaveData.silverMedals++;
            }
            else if (currentSeriesScore >= bronze)
            {
                medal = "¡MEDALLA DE BRONCE OLIMPICA!";
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
                    OnSeriesFinished?.Invoke($"¡FASE 1 (PISTOLA) COMPLETADA! +{currentSeriesScore} pts\nPreparando Rifle de Precision en 3s...", sequenceTotalScore, "CIRCUITO OLIMPICO");
                    sequenceTransitionRoutine = StartCoroutine(SequenceTransitionRoutine(ShootingDiscipline.OlympicRifleDistance, 3.5f));
                    return;
                }
                else if (activeDiscipline == ShootingDiscipline.OlympicRifleDistance)
                {
                    OnSeriesFinished?.Invoke($"¡FASE 2 (RIFLE) COMPLETADA! +{currentSeriesScore} pts\nPreparando Tiro al Plato en 3s...", sequenceTotalScore, "CIRCUITO OLIMPICO");
                    sequenceTransitionRoutine = StartCoroutine(SequenceTransitionRoutine(ShootingDiscipline.ClayPigeonShotgun, 3.5f));
                    return;
                }
                else if (activeDiscipline == ShootingDiscipline.ClayPigeonShotgun)
                {
                    isSequenceMode = false;
                    string circuitMedal = sequenceTotalScore >= 200 ? "¡GRAN CAMPEON OLIMPICO! ORO" : (sequenceTotalScore >= 140 ? "¡SUBCAMPEON OLIMPICO! PLATA" : "¡BRONCE OLIMPICO!");
                    OnSeriesFinished?.Invoke($"¡CIRCUITO OLIMPICO COMPLETADO!\nPUNTUACION COMBINADA: {sequenceTotalScore} pts", sequenceTotalScore, circuitMedal);
                    return;
                }
            }

            string disciplineName = activeDiscipline == ShootingDiscipline.OlympicRifleDistance ? "Rifle de Precision"
                : (activeDiscipline == ShootingDiscipline.DynamicPistolWall ? "Pistola Rapida" : "Tiro al Plato");

            string msg = timeExpired
                ? $"¡Tiempo Agotado en {disciplineName}! Puntuacion: {currentSeriesScore} pts."
                : $"¡Ronda de {disciplineName} Completada! Puntuacion: {currentSeriesScore} pts.";

            OnSeriesFinished?.Invoke(msg, currentSeriesScore, medal);
        }

        /// <summary>
        /// Easter Egg: Disparo acertado a las rapaces en el cielo con el Rifle de Precision.
        /// Duplica los puntos acumulados y activa multiplicador x2 para el resto de la serie.
        /// Se activa una sola vez por sesion.
        /// </summary>
        public bool TriggerEasterEggBonus()
        {
            if (easterEggTriggeredThisSession) return false;
            easterEggTriggeredThisSession = true;
            easterEggMultiplierActive = true;

            int bonus = Mathf.Max(currentSeriesScore, 50);
            currentSeriesScore += bonus;

            int maxShots = (activeDiscipline == ShootingDiscipline.DynamicPistolWall) ? 60 : ((activeDiscipline == ShootingDiscipline.ClayPigeonShotgun) ? 10 : (config != null ? config.shotsPerSeries : 10));
            OnScoreUpdated?.Invoke(currentSeriesScore, shotsFiredInSeries, maxShots);

            Debug.Log($"[ShootingRangeManager] EASTER EGG ACTIVADO: Puntaje duplicado (+{bonus} pts) y multiplicador x2 activo.");
            return true;
        }
    }
}
