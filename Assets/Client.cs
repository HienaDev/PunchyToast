using UnityEngine;
using DG.Tweening;

public class Client : MonoBehaviour
{
    [Header("Settings")]
    public Transform mouthBone;
    public Transform pivot;
    public Transform TargetForToast;
    public Vector3 mouthOpenRotation = new Vector3(-45, 0, 0);
    public float entranceDuration = 0.6f;
    public float popUpDistance = 2.0f;

    [Header("Waiting Animation Settings")]
    [SerializeField] private Vector3 waitingMouthClosed = new Vector3(-10, 0, 0); // Always slightly open
    [SerializeField] private Vector3 waitingMouthOpen = new Vector3(-20, 0, 0);   // Opens a bit more
    [SerializeField] private float waitingCycleDuration = 0.4f;

    [Header("Randomized Hopping")]
    [SerializeField] private float minHopHeight = 0.02f;
    [SerializeField] private float maxHopHeight = 0.08f;
    [SerializeField] private float minHopSpeed = 0.3f;
    [SerializeField] private float maxHopSpeed = 0.6f;

    [Header("Current Order")]
    public string desiredCondiment;
    public Color condimentColor;
    public bool isSatisfied = false;
    public bool isSat = false;

    [SerializeField] private int numberOfBites = 2;

    private Vector3 originalMouthRot;
    private Vector3 pivotInitialLocalPos;
    private Transform mySeat;
    private Vector3 targetPosition;

    [SerializeField] private Transform eyeLidL;
    [SerializeField] private Transform eyeRidL;

    private Tween hopTween;
    private Tween mouthTween;

    void Start()
    {
        originalMouthRot = mouthBone.localEulerAngles;
        if (pivot != null) pivotInitialLocalPos = pivot.localPosition;

        StartBlinking();
    }

    public void Initialize(Transform seat)
    {
        mySeat = seat;
        targetPosition = transform.position;
        EnterScene();
    }

    private void EnterScene()
    {
        transform.position = targetPosition + Vector3.down * popUpDistance;
        transform.DOMove(targetPosition, entranceDuration).SetEase(Ease.Linear).OnComplete(() => {
            isSat = true;
            StartRandomHop();
            StartWaitingForFood();
        });
    }

    private void StartRandomHop()
    {
        if (pivot == null || isSatisfied) return;

        float randomHeight = Random.Range(minHopHeight, maxHopHeight);
        float randomSpeed = Random.Range(minHopSpeed, maxHopSpeed);

        hopTween = pivot.DOLocalMoveY(pivotInitialLocalPos.y + randomHeight, randomSpeed)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => {
                pivot.DOLocalMoveY(pivotInitialLocalPos.y, randomSpeed)
                    .SetEase(Ease.InOutQuad)
                    .OnComplete(StartRandomHop);
            });
    }

    private void StartWaitingForFood()
    {
        if (mouthBone == null || isSatisfied) return;

        // Start at the 'closed' waiting position
        mouthBone.localEulerAngles = waitingMouthClosed;

        // Loop between the two custom waiting rotations
        mouthTween = mouthBone.DOLocalRotate(waitingMouthOpen, waitingCycleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void Satisfy()
    {
        isSatisfied = true;

        if (hopTween != null) hopTween.Kill();
        if (mouthTween != null) mouthTween.Kill();

        if (pivot != null)
        {
            pivot.DOLocalRotate(Vector3.zero, 0.2f);
            pivot.DOLocalMove(pivotInitialLocalPos, 0.2f);
        }
    }

    private void StartBlinking()
    {
        float delay = Random.Range(2f, 4f);

        DOVirtual.DelayedCall(delay, () => {
            if (this == null) return;

            eyeLidL.DOLocalRotate(new Vector3(-90, 0, 0), 0.1f);
            eyeRidL.DOLocalRotate(new Vector3(-90, 0, 0), 0.1f).OnComplete(() => {
                eyeLidL.DOLocalRotate(Vector3.zero, 0.1f);
                eyeRidL.DOLocalRotate(Vector3.zero, 0.1f);
                StartBlinking();
            });
        });
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

    public void PlayMunchAnimation(System.Action onComplete)
    {
        Sequence munchSeq = DOTween.Sequence();

        for (int i = 0; i < numberOfBites; i++)
        {
            // Munching returns to the absolute original rotation for a "clamp" effect
            munchSeq.Append(mouthBone.DOLocalRotate(originalMouthRot, 0.08f).SetEase(Ease.Linear));
            munchSeq.Append(mouthBone.DOLocalRotate(mouthOpenRotation / 3f, 0.08f).SetEase(Ease.Linear));
        }

        munchSeq.Append(mouthBone.DOLocalRotate(originalMouthRot, 0.05f).SetEase(Ease.Linear));
        munchSeq.OnComplete(() => onComplete?.Invoke());
    }

    public void TryEatToast(string incomingJam, GameObject toast)
    {
        if (incomingJam == desiredCondiment)
        {
            Satisfy();
            OpenMouth();

            DOVirtual.DelayedCall(0.3f, () => {
                if (toast != null) Destroy(toast);

                PlayMunchAnimation(() => {
                    ReceiveFood();
                });
            });
        }
        else
        {
            transform.DOShakePosition(0.4f, new Vector3(0.2f, 0, 0), 10, 90);
        }
    }

    public void ReceiveFood()
    {
        ClientManager.Instance.OnClientFinished();
        if (mySeat != null) ClientManager.Instance.ClearSeat(mySeat);
        ExitScene();
    }

    private void ExitScene()
    {
        Sequence exitSeq = DOTween.Sequence();
        exitSeq.Append(transform.DOMoveY(targetPosition.y + 0.1f, 0.15f).SetEase(Ease.OutQuad));
        exitSeq.Append(transform.DOMoveY(targetPosition.y - popUpDistance, 0.4f).SetEase(Ease.InBack));
        exitSeq.OnComplete(() => Destroy(gameObject));
    }
}