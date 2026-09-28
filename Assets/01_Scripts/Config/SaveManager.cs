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

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    private string savePath;

    public CircuitSaveData CurrentRunData { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Path.Combine(Application.persistentDataPath, "circuit_save.json");
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
        foreach (int score in data.scores)
        {
            if (score != -1) { hasAtLeastOneScore = true; break; }
        }

        return hasAtLeastOneScore && data.currentIndex < data.sportOrder.Length;
    }

    public CircuitSaveData LoadCircuit()
    {
        if (!File.Exists(savePath)) return null;
        string json = File.ReadAllText(savePath);
        return JsonUtility.FromJson<CircuitSaveData>(json);
    }

    public void SaveCircuit(CircuitSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
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
        if (File.Exists(savePath)) File.Delete(savePath);
    }
    public int GetTotalScore(CircuitSaveData data)
    {
        int total = 0;
        foreach (int s in data.scores)
            if (s != -1) total += s; // -1 = not played yet
        return total;
    }

    public void LogScores(string title, CircuitSaveData data)
    {
        string breakdown = "";
        for (int i = 0; i < data.sportOrder.Length; i++)
        {
            string value = data.scores[i] == -1 ? "not played" : data.scores[i].ToString();
            breakdown += $"  {data.sportOrder[i]}: {value}\n";
        }
        Debug.Log($"[{title}]\n{breakdown}  TOTAL: {GetTotalScore(data)}");
    }
}