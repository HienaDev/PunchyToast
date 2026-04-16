using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class LevelComplete : MonoBehaviour
{
    [SerializeField] private Image[] stars;
    [SerializeField] private Sprite filledStar;
    [SerializeField] private Sprite emptyStar;
    [SerializeField] private TextMeshProUGUI timeText;

    [Header("Animation Settings")]
    [SerializeField] private float starPopDuration = 0.3f;
    [SerializeField] private float delayBetweenStars = 0.15f;

    private Vector3 panelOriginalScale;
    private Vector3 timeOriginalScale;
    private Dictionary<int, Vector3> starScales = new Dictionary<int, Vector3>();

    [SerializeField] private AudioClip[] starPopSounds;

    public void Initialize(int starNumber, float totalTime)
    {
        // 1. Capture original scales and reset to zero
        panelOriginalScale = transform.localScale;
        timeOriginalScale = timeText.transform.localScale;

        for (int i = 0; i < stars.Length; i++)
        {
            // Store the specific scale of every star in the array
            starScales[i] = stars[i].transform.localScale;

            // Setup sprites and hide
            stars[i].sprite = i < starNumber ? filledStar : emptyStar;
            stars[i].transform.localScale = Vector3.zero;
        }

        transform.localScale = Vector3.zero;
        timeText.transform.localScale = Vector3.zero;
        timeText.gameObject.SetActive(true); // Set active now so we can scale it later

        // 2. Format Time String
        int minutes = Mathf.FloorToInt(totalTime / 60);
        int seconds = Mathf.FloorToInt(totalTime % 60);
        timeText.text = $"{minutes:00}m:{seconds:00}s";

        // 3. Animation Sequence
        Sequence finishSeq = DOTween.Sequence();

        // Step A: Main panel explosion
        finishSeq.Append(transform.DOScale(panelOriginalScale, 0.5f).SetEase(Ease.OutBack));

        // Step B: Pop stars in one by one using their unique original scales
        for (int i = 0; i < stars.Length; i++)
        {
            int index = i;
            AudioManager.Instance.PlaySound(starPopSounds);
            finishSeq.Append(stars[index].transform.DOScale(starScales[index], starPopDuration).SetEase(Ease.OutBack));

            if (index < starNumber)
            {
                // Add a little celebratory "punch" on top of the scale
                finishSeq.Join(stars[index].transform.DOPunchRotation(new Vector3(0, 0, 15), starPopDuration));
            }

            finishSeq.AppendInterval(delayBetweenStars);
        }

        // Step C: Show the time last
        finishSeq.Append(timeText.transform.DOScale(timeOriginalScale, 0.4f).SetEase(Ease.OutBack));
    }
}