using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using TMPro;

public class SportCircuitRuntimeInitializer : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnSceneLoaded()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName == "GolfScene" || sceneName == "ShootingScene")
        {
            EnsureCircuitSetupForScene(sceneName);
        }
    }

    private static void EnsureCircuitSetupForScene(string sceneName)
    {
        // 1. Ensure EventSystem with XRUIInputModule
        var eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var esGo = new GameObject("EventSystem");
            eventSystem = esGo.AddComponent<EventSystem>();
            esGo.AddComponent<XRUIInputModule>();
            Debug.Log($"[{sceneName}] Runtime: Created EventSystem with XRUIInputModule.");
        }
        else
        {
            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule != null)
            {
                Object.Destroy(inputModule);
            }

            if (eventSystem.GetComponent<XRUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<XRUIInputModule>();
            }
        }

        // 2. Ensure ScoreManager
        if (ScoreManager.Instance == null)
        {
            var smGo = new GameObject("ScoreManager");
            smGo.AddComponent<ScoreManager>();
            Debug.Log($"[{sceneName}] Runtime: Created ScoreManager.");
        }

        // 3. Find target Canvas (priorizar Scoreboard_WorldSpace / Whiteboard estandarizada)
        Canvas canvas = null;
        if (sceneName == "ShootingScene")
        {
            canvas = EnsureShootingWhiteboard();
        }
        else
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.name == "Scoreboard_WorldSpace")
                {
                    canvas = c;
                    break;
                }
            }
            if (canvas == null)
            {
                foreach (var c in canvases)
                {
                    if (c.name.Contains("Scoreboard") || c.name.Contains("WorldSpace"))
                    {
                        canvas = c;
                        break;
                    }
                }
            }
            if (canvas == null && canvases.Length > 0) canvas = canvases[0];
        }
        if (canvas == null) return;

        // Ensure TrackedDeviceGraphicRaycaster
        if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            Debug.Log($"[{sceneName}] Runtime: Added TrackedDeviceGraphicRaycaster to {canvas.name}.");
        }

        // 4. Ensure SportEndPanel on the target Canvas
        var sportEndPanel = canvas.GetComponent<SportEndPanel>();
        if (sportEndPanel == null)
        {
            sportEndPanel = Object.FindFirstObjectByType<SportEndPanel>();
        }
        if (sportEndPanel == null)
        {
            var panelGo = new GameObject("SportEndPanel");
            sportEndPanel = panelGo.AddComponent<SportEndPanel>();
            Debug.Log($"[{sceneName}] Runtime: Created SportEndPanel.");
        }

        // Clean up any duplicate or disconnected SportEndPanels
        var allEndPanels = Object.FindObjectsByType<SportEndPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var ep in allEndPanels)
        {
            if (ep != sportEndPanel && ep.gameObject != canvas.gameObject)
            {
                Object.Destroy(ep.gameObject);
            }
        }

        // 5. Ensure Buttons
        var continueBtn = canvas.transform.Find("ContinueButton")?.GetComponent<Button>();
        var retryBtn = canvas.transform.Find("RetryButton")?.GetComponent<Button>();

        bool isWhiteboard = canvas.name.Contains("Scoreboard") || sceneName == "GolfScene" || sceneName == "ShootingScene";
        Vector2 contPos = isWhiteboard ? new Vector2(0, -145) : new Vector2(0, -430);
        Vector2 retPos = isWhiteboard ? new Vector2(0, -145) : new Vector2(0, -430);

        if (sceneName == "GolfScene")
        {
            var clutter = new string[] { "TitleText", "ParText", "RecordText", "Btn_Restart" };
            foreach (var n in clutter)
            {
                var t = canvas.transform.Find(n);
                if (t != null) t.gameObject.SetActive(false);
            }
        }
        else if (sceneName == "ShootingScene")
        {
            var oldHud = GameObject.Find("Olympic_Shooting_HUD");
            if (oldHud != null) oldHud.SetActive(false);
            var oldStationCanvas = GameObject.Find("Station_Canvas");
            if (oldStationCanvas != null) oldStationCanvas.SetActive(false);
            var oldStationFrame = GameObject.Find("Station_Frame_Front");
            if (oldStationFrame != null) oldStationFrame.SetActive(false);
        }
        else
        {
            var modeBtns = new string[] { "Button_Sequence", "Button_Rifle", "Button_Pistol", "Button_Shotgun", "Button_Restart" };
            foreach (var btnName in modeBtns)
            {
                var b = canvas.transform.Find(btnName);
                if (b != null) b.gameObject.SetActive(false);
            }
        }

        if (continueBtn == null)
        {
            continueBtn = CreateRuntimeButton(canvas.transform, "ContinueButton", "CONTINUAR", contPos, new Color(0.12f, 0.72f, 0.88f, 1f));
        }

        if (retryBtn == null)
        {
            retryBtn = CreateRuntimeButton(canvas.transform, "RetryButton", "REINTENTAR", retPos, new Color(0.95f, 0.55f, 0.15f, 1f));
        }

        // Connect button OnClick listeners
        continueBtn.onClick.RemoveListener(sportEndPanel.OnContinuePressed);
        continueBtn.onClick.AddListener(sportEndPanel.OnContinuePressed);

        retryBtn.onClick.RemoveListener(sportEndPanel.OnRetryPressed);
        retryBtn.onClick.AddListener(sportEndPanel.OnRetryPressed);

        sportEndPanel.SetButtons(continueBtn, retryBtn);

        // 6. Ensure GameTimer
        var gameTimer = Object.FindFirstObjectByType<GameTimer>(FindObjectsInactive.Include);
        if (gameTimer == null)
        {
            gameTimer = sportEndPanel.gameObject.AddComponent<GameTimer>();
            gameTimer.fallbackTimeSeconds = 90f;
        }

        // Find or create timer text
        if (gameTimer.timerText == null)
        {
            var existingTimerText = canvas.transform.Find("TimerText_Display")?.GetComponent<TextMeshProUGUI>();
            if (existingTimerText == null)
            {
                var timerGo = new GameObject("TimerText_Display");
                timerGo.transform.SetParent(canvas.transform, false);
                var rt = timerGo.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = isWhiteboard ? new Vector2(125, -50) : new Vector2(0, -350);
                rt.sizeDelta = new Vector2(350, 65);

                existingTimerText = timerGo.AddComponent<TextMeshProUGUI>();
                existingTimerText.text = "00:30";
                existingTimerText.fontSize = isWhiteboard ? 55 : 32;
                existingTimerText.fontStyle = FontStyles.Normal;
                existingTimerText.alignment = isWhiteboard ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center;
                existingTimerText.color = isWhiteboard ? new Color(0.358f, 0.358f, 0.358f, 1f) : new Color(1f, 0.9f, 0.2f, 1f);
            }
            gameTimer.timerText = existingTimerText;
        }

        gameTimer.onTimeUp.RemoveListener(sportEndPanel.OnTimeUp);
        gameTimer.onTimeUp.AddListener(sportEndPanel.OnTimeUp);

        Debug.Log($"[{sceneName}] Runtime: Circuit sport components verified and active!");
    }

    private static Button CreateRuntimeButton(Transform parent, string name, string text, Vector2 anchoredPos, Color btnColor)
    {
        var btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);

        var rt = btnGo.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(300, 60);
        rt.localScale = Vector3.one;

        var image = btnGo.AddComponent<Image>();
        image.color = btnColor;

        var button = btnGo.AddComponent<Button>();
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = btnColor;
        colors.highlightedColor = btnColor * 1.15f;
        colors.pressedColor = btnColor * 0.8f;
        colors.selectedColor = btnColor;
        button.colors = colors;

        // Child Text
        var textGo = new GameObject("Text (TMP)");
        textGo.transform.SetParent(btnGo.transform, false);

        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.pivot = new Vector2(0.5f, 0.5f);
        textRt.sizeDelta = Vector2.zero;
        textRt.anchoredPosition = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return button;
    }

    private static Canvas EnsureShootingWhiteboard()
    {
        var existing = GameObject.Find("Scoreboard_WorldSpace");
        if (existing != null)
        {
            existing.transform.rotation = Quaternion.Euler(0f, -40f, 0f);
            var c = existing.GetComponent<Canvas>();
            if (c != null) return c;
        }

        var oldHud = GameObject.Find("Olympic_Shooting_HUD");
        if (oldHud != null) oldHud.SetActive(false);
        var oldStationCanvas = GameObject.Find("Station_Canvas");
        if (oldStationCanvas != null) oldStationCanvas.SetActive(false);
        var oldStationFrame = GameObject.Find("Station_Frame_Front");
        if (oldStationFrame != null) oldStationFrame.SetActive(false);

        GameObject boardObj = new GameObject("Scoreboard_WorldSpace");
        boardObj.transform.position = new Vector3(-2.2f, 1.5f, 1.8f);
        boardObj.transform.rotation = Quaternion.Euler(0f, -40f, 0f);

        Canvas canvas = boardObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        boardObj.AddComponent<CanvasScaler>();
        boardObj.AddComponent<GraphicRaycaster>();
        boardObj.AddComponent<TrackedDeviceGraphicRaycaster>();

        var rect = boardObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(909f, 503f);
        rect.localScale = Vector3.one * 0.002f;

        // Background
        Sprite wbSprite = null;
        var sprites = Resources.FindObjectsOfTypeAll<Sprite>();
        foreach (var s in sprites) if (s.name.Contains("jp3lye")) { wbSprite = s; break; }

        TMP_FontAsset contmFont = null;
        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var f in fonts) if (f.name.Contains("contm")) { contmFont = f; break; }

        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(boardObj.transform, false);
        var bgImg = bgObj.AddComponent<Image>();
        if (wbSprite != null) bgImg.sprite = wbSprite;
        bgImg.color = Color.white;
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        Color charcoal = new Color(0.358f, 0.358f, 0.358f, 1f);

        // ScoreText
        var scoreTmp = CreateRuntimeTMP(boardObj.transform, "ScoreText", new Vector2(125f, 140f), new Vector2(500f, 75f), "Score: 0", 60f, contmFont, charcoal);

        // DisciplineText
        var discTmp = CreateRuntimeTMP(boardObj.transform, "DisciplineText", new Vector2(125f, 40f), new Vector2(500f, 75f), "Fase 1/3: Rifle Olimpico", 60f, contmFont, charcoal);

        // TimerText
        var timerTmp = CreateRuntimeTMP(boardObj.transform, "TimerText_Display", new Vector2(125f, -50f), new Vector2(350f, 65f), "00:30", 55f, contmFont, charcoal);

        // Banner
        GameObject bannerObj = new GameObject("BannerPanel");
        bannerObj.transform.SetParent(boardObj.transform, false);
        var bannerRect = bannerObj.AddComponent<RectTransform>();
        bannerRect.anchoredPosition = Vector2.zero;
        bannerRect.sizeDelta = new Vector2(880f, 240f);
        var bannerImg = bannerObj.AddComponent<Image>();
        bannerImg.color = new Color(0.05f, 0.08f, 0.12f, 0.98f);
        var bTitle = CreateRuntimeTMP(bannerObj.transform, "BannerTitle", new Vector2(0f, 45f), new Vector2(850f, 80f), "FASE 1 COMPLETADA", 50f, contmFont, new Color(1f, 0.85f, 0.1f));
        bTitle.alignment = TextAlignmentOptions.Center;
        var bSub = CreateRuntimeTMP(bannerObj.transform, "BannerSub", new Vector2(0f, -40f), new Vector2(850f, 60f), "Preparando Pistola Rapida en 2s...", 30f, contmFont, Color.white);
        bSub.alignment = TextAlignmentOptions.Center;
        bannerObj.SetActive(false);

        // ShootingScoreboardUI
        var shootingUI = boardObj.AddComponent<Tiro.UI.ShootingScoreboardUI>();
        var fScore = typeof(Tiro.UI.ShootingScoreboardUI).GetField("scoreText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fScore?.SetValue(shootingUI, scoreTmp);
        var fDisc = typeof(Tiro.UI.ShootingScoreboardUI).GetField("disciplineText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fDisc?.SetValue(shootingUI, discTmp);
        var fTimer = typeof(Tiro.UI.ShootingScoreboardUI).GetField("timerText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fTimer?.SetValue(shootingUI, timerTmp);
        var fBanner = typeof(Tiro.UI.ShootingScoreboardUI).GetField("bannerPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fBanner?.SetValue(shootingUI, bannerObj);
        var fTitle = typeof(Tiro.UI.ShootingScoreboardUI).GetField("bannerTitleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fTitle?.SetValue(shootingUI, bTitle);
        var fSub2 = typeof(Tiro.UI.ShootingScoreboardUI).GetField("bannerSubtitleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        fSub2?.SetValue(shootingUI, bSub);

        var rangeManager = Object.FindAnyObjectByType<Tiro.Core.ShootingRangeManager>();
        if (rangeManager != null)
        {
            var fSb = typeof(Tiro.Core.ShootingRangeManager).GetField("scoreboardUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            fSb?.SetValue(rangeManager, shootingUI);
        }

        return canvas;
    }

    private static TextMeshProUGUI CreateRuntimeTMP(Transform parent, string name, Vector2 pos, Vector2 size, string text, float fontSize, TMP_FontAsset font, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        if (font != null) tmp.font = font;
        tmp.color = color;
        tmp.fontSize = fontSize;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        return tmp;
    }
}
