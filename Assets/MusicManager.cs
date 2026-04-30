using UnityEngine;
using DG.Tweening;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Assign Menu Music to A, Level Music to B")]
    [SerializeField] private AudioSource sourceMenu;
    [SerializeField] private AudioSource sourceLevel;

    [Header("Record Scratch Settings")]
    [SerializeField] private AudioSource stopScratchSource;   // Clip for "STOP"
    [SerializeField] private AudioSource resumeScratchSource; // Clip for "RESUME"
    [SerializeField] private float pitchDownDuration = 0.2f;

    [Header("General Settings")]
    [SerializeField] private float fadeDuration = 1.2f;

    private bool soundOn = true;
    private AudioSource activeSource; // Tracks which source is currently playing

    [SerializeField] private GameObject soundOnIcon;
    [SerializeField] private GameObject soundOffIcon;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Set initial volumes
            if (sourceMenu != null) sourceMenu.volume = 1f;
            if (sourceLevel != null) sourceLevel.volume = 0f;

            // Start the game with Menu music playing
            if (sourceMenu != null)
            {
                sourceMenu.Play();
                activeSource = sourceMenu;
            }
            if (sourceLevel != null) sourceLevel.Stop();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RecordScratchStop(float silenceDuration)
    {
        // If nothing is playing, don't do anything
        if (activeSource == null || !activeSource.isPlaying) return;

        // 1. Play the "Screech Stop" sound
        if (stopScratchSource != null) stopScratchSource.Play();

        // 2. Kill current fades, pitch down and fade music out
        activeSource.DOKill();
        activeSource.DOPitch(0, pitchDownDuration).SetEase(Ease.InQuad).SetUpdate(true);
        activeSource.DOFade(0, pitchDownDuration).SetUpdate(true).OnComplete(() =>
        {
            activeSource.Pause();

            // 3. Wait for the specified silence duration
            DOVirtual.DelayedCall(silenceDuration, () =>
            {
                // 4. Play the "Resume/Needle Drop" sound
                if (resumeScratchSource != null) resumeScratchSource.Play();

                // 5. Restore music
                activeSource.UnPause();
                activeSource.pitch = 1f; // Snap pitch back to normal
                activeSource.DOFade(1f, 0.15f).SetUpdate(true);
            }).SetUpdate(true);
        });
    }

    public void FadeToLevel()
    {
        activeSource = sourceLevel; // Update active tracker
        Crossfade(sourceMenu, sourceLevel);
    }

    public void FadeToMenu()
    {
        activeSource = sourceMenu; // Update active tracker
        Crossfade(sourceLevel, sourceMenu);
    }

    private void Crossfade(AudioSource fromSource, AudioSource toSource)
    {
        fromSource.DOKill();
        toSource.DOKill();

        if (!toSource.isPlaying) toSource.Play();
        toSource.pitch = 1f; // Ensure pitch is reset if we crossfade after a scratch

        fromSource.DOFade(0f, fadeDuration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() => fromSource.Stop());

        toSource.DOFade(1f, fadeDuration)
            .SetEase(Ease.Linear)
            .SetUpdate(true);
    }

    public void ToggleSound()
    {
        soundOn = !soundOn;

        if (soundOn)
        {
            Camera.main.GetComponent<AudioListener>().enabled = true;
            soundOnIcon.SetActive(true);
            soundOffIcon.SetActive(false);
        }
        else
        {
            Camera.main.GetComponent<AudioListener>().enabled = false;
            soundOnIcon.SetActive(false);
            soundOffIcon.SetActive(true);
        }
    }
}