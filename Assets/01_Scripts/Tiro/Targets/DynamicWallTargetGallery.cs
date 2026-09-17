using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Tiro.Targets
{
    /// <summary>
    /// Galería de Tiro Dinámico en Pared Cercana (8 metros) con dianas reactivas aleatorias.
    /// Aumenta la velocidad y dificultad progresivamente a medida que transcurren los 60 segundos de la ronda.
    /// </summary>
    public class DynamicWallTargetGallery : MonoBehaviour
    {
        [Header("Grid Layout")]
        [SerializeField] private int rows = 3;
        [SerializeField] private int columns = 4;
        [SerializeField] private Vector2 spacing = new Vector2(0.9f, 0.7f);

        [Header("Materials")]
        [SerializeField] private Material matWall;
        [SerializeField] private Material matTargetActive;
        [SerializeField] private Material matTargetIdle;
        [SerializeField] private Material matBullseye;

        [Header("Timing & Difficulty")]
        [SerializeField] private float roundDurationSeconds = 60f;
        [SerializeField] private float initialTargetDuration = 2.4f;
        [SerializeField] private float finalTargetDuration = 0.85f;

        private List<DynamicTargetNode> nodes = new List<DynamicTargetNode>();
        private Coroutine roundCoroutine;
        private bool isRoundRunning = false;
        private int roundScore = 0;
        private int targetsHit = 0;
        private int targetsSpawned = 0;
        private float timeRemaining = 60f;

        public bool IsRunning => isRoundRunning;
        public int RoundScore => roundScore;
        public float TimeRemaining => timeRemaining;

        public event Action<int, int> OnScoreChanged; // (currentScore, combo)
        public event Action<int, bool, Vector3> OnTargetHit; // (score, isBullseye, hitPos)
        public event Action<int, int, int> OnRoundFinished; // (finalScore, hits, total)
        public event Action<float> OnTimerTick; // (seconds)

        private void Awake()
        {
            BuildTargetNodes();
        }

        public void BuildTargetNodes()
        {
            if (nodes.Count > 0) return;

            float startX = -((columns - 1) * spacing.x) * 0.5f;
            float startY = -((rows - 1) * spacing.y) * 0.5f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Vector3 localPos = new Vector3(startX + c * spacing.x, startY + r * spacing.y, -0.04f);

                    GameObject nodeObj = new GameObject($"TargetNode_R{r}_C{c}");
                    nodeObj.transform.SetParent(transform, false);
                    nodeObj.transform.localPosition = localPos;

                    // Base circular de la diana
                    GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    disc.name = "TargetDisc";
                    disc.transform.SetParent(nodeObj.transform, false);
                    disc.transform.localPosition = Vector3.zero;
                    disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    disc.transform.localScale = new Vector3(0.35f, 0.015f, 0.35f);

                    var col = disc.GetComponent<Collider>();
                    if (col != null) Destroy(col);

                    // Centro Bullseye
                    GameObject bullseye = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    bullseye.name = "BullseyeDisc";
                    bullseye.transform.SetParent(disc.transform, false);
                    bullseye.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                    bullseye.transform.localScale = new Vector3(0.35f, 0.2f, 0.35f);
                    var bCol = bullseye.GetComponent<Collider>();
                    if (bCol != null) Destroy(bCol);

                    // BoxCollider para raycast de impacto
                    var hitCol = nodeObj.AddComponent<BoxCollider>();
                    hitCol.size = new Vector3(0.38f, 0.38f, 0.08f);

                    var node = nodeObj.AddComponent<DynamicTargetNode>();
                    node.Initialize(this, disc.GetComponent<Renderer>(), bullseye.GetComponent<Renderer>(), matTargetActive, matTargetIdle, matBullseye);

                    nodes.Add(node);
                    node.SetDeactivated();
                }
            }
        }

        public void StartGalleryRound()
        {
            StopGalleryRound();
            roundCoroutine = StartCoroutine(GalleryRoundRoutine());
        }

        public void StopGalleryRound()
        {
            if (roundCoroutine != null)
            {
                StopCoroutine(roundCoroutine);
                roundCoroutine = null;
            }
            isRoundRunning = false;

            foreach (var n in nodes)
            {
                n.SetDeactivated();
            }
        }

        private IEnumerator GalleryRoundRoutine()
        {
            isRoundRunning = true;
            roundScore = 0;
            targetsHit = 0;
            targetsSpawned = 0;
            timeRemaining = roundDurationSeconds;

            yield return new WaitForSeconds(0.8f);

            while (timeRemaining > 0f && isRoundRunning)
            {
                timeRemaining -= Time.deltaTime;
                OnTimerTick?.Invoke(Mathf.Max(0f, timeRemaining));

                // Escalar dificultad según el tiempo transcurrido (0 a 1)
                float progress = 1f - (timeRemaining / roundDurationSeconds);
                float currentExposure = Mathf.Lerp(initialTargetDuration, finalTargetDuration, progress);
                float interval = Mathf.Lerp(1.6f, 0.65f, progress);

                // Activar una diana libre aleatoria
                var availableNodes = nodes.FindAll(n => !n.IsActive);
                if (availableNodes.Count > 0)
                {
                    var chosen = availableNodes[UnityEngine.Random.Range(0, availableNodes.Count)];
                    chosen.ActivateTarget(currentExposure);
                    targetsSpawned++;
                }

                yield return new WaitForSeconds(interval);
            }

            isRoundRunning = false;
            OnRoundFinished?.Invoke(roundScore, targetsHit, targetsSpawned);
        }

        public void RegisterNodeHit(DynamicTargetNode node, Vector3 hitPoint)
        {
            if (!isRoundRunning || !node.IsActive) return;

            // Calcular si dio en el centro Bullseye
            Vector3 localHit = node.transform.InverseTransformPoint(hitPoint);
            float distFromCenter = new Vector2(localHit.x, localHit.y).magnitude;

            bool isBullseye = distFromCenter < 0.08f;
            int points = isBullseye ? 10 : 7;

            roundScore += points;
            targetsHit++;

            node.OnHitSuccess();

            OnScoreChanged?.Invoke(roundScore, targetsHit);
            OnTargetHit?.Invoke(points, isBullseye, hitPoint);
        }
    }

    /// <summary>
    /// Nodo individual de diana emergente.
    /// </summary>
    public class DynamicTargetNode : MonoBehaviour
    {
        private DynamicWallTargetGallery gallery;
        private Renderer discRenderer;
        private Renderer bullseyeRenderer;
        private Material matActive;
        private Material matIdle;
        private Material matBullseye;

        private Coroutine activeRoutine;
        private bool isActive = false;

        public bool IsActive => isActive;

        public void Initialize(DynamicWallTargetGallery g, Renderer disc, Renderer bullseye, Material active, Material idle, Material bMat)
        {
            gallery = g;
            discRenderer = disc;
            bullseyeRenderer = bullseye;
            matActive = active;
            matIdle = idle;
            matBullseye = bMat;
        }

        public void ActivateTarget(float duration)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(ActiveLifeRoutine(duration));
        }

        private IEnumerator ActiveLifeRoutine(float duration)
        {
            isActive = true;
            ApplyVisualState(true);

            // Efecto de pop-up hacia adelante
            Vector3 startPos = transform.localPosition;
            Vector3 forwardPos = new Vector3(startPos.x, startPos.y, -0.15f);

            float t = 0f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(startPos, forwardPos, t / 0.12f);
                yield return null;
            }
            transform.localPosition = forwardPos;

            yield return new WaitForSeconds(duration);

            // Retracción automática si no fue impactada
            SetDeactivated();
        }

        public void OnHitSuccess()
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            StartCoroutine(FlashAndHideRoutine());
        }

        private IEnumerator FlashAndHideRoutine()
        {
            isActive = false;
            // Destello dorado de impacto
            if (discRenderer != null && matBullseye != null) discRenderer.material = matBullseye;
            yield return new WaitForSeconds(0.08f);
            SetDeactivated();
        }

        public void SetDeactivated()
        {
            isActive = false;
            ApplyVisualState(false);
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, -0.02f);
        }

        private void ApplyVisualState(bool active)
        {
            if (discRenderer != null)
            {
                discRenderer.material = active ? (matActive != null ? matActive : discRenderer.material) : (matIdle != null ? matIdle : discRenderer.material);
            }
            if (bullseyeRenderer != null)
            {
                bullseyeRenderer.material = active ? (matBullseye != null ? matBullseye : bullseyeRenderer.material) : (matIdle != null ? matIdle : bullseyeRenderer.material);
            }
        }
    }
}
