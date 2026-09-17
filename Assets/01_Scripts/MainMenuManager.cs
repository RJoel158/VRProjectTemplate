using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Datos")]
    public MinigameData selectedMinigame;

    [Header("Audio")]
    public AudioSource audioSource;      // Arrastra un AudioSource del Canvas
    public AudioClip clickSound;         // El sonido "clic" del Wii

    [Header("Pantalla de carga")]
    public LoadingScreenManager loadingScreen; // Referencia al objeto que maneja la animación de carga

    public void SelectMinigame(MinigameData minigame)
    {
        selectedMinigame = minigame;

       
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }

    
        if (loadingScreen != null)
        {
            loadingScreen.LoadSceneWithAnimation(minigame.sceneName, minigame.icon);
        }
        else
        {
           
            SceneManager.LoadScene(minigame.sceneName);
        }
    }
}