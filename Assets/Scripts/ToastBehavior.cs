using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;

public class ToastBehavior : MonoBehaviour
{
    // Settings Passed from Toaster
    [HideInInspector] public float hoverDuration, bobAmount, bobSpeed, preHoverDelay, driftFactor;
    [HideInInspector] public List<Transform> potentialTargets;
    [HideInInspector] public GameObject armPrefab;
    [HideInInspector] public bool debugAlwaysL;
    [HideInInspector] public float armSpawnOffset, armPunchDuration, targetFlightForce;

    [Header("Momentum Settings")]
    [Range(0, 1)] public float exitMomentumScale = 0.8f;

    [Header("Visual Juice")]
    public float armShrinkDuration = 0.1f;
    public float flightDuration = 0.5f;
    public float fallGracePeriod = 0.2f; // Time allowed to punch after falling starts

    // Internal State
    private Rigidbody rb;
    private bool isRising = false, hasLeftToaster = false, isHovering = false, hasBeenHit = false;
    private bool isPunchable = true; // Starts true so you can punch it immediately
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

    private void Start()
    {
        TextMeshProUGUI letterText = GetComponentInChildren<TextMeshProUGUI>();
        if (letterText != null)
        {
            letterText.text = assignedLetter.ToString();
        }
    }

    void Update()
    {
        // Now checks the state variable instead of just isHovering
        if (!isPunchable || hasBeenHit) return;

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
        isPunchable = false; // Stop further inputs

        

        Transform target = potentialTargets[Random.Range(0, potentialTargets.Count)];
        Vector3 dirToTarget = (target.position - transform.position).normalized;

        Quaternion baseLook = Quaternion.LookRotation(dirToTarget);

        Vector3 armSpawnPos = transform.position - (dirToTarget * armSpawnOffset);
        GameObject arm = Instantiate(armPrefab, armSpawnPos, baseLook);

        // FLIP LOGIC: Check screen position to flip the arm if it's on the right
        float screenX = Camera.main.WorldToViewportPoint(arm.transform.position).x;
        if (screenX < 0f)
        {
            arm.transform.localScale = Vector3.Scale(arm.transform.localScale, new Vector3(-1, 1, 1));
        }

        arm.transform.DOMove(transform.position, armPunchDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => {
                if (bobTween != null) bobTween.Kill();
                if (hoverRoutine != null) StopCoroutine(hoverRoutine);
                LaunchAtTarget(target);

                arm.transform.DOScale(Vector3.zero, armShrinkDuration)
                    .SetEase(Ease.InQuad)
                    .OnComplete(() => Destroy(arm));
            });
    }

    void LaunchAtTarget(Transform target)
    {
        isHovering = false;
        rb.isKinematic = true;

        transform.DOMove(target.position, flightDuration).SetEase(Ease.Linear);

        transform.DORotate(new Vector3(Random.Range(360, 720), Random.Range(360, 720), Random.Range(360, 720)), flightDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => {
                Debug.Log("Target Hit!");
                rb.isKinematic = false;
                rb.useGravity = true;
            });
    }

    void ReleaseToast()
    {
        isHovering = false;
        if (bobTween != null) bobTween.Kill();

        rb.useGravity = true;
        rb.linearVelocity = new Vector3(capturedXVel * exitMomentumScale, -0.1f, capturedZVel * exitMomentumScale);

        // Start the grace period timer before disabling input
        StartCoroutine(GracePeriodTimer());
    }

    IEnumerator GracePeriodTimer()
    {
        yield return new WaitForSeconds(fallGracePeriod);
        if (!hasBeenHit)
        {
            isPunchable = false;
        }
    }

    private void OnDestroy() => transform.DOKill();
}