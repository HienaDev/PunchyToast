using TMPro;
using UnityEngine;
using System.Collections;

public class LocalizedText : MonoBehaviour
{
    [SerializeField] private string localizationKey;
    private TextMeshProUGUI textComponent;

    IEnumerator Start()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        LocalizationEvents.OnLanguageChanged += UpdateText;

        yield return new WaitForSeconds(0.1f); // Wait for Settings to initialize
        UpdateText(); // Initial update
    }

    private void OnEnable()
    {
        textComponent = GetComponent<TextMeshProUGUI>();
        LocalizationEvents.OnLanguageChanged += UpdateText;

        UpdateText(); // Initial update
    }

    void OnDestroy()
    {
        LocalizationEvents.OnLanguageChanged -= UpdateText;
    }

    void UpdateText()
    {
        textComponent.text = Settings.GetText(localizationKey);
    }
}