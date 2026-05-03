using UnityEngine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class ClientPuppet : MonoBehaviour
{
    private float moveSpeed;
    private float sessionDuration;
    private float mouthMaxOpen;
    private float talkChance;

    private Transform[] currentPath;
    private Transform assignedSeat;
    private int pathIndex = 0;

    [Header("Logic Refs")]
    [SerializeField] private Transform mouthBone;
    [SerializeField] private Transform pivot;

    [Header("Props")]
    [SerializeField] private List<GameObject> internalProps;

    [Header("Hopping Settings")]
    [SerializeField] private float minHopHeight = 0.05f;
    [SerializeField] private float maxHopHeight = 0.12f;
    [SerializeField] private float minHopSpeed = 0.2f;
    [SerializeField] private float maxHopSpeed = 0.4f;

    [Header("Talking Timing")]
    [SerializeField] private Vector2 talkSpeedRange = new Vector2(0.08f, 0.25f);
    [SerializeField] private Vector2 sentenceLengthRange = new Vector2(3, 8);
    [SerializeField] private Vector2 pauseDurationRange = new Vector2(0.5f, 2.5f);

    private bool isSeated = false;
    private bool isWalking = false;
    private bool isTalking = false;
    private GameObject activeProp;

    private Vector3 originalMouthRot;
    private Vector3 pivotInitialLocalPos;
    private Tween mouthTween;
    private Tween hopTween;
    private int cyclesUntilPause;

    void Awake()
    {
        originalMouthRot = mouthBone.localEulerAngles;
        if (pivot != null) pivotInitialLocalPos = pivot.localPosition;

        foreach (GameObject prop in internalProps)
        {
            if (prop != null) prop.SetActive(false);
        }
    }

    public void Initialize(Transform[] path, Transform seat, float speed, float duration, float mouthRot, float talkProb)
    {
        currentPath = path;
        assignedSeat = seat;
        moveSpeed = speed;
        sessionDuration = duration;
        mouthMaxOpen = mouthRot;
        talkChance = talkProb;

        isWalking = true;
        StartRandomHop();
        StartCoroutine(FollowPath());
    }

    private IEnumerator FollowPath()
    {
        while (pathIndex < currentPath.Length)
        {
            FaceTargetWithBackwardsAxis(currentPath[pathIndex].position);
            yield return MoveTo(currentPath[pathIndex].position);
            pathIndex++;
        }

        FaceTargetWithBackwardsAxis(assignedSeat.position);
        if (SeatedClientManager.Instance != null) SeatedClientManager.Instance.OpenDoors();

        yield return MoveTo(assignedSeat.position);
        transform.DORotateQuaternion(assignedSeat.rotation * Quaternion.Euler(0, 180, 0), 0.5f).SetId("NPCPuppets");
        OnReachedSeat();
    }

    private void FaceTargetWithBackwardsAxis(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180, 0);
            transform.DORotateQuaternion(lookRot, 0.3f).SetId("NPCPuppets");
        }
    }

    private IEnumerator MoveTo(Vector3 target)
    {
        float dist = Vector3.Distance(transform.position, target);
        float duration = dist / moveSpeed;
        yield return transform.DOMove(target, duration).SetId("NPCPuppets").SetEase(Ease.Linear).WaitForCompletion();
    }

    private void StartRandomHop()
    {
        if (pivot == null || !isWalking) return;
        float randomHeight = Random.Range(minHopHeight, maxHopHeight);
        float randomSpeed = Random.Range(minHopSpeed, maxHopSpeed);

        hopTween = pivot.DOLocalMoveY(pivotInitialLocalPos.y + randomHeight, randomSpeed)
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

    private void OnReachedSeat()
    {
        isWalking = false;
        isSeated = true;
        hopTween?.Kill();
        pivot.DOLocalMove(pivotInitialLocalPos, 0.2f).SetId("NPCPuppets");

        if (Random.value < 0.5f && internalProps.Count > 0)
        {
            int randomIndex = Random.Range(0, internalProps.Count);
            if (internalProps[randomIndex] != null)
            {
                activeProp = internalProps[randomIndex];
                activeProp.SetActive(true);
            }
        }

        if (Random.value < talkChance)
        {
            isTalking = true;
            ResetCycleCount();
            RandomizedMouthLoop();
        }

        Invoke(nameof(LeaveSeat), sessionDuration);
    }

    private void ResetCycleCount() { cyclesUntilPause = (int)Random.Range(sentenceLengthRange.x, sentenceLengthRange.y); }

    private void RandomizedMouthLoop()
    {
        if (!isTalking || mouthBone == null) return;
        if (cyclesUntilPause <= 0)
        {
            float pauseDuration = Random.Range(pauseDurationRange.x, pauseDurationRange.y);
            ResetCycleCount();
            DOVirtual.DelayedCall(pauseDuration, RandomizedMouthLoop).SetId("NPCPuppets");
            return;
        }
        cyclesUntilPause--;
        float randomDuration = Random.Range(talkSpeedRange.x, talkSpeedRange.y);
        Vector3 targetRot = new Vector3(-mouthMaxOpen, originalMouthRot.y, originalMouthRot.z);
        mouthTween = mouthBone.DOLocalRotate(targetRot, randomDuration).SetId("NPCPuppets").SetEase(Ease.InOutSine).OnComplete(() => {
            float returnDuration = Random.Range(talkSpeedRange.x, talkSpeedRange.y);
            mouthTween = mouthBone.DOLocalRotate(originalMouthRot, returnDuration).SetId("NPCPuppets").SetEase(Ease.InOutSine).OnComplete(RandomizedMouthLoop);
        });
    }

    private void LeaveSeat()
    {
        isTalking = false;
        mouthTween?.Kill();
        mouthBone.DOLocalRotate(originalMouthRot, 0.2f).SetId("NPCPuppets");
        if (activeProp != null) activeProp.SetActive(false);
        SeatedClientManager.Instance.ReleaseSeat(assignedSeat);
        isWalking = true;
        StartRandomHop();
        StartCoroutine(ExitRoutine());
    }

    private IEnumerator ExitRoutine()
    {
        if (SeatedClientManager.Instance != null && SeatedClientManager.Instance.doorWaitPoint != null)
        {
            FaceTargetWithBackwardsAxis(SeatedClientManager.Instance.doorWaitPoint.position);
            yield return MoveTo(SeatedClientManager.Instance.doorWaitPoint.position);
            SeatedClientManager.Instance.OpenDoors();
        }

        for (int i = currentPath.Length - 1; i >= 0; i--)
        {
            FaceTargetWithBackwardsAxis(currentPath[i].position);
            yield return MoveTo(currentPath[i].position);
        }

        transform.DOKill();
        Destroy(gameObject);
    }
}