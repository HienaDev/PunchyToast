using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public static class LocalizationEvents
{
    public static event System.Action OnLanguageChanged;
    public static void NotifyLanguageChanged() => OnLanguageChanged?.Invoke();
}


public class Settings : MonoBehaviour
{
    public static Settings instance;

    public enum LanguageOption { English, Português }

    [Header("Localization Settings")]
    [Tooltip("This will be used if the UI Dropdown is null or not yet set.")]
    public LanguageOption inspectorLanguage = LanguageOption.English;

    public string startingLanguage = "English";
    private string currentLanguage;
    public string csvFileName = "localization.csv";
    [SerializeField] private TMP_Dropdown languageDropdown;

    private string csvContent;
    private Dictionary<string, Dictionary<string, string>> localizedData;

    public static string GetText(string key)
    {
        if (instance == null || instance.localizedData == null) return key;

        // Determine which language string to use
        string langToUse = instance.currentLanguage;

        // Fallback: If currentLanguage is empty and dropdown is null, use the Inspector enum
        if (string.IsNullOrEmpty(langToUse) && instance.languageDropdown == null)
        {
            langToUse = instance.inspectorLanguage.ToString();
        }

        if (instance.localizedData.ContainsKey(langToUse) &&
            instance.localizedData[langToUse].ContainsKey(key))
        {
            return instance.localizedData[langToUse][key];
        }

        return key;
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Prioritize PlayerPrefs, then inspector enum, then startingLanguage string
        currentLanguage = PlayerPrefs.GetString("Language", inspectorLanguage.ToString());
        StartCoroutine(LoadCSV());
    }

    private void OnValidate()
    {
        // Only run this if the game is actually playing, 
        // otherwise there is no instance to update.
        if (Application.isPlaying && instance != null)
        {
            // Sync the internal string to the enum selection
            currentLanguage = inspectorLanguage.ToString();

            // Trigger the same event the dropdown uses
            LocalizationEvents.NotifyLanguageChanged();

            // Optional: Update PlayerPrefs so it persists
            PlayerPrefs.SetString("Language", currentLanguage);
        }
    }



    IEnumerator LoadCSV()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, csvFileName);

        // WebGL requires a web request to access StreamingAssets
        using (UnityWebRequest www = UnityWebRequest.Get(filePath))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                csvContent = www.downloadHandler.text;
                ParseDict();
            }
            else
            {
                Debug.LogError($"Failed to load localization CSV at {filePath}: {www.error}");
            }
        }
    }


    private void ParseDict()
    {
        localizedData = new Dictionary<string, Dictionary<string, string>>();
        string[] lines = csvContent.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length < 2) return;

        string[] headers = lines[0].Split(',');

        for (int i = 1; i < headers.Length; i++)
        {
            localizedData[headers[i].Trim()] = new Dictionary<string, string>();
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string[] parts = SplitCsvLine(lines[i]);
            if (parts.Length != headers.Length) continue;

            string key = parts[0].Trim();
            for (int j = 1; j < headers.Length; j++)
            {
                string lang = headers[j].Trim();
                string value = parts[j].Trim().Trim('"').Replace("\\n", "\n");
                localizedData[lang][key] = value;
            }
        }

        InitiateDropdown();
    }

    private string[] SplitCsvLine(string line)
    {
        var values = new List<string>();
        bool inQuotes = false;
        string current = "";
        foreach (char c in line)
        {
            if (c == '"') inQuotes = !inQuotes;
            else if (c == ',' && !inQuotes) { values.Add(current); current = ""; }
            else current += c;
        }
        values.Add(current);
        return values.ToArray();
    }

    private void InitiateDropdown()
    {
        // If there is no UI dropdown, just notify that language is ready based on inspector setting
        if (languageDropdown == null)
        {
            LocalizationEvents.NotifyLanguageChanged();
            return;
        }

        languageDropdown.ClearOptions();
        List<string> options = new List<string>(localizedData.Keys.ToList());
        languageDropdown.AddOptions(options);
        languageDropdown.onValueChanged.AddListener(ChangeLanguage);

        int index = options.IndexOf(currentLanguage);
        languageDropdown.value = Mathf.Max(0, index);

        ChangeLanguage(languageDropdown.value);
    }

    public void ChangeLanguage(int index)
    {
        if (languageDropdown != null)
        {
            currentLanguage = languageDropdown.options[index].text;
        }
        else
        {
            currentLanguage = inspectorLanguage.ToString();
        }

        PlayerPrefs.SetString("Language", currentLanguage);
        LocalizationEvents.NotifyLanguageChanged();
    }
}