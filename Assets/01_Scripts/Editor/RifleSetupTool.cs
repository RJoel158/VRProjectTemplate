#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RifleSetupTool : EditorWindow
{
    private GameObject targetRifle;
    private float currentYaw = -90f;
    private bool toggleGrab = true;

    [MenuItem("VR Sports/Calibración y Orientación de Rifle VR")]
    public static void ShowWindow()
    {
        var window = GetWindow<RifleSetupTool>("Calibración Rifle VR");
        window.minSize = new Vector2(380, 480);
    }

    [MenuItem("VR Sports/Configurar Rifle en Escena (Empties + Miras + XR)")]
    public static void SetupRifleInScene()
    {
        SetupRifleInternal(-90f, true);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("🎯 Calibración de Rifle VR (Orientación y Agarre)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Esta herramienta calibra la orientación 3D del rifle, sus miras de hierro, el punto de agarre ergonómico y el modo de agarre (Toggle / Hold).", MessageType.Info);

        GUILayout.Space(10);
        targetRifle = (GameObject)EditorGUILayout.ObjectField("Objeto Rifle:", targetRifle != null ? targetRifle : FindRifleInScene(), typeof(GameObject), true);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("1. Orientación del Cañón (Giro Y):", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("0° (Original)", GUILayout.Height(30)))
        {
            ApplyRotation(0f);
        }
        if (GUILayout.Button("90° (Derecha)", GUILayout.Height(30)))
        {
            ApplyRotation(90f);
        }
        if (GUILayout.Button("180° (Atrás)", GUILayout.Height(30)))
        {
            ApplyRotation(180f);
        }
        if (GUILayout.Button("⭐ -90° / 270° (Frente)", GUILayout.Height(30)))
        {
            ApplyRotation(-90f);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(10);
        currentYaw = EditorGUILayout.Slider("Ángulo Yaw Manual:", currentYaw, -180f, 180f);
        if (GUILayout.Button("Aplicar Ángulo Manual"))
        {
            ApplyRotation(currentYaw);
        }

        GUILayout.Space(15);
        EditorGUILayout.LabelField("2. Modo de Agarre:", EditorStyles.boldLabel);
        toggleGrab = EditorGUILayout.Toggle("Modo Toggle (1 Clic Agarra / 1 Clic Suelta)", toggleGrab);

        GUILayout.Space(15);
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
        if (GUILayout.Button("🎯 RECONFIGURAR Y ALINEAR RIFLE COMPLETO", GUILayout.Height(40)))
        {
            SetupRifleInternal(currentYaw, toggleGrab);
        }
        GUI.backgroundColor = Color.white;
    }

    private void ApplyRotation(float yawAngle)
    {
        currentYaw = yawAngle;
        GameObject rifle = targetRifle != null ? targetRifle : FindRifleInScene();
        if (rifle == null) return;

        VRRifle rifleComp = rifle.GetComponent<VRRifle>();
        if (rifleComp != null)
        {
            rifleComp.modelRotationOffset = new Vector3(0, yawAngle, 0);
            if (rifleComp.modelRoot != null)
            {
                rifleComp.modelRoot.localEulerAngles = new Vector3(0, yawAngle, 0);
                EditorUtility.SetDirty(rifleComp.modelRoot);
            }
            if (rifleComp.attachPoint != null)
            {
                rifleComp.attachPoint.localEulerAngles = new Vector3(0, yawAngle, 0);
                EditorUtility.SetDirty(rifleComp.attachPoint);
            }
            EditorUtility.SetDirty(rifleComp);
        }
        else
        {
            MeshRenderer mr = rifle.GetComponentInChildren<MeshRenderer>();
            if (mr != null)
            {
                mr.transform.localEulerAngles = new Vector3(0, yawAngle, 0);
                EditorUtility.SetDirty(mr.transform);
            }
        }
        Debug.Log($"🎯 Orientación y punto de anclaje del rifle actualizados a {yawAngle}° en Y.");
    }

    private static GameObject FindRifleInScene()
    {
        GameObject rifleObj = GameObject.Find("Rifle_Ruger_1022LR");
        if (rifleObj == null) rifleObj = GameObject.Find("Meshy_AI_Wooden_Rifle_0914021154_texture");
        if (rifleObj == null)
        {
            GameObject[] allGOs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allGOs)
            {
                if (go.name.Contains("Wooden_Rifle") || go.name.Contains("Ruger") || go.name.Contains("1022"))
                {
                    return go;
                }
            }
        }
        return rifleObj;
    }

    private static void SetupRifleInternal(float yaw, bool isToggle)
    {
        GameObject rifleObj = FindRifleInScene();
        if (rifleObj == null)
        {
            EditorUtility.DisplayDialog("Configurar Rifle", "No se encontró ningún objeto de rifle en la escena.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(rifleObj, "Configurar Rifle VR");
        rifleObj.name = "Rifle_Ruger_1022LR";

        MeshRenderer meshRenderer = rifleObj.GetComponentInChildren<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError("No se encontró MeshRenderer en el rifle.");
            return;
        }

        Material rifleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/04_Materials/Mat_Rifle_Wood.mat");
        if (rifleMat != null)
        {
            meshRenderer.sharedMaterial = rifleMat;
        }

        Transform meshTransform = meshRenderer.transform;
        meshTransform.localRotation = Quaternion.Euler(0, yaw, 0);

        Bounds bounds = meshRenderer.bounds;
        Vector3 worldCenter = bounds.center;
        Vector3 worldExtents = bounds.extents;

        Vector3 localCenter = rifleObj.transform.InverseTransformPoint(worldCenter);
        Vector3 localMin = rifleObj.transform.InverseTransformPoint(worldCenter - worldExtents);
        Vector3 localMax = rifleObj.transform.InverseTransformPoint(worldCenter + worldExtents);

        float minX = Mathf.Min(localMin.x, localMax.x);
        float maxX = Mathf.Max(localMin.x, localMax.x);
        float minY = Mathf.Min(localMin.y, localMax.y);
        float maxY = Mathf.Max(localMin.y, localMax.y);
        float minZ = Mathf.Min(localMin.z, localMax.z);
        float maxZ = Mathf.Max(localMin.z, localMax.z);

        Transform attachPoint = GetOrCreateChild(rifleObj.transform, "AttachPoint_Grip");
        Transform muzzlePoint = GetOrCreateChild(rifleObj.transform, "MuzzlePoint");
        Transform frontSight = GetOrCreateChild(rifleObj.transform, "FrontSight");
        Transform rearSight = GetOrCreateChild(rifleObj.transform, "RearSight");
        Transform shellEjection = GetOrCreateChild(rifleObj.transform, "Shell_Ejection_Point");

        attachPoint.localPosition = new Vector3(localCenter.x, minY + (maxY - minY) * 0.32f, minZ + (maxZ - minZ) * 0.38f);
        attachPoint.localRotation = Quaternion.Euler(0, yaw, 0);

        muzzlePoint.localPosition = new Vector3(localCenter.x, minY + (maxY - minY) * 0.65f, maxZ + 0.02f);
        muzzlePoint.localRotation = Quaternion.Euler(0, yaw, 0);

        frontSight.localPosition = new Vector3(localCenter.x, maxY - 0.005f, maxZ - 0.05f);
        frontSight.localRotation = Quaternion.Euler(0, yaw, 0);

        rearSight.localPosition = new Vector3(localCenter.x, maxY - 0.005f, localCenter.z - 0.04f);
        rearSight.localRotation = Quaternion.Euler(0, yaw, 0);

        shellEjection.localPosition = new Vector3(maxX + 0.015f, minY + (maxY - minY) * 0.65f, localCenter.z + 0.04f);
        shellEjection.localRotation = Quaternion.Euler(0, yaw, 0);

        Rigidbody rb = rifleObj.GetComponent<Rigidbody>();
        if (rb == null) rb = rifleObj.AddComponent<Rigidbody>();
        rb.mass = 2.0f;
        rb.linearDamping = 0.5f;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

        BoxCollider boxCol = rifleObj.GetComponent<BoxCollider>();
        if (boxCol == null) boxCol = rifleObj.AddComponent<BoxCollider>();
        boxCol.center = localCenter;
        boxCol.size = new Vector3(Mathf.Max(0.14f, maxX - minX + 0.04f), Mathf.Max(0.24f, maxY - minY + 0.04f), Mathf.Max(0.90f, maxZ - minZ + 0.04f));

        Material tracerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/04_Materials/Mat_Bullet_Tracer.mat");
        Material shellMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/04_Materials/Mat_Bullet_Shell.mat");

        LineRenderer tracer = rifleObj.GetComponent<LineRenderer>();
        if (tracer == null) tracer = rifleObj.AddComponent<LineRenderer>();
        tracer.enabled = false;
        tracer.startWidth = 0.012f;
        tracer.endWidth = 0.006f;
        if (tracerMat != null) tracer.sharedMaterial = tracerMat;

        XRGrabInteractable grab = rifleObj.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = rifleObj.AddComponent<XRGrabInteractable>();
        grab.attachTransform = attachPoint;
        grab.useDynamicAttach = false;
        grab.matchAttachPosition = true;
        grab.matchAttachRotation = true;
        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.throwOnDetach = true;
        grab.interactionLayers = ~0;

        if (grab.colliders.Count == 0 || !grab.colliders.Contains(boxCol))
        {
            grab.colliders.Clear();
            grab.colliders.Add(boxCol);
        }

        VRRifle rifleComp = rifleObj.GetComponent<VRRifle>();
        if (rifleComp == null) rifleComp = rifleObj.AddComponent<VRRifle>();
        rifleComp.modelRotationOffset = new Vector3(0, yaw, 0);
        rifleComp.toggleGrabMode = isToggle;
        rifleComp.rifleType = RifleType.SemiAutomatic;
        rifleComp.muzzlePoint = muzzlePoint;
        rifleComp.frontSight = frontSight;
        rifleComp.rearSight = rearSight;
        rifleComp.attachPoint = attachPoint;
        rifleComp.shellEjectionPoint = shellEjection;
        rifleComp.modelRoot = meshTransform;
        rifleComp.tracerLineRenderer = tracer;
        rifleComp.tracerMaterial = tracerMat;
        rifleComp.shellMaterial = shellMat;
        rifleComp.enableBodycamAim = true;
        rifleComp.maxRange = 250f;
        rifleComp.magazineCapacity = 10;
        rifleComp.infiniteAmmo = true;

        EditorUtility.SetDirty(rifleObj);
        Selection.activeGameObject = rifleObj;
        Debug.Log($"🎯 ¡Rifle calibrado exitosamente con rotación {yaw}° y agarre Toggle={isToggle}!");
    }

    private static Transform GetOrCreateChild(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            GameObject newChild = new GameObject(childName);
            newChild.transform.SetParent(parent);
            newChild.transform.localPosition = Vector3.zero;
            newChild.transform.localRotation = Quaternion.identity;
            child = newChild.transform;
        }
        return child;
    }
}
#endif
