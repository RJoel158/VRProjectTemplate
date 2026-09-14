using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public MinigameData selectedMinigame;

    public void SelectMinigame(MinigameData minigame)
    {
        selectedMinigame = minigame;

        SceneManager.LoadScene(minigame.sceneName);
    }
}