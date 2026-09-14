#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ShootingRangeBuilder : EditorWindow
{
    [MenuItem("VR Sports/Construir Campo de Tiro Deportivo (3 Carriles + 9 Dianas)")]
    public static void BuildShootingRange()
    {
        // 1. Cargar ScriptableObjects de puntuación (10m, 20m, 35m, Bullseye)
        ScoreEventData closeHitSO = AssetDatabase.LoadAssetAtPath<ScoreEventData>("Assets/_SO/Shooting_CloseHit.asset");
        ScoreEventData midHitSO = AssetDatabase.LoadAssetAtPath<ScoreEventData>("Assets/_SO/Shooting_MidHit.asset");
        ScoreEventData farHitSO = AssetDatabase.LoadAssetAtPath<ScoreEventData>("Assets/_SO/Shooting_FarHit.asset");
        ScoreEventData bullseyeSO = AssetDatabase.LoadAssetAtPath<ScoreEventData>("Assets/_SO/Shooting_Bullseye.asset");
        MinigameData shootingModeSO = AssetDatabase.LoadAssetAtPath<MinigameData>("Assets/_SO/ShootingMode.asset");

        // 2. Cargar Modelos 3D FBX de forma segura
        GameObject dianaFBX = LoadFBXModel("Meshy_AI_Archery_Target_0914022118_texture");
        GameObject rifleFBX = LoadFBXModel("Meshy_AI_Wooden_Rifle_0914021154_texture");

        Material dianaMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/04_Materials/Mat_Diana_3D.mat");
        Material rifleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/04_Materials/Mat_Rifle_Wood.mat");

        // 3. Limpieza de elementos viejos
        CleanOldSceneObjects();

        // 4. Crear / Reemplazar el GameObject 'Environment'
        GameObject env = GameObject.Find("Environment");
        if (env != null)
        {
            Undo.DestroyObjectImmediate(env);
        }
        env = new GameObject("Environment");
        Undo.RegisterCreatedObjectUndo(env, "Construir Environment Campo de Tiro");

        // Materiales
        Material matFloor = CreateColorMaterial("Mat_Range_Floor", new Color(0.20f, 0.22f, 0.20f));
        Material matWalls = CreateColorMaterial("Mat_Range_Walls", new Color(0.24f, 0.26f, 0.28f));
        Material matBackstop = CreateColorMaterial("Mat_Range_Backstop", new Color(0.12f, 0.12f, 0.13f));
        Material matTable = CreateColorMaterial("Mat_Range_Table", new Color(0.35f, 0.24f, 0.16f));
        Material matBooth = CreateColorMaterial("Mat_Range_Booth", new Color(0.18f, 0.22f, 0.26f));
        Material matStand = CreateColorMaterial("Mat_Range_Stand", new Color(0.10f, 0.10f, 0.10f));
        Material matLaneLines = CreateColorMaterial("Mat_Range_LaneLine", new Color(0.90f, 0.60f, 0.15f));

        // 5. Galería de Tiro Abierta y Proporcional (45m)
        GameObject room = new GameObject("Room");
        room.transform.SetParent(env.transform);

        // Suelo principal (14m ancho x 48m largo)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor_45m";
        floor.transform.SetParent(room.transform);
        floor.transform.position = new Vector3(0, -0.05f, 21.0f);
        floor.transform.localScale = new Vector3(14.0f, 0.1f, 48.0f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = matFloor;

        // Parabalas balístico de contención al fondo (a 42m)
        GameObject backstop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backstop.name = "Bullet_Backstop_42m";
        backstop.transform.SetParent(room.transform);
        backstop.transform.position = new Vector3(0, 4.0f, 42.0f);
        backstop.transform.localScale = new Vector3(14.0f, 8.0f, 1.0f);
        backstop.GetComponent<MeshRenderer>().sharedMaterial = matBackstop;

        // Pared Trasera (detrás del jugador a -3m)
        GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backWall.name = "Back_Wall";
        backWall.transform.SetParent(room.transform);
        backWall.transform.position = new Vector3(0, 3.5f, -3.0f);
        backWall.transform.localScale = new Vector3(14.0f, 7.0f, 0.5f);
        backWall.GetComponent<MeshRenderer>().sharedMaterial = matWalls;

        // Paredes Laterales
        GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.name = "Left_Wall";
        leftWall.transform.SetParent(room.transform);
        leftWall.transform.position = new Vector3(-7.0f, 3.5f, 21.0f);
        leftWall.transform.localScale = new Vector3(0.5f, 7.0f, 48.0f);
        leftWall.GetComponent<MeshRenderer>().sharedMaterial = matWalls;

        GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.name = "Right_Wall";
        rightWall.transform.SetParent(room.transform);
        rightWall.transform.position = new Vector3(7.0f, 3.5f, 21.0f);
        rightWall.transform.localScale = new Vector3(0.5f, 7.0f, 48.0f);
        rightWall.GetComponent<MeshRenderer>().sharedMaterial = matWalls;

        // Líneas divisoras sutiles de carril en el suelo
        CreatePillar("LaneLine_L", room.transform, new Vector3(-2.4f, 0.01f, 21.0f), new Vector3(0.08f, 0.02f, 42.0f), matLaneLines);
        CreatePillar("LaneLine_R", room.transform, new Vector3(2.4f, 0.01f, 21.0f), new Vector3(0.08f, 0.02f, 42.0f), matLaneLines);

        // 6. Puesto del Jugador (Mesa Limpia, Sin Bloques Negros Ni Paredes Laterales)
        GameObject booth = new GameObject("PlayerBooth");
        booth.transform.SetParent(env.transform);
        booth.transform.position = Vector3.zero;

        // Alfombra/delimitador de suelo del puesto
        GameObject stallMat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stallMat.name = "Stall_Mat";
        stallMat.transform.SetParent(booth.transform);
        stallMat.transform.position = new Vector3(0, 0.01f, 0);
        stallMat.transform.localScale = new Vector3(2.4f, 0.02f, 2.0f);
        stallMat.GetComponent<MeshRenderer>().sharedMaterial = matBooth;
        stallMat.AddComponent<ShootingBoothBoundary>().boothIndicatorRenderer = stallMat.GetComponent<MeshRenderer>();

        // Mesa de tiro frontal amplia y limpia (Altura 0.85m)
        GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "Shooting_Table";
        table.transform.SetParent(booth.transform);
        table.transform.position = new Vector3(0, 0.85f, 0.70f);
        table.transform.localScale = new Vector3(2.2f, 0.06f, 0.55f);
        table.GetComponent<MeshRenderer>().sharedMaterial = matTable;

        // Patas de mesa delgadas
        CreatePillar("Table_Leg_L", booth.transform, new Vector3(-1.0f, 0.425f, 0.70f), new Vector3(0.06f, 0.85f, 0.40f), matStand);
        CreatePillar("Table_Leg_R", booth.transform, new Vector3(1.0f, 0.425f, 0.70f), new Vector3(0.06f, 0.85f, 0.40f), matStand);

        // 7. Configuración de 3 Carriles × 3 Distancias (10m, 20m, 35m = 9 Dianas 3D)
        GameObject lanesRoot = new GameObject("Lanes");
        lanesRoot.transform.SetParent(env.transform);

        float[] laneX = new float[] { -3.8f, 0.0f, 3.8f };
        string[] laneNames = new string[] { "Lane_1_Left", "Lane_2_Center", "Lane_3_Right" };
        float[] distances = new float[] { 10.0f, 20.0f, 35.0f };
        float[] targetScales = new float[] { 0.9f, 1.15f, 1.4f };
        string[] distNames = new string[] { "Close_10m", "Mid_20m", "Far_35m" };
        ScoreEventData[] scores = new ScoreEventData[] { closeHitSO, midHitSO, farHitSO };
        TargetType[] targetTypes = new TargetType[] { TargetType.Close, TargetType.Medium, TargetType.Far };

        for (int l = 0; l < 3; l++)
        {
            GameObject laneObj = new GameObject(laneNames[l]);
            laneObj.transform.SetParent(lanesRoot.transform);

            for (int d = 0; d < 3; d++)
            {
                Vector3 targetPos = new Vector3(laneX[l], 1.55f, distances[d]);

                Create3DTargetInstance(
                    laneObj.transform,
                    targetPos,
                    $"Target_{distNames[d]}",
                    targetTypes[d],
                    scores[d],
                    bullseyeSO,
                    dianaFBX,
                    dianaMat,
                    matStand,
                    targetScales[d]
                );
            }
        }

        // 8. Instanciar los Rifles 3D Reales directamente sobre la mesa
        // Rifle 1: Ruger 10/22 LR (Semiautomático)
        CreateRifleWeapon(
            booth.transform,
            new Vector3(-0.35f, 0.90f, 0.68f),
            "Rifle_Ruger_1022LR",
            RifleType.SemiAutomatic,
            rifleFBX,
            rifleMat
        );

        // Rifle 2: Rifle de Cerrojo (Bolt Action)
        CreateRifleWeapon(
            booth.transform,
            new Vector3(0.35f, 0.90f, 0.68f),
            "Rifle_BoltAction_Custom",
            RifleType.BoltAction,
            rifleFBX,
            rifleMat
        );

        // 9. Reposicionar el Canvas de puntuación / tiempo en la pared frontal superior
        PositionCanvasScoreboard();

        // 10. Manager del Campo de Tiro
        ShootingRangeManager manager = env.AddComponent<ShootingRangeManager>();
        manager.targets = env.GetComponentsInChildren<ShootingTarget>();

        GameSession session = Object.FindFirstObjectByType<GameSession>();
        if (session != null && shootingModeSO != null)
        {
            session.currentMinigame = shootingModeSO;
            EditorUtility.SetDirty(session);
        }

        Selection.activeGameObject = env;
        Debug.Log("✅ ¡Campo de Tiro configurado a 10m, 20m, 35m con modelos 3D de Rifle y Dianas!");
    }

    private static void CreateRifleWeapon(
        Transform boothTransform,
        Vector3 worldPos,
        string weaponName,
        RifleType rifleType,
        GameObject fbxPrefab,
        Material rifleMat)
    {
        GameObject existing = GameObject.Find(weaponName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        GameObject rifleRoot = new GameObject(weaponName);
        rifleRoot.transform.SetParent(boothTransform);
        rifleRoot.transform.position = worldPos;
        rifleRoot.transform.rotation = Quaternion.identity;

        Rigidbody rb = rifleRoot.AddComponent<Rigidbody>();
        rb.mass = 2.4f;
        rb.linearDamping = 1.0f;
        rb.angularDamping = 1.0f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        GameObject modelRoot = new GameObject("Model_Root");
        modelRoot.transform.SetParent(rifleRoot.transform);
        modelRoot.transform.localPosition = Vector3.zero;
        modelRoot.transform.localRotation = Quaternion.identity;

        // Instanciar el modelo FBX 3D
        if (fbxPrefab != null)
        {
            GameObject meshInstance = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
            meshInstance.name = "Rifle_Mesh_Model";
            meshInstance.transform.SetParent(modelRoot.transform);
            meshInstance.transform.localPosition = Vector3.zero;
            meshInstance.transform.localRotation = Quaternion.Euler(0, -90f, 0);
            meshInstance.transform.localScale = Vector3.one * 0.85f;
            meshInstance.SetActive(true);

            if (rifleMat != null)
            {
                MeshRenderer[] renderers = meshInstance.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in renderers)
                {
                    r.sharedMaterial = rifleMat;
                }
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontró el FBX del rifle. Verificando rutas de assets...");
        }

        // Ventana de expulsión de casquillos al costado derecho
        GameObject shellPoint = new GameObject("Shell_Ejection_Point");
        shellPoint.transform.SetParent(modelRoot.transform);
        shellPoint.transform.localPosition = new Vector3(0.045f, 0.045f, 0.04f);

        // BoxCollider para agarrar el rifle
        BoxCollider boxCol = rifleRoot.AddComponent<BoxCollider>();
        boxCol.size = new Vector3(0.12f, 0.22f, 0.88f);
        boxCol.center = new Vector3(0, 0.02f, 0.15f);

        // AttachPoint (Punto de agarre ergonómico con miras alineadas naturalmente hacia el frente)
        GameObject attachPointObj = new GameObject("AttachPoint_Grip");
        attachPointObj.transform.SetParent(rifleRoot.transform);
        attachPointObj.transform.localPosition = new Vector3(0.0f, -0.04f, -0.06f);
        attachPointObj.transform.localRotation = Quaternion.Euler(0, -90f, 0);

        // Cañón y Miras de Hierro apuntando al frente (+Z)
        GameObject muzzleObj = new GameObject("MuzzlePoint");
        muzzleObj.transform.SetParent(modelRoot.transform);
        muzzleObj.transform.localPosition = new Vector3(0.0f, 0.045f, 0.68f);
        muzzleObj.transform.localRotation = Quaternion.identity;

        GameObject rearSightObj = new GameObject("RearSight");
        rearSightObj.transform.SetParent(modelRoot.transform);
        rearSightObj.transform.localPosition = new Vector3(0.0f, 0.062f, 0.02f);
        rearSightObj.transform.localRotation = Quaternion.identity;

        GameObject frontSightObj = new GameObject("FrontSight");
        frontSightObj.transform.SetParent(modelRoot.transform);
        frontSightObj.transform.localPosition = new Vector3(0.0f, 0.062f, 0.65f);
        frontSightObj.transform.localRotation = Quaternion.identity;

        // Trazador de bala
        LineRenderer tracer = rifleRoot.AddComponent<LineRenderer>();
        tracer.enabled = false;
        tracer.startWidth = 0.015f;
        tracer.endWidth = 0.008f;
        tracer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
        tracer.startColor = Color.yellow;
        tracer.endColor = new Color(1f, 0.45f, 0f, 0f);

        // XRGrabInteractable
        XRGrabInteractable grab = rifleRoot.AddComponent<XRGrabInteractable>();
        grab.attachTransform = attachPointObj.transform;
        grab.useDynamicAttach = false;
        grab.matchAttachPosition = true;
        grab.matchAttachRotation = true;
        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.throwOnDetach = true;

        // Script VRRifle
        VRRifle rifleComp = rifleRoot.AddComponent<VRRifle>();
        rifleComp.rifleType = rifleType;
        rifleComp.muzzlePoint = muzzleObj.transform;
        rifleComp.rearSight = rearSightObj.transform;
        rifleComp.frontSight = frontSightObj.transform;
        rifleComp.attachPoint = attachPointObj.transform;
        rifleComp.shellEjectionPoint = shellPoint.transform;
        rifleComp.modelRoot = modelRoot.transform;
        rifleComp.tracerLineRenderer = tracer;
        rifleComp.maxRange = 200f;
        rifleComp.magazineCapacity = rifleType == RifleType.SemiAutomatic ? 10 : 5;
        rifleComp.infiniteAmmo = true;

        Undo.RegisterCreatedObjectUndo(rifleRoot, $"Crear {weaponName}");
    }

    private static void Create3DTargetInstance(
        Transform parent,
        Vector3 position,
        string name,
        TargetType type,
        ScoreEventData scoreSO,
        ScoreEventData bullseyeSO,
        GameObject fbxPrefab,
        Material dianaMat,
        Material matStand,
        float targetScale)
    {
        GameObject targetRoot = new GameObject(name);
        targetRoot.transform.SetParent(parent);
        targetRoot.transform.position = position;

        // Poste vertical de soporte
        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stand.name = "Pole";
        stand.transform.SetParent(targetRoot.transform);
        float poleHeight = position.y;
        stand.transform.position = new Vector3(position.x, poleHeight / 2f, position.z);
        stand.transform.localScale = new Vector3(0.08f * targetScale, poleHeight / 2f, 0.08f * targetScale);
        stand.GetComponent<MeshRenderer>().sharedMaterial = matStand;

        // Base redonda
        GameObject standBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        standBase.name = "Base";
        standBase.transform.SetParent(targetRoot.transform);
        standBase.transform.position = new Vector3(position.x, 0.04f, position.z);
        standBase.transform.localScale = new Vector3(0.7f * targetScale, 0.04f, 0.7f * targetScale);
        standBase.GetComponent<MeshRenderer>().sharedMaterial = matStand;

        // Diana 3D
        GameObject model;
        if (fbxPrefab != null)
        {
            model = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
            model.name = "Diana_Mesh";
            model.transform.SetParent(targetRoot.transform);
            model.transform.position = position;
            model.transform.rotation = Quaternion.Euler(0, 180, 0);
            model.transform.localScale = Vector3.one * targetScale;
            model.SetActive(true);

            if (dianaMat != null)
            {
                MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var r in renderers)
                {
                    r.sharedMaterial = dianaMat;
                }
            }
        }
        else
        {
            model = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            model.name = "Diana_Procedural";
            model.transform.SetParent(targetRoot.transform);
            model.transform.position = position;
            model.transform.rotation = Quaternion.Euler(90, 0, 0);
            model.transform.localScale = new Vector3(0.8f * targetScale, 0.03f, 0.8f * targetScale);
        }

        BoxCollider boxCol = model.GetComponent<BoxCollider>();
        if (boxCol == null)
        {
            boxCol = model.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(1.0f, 1.0f, 0.2f);
        }

        ShootingTarget st = model.AddComponent<ShootingTarget>();
        st.targetType = type;
        st.scoreEventData = scoreSO;
        st.bullseyeEventData = bullseyeSO;
        st.targetRenderer = model.GetComponentInChildren<MeshRenderer>();
        st.reaction = TargetReaction.Wobble;
    }

    private static GameObject LoadFBXModel(string fbxName)
    {
        string[] directPaths = new string[]
        {
            $"Assets/03_Resources/{fbxName}.fbx",
            $"Assets/03_Resources/Rugger 1022LR/{fbxName}.fbx",
            $"Assets/03_Resources/Diana/{fbxName}.fbx"
        };

        foreach (string path in directPaths)
        {
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go != null) return go;
        }

        string[] guids = AssetDatabase.FindAssets($"{fbxName} t:Model");
        if (guids != null && guids.Length > 0)
        {
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (go != null) return go;
                }
            }
        }

        return null;
    }

    private static void CleanOldSceneObjects()
    {
        string[] namesToDelete = new string[] {
            "[SHOOTING_RANGE_ENVIRONMENT]",
            "Meshy_AI_A_basketball_hoop_wit_0913221157_texture",
            "Meshy_AI_Realistic_orange_bask_0913221210_texture",
            "VR_Shooting_Blaster",
            "Rifle_Ruger_1022LR",
            "Rifle_BoltAction_Custom"
        };

        foreach (string n in namesToDelete)
        {
            GameObject obj = GameObject.Find(n);
            if (obj != null) Undo.DestroyObjectImmediate(obj);
        }

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var go in allObjects)
        {
            if (go != null && (go.name.Contains("basketball") || go.name.Contains("hoop") || go.name.Contains("Blaster")))
            {
                Undo.DestroyObjectImmediate(go);
            }
        }
    }

    private static void PositionCanvasScoreboard()
    {
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj != null)
        {
            canvasObj.transform.position = new Vector3(0, 4.2f, 12.0f);
            canvasObj.transform.rotation = Quaternion.Euler(0, 0, 0);
            canvasObj.transform.localScale = Vector3.one * 0.015f;
            Undo.RecordObject(canvasObj.transform, "Reposicionar Canvas Scoreboard");
        }
    }

    private static GameObject CreatePillar(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        if (mat != null) obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return obj;
    }

    private static Material CreateColorMaterial(string matName, Color color)
    {
        string path = $"Assets/04_Materials/{matName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader standardShader = Shader.Find("Universal Render Pipeline/Lit");
            if (standardShader == null) standardShader = Shader.Find("Standard");

            mat = new Material(standardShader);
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            mat.color = color;
        }
        return mat;
    }
}
#endif
