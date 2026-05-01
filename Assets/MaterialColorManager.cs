using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Required to "grab itself"

public class MaterialColorManager : MonoBehaviour
{
    [SerializeField] private Material targetMaterial;
    [SerializeField] private string shaderColorPropertyName = "_BaseColor";

    // Link this to the OnClick() event, but leave the Object field empty in the Inspector
    public void ChangeColorAutomatically()
    {
        // Grabs the GameObject that currently has "focus" (the button you just clicked)
        GameObject clickedObject = EventSystem.current.currentSelectedGameObject;

        if (clickedObject == null || targetMaterial == null) return;

        // Try to get the Image from the child of whatever was clicked
        Image childImage = clickedObject.GetComponentInChildren<Image>();

        if (childImage != null)
        {
            targetMaterial.SetColor(shaderColorPropertyName, childImage.color);
        }
    }
}