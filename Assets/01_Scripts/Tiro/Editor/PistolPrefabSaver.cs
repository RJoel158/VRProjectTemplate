using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Tiro.Editor
{
    public static class PistolPrefabSaver
    {
        private const string PrefabDirectory = "Assets/03_Resources/Prefabs/Tiro";
        private const string PrefabPath = "Assets/03_Resources/Prefabs/Tiro/Olympic_Pistol_VR.prefab";

        [InitializeOnLoadMethod]
        private static void AutoSavePistolPrefab()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(PrefabPath))
                {
                    SavePistolPrefab();
                }
            };
        }

        [MenuItem("VR Sports/Tiro/Save Pistol As Prefab")]
        public static void SavePistolPrefab()
        {
            if (!Directory.Exists(PrefabDirectory))
            {
                Directory.CreateDirectory(PrefabDirectory);
                AssetDatabase.Refresh();
            }

            // Buscar la pistola en la escena activa
            var pistol = Object.FindAnyObjectByType<Tiro.Weapons.OlympicPistol>();
            if (pistol != null)
            {
                PrefabUtility.SaveAsPrefabAssetAndConnect(pistol.gameObject, PrefabPath, InteractionMode.AutomatedAction);
                Debug.Log($"[PistolPrefabSaver] ¡Pistola guardada exitosamente como Prefab en {PrefabPath}!");
            }
            else
            {
                // Si no está en la escena activa, abrir temporalmente ShootingScene para extraerla
                string scenePath = "Assets/00_Scenes/ShootingScene.unity";
                if (File.Exists(scenePath))
                {
                    var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                    var pistolInScene = Object.FindAnyObjectByType<Tiro.Weapons.OlympicPistol>();
                    if (pistolInScene != null)
                    {
                        PrefabUtility.SaveAsPrefabAsset(pistolInScene.gameObject, PrefabPath);
                        Debug.Log($"[PistolPrefabSaver] ¡Pistola extraída y guardada en {PrefabPath}!");
                    }
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
    }
}
