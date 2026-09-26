using UnityEngine;

namespace Golf.Gameplay
{
    /// <summary>
    /// Componente de deteccion de caida al agua o fuera de pista.
    /// Al entrar en contacto con una pelota de golf (GolfBall), activa el evento OutOfBounds.
    /// </summary>
    public class OutOfBoundsTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            GolfBall ball = other.GetComponent<GolfBall>();
            if (ball == null) ball = other.GetComponentInParent<GolfBall>();
            if (ball != null)
            {
                ball.TriggerOutOfBounds();
            }
        }
    }
}
