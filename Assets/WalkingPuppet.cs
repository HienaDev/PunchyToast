using UnityEngine;
using DG.Tweening;

public class WalkingPuppet : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkDistance = 10f;
    [SerializeField] private float walkDuration = 5f;
    [SerializeField] private Ease walkEase = Ease.Linear;

    [Header("Hopping (From Client)")]
    [SerializeField] private Transform pivot;
    [SerializeField] private float minHopHeight = 0.05f;
    [SerializeField] private float maxHopHeight = 0.12f;
    [SerializeField] private float minHopSpeed = 0.2f;
    [SerializeField] private float maxHopSpeed = 0.4f;

    [Header("Visuals")]
    [SerializeField] private Renderer[] hair;
    [SerializeField] private Renderer[] shirt;
    [SerializeField] private Renderer[] skin;
    [SerializeField] private Material[] materials;
    [SerializeField] private Material specialMaterial;
    [SerializeField] private float specialChance = 0.2f;
    [SerializeField] private float randomHairColor = 0.25f;
    [SerializeField] private float randomBaldChance = 0.1f;

    private Vector3 pivotInitialLocalPos;
    private bool isWalking = true;

    void Start()
    {
        if (pivot != null) pivotInitialLocalPos = pivot.localPosition;

        ApplyRandomVisuals();
        StartWalking();
        StartRandomHop();
    }

    private void StartWalking()
    {
        // Move forward (Z) or right (X) based on your needs. 
        // Using transform.forward * walkDistance allows you to just rotate the prefab in the scene to set direction.
        transform.DOMove(transform.position + transform.forward * walkDistance, walkDuration)
            .SetEase(walkEase)
            .OnComplete(() =>
            {
                isWalking = false;
                transform.DOKill(); // Stop any ongoing movement tweens
                Destroy(gameObject);
            });
    }

    private void StartRandomHop()
    {
        if (pivot == null || !isWalking) return;

        float randomHeight = Random.Range(minHopHeight, maxHopHeight);
        float randomSpeed = Random.Range(minHopSpeed, maxHopSpeed);

        // Simple Up-Down hop sequence
        pivot.DOLocalMoveY(pivotInitialLocalPos.y + randomHeight, randomSpeed)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                pivot.DOLocalMoveY(pivotInitialLocalPos.y, randomSpeed)
                    .SetEase(Ease.InQuad)
                    .OnComplete(StartRandomHop);
            });
    }

    private void ApplyRandomVisuals()
    {
        // 1. Special Material Check
        if (Random.value < specialChance && specialMaterial != null)
        {
            Material instance = new Material(specialMaterial);
            instance.SetColor("_SkinColor", Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f));
            instance.SetColor("_ShirtColor", Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f));
            instance.SetColor("_HairColor", Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f));

            ApplyToGroup(skin, instance);
            ApplyToGroup(shirt, instance);
            ApplyToGroup(hair, instance);
            return;
        }

        // 2. Standard Randomization
        if (materials.Length > 0)
        {
            Material selectedSkinMat = materials[Random.Range(0, materials.Length)];
            Material selectedShirtMat = materials[Random.Range(0, materials.Length)];

            ApplyToGroup(skin, selectedSkinMat);
            ApplyToGroup(shirt, selectedShirtMat);

            // Hair Logic
            if (Random.value < randomBaldChance)
            {
                foreach (Renderer r in hair) if (r != null) r.enabled = false;
            }
            else
            {
                Material selectedHairMat = (Random.value < randomHairColor)
                    ? materials[Random.Range(0, materials.Length)]
                    : materials[Random.Range(0, materials.Length)];
                ApplyToGroup(hair, selectedHairMat);
            }
        }
    }

    private void ApplyToGroup(Renderer[] group, Material mat)
    {
        if (group == null) return;
        foreach (Renderer r in group) if (r != null) r.material = mat;
    }
}