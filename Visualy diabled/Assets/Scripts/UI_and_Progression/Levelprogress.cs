using UnityEngine;

public class Levelprogress : MonoBehaviour
{
    [SerializeField] private ProgressUI progressUI;

    private SheepGroupManager sheepGroupManager;
    private bool levelEnded = false;

    private void Start()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (levelEnded) return;

        ResolveReferences();

        if (sheepGroupManager == null || sheepGroupManager.allSheep == null)
            return;

        int livingSheepCount = 0;
        int sheepInGoal = 0;

        foreach (SheepController sheep in sheepGroupManager.allSheep)
        {
            if (!IsCountableSheep(sheep))
                continue;

            livingSheepCount++;

            if (sheep.isInGoal)
                sheepInGoal++;
        }

        if (livingSheepCount == 0)
        {
            EndLevelLose();
            return;
        }

        // Check win condition: all remaining sheep in goal
        if (sheepInGoal == livingSheepCount)
        {
            EndLevelWin();
            return;
        }
    }

    private void ResolveReferences()
    {
        if (sheepGroupManager == null)
            sheepGroupManager = SheepGroupManager.Instance != null
                ? SheepGroupManager.Instance
                : FindFirstObjectByType<SheepGroupManager>();

        if (progressUI == null)
            progressUI = FindFirstObjectByType<ProgressUI>();
    }

    private bool IsCountableSheep(SheepController sheep)
    {
        if (sheep == null)
            return false;

        if (sheep.gameObject == null)
            return false;

        if (!sheep.gameObject.activeInHierarchy)
            return false;

        return true;
    }

    private void EndLevelWin()
    {
        levelEnded = true;
        Time.timeScale = 0f;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.timerRunning = false;

        if (progressUI != null)
            progressUI.ShowWinScreen();

        Debug.Log("Level Won! All sheep reached the goal.");
    }

    private void EndLevelLose()
    {
        levelEnded = true;
        Time.timeScale = 0f;

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.timerRunning = false;

        if (progressUI != null)
            progressUI.ShowLoseScreen();

        Debug.Log("Level Lost! All sheep are dead.");
    }
}
