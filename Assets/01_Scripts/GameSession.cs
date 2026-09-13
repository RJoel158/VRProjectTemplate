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
        Instance = this;
        DontDestroyOnLoad(gameObject); 
    }

    public void SelectMinigame(MinigameData data)
    {
        currentMinigame = data;
    }
}