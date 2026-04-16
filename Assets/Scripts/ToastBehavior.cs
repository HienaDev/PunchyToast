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

    public int myLetterIndex = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        assignedLetter = ClientManager.Instance.GetCurrentLetter();

        int assignedIndex = ClientManager.Instance.GetAvailableIndex();

        if(assignedIndex != -1)
        {
            myLetterIndex = assignedIndex;
            assignedLetter = ClientManager.Instance.GetCurrentWord()[assignedIndex];
        }
        else assignedLetter = (char)('A' + Random.Range(0, 26));

        assignedLetter = char.ToUpper(assignedLetter);
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
        bobTween = transform.DOMoveY(bobAmount, bobSpeed).SetRelative().SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);

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
        string currentJam = JamDecider.Instance.GetCurrentJamName();
        Transform targetTransform = ClientManager.Instance.GetBestTarget(currentJam);
        currentFlightTargetClient = targetTransform.GetComponent<Client>();

        if (currentFlightTargetClient != null)
        {
            ClientManager.Instance.IncreaseLetterIndex();
            currentFlightTargetClient.Satisfy();
            if (currentFlightTargetClient.isSatisfied)
                ClientManager.Instance.RemoveIndex(myLetterIndex);
            
            targetTransform = currentFlightTargetClient.TargetForToast;
        }

        Vector3 dirToTarget = (targetTransform.position - transform.position).normalized;
        GameObject arm = Instantiate(armPrefab, transform.position - (dirToTarget * armSpawnOffset), Quaternion.LookRotation(dirToTarget));
        if (Camera.main.WorldToViewportPoint(transform.position).x > 0.5f)
            arm.transform.localScale = Vector3.Scale(arm.transform.localScale, new Vector3(-1, 1, 1));

        arm.transform.DOMove(transform.position, armPunchDuration).SetEase(Ease.Linear).OnComplete(() => {
            StartCoroutine(ImpactSequence(arm, targetTransform, dirToTarget));

            // Normal launch only happens if we AREN'T currently in a simultaneous burst
            // or if the air is clear.
            if (!ClientManager.Instance.IsLastClientOfWave(currentFlightTargetClient) && !Toaster.Instance.AreTherePunchableToasts())
            {
                // The Toaster's Update loop or this call will handle normal flow
                Toaster.Instance.LaunchToast();
            }
        });
    }

    IEnumerator ImpactSequence(GameObject arm, Transform target, Vector3 dirToTarget)
    {
        if (bobTween != null) bobTween.Kill();
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
        armSeq.OnComplete(() => Destroy(arm));
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
        foreach (TrailRenderer lr in GetComponentsInChildren<TrailRenderer>()) lr.enabled = true;
        foreach (TAG_JamDroplets droplet in GetComponentsInChildren<TAG_JamDroplets>(true))
        {
            droplet.gameObject.SetActive(true);
            var main = droplet.GetComponent<ParticleSystem>().main;
            main.startColor = JamDecider.Instance.GetCurrentJamColor();
        }

        // Check if we actually have a valid client target
        // If currentFlightTargetClient is null, it means we are just aiming at a seating position
        if (currentFlightTargetClient != null)
        {
            rb.isKinematic = true;
            transform.DOLookAt(target.position, flightDuration / 4).SetEase(Ease.Linear);
            currentFlightTargetClient.OpenMouth();

            gameObject.GetComponent<Collider>().enabled = false; // Disable collider to prevent mid-flight collisions

            Sequence flightSeq = DOTween.Sequence();
            flightSeq.Append(transform.DOMove(target.position, flightDuration).SetEase(Ease.Linear));
            flightSeq.InsertCallback(flightDuration / 2f, () => { if (currentFlightTargetClient != null) currentFlightTargetClient.Recoil(); });
            flightSeq.OnComplete(() => {
                rb.isKinematic = false;
                rb.useGravity = true;
                currentFlightTargetClient.TryEatToast(JamDecider.Instance.allAvailableJams[JamDecider.Instance.currentJamIndex].flavor.ToString(), gameObject);
            });
        }
        else
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;

            // Use the new Manager method to find the puppet at this seat
            Client clientToBonk = ClientManager.Instance.GetClientInSeat(target);
            if (clientToBonk != null)
            {
                // Delay the recoil so it happens when the toast actually arrives
                DOVirtual.DelayedCall(flightDuration / 2f, () => {
                    if (clientToBonk != null) clientToBonk.HardRecoil();
                });
            }

            Vector3 throwDir = (target.position - transform.position).normalized;
            throwDir.y += 0.2f; // Give it that nice "oops" arc

            rb.AddForce(throwDir * targetFlightForce, ForceMode.Impulse);
            rb.AddTorque(new Vector3(Random.value, Random.value, Random.value) * 10f, ForceMode.Impulse);

            // change layer to default layer so it can interact with the environment properly on its way down
            gameObject.layer = LayerMask.NameToLayer("Default");

            if (letterText != null) letterText.enabled = false;
            isPunchable = false;
        }
    }

    private void OnMouseDown()
    {
        if (!isPunchable || hasBeenHit) return;
        StartPunchSequence();
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
        if (!hasBeenHit)
        {
            isPunchable = false;
            rb.constraints = RigidbodyConstraints.None;
            letterText.enabled = false;
        }
    }

    private void OnDestroy() { if (Toaster.Instance != null) Toaster.Instance.UnregisterToast(this); transform.DOKill(); }
}