using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
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
        private const string PistolPrefabPath = "Assets/03_Resources/Prefabs/Tiro/Olympic_Pistol_VR.prefab";

        private const string SceneRebuiltKey = "ShootingScene_VR_Rebuilt_v4";

        [InitializeOnLoadMethod]
        private static void AutoBuildIfMissing()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                if (!SessionState.GetBool(SceneRebuiltKey, false))
                {
                    SessionState.SetBool(SceneRebuiltKey, true);
                    BuildScene();
                }
                FixSunReference();
            };

            EditorApplication.playModeStateChanged += (state) =>
            {
                if (state == PlayModeStateChange.EnteredEditMode)
                {
                    if (!SessionState.GetBool(SceneRebuiltKey, false))
                    {
                        SessionState.SetBool(SceneRebuiltKey, true);
                        BuildScene();
                    }
                    FixSunReference();
                }
            };
        }

        [MenuItem("VR Sports/Tiro/Fix Sun Light Reference")]
        public static void FixSunReference()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional)
                {
                    RenderSettings.sun = l;
                    var activeScene = EditorSceneManager.GetActiveScene();
                    if (activeScene.isLoaded)
                    {
                        EditorSceneManager.MarkSceneDirty(activeScene);
                        EditorSceneManager.SaveScene(activeScene);
                    }
                    Debug.Log($"[ShootingSceneBuilder] ¡RenderSettings.sun asignado correctamente al componente Light '{l.name}'!");
                    break;
                }
            }
        }

        [MenuItem("VR Sports/Tiro/Build Shooting Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Eliminar cualquier cámara y AudioListener creados por defecto para que la única cámara sea la del XR Origin
            // Esto elimina la vista lejana de tercera persona y el error de "2 audio listeners in the scene"
            var defaultCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in defaultCameras)
            {
                Object.DestroyImmediate(cam.gameObject);
            }
            var defaultListeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
            foreach (var l in defaultListeners)
            {
                Object.DestroyImmediate(l);
            }

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
            var matWood = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Resources/Materials/M_Arena_Wood_Amber.mat");
            var matWhite = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Resources/Materials/M_Arena_Center_White.mat");
            var matCrimson = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Resources/Materials/M_Arena_Rim_Crimson.mat");

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
            floor.transform.localScale = new Vector3(18f, 0.1f, 60f);
            if (matFloor != null) floor.GetComponent<Renderer>().material = matFloor;

            // Línea roja reglamentaria de tiro (Safety Firing Line)
            GameObject redLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            redLine.name = "Safety_Firing_Line";
            redLine.transform.SetParent(rangeRoot.transform);
            redLine.transform.position = new Vector3(0f, 0.005f, 0.5f);
            redLine.transform.localScale = new Vector3(18f, 0.01f, 0.18f);
            if (matSafetyRed != null) redLine.GetComponent<Renderer>().material = matSafetyRed;

            // Mesa / Mostrador de apoyo del tirador (Shooting Bench)
            GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "Shooting_Bench";
            bench.transform.SetParent(rangeRoot.transform);
            bench.transform.position = new Vector3(0f, 0.45f, 0.85f);
            bench.transform.localScale = new Vector3(2.4f, 0.9f, 0.55f);
            if (matGunBlack != null) bench.GetComponent<Renderer>().material = matGunBlack;

            // Pared trasera de absorción balística a 55m (abierta arriba para el cielo)
            GameObject backstop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backstop.name = "Backstop_Wall";
            backstop.transform.SetParent(rangeRoot.transform);
            backstop.transform.position = new Vector3(0f, 3.5f, 55f);
            backstop.transform.localScale = new Vector3(22f, 7f, 1f);
            if (matGunBlack != null) backstop.GetComponent<Renderer>().material = matGunBlack;

            // 4. Disciplina 1: Dianas Concéntricas Olímpicas a 10m, 25m y 50m
            GameObject distanceTargetsRoot = new GameObject("Distance_Targets_Root");
            distanceTargetsRoot.transform.SetParent(rangeRoot.transform);

            List<TargetBoard> targetBoards = new List<TargetBoard>();
            targetBoards.Add(CreateTargetBoard("Target_10m", new Vector3(0f, 1.4f, 10f), 10f, "Carril 1 (10 Metros)", targetConfig, matTargetWhite, matTargetBlack, matTargetGold, matGunBlack, distanceTargetsRoot.transform));
            targetBoards.Add(CreateTargetBoard("Target_25m", new Vector3(-3.2f, 1.4f, 25f), 25f, "Carril 2 (25 Metros)", targetConfig, matTargetWhite, matTargetBlack, matTargetGold, matGunBlack, distanceTargetsRoot.transform));
            targetBoards.Add(CreateTargetBoard("Target_50m", new Vector3(3.2f, 1.4f, 50f), 50f, "Carril 3 (50 Metros)", targetConfig, matTargetWhite, matTargetBlack, matTargetGold, matGunBlack, distanceTargetsRoot.transform));

            // 5. Disciplina 2: Galería de Pared Cercana con Dianas Dinámicas (8.5m)
            GameObject wallGalleryObj = BuildDynamicWallGallery(new Vector3(0f, 1.55f, 8.5f), matGunBlack, matSightGreen, matTargetBlack, matSightOrange);
            wallGalleryObj.transform.SetParent(rangeRoot.transform);
            DynamicWallTargetGallery wallGallery = wallGalleryObj.GetComponent<DynamicWallTargetGallery>();

            // 6. Disciplina 3: Lanzador de Platos (Clay Pigeon Launcher) en el campo exterior
            GameObject clayLauncherObj = BuildClayPigeonLauncher(new Vector3(0f, 0.35f, 16f), matSightOrange, matGunBlack);
            clayLauncherObj.transform.SetParent(rangeRoot.transform);
            ClayPigeonLauncher clayLauncher = clayLauncherObj.GetComponent<ClayPigeonLauncher>();

            // 7. Construir las 3 Armas Olímpicas
            // A) Pistola Olímpica
            GameObject pistolObj = BuildOlympicPistol(matGunBlack, matSightGreen, matSightOrange, pistolData);
            pistolObj.transform.position = new Vector3(0.35f, 0.94f, 0.85f);
            OlympicPistol pistolComponent = pistolObj.GetComponent<OlympicPistol>();

            // Guardar Pistola como Prefab autónomo
            EnsureDirectoryExists(Path.GetDirectoryName(PistolPrefabPath));
            PrefabUtility.SaveAsPrefabAsset(pistolObj, PistolPrefabPath);
            Debug.Log($"[ShootingSceneBuilder] ¡Olympic_Pistol_VR guardada como Prefab en {PistolPrefabPath}!");

            // B) Rifle Olímpico de Precisión
            GameObject rifleObj = BuildOlympicRifle(matGunBlack, matWood, matSightGreen, matSightOrange, pistolData);
            rifleObj.transform.position = new Vector3(0f, 0.94f, 0.85f);
            OlympicRifle rifleComponent = rifleObj.GetComponent<OlympicRifle>();

            // C) Escopeta Olímpica Superpuesta (Tiro al Plato)
            GameObject shotgunObj = BuildOlympicShotgun(matGunBlack, matWood, matTargetGold, pistolData);
            shotgunObj.transform.position = new Vector3(-0.35f, 0.94f, 0.85f);
            OlympicShotgun shotgunComponent = shotgunObj.GetComponent<OlympicShotgun>();

            // 8. Panel Menú Selector Interactivo en la pared izquierda
            GameObject menuWallObj = BuildDisciplineSelectorWall(
                new Vector3(-1.75f, 1.4f, 1.1f),
                Quaternion.Euler(0f, 40f, 0f),
                matGunBlack,
                matWood,
                matSightGreen,
                matSightOrange,
                matCrimson
            );
            menuWallObj.transform.SetParent(rangeRoot.transform);
            DisciplineSelectorPanel selectorPanel = menuWallObj.GetComponent<DisciplineSelectorPanel>();

            // 9. Monitor HUD de mostrador
            GameObject hudObj = BuildShootingHUD(matGunBlack);
            hudObj.transform.SetParent(rangeRoot.transform);
            hudObj.transform.position = new Vector3(-0.65f, 1.05f, 0.85f);
            hudObj.transform.rotation = Quaternion.Euler(15f, 25f, 0f);

            // 10. Manager Central del Polígono de Tiro
            GameObject managerObj = new GameObject("ShootingRangeManager");
            var manager = managerObj.AddComponent<ShootingRangeManager>();

            var soManager = new SerializedObject(manager);
            soManager.FindProperty("config").objectReferenceValue = rangeConfig;
            soManager.FindProperty("activeDiscipline").enumValueIndex = (int)ShootingDiscipline.DynamicPistolWall;
            soManager.FindProperty("rifle").objectReferenceValue = rifleComponent;
            soManager.FindProperty("pistol").objectReferenceValue = pistolComponent;
            soManager.FindProperty("shotgun").objectReferenceValue = shotgunComponent;
            soManager.FindProperty("distanceTargetsRoot").objectReferenceValue = distanceTargetsRoot;
            soManager.FindProperty("wallGallery").objectReferenceValue = wallGallery;
            soManager.FindProperty("clayLauncher").objectReferenceValue = clayLauncher;

            var targetsProp = soManager.FindProperty("targets");
            targetsProp.arraySize = targetBoards.Count;
            for (int i = 0; i < targetBoards.Count; i++)
            {
                targetsProp.GetArrayElementAtIndex(i).objectReferenceValue = targetBoards[i];
            }
            soManager.ApplyModifiedProperties();

            // Asignar manager al panel selector
            if (selectorPanel != null)
            {
                var soSelector = new SerializedObject(selectorPanel);
                soSelector.FindProperty("rangeManager").objectReferenceValue = manager;
                soSelector.ApplyModifiedProperties();
            }

            // Asignar correctamente el sol de la escena al componente Light
            var sceneLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var l in sceneLights)
            {
                if (l.type == LightType.Directional)
                {
                    RenderSettings.sun = l;
                    break;
                }
            }

            // Guardar escena en disco y abrirla como la escena activa del Editor
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Debug.Log($"[ShootingSceneBuilder] ¡Escena de Tiro Deportivo Olímpico con 3 Disciplinas construida exitosamente en {ScenePath}!");
        }

        private static TargetBoard CreateTargetBoard(string name, Vector3 position, float distance, string laneName, TargetConfigSO config, Material matWhite, Material matBlack, Material matGold, Material matStand, Transform parent)
        {
            GameObject targetRoot = new GameObject(name);
            targetRoot.transform.SetParent(parent);
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

            // Alza Trasera (Rear Sight)
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

            // Punto de Mira Delantero (Front Sight)
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

            // Boca del Cañón (Muzzle Point)
            GameObject muzzlePoint = new GameObject("Muzzle_Point");
            muzzlePoint.transform.SetParent(pistolRoot.transform);
            muzzlePoint.transform.localPosition = new Vector3(0f, 0.045f, 0.155f);

            // Display OLED micro-compacto en el lateral izquierdo del armazón (no estorba la mira)
            GameObject ammoDisplayObj = new GameObject("Ammo_OLED_Side");
            ammoDisplayObj.transform.SetParent(pistolRoot.transform, false);
            ammoDisplayObj.transform.localPosition = new Vector3(-0.019f, 0.035f, 0.03f);
            ammoDisplayObj.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);

            GameObject bezel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bezel.name = "Bezel";
            bezel.transform.SetParent(ammoDisplayObj.transform, false);
            bezel.transform.localPosition = Vector3.zero;
            bezel.transform.localScale = new Vector3(0.038f, 0.016f, 0.002f);
            if (matGun != null) bezel.GetComponent<Renderer>().material = matGun;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(ammoDisplayObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.002f);
            textObj.transform.localScale = Vector3.one * 0.01f;

            var ammoTmp = textObj.AddComponent<TextMeshPro>();
            ammoTmp.alignment = TextAlignmentOptions.Center;
            ammoTmp.fontSize = 3.2f;
            ammoTmp.fontStyle = FontStyles.Bold;
            ammoTmp.color = new Color(0f, 1f, 0.85f, 1f);
            ammoTmp.text = "10 / 10";

            // Limpieza de colliders redundantes
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
            so.FindProperty("ammoText").objectReferenceValue = ammoTmp;
            so.FindProperty("ammoCounterText").objectReferenceValue = ammoTmp;
            so.FindProperty("audioSource").objectReferenceValue = audioSource;
            so.FindProperty("bindToRightControllerOnStart").boolValue = true;
            so.FindProperty("gripOffset").vector3Value = new Vector3(0f, -0.025f, 0.08f);
            so.FindProperty("gripEulerAngles").vector3Value = Vector3.zero;
            so.ApplyModifiedProperties();

            return pistolRoot;
        }

        private static GameObject BuildOlympicRifle(Material matGun, Material matWood, Material matSightGreen, Material matSightOrange, PistolDataSO rifleData)
        {
            GameObject rifleRoot = new GameObject("Olympic_Rifle_VR");

            // Cajón de mecanismos central (Receiver)
            GameObject receiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
            receiver.name = "Rifle_Receiver";
            receiver.transform.SetParent(rifleRoot.transform);
            receiver.transform.localPosition = new Vector3(0f, 0.02f, 0.05f);
            receiver.transform.localScale = new Vector3(0.038f, 0.055f, 0.36f);
            if (matGun != null) receiver.GetComponent<Renderer>().material = matGun;

            // Cañón largo de competición (.22 LR match grade)
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Rifle_Match_Barrel";
            barrel.transform.SetParent(rifleRoot.transform);
            barrel.transform.localPosition = new Vector3(0f, 0.035f, 0.46f);
            barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            barrel.transform.localScale = new Vector3(0.022f, 0.26f, 0.022f);
            if (matGun != null) barrel.GetComponent<Renderer>().material = matGun;

            // Culata ergonómica de competición (Madera de nogal / ámbar)
            GameObject stock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stock.name = "Rifle_Stock";
            stock.transform.SetParent(rifleRoot.transform);
            stock.transform.localPosition = new Vector3(0f, -0.03f, -0.24f);
            stock.transform.localRotation = Quaternion.Euler(-6f, 0f, 0f);
            stock.transform.localScale = new Vector3(0.036f, 0.11f, 0.28f);
            if (matWood != null) stock.GetComponent<Renderer>().material = matWood;

            // Cantonera trasera de apoyo al hombro (Buttplate)
            GameObject buttplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buttplate.name = "Buttplate";
            buttplate.transform.SetParent(stock.transform);
            buttplate.transform.localPosition = new Vector3(0f, 0f, -0.52f);
            buttplate.transform.localScale = new Vector3(1.1f, 1.25f, 0.15f);
            if (matGun != null) buttplate.GetComponent<Renderer>().material = matGun;

            // Empuñadura anatómica
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grip.name = "Anatomic_Grip";
            grip.transform.SetParent(rifleRoot.transform);
            grip.transform.localPosition = new Vector3(0f, -0.07f, -0.04f);
            grip.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.034f, 0.12f, 0.048f);
            if (matWood != null) grip.GetComponent<Renderer>().material = matWood;

            // Guardamonte y gatillo match
            GameObject triggerGuard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            triggerGuard.name = "Match_Trigger_Guard";
            triggerGuard.transform.SetParent(rifleRoot.transform);
            triggerGuard.transform.localPosition = new Vector3(0f, -0.028f, 0.025f);
            triggerGuard.transform.localScale = new Vector3(0.022f, 0.038f, 0.06f);
            if (matGun != null) triggerGuard.GetComponent<Renderer>().material = matGun;

            // --- SISTEMA DE PUNTERÍA OLÍMPICA CON DIÓPTER ---
            // 1. Alza Trasera de Diópter (Micro-aperture Diopter Sight)
            GameObject diopterHousing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            diopterHousing.name = "Diopter_Housing";
            diopterHousing.transform.SetParent(rifleRoot.transform);
            diopterHousing.transform.localPosition = new Vector3(0f, 0.065f, -0.08f);
            diopterHousing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            diopterHousing.transform.localScale = new Vector3(0.032f, 0.008f, 0.032f);
            if (matGun != null) diopterHousing.GetComponent<Renderer>().material = matGun;

            GameObject rearDiopterAnchor = new GameObject("Rear_Diopter_Anchor");
            rearDiopterAnchor.transform.SetParent(rifleRoot.transform);
            rearDiopterAnchor.transform.localPosition = new Vector3(0f, 0.065f, -0.08f);

            // 2. Túnel Frontal de Mira (Globe Front Sight)
            GameObject globeTunnel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            globeTunnel.name = "Front_Globe_Tunnel";
            globeTunnel.transform.SetParent(rifleRoot.transform);
            globeTunnel.transform.localPosition = new Vector3(0f, 0.065f, 0.68f);
            globeTunnel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            globeTunnel.transform.localScale = new Vector3(0.026f, 0.022f, 0.026f);
            if (matGun != null) globeTunnel.GetComponent<Renderer>().material = matGun;

            GameObject frontSightAnchor = new GameObject("Front_Sight_Anchor");
            frontSightAnchor.transform.SetParent(rifleRoot.transform);
            frontSightAnchor.transform.localPosition = new Vector3(0f, 0.065f, 0.68f);

            // 3. Boca del cañón (Muzzle Point)
            GameObject muzzlePoint = new GameObject("Muzzle_Point");
            muzzlePoint.transform.SetParent(rifleRoot.transform);
            muzzlePoint.transform.localPosition = new Vector3(0f, 0.035f, 0.72f);

            // Pantalla OLED de munición sobre el cajón de mecanismos
            GameObject ammoDisplayObj = new GameObject("AmmoDisplay");
            ammoDisplayObj.transform.SetParent(receiver.transform);
            ammoDisplayObj.transform.localPosition = new Vector3(0f, 0.52f, -0.15f);
            ammoDisplayObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var ammoTmp = ammoDisplayObj.AddComponent<TextMeshPro>();
            ammoTmp.text = "10";
            ammoTmp.fontSize = 1.0f;
            ammoTmp.alignment = TextAlignmentOptions.Center;
            ammoTmp.color = Color.cyan;

            // Limpieza de colliders redundantes
            foreach (var col in rifleRoot.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(col);
            }

            BoxCollider rootCol = rifleRoot.AddComponent<BoxCollider>();
            rootCol.isTrigger = true;
            rootCol.size = new Vector3(0.06f, 0.22f, 1.05f);
            rootCol.center = new Vector3(0f, 0f, 0.15f);

            Rigidbody rb = rifleRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            AudioSource audioSource = rifleRoot.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;

            // Script del Rifle
            OlympicRifle rifle = rifleRoot.AddComponent<OlympicRifle>();
            var so = new SerializedObject(rifle);
            so.FindProperty("rifleData").objectReferenceValue = rifleData;
            so.FindProperty("muzzlePoint").objectReferenceValue = muzzlePoint.transform;
            so.FindProperty("frontSight").objectReferenceValue = frontSightAnchor.transform;
            so.FindProperty("rearDiopterSight").objectReferenceValue = rearDiopterAnchor.transform;
            so.FindProperty("ammoText").objectReferenceValue = ammoTmp;
            so.FindProperty("audioSource").objectReferenceValue = audioSource;
            so.FindProperty("gripOffset").vector3Value = new Vector3(0f, -0.04f, 0.15f);
            so.FindProperty("gripEulerAngles").vector3Value = Vector3.zero;
            so.ApplyModifiedProperties();

            return rifleRoot;
        }

        private static GameObject BuildOlympicShotgun(Material matGun, Material matWood, Material matGold, PistolDataSO shotgunData)
        {
            GameObject shotgunRoot = new GameObject("Olympic_Shotgun_VR");

            // Báscula de acero (Receiver / Action)
            GameObject receiver = GameObject.CreatePrimitive(PrimitiveType.Cube);
            receiver.name = "Shotgun_Receiver";
            receiver.transform.SetParent(shotgunRoot.transform);
            receiver.transform.localPosition = new Vector3(0f, 0.02f, 0.04f);
            receiver.transform.localScale = new Vector3(0.042f, 0.065f, 0.24f);
            if (matGun != null) receiver.GetComponent<Renderer>().material = matGun;

            // Doble Cañón Superpuesto (Over-Under Barrels)
            GameObject topBarrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            topBarrel.name = "Top_Barrel";
            topBarrel.transform.SetParent(shotgunRoot.transform);
            topBarrel.transform.localPosition = new Vector3(0f, 0.038f, 0.44f);
            topBarrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            topBarrel.transform.localScale = new Vector3(0.024f, 0.28f, 0.024f);
            if (matGun != null) topBarrel.GetComponent<Renderer>().material = matGun;

            GameObject bottomBarrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bottomBarrel.name = "Bottom_Barrel";
            bottomBarrel.transform.SetParent(shotgunRoot.transform);
            bottomBarrel.transform.localPosition = new Vector3(0f, 0.012f, 0.44f);
            bottomBarrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            bottomBarrel.transform.localScale = new Vector3(0.024f, 0.28f, 0.024f);
            if (matGun != null) bottomBarrel.GetComponent<Renderer>().material = matGun;

            // Pasamanos / Delantera de madera (Fore-end)
            GameObject foreEnd = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foreEnd.name = "Wooden_ForeEnd";
            foreEnd.transform.SetParent(shotgunRoot.transform);
            foreEnd.transform.localPosition = new Vector3(0f, -0.005f, 0.32f);
            foreEnd.transform.localScale = new Vector3(0.046f, 0.052f, 0.24f);
            if (matWood != null) foreEnd.GetComponent<Renderer>().material = matWood;

            // Culata de madera con carrillera de tiro al plato (Skeet Buttstock)
            GameObject buttstock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buttstock.name = "Skeet_Buttstock";
            buttstock.transform.SetParent(shotgunRoot.transform);
            buttstock.transform.localPosition = new Vector3(0f, -0.035f, -0.22f);
            buttstock.transform.localRotation = Quaternion.Euler(-7f, 0f, 0f);
            buttstock.transform.localScale = new Vector3(0.040f, 0.125f, 0.30f);
            if (matWood != null) buttstock.GetComponent<Renderer>().material = matWood;

            // Punto de mira de bola (Brass Bead Sight)
            GameObject beadSight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beadSight.name = "Bead_Sight_Gold";
            beadSight.transform.SetParent(shotgunRoot.transform);
            beadSight.transform.localPosition = new Vector3(0f, 0.053f, 0.71f);
            beadSight.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f);
            if (matGold != null) beadSight.GetComponent<Renderer>().material = matGold;

            // Boca del cañón (Muzzle Point)
            GameObject muzzlePoint = new GameObject("Muzzle_Point");
            muzzlePoint.transform.SetParent(shotgunRoot.transform);
            muzzlePoint.transform.localPosition = new Vector3(0f, 0.025f, 0.72f);

            // Display de cartuchos
            GameObject ammoDisplayObj = new GameObject("AmmoDisplay");
            ammoDisplayObj.transform.SetParent(receiver.transform);
            ammoDisplayObj.transform.localPosition = new Vector3(0f, 0.52f, -0.2f);
            ammoDisplayObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var ammoTmp = ammoDisplayObj.AddComponent<TextMeshPro>();
            ammoTmp.text = "2 / 2";
            ammoTmp.fontSize = 0.95f;
            ammoTmp.alignment = TextAlignmentOptions.Center;
            ammoTmp.color = new Color(1f, 0.6f, 0f);

            // Limpieza de colliders redundantes
            foreach (var col in shotgunRoot.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(col);
            }

            BoxCollider rootCol = shotgunRoot.AddComponent<BoxCollider>();
            rootCol.isTrigger = true;
            rootCol.size = new Vector3(0.06f, 0.22f, 1.05f);
            rootCol.center = new Vector3(0f, 0f, 0.15f);

            Rigidbody rb = shotgunRoot.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            AudioSource audioSource = shotgunRoot.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;

            // Script de la Escopeta
            OlympicShotgun shotgun = shotgunRoot.AddComponent<OlympicShotgun>();
            var so = new SerializedObject(shotgun);
            so.FindProperty("shotgunData").objectReferenceValue = shotgunData;
            so.FindProperty("muzzlePoint").objectReferenceValue = muzzlePoint.transform;
            so.FindProperty("frontBeadSight").objectReferenceValue = beadSight.transform;
            so.FindProperty("ammoText").objectReferenceValue = ammoTmp;
            so.FindProperty("audioSource").objectReferenceValue = audioSource;
            so.FindProperty("pelletCount").intValue = 12;
            so.FindProperty("spreadAngleDegrees").floatValue = 3.2f;
            so.FindProperty("gripOffset").vector3Value = new Vector3(0f, -0.04f, 0.12f);
            so.FindProperty("gripEulerAngles").vector3Value = Vector3.zero;
            so.ApplyModifiedProperties();

            return shotgunRoot;
        }

        private static GameObject BuildDynamicWallGallery(Vector3 position, Material matWall, Material matActive, Material matIdle, Material matBullseye)
        {
            GameObject galleryRoot = new GameObject("Dynamic_Wall_Gallery");
            galleryRoot.transform.position = position;

            // Muro panel posterior de absorción deportiva (front face at z = 0m)
            GameObject wallBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallBoard.name = "Gallery_Backboard";
            wallBoard.transform.SetParent(galleryRoot.transform);
            wallBoard.transform.localPosition = new Vector3(0f, 0f, 0.06f);
            wallBoard.transform.localScale = new Vector3(4.6f, 2.9f, 0.12f);
            if (matWall != null) wallBoard.GetComponent<Renderer>().material = matWall;

            // Letrero superior de la galería
            GameObject headerObj = new GameObject("Gallery_Header");
            headerObj.transform.SetParent(galleryRoot.transform);
            headerObj.transform.localPosition = new Vector3(0f, 1.25f, -0.02f);
            var headerTmp = headerObj.AddComponent<TextMeshPro>();
            headerTmp.text = "PISTOLA RÁPIDA - GALERÍA REACTIVA";
            headerTmp.fontSize = 2.4f;
            headerTmp.alignment = TextAlignmentOptions.Center;
            headerTmp.color = Color.green;

            // Componente DynamicWallTargetGallery
            var gallery = galleryRoot.AddComponent<DynamicWallTargetGallery>();
            var so = new SerializedObject(gallery);
            so.FindProperty("matWall").objectReferenceValue = matWall;
            so.FindProperty("matTargetActive").objectReferenceValue = matActive;
            so.FindProperty("matTargetIdle").objectReferenceValue = matIdle;
            so.FindProperty("matBullseye").objectReferenceValue = matBullseye;
            so.FindProperty("roundDurationSeconds").floatValue = 60f;
            so.FindProperty("initialTargetDuration").floatValue = 2.4f;
            so.FindProperty("finalTargetDuration").floatValue = 0.85f;
            so.ApplyModifiedProperties();

            // Generar los nodos interactivos inmediatamente
            gallery.BuildTargetNodes();

            return galleryRoot;
        }

        private static GameObject BuildClayPigeonLauncher(Vector3 position, Material clayMaterial, Material casingMat)
        {
            GameObject launcherRoot = new GameObject("Clay_Pigeon_Launcher");
            launcherRoot.transform.position = position;

            // Chasis de la máquina lanzadora (Trap Machine Casing)
            GameObject casing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            casing.name = "Machine_Housing";
            casing.transform.SetParent(launcherRoot.transform);
            casing.transform.localPosition = Vector3.zero;
            casing.transform.localScale = new Vector3(0.9f, 0.65f, 0.9f);
            if (casingMat != null) casing.GetComponent<Renderer>().material = casingMat;

            // Rampa de eyección en ángulo apuntando al cielo
            GameObject ejectionArm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ejectionArm.name = "Ejection_Guide";
            ejectionArm.transform.SetParent(launcherRoot.transform);
            ejectionArm.transform.localPosition = new Vector3(0f, 0.35f, 0.15f);
            ejectionArm.transform.localRotation = Quaternion.Euler(-45f, 0f, 0f);
            ejectionArm.transform.localScale = new Vector3(0.28f, 0.25f, 0.28f);
            if (casingMat != null) ejectionArm.GetComponent<Renderer>().material = casingMat;

            // Punto de lanzamiento en la punta de la rampa
            GameObject launchPoint = new GameObject("Launch_Point");
            launchPoint.transform.SetParent(launcherRoot.transform);
            launchPoint.transform.localPosition = new Vector3(0f, 0.55f, 0.35f);

            var audioSource = launcherRoot.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;

            var launcher = launcherRoot.AddComponent<ClayPigeonLauncher>();
            var so = new SerializedObject(launcher);
            so.FindProperty("launchPoint").objectReferenceValue = launchPoint.transform;
            so.FindProperty("clayMaterial").objectReferenceValue = clayMaterial;
            so.FindProperty("audioSource").objectReferenceValue = audioSource;
            so.FindProperty("claysPerRound").intValue = 10;
            so.FindProperty("launchInterval").floatValue = 3.5f;
            so.FindProperty("minLaunchSpeed").floatValue = 18f;
            so.FindProperty("maxLaunchSpeed").floatValue = 24f;
            so.FindProperty("minElevationAngle").floatValue = 38f;
            so.FindProperty("maxElevationAngle").floatValue = 54f;
            so.FindProperty("minAzimuthAngle").floatValue = -22f;
            so.FindProperty("maxAzimuthAngle").floatValue = 22f;
            so.ApplyModifiedProperties();

            return launcherRoot;
        }

        private static GameObject BuildDisciplineSelectorWall(Vector3 position, Quaternion rotation, Material matBacking, Material matWood, Material matGreen, Material matOrange, Material matCrimson)
        {
            GameObject wallRoot = new GameObject("Discipline_Selector_Station");
            wallRoot.transform.position = position;
            wallRoot.transform.rotation = rotation;

            // Soporte físico del kiosco / pared
            GameObject panelFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panelFrame.name = "Station_Kiosk_Frame";
            panelFrame.transform.SetParent(wallRoot.transform);
            panelFrame.transform.localPosition = Vector3.zero;
            panelFrame.transform.localScale = new Vector3(1.4f, 1.7f, 0.08f);
            if (matBacking != null) panelFrame.GetComponent<Renderer>().material = matBacking;

            // Marco decorativo de madera ámbar
            GameObject frameTrim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frameTrim.name = "Station_Trim";
            frameTrim.transform.SetParent(wallRoot.transform);
            frameTrim.transform.localPosition = new Vector3(0f, 0.86f, 0f);
            frameTrim.transform.localScale = new Vector3(1.44f, 0.06f, 0.10f);
            if (matWood != null) frameTrim.GetComponent<Renderer>().material = matWood;

            // Canvas WorldSpace
            GameObject canvasObj = new GameObject("Station_Canvas");
            canvasObj.transform.SetParent(wallRoot.transform);
            canvasObj.transform.localPosition = new Vector3(0f, 0f, -0.045f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.AddComponent<GraphicRaycaster>();

            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1000f, 1200f);
            rect.localScale = new Vector3(0.0012f, 0.0012f, 0.0012f);

            // 1. Título
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(canvasObj.transform, false);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.rectTransform.anchoredPosition = new Vector2(0f, 500f);
            titleTmp.rectTransform.sizeDelta = new Vector2(950f, 90f);
            titleTmp.text = "POLÍGONO DE TIRO OLÍMPICO";
            titleTmp.fontSize = 50;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = new Color(1f, 0.85f, 0.2f);

            // 2. Subtítulo / Modalidad Activa
            GameObject activeDiscObj = new GameObject("ActiveDisciplineText");
            activeDiscObj.transform.SetParent(canvasObj.transform, false);
            var activeTmp = activeDiscObj.AddComponent<TextMeshProUGUI>();
            activeTmp.rectTransform.anchoredPosition = new Vector2(0f, 410f);
            activeTmp.rectTransform.sizeDelta = new Vector2(950f, 75f);
            activeTmp.text = "MODALIDAD: <color=#00E5FF>🎯 RIFLE DE PRECISIÓN (10m, 25m, 50m)</color>";
            activeTmp.fontSize = 32;
            activeTmp.alignment = TextAlignmentOptions.Center;

            // 3. Botones interactivos (con Button + BoxCollider para click o disparo)
            Button btnSequence = CreateMenuButton(canvasObj.transform, "Btn_Circuit_Sequence", new Vector2(0f, 260f), "🏆 CIRCUITO OLÍMPICO (SECUENCIA COMPLETA)", new Color(0.38f, 0.28f, 0.05f), new Color(1f, 0.88f, 0.2f));
            Button btnPistol = CreateMenuButton(canvasObj.transform, "Btn_Discipline_Pistol", new Vector2(0f, 140f), "🔫 1. PISTOLA RÁPIDA (Pared Dinámica 60s)", new Color(0.12f, 0.34f, 0.22f), Color.green);
            Button btnRifle = CreateMenuButton(canvasObj.transform, "Btn_Discipline_Rifle", new Vector2(0f, 20f), "🎯 2. RIFLE DE PRECISIÓN (10m - 25m - 50m)", new Color(0.12f, 0.24f, 0.38f), Color.cyan);
            Button btnShotgun = CreateMenuButton(canvasObj.transform, "Btn_Discipline_Shotgun", new Vector2(0f, -100f), "💥 3. TIRO AL PLATO (Escopeta Skeet / Trap)", new Color(0.38f, 0.22f, 0.12f), new Color(1f, 0.6f, 0.1f));
            Button btnRestart = CreateMenuButton(canvasObj.transform, "Btn_Restart_Round", new Vector2(0f, -220f), "🔄 REINICIAR SERIE / RONDA", new Color(0.38f, 0.12f, 0.15f), Color.white);

            // 4. Scoreboard de la serie actual
            GameObject scoreObj = new GameObject("ScoreBoardText");
            scoreObj.transform.SetParent(canvasObj.transform, false);
            var scoreTmp = scoreObj.AddComponent<TextMeshProUGUI>();
            scoreTmp.rectTransform.anchoredPosition = new Vector2(0f, -340f);
            scoreTmp.rectTransform.sizeDelta = new Vector2(920f, 100f);
            scoreTmp.text = "Puntos Ronda: <color=#FFD700>0</color>  |  Tiros: 0 / 10";
            scoreTmp.fontSize = 38;
            scoreTmp.alignment = TextAlignmentOptions.Center;

            // 5. Récords Históricos
            GameObject recordsObj = new GameObject("RecordsText");
            recordsObj.transform.SetParent(canvasObj.transform, false);
            var recordsTmp = recordsObj.AddComponent<TextMeshProUGUI>();
            recordsTmp.rectTransform.anchoredPosition = new Vector2(0f, -460f);
            recordsTmp.rectTransform.sizeDelta = new Vector2(950f, 110f);
            recordsTmp.text = "RÉCORDS:\n🎯 Rifle: 0 pts  |  🔫 Pistola: 0 pts  |  💥 Plato: 0 pts";
            recordsTmp.fontSize = 28;
            recordsTmp.alignment = TextAlignmentOptions.Center;
            recordsTmp.color = new Color(0.85f, 0.85f, 0.85f);

            var audioSource = wallRoot.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;

            // Componente DisciplineSelectorPanel
            var panel = wallRoot.AddComponent<DisciplineSelectorPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("headerTitleText").objectReferenceValue = titleTmp;
            so.FindProperty("currentDisciplineText").objectReferenceValue = activeTmp;
            so.FindProperty("scoreBoardText").objectReferenceValue = scoreTmp;
            so.FindProperty("recordsText").objectReferenceValue = recordsTmp;
            so.FindProperty("btnSequence").objectReferenceValue = btnSequence;
            so.FindProperty("btnRifle").objectReferenceValue = btnRifle;
            so.FindProperty("btnPistol").objectReferenceValue = btnPistol;
            so.FindProperty("btnShotgun").objectReferenceValue = btnShotgun;
            so.FindProperty("btnRestart").objectReferenceValue = btnRestart;
            so.FindProperty("audioSource").objectReferenceValue = audioSource;
            so.ApplyModifiedProperties();

            return wallRoot;
        }

        private static Button CreateMenuButton(Transform parent, string name, Vector2 anchoredPos, string label, Color bgColor, Color textColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(880f, 105f);

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.35f;
            colors.pressedColor = bgColor * 0.75f;
            btn.colors = colors;

            // BoxCollider físico para que se pueda interactuar mediante disparo de raycast directo
            BoxCollider col = btnObj.AddComponent<BoxCollider>();
            col.size = new Vector3(880f, 105f, 30f);

            // Texto del botón
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.rectTransform.sizeDelta = new Vector2(840f, 95f);
            tmp.text = label;
            tmp.fontSize = 32;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = textColor;

            return btn;
        }

        private static GameObject BuildShootingHUD(Material matBacking)
        {
            GameObject hudRoot = new GameObject("Olympic_Shooting_HUD");

            // Soporte físico del monitor en el mostrador
            GameObject monitorStand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monitorStand.name = "Monitor_Stand";
            monitorStand.transform.SetParent(hudRoot.transform);
            monitorStand.transform.localPosition = new Vector3(0f, -0.15f, 0f);
            monitorStand.transform.localScale = new Vector3(0.04f, 0.18f, 0.04f);
            if (matBacking != null) monitorStand.GetComponent<Renderer>().material = matBacking;

            // Marco del monitor (Pantalla deportiva compacta 17" sobre la mesa)
            GameObject monitorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            monitorFrame.name = "Monitor_Frame";
            monitorFrame.transform.SetParent(hudRoot.transform);
            monitorFrame.transform.localPosition = Vector3.zero;
            monitorFrame.transform.localScale = new Vector3(0.42f, 0.28f, 0.02f);
            if (matBacking != null) monitorFrame.GetComponent<Renderer>().material = matBacking;

            // Canvas WorldSpace
            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvasObj.transform.SetParent(hudRoot.transform);
            canvasObj.transform.localPosition = new Vector3(0f, 0f, -0.012f);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = canvasObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(840f, 560f);
            rect.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);

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

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
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
