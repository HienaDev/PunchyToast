using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIImageColorManager : MonoBehaviour
{
    [SerializeField] private Image targetImage; // The UI Image you want to tint
    [SerializeField] private string colorSaveKey = "SavedSkinColor";

    private void OnEnable()
    {
        LoadAndApplyColor();
    }

    public void ChangeColorAutomatically()
    {
        // Grabs the button you just clicked
        GameObject clickedObject = EventSystem.current.currentSelectedGameObject;

        if (clickedObject == null || targetImage == null) return;

        // Grabs the color from the button's icon/child image
        Image sourceImage = clickedObject.GetComponentInChildren<Image>();

        if (sourceImage != null)
        {
            Color newColor = sourceImage.color;

            // 1. Apply to UI Image
            targetImage.color = newColor;

            // 2. Save to PlayerPrefs
            SaveColor(newColor);
        }
    }

    private void SaveColor(Color color)
    {
        string hex = ColorUtility.ToHtmlStringRGBA(color);
        PlayerPrefs.SetString(colorSaveKey, hex);
        PlayerPrefs.Save();
    }

    private void LoadAndApplyColor()
    {
        if (targetImage == null) return;

        if (PlayerPrefs.HasKey(colorSaveKey))
        {
            string hex = PlayerPrefs.GetString(colorSaveKey);
            if (ColorUtility.TryParseHtmlString("#" + hex, out Color loadedColor))
            {
                targetImage.color = loadedColor;
            }
        }
    }
}