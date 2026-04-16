using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;

public class ToastBehavior : MonoBehaviour
{
    // ... (Keep all your existing Hidden Settings) ...
    public float hoverDuration, bobAmount, bobSpeed, preHoverDelay, driftFactor;
    [HideInInspector] public List<Transform> potentialTargets;
    [HideInInspector] public GameObject armPrefab;
    [HideInInspector] public bool debugAlwaysL;
    [HideInInspector] public float armSpawnOffset, armPunchDuration, targetFlightForce;

    [Header("Momentum Settings")]
    [Range(0, 1)] public float exitMomentumScale = 0.8f;

    [Header("Visual Juice")]
    public float armShrinkDuration = 0.8f;
    public float flightDuration = 0.5f;
    public float fallGracePeriod = 0.2f;

    [Header("Impact Settings")]
    public float impactFreezeTime = 0.05f;
    public float shakeIntensity = 0.01f;
    public int shakeVibrato = 30;
    public float pushForce = 0.1f;         // How much the toast is "pushed" into the punch
    public float armRecoilDistance = 2f;  // How far the arm snaps back on hit

    // Internal State
    private Rigidbody rb;
    private bool isRising = false, hasLeftToaster = false, isHovering = false, hasBeenHit = false;
    public bool isPunchable = true;
    private char assignedLetter;
    private KeyCode assignedKey;
    private float capturedXVel, capturedZVel;
    private Tween bobTween;
    private Coroutine hoverRoutine;
    private float timeToDisappear = 4f;
    private TextMeshProUGUI letterText;

    private Client currentFlightTargetClient;

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
        foreach (TrailRenderer lr in lineRenderers)
        {
            lr.enabled = false;
        }
        yield return new WaitForSeconds(preHoverDelay);
        capturedXVel = rb.linearVelocity.x;
        capturedZVel = rb.linearVelocity.z;
        isHovering = true;
        rb.useGravity = false;

        bobTween = transform.DOMoveY(bobAmount, bobSpeed)
            .SetRelative().SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);

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


        string currentJam = JamDecider.Instance.GetCurrentJamName();
        Transform targetTransform = ClientManager.Instance.GetBestTarget(currentJam);

        Debug.Log(targetTransform != null ? $"Punching towards {targetTransform.name}" : "No target found, punching into the void!");
        // Store the client script specifically if the target has one
        currentFlightTargetClient = targetTransform.GetComponent<Client>();



        if (currentFlightTargetClient != null)
        {
            ClientManager.Instance.IncreaseLetterIndex();
            currentFlightTargetClient.Satisfy();
            targetTransform = currentFlightTargetClient.TargetForToast; // Aim for the mouth pivot, not the whole client
        }

        isPunchable = false;
        

        Vector3 dirToTarget = (targetTransform.position - transform.position).normalized;
        Quaternion baseLook = Quaternion.LookRotation(dirToTarget);

        Vector3 armSpawnPos = transform.position - (dirToTarget * armSpawnOffset);
        GameObject arm = Instantiate(armPrefab, armSpawnPos, baseLook);

        float screenX = Camera.main.WorldToViewportPoint(transform.position).x;
        if (screenX > 0.5f)
        {
            arm.transform.localScale = Vector3.Scale(arm.transform.localScale, new Vector3(-1, 1, 1));
        }

        arm.transform.DOMove(transform.position, armPunchDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => {

                StartCoroutine(ImpactSequence(arm, targetTransform, dirToTarget));
                if (!ClientManager.Instance.IsLastClientOfWave(currentFlightTargetClient))
                    Toaster.Instance.LaunchToast(); // here

            });
    }

    IEnumerator ImpactSequence(GameObject arm, Transform target, Vector3 dirToTarget)
    {
        // 1. STOP EVERYTHING & PREP
        if (bobTween != null) bobTween.Kill();
        if (hoverRoutine != null) StopCoroutine(hoverRoutine);

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        // 2. THE SHOVE (Moving both arm and toast together)
        ApplyJamSplat();

        // Start the move for both
        arm.transform.DOMove(arm.transform.position + (dirToTarget * pushForce), impactFreezeTime).SetEase(Ease.Linear);
        transform.DOMove(transform.position + (dirToTarget * pushForce), impactFreezeTime).SetEase(Ease.Linear);

        //Time.timeScale = 0.01f; // Slow down time for impact feel
        //yield return new WaitForSeconds(0.0005f); // Short pause before the shake
        //Time.timeScale = 1f;

        // Wait for the shove to finish
        yield return new WaitForSeconds(impactFreezeTime);

        if (CameraShake.Instance != null)
        {
            // Try 0.1s duration, 0.05 strength for a very tight "click" feel
            CameraShake.Instance.Shake(0.1f, 0.05f, 30);
        }

        // 3. THE SHAKE (Triggered only after they've reached the end of the shove)
        // Shaking both for maximum impact feel
        transform.DOShakePosition(0.05f, shakeIntensity, shakeVibrato);
        arm.transform.DOShakePosition(0.05f, shakeIntensity / 2f, shakeVibrato);



        // Wait for the shake to finish (the "linger")
        yield return new WaitForSeconds(impactFreezeTime);



        // 4. BREAK AWAY
        LaunchAtTarget(target);

        // Arm recoil
        Vector3 recoilPos = arm.transform.position - (dirToTarget * armRecoilDistance);

        Sequence armSeq = DOTween.Sequence();
        armSeq.Append(arm.transform.DOMove(recoilPos, armShrinkDuration * 0.8f).SetEase(Ease.OutBack));
        armSeq.Append(arm.transform.DOScale(Vector3.zero, armShrinkDuration).SetEase(Ease.InQuad));
        armSeq.OnComplete(() => Destroy(arm));
    }

    void ApplyJamSplat()
    {
        if (JamDecider.Instance == null) return;
        TAG_Splat splat = GetComponentInChildren<TAG_Splat>();
        Renderer splatRenderer = splat?.GetComponent<Renderer>();
        if (splatRenderer != null)
        {
            splatRenderer.enabled = true;
            splatRenderer.material.SetColor("_BaseColor", JamDecider.Instance.GetCurrentJamColor());
        }
    }

    void LaunchAtTarget(Transform target)
    {
        isHovering = false;
        rb.isKinematic = true;

        TrailRenderer[] lineRenderers = GetComponentsInChildren<TrailRenderer>();
        foreach (TrailRenderer lr in lineRenderers)
        {
            lr.enabled = true;
        }

        TAG_JamDroplets[] jamDroplets = GetComponentsInChildren<TAG_JamDroplets>(true);
        foreach (TAG_JamDroplets droplet in jamDroplets)
        {
            droplet.gameObject.SetActive(true);
            var main = droplet.GetComponent<ParticleSystem>().main;
            main.startColor = JamDecider.Instance.GetCurrentJamColor();
        }

        transform.DOLookAt(target.position, flightDuration / 4).SetEase(Ease.Linear);

        if(currentFlightTargetClient != null)
        currentFlightTargetClient.OpenMouth();

        // 1. Create a Sequence
        Sequence flightSeq = DOTween.Sequence();

        // 2. Add the Move to the sequence
        flightSeq.Append(transform.DOMove(target.position, flightDuration).SetEase(Ease.Linear));

        // 3. Insert the Recoil callback halfway through the duration
        flightSeq.InsertCallback(flightDuration / 2f, () => {
            if (currentFlightTargetClient != null)
            {
                currentFlightTargetClient.Recoil();
                Debug.Log("Recoil Triggered Halfway!");
            }
        });

        // 4. Use the OnComplete on the sequence for the final impact
        flightSeq.OnComplete(() => {
            Debug.Log("Target Hit!");
            rb.isKinematic = false;
            rb.useGravity = true;

            if (currentFlightTargetClient != null)
            {
                // Note: I removed Recoil() from here since it's now happening halfway
                string myJam = JamDecider.Instance.allAvailableJams[JamDecider.Instance.currentJamIndex].flavor.ToString();
                currentFlightTargetClient.TryEatToast(myJam, gameObject);
            }
        });
    }

    void ReleaseToast()
    {
        isHovering = false;
        if (bobTween != null) bobTween.Kill();
        rb.useGravity = true;
        rb.linearVelocity = new Vector3(capturedXVel * exitMomentumScale, -0.1f, capturedZVel * exitMomentumScale);
        rb.constraints = RigidbodyConstraints.None;
        StartCoroutine(GracePeriodTimer());
        //if (timeToDisappear > 0) Destroy(gameObject, timeToDisappear);
    }

    IEnumerator GracePeriodTimer()
    {
        yield return new WaitForSeconds(fallGracePeriod);
        if (!hasBeenHit)
        {
            if (letterText != null) letterText.text = "";
            isPunchable = false;
        }
    }

    private void OnDestroy() => transform.DOKill();
}