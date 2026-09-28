using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SportEndPanel : MonoBehaviour
{
    [Header("Buttons (hidden during gameplay)")]
    [SerializeField] private Button continueButton; // Circuit Mode only
    [SerializeField] private Button retryButton;     // Free Mode only

    public void SetButtons(Button cont, Button ret)
    {
        continueButton = cont;
        retryButton = ret;
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
    }

    void Awake()
    {
        continueButton.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(false);

        if (CircuitFlowManager.Instance == null)
        {
            Debug.LogWarning("SportEndPanel: No CircuitFlowManager found. Treating as Free Mode (scene opened directly).");
        }
    }

    private bool hasEnded = false;

    // Hook this to GameTimer -> On Time Up ()
    public void OnTimeUp()
    {
        if (hasEnded) return;
        hasEnded = true;

        int finalScore = 0;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.FreezeScore();
            finalScore = ScoreManager.Instance.GetCurrentScore();
        }

        ShowEndButtons(finalScore);
    }

    private void ShowEndButtons(int finalScore)
    {
        bool isCircuitMode = CircuitFlowManager.Instance != null &&
                             CircuitFlowManager.Instance.CurrentMode == GameMode.Circuit;

        if (continueButton != null) continueButton.gameObject.SetActive(isCircuitMode);
        if (retryButton != null) retryButton.gameObject.SetActive(!isCircuitMode);

        if (isCircuitMode && SaveManager.Instance != null && SaveManager.Instance.CurrentRunData != null)
        {
            CircuitSaveData run = SaveManager.Instance.CurrentRunData;
            SaveManager.Instance.SaveSportScore(run, run.currentIndex, finalScore);
            SaveManager.Instance.LogScores("SPORT FINISHED - SAVED", run);

            if (run.currentIndex >= run.sportOrder.Length)
            {
                var tmp = continueButton != null ? continueButton.GetComponentInChildren<TMPro.TextMeshProUGUI>() : null;
                if (tmp != null) tmp.text = "VER TABLA GLOBAL";
            }
        }
    }

    // Hook this to the "Continue" button OnClick()
    public void OnContinuePressed()
    {
        Debug.Log("OnContinuePressed called");

        CircuitSaveData run = SaveManager.Instance != null ? SaveManager.Instance.CurrentRunData : null;

        if (run != null && run.currentIndex >= run.sportOrder.Length)
        {
            int total = 0;
            string breakdown = "";
            for (int i = 0; i < run.sportOrder.Length; i++)
            {
                total += run.scores[i];
                breakdown += $"{run.sportOrder[i]}: {run.scores[i]}\n";
            }
            Debug.Log($"CIRCUIT COMPLETE!\n{breakdown}TOTAL SCORE: {total}");

            CircuitRunRecord record = SaveManager.Instance.RecordCompletedCircuit(run);
            SaveManager.Instance.DeleteCircuitSave();

            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (retryButton != null) retryButton.gameObject.SetActive(false);

            CircuitLeaderboardUI.ShowOnActiveWhiteboard(record);
        }
        else if (run != null)
        {
            CircuitFlowManager.Instance.LoadSportByIndex(run.currentIndex);
        }
    }

    // Hook this to the "Retry" button OnClick()
    public void OnRetryPressed()
    {
        Debug.Log("OnRetryPressed called");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}