using UnityEngine;

public class ShootingRangeManager : MonoBehaviour
{
    public static ShootingRangeManager Instance { get; private set; }

    [Header("Configuración del Campo")]
    [Tooltip("Todas las dianas activas en el campo de tiro")]
    public ShootingTarget[] targets;

    [Header("Estadísticas")]
    [SerializeField] private int totalHits;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (targets == null || targets.Length == 0)
        {
            targets = FindObjectsByType<ShootingTarget>(FindObjectsSortMode.None);
        }
    }

    private void Start()
    {
        ResetAllTargets();
    }

    public void OnTargetHitRegistered(ShootingTarget target)
    {
        totalHits++;
    }

    public void ResetAllTargets()
    {
        totalHits = 0;
        if (targets != null)
        {
            foreach (var target in targets)
            {
                if (target != null)
                {
                    target.ResetTarget();
                }
            }
        }
    }
}
