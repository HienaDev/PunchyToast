using UnityEngine;
using DG.Tweening; // Required for killing tweens

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private GameObject levelSelectionPanel; // Assign your Level Select screen here
    [SerializeField] private ToasterCustomization toasterCustomization;
    [SerializeField] private GameObject gameplayUI;         // The UI that shows during play (combo, etc)

    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject menuPuppet;

    private bool isPaused = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !mainMenu.activeSelf && !levelSelectionPanel.activeSelf && !toasterCustomization.triggered)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        pauseMenuPanel.SetActive(isPaused);


    }

    public void BackToLevelSelection()
    {
        // 1. Resume time so things can be destroyed properly if needed
        Time.timeScale = 1f;
        isPaused = false;

        // 3. Call the Full Reset on the Managers
        ClientManager.Instance.FullResetGame();

        // 4. UI Swapping
        pauseMenuPanel.SetActive(false);
        gameplayUI.SetActive(false);

        if(!EndlessModeManager.Instance.isRunning)
            levelSelectionPanel.SetActive(true);
        else
        {
            mainMenu.SetActive(true);
            menuPuppet.SetActive(true);
            EndlessModeManager.Instance.HideUI();
        }

    }

    public void ResumeGame()
    {
        if (isPaused) TogglePause();
    }
}