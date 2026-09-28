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
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
    }

    // Hook this to GameTimer -> On Time Up ()
    public void OnTimeUp()
    {
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
        }
    }

    // Hook this to the "Continue" button OnClick()
    public void OnContinuePressed()
    {
        Debug.Log("OnContinuePressed called");

        CircuitSaveData run = SaveManager.Instance.CurrentRunData;

        if (run.currentIndex >= run.sportOrder.Length)
        {
            int total = 0;
            string breakdown = "";
            for (int i = 0; i < run.sportOrder.Length; i++)
            {
                total += run.scores[i];
                breakdown += $"{run.sportOrder[i]}: {run.scores[i]}\n";
            }
            Debug.Log($"CIRCUIT COMPLETE!\n{breakdown}TOTAL SCORE: {total}");

            SaveManager.Instance.DeleteCircuitSave();
            continueButton.gameObject.SetActive(false);
        }
        else
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