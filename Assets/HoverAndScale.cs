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
        startPos = transform.localPosition; // Use localPosition for better stability
        startScale = transform.localScale;
    }

    void OnEnable()
    {
        transform.DOKill();

        // 1. Determine the randomized targets for this "life cycle" immediately
        float varHoverHeight = hoverHeight + Random.Range(-hoverVariance, hoverVariance);
        float varScaleMult = scaleMultiplier + Random.Range(-scaleVariance, scaleVariance);
        float varHoverSpeed = hoverSpeed + Random.Range(-hoverVariance, hoverVariance);
        float varScaleSpeed = scaleSpeed + Random.Range(-scaleVariance, scaleVariance);

        // 2. Pick a random starting point in the "sine wave" (0 to 1)
        float randomPhase = Random.Range(0f, 1f);

        // Calculate where the object SHOULD be based on that phase
        // This prevents the "snap" because we scale into the offset position
        float initialYOffset = Mathf.Lerp(-varHoverHeight, varHoverHeight, randomPhase);
        float initialScaleOffset = Mathf.Lerp(1f, varScaleMult, randomPhase);

        Vector3 spawnPos = new Vector3(startPos.x, startPos.y + initialYOffset, startPos.z);
        Vector3 spawnScale = startScale * initialScaleOffset;

        // 3. Reset and Intro
        transform.localScale = Vector3.zero;
        transform.localPosition = startPos;

        // Animate from 0 to the ALREADY OFFSET position/scale
        transform.DOLocalMove(spawnPos, transitionDuration).SetEase(introEase);
        transform.DOScale(spawnScale, transitionDuration)
            .SetEase(introEase)
            .OnComplete(() => StartLooping(spawnPos, spawnScale, varHoverHeight, varScaleMult, varHoverSpeed, varScaleSpeed));
    }

    private void StartLooping(Vector3 currentPos, Vector3 currentScale, float hHeight, float sMult, float hSpeed, float sSpeed)
    {
        // 4. Start the Hover Loop from its current offset
        // We use the opposite bound as the target so it continues the motion
        float targetY = (currentPos.y >= startPos.y) ? startPos.y - hHeight : startPos.y + hHeight;

        transform.DOLocalMoveY(targetY, hSpeed)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetId(tweenID);

        // 5. Start the Scale Loop from its current offset
        Vector3 targetScale = (currentScale.magnitude >= (startScale * sMult).magnitude) ? startScale : startScale * sMult;

        transform.DOScale(targetScale, sSpeed)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetId(tweenID);
    }

    public void Descale()
    {
        transform.DOKill(); // Kill loops immediately
        transform.DOScale(Vector3.zero, transitionDuration)
            .SetEase(outroEase)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void OnDisable()
    {
        transform.DOKill();
    }
}