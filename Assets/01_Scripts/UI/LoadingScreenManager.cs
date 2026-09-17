using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class LoadingScreenManager : MonoBehaviour
{
    [Header("UI del panel de carga")]
    public GameObject loadingPanel;
    public Image gameIcon;
    public RectTransform spinner;

    [Header("Configuración")]
    public float spinnerSpeed = 180f;      // grados por segundo
    public float minLoadingTime = 1.5f;    // tiempo mínimo visible, aunque la escena cargue más rápido

    public void LoadSceneWithAnimation(string sceneName, Sprite icon)
    {
        StartCoroutine(LoadRoutine(sceneName, icon));
    }

    private IEnumerator LoadRoutine(string sceneName, Sprite icon)
    {
        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (gameIcon != null && icon != null) gameIcon.sprite = icon;

        float timer = 0f;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        // Mientras la escena carga en segundo plano, giramos el spinner
        while (operation.progress < 0.9f || timer < minLoadingTime)
        {
            timer += Time.deltaTime;

            if (spinner != null)
                spinner.Rotate(0f, 0f, -spinnerSpeed * Time.deltaTime);

            yield return null;
        }

        operation.allowSceneActivation = true;
    }
}