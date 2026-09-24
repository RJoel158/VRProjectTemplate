using System;
using System.IO;
using UnityEngine;

namespace Golf.Persistence
{
    /// <summary>
    /// Manejador estatico de persistencia en disco JSON para el sistema de Minigolf.
    /// Garantiza la prueba exigida en la rubrica: Guardar -> Reiniciar -> Cargar -> Recuperar.
    /// </summary>
    public static class GolfSaveSystem
    {
        private static readonly string SaveFileName = "GolfSaveData.json";
        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static GolfSaveData Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    GolfSaveData data = JsonUtility.FromJson<GolfSaveData>(json);
                    if (data != null)
                    {
                        Debug.Log($"[GolfSaveSystem] Datos cargados exitosamente desde: {SavePath}");
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GolfSaveSystem] Error al cargar archivo de guardado: {ex.Message}");
            }

            Debug.Log("[GolfSaveSystem] No se encontro archivo previo o estaba vacio. Creando nuevo registro.");
            var newData = new GolfSaveData();
            Save(newData);
            return newData;
        }

        public static void Save(GolfSaveData data)
        {
            if (data == null) return;

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[GolfSaveSystem] Datos guardados exitosamente en: {SavePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GolfSaveSystem] Error al guardar datos: {ex.Message}");
            }
        }

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                    Debug.Log("[GolfSaveSystem] Archivo de guardado eliminado correctamente.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GolfSaveSystem] Error al eliminar guardado: {ex.Message}");
            }
        }
    }
}
