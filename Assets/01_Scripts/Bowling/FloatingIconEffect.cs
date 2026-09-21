using UnityEngine;

// Put this on the floating icon Prefab (the one with a SpriteRenderer).
// On spawn, it drifts upward and fades to transparent over time, then
// destroys itself automatically - no manual cleanup needed.
[RequireComponent(typeof(SpriteRenderer))]
public class FloatingIconEffect : MonoBehaviour
{
    [Tooltip("How fast the icon rises, in units per second.")]
    public float floatSpeed = 0.5f;

    [Tooltip("How many seconds the whole float + fade takes before it disappears.")]
    public float duration = 1.5f;

    [Tooltip("Manual rotation (in degrees) applied so the sprite faces the player correctly. Adjust in the Inspector - common fixes are (0, 90, 0) or (90, 0, 0) depending on how your sprite plane is oriented.")]
    public Vector3 manualRotationOffset = Vector3.zero;

    private SpriteRenderer spriteRenderer;
    private float elapsedTime = 0f;
    private Color startColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startColor = spriteRenderer.color;

        // Apply the manual rotation once, right when it spawns.
        transform.rotation = Quaternion.Euler(manualRotationOffset);
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;

        // Move upward at a constant speed.
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // Fade alpha from fully visible (1) to invisible (0) over 'duration'.
        float progress = Mathf.Clamp01(elapsedTime / duration);
        float newAlpha = Mathf.Lerp(startColor.a, 0f, progress);
        spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, newAlpha);

        if (elapsedTime >= duration)
        {
            Destroy(gameObject);
        }
    }
}