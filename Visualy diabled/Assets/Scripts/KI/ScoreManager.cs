using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Score")]
    public int score = 0;

    [Header("Timer")]
    public float elapsedTime = 0f;
    public bool timerRunning = true;

    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text timerText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        if (timerRunning)
        {
            elapsedTime += Time.deltaTime;
            UpdateUI();
        }
    }

    public void AddSheepScore()
    {
        int points = 10;

        if (elapsedTime < 30f)
        {
            points = Mathf.RoundToInt(10f * 1.2f);
        }
        else if (elapsedTime < 45f)
        {
            points = Mathf.RoundToInt(10f * 1.1f);
        }
        else if (elapsedTime < 60f)
        {
            points = Mathf.RoundToInt(10f * 1.05f);
        }

        score += points;

        Debug.Log("Score: " + score + " | Zeit: " + elapsedTime.ToString("F1") + "s | +" + points);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score;
        }

        if (timerText != null)
        {
            timerText.text = "Time: " + elapsedTime.ToString("F1") + " s";
        }
    }
}