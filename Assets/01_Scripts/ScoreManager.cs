using UnityEngine;
using UnityEngine.Events;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Runtime State")]
    [SerializeField] private int currentScore;

    [Header("Events")]
    public UnityEvent<int> onScoreChanged;

    private bool isFrozen = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void RegisterScoreEvent(ScoreEventData scoreEvent)
    {
        if (isFrozen) return;

        if (scoreEvent == null)
        {
            Debug.LogWarning("ScoreManager: tried to register a null ScoreEventData.");
            return;
        }

        currentScore += scoreEvent.pointValue;

        if (scoreEvent.scoreSound != null)
        {
            AudioSource.PlayClipAtPoint(scoreEvent.scoreSound, Camera.main.transform.position);
        }

        if (!string.IsNullOrEmpty(scoreEvent.feedbackMessage))
        {
            Debug.Log($"[{scoreEvent.sport}] {scoreEvent.feedbackMessage} (+{scoreEvent.pointValue})");
        }

        onScoreChanged?.Invoke(currentScore);
    }

    public void AddPoints(int amount)
    {
        if (isFrozen) return;

        currentScore += amount;
        onScoreChanged?.Invoke(currentScore);
    }

    public int GetCurrentScore() => currentScore;

    // Call when time is up: no more points can be added
    public void FreezeScore()
    {
        isFrozen = true;
    }

    public void ResetScore()
    {
        isFrozen = false;
        currentScore = 0;
        onScoreChanged?.Invoke(currentScore);
    }
}