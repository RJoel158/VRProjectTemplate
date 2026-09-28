using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameModeMenu : MonoBehaviour
{
    [Header("Panels (same scene)")]
    [SerializeField] private GameObject modeSelectionPanel;
    [SerializeField] private GameObject circuitChoicePanel;

    [Header("Circuit Choice Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;

    [Header("Scene Names")]
    [SerializeField] private string freeModeSceneName = "FreeModeMenu";

    void Awake()
    {
        modeSelectionPanel.SetActive(true);
        circuitChoicePanel.SetActive(false);
    }

    public void OnFreeModeButtonPressed()
    {
        CircuitFlowManager.Instance.SetMode(GameMode.Free);
        SceneManager.LoadScene(freeModeSceneName);
    }

    public void OnCircuitModeButtonPressed()
    {
        modeSelectionPanel.SetActive(false);
        circuitChoicePanel.SetActive(true);

        continueButton.interactable = SaveManager.Instance.ExistsCircuitInProgress();
        newGameButton.interactable = true;
    }

    public void OnContinueButtonPressed()
    {
        CircuitSaveData data = SaveManager.Instance.LoadCircuit();
        SaveManager.Instance.LogScores("CONTINUE - LOADED FROM DISK", data);
        SaveManager.Instance.SetCurrentRun(data);
        CircuitFlowManager.Instance.SetMode(GameMode.Circuit);
        CircuitFlowManager.Instance.LoadSportByIndex(data.currentIndex);
    }

    public void OnNewGameButtonPressed()
    {
        CircuitSaveData data = SaveManager.Instance.CreateNewCircuit(CircuitFlowManager.Instance.GetSceneOrder());
        SaveManager.Instance.SetCurrentRun(data);
        CircuitFlowManager.Instance.SetMode(GameMode.Circuit);
        CircuitFlowManager.Instance.LoadSportByIndex(0);
    }

    public void OnBackToModeSelection()
    {
        circuitChoicePanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }
}