using UnityEngine;
using DG.Tweening;

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
    
    [Header("Wander Settings")]
    [SerializeField] private float wanderDistance = 0.5f;
    [SerializeField] private float leanAngle = 10f;
    [SerializeField] private int wanderCycles = 3;

    private Vector3 originalMouthRot;
    private Vector3 initialPos;
    private Quaternion originalRotation;

    void Start()
    {
        originalMouthRot = mouthBone.localEulerAngles;
        initialPos = transform.position;
        originalRotation = transform.rotation;

        CyclePuppet();
    }

    private void CyclePuppet()
    {
        ApplyRandomVisuals();

        // 1. Reset Position and Appearance
        transform.position = initialPos + Vector3.down * popUpDistance;
        transform.rotation = originalRotation;

        // 2. Enter Scene
        transform.DOMove(initialPos, entranceDuration).SetEase(Ease.OutBack).OnComplete(() =>
        {
            StartWanderSequence();
        });
    }

    private void StartWanderSequence()
    {
        Sequence wanderSeq = DOTween.Sequence();

        for (int i = 0; i < wanderCycles; i++)
        {
            float side = (i % 2 == 0) ? 1 : -1;
            Vector3 targetSidePos = initialPos + (Vector3.right * side * wanderDistance);
            
            // Move Side to Side
            wanderSeq.Append(transform.DOMoveX(targetSidePos.x, 0.8f).SetEase(Ease.InOutQuad));
            
            // Subtle Hop while moving
            wanderSeq.Join(pivot.DOLocalMoveY(0.05f, 0.4f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
            
            // Lean into the movement
            wanderSeq.Join(transform.DORotate(new Vector3(0, 0, -side * leanAngle), 0.8f).SetEase(Ease.InOutQuad));
        }

        // Return to center and upright before the big hops
        wanderSeq.Append(transform.DOMoveX(initialPos.x, 0.5f).SetEase(Ease.InOutQuad));
        wanderSeq.Join(transform.DORotate(Vector3.zero, 0.5f).SetEase(Ease.InOutQuad));

        wanderSeq.OnComplete(() =>
        {
            // 3. Big Hop Sequence (3-4 hops)
            pivot.DOLocalMoveY(0.15f, 0.25f).SetLoops(6, LoopType.Yoyo).SetEase(Ease.InOutSine).OnComplete(() =>
            {
                // 4. Munch
                PlayMunchAnimation(() =>
                {
                    // 5. Exit Scene
                    transform.DOMove(initialPos + Vector3.down * popUpDistance, 0.5f).SetEase(Ease.InBack).OnComplete(() =>
                    {
                        CyclePuppet(); // Loop
                    });
                });
            });
        });
    }

    public void PlayMunchAnimation(System.Action onComplete)
    {
        Sequence munchSeq = DOTween.Sequence();
        for (int i = 0; i < 3; i++)
        {
            munchSeq.Append(mouthBone.DOLocalRotate(originalMouthRot, 0.08f));
            munchSeq.Append(mouthBone.DOLocalRotate(mouthOpenRotation / 2f, 0.08f));
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
            else if (Random.value < randomHairColor)
            {
                foreach (Renderer r in hair) if (r != null) r.material = materials[Random.Range(0, materials.Length)];
            }
            else
            {
                ApplyToGroup(hair, materials[Random.Range(0, materials.Length)]);
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