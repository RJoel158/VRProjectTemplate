using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Gestor persistente (DontDestroyOnLoad) del sistema de Input y Mandos VR.
/// Soluciona de raíz el bloqueo de mandos al cambiar de escena o reiniciar:
/// 1. Garantiza que InputActionAsset permanezca 100% activo y nunca quede deshabilitado.
/// 2. Reactiva y sincroniza todos los TrackedPoseDriver y controladores tras cada carga o reinicio.
/// 3. Restaura Time.timeScale a 1f para evitar que menús de pausa congelen el motor al recargar.
/// 4. Asegura la existencia de un EventSystem con InputSystemUIInputModule en cada escena.
/// </summary>
[DefaultExecutionOrder(-2000)]
public class VRInputPersistenceManager : MonoBehaviour
{
    private static VRInputPersistenceManager _instance;
    public static VRInputPersistenceManager Instance => _instance;

    [Header("Input Action Assets Activos")]
    [SerializeField] private List<InputActionAsset> inputAssets = new List<InputActionAsset>();

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

        // 2. Reactivar inmediatamente todas las acciones
        CollectAndEnableInputAssets();

        // 3. Recuperar y refrescar el tracking de mandos durante los primeros frames de la nueva escena
        StartCoroutine(RecoverControllersRoutine());
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

            foreach (var asset in inputAssets)
            {
                if (asset != null)
                {
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
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[VRInputPersistenceManager] Aviso habilitando InputAssets: {ex.Message}");
        }
    }

    private IEnumerator RecoverControllersRoutine()
    {
        // Ejecución inmediata en frame 0
        RefreshControllersAndTracking();

        // Esperar al frame siguiente para que todos los Awake y Start de la nueva escena hayan concluido
        yield return null;
        CollectAndEnableInputAssets();
        RefreshControllersAndTracking();

        // Segundo frame de gracia para confirmar binding con el hardware XR
        yield return new WaitForEndOfFrame();
        CollectAndEnableInputAssets();
        EnsureEventSystemExists();
    }

    private void RefreshControllersAndTracking()
    {
        // 1. Refrescar TrackedPoseDriver para que vuelva a vincularse al hardware de Meta Quest / PCVR
        var poseDrivers = FindObjectsByType<TrackedPoseDriver>(FindObjectsSortMode.None);
        foreach (var pd in poseDrivers)
        {
            if (pd != null && pd.gameObject.activeInHierarchy)
            {
                bool wasEnabled = pd.enabled;
                pd.enabled = false;
                pd.enabled = wasEnabled;
            }
        }

        // 2. Refrescar controladores XRBaseController si existen
        var controllers = FindObjectsByType<XRBaseController>(FindObjectsSortMode.None);
        foreach (var ctrl in controllers)
        {
            if (ctrl != null && ctrl.gameObject.activeInHierarchy)
            {
                bool wasEnabled = ctrl.enabled;
                ctrl.enabled = false;
                ctrl.enabled = wasEnabled;
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
