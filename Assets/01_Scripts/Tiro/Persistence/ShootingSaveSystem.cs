using System;
using System.IO;
using UnityEngine;

namespace Tiro.Persistence
{
    [Serializable]
    public class ShootingSaveData
    {
        public int highestScore = 0;
        public int rifleHighScore = 0;
        public int pistolHighScore = 0;
        public int clayHighScore = 0;
        public int totalShotsFired = 0;
        public int totalHitsOnTarget = 0;
        public int totalBullseyes = 0;
        public int seriesCompleted = 0;
        public int goldMedals = 0;
        public int silverMedals = 0;
        public int bronzeMedals = 0;
        public float bestAccuracyPercentage = 0f;
    }

    public static class ShootingSaveSystem
    {
        private static readonly string SaveFileName = "shooting_save.json";

        private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static void Save(ShootingSaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SaveFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShootingSaveSystem] Error guardando progreso: {ex.Message}");
            }
        }

        public static ShootingSaveData Load()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    var data = JsonUtility.FromJson<ShootingSaveData>(json);
                    if (data != null) return data;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ShootingSaveSystem] No se pudo cargar archivo, generando datos nuevos: {ex.Message}");
            }

            return new ShootingSaveData();
        }

        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ShootingSaveSystem] Error borrando guardado: {ex.Message}");
            }
        }
    }
}
