using UnityEngine;
using UnityEngine.EventSystems; // Required if you want to use OnPointerEnter/Exit
using DG.Tweening;

public class UIScaleJuice : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float scaleMultiplier = 1.1f;
    [SerializeField] private float duration = 0.2f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    private Vector3 originalScale;
    private Tween scaleTween;

    void Awake()
    {
        // Store the starting scale so we always know what "normal" is
        originalScale = transform.localScale;
    }

    public void ScaleUp()
    {
        // Kill any existing tween to prevent overlapping logic
        scaleTween?.Kill();

        scaleTween = transform.DOScale(originalScale * scaleMultiplier, duration)
            .SetEase(scaleEase)
            .SetUpdate(true); // Works even if Time.timeScale is 0 (paused)
    }

    public void ScaleDown()
    {
        scaleTween?.Kill();

        scaleTween = transform.DOScale(originalScale, duration)
            .SetEase(scaleEase)
            .SetUpdate(true);
    }

    // Optional: Reset on disable so it doesn't get stuck scaled up
    private void OnDisable()
    {
        scaleTween?.Kill();
        transform.localScale = originalScale;
    }
}