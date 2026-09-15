using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Manages the shooting range session: tracks shots fired, hits, and score.
/// Displays a lightweight HUD with live stats.
/// </summary>
public class ShootingRangeManager : MonoBehaviour
{
    public static ShootingRangeManager Instance { get; private set; }

    // ─── References ──────────────────────────────────────────────────────────

    [Header("Targets (auto-populated if empty)")]
    public List<ShootingTarget> targets = new List<ShootingTarget>();

    [Header("UI (auto-created if null)")]
    public Canvas   hudCanvas;
    public TMP_Text scoreText;
    public TMP_Text shotsText;
    public TMP_Text accuracyText;
    public TMP_Text lastHitText;

    // ─── Runtime State ────────────────────────────────────────────────────────

    private int totalScore = 0;
    private int shotsFired = 0;
    private int shotsHit   = 0;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (targets.Count == 0)
            targets.AddRange(FindObjectsByType<ShootingTarget>(FindObjectsInactive.Exclude));

        if (hudCanvas == null)
            BuildHUD();

        RefreshHUD();
    }

    private void Update()
    {
        // Shift + R = reset session
        if (Keyboard.current != null
            && Keyboard.current.rKey.wasPressedThisFrame
            && Keyboard.current.leftShiftKey.isPressed)
        {
            ResetSession();
        }
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    public void RegisterHit(int points, string targetLabel)
    {
        shotsHit++;
        totalScore += points;
        RefreshHUD(targetLabel, points);
    }

    public void RegisterShot()
    {
        shotsFired++;
        RefreshHUD();
    }

    public void ResetSession()
    {
        totalScore = 0;
        shotsFired = 0;
        shotsHit   = 0;
        foreach (var t in targets) if (t != null) t.ResetTarget();
        if (ScoreManager.Instance != null) ScoreManager.Instance.ResetScore();
        RefreshHUD("--- RESET ---", 0);
    }

    // ─── HUD ──────────────────────────────────────────────────────────────────

    private void RefreshHUD(string hitLabel = null, int hitPoints = 0)
    {
        float acc = shotsFired > 0 ? (shotsHit / (float)shotsFired) * 100f : 0f;

        if (scoreText    != null) scoreText.text    = $"SCORE\n{totalScore}";
        if (shotsText    != null) shotsText.text    = $"DISPAROS\n{shotsFired}";
        if (accuracyText != null) accuracyText.text = $"PRECISION\n{acc:0}%";

        if (lastHitText != null && !string.IsNullOrEmpty(hitLabel))
        {
            if (hitLabel == "--- RESET ---")
                lastHitText.text = "✓ Sesion reiniciada";
            else
                lastHitText.text = $"+{hitPoints} pts   {hitLabel}";
        }
    }

    private void BuildHUD()
    {
        // Root canvas
        GameObject canvasGO = new GameObject("ShootingHUD");
        canvasGO.transform.SetParent(transform, false);

        hudCanvas = canvasGO.AddComponent<Canvas>();
        hudCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 10;

        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution  = new Vector2(1920, 1080);
        cs.screenMatchMode      = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        cs.matchWidthOrHeight   = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Dark semi-transparent panel – bottom-left corner
        GameObject panel = CreatePanel(canvasGO.transform,
            anchorMin: new Vector2(0, 0), anchorMax: new Vector2(0, 0),
            pivot:     new Vector2(0, 0),
            pos: new Vector2(24, 24), size: new Vector2(420, 120),
            color: new Color(0.04f, 0.04f, 0.04f, 0.72f));

        // Stats row
        scoreText    = MakeLabel(panel.transform, "Score",    new Vector2(12,  -12), new Vector2(130, 55), 16);
        shotsText    = MakeLabel(panel.transform, "Shots",    new Vector2(148, -12), new Vector2(110, 55), 16);
        accuracyText = MakeLabel(panel.transform, "Acc",      new Vector2(264, -12), new Vector2(130, 55), 16);

        // Last hit
        lastHitText  = MakeLabel(panel.transform, "LastHit",  new Vector2(12,  -78), new Vector2(380, 28), 15);
        lastHitText.color = new Color(1f, 0.9f, 0.25f);
        lastHitText.fontStyle = FontStyles.Bold;

        // Hint
        TMP_Text hint = MakeLabel(panel.transform, "Hint",   new Vector2(12, -106), new Vector2(400, 20), 11);
        hint.text  = "Clic Izq = Disparar  |  R = Recargar  |  Shift+R = Resetear";
        hint.color = new Color(0.55f, 0.55f, 0.55f);
    }

    private GameObject CreatePanel(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 pivot, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject("Panel");
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot     = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    private TMP_Text MakeLabel(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TMP_Text t   = go.AddComponent<TextMeshProUGUI>();
        t.fontSize   = fontSize;
        t.color      = Color.white;
        t.alignment  = TextAlignmentOptions.TopLeft;
        t.text       = "";
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot     = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return t;
    }
}
