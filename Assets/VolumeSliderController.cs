using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class VolumeSliderController : MonoBehaviour
{
    [Header("Audio")]
    public AudioMixer mixer;
    public string volumeParameter = "MasterVolume";

    [Header("UI")]
    public Slider volumeSlider;

    [SerializeField] private string PlayerPrefsKey = "MasterVolumeValue";


    [SerializeField] private AudioClip[] testSounds;

    void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat(PlayerPrefsKey, 1f);

        SetVolume(savedVolume);

        // Update slider UI
        volumeSlider.value = savedVolume;

        // Listen for slider changes
        volumeSlider.onValueChanged.AddListener(OnSliderValueChanged);


    }

    public void PlayTestSound()
    {
        if (testSounds.Length > 0)
        {
            GetComponent<AudioSource>().PlayOneShot(testSounds[Random.Range(0, testSounds.Length)]);
        }
    }

    private void OnSliderValueChanged(float value)
    {
        SetVolume(value);
        PlayerPrefs.SetFloat(PlayerPrefsKey, value);

    }

    private void SetVolume(float value)
    {
        // Protect against log(0)
        if (value <= 0.0001f) value = 0.0001f;

        float dB = Mathf.Log10(value) * 20f;
        mixer.SetFloat(volumeParameter, dB);

        mixer.GetFloat(volumeParameter, out float checkValue);



    }
}
