using UnityEngine;

namespace Esgrima.Data
{
    [CreateAssetMenu(fileName = "NewMatchConfig", menuName = "VR Sports/Esgrima/Match Config")]
    public class EsgrimaMatchConfigSO : ScriptableObject
    {
        [Tooltip("Puntos necesarios para ganar el duelo completo.")]
        public int roundsToWin = 5;

        [Tooltip("Radio de la plataforma circular (ring) en metros.")]
        public float ringRadius = 5.0f;

        [Tooltip("Altura Y por debajo de la cual se considera caída total del ring.")]
        public float fallYThreshold = -0.6f;

        [Tooltip("Tiempo de pausa entre rondas para resetear a los luchadores.")]
        public float roundResetDelay = 2.0f;
    }
}
