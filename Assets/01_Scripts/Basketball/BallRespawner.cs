using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


[RequireComponent(typeof(Rigidbody))]
public class BallRespawner : MonoBehaviour
{
    [Tooltip("How slow the ball must be moving to count as 'settled'.")]
    public float velocityThreshold = 0.1f;

    [Tooltip("How many seconds it must stay settled before respawning.")]
    public float settleTimeBeforeRespawn = 2.5f;

    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private float settledTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();

        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void Update()
    {
        // Never respawn while the player is holding the ball.
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            settledTimer = 0f;
            return;
        }

        bool isSettled = rb.linearVelocity.magnitude < velocityThreshold &&
                          rb.angularVelocity.magnitude < velocityThreshold;

        if (isSettled)
        {
            settledTimer += Time.deltaTime;

            if (settledTimer >= settleTimeBeforeRespawn)
            {
                RespawnBall();
            }
        }
        else
        {
            settledTimer = 0f;
        }
    }

    private void RespawnBall()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = startPosition;
        transform.rotation = startRotation;
        settledTimer = 0f;
    }
}