using UnityEngine;


[CreateAssetMenu(fileName = "NewScoreEvent", menuName = "VR Sports/Score Event Data")]
public class ScoreEventData : ScriptableObject
{
    [Header("Identification")]
    public string eventName;        // e.g. "Basketball Made Shot", "Pin Knocked Down"
    public SportType sport;         // Which sport this belongs to

    [Header("Scoring")]
    public int pointValue;          // How many points this action awards
    public bool isBonus;            // Optional: mark special/bonus scoring events

    [Header("Feedback (optional)")]
    public AudioClip scoreSound;    // Sound to play when this event triggers
    [TextArea(1, 3)]
    public string feedbackMessage;  // e.g. "Nice shot!", "Strike!"
}

public enum SportType
{
    Basketball,
    Bowling,
    SwordFighting,
    Shooting
}