using System;
using System.Collections.Generic;
using UnityEngine;

namespace Golf.Persistence
{
    /// <summary>
    /// Estructura de datos persistente para el minijuego de Minigolf.
    /// Serializada a JSON en disco para cumplir con los requisitos academicos de guardado y carga.
    /// </summary>
    [Serializable]
    public class GolfSaveData
    {
        public int gamesPlayed = 0;
        public int coursesCompleted = 0;
        public int totalStrokesAllTime = 0;
        public int bestTotalScore = 999; // Menor cantidad de golpes en el circuito completo
        public int totalHolesInOne = 0;
        public int totalEagles = 0;
        public int totalBirdies = 0;
        public int totalPars = 0;

        // Records individuales de golpes por cada hoyo (clave: numero de hoyo, valor: record de golpes)
        public List<HoleRecordEntry> holeRecords = new List<HoleRecordEntry>();

        public int GetBestForHole(int holeNumber)
        {
            for (int i = 0; i < holeRecords.Count; i++)
            {
                if (holeRecords[i].holeNumber == holeNumber)
                {
                    return holeRecords[i].bestStrokes;
                }
            }
            return 99; // Sin record previo
        }

        public void RecordHoleScore(int holeNumber, int strokes)
        {
            for (int i = 0; i < holeRecords.Count; i++)
            {
                if (holeRecords[i].holeNumber == holeNumber)
                {
                    if (strokes < holeRecords[i].bestStrokes)
                    {
                        holeRecords[i].bestStrokes = strokes;
                    }
                    return;
                }
            }

            holeRecords.Add(new HoleRecordEntry { holeNumber = holeNumber, bestStrokes = strokes });
        }
    }

    [Serializable]
    public class HoleRecordEntry
    {
        public int holeNumber;
        public int bestStrokes;
    }
}
