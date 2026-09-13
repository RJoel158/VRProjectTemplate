using UnityEngine;

// Sits on the invisible trigger collider at the center of the hoop.
// Detects a made basket and reports it to the ScoreManager using
// a designer-configured ScoreEventData asset (e.g. "BasketballMadeShot.asset").
public class HoopScoreDetector : MonoBehaviour
{
    [Tooltip("Drag the ScoreEventData asset for 'made shot' here in the Inspector.")]
    public ScoreEventData madeShotEvent;

    [Tooltip("Tag used by the basketball GameObject.")]
    public string ballTag = "Basketball";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(ballTag)) return;

        Rigidbody ballRb = other.attachedRigidbody;
        if (ballRb == null) return;

        // Only count it if the ball is moving downward through the hoop,
        // so bounces or side-entries don't falsely trigger a score.
        if (ballRb.linearVelocity.y < 0f)
        {
            ScoreManager.Instance.RegisterScoreEvent(madeShotEvent);
        }
    }
}