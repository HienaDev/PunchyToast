using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.UI; // Added for List

public class Client : MonoBehaviour
{
    [Header("Settings")]
    public Transform mouthBone;
    public Transform recoilBone;
    public Transform pivot;
    public Transform TargetForToast;
    public Vector3 mouthOpenRotation = new Vector3(-45, 0, 0);
    public float entranceDuration = 0.6f;
    public float popUpDistance = 2.0f;

    [SerializeField] private bool useRandomVisual = true;

    public int toastsToSatisfy = 1;
    private int currentToastsEaten = 0;

    [Header("Rendering")]
    [SerializeField] private Renderer[] hair;
    [SerializeField] private Renderer[] shirt;
    [SerializeField] private Renderer[] skin;
    [SerializeField] private Material[] materials;
    [SerializeField] private Material specialMaterial;
    [SerializeField] private float specialChance = 0.25f;
    [SerializeField] private float randomHairColor = 0.25f;
    [SerializeField] private float randomBaldChance = 0.1f;

    [Header("Eye Tracking")]
    [SerializeField] private Transform leftEye;
    [SerializeField] private Transform rightEye;
    private Transform currentEyeTarget;
    private float searchTimer = 0f;
    private float searchInterval = 0.2f; // Search for toast 5 times a second, not every frame

    [Header("Waiting Animation Settings")]
    [SerializeField] private Vector3 waitingMouthClosed = new Vector3(-10, 0, 0);
    [SerializeField] private Vector3 waitingMouthOpen = new Vector3(-20, 0, 0);
    [SerializeField] private float waitingCycleDuration = 0.4f;

    [Header("Randomized Hopping")]
    [SerializeField] private float minHopHeight = 0.02f;
    [SerializeField] private float maxHopHeight = 0.08f;
    [SerializeField] private float minHopSpeed = 0.3f;
    [SerializeField] private float maxHopSpeed = 0.6f;

    [Header("Current Order")]
    public string desiredCondiment;
    public Color condimentColor;
    private bool hasEaten = false;
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

    [SerializeField] private GameObject thoughtBubble;
    [SerializeField] private Image wantIcon;

    void Start()
    {
        originalMouthRot = mouthBone.localEulerAngles;
        if (pivot != null) pivotInitialLocalPos = pivot.localPosition;

        ApplyRandomVisuals();

        StartBlinking();
    }

    void Update()
    {
        HandleEyeTracking();
    }

    private void HandleEyeTracking()
    {
        if (leftEye == null || rightEye == null) return;

        // Optimization: Only search for the closest toast occasionally
        searchTimer -= Time.deltaTime;
        if (searchTimer <= 0f)
        {
            FindClosestToast();
            searchTimer = searchInterval;
        }

        // Determine look target (Toast or Camera)
        Vector3 targetPos;
        if (currentEyeTarget != null)
        {
            targetPos = currentEyeTarget.position;
        }
        else if (Camera.main != null)
        {
            targetPos = Camera.main.transform.position;
        }
        else
        {
            return;
        }

        // Apply LookAt logic
        RotateEye(leftEye, targetPos);
        RotateEye(rightEye, targetPos);
    }

    [SerializeField] private float eyeClampAngle = 45f;

    private void RotateEye(Transform eye, Vector3 targetWorldPos)
    {
        Vector3 direction = targetWorldPos - eye.position;
        if (direction != Vector3.zero)
        {
            // 1. Calculate the "Perfect Tracking" rotation (World Space)
            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.forward);
            Quaternion targetWorldRot = lookRotation * Quaternion.Euler(90, 0, 0);

            // 2. Define the Rest Pose (-90, 0, 0 local) in World Space
            // We calculate this by taking the Parent's rotation and applying your offset
            Quaternion restWorldRot = eye.parent.rotation * Quaternion.Euler(-90, 0, 0);

            // 3. Calculate the angle between the Rest Pose and the Target Pose
            float angleDiff = Quaternion.Angle(restWorldRot, targetWorldRot);

            // 4. Apply Constraint
            if (angleDiff <= eyeClampAngle)
            {
                // If within 45 degrees, track perfectly
                eye.rotation = targetWorldRot;
            }
            else
            {
                // If it tries to go past 45 degrees, stop exactly at the limit (45 deg away from rest)
                eye.rotation = Quaternion.RotateTowards(restWorldRot, targetWorldRot, eyeClampAngle);
            }
        }
    }

    private void FindClosestToast()
    {
        // Find all potential toasts
        ToastBehavior[] allToasts = Object.FindObjectsByType<ToastBehavior>(FindObjectsSortMode.None);

        if (allToasts.Length == 0)
        {
            currentEyeTarget = null;
            return;
        }

        float closestDist = Mathf.Infinity;
        Transform closestTrans = null;

        foreach (var toast in allToasts)
        {
            // Check if the toast component exists and if it is puncheable
            if (toast != null && toast.isPunchable)
            {
                float dist = Vector3.Distance(transform.position, toast.transform.position);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestTrans = toast.transform;
                }
            }
        }

        // If no puncheable toasts were found, closestTrans will be null
        // Your HandleEyeTracking logic already handles looking at the camera if this is null
        currentEyeTarget = closestTrans;
    }


    public void Initialize(Transform seat, Sprite wantedJam)
    {
        mySeat = seat;
        targetPosition = transform.position;
        wantIcon.sprite = wantedJam;
        EnterScene();
    }

    private void EnterScene()
    {
        transform.position = targetPosition + Vector3.down * popUpDistance;
        transform.DOMove(targetPosition, entranceDuration).SetEase(Ease.Linear).OnComplete(() => {
            isSat = true;
            thoughtBubble.SetActive(true);
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

        mouthBone.DOKill();

        mouthBone.DOLocalRotate(waitingMouthClosed, 1f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            CreateWaitingSequence();
        });
    }

    private void CreateWaitingSequence()
    {
        if (mouthBone == null || isSatisfied) return;

        float xOffset = Random.Range(-5f, 5f);
        Vector3 dynamicClosed = new Vector3(waitingMouthClosed.x + xOffset, waitingMouthClosed.y, waitingMouthClosed.z);
        Vector3 dynamicOpen = new Vector3(waitingMouthOpen.x + xOffset, waitingMouthOpen.y, waitingMouthOpen.z);

        Sequence waitSeq = DOTween.Sequence();
        int chatterCycles = Random.Range(3, 7);

        for (int i = 0; i < chatterCycles; i++)
        {
            waitSeq.Append(mouthBone.DOLocalRotate(dynamicOpen, waitingCycleDuration).SetEase(Ease.InOutSine));
            waitSeq.Append(mouthBone.DOLocalRotate(dynamicClosed, waitingCycleDuration).SetEase(Ease.InOutSine));
        }

        if (Random.value > 0.5f)
        {
            waitSeq.Append(mouthBone.DOLocalRotate(originalMouthRot, 0.2f).SetEase(Ease.InOutQuad));
            waitSeq.AppendInterval(0.5f);
            waitSeq.Append(mouthBone.DOLocalRotate(dynamicClosed, 0.2f).SetEase(Ease.InOutQuad));
        }

        waitSeq.OnComplete(() => {
            if (!isSatisfied) CreateWaitingSequence();
        });

        mouthTween = waitSeq;
    }

    public void Satisfy()
    {


        currentToastsEaten++;

        if (currentToastsEaten >= toastsToSatisfy)
        {
            isSatisfied = true;
            isSat = false;
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
            munchSeq.Append(mouthBone.DOLocalRotate(originalMouthRot, 0.08f).SetEase(Ease.Linear));
            munchSeq.Append(mouthBone.DOLocalRotate(mouthOpenRotation / 3f, 0.08f).SetEase(Ease.Linear));
        }

        munchSeq.Append(mouthBone.DOLocalRotate(originalMouthRot, 0.05f).SetEase(Ease.Linear));
        munchSeq.OnComplete(() => onComplete?.Invoke());
    }

    public void Recoil()
    {
        if (recoilBone != null)
        {
            recoilBone.DOKill();
            Sequence recoilSeq = DOTween.Sequence();
            recoilSeq.Append(recoilBone.DOLocalRotate(new Vector3(40, 0, 0), 0.1f).SetEase(Ease.OutBack));
            recoilSeq.Append(recoilBone.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.InQuad));
        }
    }

    public void HardRecoil()
    {
        if (recoilBone != null)
        {
            recoilBone.DOKill();
            Sequence hardRecoilSeq = DOTween.Sequence();

            // INCREASED ANGLE: 70 degrees instead of 40
            // FASTER SPEED: 0.05s instead of 0.1s
            hardRecoilSeq.Append(recoilBone.DOLocalRotate(new Vector3(70, 0, 0), 0.05f).SetEase(Ease.OutBack));

            // SNAPPY RETURN: 0.15s instead of 0.2s
            hardRecoilSeq.Append(recoilBone.DOLocalRotate(Vector3.zero, 0.15f).SetEase(Ease.InQuad));

            // ADDED: A small shake to make the impact feel "harder"
            transform.DOShakePosition(0.2f, 0.1f, 20);
        }
    }

    public void TryEatToast(string incomingJam, GameObject toast)
    {
        if (incomingJam == desiredCondiment)
        {
            OpenMouth();

            DOVirtual.DelayedCall(0.4f, () => {
                if (toast != null) Destroy(toast);

                PlayMunchAnimation(() => {
                    if (isSatisfied && !hasEaten)
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
        hasEaten = true;
        thoughtBubble.GetComponent<HoverAndScale>().Descale();

        if (hopTween != null) hopTween.Kill();
        if (mouthTween != null) mouthTween.Kill();

        if (pivot != null)
        {
            pivot.DOKill();
            pivot.DOLocalRotate(Vector3.zero, 0.2f);
            pivot.DOLocalMove(pivotInitialLocalPos, 0.2f);
        }

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

    private void ApplyRandomVisuals()
    {

        if (!useRandomVisual) return;

        // 1. Check for Special Material Trigger
        if (Random.value < specialChance && specialMaterial != null)
        {
            ApplySpecialMaterial();
            return;
        }

        // 2. Standard Material Randomization
        if (materials.Length > 0)
        {
            // Pick one material for all skin and one for all shirt
            Material selectedSkinMat = materials[Random.Range(0, materials.Length)];
            Material selectedShirtMat = materials[Random.Range(0, materials.Length)];

            ApplyToGroup(skin, selectedSkinMat);
            ApplyToGroup(shirt, selectedShirtMat);

            // 3. Handle Hair (Check for Individual Randomization Trigger)
            if (Random.value < randomBaldChance)
            {
                // Bald client, disable all hair renderers
                foreach (Renderer r in hair)
                {
                    if (r != null) r.enabled = false;
                }
            }
            else if (Random.value < randomHairColor)
            {
                // Every hair piece gets a different random material
                foreach (Renderer r in hair)
                {
                    if (r != null) r.material = materials[Random.Range(0, materials.Length)];
                }
            }
            else
            {
                // All hair pieces share the same random material
                Material selectedHairMat = materials[Random.Range(0, materials.Length)];
                ApplyToGroup(hair, selectedHairMat);
            }
        }
    }

    private void ApplySpecialMaterial()
    {
        // Create a unique instance so we don't change the project asset
        Material instance = new Material(specialMaterial);

        // Generate 3 random colors
        instance.SetColor("_SkinColor", Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f));
        instance.SetColor("_ShirtColor", Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f));
        instance.SetColor("_HairColor", Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f));

        // Apply this single material instance to every renderer
        ApplyToGroup(skin, instance);
        ApplyToGroup(shirt, instance);
        ApplyToGroup(hair, instance);
    }

    private void ApplyToGroup(Renderer[] group, Material mat)
    {
        if (group == null) return;
        foreach (Renderer r in group)
        {
            if (r != null) r.material = mat;
        }
    }
}