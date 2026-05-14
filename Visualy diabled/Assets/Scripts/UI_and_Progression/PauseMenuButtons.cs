using UnityEngine;

public class PauseMenuButtons : MonoBehaviour
{
    public void OnResumeButtonPressed()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.Resume();
    }

    public void OnSettingsButtonPressed()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.OpenSettings();
    }

    public void OnQuitButtonPressed()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.QuitGame();
    }

    public void OnBackButtonPressed()
    {
        if (PauseManager.Instance != null)
            PauseManager.Instance.CloseSettings();
    }
}
