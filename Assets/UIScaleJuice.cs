using UnityEngine;
using DG.Tweening;

public class UIScaleJuice : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    private Vector3 originalScale;
    private Tween scaleTween;

    [SerializeField] private AudioClip[] hoverSounds;

    void Awake() => originalScale = transform.localScale;

    public void ScaleUp()
    {
        AudioManager.Instance.PlaySound(hoverSounds);
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale * scaleMultiplier, duration)
            .SetEase(scaleEase)
            .SetUpdate(true); // <--- IGNORES TIMESCALE 0
    }

    public void ScaleDown()
    {
        AudioManager.Instance.PlaySound(hoverSounds);
        scaleTween?.Kill();
        scaleTween = transform.DOScale(originalScale, duration)
            .SetEase(scaleEase)
            .SetUpdate(true); // <--- IGNORES TIMESCALE 0
    }

    private void OnDisable()
    {
        scaleTween?.Kill();
        transform.localScale = originalScale;
    }
}