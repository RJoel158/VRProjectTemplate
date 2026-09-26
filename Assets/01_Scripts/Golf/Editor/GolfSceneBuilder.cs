using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Golf.Data;
using Golf.Gameplay;
using Golf.Core;
using Golf.Audio;
using Golf.UI;
using TMPro;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace Golf.Editor
{
    /// <summary>
    /// Constructor automatico de la escena de Minigolf VR (GolfScene.unity).
    /// Genera la pista estilo Golf It!, los 3 hoyos con obstaculos fisicos,
    /// el palo putter interactivo, la pelota con fisica de cesped y el sistema de UI y audio.
    /// </summary>
    public static class GolfSceneBuilder
    {
        private const string ScenePath = "Assets/00_Scenes/GolfScene.unity";
        private const string DataFolder = "Assets/01_Scripts/Golf/Data/Configs";
        private const string MaterialsFolder = "Assets/03_Resources/Materials";

        [InitializeOnLoadMethod]
        private static void AutoRunOnEditorLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath))
                {
                    BuildGolfScene();
                }
            };
        }

        [MenuItem("VR Sports/Golf/Build Minigolf Scene")]
        public static void BuildGolfScene()
        {
            Debug.Log("[GolfSceneBuilder] Iniciando construccion de Minigolf VR...");

            EnsureFolders();
            CreateScriptableObjects(out GolfCourseConfigSO courseConfig, out GolfBallSO ballSO, out PutterDataSO putterSO);

            // Crear o abrir la escena
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Configuracion de Entorno e Iluminacion
            SetupLightingAndEnvironment();

            // 2. Materiales y PhysicMaterials
            CreateOrLoadMaterials(out Material turfMat, out Material woodMat, out Material ballMat, out Material putterMat, out Material flagMat, out PhysicsMaterial turfPhysMat, out PhysicsMaterial woodPhysMat);

            // 3. XR Origin / Rig del Jugador
            GameObject xrOrigin = SetupXROrigin();

            // 4. Pelota de Golf
            GameObject ballObj = CreateGolfBall(ballSO, ballMat, turfPhysMat);

            // 5. Putter de Golf (con agarre VR)
            GameObject putterObj = CreateGolfPutter(putterSO, putterMat, ballObj.GetComponent<GolfBall>());

            // 6. Construccion de los 3 Hoyos de Minigolf
            GameObject courseRoot = new GameObject("Minigolf_Course_Root");

            // Entorno y atmosfera de laguna costera con gaviotas e islotes
            GameObject envObj = new GameObject("Golf_Lagoon_Environment");
            envObj.AddComponent<Golf.Environment.GolfLagoonEnvironment>();

            List<HoleRuntimeInstance> holeInstances = new List<HoleRuntimeInstance>();

            // Hoyo 1: Curva Verde (Par 2)
            var h1 = BuildHole1(courseRoot.transform, courseConfig.Holes[0], turfMat, woodMat, flagMat, turfPhysMat, woodPhysMat);
            holeInstances.Add(h1);

            // Hoyo 2: La Rampa y el Puente (Par 3)
            var h2 = BuildHole2(courseRoot.transform, courseConfig.Holes[1], turfMat, woodMat, flagMat, turfPhysMat, woodPhysMat);
            holeInstances.Add(h2);

            // Hoyo 3: El Molino y Pasaje (Par 3)
            var h3 = BuildHole3(courseRoot.transform, courseConfig.Holes[2], turfMat, woodMat, flagMat, turfPhysMat, woodPhysMat);
            holeInstances.Add(h3);

            // 7. Scoreboard World Space
            GameObject scoreboardObj = CreateScoreboard(courseRoot.transform);

            // 8. Gestor de Audio
            GameObject audioManagerObj = new GameObject("GolfAudioManager");
            audioManagerObj.AddComponent<GolfAudioManager>();

            // 9. Gestor del Circuito (GolfCourseManager)
            GameObject managerObj = new GameObject("GolfCourseManager");
            var manager = managerObj.AddComponent<GolfCourseManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("courseConfig").objectReferenceValue = courseConfig;
            so.FindProperty("ball").objectReferenceValue = ballObj.GetComponent<GolfBall>();
            so.FindProperty("putter").objectReferenceValue = putterObj.GetComponent<GolfPutter>();
            if (xrOrigin != null) so.FindProperty("xrOrigin").objectReferenceValue = xrOrigin.transform;
            so.FindProperty("scoreboardUI").objectReferenceValue = scoreboardObj.GetComponent<GolfScoreboardUI>();
            so.FindProperty("sessionProgress").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GolfProgressSO>("Assets/01_Scripts/Golf/Data/Configs/GolfProgressData.asset");

            var holesProp = so.FindProperty("holeInstances");
            holesProp.ClearArray();
            for (int i = 0; i < holeInstances.Count; i++)
            {
                holesProp.InsertArrayElementAtIndex(i);
                var elem = holesProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("holeData").objectReferenceValue = holeInstances[i].holeData;
                elem.FindPropertyRelative("teePoint").objectReferenceValue = holeInstances[i].teePoint;
                elem.FindPropertyRelative("cup").objectReferenceValue = holeInstances[i].cup;
                elem.FindPropertyRelative("playerSpawnPoint").objectReferenceValue = holeInstances[i].playerSpawnPoint;
            }
            so.ApplyModifiedProperties();

            // 10. Guardar la escena y registrarla en Build Settings
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);

            Debug.Log("[GolfSceneBuilder] Minigolf VR construido exitosamente en: " + ScenePath);
        }

        private static void EnsureFolders()
        {
            if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
            if (!Directory.Exists(MaterialsFolder)) Directory.CreateDirectory(MaterialsFolder);
            AssetDatabase.Refresh();
        }

        private static void CreateScriptableObjects(out GolfCourseConfigSO courseConfig, out GolfBallSO ballSO, out PutterDataSO putterSO)
        {
            string ballPath = $"{DataFolder}/GolfBallConfig.asset";
            ballSO = AssetDatabase.LoadAssetAtPath<GolfBallSO>(ballPath);
            if (ballSO == null)
            {
                ballSO = ScriptableObject.CreateInstance<GolfBallSO>();
                AssetDatabase.CreateAsset(ballSO, ballPath);
            }

            string putterPath = $"{DataFolder}/PutterConfig.asset";
            putterSO = AssetDatabase.LoadAssetAtPath<PutterDataSO>(putterPath);
            if (putterSO == null)
            {
                putterSO = ScriptableObject.CreateInstance<PutterDataSO>();
                AssetDatabase.CreateAsset(putterSO, putterPath);
            }

            // Crear hoyos individuales
            HoleDataSO h1 = GetOrCreateHole(1, "Curva Verde", "Pista suave con curva de 90 grados a la derecha.", 2, 5);
            HoleDataSO h2 = GetOrCreateHole(2, "La Rampa y el Puente", "Supera la rampa para pasar sobre el puente sin caer al agua.", 3, 6);
            HoleDataSO h3 = GetOrCreateHole(3, "El Molino Giratorio", "Mide el momento justo para embocar a traves de las aspas del molino.", 3, 6);

            string coursePath = $"{DataFolder}/GolfCourseConfig.asset";
            courseConfig = AssetDatabase.LoadAssetAtPath<GolfCourseConfigSO>(coursePath);
            if (courseConfig == null)
            {
                courseConfig = ScriptableObject.CreateInstance<GolfCourseConfigSO>();
                AssetDatabase.CreateAsset(courseConfig, coursePath);
            }

            var so = new SerializedObject(courseConfig);
            so.FindProperty("courseName").stringValue = "Golf It! Resort Minigolf";
            so.FindProperty("description").stringValue = "Circuito de 3 hoyos con fisicas de rebote en madera, rampas y molino.";
            var holesList = so.FindProperty("holes");
            holesList.ClearArray();
            holesList.InsertArrayElementAtIndex(0);
            holesList.GetArrayElementAtIndex(0).objectReferenceValue = h1;
            holesList.InsertArrayElementAtIndex(1);
            holesList.GetArrayElementAtIndex(1).objectReferenceValue = h2;
            holesList.InsertArrayElementAtIndex(2);
            holesList.GetArrayElementAtIndex(2).objectReferenceValue = h3;
            so.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
        }

        private static HoleDataSO GetOrCreateHole(int number, string name, string desc, int par, int maxStrokes)
        {
            string path = $"{DataFolder}/Hole_{number:00}.asset";
            HoleDataSO hole = AssetDatabase.LoadAssetAtPath<HoleDataSO>(path);
            if (hole == null)
            {
                hole = ScriptableObject.CreateInstance<HoleDataSO>();
                AssetDatabase.CreateAsset(hole, path);
            }

            var so = new SerializedObject(hole);
            so.FindProperty("holeNumber").intValue = number;
            so.FindProperty("holeName").stringValue = name;
            so.FindProperty("description").stringValue = desc;
            so.FindProperty("par").intValue = par;
            so.FindProperty("maxStrokes").intValue = maxStrokes;
            so.FindProperty("outOfBoundsPenalty").intValue = 1;
            so.FindProperty("cupRadius").floatValue = 0.09f;
            so.FindProperty("cupMaxEntrySpeed").floatValue = 2.8f;
            so.ApplyModifiedProperties();

            return hole;
        }

        private static void SetupLightingAndEnvironment()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 1.15f;

            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // Suelo exterior / Mar decorativo estilo Resort
            GameObject oceanObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            oceanObj.name = "Resort_Water_Plane";
            oceanObj.transform.position = new Vector3(8f, -0.6f, 6f);
            oceanObj.transform.localScale = new Vector3(8f, 1f, 8f);

            var waterRend = oceanObj.GetComponent<Renderer>();
            var waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            waterMat.color = new Color(0.12f, 0.52f, 0.78f, 1f);
            waterMat.SetFloat("_Smoothness", 0.9f);
            waterRend.sharedMaterial = waterMat;

            // Trigger de fuera de pista general bajo el agua
            var boxCol = oceanObj.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector3(100f, 2f, 100f);
            boxCol.center = new Vector3(0f, -0.5f, 0f);
            var oobTrigger = oceanObj.AddComponent<OutOfBoundsTrigger>();
        }

        private static void CreateOrLoadMaterials(out Material turfMat, out Material woodMat, out Material ballMat, out Material putterMat, out Material flagMat, out PhysicsMaterial turfPhysMat, out PhysicsMaterial woodPhysMat)
        {
            turfPhysMat = new PhysicsMaterial("TurfPhysMat")
            {
                dynamicFriction = 0.35f,
                staticFriction = 0.45f,
                bounciness = 0.05f,
                frictionCombine = PhysicsMaterialCombine.Multiply,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };

            woodPhysMat = new PhysicsMaterial("WoodBumperPhysMat")
            {
                dynamicFriction = 0.15f,
                staticFriction = 0.2f,
                bounciness = 0.65f, // Rebote elastico clasico de Golf It!
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Maximum
            };

            turfMat = GetOrCreateMaterial("M_Golf_TurfGreen", new Color(0.15f, 0.62f, 0.25f), 0.15f);
            woodMat = GetOrCreateMaterial("M_Golf_WoodMahogany", new Color(0.48f, 0.25f, 0.12f), 0.5f);
            ballMat = GetOrCreateMaterial("M_Golf_BallPureWhite", Color.white, 0.85f);
            putterMat = GetOrCreateMaterial("M_Golf_PutterMetal", new Color(0.75f, 0.78f, 0.82f), 0.85f, true);
            flagMat = GetOrCreateMaterial("M_Golf_FlagCrimson", new Color(0.9f, 0.15f, 0.15f), 0.4f);
        }

        private static Material GetOrCreateMaterial(string name, Color color, float smoothness, bool metallic = false)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = color;
                mat.SetFloat("_Smoothness", smoothness);
                if (metallic) mat.SetFloat("_Metallic", 0.85f);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private const string ControllerRigGuid = "f6336ac4ac8b4d34bc5072418cdc62a0";
        private const string PauseMenuGuid = "fb0fc33f728c20c4da4e60734a5f1a43";

        private static GameObject SetupXROrigin()
        {
            string rigPath = AssetDatabase.GUIDToAssetPath(ControllerRigGuid);
            GameObject originPrefab = !string.IsNullOrEmpty(rigPath) ? AssetDatabase.LoadAssetAtPath<GameObject>(rigPath) : null;
            if (originPrefab == null)
            {
                originPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/Hands Interaction Demo/Prefabs/XR Origin Hands (XR Rig).prefab");
            }

            GameObject originInstance = null;
            if (originPrefab != null)
            {
                originInstance = (GameObject)PrefabUtility.InstantiatePrefab(originPrefab);
                originInstance.name = "XR Origin (XR Rig)";
            }
            else
            {
                originInstance = new GameObject("XR Origin (XR Rig)");
                originInstance.AddComponent<Unity.XR.CoreUtils.XROrigin>();
                GameObject camObj = new GameObject("Main Camera");
                camObj.transform.SetParent(originInstance.transform);
                var cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            originInstance.transform.position = new Vector3(-0.45f, 0f, 0.12f);
            originInstance.transform.rotation = Quaternion.identity;

            // Instanciar PauseMenuCanvas
            string pauseMenuPath = AssetDatabase.GUIDToAssetPath(PauseMenuGuid);
            if (!string.IsNullOrEmpty(pauseMenuPath))
            {
                var pausePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pauseMenuPath);
                if (pausePrefab != null)
                {
                    PrefabUtility.InstantiatePrefab(pausePrefab);
                }
            }

            return originInstance;
        }

        private static GameObject CreateGolfBall(GolfBallSO ballSO, Material ballMat, PhysicsMaterial turfPhysMat)
        {
            GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.name = "GolfBall";
            ball.transform.localScale = Vector3.one * 0.045f;
            // Posicionar en el Tee del Hoyo 1 sobre el cesped (suelo = 0.05, radio = 0.0225 -> Y = 0.075f)
            ball.transform.position = new Vector3(0f, 0.075f, 0.2f);

            ball.GetComponent<Renderer>().sharedMaterial = ballMat;
            var sphereCol = ball.GetComponent<SphereCollider>();
            sphereCol.material = turfPhysMat;

            var rb = ball.AddComponent<Rigidbody>();
            rb.mass = 0.046f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.isKinematic = true;

            var trail = ball.AddComponent<TrailRenderer>();
            trail.startWidth = 0.02f;
            trail.endWidth = 0.002f;
            trail.time = 0.6f;
            trail.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));

            var comp = ball.AddComponent<GolfBall>();
            var so = new SerializedObject(comp);
            so.FindProperty("config").objectReferenceValue = ballSO;
            so.ApplyModifiedProperties();
            comp.ApplyConfig();

            return ball;
        }

        private static GameObject CreateGolfPutter(PutterDataSO putterSO, Material putterMat, GolfBall ball)
        {
            // El pivote raiz se coloca en la empuñadura (donde la mano sostiene el palo)
            GameObject putterRoot = new GameObject("GolfPutter_VR");
            putterRoot.transform.position = new Vector3(-0.15f, 0.85f, 0.15f);

            // Empuñadura / Grip (en el origen local del palo)
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            grip.name = "Grip";
            grip.transform.SetParent(putterRoot.transform, false);
            grip.transform.localPosition = new Vector3(0f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.024f, 0.10f, 0.024f); // 20cm de empuñadura
            Object.DestroyImmediate(grip.GetComponent<Collider>());
            var gripMat = GetOrCreateMaterial("M_Golf_PutterGrip", new Color(0.12f, 0.12f, 0.12f), 0.3f);
            grip.GetComponent<Renderer>().sharedMaterial = gripMat;

            // Vara / Shaft (se extiende hacia abajo en -Y)
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Shaft";
            shaft.transform.SetParent(putterRoot.transform, false);
            shaft.transform.localPosition = new Vector3(0f, -0.51f, 0f);
            shaft.transform.localScale = new Vector3(0.014f, 0.51f, 0.014f); // 1.02m de longitud total
            Object.DestroyImmediate(shaft.GetComponent<Collider>());
            shaft.GetComponent<Renderer>().sharedMaterial = putterMat;

            // Cabezal / Head (en la base del palo a ~1.02m por debajo de la empuñadura)
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "ClubHead";
            head.transform.SetParent(putterRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, -1.02f, 0.02f);
            head.transform.localScale = new Vector3(0.13f, 0.045f, 0.06f);
            head.GetComponent<Renderer>().sharedMaterial = putterMat;

            // Cara frontal para la normal del tiro
            GameObject faceForward = new GameObject("FaceForward");
            faceForward.transform.SetParent(head.transform, false);
            faceForward.transform.localRotation = Quaternion.identity;

            // Collider configurado como Trigger para contacto fluido con la bola
            var headCol = head.GetComponent<BoxCollider>();
            headCol.isTrigger = true;
            headCol.size = new Vector3(1.25f, 1.4f, 1.3f);

            var rb = putterRoot.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            rb.useGravity = false;
            rb.isKinematic = true;

            // Componente GolfPutter
            var putterComp = putterRoot.AddComponent<GolfPutter>();
            var so = new SerializedObject(putterComp);
            so.FindProperty("config").objectReferenceValue = putterSO;
            so.FindProperty("clubHead").objectReferenceValue = head.transform;
            so.FindProperty("clubFaceForward").objectReferenceValue = faceForward.transform;
            so.FindProperty("targetBall").objectReferenceValue = ball;
            so.FindProperty("autoBindToRightHand").boolValue = true;
            so.FindProperty("localGripPosition").vector3Value = new Vector3(0f, -0.02f, -0.02f);
            so.FindProperty("localGripEuler").vector3Value = Vector3.zero;
            so.ApplyModifiedProperties();

            return putterRoot;
        }

        private static HoleRuntimeInstance BuildHole1(Transform parent, HoleDataSO holeData, Material turfMat, Material woodMat, Material flagMat, PhysicsMaterial turfPhys, PhysicsMaterial woodPhys)
        {
            GameObject holeRoot = new GameObject("Hole_01_CurvaVerde");
            holeRoot.transform.SetParent(parent, false);
            holeRoot.transform.position = Vector3.zero;

            // 1. Tramo recto principal (Tee a Curva) con cabecera trasera
            CreateTurfSection(holeRoot.transform, "Fairway_Straight", new Vector3(0f, 0f, 2f), new Vector3(1.2f, 0.1f, 4.5f), turfMat, woodMat, turfPhys, woodPhys, hasLeftBorder: true, hasRightBorder: true, hasBackBorder: true);

            // 2. Tramo hacia la derecha (Curva a Hoyo) con cierre lateral y frontal
            CreateTurfSection(holeRoot.transform, "Fairway_TurnRight", new Vector3(1.8f, 0f, 4.25f), new Vector3(3.6f, 0.1f, 1.2f), turfMat, woodMat, turfPhys, woodPhys, hasLeftBorder: false, hasRightBorder: true, hasBackBorder: false, hasFrontBorder: true);

            // Tee Point
            GameObject tee = new GameObject("Tee_Point");
            tee.transform.SetParent(holeRoot.transform, false);
            tee.transform.position = new Vector3(0f, 0.05f, 0.2f);
            CreateTeeMarker(tee.transform);

            // Cup / Hoyo
            Vector3 cupPos = new Vector3(3.2f, 0.05f, 4.25f);
            GolfCup cup = CreateCup(holeRoot.transform, "Cup_Hole1", cupPos, holeData, flagMat);

            return new HoleRuntimeInstance
            {
                holeData = holeData,
                teePoint = tee.transform,
                cup = cup,
                playerSpawnPoint = tee.transform
            };
        }

        private static HoleRuntimeInstance BuildHole2(Transform parent, HoleDataSO holeData, Material turfMat, Material woodMat, Material flagMat, PhysicsMaterial turfPhys, PhysicsMaterial woodPhys)
        {
            GameObject holeRoot = new GameObject("Hole_02_RampaPuente");
            holeRoot.transform.SetParent(parent, false);
            holeRoot.transform.position = new Vector3(7.5f, 0f, 0f);

            // 1. Tramo de salida con cabecera trasera
            CreateTurfSection(holeRoot.transform, "Fairway_Start", new Vector3(0f, 0f, 1.5f), new Vector3(1.2f, 0.1f, 3.2f), turfMat, woodMat, turfPhys, woodPhys, hasLeftBorder: true, hasRightBorder: true, hasBackBorder: true);

            // 2. Rampa de subida con laterales de contencion
            GameObject ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ramp.name = "Ramp_Up";
            ramp.transform.SetParent(holeRoot.transform, false);
            ramp.transform.localPosition = new Vector3(0f, 0.22f, 4.1f);
            ramp.transform.localScale = new Vector3(1.2f, 0.1f, 2.2f);
            ramp.transform.localRotation = Quaternion.Euler(-11f, 0f, 0f);
            ramp.GetComponent<Renderer>().sharedMaterial = turfMat;
            ramp.GetComponent<BoxCollider>().material = turfPhys;

            CreateRampBumper(ramp.transform, "Ramp_Up_Bumper_L", new Vector3(-0.64f, 0.11f, 0f), new Vector3(0.08f, 0.22f, 2.2f), woodMat, woodPhys);
            CreateRampBumper(ramp.transform, "Ramp_Up_Bumper_R", new Vector3(0.64f, 0.11f, 0f), new Vector3(0.08f, 0.22f, 2.2f), woodMat, woodPhys);

            // 3. Puente elevado
            CreateTurfSection(holeRoot.transform, "Bridge_Elevated", new Vector3(0f, 0.44f, 6.5f), new Vector3(1.1f, 0.1f, 2.8f), turfMat, woodMat, turfPhys, woodPhys);

            // 4. Rampa de bajada con laterales de contencion
            GameObject rampDown = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rampDown.name = "Ramp_Down";
            rampDown.transform.SetParent(holeRoot.transform, false);
            rampDown.transform.localPosition = new Vector3(0f, 0.22f, 8.8f);
            rampDown.transform.localScale = new Vector3(1.2f, 0.1f, 2.2f);
            rampDown.transform.localRotation = Quaternion.Euler(11f, 0f, 0f);
            rampDown.GetComponent<Renderer>().sharedMaterial = turfMat;
            rampDown.GetComponent<BoxCollider>().material = turfPhys;

            CreateRampBumper(rampDown.transform, "Ramp_Down_Bumper_L", new Vector3(-0.64f, 0.11f, 0f), new Vector3(0.08f, 0.22f, 2.2f), woodMat, woodPhys);
            CreateRampBumper(rampDown.transform, "Ramp_Down_Bumper_R", new Vector3(0.64f, 0.11f, 0f), new Vector3(0.08f, 0.22f, 2.2f), woodMat, woodPhys);

            // 5. Isla del Hoyo con cierre perimetral
            CreateTurfSection(holeRoot.transform, "Green_CupIsland", new Vector3(0f, 0f, 11.2f), new Vector3(1.8f, 0.1f, 2.8f), turfMat, woodMat, turfPhys, woodPhys, hasLeftBorder: true, hasRightBorder: true, hasBackBorder: false, hasFrontBorder: true);

            // Tee Point
            GameObject tee = new GameObject("Tee_Point");
            tee.transform.SetParent(holeRoot.transform, false);
            tee.transform.position = holeRoot.transform.position + new Vector3(0f, 0.05f, 0.2f);
            CreateTeeMarker(tee.transform);

            // Cup
            Vector3 cupPos = holeRoot.transform.position + new Vector3(0f, 0.05f, 11.6f);
            GolfCup cup = CreateCup(holeRoot.transform, "Cup_Hole2", cupPos, holeData, flagMat);

            return new HoleRuntimeInstance
            {
                holeData = holeData,
                teePoint = tee.transform,
                cup = cup,
                playerSpawnPoint = tee.transform
            };
        }

        private static HoleRuntimeInstance BuildHole3(Transform parent, HoleDataSO holeData, Material turfMat, Material woodMat, Material flagMat, PhysicsMaterial turfPhys, PhysicsMaterial woodPhys)
        {
            GameObject holeRoot = new GameObject("Hole_03_MolinoPasaje");
            holeRoot.transform.SetParent(parent, false);
            holeRoot.transform.position = new Vector3(15f, 0f, 0f);

            // Fairway recto continuo con contencion perimetral completa (delimitado 100%)
            CreateTurfSection(holeRoot.transform, "Fairway_Windmill", new Vector3(0f, 0f, 5.5f), new Vector3(1.6f, 0.1f, 11.5f), turfMat, woodMat, turfPhys, woodPhys, hasLeftBorder: true, hasRightBorder: true, hasBackBorder: true, hasFrontBorder: true);

            // Molino Giratorio en el centro (Z = 5.5m)
            GameObject windmillBuilding = GameObject.CreatePrimitive(PrimitiveType.Cube);
            windmillBuilding.name = "Windmill_House";
            windmillBuilding.transform.SetParent(holeRoot.transform, false);
            windmillBuilding.transform.localPosition = new Vector3(0f, 1.2f, 5.5f);
            windmillBuilding.transform.localScale = new Vector3(1.8f, 1.8f, 0.5f);
            windmillBuilding.GetComponent<Renderer>().sharedMaterial = woodMat;

            // Tunel / Arco inferior de paso
            GameObject archPassage = new GameObject("Arch_Passage");
            archPassage.transform.SetParent(holeRoot.transform, false);
            archPassage.transform.localPosition = new Vector3(0f, 0.05f, 5.5f);

            // Aspas giratorias del molino
            GameObject rotor = new GameObject("Windmill_Blades_Rotor");
            rotor.transform.SetParent(windmillBuilding.transform, false);
            rotor.transform.localPosition = new Vector3(0f, -0.1f, -0.32f);

            // 4 Aspas cruzadas
            for (int i = 0; i < 4; i++)
            {
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = $"Blade_{i}";
                blade.transform.SetParent(rotor.transform, false);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, i * 90f);
                blade.transform.localPosition = blade.transform.up * 0.7f;
                blade.transform.localScale = new Vector3(0.18f, 1.1f, 0.06f);
                blade.GetComponent<Renderer>().sharedMaterial = woodMat;
                blade.GetComponent<BoxCollider>().material = woodPhys;
            }

            var obstacle = rotor.AddComponent<GolfObstacle>();
            var soObs = new SerializedObject(obstacle);
            soObs.FindProperty("type").enumValueIndex = (int)ObstacleType.RotatingWindmill;
            soObs.FindProperty("rotationAxis").vector3Value = new Vector3(0f, 0f, 1f);
            soObs.FindProperty("rotationSpeed").floatValue = 42f;
            soObs.ApplyModifiedProperties();

            // Tee Point
            GameObject tee = new GameObject("Tee_Point");
            tee.transform.SetParent(holeRoot.transform, false);
            tee.transform.position = holeRoot.transform.position + new Vector3(0f, 0.05f, 0.4f);
            CreateTeeMarker(tee.transform);

            // Cup
            Vector3 cupPos = holeRoot.transform.position + new Vector3(0f, 0.05f, 10.2f);
            GolfCup cup = CreateCup(holeRoot.transform, "Cup_Hole3", cupPos, holeData, flagMat);

            return new HoleRuntimeInstance
            {
                holeData = holeData,
                teePoint = tee.transform,
                cup = cup,
                playerSpawnPoint = tee.transform
            };
        }

        private static void CreateRampBumper(Transform parent, string name, Vector3 localPos, Vector3 scale, Material woodMat, PhysicsMaterial woodPhys)
        {
            GameObject b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = name;
            b.tag = "WoodBumper";
            b.transform.SetParent(parent, false);
            b.transform.localPosition = localPos;
            b.transform.localScale = scale;
            b.transform.localRotation = Quaternion.identity;
            b.GetComponent<Renderer>().sharedMaterial = woodMat;
            b.GetComponent<BoxCollider>().material = woodPhys;
        }

        private static void CreateTurfSection(Transform parent, string name, Vector3 localPos, Vector3 size, Material turfMat, Material woodMat, PhysicsMaterial turfPhys, PhysicsMaterial woodPhys, bool hasLeftBorder = true, bool hasRightBorder = true, bool hasBackBorder = false, bool hasFrontBorder = false)
        {
            GameObject section = new GameObject(name);
            section.transform.SetParent(parent, false);
            section.transform.localPosition = localPos;

            // Piso verde de cesped
            GameObject turf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            turf.name = "Turf_Floor";
            turf.transform.SetParent(section.transform, false);
            turf.transform.localScale = size;
            turf.GetComponent<Renderer>().sharedMaterial = turfMat;
            turf.GetComponent<BoxCollider>().material = turfPhys;

            float borderHeight = 0.22f; // Altura reforzada a 22 cm para contencion de rebotes
            float borderThickness = 0.08f;

            // Borde Izquierdo de madera
            if (hasLeftBorder)
            {
                GameObject leftBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leftBorder.name = "Bumper_Left";
                leftBorder.tag = "WoodBumper";
                leftBorder.transform.SetParent(section.transform, false);
                leftBorder.transform.localPosition = new Vector3(-size.x * 0.5f - borderThickness * 0.5f, borderHeight * 0.5f, 0f);
                leftBorder.transform.localScale = new Vector3(borderThickness, borderHeight, size.z + borderThickness * 2f);
                leftBorder.GetComponent<Renderer>().sharedMaterial = woodMat;
                leftBorder.GetComponent<BoxCollider>().material = woodPhys;
            }

            // Borde Derecho de madera
            if (hasRightBorder)
            {
                GameObject rightBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rightBorder.name = "Bumper_Right";
                rightBorder.tag = "WoodBumper";
                rightBorder.transform.SetParent(section.transform, false);
                rightBorder.transform.localPosition = new Vector3(size.x * 0.5f + borderThickness * 0.5f, borderHeight * 0.5f, 0f);
                rightBorder.transform.localScale = new Vector3(borderThickness, borderHeight, size.z + borderThickness * 2f);
                rightBorder.GetComponent<Renderer>().sharedMaterial = woodMat;
                rightBorder.GetComponent<BoxCollider>().material = woodPhys;
            }

            // Borde Trasero de madera (detrás del Tee o inicio de tramo)
            if (hasBackBorder)
            {
                GameObject backBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                backBorder.name = "Bumper_Back";
                backBorder.tag = "WoodBumper";
                backBorder.transform.SetParent(section.transform, false);
                backBorder.transform.localPosition = new Vector3(0f, borderHeight * 0.5f, -size.z * 0.5f - borderThickness * 0.5f);
                backBorder.transform.localScale = new Vector3(size.x + borderThickness * 2f, borderHeight, borderThickness);
                backBorder.GetComponent<Renderer>().sharedMaterial = woodMat;
                backBorder.GetComponent<BoxCollider>().material = woodPhys;
            }

            // Borde Frontal de madera (cierre final detrás del hoyo)
            if (hasFrontBorder)
            {
                GameObject frontBorder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                frontBorder.name = "Bumper_Front";
                frontBorder.tag = "WoodBumper";
                frontBorder.transform.SetParent(section.transform, false);
                frontBorder.transform.localPosition = new Vector3(0f, borderHeight * 0.5f, size.z * 0.5f + borderThickness * 0.5f);
                frontBorder.transform.localScale = new Vector3(size.x + borderThickness * 2f, borderHeight, borderThickness);
                frontBorder.GetComponent<Renderer>().sharedMaterial = woodMat;
                frontBorder.GetComponent<BoxCollider>().material = woodPhys;
            }
        }

        private static void CreateTeeMarker(Transform parent)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "Tee_Pad_Visual";
            marker.transform.SetParent(parent, false);
            marker.transform.localScale = new Vector3(0.24f, 0.005f, 0.24f);
            Object.DestroyImmediate(marker.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.color = new Color(1f, 0.85f, 0.1f, 0.9f); // Circulo dorado de salida
            marker.GetComponent<Renderer>().sharedMaterial = mat;
        }

        private static GolfCup CreateCup(Transform parent, string name, Vector3 worldPos, HoleDataSO holeData, Material flagMat)
        {
            GameObject cupObj = new GameObject(name);
            cupObj.transform.SetParent(parent, true);
            cupObj.transform.position = worldPos;

            // Trigger del hoyo
            var sphereCol = cupObj.AddComponent<SphereCollider>();
            sphereCol.isTrigger = true;
            sphereCol.radius = 0.11f;

            // Vaso interior visual
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Cup_Rim";
            rim.transform.SetParent(cupObj.transform, false);
            rim.transform.localScale = new Vector3(0.18f, 0.06f, 0.18f);
            rim.transform.localPosition = new Vector3(0f, -0.03f, 0f);
            Object.DestroyImmediate(rim.GetComponent<Collider>());
            var rimMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            rimMat.color = new Color(0.1f, 0.1f, 0.1f, 1f); // Interior oscuro del hoyo
            rim.GetComponent<Renderer>().sharedMaterial = rimMat;

            // Mastil de la bandera
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Flag_Pole";
            pole.transform.SetParent(cupObj.transform, false);
            pole.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            pole.transform.localScale = new Vector3(0.015f, 0.75f, 0.015f);
            Object.DestroyImmediate(pole.GetComponent<Collider>());

            // Bandera triangular roja
            GameObject flag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flag.name = "Flag_Cloth";
            flag.transform.SetParent(cupObj.transform, false);
            flag.transform.localPosition = new Vector3(0.15f, 1.35f, 0f);
            flag.transform.localScale = new Vector3(0.28f, 0.2f, 0.01f);
            Object.DestroyImmediate(flag.GetComponent<Collider>());
            flag.GetComponent<Renderer>().sharedMaterial = flagMat;

            var cupComp = cupObj.AddComponent<GolfCup>();
            var so = new SerializedObject(cupComp);
            so.FindProperty("holeData").objectReferenceValue = holeData;
            so.FindProperty("cupCenter").objectReferenceValue = rim.transform;
            so.ApplyModifiedProperties();

            return cupComp;
        }

        private static GameObject CreateScoreboard(Transform parent)
        {
            GameObject boardObj = new GameObject("Scoreboard_WorldSpace");
            boardObj.transform.SetParent(parent, false);
            boardObj.transform.position = new Vector3(-2.2f, 1.6f, 1.8f);
            boardObj.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

            Canvas canvas = boardObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rect = boardObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(900f, 650f);
            rect.localScale = Vector3.one * 0.0018f;

            boardObj.AddComponent<CanvasScaler>();
            boardObj.AddComponent<GraphicRaycaster>();

            // Fondo de madera del tablero
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(boardObj.transform, false);
            var bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.12f, 0.16f, 0.2f, 0.94f);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Titulo del Hoyo
            var title = CreateText(boardObj.transform, "TitleText", new Vector2(0f, 220f), new Vector2(850f, 65f), "HOYO 1: CURVA VERDE", 42, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.1f));

            // Par
            var par = CreateText(boardObj.transform, "ParText", new Vector2(0f, 140f), new Vector2(850f, 50f), "PAR: 2  |  MAX: 5", 34, TextAlignmentOptions.Center, Color.white);

            // Golpes actuales
            var strokes = CreateText(boardObj.transform, "StrokesText", new Vector2(0f, 50f), new Vector2(850f, 60f), "GOLPES EN ESTE HOYO: 0", 40, TextAlignmentOptions.Center, new Color(0f, 0.9f, 1f));

            // Total Score
            var total = CreateText(boardObj.transform, "TotalText", new Vector2(0f, -40f), new Vector2(850f, 55f), "TOTAL CIRCUITO: 0", 38, TextAlignmentOptions.Center, new Color(1f, 0.75f, 0.2f));

            // Record historico
            var record = CreateText(boardObj.transform, "RecordText", new Vector2(0f, -130f), new Vector2(850f, 50f), "RECORD HISTORICO: --", 28, TextAlignmentOptions.Center, new Color(0.75f, 0.75f, 0.75f));

            // Boton Reiniciar Circuito
            GameObject btnObj = new GameObject("Btn_Restart");
            btnObj.transform.SetParent(boardObj.transform, false);
            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchoredPosition = new Vector2(0f, -220f);
            btnRect.sizeDelta = new Vector2(380f, 65f);
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0.2f, 0.45f, 0.25f);
            var btn = btnObj.AddComponent<Button>();
            CreateText(btnObj.transform, "BtnText", Vector2.zero, new Vector2(380f, 65f), "REINICIAR CIRCUITO", 28, TextAlignmentOptions.Center, Color.white);

            // Banner Flotante para eventos (Birdie, Out of bounds, etc.)
            GameObject bannerObj = new GameObject("BannerPanel");
            bannerObj.transform.SetParent(boardObj.transform, false);
            var bannerRect = bannerObj.AddComponent<RectTransform>();
            bannerRect.anchoredPosition = new Vector2(0f, 0f);
            bannerRect.sizeDelta = new Vector2(880f, 240f);
            var bannerImg = bannerObj.AddComponent<Image>();
            bannerImg.color = new Color(0.05f, 0.08f, 0.12f, 0.98f);

            var bannerTitle = CreateText(bannerObj.transform, "BannerTitle", new Vector2(0f, 45f), new Vector2(850f, 80f), "¡HOLE IN ONE!", 56, TextAlignmentOptions.Center, Color.yellow);
            var bannerSub = CreateText(bannerObj.transform, "BannerSub", new Vector2(0f, -40f), new Vector2(850f, 60f), "¡Tiro perfecto al primer golpe!", 32, TextAlignmentOptions.Center, Color.white);

            var uiComp = boardObj.AddComponent<GolfScoreboardUI>();
            var so = new SerializedObject(uiComp);
            so.FindProperty("holeTitleText").objectReferenceValue = title;
            so.FindProperty("parText").objectReferenceValue = par;
            so.FindProperty("currentStrokesText").objectReferenceValue = strokes;
            so.FindProperty("totalScoreText").objectReferenceValue = total;
            so.FindProperty("recordText").objectReferenceValue = record;
            so.FindProperty("bannerPanel").objectReferenceValue = bannerObj;
            so.FindProperty("bannerTitleText").objectReferenceValue = bannerTitle;
            so.FindProperty("bannerSubtitleText").objectReferenceValue = bannerSub;
            so.FindProperty("restartButton").objectReferenceValue = btn;
            so.ApplyModifiedProperties();

            return boardObj;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, Vector2 pos, Vector2 size, string text, float fontSize, TextAlignmentOptions align, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            return tmp;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath) return; // Ya existe
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[GolfSceneBuilder] Escena agregada a Build Settings: {scenePath}");
        }
    }
}
