using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PinFallDetector : MonoBehaviour
{
    public float fallAngleThreshold = 45f;

    [Tooltip("Prefab with SpriteRenderer + FloatingIconEffect, spawned above the pin when it falls.")]
    public GameObject floatingIconPrefab;

    [Tooltip("How far above the pin's position the icon spawns.")]
    public Vector3 iconSpawnOffset = new Vector3(0f, 0.5f, 0f);

    public bool HasFallen { get; private set; }

    private void FixedUpdate()
    {
        if (HasFallen) return;

        float tiltAngle = Vector3.Angle(transform.forward, Vector3.up);

        if (tiltAngle >= fallAngleThreshold)
        {
            HasFallen = true;

            SpawnFloatingIcon();

            BowlingPinManager.Instance.NotifyPinFell(this);
        }
    }

    public void HidePin()
    {
        gameObject.SetActive(false);
    }

    public void ResetPin(Vector3 originalPosition, Quaternion originalRotation)
    {
        gameObject.SetActive(true);

        HasFallen = false;

        Rigidbody rb = GetComponent<Rigidbody>();

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = originalPosition;
        transform.rotation = originalRotation;
    }

    private void SpawnFloatingIcon()
    {
        if (floatingIconPrefab == null) return;
        Instantiate(floatingIconPrefab, transform.position + iconSpawnOffset, Quaternion.identity);
    }
}