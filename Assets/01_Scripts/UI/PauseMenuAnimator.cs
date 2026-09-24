using System.Collections;
using UnityEngine;


public class PauseMenuAnimator : MonoBehaviour
{
    [Header("Paneles que se deslizan")]
    public RectTransform topPanel;
    public RectTransform bottomPanel;

    [Header("Botones que hacen fade in/out")]
    [Tooltip("Un CanvasGroup que envuelva todos los botones del menú (Menú, Reiniciar, Continuar, etc).")]
    public CanvasGroup buttonsGroup;

    [Header("Animación")]
    public float duration = 0.35f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Sonido al abrir el menú")]
    public AudioSource audioSource;
    public AudioClip openSound;

    private Vector2 topShownPos;
    private Vector2 bottomShownPos;
    private Vector2 topHiddenPos;
    private Vector2 bottomHiddenPos;

    private Coroutine currentAnim;

    void Awake()
    {
        // Guarda la posición "visible" (la que ya armaste en el editor)
        topShownPos = topPanel.anchoredPosition;
        bottomShownPos = bottomPanel.anchoredPosition;

        // Calcula la posición escondida: arriba se va hacia arriba,
        // abajo se va hacia abajo, cada uno la distancia de su propia altura.
        topHiddenPos = topShownPos + new Vector2(0f, topPanel.rect.height);
        bottomHiddenPos = bottomShownPos - new Vector2(0f, bottomPanel.rect.height);

        // Arranca ya escondido
        topPanel.anchoredPosition = topHiddenPos;
        bottomPanel.anchoredPosition = bottomHiddenPos;

        if (buttonsGroup != null)
        {
            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }
    }

    /// <summary>Llamá esto cuando el jugador abre el menú de pausa.</summary>
    public void Open()
    {
        gameObject.SetActive(true);

        if (audioSource != null && openSound != null)
            audioSource.PlayOneShot(openSound);

        if (buttonsGroup != null)
        {
            buttonsGroup.interactable = false; // se habilita recién al terminar de aparecer
            buttonsGroup.blocksRaycasts = true;
        }

        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(AnimateTo(
            topShownPos, bottomShownPos, targetAlpha: 1f,
            onComplete: () =>
            {
                if (buttonsGroup != null) buttonsGroup.interactable = true;
            }));
    }

    /// <summary>Llamá esto cuando el jugador cierra el menú de pausa.</summary>
    public void Close()
    {
        if (buttonsGroup != null)
        {
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }

        if (currentAnim != null) StopCoroutine(currentAnim);
        currentAnim = StartCoroutine(AnimateTo(
            topHiddenPos, bottomHiddenPos, targetAlpha: 0f,
            onComplete: () => gameObject.SetActive(false)));
    }

    private IEnumerator AnimateTo(Vector2 topTarget, Vector2 bottomTarget, float targetAlpha, System.Action onComplete = null)
    {
        Vector2 topStart = topPanel.anchoredPosition;
        Vector2 bottomStart = bottomPanel.anchoredPosition;
        float alphaStart = buttonsGroup != null ? buttonsGroup.alpha : 0f;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // sigue funcionando aunque Time.timeScale esté en 0
            float progress = easeCurve.Evaluate(Mathf.Clamp01(t / duration));

            topPanel.anchoredPosition = Vector2.LerpUnclamped(topStart, topTarget, progress);
            bottomPanel.anchoredPosition = Vector2.LerpUnclamped(bottomStart, bottomTarget, progress);

            if (buttonsGroup != null)
                buttonsGroup.alpha = Mathf.Lerp(alphaStart, targetAlpha, progress);

            yield return null;
        }

        topPanel.anchoredPosition = topTarget;
        bottomPanel.anchoredPosition = bottomTarget;
        if (buttonsGroup != null) buttonsGroup.alpha = targetAlpha;

        onComplete?.Invoke();
    }
}