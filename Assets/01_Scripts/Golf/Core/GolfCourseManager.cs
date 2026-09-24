using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Golf.Data;
using Golf.Gameplay;
using Golf.Persistence;
using Golf.Audio;
using Golf.UI;

namespace Golf.Core
{
    [Serializable]
    public class HoleRuntimeInstance
    {
        public HoleDataSO holeData;
        public Transform teePoint;
        public GolfCup cup;
        public Transform playerSpawnPoint;
    }

    /// <summary>
    /// Gestor central del circuito de Minigolf VR.
    /// Administra el flujo de hoyos, conteo de golpes (strokes), teletransporte del jugador al lado de la bola,
    /// evaluacion de puntuacion (Hole-in-One, Birdie, Par), persistencia con GolfSaveSystem y feedback UI/Audio.
    /// </summary>
    public class GolfCourseManager : MonoBehaviour
    {
        public static GolfCourseManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GolfCourseConfigSO courseConfig;

        [Header("Scene References")]
        [SerializeField] private GolfBall ball;
        [SerializeField] private GolfPutter putter;
        [SerializeField] private Transform xrOrigin;
        [SerializeField] private GolfScoreboardUI scoreboardUI;

        [Header("Holes in Scene")]
        [SerializeField] private List<HoleRuntimeInstance> holeInstances = new List<HoleRuntimeInstance>();

        private int currentHoleIndex = 0;
        private int strokesCurrentHole = 0;
        private int totalCourseStrokes = 0;
        private bool isHoleCompleted = false;
        private bool isCourseCompleted = false;

        private GolfSaveData saveData;

        public int CurrentHoleIndex => currentHoleIndex;
        public int StrokesCurrentHole => strokesCurrentHole;
        public int TotalCourseStrokes => totalCourseStrokes;
        public bool IsCourseCompleted => isCourseCompleted;
        public GolfSaveData SaveData => saveData;

        public event Action<int, int> OnStrokesUpdated; // (currentHoleStrokes, totalStrokes)
        public event Action<int, string, int> OnHoleFinished; // (holeIndex, scoreTerm, strokes)
        public event Action<int, int> OnCourseFinished; // (totalStrokes, parDifference)

        private void Awake()
        {
            if (Instance == null) Instance = this;
            saveData = GolfSaveSystem.Load();
        }

        private void Start()
        {
            if (ball == null) ball = FindAnyObjectByType<GolfBall>();
            if (putter == null) putter = FindAnyObjectByType<GolfPutter>();

            if (xrOrigin == null)
            {
                var originObj = GameObject.Find("XR Origin (XR Rig)");
                if (originObj == null) originObj = GameObject.Find("XR Origin Hands (XR Rig)");
                if (originObj != null) xrOrigin = originObj.transform;
            }

            if (scoreboardUI == null) scoreboardUI = FindAnyObjectByType<GolfScoreboardUI>();

            ConnectEvents();
            StartCourse();
        }

        private void OnDestroy()
        {
            DisconnectEvents();
        }

        private void ConnectEvents()
        {
            if (ball != null)
            {
                ball.OnBallHit += HandleBallHit;
                ball.OnBallStopped += HandleBallStopped;
                ball.OnBallOutOfBounds += HandleBallOutOfBounds;
            }

            foreach (var h in holeInstances)
            {
                if (h.cup != null)
                {
                    h.cup.OnBallSink += HandleBallSink;
                }
            }
        }

        private void DisconnectEvents()
        {
            if (ball != null)
            {
                ball.OnBallHit -= HandleBallHit;
                ball.OnBallStopped -= HandleBallStopped;
                ball.OnBallOutOfBounds -= HandleBallOutOfBounds;
            }

            foreach (var h in holeInstances)
            {
                if (h.cup != null)
                {
                    h.cup.OnBallSink -= HandleBallSink;
                }
            }
        }

        public void StartCourse()
        {
            currentHoleIndex = 0;
            totalCourseStrokes = 0;
            isCourseCompleted = false;

            if (saveData != null)
            {
                saveData.gamesPlayed++;
                GolfSaveSystem.Save(saveData);
            }

            SetupHole(currentHoleIndex);
        }

        private void SetupHole(int index)
        {
            if (holeInstances == null || index < 0 || index >= holeInstances.Count) return;

            currentHoleIndex = index;
            strokesCurrentHole = 0;
            isHoleCompleted = false;

            var currentHole = holeInstances[currentHoleIndex];

            // 1. Posicionar la bola en el Tee
            Vector3 teePos = currentHole.teePoint != null ? currentHole.teePoint.position + Vector3.up * 0.03f : transform.position;
            Quaternion teeRot = currentHole.teePoint != null ? currentHole.teePoint.rotation : Quaternion.identity;

            if (ball != null)
            {
                ball.ResetToPosition(teePos, teeRot);
            }

            // 2. Teletransportar al jugador comodamente junto al Tee
            TeleportPlayerNearPosition(teePos, currentHole.cup != null ? currentHole.cup.CupPosition : teePos + Vector3.forward * 4f);

            // 3. Notificar UI y eventos
            OnStrokesUpdated?.Invoke(strokesCurrentHole, totalCourseStrokes);
            if (scoreboardUI != null)
            {
                scoreboardUI.UpdateHoleInfo(currentHole.holeData, strokesCurrentHole, totalCourseStrokes, saveData);
            }
        }

        private void HandleBallHit(GolfBall b, Vector3 hitVelocity)
        {
            if (isHoleCompleted || isCourseCompleted) return;

            strokesCurrentHole++;
            totalCourseStrokes++;

            if (saveData != null)
            {
                saveData.totalStrokesAllTime++;
            }

            OnStrokesUpdated?.Invoke(strokesCurrentHole, totalCourseStrokes);

            var currentHole = holeInstances[currentHoleIndex];
            if (scoreboardUI != null && currentHole != null)
            {
                scoreboardUI.UpdateHoleInfo(currentHole.holeData, strokesCurrentHole, totalCourseStrokes, saveData);
            }
        }

        private void HandleBallStopped(GolfBall b, Vector3 restPosition)
        {
            if (isHoleCompleted || isCourseCompleted) return;

            var currentHole = holeInstances[currentHoleIndex];
            Vector3 targetCupPos = currentHole != null && currentHole.cup != null ? currentHole.cup.CupPosition : restPosition + Vector3.forward * 3f;

            // Teletransporte suave al lado de la bola para el siguiente tiro
            TeleportPlayerNearPosition(restPosition, targetCupPos);
        }

        private void HandleBallOutOfBounds(GolfBall b)
        {
            if (isHoleCompleted || isCourseCompleted) return;

            var currentHole = holeInstances[currentHoleIndex];
            int penalty = currentHole != null && currentHole.holeData != null ? currentHole.holeData.OutOfBoundsPenalty : 1;

            strokesCurrentHole += penalty;
            totalCourseStrokes += penalty;

            OnStrokesUpdated?.Invoke(strokesCurrentHole, totalCourseStrokes);

            if (scoreboardUI != null)
            {
                scoreboardUI.ShowOutOfBoundsBanner(penalty);
                scoreboardUI.UpdateHoleInfo(currentHole.holeData, strokesCurrentHole, totalCourseStrokes, saveData);
            }
        }

        private void HandleBallSink(GolfCup cup, GolfBall b)
        {
            if (isHoleCompleted) return;
            isHoleCompleted = true;

            var currentHole = holeInstances[currentHoleIndex];
            int par = currentHole != null && currentHole.holeData != null ? currentHole.holeData.Par : 2;

            // Calcular calificacion del hoyo segun strokes
            string scoreTerm = EvaluateScoreTerm(strokesCurrentHole, par);

            // Actualizar persistencia
            if (saveData != null)
            {
                saveData.RecordHoleScore(currentHoleIndex + 1, strokesCurrentHole);

                if (strokesCurrentHole == 1) saveData.totalHolesInOne++;
                else if (strokesCurrentHole == par - 2) saveData.totalEagles++;
                else if (strokesCurrentHole == par - 1) saveData.totalBirdies++;
                else if (strokesCurrentHole == par) saveData.totalPars++;

                GolfSaveSystem.Save(saveData);
            }

            // Audio triunfal
            GolfAudioManager.PlayVictoryFanfare();

            OnHoleFinished?.Invoke(currentHoleIndex, scoreTerm, strokesCurrentHole);

            if (scoreboardUI != null)
            {
                scoreboardUI.ShowHoleCompletedBanner(scoreTerm, strokesCurrentHole, par);
            }

            StartCoroutine(AdvanceHoleRoutine());
        }

        private string EvaluateScoreTerm(int strokes, int par)
        {
            if (strokes == 1) return "HOLE IN ONE!";
            int diff = strokes - par;
            return diff switch
            {
                <= -2 => "EAGLE!",
                -1 => "BIRDIE!",
                0 => "PAR",
                1 => "BOGEY",
                _ => "DOUBLE BOGEY"
            };
        }

        private IEnumerator AdvanceHoleRoutine()
        {
            yield return new WaitForSeconds(3f);

            if (currentHoleIndex < holeInstances.Count - 1)
            {
                SetupHole(currentHoleIndex + 1);
            }
            else
            {
                CompleteCourse();
            }
        }

        private void CompleteCourse()
        {
            isCourseCompleted = true;

            int totalPar = courseConfig != null ? courseConfig.TotalPar : 7;
            int diff = totalCourseStrokes - totalPar;

            if (saveData != null)
            {
                saveData.coursesCompleted++;
                if (totalCourseStrokes < saveData.bestTotalScore)
                {
                    saveData.bestTotalScore = totalCourseStrokes;
                }
                GolfSaveSystem.Save(saveData);
            }

            OnCourseFinished?.Invoke(totalCourseStrokes, diff);

            if (scoreboardUI != null)
            {
                scoreboardUI.ShowCourseFinished(totalCourseStrokes, totalPar, saveData);
            }
        }

        /// <summary>
        /// Posiciona al jugador de forma natural a la izquierda de la bola (para diestros), mirando hacia el objetivo.
        /// </summary>
        public void TeleportPlayerNearPosition(Vector3 ballPos, Vector3 targetCupPos)
        {
            if (xrOrigin == null) return;

            Vector3 toTarget = (targetCupPos - ballPos);
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f) toTarget = Vector3.forward;
            toTarget.Normalize();

            // Vector perpendicular (a la izquierda de la trayectoria de tiro)
            Vector3 leftDir = Vector3.Cross(Vector3.up, toTarget).normalized;
            Vector3 standPos = ballPos + leftDir * 0.42f;

            // Mantener la altura del suelo
            standPos.y = ballPos.y;

            // Rotar el rig para que mire comodamente hacia la bola y el objetivo
            Quaternion standRot = Quaternion.LookRotation(toTarget, Vector3.up);

            xrOrigin.position = standPos;
            xrOrigin.rotation = standRot;
        }

        public void AddHoleInstance(HoleRuntimeInstance instance)
        {
            holeInstances.Add(instance);
        }
    }
}
