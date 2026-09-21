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

    [Header("Feedback")]
    [Tooltip("AudioSource used to play the score sound. If left empty, one will be added automatically.")]
    public AudioSource audioSource;
    [Tooltip("Sound played when the ball goes through the hoop.")]
    public AudioClip scoreSound;
    [Tooltip("Prefab with SpriteRenderer + FloatingIconEffect, shown above the hoop when scoring.")]
    public GameObject scoreIconPrefab;
    [Tooltip("How far above this trigger's position the icon spawns.")]
    public Vector3 iconSpawnOffset = new Vector3(0f, 0.3f, 0f);

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

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

            if (audioSource != null && scoreSound != null)
            {
                audioSource.PlayOneShot(scoreSound);
            }

            if (scoreIconPrefab != null)
            {
                Instantiate(scoreIconPrefab, transform.position + iconSpawnOffset, Quaternion.identity);
            }
        }
    }
}