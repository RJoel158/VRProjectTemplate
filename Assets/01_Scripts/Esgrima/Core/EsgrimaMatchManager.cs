using System;
using System.Collections;
using UnityEngine;
using Esgrima.Data;
using Esgrima.Combat;
using Esgrima.Persistence;

namespace Esgrima.Core
{
    public class EsgrimaMatchManager : MonoBehaviour
    {
        public static EsgrimaMatchManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private EsgrimaMatchConfigSO matchConfig;

        [Header("Arena & Boundary")]
        [SerializeField] private RingBoundaryDetector boundaryDetector;
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform aiSpawnPoint;

        [Header("Fighter References")]
        [SerializeField] private Transform playerRoot;
        [SerializeField] private KnockbackController playerKnockback;
        [SerializeField] private FencerAI rivalAI;
        [SerializeField] private KnockbackController aiKnockback;

        [Header("Runtime State")]
        [SerializeField] private int playerScore = 0;
        [SerializeField] private int aiScore = 0;
        [SerializeField] private int currentRound = 1;
        [SerializeField] private bool matchOver = false;

        private EsgrimaSaveData currentSaveData;

        public int PlayerScore => playerScore;
        public int AIScore => aiScore;
        public int CurrentRound => currentRound;
        public bool IsMatchOver => matchOver;
        public EsgrimaSaveData SaveData => currentSaveData;

        public event Action<int, int> OnScoreChanged;
        public event Action<int> OnRoundStarted;
        public event Action<string> OnRoundEnded; // Mensaje de quién ganó el punto
        public event Action<string> OnCombatBanner; // Mensajes temporales de combate (bloqueo/parry)
        public event Action<bool> OnMatchEnded;   // true = Ganó Jugador, false = Ganó IA

        public void AnnounceCombatBanner(string message)
        {
            OnCombatBanner?.Invoke(message);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Cargar datos persistentes
            currentSaveData = EsgrimaSaveSystem.Load();
        }

        private void Start()
        {
            if (boundaryDetector != null)
            {
                boundaryDetector.OnFighterFellOutOfBounds += HandleFighterFell;
            }

            StartNewRound();
        }

        private void OnDestroy()
        {
            if (boundaryDetector != null)
            {
                boundaryDetector.OnFighterFellOutOfBounds -= HandleFighterFell;
            }
        }

        public void StartNewRound()
        {
            if (matchOver) return;

            // Resetear posiciones y retrocesos
            if (playerKnockback != null) playerKnockback.ResetKnockback();
            if (aiKnockback != null) aiKnockback.ResetKnockback();

            if (playerRoot != null && playerSpawnPoint != null)
            {
                playerRoot.position = playerSpawnPoint.position;
                playerRoot.rotation = playerSpawnPoint.rotation;
            }

            if (rivalAI != null)
            {
                rivalAI.ResetFencer();
                if (aiSpawnPoint != null)
                {
                    rivalAI.transform.position = aiSpawnPoint.position;
                    rivalAI.transform.rotation = aiSpawnPoint.rotation;
                }
            }

            if (boundaryDetector != null)
            {
                boundaryDetector.SetRoundActive(true);
            }

            OnRoundStarted?.Invoke(currentRound);
            OnScoreChanged?.Invoke(playerScore, aiScore);
        }

        private void HandleFighterFell(bool isPlayer)
        {
            if (matchOver) return;

            string message;
            if (isPlayer)
            {
                aiScore++;
                message = "¡Caíste fuera del Ring! Punto para el Rival";
            }
            else
            {
                playerScore++;
                message = "¡Caída del Rival! ¡Punto para ti!";

                // Registrar en ScoreManager global si existe
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddPoints(100);
                }
            }

            OnScoreChanged?.Invoke(playerScore, aiScore);
            OnRoundEnded?.Invoke(message);

            int targetScore = matchConfig != null ? matchConfig.roundsToWin : 3;

            if (playerScore >= targetScore || aiScore >= targetScore)
            {
                EndMatch(playerScore >= targetScore);
            }
            else
            {
                currentRound++;
                float delay = matchConfig != null ? matchConfig.roundResetDelay : 2.0f;
                StartCoroutine(RestartRoundAfterDelay(delay));
            }
        }

        private IEnumerator RestartRoundAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartNewRound();
        }

        private void EndMatch(bool playerWon)
        {
            matchOver = true;
            if (boundaryDetector != null) boundaryDetector.SetRoundActive(false);
            if (rivalAI != null) rivalAI.StopAI();

            // Actualizar datos de guardado persistente
            if (currentSaveData == null) currentSaveData = new EsgrimaSaveData();

            if (playerWon)
            {
                currentSaveData.victories++;
                currentSaveData.currentWinStreak++;
                if (currentSaveData.currentWinStreak > currentSaveData.highestWinStreak)
                {
                    currentSaveData.highestWinStreak = currentSaveData.currentWinStreak;
                }
            }
            else
            {
                currentSaveData.defeats++;
                currentSaveData.currentWinStreak = 0;
            }

            EsgrimaSaveSystem.Save(currentSaveData);

            OnMatchEnded?.Invoke(playerWon);
        }

        /// <summary>
        /// Registra un impacto asestado en combate (jugador o rival).
        /// </summary>
        public void RegisterFighterHit(bool playerWasHit, float force, bool wasParryCounter = false)
        {
            if (matchOver) return;

            if (playerWasHit)
            {
                aiScore++;
                OnScoreChanged?.Invoke(playerScore, aiScore);
                OnRoundEnded?.Invoke("¡Te han golpeado! Punto para el Rival");

                int targetScore = matchConfig != null ? matchConfig.roundsToWin : 3;
                if (aiScore >= targetScore)
                {
                    EndMatch(false);
                }
            }
            else
            {
                playerScore++;
                if (currentSaveData != null) currentSaveData.totalHitsLanded++;

                int points = wasParryCounter ? 150 : 50;
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddPoints(points);
                }

                OnScoreChanged?.Invoke(playerScore, aiScore);
                string msg = wasParryCounter ? "¡PARRY Y CONTRAATAQUE CRÍTICO!" : "¡Estocada válida! ¡Punto para ti!";
                OnRoundEnded?.Invoke(msg);

                int targetScore = matchConfig != null ? matchConfig.roundsToWin : 3;
                if (playerScore >= targetScore)
                {
                    EndMatch(true);
                }
            }
        }

        /// <summary>
        /// Reinicia por completo el duelo (Match) a 0-0.
        /// </summary>
        public void RestartFullMatch()
        {
            playerScore = 0;
            aiScore = 0;
            currentRound = 1;
            matchOver = false;
            StartNewRound();
        }
    }
}
