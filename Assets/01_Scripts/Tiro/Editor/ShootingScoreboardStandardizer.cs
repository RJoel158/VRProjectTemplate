using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;
using Tiro.UI;
using Tiro.Core;

namespace Tiro.Editor
{
    [InitializeOnLoad]
    public static class ShootingScoreboardStandardizer
    {
        private const string PrefKey = "ShootingScoreboardStandardized_v3";
        private const string ShootingScenePath = "Assets/00_Scenes/ShootingScene.unity";
        private const string WhiteboardSpritePath = "Assets/03_Resources/Images/Gemini_Generated_Image_jp3lyejp3lyejp3l-removebg-preview.png";
        private const string ContmFontPath = "Assets/03_Resources/Font/contm SDF.asset";

        static ShootingScoreboardStandardizer()
        {
            EditorApplication.delayCall += ExecuteStandardization;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.delayCall += ExecuteStandardization;
            }
        }

        [MenuItem("VR Sports/Shooting/Standardize Whiteboard UI")]
        public static void ForceExecute()
        {
            EditorPrefs.DeleteKey(PrefKey);
            ExecuteStandardization();
        }

        public static void ExecuteStandardization()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying) return;
            if (EditorPrefs.GetBool(PrefKey, false)) return;
            EditorPrefs.SetBool(PrefKey, true);

            Debug.Log("[ShootingScoreboardStandardizer] Iniciando estandarizacion de la UI de Tiro a Pizarra Flotante...");

            if (!File.Exists(ShootingScenePath))
            {
                Debug.LogError("[ShootingScoreboardStandardizer] No se encontro ShootingScene.unity");
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            bool needRestore = false;
            string previousScenePath = activeScene.path;

            UnityEngine.SceneManagement.Scene shootingScene;
            if (activeScene.path == ShootingScenePath)
            {
                shootingScene = activeScene;
            }
            else
            {
                shootingScene = EditorSceneManager.OpenScene(ShootingScenePath, OpenSceneMode.Single);
                needRestore = true;
            }

            var whiteboardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteboardSpritePath);
            var contmFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ContmFontPath);
            Color charcoalColor = new Color(0.3584906f, 0.3584906f, 0.3584906f, 1f);

            // 1. Ocultar los elementos UI anteriores que ensuciaban la escena (HUD negro en la baranda y textos flotantes en el cielo)
            var oldHud = GameObject.Find("Olympic_Shooting_HUD");
            if (oldHud != null)
            {
                oldHud.SetActive(false);
                Debug.Log("[ShootingScoreboardStandardizer] Ocultado Olympic_Shooting_HUD anterior.");
            }

            var oldStationCanvas = GameObject.Find("Station_Canvas");
            if (oldStationCanvas != null)
            {
                oldStationCanvas.SetActive(false);
                Debug.Log("[ShootingScoreboardStandardizer] Ocultado Station_Canvas anterior.");
            }

            var oldStationFrame = GameObject.Find("Station_Frame_Front");
            if (oldStationFrame != null)
            {
                oldStationFrame.SetActive(false);
            }

            // 2. Buscar o crear Scoreboard_WorldSpace (Pizarra blanca idéntica a Bowling/Basket/Golf)
            var scoreboardObj = GameObject.Find("Scoreboard_WorldSpace");
            if (scoreboardObj == null)
            {
                scoreboardObj = new GameObject("Scoreboard_WorldSpace");
                Undo.RegisterCreatedObjectUndo(scoreboardObj, "Create Scoreboard_WorldSpace");
            }

            // Ubicacion flotante elegante a la izquierda del puesto de tiro mirando al jugador (X=0, Y=0, Z=0)
            scoreboardObj.transform.position = new Vector3(-2.2f, 1.5f, 1.8f);
            scoreboardObj.transform.rotation = Quaternion.Euler(0f, -40f, 0f);

            var canvas = scoreboardObj.GetComponent<Canvas>();
            if (canvas == null) canvas = scoreboardObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            if (scoreboardObj.GetComponent<CanvasScaler>() == null) scoreboardObj.AddComponent<CanvasScaler>();
            if (scoreboardObj.GetComponent<GraphicRaycaster>() == null) scoreboardObj.AddComponent<GraphicRaycaster>();
            if (scoreboardObj.GetComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>() == null)
                scoreboardObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster>();

            var boardRect = scoreboardObj.GetComponent<RectTransform>();
            boardRect.sizeDelta = new Vector2(909f, 503f);
            boardRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);

            // 3. Fondo Whiteboard
            var bgTransform = scoreboardObj.transform.Find("Background");
            GameObject bgObj;
            if (bgTransform == null)
            {
                bgObj = new GameObject("Background");
                bgObj.transform.SetParent(scoreboardObj.transform, false);
            }
            else
            {
                bgObj = bgTransform.gameObject;
            }

            var bgImg = bgObj.GetComponent<Image>();
            if (bgImg == null) bgImg = bgObj.AddComponent<Image>();
            if (whiteboardSprite != null) bgImg.sprite = whiteboardSprite;
            bgImg.color = Color.white;
            bgImg.type = Image.Type.Simple;

            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;

            // 4. ScoreText
            TextMeshProUGUI scoreTmp = GetOrCreateText(scoreboardObj.transform, "ScoreText", new Vector2(125f, 140f), new Vector2(500f, 75f), "Score: 0", 60f, contmFont, charcoalColor);

            // 5. DisciplineText (Fase 1/3: Rifle Olímpico)
            TextMeshProUGUI disciplineTmp = GetOrCreateText(scoreboardObj.transform, "DisciplineText", new Vector2(125f, 40f), new Vector2(500f, 75f), "Fase 1/3: Rifle Olimpico", 60f, contmFont, charcoalColor);

            // 6. TimerText
            TextMeshProUGUI timerTmp = GetOrCreateText(scoreboardObj.transform, "TimerText_Display", new Vector2(125f, -50f), new Vector2(350f, 65f), "00:30", 55f, contmFont, charcoalColor);

            // 7. Banner Flotante para transiciones
            GameObject bannerObj = null;
            var bannerTransform = scoreboardObj.transform.Find("BannerPanel");
            if (bannerTransform == null)
            {
                bannerObj = new GameObject("BannerPanel");
                bannerObj.transform.SetParent(scoreboardObj.transform, false);
                var bannerRect = bannerObj.AddComponent<RectTransform>();
                bannerRect.anchoredPosition = Vector2.zero;
                bannerRect.sizeDelta = new Vector2(880f, 240f);
                var bannerImg = bannerObj.AddComponent<Image>();
                bannerImg.color = new Color(0.05f, 0.08f, 0.12f, 0.98f);
            }
            else
            {
                bannerObj = bannerTransform.gameObject;
            }

            TextMeshProUGUI bannerTitle = GetOrCreateText(bannerObj.transform, "BannerTitle", new Vector2(0f, 45f), new Vector2(850f, 80f), "FASE 1 COMPLETADA", 50f, contmFont, new Color(1f, 0.85f, 0.1f));
            bannerTitle.alignment = TextAlignmentOptions.Center;

            TextMeshProUGUI bannerSub = GetOrCreateText(bannerObj.transform, "BannerSub", new Vector2(0f, -40f), new Vector2(850f, 60f), "Preparando Pistola Rapida en 2s...", 30f, contmFont, Color.white);
            bannerSub.alignment = TextAlignmentOptions.Center;

            bannerObj.SetActive(false);

            // 8. Botones Continuar y Reintentar (SportEndPanel)
            Button continueBtn = GetOrCreateButton(scoreboardObj.transform, "ContinueButton", "CONTINUAR", new Vector2(-140f, -145f), new Color(0.12f, 0.72f, 0.88f, 1f));
            Button retryBtn = GetOrCreateButton(scoreboardObj.transform, "RetryButton", "REINTENTAR", new Vector2(140f, -145f), new Color(0.95f, 0.55f, 0.15f, 1f));

            continueBtn.gameObject.SetActive(false);
            retryBtn.gameObject.SetActive(false);

            var sportEndPanel = scoreboardObj.GetComponent<SportEndPanel>();
            if (sportEndPanel == null) sportEndPanel = scoreboardObj.AddComponent<SportEndPanel>();

            var soEndPanel = new SerializedObject(sportEndPanel);
            soEndPanel.FindProperty("continueButton").objectReferenceValue = continueBtn;
            soEndPanel.FindProperty("retryButton").objectReferenceValue = retryBtn;
            soEndPanel.ApplyModifiedProperties();

            // Configurar OnClick de los botones
            UnityEventTools.RemovePersistentListener(continueBtn.onClick, sportEndPanel.OnContinuePressed);
            UnityEventTools.AddPersistentListener(continueBtn.onClick, sportEndPanel.OnContinuePressed);

            UnityEventTools.RemovePersistentListener(retryBtn.onClick, sportEndPanel.OnRetryPressed);
            UnityEventTools.AddPersistentListener(retryBtn.onClick, sportEndPanel.OnRetryPressed);

            // 9. Componente ShootingScoreboardUI
            var shootingUI = scoreboardObj.GetComponent<ShootingScoreboardUI>();
            if (shootingUI == null) shootingUI = scoreboardObj.AddComponent<ShootingScoreboardUI>();

            var soUI = new SerializedObject(shootingUI);
            soUI.FindProperty("scoreText").objectReferenceValue = scoreTmp;
            soUI.FindProperty("disciplineText").objectReferenceValue = disciplineTmp;
            soUI.FindProperty("timerText").objectReferenceValue = timerTmp;
            soUI.FindProperty("bannerPanel").objectReferenceValue = bannerObj;
            soUI.FindProperty("bannerTitleText").objectReferenceValue = bannerTitle;
            soUI.FindProperty("bannerSubtitleText").objectReferenceValue = bannerSub;
            soUI.ApplyModifiedProperties();

            // 10. Conectar a ShootingRangeManager
            var rangeManager = Object.FindAnyObjectByType<ShootingRangeManager>();
            if (rangeManager != null)
            {
                var soRange = new SerializedObject(rangeManager);
                var propSb = soRange.FindProperty("scoreboardUI");
                if (propSb != null)
                {
                    propSb.objectReferenceValue = shootingUI;
                    soRange.ApplyModifiedProperties();
                    Debug.Log("[ShootingScoreboardStandardizer] scoreboardUI asignado en ShootingRangeManager.");
                }
            }

            // 11. Eliminar o desactivar SportEndPanels duplicados en la escena
            var allEndPanels = Object.FindObjectsByType<SportEndPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ep in allEndPanels)
            {
                if (ep != sportEndPanel)
                {
                    ep.gameObject.SetActive(false);
                }
            }

            EditorSceneManager.MarkSceneDirty(shootingScene);
            EditorSceneManager.SaveScene(shootingScene);
            Debug.Log("[ShootingScoreboardStandardizer] Pizarra Flotante de ShootingScene estandarizada y guardada exitosamente.");

            if (needRestore && !string.IsNullOrEmpty(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }

        private static TextMeshProUGUI GetOrCreateText(Transform parent, string name, Vector2 pos, Vector2 size, string defaultText, float fontSize, TMP_FontAsset font, Color color)
        {
            var t = parent.Find(name);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = t.gameObject;
            }

            go.SetActive(true);
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = defaultText;
            if (font != null) tmp.font = font;
            tmp.color = color;
            tmp.fontSize = fontSize;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;

            return tmp;
        }

        private static Button GetOrCreateButton(Transform parent, string name, string text, Vector2 pos, Color btnColor)
        {
            var t = parent.Find(name);
            GameObject btnGo;
            if (t == null)
            {
                btnGo = new GameObject(name);
                btnGo.transform.SetParent(parent, false);
            }
            else
            {
                btnGo = t.gameObject;
            }

            var rect = btnGo.GetComponent<RectTransform>();
            if (rect == null) rect = btnGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(200f, 55f);

            var img = btnGo.GetComponent<Image>();
            if (img == null) img = btnGo.AddComponent<Image>();
            img.color = btnColor;

            var btn = btnGo.GetComponent<Button>();
            if (btn == null) btn = btnGo.AddComponent<Button>();

            var textT = btnGo.transform.Find("Text");
            GameObject textGo;
            if (textT == null)
            {
                textGo = new GameObject("Text");
                textGo.transform.SetParent(btnGo.transform, false);
            }
            else
            {
                textGo = textT.gameObject;
            }

            var textRect = textGo.GetComponent<RectTransform>();
            if (textRect == null) textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }
    }
}
