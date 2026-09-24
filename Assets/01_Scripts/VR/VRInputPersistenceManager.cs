using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Gestor persistente (DontDestroyOnLoad) del sistema de Input y Mandos VR.
/// Soluciona de raíz el bloqueo/freeze de mandos al cambiar de escena o reiniciar:
/// 1. Garantiza que todos los InputActionAsset permanezcan activos y nunca queden deshabilitados.
/// 2. Configura 'ignoreTrackingState = true' en todos los TrackedPoseDriver para que nunca descarten datos.
/// 3. Ejecuta sincronización de pose 6DOF directa (Hardware OpenXR + Input System fallback) en LateUpdate,
///    garantizando que los mandos NUNCA queden congelados en la posición anterior.
/// 4. Restaura Time.timeScale a 1f para evitar que menús de pausa congelen el motor al recargar.
/// 5. Asegura la existencia de un EventSystem con InputSystemUIInputModule en cada escena.
/// </summary>
[DefaultExecutionOrder(-32000)]
public class VRInputPersistenceManager : MonoBehaviour
{
    private static VRInputPersistenceManager _instance;
    public static VRInputPersistenceManager Instance => _instance;

    [Header("Input Action Assets Activos")]
    [SerializeField] private List<InputActionAsset> inputAssets = new List<InputActionAsset>();

    [Header("Referencias a Mandos en Escena")]
    [SerializeField] private Transform rightControllerTransform;
    [SerializeField] private Transform leftControllerTransform;
    [SerializeField] private Transform cameraTransform;

    private TrackedPoseDriver rightPoseDriver;
    private TrackedPoseDriver leftPoseDriver;
    private TrackedPoseDriver cameraPoseDriver;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance == null)
        {
            GameObject go = new GameObject("[VR_Input_Persistence_Manager]");
            _instance = go.AddComponent<VRInputPersistenceManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        CollectAndEnableInputAssets();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. Restaurar Time.timeScale a 1 por seguridad
        Time.timeScale = 1f;

        // 2. Reactivar inmediatamente todas las acciones de input
        CollectAndEnableInputAssets();

        // 3. Resetear y localizar referencias en la nueva escena
        rightControllerTransform = null;
        leftControllerTransform = null;
        cameraTransform = null;
        rightPoseDriver = null;
        leftPoseDriver = null;
        cameraPoseDriver = null;

        LocateControllersAndEnsureTrackingState();

        // 4. Coroutine suave para verificar durante los primeros frames sin romper eventos
        StartCoroutine(SceneInitCheckRoutine());
    }

    private void Update()
    {
        // Verificar periódicamente que ningún InputActionAsset haya sido deshabilitado por un InputActionManager destruido
        EnsureInputAssetsActive();
    }

    private void LateUpdate()
    {
        // Si no tenemos mandos cacheados, buscarlos
        if (rightControllerTransform == null || leftControllerTransform == null)
        {
            LocateControllersAndEnsureTrackingState();
        }

        // Sincronización continua de pose 6DOF (Safeguard irrompible)
        SyncControllerPose(XRNode.RightHand, rightControllerTransform, rightPoseDriver);
        SyncControllerPose(XRNode.LeftHand, leftControllerTransform, leftPoseDriver);
        SyncControllerPose(XRNode.CenterEye, cameraTransform, cameraPoseDriver);
    }

    /// <summary>
    /// Sincroniza la posición y rotación local del mando con el hardware físico XR (OpenXR)
    /// o con el Input System action directamente si el driver falló o se desincronizó.
    /// </summary>
    private void SyncControllerPose(XRNode node, Transform targetTransform, TrackedPoseDriver driver)
    {
        if (targetTransform == null) return;

        bool updated = false;

        // 1. Prioridad: Consulta directa a hardware nativo OpenXR (Meta Quest / PCVR)
        var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
        if (device.isValid)
        {
            bool hasPos = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 devPos);
            bool hasRot = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion devRot);

            if (hasPos && hasRot)
            {
                targetTransform.SetLocalPositionAndRotation(devPos, devRot);
                updated = true;
            }
            else if (hasPos)
            {
                targetTransform.localPosition = devPos;
                updated = true;
            }
            else if (hasRot)
            {
                targetTransform.localRotation = devRot;
                updated = true;
            }
        }

        // 2. Fallback: Si no hay dispositivo nativo (ej. XR Device Simulator en Editor), leer el InputAction directamente
        if (!updated && driver != null)
        {
            var posAction = driver.positionInput.action;
            var rotAction = driver.rotationInput.action;

            if (posAction != null && posAction.enabled && rotAction != null && rotAction.enabled)
            {
                Vector3 actPos = posAction.ReadValue<Vector3>();
                Quaternion actRot = rotAction.ReadValue<Quaternion>();

                if (actPos != Vector3.zero || actRot != Quaternion.identity)
                {
                    targetTransform.SetLocalPositionAndRotation(actPos, actRot);
                }
            }
        }
    }

    /// <summary>
    /// Localiza mandos y cámara en la escena actual y asegura ignoreTrackingState = true.
    /// </summary>
    public void LocateControllersAndEnsureTrackingState()
    {
        var poseDrivers = FindObjectsByType<TrackedPoseDriver>(FindObjectsSortMode.None);
        foreach (var pd in poseDrivers)
        {
            if (pd == null) continue;

            // Ignorar estado de tracking para que nunca deje de actualizar el transform
            pd.ignoreTrackingState = true;

            string n = pd.gameObject.name.ToLower();
            if (n.Contains("right") && !n.Contains("stabiliz") && !n.Contains("attach") && !n.Contains("ray"))
            {
                rightControllerTransform = pd.transform;
                rightPoseDriver = pd;
            }
            else if (n.Contains("left") && !n.Contains("stabiliz") && !n.Contains("attach") && !n.Contains("ray"))
            {
                leftControllerTransform = pd.transform;
                leftPoseDriver = pd;
            }
            else if (n.Contains("camera") || n.Contains("head") || n.Contains("eye"))
            {
                cameraTransform = pd.transform;
                cameraPoseDriver = pd;
            }
        }

        // Fallback por nombres estándar si no se asignaron por TrackedPoseDriver
        if (rightControllerTransform == null)
        {
            var go = GameObject.Find("Right Controller") ?? GameObject.Find("RightHand Controller") ?? GameObject.Find("Right Hand");
            if (go != null) rightControllerTransform = go.transform;
        }

        if (leftControllerTransform == null)
        {
            var go = GameObject.Find("Left Controller") ?? GameObject.Find("LeftHand Controller") ?? GameObject.Find("Left Hand");
            if (go != null) leftControllerTransform = go.transform;
        }

        if (cameraTransform == null)
        {
            var cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }
    }

    private IEnumerator SceneInitCheckRoutine()
    {
        // Frame 0
        LocateControllersAndEnsureTrackingState();
        EnsureInputAssetsActive();

        // Frame 1: Tras la ejecución de Awake y Start de todos los objetos en la nueva escena
        yield return null;
        LocateControllersAndEnsureTrackingState();
        EnsureInputAssetsActive();

        // Frame 2: Fin de frame para asegurar EventSystem y controladores
        yield return new WaitForEndOfFrame();
        EnsureInputAssetsActive();
        EnsureEventSystemExists();
    }

    /// <summary>
    /// Busca y habilita todas las instancias de InputActionAsset cargadas en memoria.
    /// </summary>
    public void CollectAndEnableInputAssets()
    {
        try
        {
            InputActionAsset[] loadedAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            foreach (var asset in loadedAssets)
            {
                if (asset != null && !inputAssets.Contains(asset))
                {
                    inputAssets.Add(asset);
                }
            }

            EnsureInputAssetsActive();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[VRInputPersistenceManager] Aviso habilitando InputAssets: {ex.Message}");
        }
    }

    private void EnsureInputAssetsActive()
    {
        for (int i = 0; i < inputAssets.Count; i++)
        {
            var asset = inputAssets[i];
            if (asset == null) continue;

            if (!asset.enabled)
            {
                asset.Enable();
            }

            foreach (var map in asset.actionMaps)
            {
                if (!map.enabled)
                {
                    map.Enable();
                }
            }
        }
    }

    private void EnsureEventSystemExists()
    {
        var existingEventSystem = FindAnyObjectByType<EventSystem>();
        if (existingEventSystem == null)
        {
            GameObject esGo = new GameObject("EventSystem_AutoCreated");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }
        else
        {
            var uiModule = existingEventSystem.GetComponent<InputSystemUIInputModule>();
            if (uiModule == null && existingEventSystem.GetComponent<StandaloneInputModule>() == null)
            {
                existingEventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }
    }
}
