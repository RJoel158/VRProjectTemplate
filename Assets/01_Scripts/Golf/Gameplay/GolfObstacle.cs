using UnityEngine;
using Golf.Audio;

namespace Golf.Gameplay
{
    public enum ObstacleType
    {
        RotatingWindmill,
        OscillatingBar,
        BouncePad
    }

    /// <summary>
    /// Maneja obstaculos dinamicos e interactivos en los hoyos de minigolf (estilo Golf It!).
    /// Incluye aspas de molino giratorio, barras oscilantes y bumpers que rebotan con impulso extra.
    /// </summary>
    public class GolfObstacle : MonoBehaviour
    {
        [Header("Obstacle Type")]
        [SerializeField] private ObstacleType type = ObstacleType.RotatingWindmill;

        [Header("Rotation Settings")]
        [SerializeField] private Vector3 rotationAxis = new Vector3(0f, 0f, 1f);
        [SerializeField] private float rotationSpeed = 45f;

        [Header("Oscillation Settings")]
        [SerializeField] private Vector3 moveAxis = Vector3.right;
        [SerializeField] private float moveDistance = 1.2f;
        [SerializeField] private float moveSpeed = 1.5f;

        [Header("Bounce Pad Boost")]
        [SerializeField] private float boostForce = 6f;

        private Vector3 initialPos;

        private void Awake()
        {
            initialPos = transform.position;
        }

        private void Update()
        {
            switch (type)
            {
                case ObstacleType.RotatingWindmill:
                    transform.Rotate(rotationAxis * (rotationSpeed * Time.deltaTime), Space.Self);
                    break;

                case ObstacleType.OscillatingBar:
                    float offset = Mathf.Sin(Time.time * moveSpeed) * moveDistance;
                    transform.position = initialPos + moveAxis * offset;
                    break;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            GolfBall ball = collision.collider.GetComponent<GolfBall>();
            if (ball == null) ball = collision.collider.GetComponentInParent<GolfBall>();

            if (ball != null)
            {
                if (type == ObstacleType.BouncePad)
                {
                    Vector3 normal = collision.contacts.Length > 0 ? collision.contacts[0].normal : -transform.forward;
                    ball.ApplyPutterHit(-normal * boostForce);
                    GolfAudioManager.PlayWoodBounce(2.5f);
                }
                else
                {
                    GolfAudioManager.PlayWoodBounce(1.2f);
                }
            }
        }
    }
}
