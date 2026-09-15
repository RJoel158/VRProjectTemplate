#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// Configura el Avatar Emerald Sentinel con el Shooter Pack de animaciones:
/// 1. Importa el modelo y animaciones como Humanoid para máxima compatibilidad.
/// 2. Crea el Animator Controller completo (Aim Idle, Firing Rifle, Walk, Run, Strafe).
/// 3. Conecta el arma a la mano derecha del avatar (mixamorig:RightHand).
/// 4. Vincula el Animator a VRRifle para que al disparar ejecute la animación.
/// </summary>
public static class ShooterAvatarSetup
{
    private const string ShooterPackPath = "Assets/03_Resources/Shooter Pack";
    private const string ControllerPath = "Assets/03_Resources/Shooter Pack/EmeraldSentinel_Shooter.controller";

    [MenuItem("Tools/Shooter Avatar/🦾  Configurar Avatar y Animaciones", false, 0)]
    public static void SetupShooterAvatar()
    {
        EditorUtility.DisplayProgressBar("Configurando Avatar", "Verificando importación Humanoid...", 0.1f);

        try
        {
            // ── 1. Configurar Modelos y Animaciones como Humanoid ─────────────────
            ConfigureHumanoid(Path.Combine(ShooterPackPath, "Meshy_AI_Emerald_Sentinel_0915025451_texture.fbx"));
            ConfigureHumanoid(Path.Combine(ShooterPackPath, "rifle aiming idle.fbx"), true);
            ConfigureHumanoid(Path.Combine(ShooterPackPath, "firing rifle.fbx"), false);
            ConfigureHumanoid(Path.Combine(ShooterPackPath, "walking.fbx"), true);
            ConfigureHumanoid(Path.Combine(ShooterPackPath, "rifle run.fbx"), true);

            AssetDatabase.Refresh();

            // ── 2. Cargar Animation Clips ─────────────────────────────────────────
            EditorUtility.DisplayProgressBar("Configurando Avatar", "Creando Animator Controller...", 0.4f);

            AnimationClip aimIdleClip = LoadClip(Path.Combine(ShooterPackPath, "rifle aiming idle.fbx"));
            AnimationClip fireClip    = LoadClip(Path.Combine(ShooterPackPath, "firing rifle.fbx"));
            AnimationClip walkClip    = LoadClip(Path.Combine(ShooterPackPath, "walking.fbx"));
            AnimationClip runClip     = LoadClip(Path.Combine(ShooterPackPath, "rifle run.fbx"));

            // ── 3. Crear Animator Controller ─────────────────────────────────────
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            // Parámetros
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("IsAiming", AnimatorControllerParameterType.Bool);

            var rootStateMachine = controller.layers[0].stateMachine;

            // Estados
            var stateAimIdle = rootStateMachine.AddState("Aim_Idle");
            stateAimIdle.motion = aimIdleClip;

            var stateFire = rootStateMachine.AddState("Firing_Rifle");
            stateFire.motion = fireClip;

            var stateWalk = rootStateMachine.AddState("Walking");
            stateWalk.motion = walkClip;

            var stateRun = rootStateMachine.AddState("Rifle_Run");
            stateRun.motion = runClip;

            // Default State = Aim_Idle
            rootStateMachine.defaultState = stateAimIdle;

            // Transiciones: Disparo
            var anyToFire = rootStateMachine.AddAnyStateTransition(stateFire);
            anyToFire.AddCondition(AnimatorConditionMode.If, 0, "Shoot");
            anyToFire.hasExitTime = false;
            anyToFire.duration = 0.05f;

            var fireToAim = stateFire.AddTransition(stateAimIdle);
            fireToAim.hasExitTime = true;
            fireToAim.exitTime = 0.7f;
            fireToAim.duration = 0.1f;

            // Transiciones: Movimiento
            if (walkClip != null)
            {
                var idleToWalk = stateAimIdle.AddTransition(stateWalk);
                idleToWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
                idleToWalk.hasExitTime = false;
                idleToWalk.duration = 0.15f;

                var walkToIdle = stateWalk.AddTransition(stateAimIdle);
                walkToIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
                walkToIdle.hasExitTime = false;
                walkToIdle.duration = 0.15f;

                if (runClip != null)
                {
                    var walkToRun = stateWalk.AddTransition(stateRun);
                    walkToRun.AddCondition(AnimatorConditionMode.Greater, 2.5f, "Speed");
                    walkToRun.hasExitTime = false;
                    walkToRun.duration = 0.15f;

                    var runToWalk = stateRun.AddTransition(stateWalk);
                    runToWalk.AddCondition(AnimatorConditionMode.Less, 2.5f, "Speed");
                    runToWalk.hasExitTime = false;
                    runToWalk.duration = 0.15f;
                }
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            // ── 4. Configurar Avatar en Primera Persona ─────────────────────────
            EditorUtility.DisplayProgressBar("Configurando Avatar", "Conectando Avatar en Primera Persona...", 0.7f);

            GameObject avatarInstance = FindOrInstantiateAvatar();
            if (avatarInstance != null)
            {
                Animator anim = avatarInstance.GetComponent<Animator>();
                if (anim == null) anim = avatarInstance.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;

                // Buscar XR Origin y Main Camera
                GameObject xrOrigin = GameObject.Find("XR Origin Hands (XR Rig)") ?? GameObject.Find("XR Origin");
                Camera mainCam = Camera.main;

                if (xrOrigin != null)
                {
                    // Emparentar el avatar directamente al XR Origin
                    avatarInstance.transform.SetParent(xrOrigin.transform, false);
                    avatarInstance.transform.localPosition = Vector3.zero;
                    avatarInstance.transform.localRotation = Quaternion.identity;
                }

                // Configurar FirstPersonVRBody para sincronizar con la cámara
                FirstPersonVRBody fpBody = avatarInstance.GetComponent<FirstPersonVRBody>();
                if (fpBody == null) fpBody = avatarInstance.AddComponent<FirstPersonVRBody>();

                if (mainCam != null)
                {
                    fpBody.cameraTransform = mainCam.transform;
                    // Asegurar Near Clip Plane bajo para no cortar geometría cercana
                    mainCam.nearClipPlane = Mathf.Min(mainCam.nearClipPlane, 0.05f);
                    EditorUtility.SetDirty(mainCam);
                }

                Transform head = FindBone(avatarInstance.transform, "Head");
                if (head != null)
                {
                    fpBody.headBone = head;
                }

                // Buscar mano derecha para montar el arma
                Transform rightHand = FindBone(avatarInstance.transform, "RightHand");
                if (rightHand != null)
                {
                    Transform socket = rightHand.Find("WeaponSocket");
                    if (socket == null)
                    {
                        GameObject sockGO = new GameObject("WeaponSocket");
                        socket = sockGO.transform;
                        socket.SetParent(rightHand, false);
                        socket.localPosition = new Vector3(0.08f, 0.02f, 0.03f);
                        socket.localEulerAngles = new Vector3(0f, 90f, -15f);
                    }

                    // Vincular el rifle a la mano si está en la escena
                    GameObject ruger = GameObject.Find("Rifle_Ruger_1022LR");
                    if (ruger != null)
                    {
                        ruger.transform.SetParent(socket, false);
                        ruger.transform.localPosition = Vector3.zero;
                        ruger.transform.localRotation = Quaternion.identity;

                        VRRifle rifleScript = ruger.GetComponent<VRRifle>();
                        if (rifleScript != null)
                        {
                            rifleScript.avatarAnimator = anim;
                            EditorUtility.SetDirty(rifleScript);
                        }
                    }
                }

                EditorUtility.SetDirty(avatarInstance);
            }

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("¡Avatar en Primera Persona Configurado!",
                "1. Se configuró el modelo Emerald Sentinel con rig Humanoid y animaciones.\n" +
                "2. El cuerpo se sincronizó bajo el XR Origin con el script FirstPersonVRBody (sigue la vista de la cámara).\n" +
                "3. El rifle está colocado en la mano derecha (WeaponSocket) listo para apuntar y disparar.\n\n" +
                "¡Dale a Play ▶ para jugar con tu avatar en primera persona!", "¡Entendido!");
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[ShooterAvatarSetup] Error: {ex}");
            EditorUtility.DisplayDialog("Error", ex.Message, "OK");
        }
    }

    private static void ConfigureHumanoid(string fbxPath, bool loopTime = false)
    {
        ModelImporter importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        bool modified = false;
        if (importer.animationType != ModelImporterAnimationType.Human)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            modified = true;
        }

        if (loopTime && importer.clipAnimations != null && importer.clipAnimations.Length > 0)
        {
            var clips = importer.defaultClipAnimations;
            foreach (var c in clips)
            {
                c.loopTime = true;
            }
            importer.clipAnimations = clips;
            modified = true;
        }

        if (modified)
        {
            importer.SaveAndReimport();
        }
    }

    private static AnimationClip LoadClip(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (var a in assets)
        {
            if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                return clip;
        }
        return null;
    }

    private static GameObject FindOrInstantiateAvatar()
    {
        // 1. Eliminar el modelo viejo sin esqueleto/huesos si está en la escena
        GameObject oldUnrigged = GameObject.Find("Meshy_AI_Emerald_Sentinel_0915025300_texture");
        if (oldUnrigged != null)
        {
            Undo.DestroyObjectImmediate(oldUnrigged);
        }

        // 2. Buscar si ya existe la instancia correcta con esqueleto
        GameObject inScene = GameObject.Find("Meshy_AI_Emerald_Sentinel");
        if (inScene != null) return inScene;

        // 3. Instanciar desde el FBX con esqueleto y rig (0915025451 de Shooter Pack)
        string fbx = Path.Combine(ShooterPackPath, "Meshy_AI_Emerald_Sentinel_0915025451_texture.fbx");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        if (prefab != null)
        {
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = "Meshy_AI_Emerald_Sentinel";
            Undo.RegisterCreatedObjectUndo(inst, "Instantiate Rigged First-Person Avatar");
            return inst;
        }

        return null;
    }

    private static Transform FindBone(Transform root, string boneName)
    {
        if (root.name.IndexOf(boneName, System.StringComparison.OrdinalIgnoreCase) >= 0) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindBone(root.GetChild(i), boneName);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
