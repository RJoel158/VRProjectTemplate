#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ShootingRangeBuilder : EditorWindow
{
    [MenuItem("VR Sports/Construir Campo de Tiro Deportivo (3 Carriles + 9 Dianas)")]
    public static void BuildShootingRange()
    {
        // 1. Cargar ScriptableObjects de puntuación
        ScoreEventData closeHitSO = AssetDatabase.LoadAssetAtPath<ScoreEventData>("Assets/_SO/Shooting_CloseHit.asset");
        ScoreEventData midHitSO = AssetDatabase.LoadAssetAtPath<ScoreEventData>("Assets/_SO/Shooting_MidHit.asset");
        ScoreEventData farHitSO = AssetDatabase.LoadAssetAtPath<ScoreEventData>("Assets/_SO/Shooting_FarHit.asset");
        ScoreEventData bullseyeSO = AssetDatabase.LoadAssetAtPath<ScoreEventData>("Assets/_SO/Shooting_Bullseye.asset");
        MinigameData shootingModeSO = AssetDatabase.LoadAssetAtPath<MinigameData>("Assets/_SO/ShootingMode.asset");

        // 2. Cargar Modelo 3D de Diana y Texturas
        GameObject dianaFBX = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Resources/Diana/Meshy_AI_Archery_Target_0914022118_texture.fbx");
        Texture2D dianaBaseTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/03_Resources/Diana/Meshy_AI_Archery_Target_0914022118_texture.png");
        Texture2D dianaNormalTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/03_Resources/Diana/Meshy_AI_Archery_Target_0914022118_texture_normal.png");
        Material dianaCustomMat = CreateDianaMaterial(dianaBaseTex, dianaNormalTex);

        // 3. Limpiar elementos antiguos de Basket o builds anteriores
        CleanOldSceneObjects();

        // 4. Buscar o Crear el GameObject 'Environment' principal
        GameObject env = GameObject.Find("Environment");
        if (env != null)
        {
            Undo.DestroyObjectImmediate(env);
        }
        env = new GameObject("Environment");
        Undo.RegisterCreatedObjectUndo(env, "Construir Environment de Campo de Tiro");

        // Materiales del entorno
        Material matFloor = CreateColorMaterial("Mat_Range_Floor", new Color(0.20f, 0.22f, 0.20f));
        Material matWalls = CreateColorMaterial("Mat_Range_Walls", new Color(0.25f, 0.27f, 0.30f));
        Material matBackstop = CreateColorMaterial("Mat_Range_Backstop", new Color(0.12f, 0.12f, 0.13f));
        Material matTable = CreateColorMaterial("Mat_Range_Table", new Color(0.32f, 0.22f, 0.15f));
        Material matBooth = CreateColorMaterial("Mat_Range_Booth", new Color(0.18f, 0.20f, 0.24f));
        Material matStand = CreateColorMaterial("Mat_Range_Stand", new Color(0.10f, 0.10f, 0.10f));
        Material matLaneLines = CreateColorMaterial("Mat_Range_LaneLine", new Color(0.85f, 0.55f, 0.15f));

        // 5. Estructura de la Galería de Tiro (Room)
        GameObject room = new GameObject("Room");
        room.transform.SetParent(env.transform);

        // Suelo principal (Galería de 12m ancho x 24m largo)
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(room.transform);
        floor.transform.position = new Vector3(0, -0.05f, 10.0f);
        floor.transform.localScale = new Vector3(12.0f, 0.1f, 24.0f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = matFloor;

        // Pared de Fondo (Backstop balístico a 22m)
        GameObject backstopWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backstopWall.name = "Backstop_Wall";
        backstopWall.transform.SetParent(room.transform);
        backstopWall.transform.position = new Vector3(0, 3.0f, 22.0f);
        backstopWall.transform.localScale = new Vector3(12.0f, 6.0f, 0.5f);
        backstopWall.GetComponent<MeshRenderer>().sharedMaterial = matBackstop;

        // Pared Trasera (detrás del jugador a -2m)
        GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backWall.name = "Back_Wall";
        backWall.transform.SetParent(room.transform);
        backWall.transform.position = new Vector3(0, 3.0f, -2.0f);
        backWall.transform.localScale = new Vector3(12.0f, 6.0f, 0.5f);
        backWall.GetComponent<MeshRenderer>().sharedMaterial = matWalls;

        // Pared Izquierda
        GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWall.name = "Left_Wall";
        leftWall.transform.SetParent(room.transform);
        leftWall.transform.position = new Vector3(-6.0f, 3.0f, 10.0f);
        leftWall.transform.localScale = new Vector3(0.5f, 6.0f, 24.0f);
        leftWall.GetComponent<MeshRenderer>().sharedMaterial = matWalls;

        // Pared Derecha
        GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWall.name = "Right_Wall";
        rightWall.transform.SetParent(room.transform);
        rightWall.transform.position = new Vector3(6.0f, 3.0f, 10.0f);
        rightWall.transform.localScale = new Vector3(0.5f, 6.0f, 24.0f);
        rightWall.GetComponent<MeshRenderer>().sharedMaterial = matWalls;

        // Líneas divisoras de carril en el suelo (discretas, limpias)
        CreatePillar("LaneLine_Left", room.transform, new Vector3(-1.8f, 0.01f, 10.5f), new Vector3(0.08f, 0.02f, 21.0f), matLaneLines);
        CreatePillar("LaneLine_Right", room.transform, new Vector3(1.8f, 0.01f, 10.5f), new Vector3(0.08f, 0.02f, 21.0f), matLaneLines);

        // 6. Puesto del Jugador (Shooting Booth)
        GameObject booth = new GameObject("PlayerBooth");
        booth.transform.SetParent(env.transform);
        booth.transform.position = Vector3.zero;

        // Alfombra/delimitador de suelo del puesto
        GameObject stallMat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stallMat.name = "Stall_Mat";
        stallMat.transform.SetParent(booth.transform);
        stallMat.transform.position = new Vector3(0, 0.01f, 0);
        stallMat.transform.localScale = new Vector3(2.0f, 0.02f, 1.8f);
        stallMat.GetComponent<MeshRenderer>().sharedMaterial = matBooth;
        stallMat.AddComponent<ShootingBoothBoundary>().boothIndicatorRenderer = stallMat.GetComponent<MeshRenderer>();

        // Mesa de tiro frontal (Altura ergonómica 0.85m)
        GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "Shooting_Table";
        table.transform.SetParent(booth.transform);
        table.transform.position = new Vector3(0, 0.85f, 0.75f);
        table.transform.localScale = new Vector3(1.8f, 0.08f, 0.5f);
        table.GetComponent<MeshRenderer>().sharedMaterial = matTable;

        // Patas de mesa
        CreatePillar("Table_Leg_L", booth.transform, new Vector3(-0.8f, 0.425f, 0.75f), new Vector3(0.08f, 0.85f, 0.4f), matStand);
        CreatePillar("Table_Leg_R", booth.transform, new Vector3(0.8f, 0.425f, 0.75f), new Vector3(0.08f, 0.85f, 0.4f), matStand);

        // Paneles laterales de la cabina
        CreatePillar("Booth_Divider_L", booth.transform, new Vector3(-1.0f, 1.1f, 0.2f), new Vector3(0.08f, 2.2f, 1.5f), matBooth);
        CreatePillar("Booth_Divider_R", booth.transform, new Vector3(1.0f, 1.1f, 0.2f), new Vector3(0.08f, 2.2f, 1.5f), matBooth);

        // 7. 3 Carriles × 3 Distancias (9 Dianas distribuidas limpiamente)
        GameObject lanesRoot = new GameObject("Lanes");
        lanesRoot.transform.SetParent(env.transform);

        float[] laneX = new float[] { -3.2f, 0.0f, 3.2f };
        string[] laneNames = new string[] { "Lane_1_Left", "Lane_2_Center", "Lane_3_Right" };
        float[] distances = new float[] { 5.0f, 10.0f, 16.0f };
        string[] distNames = new string[] { "Close_5m", "Mid_10m", "Far_16m" };
        ScoreEventData[] scores = new ScoreEventData[] { closeHitSO, midHitSO, farHitSO };
        TargetType[] targetTypes = new TargetType[] { TargetType.Close, TargetType.Medium, TargetType.Far };

        for (int l = 0; l < 3; l++)
        {
            GameObject laneObj = new GameObject(laneNames[l]);
            laneObj.transform.SetParent(lanesRoot.transform);

            for (int d = 0; d < 3; d++)
            {
                Vector3 targetPos = new Vector3(laneX[l], 1.45f, distances[d]);

                if (dianaFBX != null)
                {
                    Create3DTarget(laneObj.transform, targetPos, $"Target_{distNames[d]}", targetTypes[d], scores[d], bullseyeSO, dianaFBX, dianaCustomMat, matStand);
                }
                else
                {
                    CreateSimpleTarget(laneObj.transform, targetPos, $"Target_{distNames[d]}", targetTypes[d], scores[d], bullseyeSO, matStand);
                }
            }
        }

        // 8. Reposicionar el Canvas de puntuación / tiempo en la pared frontal sobre el backstop
        PositionCanvasScoreboard();

        // 9. Asegurar que GameManager y GameSession tengan el modo de disparo
        ShootingRangeManager manager = env.AddComponent<ShootingRangeManager>();
        manager.targets = env.GetComponentsInChildren<ShootingTarget>();

        GameSession session = Object.FindFirstObjectByType<GameSession>();
        if (session != null && shootingModeSO != null)
        {
            session.currentMinigame = shootingModeSO;
            EditorUtility.SetDirty(session);
        }

        // 10. Colocar Blaster o Rifle en la mesa si existen
        SpawnWeaponOnTable(booth.transform);

        Selection.activeGameObject = env;
        Debug.Log("✅ ¡Entorno limpio de Campo de Tiro configurado dentro de 'Environment'!");
    }

    private static void CleanOldSceneObjects()
    {
        // Eliminar basura previa
        string[] namesToDelete = new string[] {
            "[SHOOTING_RANGE_ENVIRONMENT]",
            "Meshy_AI_A_basketball_hoop_wit_0913221157_texture",
            "Meshy_AI_Realistic_orange_bask_0913221210_texture"
        };

        foreach (string n in namesToDelete)
        {
            GameObject obj = GameObject.Find(n);
            if (obj != null)
            {
                Undo.DestroyObjectImmediate(obj);
            }
        }

        // Buscar aros o balones residuales por nombre parcial
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (var go in allObjects)
        {
            if (go != null && (go.name.Contains("basketball") || go.name.Contains("hoop")))
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
            canvasObj.transform.position = new Vector3(0, 4.2f, 21.7f);
            canvasObj.transform.rotation = Quaternion.Euler(0, 0, 0);
            canvasObj.transform.localScale = Vector3.one * 0.015f;
            Undo.RecordObject(canvasObj.transform, "Reposicionar Canvas Scoreboard");
        }
    }

    private static void Create3DTarget(
        Transform parent,
        Vector3 position,
        string name,
        TargetType type,
        ScoreEventData scoreSO,
        ScoreEventData bullseyeSO,
        GameObject fbxPrefab,
        Material dianaMat,
        Material matStand)
    {
        GameObject targetRoot = new GameObject(name);
        targetRoot.transform.SetParent(parent);
        targetRoot.transform.position = position;

        // Poste vertical
        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stand.name = "Pole";
        stand.transform.SetParent(targetRoot.transform);
        float poleHeight = position.y;
        stand.transform.position = new Vector3(position.x, poleHeight / 2f, position.z);
        stand.transform.localScale = new Vector3(0.06f, poleHeight / 2f, 0.06f);
        stand.GetComponent<MeshRenderer>().sharedMaterial = matStand;

        // Base redonda del poste
        GameObject standBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        standBase.name = "Base";
        standBase.transform.SetParent(targetRoot.transform);
        standBase.transform.position = new Vector3(position.x, 0.03f, position.z);
        standBase.transform.localScale = new Vector3(0.45f, 0.03f, 0.45f);
        standBase.GetComponent<MeshRenderer>().sharedMaterial = matStand;

        // Modelo 3D de la Diana
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
        model.name = "Diana_Mesh";
        model.transform.SetParent(targetRoot.transform);
        model.transform.position = position;
        model.transform.rotation = Quaternion.Euler(0, 180, 0);
        model.transform.localScale = Vector3.one * 0.7f;

        if (dianaMat != null)
        {
            MeshRenderer[] renderers = model.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renderers)
            {
                r.sharedMaterial = dianaMat;
            }
        }

        // Colisionador para registrar impacto
        BoxCollider boxCol = model.GetComponent<BoxCollider>();
        if (boxCol == null)
        {
            boxCol = model.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(0.85f, 0.85f, 0.15f);
        }

        // Script ShootingTarget
        ShootingTarget st = model.AddComponent<ShootingTarget>();
        st.targetType = type;
        st.scoreEventData = scoreSO;
        st.bullseyeEventData = bullseyeSO;
        st.targetRenderer = model.GetComponentInChildren<MeshRenderer>();
        st.reaction = TargetReaction.Wobble;
    }

    private static void CreateSimpleTarget(
        Transform parent,
        Vector3 position,
        string name,
        TargetType type,
        ScoreEventData scoreSO,
        ScoreEventData bullseyeSO,
        Material matStand)
    {
        GameObject targetRoot = new GameObject(name);
        targetRoot.transform.SetParent(parent);
        targetRoot.transform.position = position;

        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stand.name = "Pole";
        stand.transform.SetParent(targetRoot.transform);
        stand.transform.position = new Vector3(position.x, position.y / 2f, position.z);
        stand.transform.localScale = new Vector3(0.06f, position.y / 2f, 0.06f);
        stand.GetComponent<MeshRenderer>().sharedMaterial = matStand;

        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = "Target_Disc";
        disc.transform.SetParent(targetRoot.transform);
        disc.transform.position = position;
        disc.transform.rotation = Quaternion.Euler(90f, 0, 0);
        disc.transform.localScale = new Vector3(0.6f, 0.02f, 0.6f);

        ShootingTarget st = disc.AddComponent<ShootingTarget>();
        st.targetType = type;
        st.scoreEventData = scoreSO;
        st.bullseyeEventData = bullseyeSO;
        st.targetRenderer = disc.GetComponent<MeshRenderer>();
        st.reaction = TargetReaction.Wobble;
    }

    private static void SpawnWeaponOnTable(Transform boothTransform)
    {
        GameObject blasterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/VRTemplateAssets/Prefabs/Interactables/Blaster Variant.prefab");
        if (blasterPrefab == null)
        {
            blasterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/DemoAssets/Prefabs/Interactables/Blaser.prefab");
        }

        if (blasterPrefab != null)
        {
            GameObject existingBlaster = GameObject.Find("VR_Shooting_Blaster");
            if (existingBlaster != null) Undo.DestroyObjectImmediate(existingBlaster);

            GameObject weapon = (GameObject)PrefabUtility.InstantiatePrefab(blasterPrefab);
            weapon.name = "VR_Shooting_Blaster";
            weapon.transform.SetParent(boothTransform);
            weapon.transform.position = new Vector3(0.15f, 0.92f, 0.72f);
            weapon.transform.rotation = Quaternion.Euler(0, 180, 0);
            Undo.RegisterCreatedObjectUndo(weapon, "Colocar Arma en Mesa");
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

    private static Material CreateDianaMaterial(Texture2D baseTex, Texture2D normalTex)
    {
        if (baseTex == null) return null;

        string matPath = "Assets/04_Materials/Mat_Diana_3D.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            mat = new Material(shader);
            mat.mainTexture = baseTex;
            if (normalTex != null)
            {
                mat.EnableKeyword("_NORMALMAP");
                mat.SetTexture("_BumpMap", normalTex);
            }
            AssetDatabase.CreateAsset(mat, matPath);
        }
        return mat;
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
