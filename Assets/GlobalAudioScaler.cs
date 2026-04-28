using UnityEngine;

public class GlobalAudioScaler : MonoBehaviour
{
    void Update()
    {
        // This force-syncs the global audio pitch to your game's Time.timeScale
        // If timeScale is 0.2f, the audio plays at 0.2f speed (and sounds lower)
        AudioListener.pause = (Time.timeScale == 0); // Handle pausing

        // Optional: Clamping it so it doesn't get TOO deep/distorted
        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (var source in allSources)
        {
            source.pitch = Time.timeScale;
        }
    }
}