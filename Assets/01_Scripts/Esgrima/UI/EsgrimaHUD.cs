using UnityEngine;
using TMPro;
using Esgrima.Core;
using Esgrima.Persistence;

namespace Esgrima.UI
{
    /// <summary>
    /// HUD en World Space que muestra el marcador del duelo de esgrima,
    /// feedback de punto/caída y las estadísticas persistentes guardadas en disco.
    /// </summary>
    public class EsgrimaHUD : MonoBehaviour
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI roundText;
        [SerializeField] private TextMeshProUGUI statusBannerText;

        [Header("Persistent Stats Display")]
        [SerializeField] private TextMeshProUGUI persistentStatsText;

        [Header("UI Panels")]
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        private void Start()
        {
            if (EsgrimaMatchManager.Instance != null)
            {
                EsgrimaMatchManager.Instance.OnScoreChanged += UpdateScoreDisplay;
                EsgrimaMatchManager.Instance.OnRoundStarted += HandleRoundStarted;
                EsgrimaMatchManager.Instance.OnRoundEnded += HandleRoundEnded;
                EsgrimaMatchManager.Instance.OnCombatBanner += HandleRoundEnded;
                EsgrimaMatchManager.Instance.OnMatchEnded += HandleMatchEnded;
            }

            RefreshPersistentStats();
        }

        private void OnDestroy()
        {
            if (EsgrimaMatchManager.Instance != null)
            {
                EsgrimaMatchManager.Instance.OnScoreChanged -= UpdateScoreDisplay;
                EsgrimaMatchManager.Instance.OnRoundStarted -= HandleRoundStarted;
                EsgrimaMatchManager.Instance.OnRoundEnded -= HandleRoundEnded;
                EsgrimaMatchManager.Instance.OnCombatBanner -= HandleRoundEnded;
                EsgrimaMatchManager.Instance.OnMatchEnded -= HandleMatchEnded;
            }
        }

        public void RefreshPersistentStats()
        {
            EsgrimaSaveData data = EsgrimaSaveSystem.Load();
            if (persistentStatsText != null)
            {
                persistentStatsText.text = $"Récord: {data.victories} Victorias | {data.defeats} Derrotas\nMejor Racha: {data.highestWinStreak} seguidas";
            }
        }

        private void UpdateScoreDisplay(int playerScore, int aiScore)
        {
            if (scoreText != null)
            {
                scoreText.text = $"JUGADOR  <color=#38bdf8>{playerScore}</color>  -  <color=#f87171>{aiScore}</color>  RIVAL";
            }
        }

        private void HandleRoundStarted(int roundNumber)
        {
            if (roundText != null)
            {
                roundText.text = $"RONDA {roundNumber}";
            }

            if (statusBannerText != null)
            {
                statusBannerText.text = "¡EN GUARDIA!";
            }

            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
        }

        private void HandleRoundEnded(string message)
        {
            if (statusBannerText != null)
            {
                statusBannerText.text = message;
            }
        }

        private void HandleMatchEnded(bool playerWon)
        {
            if (playerWon)
            {
                if (statusBannerText != null) statusBannerText.text = "¡VICTORIA TOTAL!";
                if (victoryPanel != null) victoryPanel.SetActive(true);
            }
            else
            {
                if (statusBannerText != null) statusBannerText.text = "¡DERROTA!";
                if (defeatPanel != null) defeatPanel.SetActive(true);
            }

            RefreshPersistentStats();
        }

        /// <summary>
        /// Puede ser vinculado a un botón de UI de Unity.
        /// </summary>
        public void OnClickRestart()
        {
            if (EsgrimaMatchManager.Instance != null)
            {
                EsgrimaMatchManager.Instance.RestartFullMatch();
            }
        }
    }
}
