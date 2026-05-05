using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

public class StickerUIItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject associatedSticker;
    public Image stickerPreviewImage;

    private Vector3 originalStickerSize;
    private Color originalColor = Color.white;
    private DecalProjector projector;
    private Sequence hoverSequence;

    public Button xButton;

    public void Setup(GameObject sticker, Sprite sprite)
    {
        associatedSticker = sticker;
        stickerPreviewImage.sprite = sprite;
        projector = associatedSticker.GetComponent<DecalProjector>();
        originalStickerSize = projector.size;

        // Apply your custom Resolution rules (Max H: 16, Max W: 24)
        AdjustUIImageSize(sprite);
    }

    private void AdjustUIImageSize(Sprite s)
    {
        float sw = s.rect.width;
        float sh = s.rect.height;
        float ratio = sw / sh;

        float targetH = 16f;
        float targetW = targetH * ratio;

        // If the width exceeds 24 with height 16, we shrink based on width instead
        if (targetW > 24f)
        {
            targetW = 24f;
            targetH = targetW / ratio;
        }

        stickerPreviewImage.rectTransform.sizeDelta = new Vector2(targetW, targetH);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (associatedSticker == null) return;

        hoverSequence?.Kill();
        hoverSequence = DOTween.Sequence();

        float cycleTime = 0.5f; // Time to go from small to big

        // 1. Scale Pulse: Use Ease.Linear to remove the "stop" at the peaks
        hoverSequence.Join(DOTween.To(() => projector.size, x => projector.size = x, originalStickerSize * 1.1f, cycleTime)
            .SetEase(Ease.Linear));

        // 2. Color Blink: Two blinks per scale phase
        // Use Ease.Linear here too for a constant "heartbeat" flash
        Sequence colorSeq = DOTween.Sequence();
        colorSeq.Append(projector.material.DOColor(Color.red, "_BaseColor", cycleTime / 2f).SetEase(Ease.Linear));
        colorSeq.Append(projector.material.DOColor(new Color(1, 0, 0, 0.25f), "_BaseColor", cycleTime / 2f).SetEase(Ease.Linear));
        colorSeq.SetLoops(1);

        hoverSequence.Join(colorSeq);

        // Yoyo + Linear Ease = A smooth, non-stop bouncing motion
        hoverSequence.SetLoops(-1, LoopType.Yoyo);
    }

    private void ResetSticker()
    {
        if (associatedSticker == null) return;

        hoverSequence?.Kill();

        // Smoothly return to original state instead of snapping
        projector.material.DOColor(originalColor, "_BaseColor", 0.2f);
        DOTween.To(() => projector.size, x => projector.size = x, originalStickerSize, 0.2f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetSticker();
    }



    public void DeleteSticker()
    {
        ResetSticker(); // Cleanup tweens before destroying
        var manager = Object.FindFirstObjectByType<ToasterCustomization>();
        if (manager != null) manager.RemoveStickerFromList(associatedSticker, gameObject);

        Destroy(associatedSticker);
        Destroy(gameObject);
    }
}