using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class CircuitLeaderboardUI : MonoBehaviour
{
    private static CircuitLeaderboardUI activeInstance;

    public static void ShowOnActiveWhiteboard(CircuitRunRecord currentRecord)
    {
        Canvas targetCanvas = null;

        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c.name == "Scoreboard_WorldSpace" && c.gameObject.activeInHierarchy)
            {
                targetCanvas = c;
                break;
            }
        }

        if (targetCanvas == null)
        {
            foreach (var c in canvases)
            {
                if ((c.name.Contains("Scoreboard") || c.name.Contains("WorldSpace")) && c.gameObject.activeInHierarchy)
                {
                    targetCanvas = c;
                    break;
                }
            }
        }

        if (targetCanvas == null && canvases.Length > 0)
        {
            targetCanvas = canvases[0];
        }

        if (targetCanvas == null)
        {
            Debug.LogError("[CircuitLeaderboardUI] No suitable WorldSpace Canvas found to display Leaderboard.");
            return;
        }

        // Hide old gameplay elements on this canvas
        for (int i = 0; i < targetCanvas.transform.childCount; i++)
        {
            Transform child = targetCanvas.transform.GetChild(i);
            if (child.name == "Background") continue; // keep whiteboard background!
            child.gameObject.SetActive(false);
        }

        // Create or get container
        Transform containerTrans = targetCanvas.transform.Find("Leaderboard_Container");
        if (containerTrans != null)
        {
            Destroy(containerTrans.gameObject);
        }

        GameObject containerGo = new GameObject("Leaderboard_Container");
        containerGo.transform.SetParent(targetCanvas.transform, false);

        RectTransform contRect = containerGo.AddComponent<RectTransform>();
        contRect.anchorMin = Vector2.zero;
        contRect.anchorMax = Vector2.one;
        contRect.sizeDelta = Vector2.zero;
        contRect.anchoredPosition = Vector2.zero;

        CircuitLeaderboardUI ui = containerGo.AddComponent<CircuitLeaderboardUI>();
        activeInstance = ui;
        ui.BuildLeaderboard(containerGo.transform, currentRecord);
    }

    private void BuildLeaderboard(Transform parent, CircuitRunRecord currentRecord)
    {
        TMP_FontAsset font = null;
        var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var f in fonts)
        {
            if (f.name.Contains("contm")) { font = f; break; }
        }

        Color charcoal = new Color(0.18f, 0.22f, 0.28f, 1f);
        Color accentNavy = new Color(0.08f, 0.15f, 0.25f, 1f);
        Color highlightGold = new Color(0.95f, 0.65f, 0.10f, 1f);

        CircuitHistoryData history = SaveManager.Instance != null ? SaveManager.Instance.LoadCircuitHistory() : new CircuitHistoryData();
        int rank = SaveManager.Instance != null ? SaveManager.Instance.GetRunRank(currentRecord, history) : 1;
        int totalRuns = history.records != null ? history.records.Count : 1;

        // 1. Header Title
        CreateTMP(parent, "HeaderTitle", new Vector2(0f, 195f), new Vector2(850f, 50f),
            "CIRCUITO COMPLETADO - TABLA DE RÉCORDS", 34f, font, accentNavy, FontStyles.Bold, TextAlignmentOptions.Center);

        // 2. Summary Card Banner
        GameObject summaryBanner = new GameObject("SummaryBanner");
        summaryBanner.transform.SetParent(parent, false);
        var sumRect = summaryBanner.AddComponent<RectTransform>();
        sumRect.anchoredPosition = new Vector2(0f, 122f);
        sumRect.sizeDelta = new Vector2(820f, 80f);

        var sumImg = summaryBanner.AddComponent<Image>();
        sumImg.color = rank == 1 ? new Color(1.0f, 0.95f, 0.80f, 0.95f) : new Color(0.92f, 0.95f, 0.98f, 0.95f);

        // Outline / Border for the banner
        var outline = summaryBanner.AddComponent<Outline>();
        outline.effectColor = rank == 1 ? highlightGold : new Color(0.3f, 0.6f, 0.9f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);

        // No special glyphs like ★ to avoid missing-glyph square boxes in TextMeshPro
        string rankBadge = rank == 1 ? "¡NUEVO RÉCORD HISTÓRICO!" : $"Puesto #{rank} de {totalRuns}";
        int myTotal = currentRecord != null ? currentRecord.totalScore : 0;

        CreateTMP(summaryBanner.transform, "SummaryScore", new Vector2(0f, 15f), new Vector2(790f, 36f),
            $"TU RUN:  {myTotal} PTS   |   {rankBadge}", 26f, font, accentNavy, FontStyles.Bold, TextAlignmentOptions.Center);

        int bsk = currentRecord != null ? currentRecord.basketScore : 0;
        int bowl = currentRecord != null ? currentRecord.bowlingScore : 0;
        int golf = currentRecord != null ? currentRecord.golfScore : 0;
        int tiro = currentRecord != null ? currentRecord.shootingScore : 0;

        CreateTMP(summaryBanner.transform, "SummaryBreakdown", new Vector2(0f, -18f), new Vector2(790f, 30f),
            $"Basket: {bsk} pts   |   Bolos: {bowl} pts   |   Golf: {golf} pts   |   Tiro: {tiro} pts", 20f, font, charcoal, FontStyles.Normal, TextAlignmentOptions.Center);

        // 3. Table Header Bar
        GameObject tableHeader = new GameObject("TableHeader");
        tableHeader.transform.SetParent(parent, false);
        var thRect = tableHeader.AddComponent<RectTransform>();
        thRect.anchoredPosition = new Vector2(0f, 54f);
        thRect.sizeDelta = new Vector2(820f, 32f);

        var thImg = tableHeader.AddComponent<Image>();
        thImg.color = new Color(0.12f, 0.20f, 0.32f, 1f);

        // Cell-based alignment for table header
        CreateRowCells(tableHeader.transform, "POS", "RUN / FECHA", "BASKET", "BOLOS", "GOLF", "TIRO", "TOTAL", font, Color.white, FontStyles.Bold, 18f);

        // 4. Table Rows (Top 4 Runs)
        List<CircuitRunRecord> sortedRuns = new List<CircuitRunRecord>(history.records);
        sortedRuns.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));
        int rowsToShow = Mathf.Min(sortedRuns.Count, 4);

        float rowStartY = 18f;
        float rowSpacing = 31f;

        for (int i = 0; i < rowsToShow; i++)
        {
            var r = sortedRuns[i];
            bool isCurrent = currentRecord != null && r.runNumber == currentRecord.runNumber && r.totalScore == currentRecord.totalScore;

            GameObject rowObj = new GameObject($"Row_{i + 1}");
            rowObj.transform.SetParent(parent, false);
            var rRect = rowObj.AddComponent<RectTransform>();
            rRect.anchoredPosition = new Vector2(0f, rowStartY - (i * rowSpacing));
            rRect.sizeDelta = new Vector2(820f, 28f);

            var rImg = rowObj.AddComponent<Image>();
            rImg.color = isCurrent ? new Color(0.2f, 0.7f, 0.95f, 0.28f) : (i % 2 == 0 ? new Color(0.95f, 0.95f, 0.95f, 0.65f) : new Color(0.88f, 0.88f, 0.88f, 0.45f));

            if (isCurrent)
            {
                var rowOutline = rowObj.AddComponent<Outline>();
                rowOutline.effectColor = new Color(0.15f, 0.55f, 0.85f, 0.7f);
                rowOutline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            string rankStr = $"#{i + 1}";
            string nameStr = isCurrent ? $"{r.date} (TÚ)" : $"{r.date} R#{r.runNumber}";
            Color textColor = isCurrent ? new Color(0.04f, 0.32f, 0.68f, 1f) : charcoal;
            FontStyles style = isCurrent ? FontStyles.Bold : FontStyles.Normal;

            // Individual column cells matching Header coordinates
            CreateRowCells(rowObj.transform, rankStr, nameStr, r.basketScore.ToString(), r.bowlingScore.ToString(), r.golfScore.ToString(), r.shootingScore.ToString(), $"{r.totalScore} pts", font, textColor, style, 17f);
        }

        // 5. Action Buttons (Bottom)
        CreateActionButton(parent, "RetryCircuitButton", "REINTENTAR CIRCUITO",
            new Vector2(-170f, -175f), new Vector2(280f, 58f), new Color(0.10f, 0.62f, 0.82f, 1f), font, OnRetryCircuitClicked);

        CreateActionButton(parent, "MenuButton", "VOLVER AL MENÚ",
            new Vector2(170f, -175f), new Vector2(280f, 58f), new Color(0.92f, 0.44f, 0.16f, 1f), font, OnBackToMenuClicked);
    }

    /// <summary>
    /// Creates individual text cells for each column with fixed anchored X positions,
    /// guaranteeing 100% vertical alignment between headers and rows regardless of font proportionality.
    /// </summary>
    private static void CreateRowCells(Transform rowParent, string colPos, string colDate, string colBasket, string colBolos, string colGolf, string colTiro, string colTotal, TMP_FontAsset font, Color textColor, FontStyles fontStyle, float fontSize)
    {
        // 1. POS (center)
        CreateCellTMP(rowParent, "Col_POS", new Vector2(-355f, 0f), new Vector2(70f, 26f), colPos, fontSize, font, textColor, fontStyle, TextAlignmentOptions.Center);

        // 2. RUN / FECHA (left-aligned)
        CreateCellTMP(rowParent, "Col_Date", new Vector2(-235f, 0f), new Vector2(150f, 26f), colDate, fontSize, font, textColor, fontStyle, TextAlignmentOptions.Left);

        // 3. BASKET (center)
        CreateCellTMP(rowParent, "Col_Basket", new Vector2(-115f, 0f), new Vector2(80f, 26f), colBasket, fontSize, font, textColor, fontStyle, TextAlignmentOptions.Center);

        // 4. BOLOS (center)
        CreateCellTMP(rowParent, "Col_Bolos", new Vector2(-25f, 0f), new Vector2(80f, 26f), colBolos, fontSize, font, textColor, fontStyle, TextAlignmentOptions.Center);

        // 5. GOLF (center)
        CreateCellTMP(rowParent, "Col_Golf", new Vector2(65f, 0f), new Vector2(80f, 26f), colGolf, fontSize, font, textColor, fontStyle, TextAlignmentOptions.Center);

        // 6. TIRO (center)
        CreateCellTMP(rowParent, "Col_Tiro", new Vector2(155f, 0f), new Vector2(80f, 26f), colTiro, fontSize, font, textColor, fontStyle, TextAlignmentOptions.Center);

        // 7. TOTAL (center)
        CreateCellTMP(rowParent, "Col_Total", new Vector2(275f, 0f), new Vector2(120f, 26f), colTotal, fontSize, font, textColor, fontStyle, TextAlignmentOptions.Center);
    }

    private static TextMeshProUGUI CreateCellTMP(Transform parent, string name, Vector2 pos, Vector2 size,
        string text, float fontSize, TMP_FontAsset font, Color color, FontStyles style, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        if (font != null) tmp.font = font;

        return tmp;
    }

    private static void OnRetryCircuitClicked()
    {
        Debug.Log("[CircuitLeaderboardUI] Reintentar Circuito seleccionado.");
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteCircuitSave();
        }
        if (CircuitFlowManager.Instance != null)
        {
            CircuitFlowManager.Instance.SetMode(GameMode.Circuit);
            CircuitFlowManager.Instance.LoadSportByIndex(0);
        }
        else
        {
            SceneManager.LoadScene("BasketScene");
        }
    }

    private static void OnBackToMenuClicked()
    {
        Debug.Log("[CircuitLeaderboardUI] Volver al Menú seleccionado.");
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteCircuitSave();
        }
        SceneManager.LoadScene("ModeMenu");
    }

    private static TextMeshProUGUI CreateTMP(Transform parent, string name, Vector2 pos, Vector2 size,
        string text, float fontSize, TMP_FontAsset font, Color color, FontStyles style, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        if (font != null) tmp.font = font;

        return tmp;
    }

    private static Button CreateActionButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color btnColor, TMP_FontAsset font, UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);

        RectTransform rect = btnGo.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image img = btnGo.AddComponent<Image>();
        img.color = btnColor;

        BoxCollider col = btnGo.AddComponent<BoxCollider>();
        col.size = new Vector3(size.x, size.y, 10f);

        Button btn = btnGo.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = btnColor;
        colors.highlightedColor = Color.Lerp(btnColor, Color.white, 0.25f);
        colors.pressedColor = Color.Lerp(btnColor, Color.black, 0.25f);
        btn.colors = colors;

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        RectTransform textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22f;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;

        btn.onClick.AddListener(onClickAction);
        return btn;
    }
}
