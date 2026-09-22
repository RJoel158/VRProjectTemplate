using UnityEngine;
using UnityEngine.SceneManagement;

// Controls a World Space Canvas that acts as the pause menu.
// Toggle it open/closed with the controller's Menu button (wired up via
// an Input Action - see the Inspector setup notes below), and use the
// buttons on the menu to resume, restart, or go back to the main menu.
public class PauseMenuController : MonoBehaviour
{
    [Tooltip("The Canvas GameObject that holds all the pause menu UI.")]
    public GameObject pauseMenuCanvas;

    [Tooltip("Name of the main menu scene to return to.")]
    public string mainMenuSceneName = "MainMenu";

    private bool isPaused = false;

    private void Start()
    {
        // Make sure the menu starts hidden.
        pauseMenuCanvas.SetActive(false);
    }

    // Call this from your Input Action (Menu button on the controller).
    public void TogglePauseMenu()
    {
        isPaused = !isPaused;
        pauseMenuCanvas.SetActive(isPaused);

        // Optional: actually pause game time while the menu is open.
        // Comment this out if you'd rather let physics keep running
        // (e.g. so the ball/pins can still settle) while paused.
        Time.timeScale = isPaused ? 0f : 1f;
    }

    // Hook this to the "Resume" button's OnClick in the Inspector.
    public void OnResumeButtonPressed()
    {
        isPaused = false;
        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(false);
        Time.timeScale = 1f;
    }

    // Hook this to the "Restart" button's OnClick in the Inspector.
    public void OnRestartButtonPressed()
    {
        isPaused = false;
        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(false);
        Time.timeScale = 1f;

        if (VRInputPersistenceManager.Instance != null)
        {
            VRInputPersistenceManager.Instance.CollectAndEnableInputAssets();
        }

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    // Hook this to the "Main Menu" button's OnClick in the Inspector.
    public void OnMainMenuButtonPressed()
    {
        isPaused = false;
        if (pauseMenuCanvas != null) pauseMenuCanvas.SetActive(false);
        Time.timeScale = 1f;

        if (VRInputPersistenceManager.Instance != null)
        {
            VRInputPersistenceManager.Instance.CollectAndEnableInputAssets();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}