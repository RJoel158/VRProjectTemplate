using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Golf.Data;
using Golf.Persistence;

namespace Golf.UI
{
    /// <summary>
    /// Tarjeta de puntuacion y cartel informativo en World Space para el circuito de Minigolf VR.
    /// Muestra el hoyo actual, el par, los golpes del tiro en curso, el record historico
    /// y despliega banners animados al embocar (Hole In One, Birdie, etc.).
    /// </summary>
    public class GolfScoreboardUI : MonoBehaviour
    {
        [Header("Text Displays")]
        [SerializeField] private TextMeshProUGUI holeTitleText;
        [SerializeField] private TextMeshProUGUI parText;
        [SerializeField] private TextMeshProUGUI currentStrokesText;
        [SerializeField] private TextMeshProUGUI totalScoreText;
        [SerializeField] private TextMeshProUGUI recordText;

        [Header("Banner Feedback")]
        [SerializeField] private GameObject bannerPanel;
        [SerializeField] private TextMeshProUGUI bannerTitleText;
        [SerializeField] private TextMeshProUGUI bannerSubtitleText;

        [Header("Buttons")]
        [SerializeField] private Button restartButton;

        private Coroutine bannerCoroutine;

        private void Awake()
        {
            if (bannerPanel != null) bannerPanel.SetActive(false);
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }
        }

        public void UpdateHoleInfo(HoleDataSO holeData, int currentStrokes, int totalStrokes, GolfSaveData saveData)
        {
            if (holeTitleText != null && holeData != null)
            {
                holeTitleText.text = $"HOYO {holeData.HoleNumber}: {holeData.HoleName.ToUpper()}";
            }

            if (parText != null && holeData != null)
            {
                parText.text = $"PAR: {holeData.Par}  |  MAX: {holeData.MaxStrokes}";
            }

            if (currentStrokesText != null)
            {
                currentStrokesText.text = $"GOLPES EN ESTE HOYO: <color=#00E5FF>{currentStrokes}</color>";
            }

            if (totalScoreText != null)
            {
                totalScoreText.text = $"TOTAL CIRCUITO: <color=#FFD700>{totalStrokes}</color>";
            }

            if (recordText != null && saveData != null && holeData != null)
            {
                int best = saveData.GetBestForHole(holeData.HoleNumber);
                string bestStr = best < 90 ? $"{best} golpes" : "--";
                recordText.text = $"RECORD HISTORICO: {bestStr}  |  MEJOR CIRCUITO: {(saveData.bestTotalScore < 900 ? saveData.bestTotalScore.ToString() : "--")}";
            }
        }

        public void ShowHoleCompletedBanner(string scoreTerm, int strokes, int par)
        {
            if (bannerPanel == null) return;

            string colorHex = (strokes <= par) ? "#00FF66" : "#FFB300";
            if (strokes == 1) colorHex = "#FFD700"; // Oro

            if (bannerTitleText != null)
            {
                bannerTitleText.text = $"<color={colorHex}>{scoreTerm}</color>";
            }

            if (bannerSubtitleText != null)
            {
                bannerSubtitleText.text = $"{strokes} golpes en Par {par}";
            }

            if (bannerCoroutine != null) StopCoroutine(bannerCoroutine);
            bannerCoroutine = StartCoroutine(ShowBannerRoutine(2.8f));
        }

        public void ShowOutOfBoundsBanner(int penalty)
        {
            if (bannerPanel == null) return;

            if (bannerTitleText != null)
            {
                bannerTitleText.text = "<color=#FF3333>¡FUERA DE PISTA!</color>";
            }

            if (bannerSubtitleText != null)
            {
                bannerSubtitleText.text = $"Pelota recolocada (+{penalty} golpe de penalizacion)";
            }

            if (bannerCoroutine != null) StopCoroutine(bannerCoroutine);
            bannerCoroutine = StartCoroutine(ShowBannerRoutine(2.0f));
        }

        public void ShowCourseFinished(int totalStrokes, int totalPar, GolfSaveData saveData)
        {
            if (bannerPanel == null) return;

            int diff = totalStrokes - totalPar;
            string diffStr = diff == 0 ? "E (PAR)" : (diff < 0 ? $"{diff}" : $"+{diff}");

            if (bannerTitleText != null)
            {
                bannerTitleText.text = "<color=#00E5FF>¡CIRCUITO DE MINIGOLF COMPLETADO!</color>";
            }

            if (bannerSubtitleText != null)
            {
                bannerSubtitleText.text = $"Puntuacion Final: {totalStrokes} golpes ({diffStr})\n¡Nuevo record guardado en disco!";
            }

            bannerPanel.SetActive(true);
        }

        private IEnumerator ShowBannerRoutine(float duration)
        {
            bannerPanel.SetActive(true);
            yield return new WaitForSeconds(duration);
            bannerPanel.SetActive(false);
            bannerCoroutine = null;
        }

        private void OnRestartClicked()
        {
            if (Golf.Core.GolfCourseManager.Instance != null)
            {
                Golf.Core.GolfCourseManager.Instance.StartCourse();
            }
        }
    }
}
