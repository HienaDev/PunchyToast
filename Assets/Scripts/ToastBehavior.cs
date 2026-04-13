using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class ToastBehavior : MonoBehaviour
{
    [HideInInspector] public float hoverDuration, bobAmount, bobSpeed, preHoverDelay;
    [HideInInspector] public List<Transform> potentialTargets;
    [HideInInspector] public GameObject armPrefab;

    [Header("Momentum Settings")]
    [Range(0, 1)] public float driftFactor = 0.2f;
    [Range(0, 1)] public float exitMomentumScale = 0.8f;

    [Header("Punch Settings")]
    public bool debugAlwaysL = true;
    public float armSpawnOffset = 4f;
    public float armPunchDuration = 0.15f;
    public float targetFlightForce = 25f;

    private Rigidbody rb;
    private bool isRising = false;
    private bool hasLeftToaster = false;
    private bool isHovering = false;
    private bool hasBeenHit = false;

    private char assignedLetter;
    private KeyCode assignedKey;
    private float capturedXVel, capturedZVel;
    private Tween bobTween;
    private Coroutine hoverRoutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        assignedLetter = (char)Random.Range(65, 91);
        assignedKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), assignedLetter.ToString());
    }

    void Update()
    {
        if (!isHovering || hasBeenHit) return;

        if (Input.GetKeyDown(assignedKey) || (debugAlwaysL && Input.GetKeyDown(KeyCode.L)))
        {
            StartPunchSequence();
        }
    }

    void FixedUpdate()
    {
        if (hasBeenHit) return;

        if (!hasLeftToaster)
        {
            if (rb.linearVelocity.y > 0.1f)
            {
                hasLeftToaster = true;
                isRising = true;
            }
            return;
        }

        if (isRising && rb.linearVelocity.y <= 0.05f)
        {
            isRising = false;
            hoverRoutine = StartCoroutine(HoverRoutine());
        }
    }

    IEnumerator HoverRoutine()
    {
        yield return new WaitForSeconds(preHoverDelay);

        capturedXVel = rb.linearVelocity.x;
        capturedZVel = rb.linearVelocity.z;

        isHovering = true;
        rb.useGravity = false;

        bobTween = transform.DOMoveY(bobAmount, bobSpeed)
            .SetRelative()
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        float timer = 0;
        while (timer < hoverDuration)
        {
            timer += Time.fixedDeltaTime;
            rb.linearVelocity = new Vector3(capturedXVel * driftFactor, 0, capturedZVel * driftFactor);
            yield return new WaitForFixedUpdate();
        }

        if (!hasBeenHit) ReleaseToast();
    }

    void StartPunchSequence()
    {
        hasBeenHit = true;

        if (bobTween != null) bobTween.Kill();
        if (hoverRoutine != null) StopCoroutine(hoverRoutine);

        Transform target = potentialTargets[Random.Range(0, potentialTargets.Count)];
        Vector3 dirToTarget = (target.position - transform.position).normalized;

        // --- ARM ROTATION LOGIC ---
        // 1. Find the base rotation to look at the target
        Quaternion baseLook = Quaternion.LookRotation(dirToTarget);

        // 2. Add the 90 degree X offset so the Capsule tip points at the toast
        Quaternion capsuleOffset = Quaternion.Euler(90, 0, 0);
        Quaternion finalRotation = baseLook * capsuleOffset;

        Vector3 armSpawnPos = transform.position - (dirToTarget * armSpawnOffset);
        GameObject arm = Instantiate(armPrefab, armSpawnPos, finalRotation);

        arm.transform.DOMove(transform.position, armPunchDuration)
            .SetEase(Ease.InExpo)
            .OnComplete(() => {
                Destroy(arm);
                LaunchAtTarget(dirToTarget);
            });
    }

    void LaunchAtTarget(Vector3 direction)
    {
        isHovering = false;
        rb.useGravity = true;
        rb.linearVelocity = Vector3.zero;

        rb.AddForce(direction * targetFlightForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.Impulse);
    }

    void ReleaseToast()
    {
        isHovering = false;
        if (bobTween != null) bobTween.Kill();
        rb.useGravity = true;
        rb.linearVelocity = new Vector3(capturedXVel * exitMomentumScale, -0.1f, capturedZVel * exitMomentumScale);
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}