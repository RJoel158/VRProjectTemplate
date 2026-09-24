using System;
using UnityEngine;
using Golf.Data;
using Golf.Audio;

namespace Golf.Gameplay
{
    /// <summary>
    /// Representa el hoyo/vaso de minigolf.
    /// Contiene el trigger que detecta la entrada de la pelota con velocidad adecuada,
    /// reproduce el sonido de embocado, y dispara el evento de hoyo completado.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class GolfCup : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private HoleDataSO holeData;

        [Header("Cup Visuals")]
        [Tooltip("Punto central interior en el fondo del hoyo.")]
        [SerializeField] private Transform cupCenter;
        [SerializeField] private GameObject flagObject;

        [Header("Hole In One Effects")]
        [SerializeField] private ParticleSystem confettiParticles;

        public HoleDataSO HoleData => holeData;
        public Vector3 CupPosition => cupCenter != null ? cupCenter.position : transform.position;

        public event Action<GolfCup, GolfBall> OnBallSink;

        private void Awake()
        {
            if (cupCenter == null) cupCenter = transform;

            Collider col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            GolfBall ball = other.GetComponent<GolfBall>();
            if (ball == null) ball = other.GetComponentInParent<GolfBall>();
            if (ball == null) return;

            if (ball.CurrentState == BallState.InCup) return;

            float speed = ball.RigidbodyComponent != null ? ball.RigidbodyComponent.linearVelocity.magnitude : 0f;
            float maxSpeed = holeData != null ? holeData.CupMaxEntrySpeed : 2.8f;

            // Si la velocidad es demasiado alta, la bola saltaria por encima del hoyo
            if (speed > maxSpeed)
            {
                // Pequeno desvio fisico por golpear el borde del hoyo
                return;
            }

            // Embocado exitoso
            ball.TriggerHoleInCup(CupPosition);
            GolfAudioManager.PlayCupSink();

            if (confettiParticles != null)
            {
                confettiParticles.Play();
            }

            OnBallSink?.Invoke(this, ball);
        }

        public void SetHoleData(HoleDataSO data)
        {
            holeData = data;
        }
    }
}
