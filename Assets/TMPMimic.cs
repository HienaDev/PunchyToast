using UnityEngine;
using TMPro;

[ExecuteAlways] // Allows it to work in the Editor without hitting Play
public class TMPMimic : MonoBehaviour
{
    [Header("Settings")]
    public TextMeshProUGUI targetText;

    [Header("Properties to Mimic")]
    public bool mimicContent = true;
    public bool mimicFontSize = true;
    public bool mimicAlignment = true;
    public bool mimicFontStyle = true;

    private TextMeshProUGUI myText;

    private void Awake()
    {
        myText = GetComponent<TextMeshProUGUI>();
    }

    // LateUpdate ensures we copy the data AFTER the target might have changed in Update
    private void LateUpdate()
    {
        if (targetText == null || myText == null) return;

        if (mimicContent)
            myText.text = targetText.text;

        if (mimicFontSize)
            myText.fontSize = targetText.fontSize;

        if (mimicAlignment)
            myText.alignment = targetText.alignment;

        if (mimicFontStyle)
            myText.fontStyle = targetText.fontStyle;

        // Add more properties here if needed (margin, spacing, etc.)
    }
}