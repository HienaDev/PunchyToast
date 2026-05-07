using UnityEngine;

public class PuppetVisualizer : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private Renderer[] hair;
    [SerializeField] private Renderer[] shirt;
    [SerializeField] private Renderer[] skin;

    [SerializeField] private GameObject[] hairs;

    [Header("Materials")]
    [SerializeField] private Material[] materials;
    [SerializeField] private Material specialMaterial;

    [Header("Chances")]
    [SerializeField] private float specialChance = 0.2f;
    [SerializeField] private float randomBaldChance = 0.1f;

    void Awake()
    {
        gameObject.name = "Puppet_" + Random.Range(1000, 9999);
        ApplyRandomVisuals();
    }

    public void ApplyRandomVisuals()
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
  
        }

        // 2. Standard Randomization
        else if (materials.Length > 0)
        {
            Material selectedSkinMat = materials[Random.Range(0, materials.Length)];
            Material selectedShirtMat = materials[Random.Range(0, materials.Length)];

            ApplyToGroup(skin, selectedSkinMat);
            ApplyToGroup(shirt, selectedShirtMat);


        }

        // Hair Logic
        if (Random.value < randomBaldChance)
        {
            foreach (GameObject hairObj in hairs)
            {
                if (hairObj != null) hairObj.SetActive(false);
            }
        }
        else
        {
            SelectRandomHair();
            Material selectedHairMat = materials[Random.Range(0, materials.Length)];
            ApplyToGroup(hair, selectedHairMat);
        }
    }

    private void SelectRandomHair()
    {
        if (hairs.Length == 0) return;
        int index = Random.Range(0, hairs.Length);

        foreach (GameObject hairObj in hairs)
        {
            hairObj.SetActive(false);
        }

        hairs[index].SetActive(true);
    }

    private void ApplyToGroup(Renderer[] group, Material mat)
    {
        if (group == null) return;
        foreach (Renderer r in group) if (r != null) r.material = mat;
    }
}