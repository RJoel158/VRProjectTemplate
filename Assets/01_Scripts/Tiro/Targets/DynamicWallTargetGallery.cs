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

        [SerializeField] private List<DynamicTargetNode> nodes = new List<DynamicTargetNode>();
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
            SyncTargetNodes();
        }

        private void OnEnable()
        {
            SyncTargetNodes();
        }

        public void SyncTargetNodes()
        {
            var existingNodes = GetComponentsInChildren<DynamicTargetNode>(true);
            if (existingNodes.Length > 0)
            {
                nodes.Clear();
                nodes.AddRange(existingNodes);
                foreach (var n in nodes)
                {
                    if (n != null)
                    {
                        n.CacheComponents();
                        n.RefreshMaterials(matTargetActive, matTargetIdle, matBullseye);
                    }
                }
            }
            else
            {
                BuildTargetNodes();
            }
        }

        public void BuildTargetNodes()
        {
            nodes.Clear();
            var oldNodes = GetComponentsInChildren<DynamicTargetNode>(true);
            foreach (var on in oldNodes)
            {
                if (on != null) DestroyImmediate(on.gameObject);
            }

            float startX = -((columns - 1) * spacing.x) * 0.5f;
            float startY = -((rows - 1) * spacing.y) * 0.5f;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < columns; c++)
                {
                    Vector3 localPos = new Vector3(startX + c * spacing.x, startY + r * spacing.y, -0.015f);

                    GameObject nodeObj = new GameObject($"TargetNode_R{r}_C{c}");
                    nodeObj.transform.SetParent(transform, false);
                    nodeObj.transform.localPosition = localPos;

                    // Base circular de la diana (disco exterior de 38cm)
                    GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    disc.name = "TargetDisc";
                    disc.transform.SetParent(nodeObj.transform, false);
                    disc.transform.localPosition = Vector3.zero;
                    disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    disc.transform.localScale = new Vector3(0.38f, 0.012f, 0.38f);
                    var col = disc.GetComponent<Collider>();
                    if (col != null) DestroyImmediate(col);

                    // Anillo de contraste exterior
                    GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    ring.name = "ContrastRing";
                    ring.transform.SetParent(disc.transform, false);
                    ring.transform.localPosition = new Vector3(0f, 0.55f, 0f);
                    ring.transform.localScale = new Vector3(0.65f, 0.15f, 0.65f);
                    var rCol = ring.GetComponent<Collider>();
                    if (rCol != null) DestroyImmediate(rCol);

                    // Centro Bullseye de 12cm
                    GameObject bullseye = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    bullseye.name = "BullseyeDisc";
                    bullseye.transform.SetParent(disc.transform, false);
                    bullseye.transform.localPosition = new Vector3(0f, 0.75f, 0f);
                    bullseye.transform.localScale = new Vector3(0.28f, 0.25f, 0.28f);
                    var bCol = bullseye.GetComponent<Collider>();
                    if (bCol != null) DestroyImmediate(bCol);

                    // BoxCollider para raycast de impacto
                    var hitCol = nodeObj.AddComponent<BoxCollider>();
                    hitCol.size = new Vector3(0.40f, 0.40f, 0.10f);

                    var node = nodeObj.AddComponent<DynamicTargetNode>();
                    node.Initialize(this, disc.GetComponent<Renderer>(), ring.GetComponent<Renderer>(), bullseye.GetComponent<Renderer>(), matTargetActive, matTargetIdle, matBullseye);

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
                if (n != null) n.SetDeactivated();
            }
        }

        private IEnumerator GalleryRoundRoutine()
        {
            isRoundRunning = true;
            roundScore = 0;
            targetsHit = 0;
            targetsSpawned = 0;
            timeRemaining = roundDurationSeconds;

            // Asegurar que todos inicien desactivados
            foreach (var n in nodes)
            {
                if (n != null) n.SetDeactivated();
            }

            yield return new WaitForSeconds(0.4f);

            float nextSpawnTime = 0f;

            while (timeRemaining > 0f && isRoundRunning)
            {
                timeRemaining -= Time.deltaTime;
                OnTimerTick?.Invoke(Mathf.Max(0f, timeRemaining));

                // Progreso de la ronda (0 a 1)
                float progress = 1f - Mathf.Clamp01(timeRemaining / roundDurationSeconds);

                // Dificultad escalada:
                // Primeros ~25 segundos: 1 diana activa a la vez para máxima claridad
                // Segundos 25 a 60: 2 dianas simultáneas
                int targetActiveCount = progress > 0.40f ? 2 : 1;

                // Duración de la diana activa antes de retraerse
                float currentExposure = Mathf.Lerp(initialTargetDuration, finalTargetDuration, progress);
                float interval = Mathf.Lerp(1.5f, 0.65f, progress);

                // Contar cuántas están activas actualmente
                int activeCount = 0;
                for (int i = 0; i < nodes.Count; i++)
                {
                    if (nodes[i] != null && nodes[i].IsActive) activeCount++;
                }

                // Activar nueva diana si hay cupo disponible
                if (activeCount < targetActiveCount && (Time.time >= nextSpawnTime || activeCount == 0))
                {
                    nextSpawnTime = Time.time + interval;

                    var availableNodes = nodes.FindAll(n => n != null && !n.IsActive);
                    if (availableNodes.Count > 0)
                    {
                        var chosen = availableNodes[UnityEngine.Random.Range(0, availableNodes.Count)];
                        chosen.ActivateTarget(currentExposure);
                        targetsSpawned++;
                    }
                }

                yield return null;
            }

            isRoundRunning = false;
            foreach (var n in nodes)
            {
                if (n != null) n.SetDeactivated();
            }
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
    /// Nodo individual de diana emergente ultrarreactiva.
    /// En estado inactivo: gris apagado al ras de la pared.
    /// En estado activo: salta hacia adelante e ilumina en verde neón radiante con centro rojo carmesí.
    /// </summary>
    public class DynamicTargetNode : MonoBehaviour
    {
        [SerializeField] private DynamicWallTargetGallery gallery;
        [SerializeField] private Renderer discRenderer;
        [SerializeField] private Renderer ringRenderer;
        [SerializeField] private Renderer bullseyeRenderer;
        [SerializeField] private Material matActive;
        [SerializeField] private Material matIdle;
        [SerializeField] private Material matBullseye;
        [SerializeField] private Vector3 baseLocalPos;

        private Coroutine activeRoutine;
        private bool isActive = false;
        private static MaterialPropertyBlock mpb;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        public bool IsActive => isActive;

        private void Awake()
        {
            CacheComponents();
        }

        public void CacheComponents()
        {
            if (gallery == null) gallery = GetComponentInParent<DynamicWallTargetGallery>();

            if (discRenderer == null)
            {
                var disc = transform.Find("TargetDisc");
                if (disc != null) discRenderer = disc.GetComponent<Renderer>();
            }
            if (ringRenderer == null)
            {
                var ring = transform.Find("TargetDisc/ContrastRing");
                if (ring != null) ringRenderer = ring.GetComponent<Renderer>();
            }
            if (bullseyeRenderer == null)
            {
                var bullseye = transform.Find("TargetDisc/BullseyeDisc");
                if (bullseye != null) bullseyeRenderer = bullseye.GetComponent<Renderer>();
            }

            if (baseLocalPos == Vector3.zero)
            {
                baseLocalPos = new Vector3(transform.localPosition.x, transform.localPosition.y, -0.015f);
            }
        }

        public void Initialize(DynamicWallTargetGallery g, Renderer disc, Renderer ring, Renderer bullseye, Material active, Material idle, Material bMat)
        {
            gallery = g;
            discRenderer = disc;
            ringRenderer = ring;
            bullseyeRenderer = bullseye;
            matActive = active;
            matIdle = idle;
            matBullseye = bMat;
            baseLocalPos = new Vector3(transform.localPosition.x, transform.localPosition.y, -0.015f);
        }

        public void RefreshMaterials(Material active, Material idle, Material bMat)
        {
            matActive = active;
            matIdle = idle;
            matBullseye = bMat;
            CacheComponents();
            if (!isActive) SetDeactivated();
        }

        public void ActivateTarget(float duration)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(ActiveLifeRoutine(duration));
        }

        private IEnumerator ActiveLifeRoutine(float duration)
        {
            isActive = true;
            CacheComponents();
            ApplyVisualState(true);

            // Efecto de pop-up hacia adelante (emerge 18cm hacia el tirador)
            Vector3 startPos = baseLocalPos;
            Vector3 forwardPos = new Vector3(baseLocalPos.x, baseLocalPos.y, -0.18f);

            float t = 0f;
            while (t < 0.09f)
            {
                t += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(startPos, forwardPos, t / 0.09f);
                yield return null;
            }
            transform.localPosition = forwardPos;

            yield return new WaitForSeconds(duration);

            // Retracción automática si no fue impactada a tiempo
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
            // Destello dorado de impacto certero
            if (mpb == null) mpb = new MaterialPropertyBlock();
            Color goldHit = new Color(1f, 0.90f, 0.15f);
            mpb.Clear();
            mpb.SetColor(BaseColorId, goldHit);
            mpb.SetColor(ColorId, goldHit);
            mpb.SetColor(EmissionColorId, goldHit * 2.5f);

            if (discRenderer != null) discRenderer.SetPropertyBlock(mpb);
            if (ringRenderer != null) ringRenderer.SetPropertyBlock(mpb);
            if (bullseyeRenderer != null) bullseyeRenderer.SetPropertyBlock(mpb);

            yield return new WaitForSeconds(0.08f);
            SetDeactivated();
        }

        public void SetDeactivated()
        {
            if (activeRoutine != null)
            {
                StopCoroutine(activeRoutine);
                activeRoutine = null;
            }
            isActive = false;
            CacheComponents();
            ApplyVisualState(false);
            transform.localPosition = baseLocalPos;
        }

        private void ApplyVisualState(bool active)
        {
            CacheComponents();
            if (mpb == null) mpb = new MaterialPropertyBlock();

            if (active)
            {
                // --- DIANA ACTIVA: VÍVIDA, ILUMINADA Y NEÓN DE ALTA VISIBILIDAD ---
                if (discRenderer != null)
                {
                    Color neonGreen = new Color(0.12f, 1f, 0.28f);
                    mpb.Clear();
                    mpb.SetColor(BaseColorId, neonGreen);
                    mpb.SetColor(ColorId, neonGreen);
                    mpb.SetColor(EmissionColorId, neonGreen * 1.8f);
                    discRenderer.SetPropertyBlock(mpb);
                }
                if (ringRenderer != null)
                {
                    Color brightWhite = Color.white;
                    mpb.Clear();
                    mpb.SetColor(BaseColorId, brightWhite);
                    mpb.SetColor(ColorId, brightWhite);
                    mpb.SetColor(EmissionColorId, brightWhite * 1.2f);
                    ringRenderer.SetPropertyBlock(mpb);
                }
                if (bullseyeRenderer != null)
                {
                    Color hotCrimson = new Color(1f, 0.15f, 0.05f);
                    mpb.Clear();
                    mpb.SetColor(BaseColorId, hotCrimson);
                    mpb.SetColor(ColorId, hotCrimson);
                    mpb.SetColor(EmissionColorId, hotCrimson * 2.2f);
                    bullseyeRenderer.SetPropertyBlock(mpb);
                }
            }
            else
            {
                // --- DIANA INACTIVA: GRIS OSCURO APAGADO (REPOSO DISCRETO) ---
                if (discRenderer != null)
                {
                    Color idleDisc = new Color(0.18f, 0.19f, 0.21f);
                    mpb.Clear();
                    mpb.SetColor(BaseColorId, idleDisc);
                    mpb.SetColor(ColorId, idleDisc);
                    mpb.SetColor(EmissionColorId, Color.black);
                    discRenderer.SetPropertyBlock(mpb);
                }
                if (ringRenderer != null)
                {
                    Color idleRing = new Color(0.24f, 0.25f, 0.28f);
                    mpb.Clear();
                    mpb.SetColor(BaseColorId, idleRing);
                    mpb.SetColor(ColorId, idleRing);
                    mpb.SetColor(EmissionColorId, Color.black);
                    ringRenderer.SetPropertyBlock(mpb);
                }
                if (bullseyeRenderer != null)
                {
                    Color idleBull = new Color(0.14f, 0.15f, 0.17f);
                    mpb.Clear();
                    mpb.SetColor(BaseColorId, idleBull);
                    mpb.SetColor(ColorId, idleBull);
                    mpb.SetColor(EmissionColorId, Color.black);
                    bullseyeRenderer.SetPropertyBlock(mpb);
                }
            }
        }
    }
}

