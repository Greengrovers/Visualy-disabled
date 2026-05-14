using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ProgressUI : MonoBehaviour
{
    [SerializeField] private Canvas winCanvas;
    [SerializeField] private Canvas loseCanvas;

    [SerializeField] private TextMeshProUGUI winTimeText;
    [SerializeField] private TextMeshProUGUI winScoreText;

    [SerializeField] private string nextSceneName;

    private void Start()
    {
        if (winCanvas != null)
            winCanvas.gameObject.SetActive(false);

        if (loseCanvas != null)
            loseCanvas.gameObject.SetActive(false);
    }

    public void ShowWinScreen()
    {
        if (winCanvas != null)
            winCanvas.gameObject.SetActive(true);

        UpdateWinUI();
    }

    public void ShowLoseScreen()
    {
        if (loseCanvas != null)
            loseCanvas.gameObject.SetActive(true);
    }

    private void UpdateWinUI()
    {
        ScoreManager scoreManager = ScoreManager.Instance;

        if (scoreManager == null) return;

        if (winTimeText != null)
            winTimeText.text = "Time: " + scoreManager.elapsedTime.ToString("F1") + " s";

        if (winScoreText != null)
            winScoreText.text = "Score: " + scoreManager.score;
    }

    public void OnNextLevelButtonPressed()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("ProgressUI: Next scene name is not assigned!");
        }
    }

    public void OnRetryButtonPressed()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
