using UnityEngine;


[CreateAssetMenu(fileName = "NewMinigameData", menuName = "VR Sports/Minigame Data")]
public class MinigameData : ScriptableObject
{
    [Header("Display Info")]
    public string sportName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    [Header("Scene & Rules")]
    public string sceneName;       // Name of the scene to load for this sport
    public float timeLimitSeconds; // 0 = no time limit
    public int targetScoreToWin;   // Score needed to "win" this minigame, if applicable

    [Header("Instructions")]
    [TextArea(3, 6)]
    public string instructionsText;
}