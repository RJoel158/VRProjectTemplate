using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Tiro.UI
{
    /// <summary>
    /// Tarjeta de puntuacion estilo Pizarra Flotante (Whiteboard) para el circuito de Tiro Olimpico VR.
    /// Estandarizada visualmente con las pizarras de Bowling, Basketball y Golf.
    /// Muestra unicamente el Score acumulado, la fase/disciplina actual y el cronometro.
    /// </summary>
    public class ShootingScoreboardUI : MonoBehaviour
    {
        [Header("Whiteboard Text Displays")]
        [Tooltip("Texto para mostrar la puntuacion (Score: X)")]
        [SerializeField] private TextMeshProUGUI scoreText;

        [Tooltip("Texto para mostrar la disciplina activa (Fase 1/3: Rifle Olimpico)")]
        [SerializeField] private TextMeshProUGUI disciplineText;

        [Tooltip("Texto de tiempo en la pizarra")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Event Banner")]
        [SerializeField] private GameObject bannerPanel;
        [SerializeField] private TextMeshProUGUI bannerTitleText;
        [SerializeField] private TextMeshProUGUI bannerSubtitleText;

        private Coroutine bannerCoroutine;

        private void Awake()
        {
            if (bannerPanel != null) bannerPanel.SetActive(false);
            FormatDisplays();
        }

        private void Start()
        {
            FormatDisplays();

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.onScoreChanged.AddListener(HandleCircuitScoreChanged);
                UpdateScore(ScoreManager.Instance.GetCurrentScore());
            }
            else
            {
                UpdateScore(0);
            }
        }

        private void OnDestroy()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.onScoreChanged.RemoveListener(HandleCircuitScoreChanged);
            }
        }

        private void FormatDisplays()
        {
            FormatText(scoreText, 60f);
            FormatText(disciplineText, 60f);
            FormatText(timerText, 55f);
        }

        private void FormatText(TextMeshProUGUI tmp, float targetFontSize)
        {
            if (tmp == null) return;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            if (tmp.fontSize > targetFontSize + 5f)
            {
                tmp.fontSize = targetFontSize;
            }

            RectTransform rt = tmp.rectTransform;
            if (rt != null && rt.sizeDelta.x < 500f)
            {
                float currentLeft = rt.anchoredPosition.x - (rt.sizeDelta.x * rt.pivot.x);
                rt.sizeDelta = new Vector2(500f, Mathf.Max(rt.sizeDelta.y, 75f));
                rt.anchoredPosition = new Vector2(currentLeft + (500f * rt.pivot.x), rt.anchoredPosition.y);
            }
        }

        private void HandleCircuitScoreChanged(int totalScore)
        {
            UpdateScore(totalScore);
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {score}";
            }
        }

        public void UpdateDiscipline(int phaseIndex, int totalPhases, string disciplineName)
        {
            if (disciplineText != null)
            {
                disciplineText.text = $"Fase {phaseIndex}/{totalPhases}: {disciplineName}";
            }
        }

        public void UpdateTimer(float timeRemainingSeconds)
        {
            if (timerText != null)
            {
                float clamped = Mathf.Max(0f, timeRemainingSeconds);
                int minutes = Mathf.FloorToInt(clamped / 60f);
                int seconds = Mathf.FloorToInt(clamped % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        public void ShowBanner(string title, string subtitle, float duration = 2.5f)
        {
            if (bannerCoroutine != null)
            {
                StopCoroutine(bannerCoroutine);
            }
            bannerCoroutine = StartCoroutine(BannerRoutine(title, subtitle, duration));
        }

        private IEnumerator BannerRoutine(string title, string subtitle, float duration)
        {
            if (bannerPanel != null)
            {
                if (bannerTitleText != null) bannerTitleText.text = title;
                if (bannerSubtitleText != null) bannerSubtitleText.text = subtitle;
                bannerPanel.SetActive(true);
            }

            yield return new WaitForSeconds(duration);

            if (bannerPanel != null)
            {
                bannerPanel.SetActive(false);
            }
            bannerCoroutine = null;
        }
    }
}
