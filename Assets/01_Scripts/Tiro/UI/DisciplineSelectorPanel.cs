using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Tiro.Core;

namespace Tiro.UI
{
    /// <summary>
    /// Panel Interactivo de la pared izquierda del puesto de tiro.
    /// Permite al jugador seleccionar entre las 3 disciplinas olímpicas:
    /// 1. Rifle de Precisión a Distancia
    /// 2. Pistola Rápida en Pared Dinámica
    /// 3. Tiro al Plato con Escopeta
    /// Los botones pueden presionarse con el rayo de interacción VR o disparándoles directamente.
    /// </summary>
    public class DisciplineSelectorPanel : MonoBehaviour
    {
        [Header("Manager Reference")]
        [SerializeField] private ShootingRangeManager rangeManager;

        [Header("UI Text Displays")]
        [SerializeField] private TextMeshProUGUI headerTitleText;
        [SerializeField] private TextMeshProUGUI currentDisciplineText;
        [SerializeField] private TextMeshProUGUI scoreBoardText;
        [SerializeField] private TextMeshProUGUI recordsText;

        [Header("Buttons")]
        [SerializeField] private Button btnSequence;
        [SerializeField] private Button btnRifle;
        [SerializeField] private Button btnPistol;
        [SerializeField] private Button btnShotgun;
        [SerializeField] private Button btnRestart;

        [Header("Audio Feedback")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip clickSound;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponent<AudioSource>();

            // Ocultar botones de seleccion de modo para estandarizar el inicio directo del circuito
            if (btnSequence != null) btnSequence.gameObject.SetActive(false);
            if (btnRifle != null) btnRifle.gameObject.SetActive(false);
            if (btnPistol != null) btnPistol.gameObject.SetActive(false);
            if (btnShotgun != null) btnShotgun.gameObject.SetActive(false);
            if (btnRestart != null) btnRestart.gameObject.SetActive(false);

            AlignToShooterView();
            UpdatePanelDisplay();
        }

        public void AlignToShooterView()
        {
            Vector3 menuPos = new Vector3(-1.35f, 1.35f, 0.95f);
            transform.position = menuPos;

            Vector3 lookTarget = new Vector3(0f, 1.45f, 0f);
            Vector3 toPlayer = (lookTarget - menuPos).normalized;
            transform.rotation = Quaternion.LookRotation(-toPlayer, Vector3.up);

            var canvas = transform.Find("Station_Canvas");
            if (canvas != null)
            {
                canvas.localPosition = new Vector3(0f, 0f, -0.045f);
                canvas.localRotation = Quaternion.identity;
            }
        }

        private void Start()
        {
            if (rangeManager == null) rangeManager = ShootingRangeManager.Instance;

            if (rangeManager != null)
            {
                rangeManager.OnDisciplineChanged += HandleDisciplineChanged;
                rangeManager.OnScoreUpdated += HandleScoreUpdated;
                rangeManager.OnSeriesFinished += HandleSeriesFinished;
            }

            // Conectar botones UI
            if (btnSequence != null) btnSequence.onClick.AddListener(OnStartSequence);
            if (btnRifle != null) btnRifle.onClick.AddListener(() => OnSelectDiscipline(ShootingDiscipline.OlympicRifleDistance));
            if (btnPistol != null) btnPistol.onClick.AddListener(() => OnSelectDiscipline(ShootingDiscipline.DynamicPistolWall));
            if (btnShotgun != null) btnShotgun.onClick.AddListener(() => OnSelectDiscipline(ShootingDiscipline.ClayPigeonShotgun));
            if (btnRestart != null) btnRestart.onClick.AddListener(OnRestartCurrentRound);

            UpdatePanelDisplay();
        }

        private void OnDestroy()
        {
            if (rangeManager != null)
            {
                rangeManager.OnDisciplineChanged -= HandleDisciplineChanged;
                rangeManager.OnScoreUpdated -= HandleScoreUpdated;
                rangeManager.OnSeriesFinished -= HandleSeriesFinished;
            }
        }

        public void OnStartSequence()
        {
            PlayClickSound();
            if (rangeManager != null)
            {
                rangeManager.StartFullOlympicSequence();
            }
            UpdatePanelDisplay();
        }

        public void OnSelectDiscipline(ShootingDiscipline discipline)
        {
            PlayClickSound();
            if (rangeManager != null)
            {
                rangeManager.SelectDiscipline(discipline);
            }
            UpdatePanelDisplay();
        }

        public void OnRestartCurrentRound()
        {
            PlayClickSound();
            if (rangeManager != null)
            {
                rangeManager.StartNewDisciplineRound();
            }
            UpdatePanelDisplay();
        }

        private void HandleDisciplineChanged(ShootingDiscipline newDiscipline)
        {
            UpdatePanelDisplay();
        }

        private void HandleScoreUpdated(int score, int shots, int maxShots)
        {
            if (scoreBoardText != null)
            {
                int circuitPts = ScoreManager.Instance != null ? ScoreManager.Instance.GetCurrentScore() : score;
                scoreBoardText.text = $"PUNTOS CIRCUITO: <color=#FFD700>{circuitPts} pts</color>  |  TIROS: {shots}/{maxShots}";
            }
        }

        private void HandleSeriesFinished(string message, int finalScore, string medal)
        {
            UpdatePanelDisplay();
            if (scoreBoardText != null)
            {
                int circuitPts = ScoreManager.Instance != null ? ScoreManager.Instance.GetCurrentScore() : finalScore;
                scoreBoardText.text = $"{medal}\n<color=#FFD700>PUNTOS TOTALES: {circuitPts} pts</color>";
            }
        }

        public void UpdatePanelDisplay()
        {
            if (rangeManager == null) rangeManager = ShootingRangeManager.Instance;
            if (rangeManager == null) return;

            if (headerTitleText != null)
            {
                headerTitleText.text = "CIRCUITO DE TIRO OLIMPICO";
            }

            if (currentDisciplineText != null)
            {
                currentDisciplineText.text = "MODALIDAD: <color=#00E5FF>[RIFLE DE PRECISION (10m, 25m, 50m)]</color>";
            }

            if (scoreBoardText != null)
            {
                int circuitPts = ScoreManager.Instance != null ? ScoreManager.Instance.GetCurrentScore() : 0;
                int maxShots = rangeManager != null && rangeManager.Config != null ? rangeManager.Config.shotsPerSeries : 10;
                int shotsFired = rangeManager != null ? rangeManager.ShotsFiredInSeries : 0;
                scoreBoardText.text = $"PUNTOS CIRCUITO: <color=#FFD700>{circuitPts} pts</color>  |  TIROS: {shotsFired}/{maxShots}";
            }

            if (recordsText != null && rangeManager.SaveData != null)
            {
                var save = rangeManager.SaveData;
                recordsText.text = $"RECORDS HISTORICOS:\nRifle: {save.rifleHighScore} pts  |  Pistola: {save.pistolHighScore} pts  |  Plato: {save.clayHighScore} pts";
            }
        }

        private void PlayClickSound()
        {
            if (audioSource != null && clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
        }

        /// <summary>
        /// Permite cambiar de disciplina disparándole directamente a los botones del panel.
        /// </summary>
        public void HandleShotOnButton(string buttonTag)
        {
            StartCoroutine(DeferredHandleShotOnButton(buttonTag));
        }

        private System.Collections.IEnumerator DeferredHandleShotOnButton(string buttonTag)
        {
            // Esperar al final del cuadro para que el arma actual termine su ciclo de disparo limpiamente sin ser desactivada a mitad de ejecucion
            yield return new WaitForEndOfFrame();

            if (buttonTag.Contains("sequence") || buttonTag.Contains("circuito") || buttonTag.Contains("torneo")) OnStartSequence();
            else if (buttonTag.Contains("rifle")) OnSelectDiscipline(ShootingDiscipline.OlympicRifleDistance);
            else if (buttonTag.Contains("pistol")) OnSelectDiscipline(ShootingDiscipline.DynamicPistolWall);
            else if (buttonTag.Contains("shotgun") || buttonTag.Contains("clay")) OnSelectDiscipline(ShootingDiscipline.ClayPigeonShotgun);
            else if (buttonTag.Contains("restart")) OnRestartCurrentRound();
        }
    }
}
