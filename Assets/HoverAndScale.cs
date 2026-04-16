using UnityEngine;
using DG.Tweening;

public class HoverAndScale : MonoBehaviour
{
    [Header("Hover Settings")]
    public float hoverHeight = 0.5f;
    public float hoverSpeed = 1f;
    public float hoverVariance = 0.2f;

    [Header("Scale Loop Settings")]
    public float scaleMultiplier = 1.2f;
    public float scaleSpeed = 1f;
    public float scaleVariance = 0.2f;

    [Header("Transition Settings")]
    public float transitionDuration = 0.4f;
    public Ease introEase = Ease.OutBack;
    public Ease outroEase = Ease.InBack;

    private Vector3 startPos;
    private Vector3 startScale;
    private string tweenID;

    void Awake()
    {
        tweenID = "HoverScale_" + gameObject.GetInstanceID();
        startPos = transform.localPosition;
        startScale = transform.localScale;
    }

    void OnEnable()
    {
        transform.DOKill();

        float varHoverHeight = hoverHeight + Random.Range(-hoverVariance, hoverVariance);
        float varScaleMult = scaleMultiplier + Random.Range(-scaleVariance, scaleVariance);
        float varHoverSpeed = hoverSpeed + Random.Range(-hoverVariance, hoverVariance);
        float varScaleSpeed = scaleSpeed + Random.Range(-scaleVariance, scaleVariance);
        float randomPhase = Random.Range(0f, 1f);

        float initialYOffset = Mathf.Lerp(-varHoverHeight, varHoverHeight, randomPhase);
        float initialScaleOffset = Mathf.Lerp(1f, varScaleMult, randomPhase);

        Vector3 spawnPos = new Vector3(startPos.x, startPos.y + initialYOffset, startPos.z);
        Vector3 spawnScale = startScale * initialScaleOffset;

        transform.localScale = Vector3.zero;
        transform.localPosition = startPos;

        // Apply SetUpdate(true) to Intro
        transform.DOLocalMove(spawnPos, transitionDuration).SetEase(introEase).SetUpdate(true);
        transform.DOScale(spawnScale, transitionDuration)
            .SetEase(introEase)
            .SetUpdate(true)
            .OnComplete(() => StartLooping(spawnPos, spawnScale, varHoverHeight, varScaleMult, varHoverSpeed, varScaleSpeed));
    }

    private void StartLooping(Vector3 currentPos, Vector3 currentScale, float hHeight, float sMult, float hSpeed, float sSpeed)
    {
        float targetY = (currentPos.y >= startPos.y) ? startPos.y - hHeight : startPos.y + hHeight;

        // Apply SetUpdate(true) to continuous loops
        transform.DOLocalMoveY(targetY, hSpeed)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetId(tweenID)
            .SetUpdate(true);

        Vector3 targetScale = (currentScale.magnitude >= (startScale * sMult).magnitude) ? startScale : startScale * sMult;

        transform.DOScale(targetScale, sSpeed)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetId(tweenID)
            .SetUpdate(true);
    }

    public void Descale()
    {
        transform.DOKill();
        transform.DOScale(Vector3.zero, transitionDuration)
            .SetEase(outroEase)
            .SetUpdate(true)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void OnDisable() => transform.DOKill();
}