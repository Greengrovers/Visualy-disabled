using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [SerializeField] private Canvas pauseMenuCanvas;
    [SerializeField] private Canvas settingsMenuCanvas;

    private bool isPaused = false;
    private float previousTimeScale = 1f;

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
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.gameObject.SetActive(false);

        if (settingsMenuCanvas != null)
            settingsMenuCanvas.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        if (isPaused) return;

        isPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.gameObject.SetActive(true);

        if (settingsMenuCanvas != null)
            settingsMenuCanvas.gameObject.SetActive(false);

        Debug.Log("Game Paused");
    }

    public void Resume()
    {
        if (!isPaused) return;

        isPaused = false;
        Time.timeScale = previousTimeScale;

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.gameObject.SetActive(false);

        if (settingsMenuCanvas != null)
            settingsMenuCanvas.gameObject.SetActive(false);

        Debug.Log("Game Resumed");
    }

    public void OpenSettings()
    {
        if (pauseMenuCanvas != null)
            pauseMenuCanvas.gameObject.SetActive(false);

        if (settingsMenuCanvas != null)
            settingsMenuCanvas.gameObject.SetActive(true);

        Debug.Log("Settings Opened");
    }

    public void CloseSettings()
    {
        if (settingsMenuCanvas != null)
            settingsMenuCanvas.gameObject.SetActive(false);

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.gameObject.SetActive(true);

        Debug.Log("Settings Closed");
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public bool IsPaused
    {
        get { return isPaused; }
    }
}
