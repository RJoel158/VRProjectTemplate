using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Tiro.Targets;
using Tiro.Weapons;
using Tiro.Core;

namespace Tiro.Editor
{
    public static class ApplyNewShootingModels
    {
        private const string ScenePath = "Assets/00_Scenes/ShootingScene.unity";
        private const string BounPath = "Assets/03_Resources/NewModels/Boun/Meshy_AI_Brick_and_Timber_Boun_0923230945_texture.fbx";
        private const string GrassPath = "Assets/03_Resources/NewModels/grass/Meshy_AI_necesito_pasto_para_u_0923230439_texture.fbx";
        private const string ObjectivesPath = "Assets/03_Resources/NewModels/OBJECTIVES/Meshy_AI_Necesito_objetivos_pa_0923225936_texture.fbx";

        // Nota: Solo se ejecuta manualmente si el usuario lo solicita desde el menu
        [MenuItem("VR Sports/Tiro/Apply New Models (Boun, Grass, Objectives)")]
        public static void ApplyModelsToScene()
        {
            Debug.Log("[ApplyNewShootingModels] Iniciando integracion de modelos Boun, Grass y Objectives...");

            // 1. Configurar Materiales para que tengan sus texturas y mapas normales
            SetupMaterial("Assets/03_Resources/NewModels/Boun/Material.001.mat",
                "Assets/03_Resources/NewModels/Boun/Image_0.jpg",
                "Assets/03_Resources/NewModels/Boun/Meshy_AI_Brick_and_Timber_Boun_0923230945_texture_normal.png");

            SetupMaterial("Assets/03_Resources/NewModels/grass/Material.001.mat",
                "Assets/03_Resources/NewModels/grass/Meshy_AI_necesito_pasto_para_u_0923230439_texture.png",
                "Assets/03_Resources/NewModels/grass/Meshy_AI_necesito_pasto_para_u_0923230439_texture_normal.png");

            SetupMaterial("Assets/03_Resources/NewModels/OBJECTIVES/Material.001.mat",
                "Assets/03_Resources/NewModels/OBJECTIVES/Meshy_AI_Necesito_objetivos_pa_0923225936_texture.png",
                "Assets/03_Resources/NewModels/OBJECTIVES/Meshy_AI_Necesito_objetivos_pa_0923225936_texture_normal.png");

            // 2. Cargar la escena de Tiro
            var currentScene = EditorSceneManager.GetActiveScene();
            if (currentScene.path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            // 3. Cargar Prefabs FBX
            GameObject bounPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BounPath);
            GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrassPath);
            GameObject objectivesPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ObjectivesPath);

            if (bounPrefab == null || grassPrefab == null || objectivesPrefab == null)
            {
                Debug.LogError("[ApplyNewShootingModels] Error cargando los prefabs FBX de NewModels.");
                return;
            }

            LogPrefabMetrics("Boun", bounPrefab);
            LogPrefabMetrics("Grass", grassPrefab);
            LogPrefabMetrics("Objectives", objectivesPrefab);

            GameObject rangeRoot = GameObject.Find("Olympic_Shooting_Range");
            if (rangeRoot == null)
            {
                Debug.LogError("[ApplyNewShootingModels] No se encontro Olympic_Shooting_Range en la escena.");
                return;
            }

            // --- A) Integrar Boun (Cerca de madera en lugar de la mesa y la linea roja) ---
            var oldBench = GameObject.Find("Shooting_Bench");
            if (oldBench != null) Object.DestroyImmediate(oldBench);

            var oldLine = GameObject.Find("Safety_Firing_Line");
            if (oldLine != null) Object.DestroyImmediate(oldLine);

            var existingFence = rangeRoot.transform.Find("Shooting_Fence_Boun");
            if (existingFence != null) Object.DestroyImmediate(existingFence.gameObject);

            var oldColHolder = rangeRoot.transform.Find("Fence_Collision_Barrier");
            if (oldColHolder != null) Object.DestroyImmediate(oldColHolder.gameObject);

            GameObject fenceObj = (GameObject)PrefabUtility.InstantiatePrefab(bounPrefab, rangeRoot.transform);
            fenceObj.name = "Shooting_Fence_Boun";
            // Rotar -90 en X para que el eje Z del modelo quede vertical (eje Y de Unity)
            // Altura total: 0.88m (waist height). Base en y=0, centro en y=0.44m
            fenceObj.transform.position = new Vector3(0f, 0.44f, 0.85f);
            fenceObj.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            fenceObj.transform.localScale = new Vector3(1.30f, 1.40f, 1.57f);

            // Eliminar cualquier collider deformado en el prefab
            foreach (var col in fenceObj.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(col);
            }

            // Objeto de colision limpio no deformado
            GameObject colHolder = new GameObject("Fence_Collision_Barrier");
            colHolder.transform.SetParent(rangeRoot.transform);
            colHolder.transform.position = new Vector3(0f, 0.44f, 0.85f);
            colHolder.transform.rotation = Quaternion.identity;
            colHolder.transform.localScale = Vector3.one;

            BoxCollider fenceCol = colHolder.AddComponent<BoxCollider>();
            fenceCol.center = Vector3.zero;
            fenceCol.size = new Vector3(2.45f, 0.88f, 0.25f);

            // Reposicionar las armas descansando perfectamente sobre el riel superior de madera (y = 0.89)
            var shotgun = GameObject.Find("Olympic_Shotgun_VR");
            if (shotgun != null) shotgun.transform.position = new Vector3(-0.45f, 0.89f, 0.85f);

            var rifle = GameObject.Find("Olympic_Rifle_VR");
            if (rifle != null) rifle.transform.position = new Vector3(0f, 0.89f, 0.85f);

            var pistol = GameObject.Find("Olympic_Pistol_VR");
            if (pistol != null) pistol.transform.position = new Vector3(0.45f, 0.89f, 0.85f);

            var hud = GameObject.Find("Olympic_Shooting_HUD");
            if (hud != null) hud.transform.position = new Vector3(-0.65f, 1.03f, 0.85f);

            Debug.Log("[ApplyNewShootingModels] Cerca Boun calibrada a escala de cintura (0.88m) y armas apoyadas en el riel.");

            // --- B) Integrar Grass (Pasto del polígono) ---
            var floor = GameObject.Find("Range_Floor");
            if (floor != null)
            {
                var mr = floor.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.enabled = true;
                    var boothMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Resources/Materials/M_Range_BoothFloor.mat");
                    if (boothMat != null)
                    {
                        boothMat.SetColor("_BaseColor", new Color(0.22f, 0.42f, 0.18f, 1f));
                        boothMat.SetFloat("_Smoothness", 0.08f);
                        mr.material = boothMat;
                    }
                }
            }

            var existingGrass = rangeRoot.transform.Find("Range_Grass_Foreground");
            if (existingGrass != null) Object.DestroyImmediate(existingGrass.gameObject);

            var oldStretchedGrass = rangeRoot.transform.Find("Range_Grass_Floor");
            if (oldStretchedGrass != null) Object.DestroyImmediate(oldStretchedGrass.gameObject);

            // Césped 3D proporcionado sin distorsión para el primer plano frente a la cerca
            GameObject grassObj = (GameObject)PrefabUtility.InstantiatePrefab(grassPrefab, rangeRoot.transform);
            grassObj.name = "Range_Grass_Foreground";
            grassObj.transform.position = new Vector3(0f, -0.02f, 2.2f);
            grassObj.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            grassObj.transform.localScale = new Vector3(2.5f, 2.5f, 1.0f);

            Debug.Log("[ApplyNewShootingModels] Suelo de cesped sincronizado y vegetacion 3D de primer plano colocada con proporcion natural.");

            // --- C) Integrar OBJECTIVES para las dianas de tiro ---
            var targetsRoot = GameObject.Find("Distance_Targets_Root");
            if (targetsRoot != null)
            {
                var targetBoards = targetsRoot.GetComponentsInChildren<TargetBoard>(true);
                foreach (var tb in targetBoards)
                {
                    Transform tRoot = tb.transform;

                    // 1. Eliminar poste antiguo y soportes
                    var oldPole = tRoot.Find("Stand_Pole");
                    if (oldPole != null) Object.DestroyImmediate(oldPole.gameObject);
                    var oldSupport = tRoot.Find("Stand_Support_Post");
                    if (oldSupport != null) Object.DestroyImmediate(oldSupport.gameObject);

                    // 2. Encontrar Swing_Pivot y Target_Face
                    var swingPivot = tRoot.Find("Swing_Pivot");
                    Transform targetFace = swingPivot != null ? swingPivot.Find("Target_Face") : tRoot.Find("Target_Face");
                    if (targetFace == null) targetFace = tRoot;

                    // 3. Eliminar primitivas de anillos antiguos
                    string[] ringsToRemove = new string[] {
                        "Ring_Outer_White_50cm",
                        "Ring_Middle_Black_30cm",
                        "Ring_Center_Gold_10cm",
                        "Bullseye_InnerTen_5cm"
                    };
                    foreach (var ringName in ringsToRemove)
                    {
                        var ringObj = targetFace.Find(ringName);
                        if (ringObj != null) Object.DestroyImmediate(ringObj.gameObject);
                    }

                    // 4. Eliminar modelo de objetivo previo si ya existia
                    var prevModel = targetFace.Find("Objective_Model");
                    if (prevModel != null) Object.DestroyImmediate(prevModel.gameObject);

                    // 5. Escala y colisión calibradas según la distancia del carril
                    float distance = tb.DistanceMeters;
                    float modelScale = 0.42f;
                    Vector2 colSize = new Vector2(0.55f, 0.55f);

                    if (distance > 35f) // 50 Metros
                    {
                        modelScale = 1.0f;
                        colSize = new Vector2(1.25f, 1.25f);
                    }
                    else if (distance > 15f) // 25 Metros
                    {
                        modelScale = 0.65f;
                        colSize = new Vector2(0.85f, 0.85f);
                    }
                    else // 10 Metros
                    {
                        modelScale = 0.42f;
                        colSize = new Vector2(0.55f, 0.55f);
                    }

                    // 6. Instanciar nuevo modelo 3D OBJECTIVES erguido verticalmente (-90 en X)
                    GameObject objModel = (GameObject)PrefabUtility.InstantiatePrefab(objectivesPrefab, targetFace);
                    objModel.name = "Objective_Model";
                    objModel.transform.localPosition = Vector3.zero;
                    objModel.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                    objModel.transform.localScale = Vector3.one * modelScale;

                    // 7. Configurar BoxCollider de impacto en Target_Face
                    BoxCollider bc = targetFace.GetComponent<BoxCollider>();
                    if (bc == null) bc = targetFace.gameObject.AddComponent<BoxCollider>();
                    bc.size = new Vector3(colSize.x, colSize.y, 0.15f);
                    bc.center = Vector3.zero;

                    // 8. Soporte vertical (Stand_Support_Post) desde el suelo hasta la diana (para que no flote en el aire)
                    GameObject postObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    postObj.name = "Stand_Support_Post";
                    postObj.transform.SetParent(tRoot);
                    postObj.transform.localPosition = new Vector3(0f, -0.70f, 0.02f);
                    postObj.transform.localScale = new Vector3(0.08f * (modelScale / 0.5f), 0.70f, 0.08f * (modelScale / 0.5f));
                    var postCol = postObj.GetComponent<Collider>();
                    if (postCol != null) Object.DestroyImmediate(postCol);

                    var matStand = AssetDatabase.LoadAssetAtPath<Material>("Assets/03_Resources/Materials/M_Arena_Wood_Amber.mat");
                    if (matStand != null) postObj.GetComponent<Renderer>().material = matStand;

                    // 9. Configurar targetCenter en TargetBoard hacia targetFace
                    var soTb = new SerializedObject(tb);
                    soTb.FindProperty("targetCenter").objectReferenceValue = targetFace;
                    soTb.ApplyModifiedProperties();
                }
                Debug.Log($"[ApplyNewShootingModels] Objetivos calibrados proporcionalmente y anclados al suelo en {targetBoards.Length} dianas.");
            }

            // --- D) Activar por defecto la disciplina de Dianas Olímpicas (OlympicRifleDistance) ---
            var rangeMgr = Object.FindAnyObjectByType<ShootingRangeManager>();
            if (rangeMgr != null)
            {
                var soMgr = new SerializedObject(rangeMgr);
                soMgr.FindProperty("activeDiscipline").enumValueIndex = (int)ShootingDiscipline.OlympicRifleDistance;
                soMgr.ApplyModifiedProperties();
            }

            // Guardar cambios en la escena
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[ApplyNewShootingModels] Escena ShootingScene guardada con exito con todos los nuevos modelos.");
        }

        private static void SetupMaterial(string matPath, string texPath, string normalPath)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

            if (mat != null)
            {
                if (tex != null)
                {
                    mat.SetTexture("_BaseMap", tex);
                    mat.SetTexture("_MainTex", tex);
                    mat.SetColor("_BaseColor", Color.white);
                    mat.SetColor("_Color", Color.white);
                }
                if (normal != null)
                {
                    mat.SetTexture("_BumpMap", normal);
                }
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssets();
            }
        }

        private static void LogPrefabMetrics(string label, GameObject prefab)
        {
            var mfs = prefab.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in mfs)
            {
                if (mf.sharedMesh != null)
                {
                    var b = mf.sharedMesh.bounds;
                    Debug.Log($"[Metrics] {label} - Mesh: {mf.name}, Local Bounds Center={b.center}, Size={b.size}, Min={b.min}, Max={b.max}");
                }
            }
        }
    }
}
