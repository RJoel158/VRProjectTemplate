#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Herramienta de mantenimiento en Editor.
/// </summary>
public static class RifleSetupTool
{
    [MenuItem("Tools/Shooting Range/✋  Dejar Rifle Fijo Listo Para Coger", false, 0)]
    public static void SetRifleFixedAndGrabbable()
    {
        GameObject ruger = GameObject.Find("Rifle_Ruger_1022LR");
        if (ruger == null)
        {
            VRRifle r = Object.FindAnyObjectByType<VRRifle>();
            if (r != null) ruger = r.gameObject;
        }

        if (ruger == null)
        {
            EditorUtility.DisplayDialog("Error", "No se encontró el objeto del rifle en la escena.", "OK");
            return;
        }

        // 1. Desvincular de la cámara manteniendo su posición y rotación exacta en el mundo
        if (ruger.transform.parent != null && ruger.transform.parent.name.Contains("Camera"))
        {
            // Ponerlo en el stand / PlayerBooth o raíz manteniendo la posición en el mundo
            GameObject booth = GameObject.Find("PlayerBooth");
            if (booth != null)
                ruger.transform.SetParent(booth.transform, true);
            else
                ruger.transform.SetParent(null, true);

            Debug.Log("[RifleSetupTool] ✅ Rifle desvinculado de la cámara (mantiene su posición en el mundo).");
        }

        // 2. Asegurar Rigidbody quieto (Kinematic = true)
        Rigidbody rb = ruger.GetComponent<Rigidbody>();
        if (rb == null) rb = ruger.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // 3. Asegurar BoxCollider ajustado a la forma del rifle
        BoxCollider bc = ruger.GetComponent<BoxCollider>();
        if (bc == null) bc = ruger.AddComponent<BoxCollider>();
        
        MeshFilter mf = ruger.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            bc.center = mf.sharedMesh.bounds.center;
            bc.size   = mf.sharedMesh.bounds.size;
        }
        else
        {
            bc.size = new Vector3(0.012f, 0.02f, 0.088f);
        }
        bc.enabled = true;

        // 4. Configurar XRGrabInteractable listo para la mano
        XRGrabInteractable grab = ruger.GetComponent<XRGrabInteractable>();
        if (grab == null) grab = ruger.AddComponent<XRGrabInteractable>();
        grab.enabled = true;
        grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
        grab.throwOnDetach = false;
        grab.colliders.Clear();
        grab.colliders.Add(bc);

        // 5. Asegurar AudioSource y VRRifle
        AudioSource audio = ruger.GetComponent<AudioSource>();
        if (audio == null)
        {
            audio = ruger.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0.8f;
        }

        VRRifle rifle = ruger.GetComponent<VRRifle>();
        if (rifle == null) rifle = ruger.AddComponent<VRRifle>();
        rifle.infiniteAmmo = true;

        EditorUtility.SetDirty(ruger);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );

        Selection.activeGameObject = ruger;
        EditorGUIUtility.PingObject(ruger);

        EditorUtility.DisplayDialog("¡Rifle Fijo y Listo!",
            "1. El rifle ya NO está pegado a tu cabeza (se desanexó de la cámara conservando su lugar exacto).\n" +
            "2. Rigidbody está quieto (Kinematic = True) para que no salga volando ni caiga.\n" +
            "3. Box Collider y XR Grab Interactable están activos.\n\n" +
            "Al presionar Play, el rifle se quedará quieto donde está. Acércale la mano y presiona Grip para cogerlo.", "Entendido");
    }

    [MenuItem("Tools/Shooting Range/🛠  Reparar Escena (Enderezar Pared Fondo)", false, 10)]
    public static void FixBackstopWall()
    {
        GameObject backstop = GameObject.Find("Bullet_Backstop_42m");
        if (backstop != null)
        {
            var rifleOnWall = backstop.GetComponent<VRRifle>();
            if (rifleOnWall != null) Undo.DestroyObjectImmediate(rifleOnWall);

            var grabOnWall = backstop.GetComponent<XRGrabInteractable>();
            if (grabOnWall != null) Undo.DestroyObjectImmediate(grabOnWall);

            var rbOnWall = backstop.GetComponent<Rigidbody>();
            if (rbOnWall != null) Undo.DestroyObjectImmediate(rbOnWall);

            backstop.transform.localRotation = Quaternion.identity;
            backstop.transform.localPosition = new Vector3(0f, 4f, 42f);
            backstop.transform.localScale    = new Vector3(14f, 8f, 1f);
            EditorUtility.SetDirty(backstop);
            Debug.Log("[RifleSetupTool] ✅ Pared de fondo 'Bullet_Backstop_42m' reparada y enderezada.");
        }
    }
}
#endif
