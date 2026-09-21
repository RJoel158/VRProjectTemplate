using UnityEngine;

// Put this on the basketball. Plays a bounce sound every time it collides
// with an object tagged "Floor" (the court floor collider).
[RequireComponent(typeof(Rigidbody))]
public class BallFloorBounceSound : MonoBehaviour
{
    [Tooltip("AudioSource used to play the bounce sound. If left empty, one will be added automatically.")]
    public AudioSource audioSource;
    [Tooltip("Sound played each time the ball touches the floor.")]
    public AudioClip floorBounceSound;
    [Tooltip("Tag used by the court floor collider.")]
    public string floorTag = "Floor";
    [Tooltip("Minimum seconds between bounce sounds, to avoid rapid-fire spam on fast little bounces.")]
    public float minTimeBetweenSounds = 0.15f;

    private float lastSoundTime = -999f;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.collider.CompareTag(floorTag)) return;
        if (floorBounceSound == null) return;

        if (Time.time - lastSoundTime >= minTimeBetweenSounds)
        {
            audioSource.PlayOneShot(floorBounceSound);
            lastSoundTime = Time.time;
        }
    }
}