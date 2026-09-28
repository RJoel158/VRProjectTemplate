using UnityEngine;


public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public MinigameData currentMinigame;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // If attached to a GameObject that also contains MainMenuManager or AudioSources,
        // move GameSession to a dedicated clean GameObject so menu audio is not kept alive across scenes.
        if (GetComponent<MainMenuManager>() != null || GetComponent<AudioSource>() != null)
        {
            GameObject dedicatedGO = new GameObject("[GameSession]");
            GameSession newSession = dedicatedGO.AddComponent<GameSession>();
            newSession.currentMinigame = this.currentMinigame;
            Instance = newSession;
            DontDestroyOnLoad(dedicatedGO);
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); 
    }

    public void SelectMinigame(MinigameData data)
    {
        currentMinigame = data;
    }
}