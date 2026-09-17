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
                scoreBoardText.text = $"Puntos Ronda: <color=#FFD700>{score}</color>  |  Tiros: {shots}/{maxShots}";
            }
        }

        private void HandleSeriesFinished(string message, int finalScore, string medal)
        {
            UpdatePanelDisplay();
            if (scoreBoardText != null)
            {
                scoreBoardText.text = $"{medal}\n<color=#FFD700>Puntuación Final: {finalScore} pts</color>";
            }
        }

        public void UpdatePanelDisplay()
        {
            if (rangeManager == null) rangeManager = ShootingRangeManager.Instance;
            if (rangeManager == null) return;

            var discipline = rangeManager.ActiveDiscipline;

            if (currentDisciplineText != null)
            {
                switch (discipline)
                {
                    case ShootingDiscipline.OlympicRifleDistance:
                        currentDisciplineText.text = "MODALIDAD: <color=#00E5FF>🎯 RIFLE DE PRECISIÓN (10m, 25m, 50m)</color>";
                        break;
                    case ShootingDiscipline.DynamicPistolWall:
                        currentDisciplineText.text = "MODALIDAD: <color=#00FF66>🔫 PISTOLA RÁPIDA (Pared Reactiva)</color>";
                        break;
                    case ShootingDiscipline.ClayPigeonShotgun:
                        currentDisciplineText.text = "MODALIDAD: <color=#FF8800>💥 TIRO AL PLATO (Escopeta Skeet)</color>";
                        break;
                }
            }

            if (recordsText != null && rangeManager.SaveData != null)
            {
                var save = rangeManager.SaveData;
                recordsText.text = $"RÉCORDS:\n🎯 Rifle: {save.rifleHighScore} pts  |  🔫 Pistola: {save.pistolHighScore} pts  |  💥 Plato: {save.clayHighScore} pts";
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
            if (buttonTag.Contains("rifle")) OnSelectDiscipline(ShootingDiscipline.OlympicRifleDistance);
            else if (buttonTag.Contains("pistol")) OnSelectDiscipline(ShootingDiscipline.DynamicPistolWall);
            else if (buttonTag.Contains("shotgun") || buttonTag.Contains("clay")) OnSelectDiscipline(ShootingDiscipline.ClayPigeonShotgun);
            else if (buttonTag.Contains("restart")) OnRestartCurrentRound();
        }
    }
}
