using UnityEngine;
using TMPro;


public class ScoreboardUI : MonoBehaviour
{
    [Tooltip("Drag the TextMeshPro object that shows the score here.")]
    public TextMeshProUGUI scoreText;

    private void Start()
    {
        FormatScoreText();

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

    private void FormatScoreText()
    {
        if (scoreText == null) return;

        scoreText.enableWordWrapping = false;
        scoreText.overflowMode = TextOverflowModes.Overflow;

        if (scoreText.fontSize > 65f)
        {
            scoreText.fontSize = 60f;
        }

        RectTransform rt = scoreText.rectTransform;
        if (rt != null && rt.sizeDelta.x < 500f)
        {
            float currentLeft = rt.anchoredPosition.x - (rt.sizeDelta.x * rt.pivot.x);
            rt.sizeDelta = new Vector2(500f, Mathf.Max(rt.sizeDelta.y, 75f));
            rt.anchoredPosition = new Vector2(currentLeft + (500f * rt.pivot.x), rt.anchoredPosition.y);
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