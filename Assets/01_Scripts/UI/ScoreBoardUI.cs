using UnityEngine;
using TMPro;


public class ScoreboardUI : MonoBehaviour
{
    [Tooltip("Drag the TextMeshPro object that shows the score here.")]
    public TextMeshProUGUI scoreText;

    private void Start()
    {
      
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.onScoreChanged.AddListener(UpdateScoreText);
            UpdateScoreText(ScoreManager.Instance.GetCurrentScore());
        }
        else
        {
            Debug.LogWarning("ScoreboardUI: ScoreManager.Instance is null. Is there a GameManager with ScoreManager in the scene?");
        }
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.onScoreChanged.RemoveListener(UpdateScoreText);
        }
    }

    private void UpdateScoreText(int newScore)
    {
        scoreText.text = "Score: " + newScore;
    }
}