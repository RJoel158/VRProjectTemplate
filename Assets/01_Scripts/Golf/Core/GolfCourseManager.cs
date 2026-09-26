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

        [Header("Live Session Progress (ScriptableObject)")]
        [Tooltip("ScriptableObject inspeccionable en tiempo real con datos de golpes, hoyo actual y última posición de tiro.")]
        [SerializeField] private GolfProgressSO sessionProgress;

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
        public GolfProgressSO SessionProgress => sessionProgress;

        public event Action<int, int> OnStrokesUpdated; // (currentHoleStrokes, totalStrokes)
        public event Action<int, string, int> OnHoleFinished; // (holeIndex, scoreTerm, strokes)
        public event Action<int, int> OnCourseFinished; // (totalStrokes, parDifference)

        private void Awake()
        {
            if (Instance == null) Instance = this;
            saveData = GolfSaveSystem.Load();

#if UNITY_EDITOR
            if (sessionProgress == null)
            {
                sessionProgress = UnityEditor.AssetDatabase.LoadAssetAtPath<GolfProgressSO>("Assets/01_Scripts/Golf/Data/Configs/GolfProgressData.asset");
            }
#endif
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

            EnsureLagoonEnvironment();
            EnsureHoleBoundaries();
            ConnectEvents();
            StartCourse();
        }

        private void EnsureLagoonEnvironment()
        {
            // Limpieza inmediata de cualquier suelo/bloque gris solido residual
            string[] obsoleteNames = { "Resort_Patio_Floor", "Hole2_Bridge_Catwalk", "Hole2_Catwalk_Ramp" };
            foreach (string objName in obsoleteNames)
            {
                var obj = GameObject.Find(objName);
                if (obj != null) Destroy(obj);
            }

            // Asegurar que el gestor de atmosfera (agua turquesa, gaviotas, islotes y boyas) este activo
            if (FindAnyObjectByType<Golf.Environment.GolfLagoonEnvironment>() == null)
            {
                GameObject envObj = new GameObject("Golf_Lagoon_Environment");
                envObj.AddComponent<Golf.Environment.GolfLagoonEnvironment>();
            }
        }

        private void EnsureHoleBoundaries()
        {
            // Obtener el material de caoba genuino y material fisico desde los bumpers existentes
            Material woodMat = null;
            PhysicsMaterial woodPhysMat = null;
            var sampleBumper = GameObject.FindWithTag("WoodBumper");
            if (sampleBumper != null)
            {
                var r = sampleBumper.GetComponent<Renderer>();
                if (r != null) woodMat = r.sharedMaterial;

                var c = sampleBumper.GetComponent<Collider>();
                if (c != null) woodPhysMat = c.sharedMaterial;
            }

            // 1. Hoyo 1 - Cabecera trasera del Tee y cierre frontal detrás del hoyo
            EnsureBumper("Hole1_BackBumper", new Vector3(0f, 0.11f, -0.3f), new Vector3(1.36f, 0.22f, 0.10f), woodMat, woodPhysMat);
            EnsureBumper("Hole1_FrontBumper", new Vector3(3.6f, 0.11f, 4.9f), new Vector3(1.36f, 0.22f, 0.10f), woodMat, woodPhysMat);

            // 2. Hoyo 2 - Laterales de rampa de subida y rampa de bajada, más cabeceras
            Transform hole2 = GameObject.Find("Hole_02_RampaPuente")?.transform;
            Vector3 h2Base = hole2 != null ? hole2.position : new Vector3(7.5f, 0f, 0f);
            EnsureBumper("Hole2_Tee_BackBumper", h2Base + new Vector3(0f, 0.11f, -0.15f), new Vector3(1.36f, 0.22f, 0.10f), woodMat, woodPhysMat);
            EnsureBumper("Hole2_RampUp_L", h2Base + new Vector3(-0.64f, 0.33f, 4.1f), new Vector3(0.08f, 0.24f, 2.2f), woodMat, woodPhysMat, Quaternion.Euler(-11f, 0f, 0f));
            EnsureBumper("Hole2_RampUp_R", h2Base + new Vector3(0.64f, 0.33f, 4.1f), new Vector3(0.08f, 0.24f, 2.2f), woodMat, woodPhysMat, Quaternion.Euler(-11f, 0f, 0f));
            EnsureBumper("Hole2_RampDown_L", h2Base + new Vector3(-0.64f, 0.33f, 8.8f), new Vector3(0.08f, 0.24f, 2.2f), woodMat, woodPhysMat, Quaternion.Euler(11f, 0f, 0f));
            EnsureBumper("Hole2_RampDown_R", h2Base + new Vector3(0.64f, 0.33f, 8.8f), new Vector3(0.08f, 0.24f, 2.2f), woodMat, woodPhysMat, Quaternion.Euler(11f, 0f, 0f));
            EnsureBumper("Hole2_Cup_FrontBumper", h2Base + new Vector3(0f, 0.11f, 12.65f), new Vector3(1.96f, 0.22f, 0.10f), woodMat, woodPhysMat);

            // 3. Hoyo 3 - Cabeceras de salida y cierre
            Transform hole3 = GameObject.Find("Hole_03_MolinoPasaje")?.transform;
            Vector3 h3Base = hole3 != null ? hole3.position : new Vector3(15f, 0f, 0f);
            EnsureBumper("Hole3_BackBumper", h3Base + new Vector3(0f, 0.11f, -0.3f), new Vector3(1.76f, 0.22f, 0.10f), woodMat, woodPhysMat);
            EnsureBumper("Hole3_FrontBumper", h3Base + new Vector3(0f, 0.11f, 11.3f), new Vector3(1.76f, 0.22f, 0.10f), woodMat, woodPhysMat);
        }

        private void EnsureBumper(string name, Vector3 pos, Vector3 scale, Material mat, PhysicsMaterial physMat, Quaternion rot = default)
        {
            if (GameObject.Find(name) != null) return;

            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name;
            b.tag = "WoodBumper";
            b.transform.position = pos;
            b.transform.localScale = scale;
            if (rot != default) b.transform.rotation = rot;
            if (mat != null) b.GetComponent<Renderer>().sharedMaterial = mat;
            if (physMat != null)
            {
                var col = b.GetComponent<Collider>();
                if (col != null) col.sharedMaterial = physMat;
            }
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

            var firstHole = (holeInstances != null && holeInstances.Count > 0) ? holeInstances[0] : null;
            sessionProgress?.ResetSession(0, firstHole != null && firstHole.holeData != null ? firstHole.holeData.HoleName : "Hoyo 1", firstHole != null && firstHole.holeData != null ? firstHole.holeData.Par : 2);

            SetupHole(currentHoleIndex);
        }

        private void SetupHole(int index)
        {
            if (holeInstances == null || index < 0 || index >= holeInstances.Count) return;

            currentHoleIndex = index;
            strokesCurrentHole = 0;
            isHoleCompleted = false;

            var currentHole = holeInstances[currentHoleIndex];
            sessionProgress?.StartHole(index, currentHole.holeData != null ? currentHole.holeData.HoleName : $"Hoyo {index + 1}", currentHole.holeData != null ? currentHole.holeData.Par : 2);

            // 1. Posicionar la bola en el Tee
            Vector3 teePos = currentHole.teePoint != null ? currentHole.teePoint.position + Vector3.up * 0.03f : transform.position;
            Quaternion teeRot = currentHole.teePoint != null ? currentHole.teePoint.rotation : Quaternion.identity;

            if (ball != null)
            {
                ball.ResetToPosition(teePos, teeRot);
            }

            // 2. Teletransportar al jugador comodamente junto al Tee
            Vector3 targetCup = currentHole.cup != null ? currentHole.cup.CupPosition : teePos + Vector3.forward * 4f;
            TeleportPlayerNearPosition(teePos, targetCup);

            // 3. Notificar UI y eventos, orientando el marcador hacia el jugador
            OnStrokesUpdated?.Invoke(strokesCurrentHole, totalCourseStrokes);
            if (scoreboardUI != null)
            {
                scoreboardUI.PositionNearHole(teePos, (targetCup - teePos).normalized);
                scoreboardUI.UpdateHoleInfo(currentHole.holeData, strokesCurrentHole, totalCourseStrokes, saveData);
            }
        }

        private void HandleBallHit(GolfBall b, Vector3 hitVelocity)
        {
            if (isHoleCompleted || isCourseCompleted) return;

            strokesCurrentHole++;
            totalCourseStrokes++;

            sessionProgress?.RecordStroke(b.transform.position, hitVelocity);

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

            sessionProgress?.RecordPenalty(penalty);

            OnStrokesUpdated?.Invoke(strokesCurrentHole, totalCourseStrokes);

            if (scoreboardUI != null)
            {
                scoreboardUI.ShowOutOfBoundsBanner(penalty);
                scoreboardUI.UpdateHoleInfo(currentHole.holeData, strokesCurrentHole, totalCourseStrokes, saveData);
            }

            // Reiniciar la pelota al principio del hoyo (Tee de salida) con penalización
            Vector3 teePos = currentHole.teePoint != null ? currentHole.teePoint.position + Vector3.up * 0.03f : transform.position;
            Quaternion teeRot = currentHole.teePoint != null ? currentHole.teePoint.rotation : Quaternion.identity;
            if (b != null)
            {
                b.ResetToPosition(teePos, teeRot);
            }

            // Reposicionar al jugador junto al Tee para recomenzar el hoyo
            TeleportPlayerNearPosition(teePos, currentHole.cup != null ? currentHole.cup.CupPosition : teePos + Vector3.forward * 4f);
        }

        private void HandleBallSink(GolfCup cup, GolfBall b)
        {
            if (isHoleCompleted) return;
            isHoleCompleted = true;

            var currentHole = holeInstances[currentHoleIndex];
            int par = currentHole != null && currentHole.holeData != null ? currentHole.holeData.Par : 2;

            sessionProgress?.CompleteHole(strokesCurrentHole);

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

            sessionProgress?.CompleteCourse(saveData != null ? saveData.bestTotalScore : totalCourseStrokes);

            OnCourseFinished?.Invoke(totalCourseStrokes, diff);

            if (scoreboardUI != null)
            {
                scoreboardUI.ShowCourseFinished(totalCourseStrokes, totalPar, saveData);
            }
        }

        /// <summary>
        /// Posiciona al jugador de forma natural a la izquierda de la bola (para diestros), mirando hacia el objetivo.
        /// Detecta la altura exacta del piso bajo los pies mediante Raycast y compensa el offset de la camara XR.
        /// </summary>
        public void TeleportPlayerNearPosition(Vector3 ballPos, Vector3 targetCupPos)
        {
            if (xrOrigin == null) return;

            Vector3 toTarget = (targetCupPos - ballPos);
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f) toTarget = Vector3.forward;
            toTarget.Normalize();

            // Vector perpendicular a la izquierda de la trayectoria (para golfista diestro)
            // En Unity (left-handed): Cross(toTarget, Vector3.up) da la izquierda (-X cuando toTarget es +Z)
            Vector3 leftDir = Vector3.Cross(toTarget, Vector3.up).normalized;

            // Posicionar al jugador 45 cm a la izquierda de la bola y 8 cm retrasado respecto al tiro
            Vector3 standFeetPos = ballPos + leftDir * 0.45f - toTarget * 0.08f;

            // Determinar la altura exacta del suelo bajo los pies del jugador
            float floorY = 0f;
            if (Physics.Raycast(standFeetPos + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 6f, ~0, QueryTriggerInteraction.Ignore))
            {
                floorY = hit.point.y;
            }
            else
            {
                floorY = ballPos.y - 0.025f; // Nivel del cesped bajo la bola
            }
            standFeetPos.y = floorY;

            Quaternion standRot = Quaternion.LookRotation(toTarget, Vector3.up);

            // Si el objeto tiene componente XROrigin, compensar el offset horizontal de la camara
            var xrOriginComp = xrOrigin.GetComponent<Unity.XR.CoreUtils.XROrigin>();
            if (xrOriginComp != null && xrOriginComp.Camera != null)
            {
                Vector3 cameraWorld = xrOriginComp.Camera.transform.position;
                Vector3 cameraOffsetFromOrigin = cameraWorld - xrOrigin.position;
                cameraOffsetFromOrigin.y = 0f;

                Vector3 targetOriginPos = standFeetPos - cameraOffsetFromOrigin;
                targetOriginPos.y = floorY;

                xrOrigin.position = targetOriginPos;
                xrOrigin.rotation = standRot;
            }
            else
            {
                xrOrigin.position = standFeetPos;
                xrOrigin.rotation = standRot;
            }

            Debug.Log($"[GolfCourseManager] Jugador teletransportado a {standFeetPos} (Piso Y: {floorY:F2}) mirando hacia {toTarget}");
        }

        public void AddHoleInstance(HoleRuntimeInstance instance)
        {
            holeInstances.Add(instance);
        }
    }
}
