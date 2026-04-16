using UnityEngine;

public class GameExitHandler : MonoBehaviour
{
    public void QuitGame()
    {
        // 1. Logs to console so you know the button actually worked
        Debug.Log("Quit button pressed!");

        // 2. If running in the Unity Editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif

        // 3. If running as a standalone build (PC/Mac) or Mobile
        Application.Quit();
    }
}