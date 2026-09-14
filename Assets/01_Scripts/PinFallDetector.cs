using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PinFallDetector : MonoBehaviour
{
    public float fallAngleThreshold = 45f;

    public bool HasFallen { get; private set; }

    private void FixedUpdate()
    {
        if (HasFallen) return;

        float tiltAngle = Vector3.Angle(transform.forward, Vector3.up);

        if (tiltAngle >= fallAngleThreshold)
        {
            HasFallen = true;

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
}