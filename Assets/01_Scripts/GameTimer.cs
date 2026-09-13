using UnityEngine;
using UnityEngine.Events;
using TMPro;


public class GameTimer : MonoBehaviour
{
    [Tooltip("Drag the TextMeshPro object that shows the timer here.")]
    public TextMeshProUGUI timerText;

    [Tooltip("Fallback time (seconds) used only if no MinigameData is found via GameSession.")]
    public float fallbackTimeSeconds = 60f;

    public UnityEvent onTimeUp;

    private float timeRemaining;
    private bool isRunning;

    private void Start()
    {
        float startTime = fallbackTimeSeconds;

        
        if (GameSession.Instance != null && GameSession.Instance.currentMinigame != null)
        {
            startTime = GameSession.Instance.currentMinigame.timeLimitSeconds;
        }

        timeRemaining = startTime;
        isRunning = true;
        UpdateTimerText();
    }

    private void Update()
    {
        if (!isRunning) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isRunning = false;
            onTimeUp?.Invoke();
        }

        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}