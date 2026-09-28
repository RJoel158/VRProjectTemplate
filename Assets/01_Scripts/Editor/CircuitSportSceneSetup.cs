using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;
using Golf.Core;
using Tiro.Core;

namespace VRProjectTemplate.Editor
{
    public static class CircuitSportSceneSetup
    {
        private const string ActionsAssetGuid = "ca9f5fa95ffab41fb9a615ab714db018";
        private const string GolfScenePath = "Assets/00_Scenes/GolfScene.unity";
        private const string ShootingScenePath = "Assets/00_Scenes/ShootingScene.unity";

        [MenuItem("VR Sports/Circuit/Setup All Sport Scenes")]
        public static void RunSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
            {
                Debug.LogWarning("[CircuitSportSceneSetup] No se puede ejecutar el setup de escenas durante el Play Mode. Por favor sal del Play Mode.");
                return;
            }

            Debug.Log("[CircuitSportSceneSetup] Starting automated circuit setup for sports scenes...");
            SetupGolfScene();
            SetupShootingScene();
            Debug.Log("[CircuitSportSceneSetup] Completed automated circuit setup for all sports scenes!");
        }

        private static void SetupGolfScene()
        {
            if (!File.Exists(GolfScenePath)) return;

            var scene = EditorSceneManager.OpenScene(GolfScenePath, OpenSceneMode.Single);
            bool isDirty = false;

            // 1. EventSystem
            EnsureEventSystem();

            // 2. ScoreManager in GolfCourseManager
            var golfManager = Object.FindFirstObjectByType<GolfCourseManager>();
            if (golfManager != null && golfManager.GetComponent<ScoreManager>() == null)
            {
                golfManager.gameObject.AddComponent<ScoreManager>();
                Debug.Log("[GolfScene] Added ScoreManager to GolfCourseManager.");
                isDirty = true;
            }

            // 3. Find Canvas
            Canvas canvas = null;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.name.Contains("Scoreboard") || c.name.Contains("WorldSpace"))
                {
                    canvas = c;
                    break;
                }
            }
            if (canvas == null && canvases.Length > 0) canvas = canvases[0];

            if (canvas != null)
            {
                // Ensure TrackedDeviceGraphicRaycaster
                if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                    Debug.Log("[GolfScene] Added TrackedDeviceGraphicRaycaster to Canvas.");
                    isDirty = true;
                }

                // 4. SportEndPanel
                var sportEndPanel = Object.FindFirstObjectByType<SportEndPanel>(FindObjectsInactive.Include);
                if (sportEndPanel == null)
                {
                    var panelGo = new GameObject("SportEndPanel");
                    sportEndPanel = panelGo.AddComponent<SportEndPanel>();
                    Debug.Log("[GolfScene] Created SportEndPanel GameObject.");
                    isDirty = true;
                }

                // 5. Buttons under Canvas
                var continueBtn = canvas.transform.Find("ContinueButton")?.GetComponent<Button>();
                if (continueBtn == null)
                {
                    continueBtn = CreateCircuitButton(canvas.transform, "ContinueButton", "CONTINUAR", new Vector2(-130, -240), new Color(0.12f, 0.72f, 0.88f, 1f));
                    isDirty = true;
                }

                var retryBtn = canvas.transform.Find("RetryButton")?.GetComponent<Button>();
                if (retryBtn == null)
                {
                    retryBtn = CreateCircuitButton(canvas.transform, "RetryButton", "REINTENTAR", new Vector2(130, -240), new Color(0.95f, 0.55f, 0.15f, 1f));
                    isDirty = true;
                }

                // Wire Button OnClick persistent calls
                WireButtonOnClick(continueBtn, sportEndPanel, "OnContinuePressed");
                WireButtonOnClick(retryBtn, sportEndPanel, "OnRetryPressed");

                // Assign to SportEndPanel serialized fields
                var soPanel = new SerializedObject(sportEndPanel);
                soPanel.FindProperty("continueButton").objectReferenceValue = continueBtn;
                soPanel.FindProperty("retryButton").objectReferenceValue = retryBtn;
                soPanel.ApplyModifiedProperties();

                // 6. GameTimer
                var gameTimer = Object.FindFirstObjectByType<GameTimer>(FindObjectsInactive.Include);
                if (gameTimer == null)
                {
                    gameTimer = sportEndPanel.gameObject.AddComponent<GameTimer>();
                    gameTimer.fallbackTimeSeconds = 90f;
                    isDirty = true;
                }

                var timerText = canvas.transform.Find("TimerText_Display")?.GetComponent<TextMeshProUGUI>();
                if (timerText == null)
                {
                    var timerGo = new GameObject("TimerText_Display");
                    timerGo.transform.SetParent(canvas.transform, false);
                    var rt = timerGo.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0, -170);
                    rt.sizeDelta = new Vector2(250, 45);

                    timerText = timerGo.AddComponent<TextMeshProUGUI>();
                    timerText.text = "01:30";
                    timerText.fontSize = 28;
                    timerText.fontStyle = FontStyles.Bold;
                    timerText.alignment = TextAlignmentOptions.Center;
                    timerText.color = new Color(1f, 0.9f, 0.2f, 1f);
                    isDirty = true;
                }

                gameTimer.timerText = timerText;

                // Wire GameTimer onTimeUp -> SportEndPanel.OnTimeUp
                WireTimerOnTimeUp(gameTimer, sportEndPanel);
            }

            if (isDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[GolfScene] Successfully updated and saved with Circuit sport setup.");
            }
        }

        private static void SetupShootingScene()
        {
            if (!File.Exists(ShootingScenePath)) return;

            var scene = EditorSceneManager.OpenScene(ShootingScenePath, OpenSceneMode.Single);
            bool isDirty = false;

            // 1. EventSystem
            EnsureEventSystem();

            // 2. ScoreManager in ShootingRangeManager
            var shootingManager = Object.FindFirstObjectByType<ShootingRangeManager>();
            if (shootingManager != null && shootingManager.GetComponent<ScoreManager>() == null)
            {
                shootingManager.gameObject.AddComponent<ScoreManager>();
                Debug.Log("[ShootingScene] Added ScoreManager to ShootingRangeManager.");
                isDirty = true;
            }

            // 3. Find Canvas
            Canvas canvas = null;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c.name.Contains("Station") || c.name.Contains("Scoreboard"))
                {
                    canvas = c;
                    break;
                }
            }
            if (canvas == null && canvases.Length > 0) canvas = canvases[0];

            if (canvas != null)
            {
                // Ensure TrackedDeviceGraphicRaycaster
                if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                {
                    canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                    Debug.Log("[ShootingScene] Added TrackedDeviceGraphicRaycaster to Canvas.");
                    isDirty = true;
                }

                // 4. Desactivar botones viejos de selector de modos en el panel para estandarizar el inicio directo del circuito
                var modeBtns = new string[] { "Button_Sequence", "Button_Rifle", "Button_Pistol", "Button_Shotgun", "Button_Restart" };
                foreach (var btnName in modeBtns)
                {
                    var b = canvas.transform.Find(btnName);
                    if (b != null)
                    {
                        b.gameObject.SetActive(false);
                        isDirty = true;
                    }
                }

                // 5. SportEndPanel
                var sportEndPanel = Object.FindFirstObjectByType<SportEndPanel>(FindObjectsInactive.Include);
                if (sportEndPanel == null)
                {
                    var panelGo = new GameObject("SportEndPanel");
                    sportEndPanel = panelGo.AddComponent<SportEndPanel>();
                    Debug.Log("[ShootingScene] Created SportEndPanel GameObject.");
                    isDirty = true;
                }

                // 5. Buttons under Canvas
                var continueBtn = canvas.transform.Find("ContinueButton")?.GetComponent<Button>();
                if (continueBtn == null)
                {
                    continueBtn = CreateCircuitButton(canvas.transform, "ContinueButton", "CONTINUAR", new Vector2(-160, -430), new Color(0.12f, 0.72f, 0.88f, 1f));
                    isDirty = true;
                }

                var retryBtn = canvas.transform.Find("RetryButton")?.GetComponent<Button>();
                if (retryBtn == null)
                {
                    retryBtn = CreateCircuitButton(canvas.transform, "RetryButton", "REINTENTAR", new Vector2(160, -430), new Color(0.95f, 0.55f, 0.15f, 1f));
                    isDirty = true;
                }

                // Wire Button OnClick persistent calls
                WireButtonOnClick(continueBtn, sportEndPanel, "OnContinuePressed");
                WireButtonOnClick(retryBtn, sportEndPanel, "OnRetryPressed");

                // Assign to SportEndPanel serialized fields
                var soPanel = new SerializedObject(sportEndPanel);
                soPanel.FindProperty("continueButton").objectReferenceValue = continueBtn;
                soPanel.FindProperty("retryButton").objectReferenceValue = retryBtn;
                soPanel.ApplyModifiedProperties();

                // 6. GameTimer
                var gameTimer = Object.FindFirstObjectByType<GameTimer>(FindObjectsInactive.Include);
                if (gameTimer == null)
                {
                    gameTimer = sportEndPanel.gameObject.AddComponent<GameTimer>();
                    gameTimer.fallbackTimeSeconds = 90f;
                    isDirty = true;
                }

                // Find timer TextMeshProUGUI in Canvas (e.g. from ShootingHUD or create)
                TextMeshProUGUI timerText = null;
                var tmps = canvas.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in tmps)
                {
                    if (t.name.ToLower().Contains("timer") || t.text.Contains("01:30") || t.text.Contains("90s") || t.text.Contains(":"))
                    {
                        timerText = t;
                        break;
                    }
                }

                if (timerText == null)
                {
                    var timerGo = new GameObject("TimerText_Display");
                    timerGo.transform.SetParent(canvas.transform, false);
                    var rt = timerGo.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(0, -350);
                    rt.sizeDelta = new Vector2(250, 50);

                    timerText = timerGo.AddComponent<TextMeshProUGUI>();
                    timerText.text = "01:30";
                    timerText.fontSize = 32;
                    timerText.fontStyle = FontStyles.Bold;
                    timerText.alignment = TextAlignmentOptions.Center;
                    timerText.color = new Color(1f, 0.9f, 0.2f, 1f);
                    isDirty = true;
                }

                gameTimer.timerText = timerText;

                // Wire GameTimer onTimeUp -> SportEndPanel.OnTimeUp
                WireTimerOnTimeUp(gameTimer, sportEndPanel);
            }

            if (isDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[ShootingScene] Successfully updated and saved with Circuit sport setup.");
            }
        }

        private static void EnsureEventSystem()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (eventSystem == null)
            {
                var esGo = new GameObject("EventSystem");
                eventSystem = esGo.AddComponent<EventSystem>();
                esGo.AddComponent<XRUIInputModule>();
                Debug.Log($"[{EditorSceneManager.GetActiveScene().name}] Created EventSystem with XRUIInputModule.");
            }
            else
            {
                var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
                if (inputModule != null)
                {
                    Object.DestroyImmediate(inputModule);
                }

                if (eventSystem.GetComponent<XRUIInputModule>() == null)
                {
                    eventSystem.gameObject.AddComponent<XRUIInputModule>();
                }
            }
        }

        private static Button CreateCircuitButton(Transform parent, string name, string text, Vector2 anchoredPos, Color btnColor)
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);

            var rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(240, 60);
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

        private static void WireButtonOnClick(Button button, SportEndPanel targetPanel, string methodName)
        {
            if (button == null || targetPanel == null) return;

            // Check if already registered
            int count = button.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                if (button.onClick.GetPersistentTarget(i) == targetPanel &&
                    button.onClick.GetPersistentMethodName(i) == methodName)
                {
                    return;
                }
            }

            if (methodName == "OnContinuePressed")
            {
                UnityEventTools.AddPersistentListener(button.onClick, targetPanel.OnContinuePressed);
            }
            else if (methodName == "OnRetryPressed")
            {
                UnityEventTools.AddPersistentListener(button.onClick, targetPanel.OnRetryPressed);
            }
        }

        private static void WireTimerOnTimeUp(GameTimer timer, SportEndPanel targetPanel)
        {
            if (timer == null || targetPanel == null) return;

            int count = timer.onTimeUp.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                if (timer.onTimeUp.GetPersistentTarget(i) == targetPanel &&
                    timer.onTimeUp.GetPersistentMethodName(i) == "OnTimeUp")
                {
                    return;
                }
            }

            UnityEventTools.AddPersistentListener(timer.onTimeUp, targetPanel.OnTimeUp);
        }
    }
}
