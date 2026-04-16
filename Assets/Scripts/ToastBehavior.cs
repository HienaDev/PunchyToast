using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;

public class ToastBehavior : MonoBehaviour
{
    public float hoverDuration, bobAmount, bobSpeed, preHoverDelay, driftFactor;
    [HideInInspector] public List<Transform> potentialTargets;
    [HideInInspector] public GameObject armPrefab;
    [HideInInspector] public bool debugAlwaysL;
    [HideInInspector] public float armSpawnOffset, armPunchDuration, targetFlightForce;

    public float exitMomentumScale = 0.8f;
    public float armShrinkDuration = 0.8f;
    public float flightDuration = 0.5f;
    public float fallGracePeriod = 0.2f;

    public float impactFreezeTime = 0.05f;
    public float shakeIntensity = 0.01f;
    public int shakeVibrato = 30;
    public float pushForce = 0.1f;
    public float armRecoilDistance = 2f;

    private Rigidbody rb;
    private bool isRising = false, hasLeftToaster = false, isHovering = false, hasBeenHit = false;
    public bool isPunchable = true;
    public char assignedLetter;
    private KeyCode assignedKey;
    private float capturedXVel, capturedZVel;
    private Tween bobTween;
    private Coroutine hoverRoutine;
    private TextMeshProUGUI letterText;
    private Client currentFlightTargetClient;

    public bool isSimultaneous = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        assignedLetter = ClientManager.Instance.GetCurrentLetter();
        assignedKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), assignedLetter.ToString());
    }

    private void Start()
    {
        letterText = GetComponentInChildren<TextMeshProUGUI>();
        if (letterText != null) { letterText.text = assignedLetter.ToString(); }
    }

    void Update()
    {
        if (!isPunchable || hasBeenHit) return;
        if (Input.GetKeyDown(assignedKey) || (debugAlwaysL && Input.GetKeyDown(KeyCode.L))) StartPunchSequence();
    }

    void FixedUpdate()
    {
        if (hasBeenHit) return;
        if (!hasLeftToaster)
        {
            if (rb.linearVelocity.y > 0.1f) { hasLeftToaster = true; isRising = true; }
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
        TrailRenderer[] lineRenderers = GetComponentsInChildren<TrailRenderer>();
        foreach (TrailRenderer lr in lineRenderers) lr.enabled = false;
        yield return new WaitForSeconds(preHoverDelay);

        capturedXVel = rb.linearVelocity.x;
        capturedZVel = rb.linearVelocity.z;
        isHovering = true;
        rb.useGravity = false;

        // ID individual tween to kill it later
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
        isPunchable = false;

        // Safety: Kill the bobbing immediately
        if (bobTween != null) bobTween.Kill();

        string currentJam = JamDecider.Instance.GetCurrentJamName();
        Transform targetTransform = ClientManager.Instance.GetBestTarget(currentJam);
        currentFlightTargetClient = targetTransform.GetComponent<Client>();

        if (currentFlightTargetClient != null)
        {
            ClientManager.Instance.IncreaseLetterIndex();
            currentFlightTargetClient.Satisfy();
            targetTransform = currentFlightTargetClient.TargetForToast;
        }

        Vector3 dirToTarget = (targetTransform.position - transform.position).normalized;
        GameObject arm = Instantiate(armPrefab, transform.position - (dirToTarget * armSpawnOffset), Quaternion.LookRotation(dirToTarget));

        if (Camera.main.WorldToViewportPoint(transform.position).x > 0.5f)
            arm.transform.localScale = Vector3.Scale(arm.transform.localScale, new Vector3(-1, 1, 1));

        arm.transform.DOMove(transform.position, armPunchDuration).SetEase(Ease.Linear).OnComplete(() => {
            // Check if arm still exists (safety)
            if (arm != null) StartCoroutine(ImpactSequence(arm, targetTransform, dirToTarget));

            if (!ClientManager.Instance.IsLastClientOfWave(currentFlightTargetClient))
            {
                Toaster.Instance.LaunchToast();
            }
        });
    }

    IEnumerator ImpactSequence(GameObject arm, Transform target, Vector3 dirToTarget)
    {
        if (hoverRoutine != null) StopCoroutine(hoverRoutine);
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
        ApplyJamSplat();

        arm.transform.DOMove(arm.transform.position + (dirToTarget * pushForce), impactFreezeTime).SetEase(Ease.Linear);
        transform.DOMove(transform.position + (dirToTarget * pushForce), impactFreezeTime).SetEase(Ease.Linear);
        yield return new WaitForSeconds(impactFreezeTime);

        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.1f, 0.05f, 30);
        transform.DOShakePosition(0.05f, shakeIntensity, shakeVibrato);
        yield return new WaitForSeconds(impactFreezeTime);

        LaunchAtTarget(target);

        Vector3 recoilPos = arm.transform.position - (dirToTarget * armRecoilDistance);
        Sequence armSeq = DOTween.Sequence();
        armSeq.Append(arm.transform.DOMove(recoilPos, armShrinkDuration * 0.8f).SetEase(Ease.OutBack));
        armSeq.Append(arm.transform.DOScale(Vector3.zero, armShrinkDuration).SetEase(Ease.InQuad));
        armSeq.OnComplete(() => { if (arm != null) Destroy(arm); });
    }

    void ApplyJamSplat()
    {
        TAG_Splat splat = GetComponentInChildren<TAG_Splat>();
        Renderer splatRenderer = splat?.GetComponent<Renderer>();
        if (splatRenderer != null) { splatRenderer.enabled = true; splatRenderer.material.SetColor("_BaseColor", JamDecider.Instance.GetCurrentJamColor()); }
    }

    void LaunchAtTarget(Transform target)
    {
        isHovering = false;
        rb.isKinematic = true;
        foreach (TrailRenderer lr in GetComponentsInChildren<TrailRenderer>()) lr.enabled = true;
        foreach (TAG_JamDroplets droplet in GetComponentsInChildren<TAG_JamDroplets>(true))
        {
            droplet.gameObject.SetActive(true);
            var main = droplet.GetComponent<ParticleSystem>().main;
            main.startColor = JamDecider.Instance.GetCurrentJamColor();
        }

        // Only look if target exists
        if (target != null) transform.DOLookAt(target.position, flightDuration / 4).SetEase(Ease.Linear);
        if (currentFlightTargetClient != null) currentFlightTargetClient.OpenMouth();

        Sequence flightSeq = DOTween.Sequence();
        flightSeq.Append(transform.DOMove(target.position, flightDuration).SetEase(Ease.Linear));
        flightSeq.InsertCallback(flightDuration / 2f, () => { if (currentFlightTargetClient != null) currentFlightTargetClient.Recoil(); });
        flightSeq.OnComplete(() => {
            if (this == null) return;
            rb.isKinematic = false;
            rb.useGravity = true;
            if (currentFlightTargetClient != null) currentFlightTargetClient.TryEatToast(JamDecider.Instance.allAvailableJams[JamDecider.Instance.currentJamIndex].flavor.ToString(), gameObject);
        });
    }

    void ReleaseToast()
    {
        isHovering = false;
        if (bobTween != null) bobTween.Kill();
        rb.useGravity = true;
        rb.linearVelocity = new Vector3(capturedXVel * exitMomentumScale, -0.1f, capturedZVel * exitMomentumScale);
        StartCoroutine(GracePeriodTimer());
    }

    IEnumerator GracePeriodTimer()
    {
        yield return new WaitForSeconds(fallGracePeriod);
        if (!hasBeenHit) isPunchable = false;
    }

    private void OnDestroy()
    {
        // CRITICAL: Stop all tweens on this object to prevent DOTween errors
        transform.DOKill();
        if (Toaster.Instance != null) Toaster.Instance.UnregisterToast(this);
    }
}