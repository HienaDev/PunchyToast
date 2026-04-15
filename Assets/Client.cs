using UnityEngine;
using DG.Tweening;

public class Client : MonoBehaviour
{
    [Header("Settings")]
    public Transform mouthBone;
    public Vector3 mouthOpenRotation = new Vector3(-45, 0, 0);
    public float entranceDuration = 0.6f;
    public float popUpDistance = 2.0f;

    [Header("Current Order")]
    public string desiredCondiment;
    public Color condimentColor;
    public bool isSatisfied = false;

    private Vector3 originalMouthRot;
    private Transform mySeat;
    private Vector3 targetPosition;

    void Start()
    {
        // Store initial rotation for the "Closed" state
        originalMouthRot = mouthBone.localEulerAngles;
    }

    public void Initialize(Transform seat)
    {
        mySeat = seat;
        targetPosition = transform.position;
        EnterScene();
    }

    private void EnterScene()
    {
        // Start below and pop up with a bounce
        transform.position = targetPosition + Vector3.down * popUpDistance;
        transform.DOMove(targetPosition, entranceDuration).SetEase(Ease.OutBack);
    }

    public void SetOrder(string jamName, Color jamColor)
    {
        desiredCondiment = jamName;
        condimentColor = jamColor;

        TAG_Thought thought = GetComponentInChildren<TAG_Thought>();
        if (thought != null)
        {
            thought.GetComponent<Renderer>().material.SetColor("_BaseColor", condimentColor);
        }
    }

    public void OpenMouth()
    {
        mouthBone.DOLocalRotate(mouthOpenRotation, 0.15f).SetEase(Ease.OutQuad);
    }

    // New Munching Animation: Chops the mouth 3 times
    public void PlayMunchAnimation(System.Action onComplete)
    {
        // Sequence of quick open/close movements
        Sequence munchSeq = DOTween.Sequence();

        for (int i = 0; i < 3; i++)
        {
            munchSeq.Append(mouthBone.DOLocalRotate(originalMouthRot, 0.1f).SetEase(Ease.InQuad));
            munchSeq.Append(mouthBone.DOLocalRotate(mouthOpenRotation/3f, 0.1f).SetEase(Ease.OutQuad));
        }

        // Final snap shut and then trigger the callback
        munchSeq.Append(mouthBone.DOLocalRotate(originalMouthRot, 0.1f).SetEase(Ease.InBounce));
        munchSeq.OnComplete(() => onComplete?.Invoke());
    }

    public void TryEatToast(string incomingJam, GameObject toast)
    {
        if (incomingJam == desiredCondiment)
        {
            isSatisfied = true;
            OpenMouth();

            // Wait a moment for the food to "enter" the mouth
            DOVirtual.DelayedCall(0.3f, () => {
                if (toast != null) Destroy(toast);

                // Munch first, then leave
                PlayMunchAnimation(() => {
                    ReceiveFood();
                });
            });
        }
        else
        {
            // Shakes head "No"
            transform.DOShakePosition(0.4f, new Vector3(0.2f, 0, 0), 10, 90);
        }
    }

    public void ReceiveFood()
    {
        ClientManager.Instance.OnClientFinished();

        if (mySeat != null)
        {
            ClientManager.Instance.ClearSeat(mySeat);
        }

        ExitScene();
    }

    private void ExitScene()
    {
        Sequence exitSeq = DOTween.Sequence();

        // 1. Wind up (Small hop up)
        exitSeq.Append(transform.DOMoveY(targetPosition.y + 0.1f, 0.15f).SetEase(Ease.OutQuad));

        // 2. Dive down fast
        exitSeq.Append(transform.DOMoveY(targetPosition.y - popUpDistance, 0.4f).SetEase(Ease.InBack));

        // 3. Clean up
        exitSeq.OnComplete(() => Destroy(gameObject));
    }
}