using UnityEngine;

namespace Esgrima.Data
{
    [CreateAssetMenu(fileName = "NewMatchConfig", menuName = "VR Sports/Esgrima/Match Config")]
    public class EsgrimaMatchConfigSO : ScriptableObject
    {
        [Header("Match Rules")]
        [Tooltip("Rondas ganadas necesarias para ganar el duelo completo (estilo Wii Sports: al mejor de 3 o 5).")]
        public int roundsToWin = 3;

        [Tooltip("Radio de la plataforma circular (ring) en metros.")]
        public float ringRadius = 3.5f;

        [Tooltip("Altura Y por debajo de la cual se considera caída total del ring.")]
        public float fallYThreshold = -0.8f;

        [Tooltip("Tiempo de pausa entre rondas para resetear a los luchadores.")]
        public float roundResetDelay = 2.0f;
    }
}
