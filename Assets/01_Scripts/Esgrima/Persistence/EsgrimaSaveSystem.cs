using System;
using System.IO;
using UnityEngine;

namespace Esgrima.Persistence
{
    [Serializable]
    public class EsgrimaSaveData
    {
        public int victories = 0;
        public int defeats = 0;
        public int currentWinStreak = 0;
        public int highestWinStreak = 0;
        public int totalHitsLanded = 0;
        public string lastSaveTimestamp = "";
    }

    /// <summary>
    /// Sistema de guardado y persistencia en disco (JSON) que cumple
    /// con el requisito obligatorio de: Guardar -> Reiniciar -> Cargar -> Recuperar progreso.
    /// </summary>
    public static class EsgrimaSaveSystem
    {
        private static readonly string SaveFileName = "esgrima_save.json";

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static void Save(EsgrimaSaveData data)
        {
            if (data == null) return;

            data.lastSaveTimestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[EsgrimaSaveSystem] Datos guardados exitosamente en: {SavePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EsgrimaSaveSystem] Error al guardar datos: {ex.Message}");
            }
        }

        public static EsgrimaSaveData Load()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    EsgrimaSaveData data = JsonUtility.FromJson<EsgrimaSaveData>(json);
                    Debug.Log($"[EsgrimaSaveSystem] Datos cargados con éxito. Victorias: {data.victories}, Derrotas: {data.defeats}");
                    return data;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EsgrimaSaveSystem] Error al cargar datos: {ex.Message}");
            }

            // Si no existe archivo previo, devolver un nuevo registro
            return new EsgrimaSaveData();
        }

        public static void ResetData()
        {
            try
            {
                if (File.Exists(SavePath))
                {
                    File.Delete(SavePath);
                    Debug.Log("[EsgrimaSaveSystem] Archivo de guardado reseteado.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EsgrimaSaveSystem] Error al borrar datos: {ex.Message}");
            }
        }
    }
}
