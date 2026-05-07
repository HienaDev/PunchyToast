using UnityEngine;
using DG.Tweening;

public class TransformScaler : MonoBehaviour
{
    private Vector3 _originalScale;
    private Tween _activeTween;
    private int _steps = 0;

    [SerializeField] private Transform transformToScale;

    [Header("Tween Settings")]
    [SerializeField] private float duration = 0.15f;
    [SerializeField] private float elasticity = 0.5f;

    void Start()
    {
        _originalScale = transformToScale.localScale;
    }

    public void IncreaseScale(float amount)
    {
        _steps++;

        Vector3 targetScale = _originalScale + (_originalScale * amount * _steps);

        _activeTween?.Kill(complete: false);
        transformToScale.localScale = targetScale;

        Vector3 punch = _originalScale * amount;
        _activeTween = transformToScale.DOPunchScale(punch, duration, 5, elasticity)
            .SetEase(Ease.OutQuad)
            .OnKill(() => transformToScale.localScale = targetScale)
            .OnComplete(() => transformToScale.localScale = targetScale);
    }

    public void ResetScale()
    {
        _activeTween?.Kill(complete: false);
        _steps = 0;
        transformToScale.localScale = _originalScale;
    }
}