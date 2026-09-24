using UnityEngine;
using UnityEngine.SceneManagement;


public class PauseMenuController : MonoBehaviour
{
    [Tooltip("El PauseMenuAnimator que maneja la animación de deslizamiento y fade (está en el mismo objeto que el Canvas del menú de pausa).")]
    public PauseMenuAnimator pauseMenuAnimator;

    [Tooltip("Name of the main menu scene to return to.")]
    public string mainMenuSceneName = "MainMenu";

    [Header("Sonido al apretar un botón del menú")]
    public AudioSource buttonAudioSource;
    public AudioClip buttonClickSound;

    private bool isPaused = false;

    private void PlayButtonSound()
    {
        if (buttonAudioSource != null && buttonClickSound != null)
            buttonAudioSource.PlayOneShot(buttonClickSound);
    }

    // Call this from your Input Action (Menu button on the controller).
    public void TogglePauseMenu()
    {
        isPaused = !isPaused;

        if (isPaused)
            pauseMenuAnimator.Open();
        else
            pauseMenuAnimator.Close();

        // Optional: actually pause game time while the menu is open.
        // Comment this out if you'd rather let physics keep running
        // (e.g. so the ball/pins can still settle) while paused.
        Time.timeScale = isPaused ? 0f : 1f;
    }

    // Hook this to the "Resume" button's OnClick in the Inspector.
    public void OnResumeButtonPressed()
    {
        PlayButtonSound();

        isPaused = false;
        pauseMenuAnimator.Close();
        Time.timeScale = 1f;
    }

    // Hook this to the "Restart" button's OnClick in the Inspector.
    public void OnRestartButtonPressed()
    {
        PlayButtonSound();

        isPaused = false;
        pauseMenuAnimator.Close();
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
        PlayButtonSound();

        isPaused = false;
        pauseMenuAnimator.Close();
        Time.timeScale = 1f;

        if (VRInputPersistenceManager.Instance != null)
        {
            VRInputPersistenceManager.Instance.CollectAndEnableInputAssets();
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }
}