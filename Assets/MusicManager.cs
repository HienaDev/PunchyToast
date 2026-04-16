using UnityEngine;
using DG.Tweening;
using UnityEngine.UIElements;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Header("Assign Menu Music to A, Level Music to B")]
    [SerializeField] private AudioSource sourceMenu;
    [SerializeField] private AudioSource sourceLevel;

    [SerializeField] private float fadeDuration = 1.2f;

    private bool soundOn = true;

    [SerializeField] private GameObject soundOnIcon;
    [SerializeField] private GameObject soundOffIcon;

    public void ToggleSound()
    {
        soundOn = !soundOn;
        
        if(soundOn)
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
            if (sourceMenu != null) sourceMenu.Play();
            if (sourceLevel != null) sourceLevel.Stop();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void FadeToLevel()
    {
        Crossfade(sourceMenu, sourceLevel);
    }

    public void FadeToMenu()
    {
        Crossfade(sourceLevel, sourceMenu);
    }

    private void Crossfade(AudioSource fromSource, AudioSource toSource)
    {
        // 1. Kill any existing volume tweens on these sources to prevent fighting
        fromSource.DOKill();
        toSource.DOKill();

        // 2. Ensure the incoming source starts playing
        if (!toSource.isPlaying) toSource.Play();

        // 3. Use DOFade for smooth transitions
        // SetUpdate(true) ensures music fades even when the game is paused!
        fromSource.DOFade(0f, fadeDuration)
            .SetEase(Ease.Linear)
            .SetUpdate(true)
            .OnComplete(() => fromSource.Stop());

        toSource.DOFade(1f, fadeDuration)
            .SetEase(Ease.Linear)
            .SetUpdate(true);
    }
}