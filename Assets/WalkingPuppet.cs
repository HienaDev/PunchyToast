using UnityEngine;
using DG.Tweening;

public class WalkingPuppet : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkDistance = 10f;
    [SerializeField] private float walkDuration = 5f;
    [SerializeField] private Ease walkEase = Ease.Linear;

    [Header("Spooky Stop Settings")]
    [SerializeField][Range(0f, 1f)] private float stopChance = 0.1f;
    [SerializeField] private float minWaitTime = 1f;
    [SerializeField] private float maxWaitTime = 3f;
    [SerializeField] private float pushAmount = 0.5f; // How much to step toward the camera
    [SerializeField] private Transform mouthBone;
    [SerializeField][Range(0f, 1f)] private float mouthChatterChance = 0.5f;

    [Header("Hopping (From Client)")]
    [SerializeField] private Transform pivot;
    [SerializeField] private float minHopHeight = 0.05f;
    [SerializeField] private float maxHopHeight = 0.12f;
    [SerializeField] private float minHopSpeed = 0.2f;
    [SerializeField] private float maxHopSpeed = 0.4f;

    private Vector3 pivotInitialLocalPos;
    private bool isWalking = true;
    private bool isStopped = false;
    private Tween walkTween;
    private Tween mouthTween;

    private Quaternion rotationBeforeStop;
    private Vector3 positionBeforeStop;

    void Start()
    {
        if (pivot != null) pivotInitialLocalPos = pivot.localPosition;

        StartWalking();
        StartRandomHop();

        // Check if we should perform the spooky stop
        if (Random.value < stopChance)
        {
            float randomTriggerTime = Random.Range(walkDuration * 0.1f, walkDuration * 0.8f);
            // Tagging the delayed call so it survives KillAll
            DOVirtual.DelayedCall(randomTriggerTime, TriggerSpookyStop).SetId("NPCPuppets");
        }
    }

    private void StartWalking()
    {
        walkTween = transform.DOMove(transform.position + transform.forward * walkDistance, walkDuration)
            .SetId("NPCPuppets")
            .SetEase(walkEase)
            .OnComplete(() =>
            {
                isWalking = false;
                Destroy(gameObject);
            });
    }

    private void TriggerSpookyStop()
    {
        if (this == null || !isWalking) return;

        isStopped = true;
        rotationBeforeStop = transform.rotation;
        positionBeforeStop = transform.position;
        walkTween.Pause();

        Vector3 directionToCam = Camera.main.transform.position - transform.position;
        directionToCam.y = 0;

        Quaternion targetRotation = Quaternion.LookRotation(directionToCam) * Quaternion.Euler(0, 180, 0);
        transform.DORotateQuaternion(targetRotation, 0.3f).SetId("NPCPuppets").SetEase(Ease.OutCubic);

        Vector3 pushPos = transform.position + (directionToCam.normalized * pushAmount);
        transform.DOMove(pushPos, 0.3f).SetId("NPCPuppets").SetEase(Ease.OutCubic);

        if (Random.value < mouthChatterChance) StartMouthChatter();

        float waitTime = Random.Range(minWaitTime, maxWaitTime);
        DOVirtual.DelayedCall(waitTime, ResumeWalking).SetId("NPCPuppets");
    }

    private void ResumeWalking()
    {
        if (this == null) return;

        isStopped = false;
        if (mouthTween != null) mouthTween.Kill();
        if (mouthBone != null) mouthBone.DOLocalRotate(Vector3.zero, 0.2f).SetId("NPCPuppets");

        transform.DOMove(positionBeforeStop, 0.3f).SetId("NPCPuppets").SetEase(Ease.InCubic);
        transform.DORotateQuaternion(rotationBeforeStop, 0.3f)
            .SetId("NPCPuppets")
            .SetEase(Ease.InCubic)
            .OnComplete(() => {
                if (walkTween != null) walkTween.Play();
            });
    }

    private void StartMouthChatter()
    {
        if (mouthBone == null) return;

        mouthTween = mouthBone.DOLocalRotate(new Vector3(15, 0, 0), 0.5f)
            .SetId("NPCPuppets")
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutQuad);
    }

    private void StartRandomHop()
    {
        if (pivot == null || !isWalking || isStopped)
        {
            DOVirtual.DelayedCall(0.1f, StartRandomHop).SetId("NPCPuppets");
            return;
        }

        float randomHeight = Random.Range(minHopHeight, maxHopHeight);
        float randomSpeed = Random.Range(minHopSpeed, maxHopSpeed);

        pivot.DOLocalMoveY(pivotInitialLocalPos.y + randomHeight, randomSpeed)
            .SetId("NPCPuppets")
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                pivot.DOLocalMoveY(pivotInitialLocalPos.y, randomSpeed)
                    .SetId("NPCPuppets")
                    .SetEase(Ease.InQuad)
                    .OnComplete(StartRandomHop);
            });
    }
}