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

    private bool bald = false;

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
            bald = true;
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
        for (int i = 0; i < hairs.Length; i++)
        {
            
            if (hairs[i] != null) hairs[i].SetActive(i == index);
            Debug.Log(gameObject.name + " - Hair Index: " + i + ", Active: " + (hairs[i] != null && hairs[i].activeSelf));
            Debug.LogFormat("Hair {0}: {1}", i, (hairs[i] != null) ? hairs[i].name : "null");
        }
    }

    private void ApplyToGroup(Renderer[] group, Material mat)
    {
        if (group == null) return;
        foreach (Renderer r in group) if (r != null) r.material = mat;
    }
}