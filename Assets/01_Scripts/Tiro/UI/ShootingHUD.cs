using UnityEngine;
using TMPro;
using Tiro.Core;
using Tiro.Weapons;

namespace Tiro.UI
{
    /// <summary>
    /// HUD de alta visibilidad para el puesto de tiro olímpico en VR.
    /// Muestra puntuación acumulada, tiros restantes, cronómetro, distancia del blanco y medallas.
    /// </summary>
    public class ShootingHUD : MonoBehaviour
    {
        [Header("Text Displays")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI shotsText;
        [SerializeField] private TextMeshProUGUI distanceText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI feedbackBannerText;
        [SerializeField] private TextMeshProUGUI recordText;

        [Header("Weapon & Manager References")]
        [SerializeField] private ShootingRangeManager rangeManager;
        [SerializeField] private OlympicPistol pistol;

        private void Start()
        {
            if (rangeManager == null) rangeManager = ShootingRangeManager.Instance;
            if (pistol == null) pistol = FindAnyObjectByType<OlympicPistol>();

            if (rangeManager != null)
            {
                rangeManager.OnScoreUpdated += HandleScoreUpdated;
                rangeManager.OnShotLanded += HandleShotLanded;
                rangeManager.OnSeriesStarted += HandleSeriesStarted;
                rangeManager.OnSeriesFinished += HandleSeriesFinished;
                rangeManager.OnTimerUpdated += HandleTimerUpdated;

                UpdateRecordDisplay();
            }

            if (pistol != null)
            {
                pistol.OnAmmoChanged += HandleAmmoChanged;
            }
        }

        private void OnDestroy()
        {
            if (rangeManager != null)
            {
                rangeManager.OnScoreUpdated -= HandleScoreUpdated;
                rangeManager.OnShotLanded -= HandleShotLanded;
                rangeManager.OnSeriesStarted -= HandleSeriesStarted;
                rangeManager.OnSeriesFinished -= HandleSeriesFinished;
                rangeManager.OnTimerUpdated -= HandleTimerUpdated;
            }

            if (pistol != null)
            {
                pistol.OnAmmoChanged -= HandleAmmoChanged;
            }
        }

        private int cachedShotsFired = 0;
        private int cachedMaxShots = 10;
        private int cachedAmmo = 10;

        private void HandleScoreUpdated(int currentScore, int shotsFired, int maxShots)
        {
            cachedShotsFired = shotsFired;
            cachedMaxShots = maxShots;

            if (scoreText != null)
            {
                scoreText.text = $"Puntos: <color=#FFD700>{currentScore}</color> / {maxShots * 10}";
            }

            UpdateShotsAndAmmoDisplay();
        }

        private void HandleShotLanded(int score, bool isBullseye, Vector3 hitPoint)
        {
            if (feedbackBannerText == null) return;

            if (isBullseye)
            {
                feedbackBannerText.text = "★ ¡BULLSEYE 10X PERFECTO! ★";
                feedbackBannerText.color = new Color(1f, 0.85f, 0.1f);
            }
            else if (score >= 9)
            {
                feedbackBannerText.text = $"¡Excelente impacto! +{score} pts";
                feedbackBannerText.color = new Color(0.2f, 0.95f, 0.4f);
            }
            else if (score > 0)
            {
                feedbackBannerText.text = $"Impacto en zona: +{score} pts";
                feedbackBannerText.color = Color.white;
            }
            else
            {
                feedbackBannerText.text = "¡Disparo fuera de diana!";
                feedbackBannerText.color = new Color(0.9f, 0.25f, 0.25f);
            }
        }

        private void HandleSeriesStarted(int seriesIndex, float distance)
        {
            if (distanceText != null)
            {
                distanceText.text = $"Serie {seriesIndex} • Distancia: {distance:0}m";
            }

            if (feedbackBannerText != null)
            {
                feedbackBannerText.text = "¡Alinea tu mira de hierro y dispara!";
                feedbackBannerText.color = Color.cyan;
            }

            UpdateRecordDisplay();
        }

        private void HandleSeriesFinished(string message, int finalScore, string medal)
        {
            if (feedbackBannerText != null)
            {
                feedbackBannerText.text = $"{medal}\n{message}";
                feedbackBannerText.color = new Color(1f, 0.85f, 0.1f);
            }

            UpdateRecordDisplay();
        }

        private void HandleTimerUpdated(float seconds)
        {
            if (timerText == null) return;
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            timerText.text = $"Tiempo: {mins:00}:{secs:00}";
        }

        private void HandleAmmoChanged(int current, int max)
        {
            cachedAmmo = current;
            UpdateShotsAndAmmoDisplay();

            if (current == 0 && feedbackBannerText != null)
            {
                feedbackBannerText.text = "¡Cargador vacío! Inclina o sacude la mano hacia abajo para recargar";
                feedbackBannerText.color = new Color(1f, 0.35f, 0.35f);
            }
            else if (current == max && feedbackBannerText != null && feedbackBannerText.text.Contains("Cargador vacío"))
            {
                feedbackBannerText.text = "¡Cargador recargado! Listo para disparar";
                feedbackBannerText.color = new Color(0f, 1f, 0.75f);
            }
        }

        private void UpdateShotsAndAmmoDisplay()
        {
            if (shotsText != null)
            {
                string ammoColor = cachedAmmo > 3 ? "#00FFAA" : (cachedAmmo > 0 ? "#FFB300" : "#FF3333");
                shotsText.text = $"Tiros: {cachedShotsFired}/{cachedMaxShots}  |  Cargador: <color={ammoColor}>{cachedAmmo}</color>";
            }
        }

        private void UpdateRecordDisplay()
        {
            if (recordText == null || rangeManager == null || rangeManager.SaveData == null) return;
            var data = rangeManager.SaveData;
            recordText.text = $"Récord: {data.highestScore} pts | Bullseyes: {data.totalBullseyes} | 🥇{data.goldMedals} 🥈{data.silverMedals} 🥉{data.bronzeMedals}";
        }
    }
}
