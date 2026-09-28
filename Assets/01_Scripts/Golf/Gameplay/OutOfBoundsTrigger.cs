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
                // La superficie de los hoyos se encuentra en Y >= 0.05m.
                // Solo debe considerarse fuera de pista si la bola realmente cayó al agua/vacío (Y < -0.10m).
                if (ball.transform.position.y > -0.10f)
                {
                    return;
                }

                ball.TriggerOutOfBounds();
            }
        }
    }
}
