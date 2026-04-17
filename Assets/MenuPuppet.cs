using UnityEngine;
using DG.Tweening;
using System.Collections;

public class MenuPuppet : MonoBehaviour
{
    [Header("Rendering")]
    [SerializeField] private Renderer[] hair;
    [SerializeField] private Renderer[] shirt;
    [SerializeField] private Renderer[] skin;
    [SerializeField] private Material[] materials;
    [SerializeField] private Material specialMaterial;
    [SerializeField] private float specialChance = 0.25f;
    [SerializeField] private float randomHairColor = 0.25f;
    [SerializeField] private float randomBaldChance = 0.1f;

    [Header("Animation Settings")]
    [SerializeField] private Transform mouthBone;
    [SerializeField] private Transform pivot;
    [SerializeField] private float entranceDuration = 0.6f;
    [SerializeField] private float popUpDistance = 2.0f;
    [SerializeField] private Vector3 mouthOpenRotation = new Vector3(-45, 0, 0);

    [Header("Hop Settings")]
    [SerializeField] private float wanderHopHeight = 0.05f;

    [Header("Look Settings")]
    [SerializeField] private float lookAngle = 30f;
    [SerializeField] private float lookSpeed = 0.4f;
    [SerializeField] private float lookPause = 0.2f;

    [Header("Wander Settings")]
    [SerializeField] private float wanderDistance = 0.5f;
    [SerializeField] private float leanAngle = 10f;
    [SerializeField] private int wanderCycles = 3;
    [SerializeField] private float wanderSpeed = 0.8f;

    [Header("Spawn Delay Settings")]
    [SerializeField] private float minSpawnDelay = 1.0f;
    [SerializeField] private float maxSpawnDelay = 5.0f;
    [Tooltip("Positive plays after pop-up starts, Negative plays before it pops up")]
    [SerializeField] private float soundDelay = 0.0f;

    private Vector3 originalMouthRot;
    private Vector3 initialPos;
    private Quaternion originalRotation;
    private Vector3 initialPivotPos;
    private Coroutine cycleCoroutine;

    [SerializeField] private AudioSource menuGuySound;

    void Awake()
    {
        // Cache initial states in Awake to ensure they are captured before any movement
        originalMouthRot = mouthBone.localEulerAngles;
        initialPos = transform.position;
        originalRotation = transform.rotation;
        initialPivotPos = pivot.localPosition;
    }

    private void OnEnable()
    {
        // Reset position to hidden and start the loop
        transform.position = initialPos + Vector3.down * popUpDistance;
        cycleCoroutine = StartCoroutine(CycleRoutine());
    }

    private void OnDisable()
    {
        // Clean up to prevent logic conflicts when the object is toggled
        if (cycleCoroutine != null) StopCoroutine(cycleCoroutine);
        transform.DOKill();
        pivot.DOKill();
        mouthBone.DOKill();
    }

    private IEnumerator PlaySoundWithDelay()
    {
        yield return new WaitForSeconds(soundDelay);
        if (menuGuySound != null) menuGuySound.Play();
    }

    private IEnumerator CycleRoutine()
    {
        while (true)
        {

            float randomWait = Random.Range(minSpawnDelay, maxSpawnDelay);

            // If delay is negative (e.g., -0.5), we wait slightly less time before playing sound
            float waitBeforeSound = (soundDelay < 0) ? randomWait + soundDelay : randomWait;
            yield return new WaitForSeconds(Mathf.Max(0, waitBeforeSound));

            // Play sound (This will trigger early if soundDelay is negative)
            if (menuGuySound != null) menuGuySound.Play();

            // If delay was positive, we still need to finish the original random wait
            if (soundDelay > 0) yield return new WaitForSeconds(soundDelay);

            ApplyRandomVisuals();

            // Safety kill before starting new sequence
            transform.DOKill();
            pivot.DOKill();

            transform.position = initialPos + Vector3.down * popUpDistance;
            transform.rotation = originalRotation;
            pivot.localPosition = initialPivotPos;

            bool sequenceFinished = false;

            transform.DOMove(initialPos, entranceDuration)
                .SetEase(Ease.OutBack)
                .OnComplete(() => StartWanderSequence(() => sequenceFinished = true));

            yield return new WaitUntil(() => sequenceFinished);

            bool exitFinished = false;
            transform.DOMove(initialPos + Vector3.down * popUpDistance, 0.5f)
                .SetEase(Ease.InBack)
                .OnComplete(() => exitFinished = true);

            yield return new WaitUntil(() => exitFinished);
        }
    }

    private void StartWanderSequence(System.Action onAllFinished)
    {
        Sequence wanderSeq = DOTween.Sequence();

        for (int i = 0; i < wanderCycles; i++)
        {
            float side = (i % 2 == 0) ? 1 : -1;
            Vector3 targetSidePos = initialPos + (Vector3.right * side * wanderDistance);

            wanderSeq.Append(transform.DOMoveX(targetSidePos.x, wanderSpeed).SetEase(Ease.InOutQuad));

            wanderSeq.Join(pivot.DOLocalMoveY(wanderHopHeight, wanderSpeed / 2f)
                .SetRelative(true)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine));

            Vector3 leanRotation = new Vector3(originalRotation.eulerAngles.x, originalRotation.eulerAngles.y, -side * leanAngle);
            wanderSeq.Join(transform.DORotate(leanRotation, wanderSpeed).SetEase(Ease.InOutQuad));
        }

        wanderSeq.Append(transform.DOMoveX(initialPos.x, wanderSpeed * 0.6f).SetEase(Ease.InOutQuad));
        wanderSeq.Join(transform.DORotate(originalRotation.eulerAngles, wanderSpeed * 0.6f).SetEase(Ease.InOutQuad));

        wanderSeq.Append(transform.DORotate(new Vector3(0, lookAngle, 0), lookSpeed).SetEase(Ease.OutBack));
        wanderSeq.AppendInterval(lookPause);
        wanderSeq.Append(transform.DORotate(new Vector3(0, -lookAngle, 0), lookSpeed * 1.5f).SetEase(Ease.OutBack));
        wanderSeq.AppendInterval(lookPause);
        wanderSeq.Append(transform.DORotate(originalRotation.eulerAngles, lookSpeed).SetEase(Ease.OutBack));

        wanderSeq.OnComplete(() =>
        {
            PlayMunchAnimation(() =>
            {
                onAllFinished?.Invoke();
            });
        });
    }

    public void PlayMunchAnimation(System.Action onComplete)
    {
        Sequence munchSeq = DOTween.Sequence();
        for (int i = 0; i < 3; i++)
        {
            munchSeq.Append(mouthBone.DOLocalRotate(mouthOpenRotation / 2f, 0.08f));
            munchSeq.Append(mouthBone.DOLocalRotate(originalMouthRot, 0.08f));
        }
        munchSeq.OnComplete(() => onComplete?.Invoke());
    }

    private void ApplyRandomVisuals()
    {
        foreach (Renderer r in hair) if (r != null) r.enabled = true;
        if (Random.value < specialChance && specialMaterial != null)
        {
            ApplySpecialMaterial();
            return;
        }
        if (materials.Length > 0)
        {
            Material selectedSkinMat = materials[Random.Range(0, materials.Length)];
            Material selectedShirtMat = materials[Random.Range(0, materials.Length)];
            ApplyToGroup(skin, selectedSkinMat);
            ApplyToGroup(shirt, selectedShirtMat);
            if (Random.value < randomBaldChance)
            {
                foreach (Renderer r in hair) if (r != null) r.enabled = false;
            }
            else
            {
                Material hairMat = (Random.value < randomHairColor) ? materials[Random.Range(0, materials.Length)] : selectedSkinMat;
                ApplyToGroup(hair, hairMat);
            }
        }
    }

    private void ApplySpecialMaterial()
    {
        Material instance = new Material(specialMaterial);
        instance.SetColor("_SkinColor", Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.7f, 1f));
        instance.SetColor("_ShirtColor", Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.7f, 1f));
        instance.SetColor("_HairColor", Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.7f, 1f));
        ApplyToGroup(skin, instance);
        ApplyToGroup(shirt, instance);
        ApplyToGroup(hair, instance);
    }

    private void ApplyToGroup(Renderer[] group, Material mat)
    {
        if (group == null) return;
        foreach (Renderer r in group) if (r != null) r.material = mat;
    }
}