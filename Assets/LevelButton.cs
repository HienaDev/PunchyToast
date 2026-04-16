using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.STP;

public class LevelButton : MonoBehaviour
{

    [SerializeField] private Image[] levelStars;
    [SerializeField] private Sprite fullStar;
    [SerializeField] private Sprite emptyStar;
    [SerializeField] private TextMeshProUGUI levelNumber;
    [SerializeField] private TextMeshProUGUI bestTime;

    [SerializeField] private Button levelButton;

    private LevelConfiguration levelConfig;
    private GameObject menuPanel; // Reference to the menu to close it


    public void Initialize(LevelConfiguration config, GameObject menuToClose)
    {
        levelConfig = config;
        menuPanel = menuToClose;

        levelNumber.text = config.levelNumber.ToString();
        int starsEarned = PlayerPrefs.GetInt($"Level_{config.levelNumber}_Stars", 0);
        for (int i = 0; i < levelStars.Length; i++)
        {
            levelStars[i].sprite = i < starsEarned ? fullStar : emptyStar;
        }

        // Add best time with minutes:seconds and add -- if no time recorded
        float bestTimeValue = PlayerPrefs.GetFloat($"Level_{config.levelNumber}_Time", -1f);
        if (bestTimeValue >= 0f)
        {
            int minutes = Mathf.FloorToInt(bestTimeValue / 60f);
            int seconds = Mathf.FloorToInt(bestTimeValue % 60f);
            bestTime.text = $"{minutes:00}m:{seconds:00}s";
        }
        else
        {
            bestTime.text = "--:--";
        }


        levelButton.onClick.AddListener(StartLevel);
    }

    public void StartLevel()
    {
        ClientManager.Instance.StartLevel(levelConfig);

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.FadeToLevel();
        }

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }
}
