using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class CircuitSaveData
{
    public bool exists;
    public string[] sportOrder;
    public int currentIndex;
    public int[] scores; // -1 means not played yet
}

[System.Serializable]
public class CircuitRunRecord
{
    public int runNumber;
    public string date;
    public int totalScore;
    public int basketScore;
    public int bowlingScore;
    public int golfScore;
    public int shootingScore;
    public bool isCurrentRun;
}

[System.Serializable]
public class CircuitHistoryData
{
    public int totalRunsCount = 0;
    public int allTimeHighScore = 0;
    public List<CircuitRunRecord> records = new List<CircuitRunRecord>();
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    private string savePath;
    private string historyPath;

    public CircuitSaveData CurrentRunData { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Path.Combine(Application.persistentDataPath, "circuit_save.json");
            historyPath = Path.Combine(Application.persistentDataPath, "circuit_history.json");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool ExistsCircuitInProgress()
    {
        if (!File.Exists(savePath)) return false;

        CircuitSaveData data = LoadCircuit();
        if (data == null || !data.exists) return false;

        // Only counts as "in progress" if at least one sport has a saved score
        bool hasAtLeastOneScore = false;
        if (data.scores != null)
        {
            foreach (int score in data.scores)
            {
                if (score != -1) { hasAtLeastOneScore = true; break; }
            }
        }

        return hasAtLeastOneScore && data.currentIndex < (data.sportOrder != null ? data.sportOrder.Length : 0);
    }

    public CircuitSaveData LoadCircuit()
    {
        if (!File.Exists(savePath)) return null;
        try
        {
            string json = File.ReadAllText(savePath);
            return JsonUtility.FromJson<CircuitSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Error loading circuit save: {e.Message}");
            return null;
        }
    }

    public void SaveCircuit(CircuitSaveData data)
    {
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(savePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Error saving circuit: {e.Message}");
        }
    }

    // Creates a fresh run WITHOUT writing to disk yet.
    // Disk write only happens when the first sport finishes (via SaveSportScore).
    public CircuitSaveData CreateNewCircuit(string[] sportOrder)
    {
        CircuitSaveData data = new CircuitSaveData
        {
            exists = true,
            sportOrder = sportOrder,
            currentIndex = 0,
            scores = new int[sportOrder.Length]
        };
        for (int i = 0; i < data.scores.Length; i++) data.scores[i] = -1;

        return data; // NOT saved to disk here
    }

    // Call this when a sport finishes during circuit mode
    public void SaveSportScore(CircuitSaveData data, int sportIndex, int score)
    {
        if (data == null || data.scores == null || sportIndex >= data.scores.Length) return;
        data.scores[sportIndex] = score;
        data.currentIndex = sportIndex + 1;
        SaveCircuit(data); // this is when it actually hits disk
    }

    public void SetCurrentRun(CircuitSaveData data)
    {
        CurrentRunData = data;
    }

    public void DeleteCircuitSave()
    {
        if (File.Exists(savePath))
        {
            try
            {
                File.Delete(savePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Error deleting circuit save: {e.Message}");
            }
        }
        CurrentRunData = null;
    }

    public int GetTotalScore(CircuitSaveData data)
    {
        if (data == null || data.scores == null) return 0;
        int total = 0;
        foreach (int s in data.scores)
            if (s != -1) total += s; // -1 = not played yet
        return total;
    }

    public void LogScores(string title, CircuitSaveData data)
    {
        if (data == null || data.sportOrder == null || data.scores == null) return;
        string breakdown = "";
        for (int i = 0; i < data.sportOrder.Length; i++)
        {
            string value = data.scores[i] == -1 ? "not played" : data.scores[i].ToString();
            breakdown += $"  {data.sportOrder[i]}: {value}\n";
        }
        Debug.Log($"[{title}]\n{breakdown}  TOTAL: {GetTotalScore(data)}");
    }

    // ==========================================
    // CIRCUIT HISTORY & LEADERBOARD SYSTEM
    // ==========================================

    public CircuitHistoryData LoadCircuitHistory()
    {
        if (string.IsNullOrEmpty(historyPath))
        {
            historyPath = Path.Combine(Application.persistentDataPath, "circuit_history.json");
        }

        if (!File.Exists(historyPath))
        {
            // Seed initial records with realistic previous circuit runs
            CircuitHistoryData initial = new CircuitHistoryData();
            initial.totalRunsCount = 3;
            initial.allTimeHighScore = 215;
            initial.records.Add(new CircuitRunRecord
            {
                runNumber = 1,
                date = "25/09",
                basketScore = 30,
                bowlingScore = 45,
                golfScore = 35,
                shootingScore = 25,
                totalScore = 135,
                isCurrentRun = false
            });
            initial.records.Add(new CircuitRunRecord
            {
                runNumber = 2,
                date = "26/09",
                basketScore = 40,
                bowlingScore = 60,
                golfScore = 45,
                shootingScore = 30,
                totalScore = 175,
                isCurrentRun = false
            });
            initial.records.Add(new CircuitRunRecord
            {
                runNumber = 3,
                date = "27/09",
                basketScore = 55,
                bowlingScore = 75,
                golfScore = 45,
                shootingScore = 40,
                totalScore = 215,
                isCurrentRun = false
            });

            SaveCircuitHistory(initial);
            return initial;
        }

        try
        {
            string json = File.ReadAllText(historyPath);
            CircuitHistoryData data = JsonUtility.FromJson<CircuitHistoryData>(json);
            if (data == null) data = new CircuitHistoryData();
            if (data.records == null) data.records = new List<CircuitRunRecord>();
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Error loading circuit history: {e.Message}");
            return new CircuitHistoryData();
        }
    }

    public void SaveCircuitHistory(CircuitHistoryData history)
    {
        if (string.IsNullOrEmpty(historyPath))
        {
            historyPath = Path.Combine(Application.persistentDataPath, "circuit_history.json");
        }

        try
        {
            string json = JsonUtility.ToJson(history, true);
            File.WriteAllText(historyPath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Error saving circuit history: {e.Message}");
        }
    }

    public CircuitRunRecord RecordCompletedCircuit(CircuitSaveData run)
    {
        if (run == null) return null;

        CircuitHistoryData history = LoadCircuitHistory();
        history.totalRunsCount++;

        CircuitRunRecord record = new CircuitRunRecord
        {
            runNumber = history.totalRunsCount,
            date = DateTime.Now.ToString("dd/MM"),
            basketScore = GetSportScoreFromRun(run, "Basket"),
            bowlingScore = GetSportScoreFromRun(run, "Bowling"),
            golfScore = GetSportScoreFromRun(run, "Golf"),
            shootingScore = GetSportScoreFromRun(run, "Shooting"),
            isCurrentRun = true
        };
        record.totalScore = record.basketScore + record.bowlingScore + record.golfScore + record.shootingScore;

        if (history.records != null)
        {
            foreach (var r in history.records) r.isCurrentRun = false;
        }
        else
        {
            history.records = new List<CircuitRunRecord>();
        }

        if (record.totalScore > history.allTimeHighScore)
        {
            history.allTimeHighScore = record.totalScore;
        }

        history.records.Add(record);
        SaveCircuitHistory(history);

        Debug.Log($"[SaveManager] Circuit Run #{record.runNumber} recorded! Total: {record.totalScore} pts (High Score: {history.allTimeHighScore})");
        return record;
    }

    public int GetSportScoreFromRun(CircuitSaveData run, string sportKeyword)
    {
        if (run == null || run.sportOrder == null || run.scores == null) return 0;
        for (int i = 0; i < run.sportOrder.Length; i++)
        {
            if (i < run.scores.Length && run.sportOrder[i].IndexOf(sportKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return run.scores[i] >= 0 ? run.scores[i] : 0;
            }
        }
        return 0;
    }

    public int GetRunRank(CircuitRunRecord record, CircuitHistoryData history)
    {
        if (record == null || history == null || history.records == null || history.records.Count == 0) return 1;

        List<CircuitRunRecord> sorted = new List<CircuitRunRecord>(history.records);
        sorted.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));

        int idx = sorted.FindIndex(r => r.runNumber == record.runNumber && r.totalScore == record.totalScore);
        return idx >= 0 ? idx + 1 : 1;
    }

    public List<CircuitRunRecord> GetTopLeaderboard(int maxCount = 5)
    {
        CircuitHistoryData history = LoadCircuitHistory();
        if (history == null || history.records == null) return new List<CircuitRunRecord>();

        List<CircuitRunRecord> sorted = new List<CircuitRunRecord>(history.records);
        sorted.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));

        if (sorted.Count > maxCount)
        {
            sorted = sorted.GetRange(0, maxCount);
        }
        return sorted;
    }
}