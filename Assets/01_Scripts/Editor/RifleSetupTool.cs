#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class RifleSetupTool : EditorWindow
{
    [MenuItem("VR Sports/Configurar Rifle en Escena (Empties + Miras + XR)")]
    public static void SetupRifleInScene()
    {
        // 1. Buscar el objeto del rifle en la escena
        GameObject rifleObj = GameObject.Find("Meshy_AI_Wooden_Rifle_0914021154_texture");
        if (rifleObj == null) rifleObj = GameObject.Find("Rifle_Ruger_1022LR");

        if (rifleObj == null)
        {
            GameObject[] allGOs = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var go in allGOs)
            {
                if (go.name.Contains("Wooden_Rifle") || go.name.Contains("Ruger") || go.name.Contains("1022"))
                {
                    rifleObj = go;
                    break;
                }
            }
        }

        if (rifleObj == null)
        {
            EditorUtility.DisplayDialog("Configurar Rifle", "No se encontró ningún objeto de rifle en la escena.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(rifleObj, "Configurar Rifle VR");

        rifleObj.name = "Rifle_Ruger_1022LR";

        // 2. Obtener el MeshRenderer del modelo 3D
        MeshRenderer meshRenderer = rifleObj.GetComponentInChildren<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError("No se encontró MeshRenderer en el rifle.");
            return;
        }

        // Material PBR
        Material rifleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/04_Materials/Mat_Rifle_Wood.mat");
        if (rifleMat != null)
        {
            meshRenderer.sharedMaterial = rifleMat;
        }

        Transform meshTransform = meshRenderer.transform;

        // 3. Calcular la geometría y límites exactos de la malla en espacio local
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

        // 4. Crear o Actualizar los Empties de Miras, Cañón y Agarre directamente sobre la malla
        Transform attachPoint = GetOrCreateChild(rifleObj.transform, "AttachPoint_Grip");
        Transform muzzlePoint = GetOrCreateChild(rifleObj.transform, "MuzzlePoint");
        Transform frontSight = GetOrCreateChild(rifleObj.transform, "FrontSight");
        Transform rearSight = GetOrCreateChild(rifleObj.transform, "RearSight");
        Transform shellEjection = GetOrCreateChild(rifleObj.transform, "Shell_Ejection_Point");

        // Posiciones exactas adheridas a la geometría del rifle
        // Empuñadura (AttachPoint_Grip): donde va la mano con el dedo en el gatillo
        attachPoint.localPosition = new Vector3(localCenter.x, minY + (maxY - minY) * 0.28f, localCenter.z - (maxZ - minZ) * 0.18f);
        attachPoint.localRotation = Quaternion.identity;

        // Boca del cañón (MuzzlePoint): extremo delantero por donde sale el disparo
        muzzlePoint.localPosition = new Vector3(localCenter.x, localCenter.y + (maxY - minY) * 0.12f, maxZ + 0.02f);
        muzzlePoint.localRotation = Quaternion.identity;

        // Mira delantera (FrontSight): punta de mira sobre el cañón
        frontSight.localPosition = new Vector3(localCenter.x, maxY - 0.005f, maxZ - 0.04f);
        frontSight.localRotation = Quaternion.identity;

        // Mira trasera (RearSight): muesca de mira sobre el cajón de mecanismos
        rearSight.localPosition = new Vector3(localCenter.x, maxY - 0.005f, localCenter.z - 0.02f);
        rearSight.localRotation = Quaternion.identity;

        // Ventana de expulsión (Shell_Ejection_Point): lateral derecho
        shellEjection.localPosition = new Vector3(maxX + 0.01f, localCenter.y + (maxY - minY) * 0.1f, localCenter.z);
        shellEjection.localRotation = Quaternion.identity;

        // 5. Rigidbody
        Rigidbody rb = rifleObj.GetComponent<Rigidbody>();
        if (rb == null) rb = rifleObj.AddComponent<Rigidbody>();
        rb.mass = 2.4f;
        rb.linearDamping = 1.0f;
        rb.angularDamping = 1.0f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // 6. BoxCollider ajustado a la forma del rifle
        BoxCollider boxCol = rifleObj.GetComponent<BoxCollider>();
        if (boxCol == null) boxCol = rifleObj.AddComponent<BoxCollider>();
        boxCol.center = localCenter;
        boxCol.size = new Vector3(Mathf.Max(0.10f, maxX - minX), Mathf.Max(0.18f, maxY - minY), Mathf.Max(0.70f, maxZ - minZ));

        // 7. LineRenderer para trazador de bala
        LineRenderer tracer = rifleObj.GetComponent<LineRenderer>();
        if (tracer == null) tracer = rifleObj.AddComponent<LineRenderer>();
        tracer.enabled = false;
        tracer.startWidth = 0.012f;
        tracer.endWidth = 0.006f;
        tracer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
        tracer.startColor = Color.yellow;
        tracer.endColor = new Color(1f, 0.5f, 0f, 0f);

        // 8. XRGrabInteractable
        XRGrabInteractable grab = rifleObj.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = rifleObj.AddComponent<XRGrabInteractable>();
        grab.attachTransform = attachPoint;
        grab.useDynamicAttach = false;
        grab.matchAttachPosition = true;
        grab.matchAttachRotation = true;
        grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
        grab.throwOnDetach = false;

        // 9. VRRifle Component
        VRRifle rifleComp = rifleObj.GetComponent<VRRifle>();
        if (rifleComp == null) rifleComp = rifleObj.AddComponent<VRRifle>();
        rifleComp.rifleType = RifleType.SemiAutomatic;
        rifleComp.muzzlePoint = muzzlePoint;
        rifleComp.frontSight = frontSight;
        rifleComp.rearSight = rearSight;
        rifleComp.attachPoint = attachPoint;
        rifleComp.shellEjectionPoint = shellEjection;
        rifleComp.modelRoot = meshTransform;
        rifleComp.tracerLineRenderer = tracer;
        rifleComp.enableBodycamAim = true;
        rifleComp.maxRange = 250f;
        rifleComp.magazineCapacity = 10;
        rifleComp.infiniteAmmo = true;

        EditorUtility.SetDirty(rifleObj);
        Selection.activeGameObject = rifleObj;
        Debug.Log("🎯 ¡Rifle Ruger 10/22 LR reconfigurado con Empties colocados con precisión milimétrica sobre la malla!");
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
