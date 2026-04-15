using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LevelComplete : MonoBehaviour
{
    [SerializeField] private Image[] stars;
    [SerializeField] private Sprite filledStar;
    [SerializeField] private Sprite emptyStar;
    [SerializeField] private TextMeshProUGUI time;

    private Vector3 originalScale;

    public void Initialize(int starNumber, float time)
    {
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].sprite = i < starNumber ? filledStar : emptyStar;
        }

        // time with minutes and seconds
        int minutes = Mathf.FloorToInt(time / 60);
        int seconds = Mathf.FloorToInt(time % 60);
        this.time.text = $"{minutes:00}m:{seconds:00}s";

        originalScale = transform.localScale;
        // Dotween scale animation like its showing up and scaling like an happy explosion
        transform.localScale = Vector3.zero;

        // Animate to original scale with a bounce effect
        transform.DOScale(originalScale, 0.5f).SetEase(Ease.OutBack);

    }


}
