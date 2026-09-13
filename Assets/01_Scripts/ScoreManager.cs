using UnityEngine;
using UnityEngine.Events;

// One single, reusable score manager that ANY sport's scripts can call.
// It doesn't know or care what "made a shot" or "knocked a pin" means -
// it just receives a ScoreEventData asset and adds its pointValue.
//
// This is the payoff of using ScriptableObjects here: the same component
// can live in the Basketball scene, the Bowling scene, the Sword scene,
// and the Shooting scene, with zero code duplication and zero if/else
// chains checking "which sport am I in".
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Runtime State")]
    [SerializeField] private int currentScore;

    [Header("Events")]
    public UnityEvent<int> onScoreChanged; // e.g. hook up to UI text update

    private void Awake()
    {
        // Simple singleton so any script in the scene can reach it easily.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Call this from ANY sport's script, passing the relevant ScoreEventData asset.
    // Example (basketball): ScoreManager.Instance.RegisterScoreEvent(madeShotEventData);
    // Example (bowling):    ScoreManager.Instance.RegisterScoreEvent(pinKnockedEventData);
    public void RegisterScoreEvent(ScoreEventData scoreEvent)
    {
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

    public int GetCurrentScore() => currentScore;

    public void ResetScore()
    {
        currentScore = 0;
        onScoreChanged?.Invoke(currentScore);
    }
}