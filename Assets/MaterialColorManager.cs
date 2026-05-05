using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MaterialColorManager : MonoBehaviour
{
    [SerializeField] private Material targetMaterial;
    [SerializeField] private string shaderColorPropertyName = "_BaseColor";

    // Unique key to identify this specific material's color in PlayerPrefs
    public string ColorSaveKey = "SavedSkinColor";

    private void OnEnable()
    {
        LoadAndApplyColor();
    }

    public void ChangeColorAutomatically()
    {
        // Grabs the GameObject that currently has "focus" (the button you just clicked)
        GameObject clickedObject = EventSystem.current.currentSelectedGameObject;

        if (clickedObject == null || targetMaterial == null) return;

        Image childImage = clickedObject.GetComponentInChildren<Image>();

        if (childImage != null)
        {
            Color newColor = childImage.color;

            // 1. Apply to Material
            targetMaterial.SetColor(shaderColorPropertyName, newColor);

            // 2. Save to PlayerPrefs
            SaveColor(newColor);
        }
    }

    private void SaveColor(Color color)
    {
        // Convert color to a Hex string (e.g., "#FF0000") to save as text
        string hex = ColorUtility.ToHtmlStringRGBA(color);
        PlayerPrefs.SetString(ColorSaveKey, hex);
        PlayerPrefs.Save();
    }

    private void LoadAndApplyColor()
    {
        if (targetMaterial == null) return;

        // Check if we have a saved color
        if (PlayerPrefs.HasKey(ColorSaveKey))
        {
            string hex = PlayerPrefs.GetString(ColorSaveKey);

            // Parse the Hex string back into a Unity Color object
            if (ColorUtility.TryParseHtmlString("#" + hex, out Color loadedColor))
            {
                targetMaterial.SetColor(shaderColorPropertyName, loadedColor);
            }
        }
    }
}