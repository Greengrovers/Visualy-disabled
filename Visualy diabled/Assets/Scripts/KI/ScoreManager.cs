using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text timeText;

    private int score = 0;
    private float time = 0f;

    void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        time += Time.deltaTime;
        UpdateUI();
    }

    public void AddSheepScore()
    {
        int baseScore = 10;
        int bonus = GetTimeBonus();

        int total = baseScore + bonus;
        score += total;

        Debug.Log($"Score: {score} | Zeit: {time:F1}s | +{total}");

        UpdateUI();
    }

    int GetTimeBonus()
    {
        if (time < 30f) return 2;     // +20%
        if (time < 45f) return 1;     // +10%
        if (time < 60f) return 0;     // +5% -> optional
        return 0;
    }

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;

        if (timeText != null)
            timeText.text = "Time: " + time.ToString("F1") + "s";
    }
}