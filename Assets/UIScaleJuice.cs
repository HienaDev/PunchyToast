using UnityEngine;
using DG.Tweening;
using UnityEngine.Audio;

public class UIScaleJuice : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    private Vector3 originalScale;
    private Tween scaleTween;

    // We no longer need to drag this in the Inspector!
    private AudioMixer sfxMixer;
    [SerializeField] private AudioClip[] hoverSounds;

    void Awake()
    {
        originalScale = transform.localScale;

        // Automatically find the Mixer in the Resources folder
        // Replace "MainMixer" with the actual name of your Mixer asset
        sfxMixer = Resources.Load<AudioMixer>("SFX");

        if (sfxMixer == null)
        {
            Debug.LogError($"UIScaleJuice on {gameObject.name} couldn't find the Mixer! Make sure it's in Assets/Resources/MainMixer");
        }
    }

    public void ScaleUp()
    {
        // Now sfxMixer is automatically ready to go
        AudioManager.Instance.PlaySound(hoverSounds, sfxMixer);

        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale * scaleMultiplier, duration)
            .SetEase(scaleEase)
            .SetUpdate(true);
    }

    public void ScaleDown()
    {
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale, duration)
            .SetEase(scaleEase)
            .SetUpdate(true);
    }

    private void OnDisable()
    {
        scaleTween?.Kill();
        transform.localScale = originalScale;
    }
}