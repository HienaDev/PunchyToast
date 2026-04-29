using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.WSA;

public class ToastBehavior : MonoBehaviour
{
    public float hoverDuration, bobAmount, bobSpeed, preHoverDelay, driftFactor;
    [HideInInspector] public List<Transform> potentialTargets;
    [HideInInspector] public GameObject armPrefab;
    [HideInInspector] public bool debugAlwaysL;
    [HideInInspector] public float armSpawnOffset, armPunchDuration, targetFlightForce;

    [Header("Slap Settings")]
    public int slapsLeft = 0;           // Set this in the inspector for "tough" toasts
    public float slapSpinDuration = 0.4f;
    string slapString = "";
    private bool isSlappable = false;

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

    public AudioMixer sfxMixer;
    public AudioClip[] punchSounds;
    public AudioClip[] toastGettingIntoMouth;
    public AudioClip[] toastFlying;
    public AudioClip[] toastLandingNaturally;

    public GameObject punchEffect;

    private FireController fireController;

    private string jamOnThisToast = "";

    private bool isWrongToast = false;

    private void OnCollisionEnter(Collision collision)
    {
        AudioManager.Instance.PlaySound(toastLandingNaturally, sfxMixer, transform.position);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        int assignedIndex = ClientManager.Instance.GetAvailableIndex();

        fireController = GetComponent<FireController>();

        if (assignedIndex != -1)
        {
            myLetterIndex = assignedIndex;
            assignedLetter = ClientManager.Instance.GetCurrentWord()[assignedIndex];
        }
        else assignedLetter = (char)('A' + Random.Range(0, 26));

        Debug.Log($"Toast assigned letter {assignedLetter} at index {myLetterIndex}");

        slapsLeft = 0;

        // check if assigned letter is a number
        if (char.IsDigit(assignedLetter))
        {
            Debug.Log("Digit found, current digit is: " + assignedLetter);
            isSlappable = true;
            slapString = ClientManager.Instance.GetSlapWordForIndex(assignedLetter - '0');
            Debug.Log("Slap string for this toast is: " + slapString);
            slapsLeft = slapString.Length;

            SetCurrentLetter(slapString[slapString.Length - slapsLeft]);
        }
        else
        {
            //int assignedIndex = ClientManager.Instance.GetAvailableIndex();

            if (assignedIndex != -1)
            {
                myLetterIndex = assignedIndex;
                assignedLetter = ClientManager.Instance.GetCurrentWord()[assignedIndex];
            }
            else assignedLetter = (char)('A' + Random.Range(0, 26));

            assignedLetter = char.ToUpper(assignedLetter);
            assignedKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), assignedLetter.ToString());
        }
    }



    private void SetCurrentLetter(char c)
    {
        SetFire();
        assignedLetter = c;
        assignedKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), assignedLetter.ToString());
        if (letterText != null) letterText.text = assignedLetter.ToString();
    }

    private void SetFire()
    {
        fireController.Disable();

        if (Toaster.Instance.currentCombo > 0)
        {
            if (Toaster.Instance.currentCombo >= 7) fireController.fire3.SetActive(true);
            else if (Toaster.Instance.currentCombo >= 3) fireController.fire2.SetActive(true);
            else fireController.fire1.SetActive(true);

        }
    }

    private void Start()
    {
        SetFire();
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
        slapsLeft--;

        string currentJam = JamDecider.Instance.GetCurrentJamName();
        Transform targetTransform = ClientManager.Instance.GetBestTarget(currentJam);

        jamOnThisToast = JamDecider.Instance.GetCurrentJamName();

        currentFlightTargetClient = targetTransform.GetComponent<Client>();

        if (currentFlightTargetClient != null && slapsLeft <= 0)
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

        arm.transform.DOMove(transform.position, armPunchDuration).SetEase(Ease.Linear).OnComplete(() =>
        {

            bool isLastHit = ClientManager.Instance.IsLastToastOfLevel() && slapsLeft <= 1;
            if (isLastHit)
            {
                Time.timeScale = 0.05f; // Slow down time as punch connects
                if (Toaster.Instance.cinematicCamera != null)
                {
                    Toaster.Instance.cinematicCamera.gameObject.SetActive(true);
                    Toaster.Instance.defaultCamera.gameObject.SetActive(false);
                }
            }

            StartCoroutine(ImpactSequence(arm, targetTransform, dirToTarget));

            AudioManager.Instance.PlaySound(punchSounds, sfxMixer, transform.position, volume: 0.4f);

            float pitch = Toaster.Instance.GetComboPitch();
            if (Toaster.Instance.currentCombo >= 1)
            {

                AudioManager.Instance.PlaySoundFixedPitch(Toaster.Instance.toastComboSound, pitch, sfxMixer, transform.position, volume: 2.0f);



            }

            Toaster.Instance.IncrementCombo();

            // ONLY launch a new toast if this hit is going to finish the current toast
            if (slapsLeft <= 0)
            {
                isPunchable = false;
                if (!ClientManager.Instance.IsLastClientOfWave() && !Toaster.Instance.AreTherePunchableToasts())
                {
                    Debug.Log("Launching new toast from punch because no punchable toasts remain!");
                    if(currentFlightTargetClient != null)
                        Toaster.Instance.LaunchToast();
                    else
                    {
                        //do wrong toast sequence
                        isWrongToast = true;

                    }
                }
            }
        });
    }

    IEnumerator ImpactSequence(GameObject arm, Transform target, Vector3 dirToTarget)
    {
        if (bobTween != null) bobTween.Kill();
        if (hoverRoutine != null) StopCoroutine(hoverRoutine);
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;

        // 1. Instantiate punch effect
        GameObject punchEffectInstance = Instantiate(punchEffect, transform.position, Quaternion.identity);

        ApplyJamSplat();

        // Calculate the common destination offset
        Vector3 moveOffset = dirToTarget * pushForce;

        // 2. Fire all three Tweens at once. 
        // Using the same duration (impactFreezeTime) and Ease (Linear) keeps them locked together.
        arm.transform.DOMove(arm.transform.position + moveOffset, impactFreezeTime).SetEase(Ease.Linear);
        transform.DOMove(transform.position + moveOffset, impactFreezeTime).SetEase(Ease.Linear);

        // The effect now moves alongside the toast without being a child
        if (punchEffectInstance != null)
        {
            punchEffectInstance.transform.DOMove(punchEffectInstance.transform.position + moveOffset, impactFreezeTime).SetEase(Ease.Linear);
        }

        yield return new WaitForSeconds(impactFreezeTime);

        // After this point, the punchEffectInstance has finished its move and stays in place 
        // while the toast continues with the shake and the rest of the logic.

        if (CameraShake.Instance != null) CameraShake.Instance.Shake(0.2f, 0.1f, 30);
        transform.DOShakePosition(0.05f, shakeIntensity, shakeVibrato);

        yield return new WaitForSeconds(impactFreezeTime);



        if (slapsLeft > 0 && isSlappable)
        {



            SetCurrentLetter(slapString[slapString.Length - slapsLeft]);

            // 1. Invert drift and Reset Physics
            driftFactor *= -1;
            rb.isKinematic = false;
            rb.useGravity = false;

            // 2. Clear previous rotation and Spin (720 degrees)
            transform.DORotate(new Vector3(0, 720, 0), slapSpinDuration, RotateMode.LocalAxisAdd).SetEase(Ease.OutBack);

            // 3. Recoil the arm so it disappears
            Vector3 armRecoilPos = arm.transform.position - (dirToTarget * armRecoilDistance);
            arm.transform.DOMove(armRecoilPos, armShrinkDuration).SetEase(Ease.OutBack).OnComplete(() => Destroy(arm));

            //yield return new WaitForSeconds(slapSpinDuration * 0.25f);

            // 4. Reset state to allow hitting again
            hasBeenHit = false;
            isPunchable = true;

            // Restart the hover behavior
            hoverRoutine = StartCoroutine(HoverRoutine());
        }
        else
        {
            // FINAL HIT: Normal Launch behavior
            LaunchAtTarget(target);

            Vector3 recoilPos = arm.transform.position - (dirToTarget * armRecoilDistance);
            Sequence armSeq = DOTween.Sequence();
            armSeq.Append(arm.transform.DOMove(recoilPos, armShrinkDuration * 0.8f).SetEase(Ease.OutBack));
            armSeq.Append(arm.transform.DOScale(Vector3.zero, armShrinkDuration).SetEase(Ease.InQuad));
            armSeq.OnComplete(() =>
            {
                arm.transform.DOKill();
                Destroy(arm);
            });
            
        }
    }

    void ApplyJamSplat()
    {
        TAG_Splat splat = GetComponentInChildren<TAG_Splat>();
        Renderer splatRenderer = splat?.GetComponent<Renderer>();
        if (splatRenderer != null) { splatRenderer.enabled = true; splatRenderer.material.SetColor("_BaseColor", JamDecider.Instance.GetCurrentJamColor()); }
    }

    void LaunchAtTarget(Transform target)
    {
        
        bool isCinematic = Time.timeScale < 1f;

        isHovering = false;

        AudioManager.Instance.PlaySound(toastFlying, sfxMixer, transform.position);

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

            //float actualFlightDuration = isCinematic ? flightDuration * 2f : flightDuration;

            transform.DOLookAt(target.position, flightDuration / 4).SetEase(Ease.Linear);
            currentFlightTargetClient.OpenMouth();

            gameObject.GetComponent<Collider>().enabled = false; // Disable collider to prevent mid-flight collisions

            Sequence flightSeq = DOTween.Sequence();

            if (isCinematic && Toaster.Instance.cinematicCamera != null)
            {
                // Update camera to look at the toast during flight
                flightSeq.OnUpdate(() =>
                {
                    Toaster.Instance.cinematicCamera.transform.LookAt(transform.position);
                });
            }

            flightSeq.Append(transform.DOMove(target.position, flightDuration).SetEase(Ease.Linear));
            flightSeq.InsertCallback(flightDuration / 2f, () => { if (currentFlightTargetClient != null) currentFlightTargetClient.Recoil(); });
            flightSeq.OnComplete(() =>
            {
                rb.isKinematic = false;
                rb.useGravity = true;


                if (Toaster.Instance.cinematicCamera != null && Time.timeScale < 1f)
                {

                    DOVirtual.DelayedCall(0f, () =>
                    {
                        Time.timeScale = 1f;
                        currentFlightTargetClient.TryEatToast(jamOnThisToast, gameObject);

                        DOVirtual.DelayedCall(3f, () =>
                        {

                            Toaster.Instance.cinematicCamera.gameObject.SetActive(false);
                            Toaster.Instance.defaultCamera.gameObject.SetActive(true);
                        });
                    });
                }
                else
                {
                    currentFlightTargetClient.TryEatToast(jamOnThisToast, gameObject);

                }

                AudioManager.Instance.PlaySound(toastGettingIntoMouth, sfxMixer, transform.position);

            });
        }
        else
        {
            Toaster.Instance.TriggerPunishmentCooldown();

            rb.isKinematic = true;
            gameObject.GetComponent<Collider>().enabled = false;//
            if (letterText != null) letterText.enabled = false;

            Toaster.Instance.ResetCombo();

            MusicManager.Instance.RecordScratchStop(5.2f);

            foreach (ToastBehavior tb in Toaster.Instance.activeToasts)
            {
                if (tb != null && tb != this)
                    tb.ForceReleaseToast();
            }

            Client clientToBonk = ClientManager.Instance.GetClientInSeat(target);
            SphereCollider headCollider = clientToBonk != null ? clientToBonk.GetComponentInChildren<SphereCollider>() : null;
            Vector3 impactPoint = target.position;

            if (headCollider != null)
            {
                Vector3 dirToHead = (target.position - transform.position).normalized;
                float worldRadius = headCollider.radius * headCollider.transform.lossyScale.x;
                impactPoint = target.position - (dirToHead * worldRadius);
            }

            // --- CAMERA & EYELID CACHING ---
            Camera mainCam = Camera.main;
            float originalFOV = mainCam.fieldOfView;
            Quaternion originalCamRot = mainCam.transform.rotation;

            float zoomedFOV = 25f;

            Sequence splatSeq = DOTween.Sequence();

            // 1. Travel to face
            splatSeq.Append(transform.DOMove(impactPoint, flightDuration).SetEase(Ease.InQuad));

            // 2. Impact Phase
            splatSeq.AppendCallback(() => {
                if (clientToBonk != null)
                {
                    clientToBonk.HardRecoil();
                    clientToBonk.FreezeClient();
                    clientToBonk.SetAngryEyelids();

                }
                transform.DOScaleZ(0.1f, 0.05f);

                // --- CINEMATIC CAMERA: Zoom + Kickback Shake ---
                mainCam.DOFieldOfView(zoomedFOV, 0.25f).SetEase(Ease.OutExpo);
                mainCam.transform.DOLookAt(impactPoint, 0.25f).SetEase(Ease.OutExpo);
                mainCam.transform.DOShakePosition(0.3f, 0.05f, 20); // Adds a "thud" feel to the camera
            });

            // 3. The Sticky Pause
            splatSeq.AppendInterval(1.2f);

            // 4. The Slow Slide
            float slideDistance = headCollider != null ? headCollider.radius * 3f : 2f;
            splatSeq.Append(transform.DOMoveY(impactPoint.y - slideDistance, 4.0f).SetEase(Ease.InSine));
            splatSeq.Join(transform.DORotate(new Vector3(60, transform.eulerAngles.y, 0), 4.0f).SetEase(Ease.InSine));

            // 5. Cleanup & Return
            splatSeq.OnComplete(() => {
                if (clientToBonk != null)
                {
                    clientToBonk.RestoreEyelids();
                    clientToBonk.UnfreezeClient();
                }

                // --- CINEMATIC CAMERA: Snap back with a "Back" ease for extra style ---
                mainCam.DOFieldOfView(originalFOV, 0.6f).SetEase(Ease.InOutBack);
                mainCam.transform.DORotateQuaternion(originalCamRot, 0.6f).SetEase(Ease.InOutBack);

                Destroy(gameObject);
            });

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

    void ForceReleaseToast()
    {
        isHovering = false;
        if (bobTween != null) bobTween.Kill();
        rb.useGravity = true;
        rb.linearVelocity = new Vector3(capturedXVel, -0.1f, capturedZVel);

        isPunchable = false;
        rb.constraints = RigidbodyConstraints.None;
        Toaster.Instance.ResetCombo();
        letterText.enabled = false;
    }

    IEnumerator GracePeriodTimer()
    {
        yield return new WaitForSeconds(fallGracePeriod);
        if (!hasBeenHit)
        {
            isPunchable = false;
            rb.constraints = RigidbodyConstraints.None;
            Toaster.Instance.ResetCombo();
            letterText.enabled = false;
        }
    }

    private void OnDestroy() { if (Toaster.Instance != null) Toaster.Instance.UnregisterToast(this); transform.DOKill(); }
}