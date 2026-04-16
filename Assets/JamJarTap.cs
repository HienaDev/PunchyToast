using UnityEngine;

public class JamJarTap : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Match this flavor to the specific jar this script is on")]
    [SerializeField] private JamFlavor myFlavor;

    private void OnMouseDown()
    {
        // Ignore clicks if the game is paused (Time.timeScale = 0)
        if (Time.timeScale == 0) return;

        // Ask the JamDecider to select this flavor
        JamDecider.Instance.SelectByFlavor(myFlavor);
    }
}