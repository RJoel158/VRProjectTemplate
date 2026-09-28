using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameMode { None, Free, Circuit }

public class CircuitFlowManager : MonoBehaviour
{
    public static CircuitFlowManager Instance;

    [Header("Circuit order (drag your MinigameData assets here, in order)")]
    public MinigameData[] circuitMinigames;

    public GameMode CurrentMode { get; private set; } = GameMode.None;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetMode(GameMode mode)
    {
        CurrentMode = mode;
    }

    public string[] GetSceneOrder()
    {
        string[] names = new string[circuitMinigames.Length];
        for (int i = 0; i < names.Length; i++)
            names[i] = circuitMinigames[i].sceneName;
        return names;
    }

    // Sets the minigame in GameSession (so GameTimer gets the right time) and loads its scene
    public void LoadSportByIndex(int index)
    {
        MinigameData minigame = circuitMinigames[index];

        if (GameSession.Instance != null)
            GameSession.Instance.SelectMinigame(minigame);
        else
            Debug.LogWarning("CircuitFlowManager: GameSession.Instance is null, timer will use fallback time.");

        SceneManager.LoadScene(minigame.sceneName);
    }
}