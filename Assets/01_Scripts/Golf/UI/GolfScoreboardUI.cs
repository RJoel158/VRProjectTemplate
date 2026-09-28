using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Golf.Data;
using Golf.Persistence;
using Golf.Core;

namespace Golf.UI
{
    /// <summary>
    /// Tarjeta de puntuacion estilo Pizarra Flotante (Whiteboard) para el circuito de Minigolf VR.
    /// Estandarizada visualmente con las pizarras de Bowling y Basketball.
    /// Muestra unicamente el Score acumulado y los Golpes (con soporte opcional de temporizador y banner).
    /// </summary>
    public class GolfScoreboardUI : MonoBehaviour
    {
        [Header("Whiteboard Text Displays")]
        [Tooltip("Texto para mostrar la puntuacion traducida de golpes (Score: X)")]
        [SerializeField] private TextMeshProUGUI scoreText;

        [Tooltip("Texto para mostrar los golpes del hoyo/circuito (Golpes: X)")]
        [SerializeField] private TextMeshProUGUI strokesText;

        [Tooltip("Texto de tiempo en la pizarra (opcional, enlazado a GameTimer si existe)")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Optional Banner Feedback")]
        [SerializeField] private GameObject bannerPanel;
        [SerializeField] private TextMeshProUGUI bannerTitleText;
        [SerializeField] private TextMeshProUGUI bannerSubtitleText;

        [Header("Buttons")]
        [SerializeField] private Button restartButton;

        // Campos legados para mantener compatibilidad con referencias ya asignadas en la escena
        [SerializeField, HideInInspector] private TextMeshProUGUI holeTitleText;
        [SerializeField, HideInInspector] private TextMeshProUGUI parText;
        [SerializeField, HideInInspector] private TextMeshProUGUI currentStrokesText;
        [SerializeField, HideInInspector] private TextMeshProUGUI totalScoreText;
        [SerializeField, HideInInspector] private TextMeshProUGUI recordText;

        private Coroutine bannerCoroutine;
        private int cachedStrokes = 0;

        private void Awake()
        {
            // Migrar referencias legadas si las nuevas no se han asignado
            if (scoreText == null && totalScoreText != null) scoreText = totalScoreText;
            if (strokesText == null && currentStrokesText != null) strokesText = currentStrokesText;

            // Ocultar textos no deseados para mantener la interfaz limpia como en Bowling/Basket
            if (holeTitleText != null && holeTitleText != scoreText && holeTitleText != strokesText)
                holeTitleText.gameObject.SetActive(false);
            if (parText != null && parText != scoreText && parText != strokesText)
                parText.gameObject.SetActive(false);
            if (recordText != null && recordText != scoreText && recordText != strokesText)
                recordText.gameObject.SetActive(false);
            if (restartButton != null)
                restartButton.gameObject.SetActive(false);

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

            UpdateStrokes(cachedStrokes);
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
            FormatText(strokesText, 60f);
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

        private void HandleCircuitScoreChanged(int newScore)
        {
            UpdateScore(newScore);
        }

        private void UpdateScore(int newScore)
        {
            if (scoreText != null)
            {
                scoreText.text = "Score: " + newScore;
            }
        }

        private void UpdateStrokes(int strokes)
        {
            cachedStrokes = strokes;
            if (strokesText != null)
            {
                strokesText.text = "Golpes: " + strokes;
            }
        }

        public void UpdateHoleInfo(HoleDataSO holeData, int currentStrokes, int totalStrokes, GolfSaveData saveData)
        {
            UpdateStrokes(currentStrokes);
            int circuitPts = ScoreManager.Instance != null ? ScoreManager.Instance.GetCurrentScore() : 0;
            UpdateScore(circuitPts);
        }

        public void ShowHoleCompletedBanner(string scoreTerm, int strokes, int par, int pointsAwarded = 0)
        {
            if (bannerPanel == null) return;

            string colorHex = (strokes <= par) ? "#2E7D32" : "#E65100";
            if (strokes == 1) colorHex = "#F57F17";

            if (bannerTitleText != null)
            {
                string ptsSuffix = pointsAwarded > 0 ? $" (+{pointsAwarded} PTS)" : "";
                bannerTitleText.text = $"<color={colorHex}>{scoreTerm}{ptsSuffix}</color>";
            }

            if (bannerSubtitleText != null)
            {
                bannerSubtitleText.text = $"{strokes} golpes (Par {par})";
            }

            if (bannerCoroutine != null) StopCoroutine(bannerCoroutine);
            bannerCoroutine = StartCoroutine(ShowBannerRoutine(2.5f));
        }

        public void ShowOutOfBoundsBanner(int penalty)
        {
            if (bannerPanel == null) return;

            if (bannerTitleText != null)
            {
                bannerTitleText.text = "<color=#C62828>FUERA DE PISTA</color>";
            }

            if (bannerSubtitleText != null)
            {
                bannerSubtitleText.text = $"+{penalty} golpe de penalizacion";
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
                bannerTitleText.text = "<color=#1565C0>MINIGOLF COMPLETADO</color>";
            }

            if (bannerSubtitleText != null)
            {
                bannerSubtitleText.text = $"Total: {totalStrokes} golpes ({diffStr})";
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

        public void PositionNearHole(Vector3 teePosition, Vector3 targetDirection)
        {
            Vector3 toTarget = targetDirection;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f) toTarget = Vector3.forward;
            toTarget.Normalize();

            // Ubicar a la izquierda-frente del Tee a la altura comoda del ojo del jugador VR
            Vector3 leftDir = Vector3.Cross(toTarget, Vector3.up).normalized;
            Vector3 boardPos = teePosition + leftDir * 2.2f + toTarget * 1.0f + Vector3.up * 1.45f;

            transform.position = boardPos;
            Vector3 lookAtTarget = teePosition + leftDir * 0.45f + Vector3.up * 1.4f;
            Vector3 forwardDir = (boardPos - lookAtTarget).normalized;
            forwardDir.y = 0f;
            if (forwardDir.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(forwardDir);
            }
        }
    }
}
