using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum TargetType
{
    Close,
    Medium,
    Far,
    Bullseye
}

public enum TargetReaction
{
    Wobble,     // Se balancea al recibir impacto y regresa al centro
    Knockdown,  // Cae hacia atrás y se levanta tras un tiempo
    FlashOnly   // Solo destello de color
}

public class ShootingTarget : MonoBehaviour
{
    [Header("Configuración de Diana")]
    [Tooltip("Tipo/distancia de la diana")]
    public TargetType targetType = TargetType.Close;

    [Tooltip("Evento de puntuación que se enviará a ScoreManager")]
    public ScoreEventData scoreEventData;

    [Tooltip("Puntos de bonificación si es tiro perfecto en el centro")]
    public ScoreEventData bullseyeEventData;

    [Header("Comportamiento Físico")]
    public TargetReaction reaction = TargetReaction.Wobble;
    public float resetTime = 3f;
    public float wobbleIntensity = 25f;
    public float wobbleSpeed = 12f;

    [Header("Feedback")]
    public MeshRenderer targetRenderer;
    public Color hitColor = Color.yellow;
    public AudioClip hitAudioClip;
    public ParticleSystem hitParticles;

    [Header("Filtros de Impacto")]
    public string[] validTags = new string[] { "Projectile", "Bullet", "BlasterProjectile", "Basketball", "Interactable" };

    [Header("Eventos")]
    public UnityEvent onTargetHit;

    private Quaternion initialRotation;
    private Color originalColor;
    private Material targetMaterialInstance;
    private bool isKnockedDown = false;
    private bool isHitCooldown = false;
    private Coroutine currentReactionCoroutine;

    private void Awake()
    {
        initialRotation = transform.localRotation;
        if (targetRenderer != null)
        {
            targetMaterialInstance = targetRenderer.material;
            originalColor = targetMaterialInstance.color;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isHitCooldown || isKnockedDown) return;

        if (IsValidHit(collision.gameObject))
        {
            Vector3 contactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;
            ProcessHit(contactPoint);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isHitCooldown || isKnockedDown) return;

        if (IsValidHit(other.gameObject))
        {
            ProcessHit(other.transform.position);
        }
    }

    public void Hit(Vector3 hitWorldPoint, bool isCenterBullseye = false)
    {
        if (isHitCooldown || isKnockedDown) return;
        ProcessHit(hitWorldPoint, isCenterBullseye);
    }

    private bool IsValidHit(GameObject obj)
    {
        if (validTags == null || validTags.Length == 0) return true;

        foreach (string tag in validTags)
        {
            if (obj.CompareTag(tag)) return true;
        }

        // Si el objeto tiene un Rigidbody con cierta velocidad también cuenta
        Rigidbody rb = obj.GetComponentInParent<Rigidbody>();
        if (rb != null && rb.linearVelocity.magnitude > 1.5f) return true;

        return false;
    }

    private void ProcessHit(Vector3 hitPoint, bool isCenterBullseye = false)
    {
        isHitCooldown = true;
        StartCoroutine(HitCooldownRoutine(0.2f));

        // Registrar puntuación en ScoreManager
        ScoreEventData eventToRegister = isCenterBullseye && bullseyeEventData != null ? bullseyeEventData : scoreEventData;
        if (eventToRegister != null && ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterScoreEvent(eventToRegister);
        }

        // Feedback sonoro
        if (hitAudioClip != null)
        {
            AudioSource.PlayClipAtPoint(hitAudioClip, hitPoint);
        }

        // Partículas
        if (hitParticles != null)
        {
            hitParticles.transform.position = hitPoint;
            hitParticles.Play();
        }

        // Destello visual
        if (targetMaterialInstance != null)
        {
            StartCoroutine(FlashMaterialRoutine());
        }

        // Reacción física/animada
        if (currentReactionCoroutine != null)
        {
            StopCoroutine(currentReactionCoroutine);
        }

        switch (reaction)
        {
            case TargetReaction.Wobble:
                currentReactionCoroutine = StartCoroutine(WobbleRoutine());
                break;
            case TargetReaction.Knockdown:
                currentReactionCoroutine = StartCoroutine(KnockdownRoutine());
                break;
        }

        onTargetHit?.Invoke();
    }

    private IEnumerator FlashMaterialRoutine()
    {
        targetMaterialInstance.color = hitColor;
        yield return new WaitForSeconds(0.15f);
        targetMaterialInstance.color = originalColor;
    }

    private IEnumerator WobbleRoutine()
    {
        float elapsed = 0f;
        float duration = 1.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Sin(elapsed * wobbleSpeed) * wobbleIntensity * (1f - (elapsed / duration));
            transform.localRotation = initialRotation * Quaternion.Euler(angle, 0, 0);
            yield return null;
        }

        transform.localRotation = initialRotation;
        currentReactionCoroutine = null;
    }

    private IEnumerator KnockdownRoutine()
    {
        isKnockedDown = true;
        Quaternion downRotation = initialRotation * Quaternion.Euler(85f, 0, 0);

        // Caer
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(initialRotation, downRotation, elapsed / 0.15f);
            yield return null;
        }
        transform.localRotation = downRotation;

        // Esperar tiempo de reseteo
        yield return new WaitForSeconds(resetTime);

        // Levantarse
        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            transform.localRotation = Quaternion.Slerp(downRotation, initialRotation, elapsed / 0.3f);
            yield return null;
        }
        transform.localRotation = initialRotation;
        isKnockedDown = false;
        currentReactionCoroutine = null;
    }

    private IEnumerator HitCooldownRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        isHitCooldown = false;
    }

    public void ResetTarget()
    {
        if (currentReactionCoroutine != null)
        {
            StopCoroutine(currentReactionCoroutine);
        }
        transform.localRotation = initialRotation;
        isKnockedDown = false;
        isHitCooldown = false;
        if (targetMaterialInstance != null)
        {
            targetMaterialInstance.color = originalColor;
        }
    }
}
