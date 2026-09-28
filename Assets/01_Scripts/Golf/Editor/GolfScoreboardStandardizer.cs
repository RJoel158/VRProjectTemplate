using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using TMPro;
using Golf.UI;
using Golf.Core;

namespace Golf.Editor
{
    [InitializeOnLoad]
    public static class GolfScoreboardStandardizer
    {
        private const string PrefKey = "GolfScoreboardStandardized_v3";
        private const string GolfScenePath = "Assets/00_Scenes/GolfScene.unity";
        private const string WhiteboardSpritePath = "Assets/03_Resources/Images/Gemini_Generated_Image_jp3lyejp3lyejp3l-removebg-preview.png";
        private const string ContmFontPath = "Assets/03_Resources/Font/contm SDF.asset";

        static GolfScoreboardStandardizer()
        {
            EditorApplication.delayCall += ExecuteStandardization;
        }

        [MenuItem("VR Sports/Golf/Standardize Whiteboard UI")]
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

            Debug.Log("[GolfScoreboardStandardizer] Iniciando estandarizacion de la UI de Golf a Pizarra Flotante...");

            if (!File.Exists(GolfScenePath))
            {
                Debug.LogError("[GolfScoreboardStandardizer] No se encontro GolfScene.unity");
                return;
            }

            var activeScene = EditorSceneManager.GetActiveScene();
            bool needRestore = false;
            string previousScenePath = activeScene.path;

            UnityEngine.SceneManagement.Scene golfScene;
            if (activeScene.path == GolfScenePath)
            {
                golfScene = activeScene;
            }
            else
            {
                golfScene = EditorSceneManager.OpenScene(GolfScenePath, OpenSceneMode.Single);
                needRestore = true;
            }

            var whiteboardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteboardSpritePath);
            var contmFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ContmFontPath);
            Color charcoalColor = new Color(0.3584906f, 0.3584906f, 0.3584906f, 1f);

            var scoreboardObj = GameObject.Find("Scoreboard_WorldSpace");
            if (scoreboardObj == null)
            {
                var sbUI = Object.FindAnyObjectByType<GolfScoreboardUI>();
                if (sbUI != null) scoreboardObj = sbUI.gameObject;
            }

            if (scoreboardObj == null)
            {
                Debug.LogError("[GolfScoreboardStandardizer] No se encontro Scoreboard_WorldSpace en la escena de Golf.");
                return;
            }

            // 1. RectTransform del Canvas principal de la pizarra
            var boardRect = scoreboardObj.GetComponent<RectTransform>();
            if (boardRect != null)
            {
                boardRect.sizeDelta = new Vector2(909f, 503f);
                boardRect.localScale = new Vector3(0.002f, 0.002f, 0.002f);
                scoreboardObj.transform.rotation = Quaternion.Euler(0f, -40f, 0f);
            }

            // 2. Fondo Whiteboard
            var bgTransform = scoreboardObj.transform.Find("Background");
            if (bgTransform != null)
            {
                var bgImg = bgTransform.GetComponent<Image>();
                if (bgImg != null)
                {
                    if (whiteboardSprite != null) bgImg.sprite = whiteboardSprite;
                    bgImg.color = Color.white;
                    bgImg.type = Image.Type.Simple;
                }
                var bgRect = bgTransform.GetComponent<RectTransform>();
                if (bgRect != null)
                {
                    bgRect.anchorMin = Vector2.zero;
                    bgRect.anchorMax = Vector2.one;
                    bgRect.sizeDelta = Vector2.zero;
                    bgRect.anchoredPosition = Vector2.zero;
                }
            }

            // 3. ScoreText (reutiliza TotalText)
            TextMeshProUGUI scoreTmp = null;
            var totalObj = scoreboardObj.transform.Find("TotalText");
            if (totalObj != null)
            {
                totalObj.gameObject.name = "ScoreText";
                scoreTmp = totalObj.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                var scoreObj = scoreboardObj.transform.Find("ScoreText");
                if (scoreObj != null) scoreTmp = scoreObj.GetComponent<TextMeshProUGUI>();
            }

            if (scoreTmp != null)
            {
                scoreTmp.gameObject.SetActive(true);
                scoreTmp.text = "Score: 0";
                if (contmFont != null) scoreTmp.font = contmFont;
                scoreTmp.color = charcoalColor;
                scoreTmp.fontSize = 60f;
                scoreTmp.enableWordWrapping = false;
                scoreTmp.overflowMode = TextOverflowModes.Overflow;
                scoreTmp.alignment = TextAlignmentOptions.MidlineLeft;

                var rt = scoreTmp.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(125f, 140f);
                rt.sizeDelta = new Vector2(500f, 75f);
            }

            // 4. GolpesText (reutiliza StrokesText)
            TextMeshProUGUI strokesTmp = null;
            var strokesObj = scoreboardObj.transform.Find("StrokesText");
            if (strokesObj != null)
            {
                strokesTmp = strokesObj.GetComponent<TextMeshProUGUI>();
                strokesTmp.gameObject.SetActive(true);
                strokesTmp.text = "Golpes: 0";
                if (contmFont != null) strokesTmp.font = contmFont;
                strokesTmp.color = charcoalColor;
                strokesTmp.fontSize = 60f;
                strokesTmp.enableWordWrapping = false;
                strokesTmp.overflowMode = TextOverflowModes.Overflow;
                strokesTmp.alignment = TextAlignmentOptions.MidlineLeft;

                var rt = strokesTmp.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(125f, 40f);
                rt.sizeDelta = new Vector2(500f, 75f);
            }

            // 5. TimerText (reutiliza TimerText_Display)
            TextMeshProUGUI timerTmp = null;
            var timerObj = scoreboardObj.transform.Find("TimerText_Display");
            if (timerObj != null)
            {
                timerTmp = timerObj.GetComponent<TextMeshProUGUI>();
                timerTmp.gameObject.SetActive(true);
                if (contmFont != null) timerTmp.font = contmFont;
                timerTmp.color = charcoalColor;
                timerTmp.fontSize = 55f;
                timerTmp.enableWordWrapping = false;
                timerTmp.overflowMode = TextOverflowModes.Overflow;
                timerTmp.alignment = TextAlignmentOptions.MidlineLeft;

                var rt = timerTmp.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(125f, -50f);
                rt.sizeDelta = new Vector2(350f, 65f);
            }

            // 6. Botones de SportEndPanel
            var continueBtnTransform = scoreboardObj.transform.Find("ContinueButton");
            Button continueBtn = null;
            if (continueBtnTransform != null)
            {
                continueBtn = continueBtnTransform.GetComponent<Button>();
                var rt = continueBtnTransform.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(-140f, -145f);
                rt.sizeDelta = new Vector2(200f, 55f);
            }

            var retryBtnTransform = scoreboardObj.transform.Find("RetryButton");
            Button retryBtn = null;
            if (retryBtnTransform != null)
            {
                retryBtn = retryBtnTransform.GetComponent<Button>();
                var rt = retryBtnTransform.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(140f, -145f);
                rt.sizeDelta = new Vector2(200f, 55f);
            }

            // 7. Desactivar textos viejos no deseados para dejar la pizarra limpia
            string[] clutterNames = { "TitleText", "ParText", "RecordText", "Btn_Restart" };
            foreach (var n in clutterNames)
            {
                var t = scoreboardObj.transform.Find(n);
                if (t != null) t.gameObject.SetActive(false);
            }

            // 8. Re-conectar referencias en GolfScoreboardUI
            var golfUI = scoreboardObj.GetComponent<GolfScoreboardUI>();
            if (golfUI != null)
            {
                var soUI = new SerializedObject(golfUI);
                soUI.FindProperty("scoreText").objectReferenceValue = scoreTmp;
                soUI.FindProperty("strokesText").objectReferenceValue = strokesTmp;
                soUI.FindProperty("timerText").objectReferenceValue = timerTmp;
                soUI.ApplyModifiedProperties();
            }

            // 9. Conectar SportEndPanel y GameTimer
            var sportEndPanel = Object.FindAnyObjectByType<SportEndPanel>();
            if (sportEndPanel != null)
            {
                var soPanel = new SerializedObject(sportEndPanel);
                if (continueBtn != null) soPanel.FindProperty("continueButton").objectReferenceValue = continueBtn;
                if (retryBtn != null) soPanel.FindProperty("retryButton").objectReferenceValue = retryBtn;
                soPanel.ApplyModifiedProperties();

                if (continueBtn != null)
                {
                    UnityEventTools.RemovePersistentListener(continueBtn.onClick, sportEndPanel.OnContinuePressed);
                    UnityEventTools.AddPersistentListener(continueBtn.onClick, sportEndPanel.OnContinuePressed);
                }

                if (retryBtn != null)
                {
                    UnityEventTools.RemovePersistentListener(retryBtn.onClick, sportEndPanel.OnRetryPressed);
                    UnityEventTools.AddPersistentListener(retryBtn.onClick, sportEndPanel.OnRetryPressed);
                }
            }

            var timer = Object.FindAnyObjectByType<GameTimer>();
            if (timer != null)
            {
                var soTimer = new SerializedObject(timer);
                if (timerTmp != null) soTimer.FindProperty("timerText").objectReferenceValue = timerTmp;
                soTimer.ApplyModifiedProperties();

                if (sportEndPanel != null)
                {
                    UnityEventTools.RemovePersistentListener(timer.onTimeUp, sportEndPanel.OnTimeUp);
                    UnityEventTools.AddPersistentListener(timer.onTimeUp, sportEndPanel.OnTimeUp);
                }
            }

            EditorSceneManager.MarkSceneDirty(golfScene);
            EditorSceneManager.SaveScene(golfScene);
            Debug.Log("[GolfScoreboardStandardizer] Pizarra de Golf estandarizada y guardada exitosamente!");

            if (needRestore && !string.IsNullOrEmpty(previousScenePath))
            {
                EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
            }
        }
    }
}
