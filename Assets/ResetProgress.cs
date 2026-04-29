using System.Linq;
using UnityEngine;

public class ResetProgress : MonoBehaviour
{
    public void ResetLevelProgress()
    {

        LevelConfiguration[] allConfigs = Resources.LoadAll<LevelConfiguration>("Levels");


        foreach (var config in allConfigs)
        {
            // Delete the Stars entry
            PlayerPrefs.DeleteKey($"Level_{config.levelNumber}_Stars");

            // Delete the Time entry
            PlayerPrefs.DeleteKey($"Level_{config.levelNumber}_Time");
        }

        // Optional: Reset the "Last Played" tracking
        PlayerPrefs.DeleteKey("LastPlayedLevel");

        PlayerPrefs.Save();
    }
}
