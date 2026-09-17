using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using Tiro.Data;
using Tiro.Targets;
using Tiro.Weapons;
using Tiro.Core;
using Tiro.UI;

namespace Tiro.Editor
{
    public static class ShootingSceneBuilder
    {
        private const string ScenePath = "Assets/00_Scenes/ShootingScene.unity";
        private const string XROriginPrefabGuid = "f6336ac4ac8b4d34bc5072418cdc62a0";
        private const string PauseMenuPrefabGuid = "fb0fc33f728c20c4da4e60734a5f1a43";

        [InitializeOnLoadMethod]
        private static void AutoBuildIfMissing()
        {
            if (!File.Exists(ScenePath))
            {
                BuildScene();
            }
        }

        [MenuItem("VR Sports/Tiro/Build Shooting Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Cargar recursos y materiales
            var pistolData = AssetDatabase.LoadAssetAtPath<PistolDataSO>("Assets/_SO/PistolData_Olympic.asset");
            var targetConfig = AssetDatabase.LoadAssetAtPath<TargetConfigSO>("Assets/_SO/TargetConfig_OlympicStandard.asset");
            var rangeConfig = AssetDatabase.LoadAssetAtPath<ShootingRangeConfigSO>("Assets/_SO/ShootingRangeConfig_Olympic.asset");

            var matGunBlack = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Resources/Materials/M_Gun_MatteBlack.mat");
            var matSightGreen = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Resources/Materials/M_Sight_NeonGreen.mat");
            var matSightOrange = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Resources/Materials/M_Sight_OrangeOptic.mat");
            var matTargetWhite = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Resources/Materials/M_Target_WhiteRing.mat");
            var matTargetBlack = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Resources/Materials/M_Target_BlackRing.mat");
            var matTargetGold = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Resources/Materials/M_Target_GoldCenter.mat");
            var matFloor = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Resources/Materials/M_Range_BoothFloor.mat");
            var matSafetyRed = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Resources/Materials/M_Range_SafetyRed.mat");

            // 1. Instanciar XR Origin
            string xrOriginPath = AssetDatabase.GUIDToAssetPath(XROriginPrefabGuid);
            GameObject xrOrigin = null;
            if (!string.IsNullOrEmpty(xrOriginPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(xrOriginPath);
                if (prefab != null)
                {
                    xrOrigin = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    xrOrigin.transform.position = Vector3.zero;
                    xrOrigin.transform.rotation = Quaternion.identity;
                }
            }

            // 2. Instanciar PauseMenuCanvas
            string pauseMenuPath = AssetDatabase.GUIDToAssetPath(PauseMenuPrefabGuid);
            if (!string.IsNullOrEmpty(pauseMenuPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(pauseMenuPath);
                if (prefab != null)
                {
                    PrefabUtility.InstantiatePrefab(prefab);
                }
            }

            // 3. Entorno del Polígono Olímpico (Shooting Booth & Range)
            GameObject rangeRoot = new GameObject("Olympic_Shooting_Range");

            // Suelo del polígono
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Range_Floor";
            floor.transform.SetParent(rangeRoot.transform);
            floor.transform.position = new Vector3(0f, -0.05f, 25f);
            floor.transform.localScale = new Vector3(16f, 0.1f, 60f);
            if (matFloor != null) floor.GetComponent<Renderer>().material = matFloor;

            // Línea roja reglamentaria de tiro (Safety Firing Line)
            GameObject redLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            redLine.name = "Safety_Firing_Line";
            redLine.transform.SetParent(rangeRoot.transform);
            redLine.transform.position = new Vector3(0f, 0.005f, 0.5f);
            redLine.transform.localScale = new Vector3(16f, 0.01f, 0.18f);
            if (matSafetyRed != null) redLine.GetComponent<Renderer>().material = matSafetyRed;

            // Mesa / Mostrador de apoyo del tirador (Shooting Bench)
            GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "Shooting_Bench";
            bench.transform.SetParent(rangeRoot.transform);
            bench.transform.position = new Vector3(0f, 0.45f, 0.85f);
            bench.transform.localScale = new Vector3(2.4f, 0.9f, 0.55f);
            if (matGunBlack != null) bench.GetComponent<Renderer>().material = matGunBlack;

            // Pared trasera de absorción balística (Bullet Trap Backstop)
            GameObject backstop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backstop.name = "Backstop_Wall";
            backstop.transform.SetParent(rangeRoot.transform);
            backstop.transform.position = new Vector3(0f, 3.5f, 55f);
            backstop.transform.localScale = new Vector3(18f, 7f, 1f);
            if (matGunBlack != null) backstop.GetComponent<Renderer>().material = matGunBlack;

            // Paredes laterales del túnel de tiro
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "Tunnel_Left_Wall";
            leftWall.transform.SetParent(rangeRoot.transform);
            leftWall.transform.position = new Vector3(-8f, 3f, 25f);
            leftWall.transform.localScale = new Vector3(0.5f, 6f, 60f);
            if (matFloor != null) leftWall.GetComponent<Renderer>().material = matFloor;

            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "Tunnel_Right_Wall";
            rightWall.transform.SetParent(rangeRoot.transform);
            rightWall.transform.position = new Vector3(8f, 3f, 25f);
            rightWall.transform.localScale = new Vector3(0.5f, 6f, 60f);
            if (matFloor != null) rightWall.GetComponent<Renderer>().material = matFloor;

            // 4. Construir las Dianas Concéntricas Olímpicas a 10m, 25m y 50m
            List<TargetBoard> targetBoards = new List<TargetBoard>();
            targetBoards.Add(CreateTargetBoard("Target_10m", new Vector3(0f, 1.4f, 10f), 10f, "Carril 1 (10 Metros)", targetConfig, matTargetWhite, matTargetBlack, matTargetGold, matGunBlack));
            targetBoards.Add(CreateTargetBoard("Target_25m", new Vector3(-2.8f, 1.4f, 25f), 25f, "Carril 2 (25 Metros)", targetConfig, matTargetWhite, matTargetBlack, matTargetGold, matGunBlack));
            targetBoards.Add(CreateTargetBoard("Target_50m", new Vector3(2.8f, 1.4f, 50f), 50f, "Carril 3 (50 Metros)", targetConfig, matTargetWhite, matTargetBlack, matTargetGold, matGunBlack));

            // 5. Construir la Pistola Olímpica de Precisión (Estilo Pistol Whip con Mira de Hierro)
            GameObject pistolObj = BuildOlympicPistol(matGunBlack, matSightGreen, matSightOrange, pistolData);
            pistolObj.transform.position = new Vector3(0.25f, 0.94f, 0.85f);
            OlympicPistol pistolComponent = pistolObj.GetComponent<OlympicPistol>();

            // 6. Monitor HUD Olímpico en el puesto de tiro
            GameObject hudObj = BuildShootingHUD(matGunBlack);
            hudObj.transform.position = new Vector3(-0.95f, 1.35f, 1.05f);
            hudObj.transform.rotation = Quaternion.Euler(0f, 25f, 0f);

            // 7. Manager del Polígono de Tiro
            GameObject managerObj = new GameObject("ShootingRangeManager");
            var manager = managerObj.AddComponent<ShootingRangeManager>();

            var soManager = new SerializedObject(manager);
            soManager.FindProperty("config").objectReferenceValue = rangeConfig;
            soManager.FindProperty("pistol").objectReferenceValue = pistolComponent;

            var targetsProp = soManager.FindProperty("targets");
            targetsProp.arraySize = targetBoards.Count;
            for (int i = 0; i < targetBoards.Count; i++)
            {
                targetsProp.GetArrayElementAtIndex(i).objectReferenceValue = targetBoards[i];
            }
            soManager.ApplyModifiedProperties();

            // Guardar escena en disco
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log($"[ShootingSceneBuilder] ¡Escena de Tiro Deportivo Olímpico construida con éxito en {ScenePath}!");
        }

        private static TargetBoard CreateTargetBoard(string name, Vector3 position, float distance, string laneName, TargetConfigSO config, Material matWhite, Material matBlack, Material matGold, Material matStand)
        {
            GameObject targetRoot = new GameObject(name);
            targetRoot.transform.position = position;

            // Poste de soporte al suelo
            GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stand.name = "Stand_Pole";
            stand.transform.SetParent(targetRoot.transform);
            stand.transform.localPosition = new Vector3(0f, -position.y * 0.5f, 0.05f);
            stand.transform.localScale = new Vector3(0.06f, position.y * 0.5f, 0.06f);
            if (matStand != null) stand.GetComponent<Renderer>().material = matStand;
            Object.DestroyImmediate(stand.GetComponent<Collider>());

            // Marco superior / Pivote de balanceo
            GameObject swingPivotObj = new GameObject("Swing_Pivot");
            swingPivotObj.transform.SetParent(targetRoot.transform);
            swingPivotObj.transform.localPosition = new Vector3(0f, 0.35f, 0f);

            // Tablero de diana
            GameObject board = new GameObject("Target_Face");
            board.transform.SetParent(swingPivotObj.transform);
            board.transform.localPosition = new Vector3(0f, -0.35f, 0f);

            // Placa trasera de impacto (BoxCollider para detección de raycast balístico)
            BoxCollider boxCollider = board.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(0.65f, 0.65f, 0.04f);

            // Anillo Exterior Blanco (Zona 1 a 6) - Diámetro 50cm
            GameObject outerRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outerRing.name = "Ring_Outer_White_50cm";
            outerRing.transform.SetParent(board.transform);
            outerRing.transform.localPosition = Vector3.zero;
            outerRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            outerRing.transform.localScale = new Vector3(0.50f, 0.002f, 0.50f);
            if (matWhite != null) outerRing.GetComponent<Renderer>().material = matWhite;
            Object.DestroyImmediate(outerRing.GetComponent<Collider>());

            // Anillo Medio Negro (Zona 7 a 8) - Diámetro 30cm
            GameObject middleRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            middleRing.name = "Ring_Middle_Black_30cm";
            middleRing.transform.SetParent(board.transform);
            middleRing.transform.localPosition = new Vector3(0f, 0f, -0.003f);
            middleRing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            middleRing.transform.localScale = new Vector3(0.30f, 0.002f, 0.30f);
            if (matBlack != null) middleRing.GetComponent<Renderer>().material = matBlack;
            Object.DestroyImmediate(middleRing.GetComponent<Collider>());

            // Anillo Dorado Central (Zona 9 y 10 / Bullseye) - Diámetro 10cm
            GameObject centerGold = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            centerGold.name = "Ring_Center_Gold_10cm";
            centerGold.transform.SetParent(board.transform);
            centerGold.transform.localPosition = new Vector3(0f, 0f, -0.005f);
            centerGold.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            centerGold.transform.localScale = new Vector3(0.10f, 0.002f, 0.10f);
            if (matGold != null) centerGold.GetComponent<Renderer>().material = matGold;
            Object.DestroyImmediate(centerGold.GetComponent<Collider>());

            // Punto central 'X' (Bullseye de 5cm)
            GameObject centerCross = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            centerCross.name = "Bullseye_InnerTen_5cm";
            centerCross.transform.SetParent(board.transform);
            centerCross.transform.localPosition = new Vector3(0f, 0f, -0.007f);
            centerCross.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            centerCross.transform.localScale = new Vector3(0.045f, 0.002f, 0.045f);
            if (matBlack != null) centerCross.GetComponent<Renderer>().material = matBlack;
            Object.DestroyImmediate(centerCross.GetComponent<Collider>());

            // Etiqueta de distancia en el poste
            GameObject labelObj = new GameObject("Distance_Label");
            labelObj.transform.SetParent(targetRoot.transform);
            labelObj.transform.localPosition = new Vector3(0f, 0.45f, -0.05f);
            var tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.text = $"{distance:0}m";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 2.2f;
            tmp.color = Color.white;

            // Componente TargetBoard
            var targetBoard = targetRoot.AddComponent<TargetBoard>();
            var audioSource = targetRoot.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;

            var so = new SerializedObject(targetBoard);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("targetCenter").objectReferenceValue = centerGold.transform;
            so.FindProperty("swingPivot").objectReferenceValue = swingPivotObj.transform;
            so.FindProperty("distanceMeters").floatValue = distance;
            so.FindProperty("laneName").stringValue = laneName;
            so.FindProperty("audioSource").objectReferenceValue = audioSource;
            so.ApplyModifiedProperties();

            return targetBoard;
        }

        private static GameObject BuildOlympicPistol(Material matGun, Material matSightGreen, Material matSightOrange, PistolDataSO pistolData)
        {
            GameObject pistolRoot = new GameObject("Olympic_Pistol_VR");

            // Empuñadura deportiva ergonómica
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Pistol_Grip";
            grip.transform.SetParent(pistolRoot.transform);
            grip.transform.localPosition = new Vector3(0f, -0.045f, -0.035f);
            grip.transform.localRotation = Quaternion.Euler(16f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.032f, 0.11f, 0.046f);
            if (matGun != null) grip.GetComponent<Renderer>().material = matGun;

            // Guardamonte y gatillo
            GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            guard.name = "Trigger_Guard";
            guard.transform.SetParent(pistolRoot.transform);
            guard.transform.localPosition = new Vector3(0f, -0.02f, 0.02f);
            guard.transform.localScale = new Vector3(0.022f, 0.038f, 0.05f);
            if (matGun != null) guard.GetComponent<Renderer>().material = matGun;

            // Armazón inferior (Frame)
            GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Pistol_Frame";
            frame.transform.SetParent(pistolRoot.transform);
            frame.transform.localPosition = new Vector3(0f, 0.015f, 0.04f);
            frame.transform.localScale = new Vector3(0.032f, 0.035f, 0.18f);
            if (matGun != null) frame.GetComponent<Renderer>().material = matGun;

            // Corredera móvil superior (Slide Assembly)
            GameObject slide = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slide.name = "Pistol_Slide";
            slide.transform.SetParent(pistolRoot.transform);
            slide.transform.localPosition = new Vector3(0f, 0.045f, 0.035f);
            slide.transform.localScale = new Vector3(0.030f, 0.032f, 0.22f);
            if (matGun != null) slide.GetComponent<Renderer>().material = matGun;

            // Cañón frontal
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(slide.transform);
            barrel.transform.localPosition = new Vector3(0f, 0f, 0.45f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.45f, 0.12f, 0.45f);
            if (matGun != null) barrel.GetComponent<Renderer>().material = matGun;

            // --- MIRA DE HIERRO OLÍMPICA DE ALTA PRECISIÓN (IRON SIGHTS) ---

            // 1. Alza Trasera (Rear Sight) con muesca central y 2 puntos verde neón
            GameObject rearSightBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearSightBase.name = "Rear_Sight_Base";
            rearSightBase.transform.SetParent(slide.transform);
            rearSightBase.transform.localPosition = new Vector3(0f, 0.58f, -0.42f);
            rearSightBase.transform.localScale = new Vector3(0.75f, 0.25f, 0.08f);
            if (matGun != null) rearSightBase.GetComponent<Renderer>().material = matGun;

            GameObject rearDotLeft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearDotLeft.name = "Rear_Dot_Left_Green";
            rearDotLeft.transform.SetParent(rearSightBase.transform);
            rearDotLeft.transform.localPosition = new Vector3(-0.35f, 0.1f, -0.52f);
            rearDotLeft.transform.localScale = new Vector3(0.20f, 0.55f, 0.1f);
            if (matSightGreen != null) rearDotLeft.GetComponent<Renderer>().material = matSightGreen;

            GameObject rearDotRight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rearDotRight.name = "Rear_Dot_Right_Green";
            rearDotRight.transform.SetParent(rearSightBase.transform);
            rearDotRight.transform.localPosition = new Vector3(0.35f, 0.1f, -0.52f);
            rearDotRight.transform.localScale = new Vector3(0.20f, 0.55f, 0.1f);
            if (matSightGreen != null) rearDotRight.GetComponent<Renderer>().material = matSightGreen;

            GameObject rearSightAnchor = new GameObject("Rear_Sight_Anchor");
            rearSightAnchor.transform.SetParent(slide.transform);
            rearSightAnchor.transform.localPosition = new Vector3(0f, 0.60f, -0.44f);

            // 2. Punto de Mira Delantero (Front Sight) con poste de fibra óptica naranja fluorescente
            GameObject frontPost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frontPost.name = "Front_Sight_Post";
            frontPost.transform.SetParent(slide.transform);
            frontPost.transform.localPosition = new Vector3(0f, 0.58f, 0.44f);
            frontPost.transform.localScale = new Vector3(0.18f, 0.25f, 0.08f);
            if (matGun != null) frontPost.GetComponent<Renderer>().material = matGun;

            GameObject opticFiber = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            opticFiber.name = "Optic_Fiber_Orange";
            opticFiber.transform.SetParent(frontPost.transform);
            opticFiber.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            opticFiber.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            opticFiber.transform.localScale = new Vector3(0.55f, 0.65f, 0.55f);
            if (matSightOrange != null) opticFiber.GetComponent<Renderer>().material = matSightOrange;

            GameObject frontSightAnchor = new GameObject("Front_Sight_Anchor");
            frontSightAnchor.transform.SetParent(slide.transform);
            frontSightAnchor.transform.localPosition = new Vector3(0f, 0.60f, 0.44f);

            // 3. Boca del Cañón (Muzzle Point) perfectamente colineal con la línea óptica
            GameObject muzzlePoint = new GameObject("Muzzle_Point");
            muzzlePoint.transform.SetParent(pistolRoot.transform);
            muzzlePoint.transform.localPosition = new Vector3(0f, 0.045f, 0.155f);

            // Limpieza de colliders redundantes en piezas cosméticas
            foreach (var col in pistolRoot.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(col);
            }

            // Collider principal trigger y física cinemática para VR
            BoxCollider rootCol = pistolRoot.AddComponent<BoxCollider>();
            rootCol.isTrigger = true;
            rootCol.size = new Vector3(0.04f, 0.18f, 0.26f);
            rootCol.center = new Vector3(0f, 0f, 0.02f);

            Rigidbody rb = pistolRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            AudioSource audioSource = pistolRoot.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;

            // Script de la Pistola
            OlympicPistol pistol = pistolRoot.AddComponent<OlympicPistol>();
            var so = new SerializedObject(pistol);
            so.FindProperty("pistolData").objectReferenceValue = pistolData;
            so.FindProperty("muzzlePoint").objectReferenceValue = muzzlePoint.transform;
            so.FindProperty("frontSight").objectReferenceValue = frontSightAnchor.transform;
            so.FindProperty("rearSight").objectReferenceValue = rearSightAnchor.transform;
            so.FindProperty("slideTransform").objectReferenceValue = slide.transform;
            so.FindProperty("audioSource").objectReferenceValue = audioSource;
            so.FindProperty("bindToRightControllerOnStart").boolValue = true;
            so.ApplyModifiedProperties();

            return pistolRoot;
        }

        private static GameObject BuildShootingHUD(Material matBacking)
        {
            GameObject hudRoot = new GameObject("Olympic_Shooting_HUD");

            // Soporte físico del monitor
            GameObject monitorStand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monitorStand.name = "Monitor_Stand";
            monitorStand.transform.SetParent(hudRoot.transform);
            monitorStand.transform.localPosition = new Vector3(0f, -0.45f, 0f);
            monitorStand.transform.localScale = new Vector3(0.08f, 0.9f, 0.08f);
            if (matBacking != null) monitorStand.GetComponent<Renderer>().material = matBacking;

            GameObject monitorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monitorFrame.name = "Monitor_Frame";
            monitorFrame.transform.SetParent(hudRoot.transform);
            monitorFrame.transform.localPosition = Vector3.zero;
            monitorFrame.transform.localScale = new Vector3(1.1f, 0.7f, 0.04f);
            if (matBacking != null) monitorFrame.GetComponent<Renderer>().material = matBacking;

            // Canvas WorldSpace
            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvasObj.transform.SetParent(hudRoot.transform);
            canvasObj.transform.localPosition = new Vector3(0f, 0f, -0.025f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000f, 650f);
            rect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            // Textos TMP
            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(canvasObj.transform, false);
            var scoreTmp = scoreObj.AddComponent<TextMeshProUGUI>();
            scoreTmp.rectTransform.anchoredPosition = new Vector2(0f, 200f);
            scoreTmp.rectTransform.sizeDelta = new Vector2(900f, 100f);
            scoreTmp.text = "Puntos: <color=#FFD700>0</color> / 100";
            scoreTmp.fontSize = 54;
            scoreTmp.alignment = TextAlignmentOptions.Center;

            GameObject shotsObj = new GameObject("ShotsText");
            shotsObj.transform.SetParent(canvasObj.transform, false);
            var shotsTmp = shotsObj.AddComponent<TextMeshProUGUI>();
            shotsTmp.rectTransform.anchoredPosition = new Vector2(-220f, 90f);
            shotsTmp.rectTransform.sizeDelta = new Vector2(400f, 80f);
            shotsTmp.text = "Tiros: 0 / 10";
            shotsTmp.fontSize = 42;
            shotsTmp.alignment = TextAlignmentOptions.Center;

            GameObject timerObj = new GameObject("TimerText");
            timerObj.transform.SetParent(canvasObj.transform, false);
            var timerTmp = timerObj.AddComponent<TextMeshProUGUI>();
            timerTmp.rectTransform.anchoredPosition = new Vector2(220f, 90f);
            timerTmp.rectTransform.sizeDelta = new Vector2(400f, 80f);
            timerTmp.text = "Tiempo: 01:30";
            timerTmp.fontSize = 42;
            timerTmp.alignment = TextAlignmentOptions.Center;

            GameObject distanceObj = new GameObject("DistanceText");
            distanceObj.transform.SetParent(canvasObj.transform, false);
            var distTmp = distanceObj.AddComponent<TextMeshProUGUI>();
            distTmp.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            distTmp.rectTransform.sizeDelta = new Vector2(900f, 70f);
            distTmp.text = "Serie 1 • Distancia: 10m";
            distTmp.fontSize = 38;
            distTmp.alignment = TextAlignmentOptions.Center;
            distTmp.color = Color.cyan;

            GameObject bannerObj = new GameObject("BannerText");
            bannerObj.transform.SetParent(canvasObj.transform, false);
            var bannerTmp = bannerObj.AddComponent<TextMeshProUGUI>();
            bannerTmp.rectTransform.anchoredPosition = new Vector2(0f, -90f);
            bannerTmp.rectTransform.sizeDelta = new Vector2(900f, 90f);
            bannerTmp.text = "¡Alinea tu mira de hierro y dispara!";
            bannerTmp.fontSize = 38;
            bannerTmp.alignment = TextAlignmentOptions.Center;
            bannerTmp.color = new Color(1f, 0.85f, 0.2f);

            GameObject recordObj = new GameObject("RecordText");
            recordObj.transform.SetParent(canvasObj.transform, false);
            var recordTmp = recordObj.AddComponent<TextMeshProUGUI>();
            recordTmp.rectTransform.anchoredPosition = new Vector2(0f, -190f);
            recordTmp.rectTransform.sizeDelta = new Vector2(950f, 65f);
            recordTmp.text = "Récord: 0 pts | Bullseyes: 0";
            recordTmp.fontSize = 28;
            recordTmp.alignment = TextAlignmentOptions.Center;
            recordTmp.color = new Color(0.8f, 0.8f, 0.8f);

            // Componente ShootingHUD
            var hud = hudRoot.AddComponent<ShootingHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("scoreText").objectReferenceValue = scoreTmp;
            so.FindProperty("shotsText").objectReferenceValue = shotsTmp;
            so.FindProperty("distanceText").objectReferenceValue = distTmp;
            so.FindProperty("timerText").objectReferenceValue = timerTmp;
            so.FindProperty("feedbackBannerText").objectReferenceValue = bannerTmp;
            so.FindProperty("recordText").objectReferenceValue = recordTmp;
            so.ApplyModifiedProperties();

            return hudRoot;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in scenes)
            {
                if (s.path == scenePath) return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
