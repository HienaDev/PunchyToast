using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [SerializeField] private GameObject pauseMenuPanel;
    private bool isPaused = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        // Ensure menu is off at start
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            pauseMenuPanel.SetActive(true);
            // Optional: Show mouse cursor if it was hidden
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Time.timeScale = 1f;
            pauseMenuPanel.SetActive(false);
            // Optional: Re-hide cursor
            // Cursor.visible = false;
        }
    }

    // Call this from a "Quit" button in the menu
    public void ResumeGame()
    {
        if (isPaused) TogglePause();
    }
}