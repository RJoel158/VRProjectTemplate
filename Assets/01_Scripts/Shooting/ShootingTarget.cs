using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// A paper circular shooting target with scoring rings.
/// Points are calculated dynamically based on hit distance to center.
/// </summary>
public class ShootingTarget : MonoBehaviour
{
    // ─── Config ───────────────────────────────────────────────────────────────

    [Header("Identity")]
    public string targetLabel = "Target";

    [Header("Scoring Rings")]
    [Tooltip("Radius (in world units) of the bullseye zone → 10 pts")]
    public float bullseyeRadius = 0.05f;
    [Tooltip("Outer radius of the full target face → 2 pts at edge")]
    public float targetRadius   = 0.25f;

    [Header("Visual Feedback")]
    public Color normalColor   = Color.white;
    public Color hitFlashColor = new Color(1f, 0.85f, 0.1f);
    public float flashDuration = 0.12f;

    [Header("Physical Reaction")]
    [Tooltip("How much the target wobbles when hit (degrees)")]
    public float wobbleAngle    = 20f;
    public float wobbleDuration = 1.0f;
    public float wobbleSpeed    = 14f;

    [Header("Events")]
    public UnityEvent<int> onHit;   // passes points scored

    // ─── Private ──────────────────────────────────────────────────────────────

    private Renderer   rend;
    private Material   matInstance;
    private Quaternion initialRotation;
    private bool       isCooldown = false;
    private Coroutine  wobbleCoroutine;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        initialRotation = transform.localRotation;
        rend = GetComponentInChildren<Renderer>();
        if (rend != null)
            matInstance = rend.material;   // get instance so we don't affect shared mat
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Called by VRRifle when a raycast hits this target.
    /// hitWorldPoint is the world-space impact position.
    /// </summary>
    public void Hit(Vector3 hitWorldPoint, bool unused = false)
    {
        if (isCooldown) return;

        int points = CalculatePoints(hitWorldPoint);
        ProcessHit(hitWorldPoint, points);
    }

    public void ResetTarget()
    {
        if (wobbleCoroutine != null) StopCoroutine(wobbleCoroutine);
        transform.localRotation = initialRotation;
        isCooldown = false;
        if (matInstance != null) matInstance.color = normalColor;
    }

    // ─── Internal ─────────────────────────────────────────────────────────────

    private int CalculatePoints(Vector3 hitWorldPoint)
    {
        // Distance from hit to the center of the target face
        float dist = Vector3.Distance(hitWorldPoint, transform.position);
        float t    = Mathf.Clamp01(dist / targetRadius);

        // Lerp from 10 (bullseye) down to 2 (edge)
        // Snap to ring values: 10, 8, 6, 4, 2
        if (dist <= bullseyeRadius)           return 10;
        if (dist <= targetRadius * 0.33f)     return 8;
        if (dist <= targetRadius * 0.55f)     return 6;
        if (dist <= targetRadius * 0.75f)     return 4;
        return 2;
    }

    private void ProcessHit(Vector3 hitPoint, int points)
    {
        isCooldown = true;

        // Notify manager
        if (ShootingRangeManager.Instance != null)
            ShootingRangeManager.Instance.RegisterHit(points, targetLabel);

        // Visual flash
        if (matInstance != null)
            StartCoroutine(FlashRoutine());

        // Physical wobble
        if (wobbleCoroutine != null) StopCoroutine(wobbleCoroutine);
        wobbleCoroutine = StartCoroutine(WobbleRoutine());

        // SpawnBulletHole(hitPoint);   // future feature

        onHit?.Invoke(points);

        StartCoroutine(ResetCooldownRoutine(0.15f));
    }

    private IEnumerator FlashRoutine()
    {
        matInstance.color = hitFlashColor;
        yield return new WaitForSeconds(flashDuration);
        matInstance.color = normalColor;
    }

    private IEnumerator WobbleRoutine()
    {
        float elapsed = 0f;
        while (elapsed < wobbleDuration)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Sin(elapsed * wobbleSpeed) * wobbleAngle * (1f - elapsed / wobbleDuration);
            transform.localRotation = initialRotation * Quaternion.Euler(angle, 0f, 0f);
            yield return null;
        }
        transform.localRotation = initialRotation;
        wobbleCoroutine = null;
    }

    private IEnumerator ResetCooldownRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        isCooldown = false;
    }

    // ─── Gizmo ────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        // Bullseye ring
        Gizmos.color = Color.red;
        DrawCircleGizmo(transform.position, transform.forward, bullseyeRadius, 32);

        // Outer ring
        Gizmos.color = Color.yellow;
        DrawCircleGizmo(transform.position, transform.forward, targetRadius, 32);
    }

    private static void DrawCircleGizmo(Vector3 center, Vector3 normal, float radius, int segments)
    {
        Vector3 right = Vector3.Cross(normal, Vector3.up).normalized;
        if (right.sqrMagnitude < 0.01f) right = Vector3.Cross(normal, Vector3.right).normalized;
        Vector3 up = Vector3.Cross(right, normal).normalized;

        Vector3 prev = center + right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            Vector3 next = center + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
