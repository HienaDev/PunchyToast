using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using TMPro;

public class ClientManager : MonoBehaviour
{
    public static ClientManager Instance;

    [Header("Level Configuration")]
    public LevelConfiguration levelConfig;
    private int currentWaveIndex = 0;
    private int clientsFinishedInWave = 0;
    private int totalClientsInLevel = 0;
    private int totalClientsSatisfied = 0;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI maxClientsText;
    [SerializeField] private TextMeshProUGUI satisfiedClientsText;

    [Header("Setup")]
    public GameObject[] clientPrefab;
    public List<Transform> seatingPositions;
    private Dictionary<Transform, Client> activeClients = new Dictionary<Transform, Client>();
    public bool areThereClients => activeClients.Values.Count(c => c.isSat) > 0;

    private string currentWord = "";
    private List<int> availableIndexes = new List<int>();
    private int currentIndex = 0;
    private float levelStartTime;
    private bool levelFinished = false;

    [SerializeField] private LevelComplete levelCompleteUI;
    [SerializeField] private GameObject clientCounterUI;

    [SerializeField] private Sprite butterIcon;
    [SerializeField] private Sprite strawberryIcon;
    [SerializeField] private Sprite grapeIcon;
    [SerializeField] private Sprite chocolateIcon;

    void Awake() { if (Instance == null) Instance = this; }

    public void StartLevel(LevelConfiguration config)
    {
        levelStartTime = Time.time;
        levelConfig = config;
        currentWaveIndex = 0;
        totalClientsSatisfied = 0;
        Toaster.Instance.SetupToasterSettings(config);
        clientCounterUI.SetActive(true);
        CalculateTotalLevelClients();
        UpdateUI();
        ConfigureJamsForLevel();
        StartCoroutine(SpawnWave(currentWaveIndex));
    }

    public void PrepareNextLetterForSimultaneous()
    {
        // This allows the Toaster to "consume" the next letter index 
        // immediately during a simultaneous burst
        currentIndex++;
    }

    private void CalculateTotalLevelClients()
    {
        totalClientsInLevel = levelConfig.waves.Sum(w => w.clientsInWave.Count);
    }

    private void UpdateUI()
    {
        if (maxClientsText != null) maxClientsText.text = totalClientsInLevel.ToString();
        if (satisfiedClientsText != null) satisfiedClientsText.text = totalClientsSatisfied.ToString();
    }

    void ConfigureJamsForLevel()
    {
        HashSet<JamFlavor> neededFlavors = new HashSet<JamFlavor>();
        foreach (var wave in levelConfig.waves)
            foreach (var client in wave.clientsInWave)
            {
                if (client.jamFlavor == JamFlavor.Random) { neededFlavors.Add(JamFlavor.Butter); neededFlavors.Add(JamFlavor.StrawberryJam); }
                else if (client.jamFlavor != JamFlavor.None) neededFlavors.Add(client.jamFlavor);
            }
        JamDecider.Instance.SetupLevelJams(neededFlavors);
    }

    public bool GetSimultaneousStatusForNextToast()
    {
        if (levelConfig == null || currentWaveIndex >= levelConfig.waves.Count) return false;
        var wave = levelConfig.waves[currentWaveIndex];
        int dataIndex = Mathf.Clamp(currentIndex, 0, wave.clientsInWave.Count - 1);
        return wave.clientsInWave[dataIndex].simultaneousToast;
    }

    public bool IsLastClientOfWave(Client client)
    {
        if (levelConfig == null || currentWaveIndex >= levelConfig.waves.Count) return false;
        int progress = clientsFinishedInWave + activeClients.Values.Count(c => c.isSatisfied);
        return progress >= levelConfig.waves[currentWaveIndex].clientsInWave.Count;
    }

    IEnumerator SpawnWave(int index)
    {
        if (index >= levelConfig.waves.Count) { StartCoroutine(FinishLevelRoutine()); yield break; }
        if (index > 0) yield return new WaitForSeconds(0.2f);

        clientsFinishedInWave = 0;
        var wave = levelConfig.waves[index];
        currentWord = "";
        currentIndex = 0;
        availableIndexes = new List<int>();

        foreach (var clientData in wave.clientsInWave)
        {
            while (activeClients.Count >= seatingPositions.Count) yield return null;
            SpawnClient(clientData, wave); // Pass wave data here
            yield return new WaitForSeconds(0.1f);
        }
    }

    public char GetCurrentLetter()
    {
        if (currentIndex < currentWord.Length) return char.ToUpper(currentWord[currentIndex]);

        return (char)('A' + Random.Range(0, 26));
    }

    public int GetAvailableIndex()
    {
        // Ensure the list of active toasts is clean (no nulls)
        var activeToasts = Toaster.Instance.activeToasts.Where(t => t != null).ToList();

        for (int i = 0; i < availableIndexes.Count; i++)
        {
            int targetIndex = availableIndexes[i];

            // Check if ANY toast in the air is already "carrying" this index
            bool isIndexBusy = activeToasts.Any(toast => toast.myLetterIndex == targetIndex);

            // If NO toast is using this index, it's available!
            if (!isIndexBusy)
            {
                return targetIndex;
            }
        }

        // All currently available indexes are busy in the air
        return -1;
    }

    public void RemoveIndex(int i) => availableIndexes.Remove(i);

    public string GetCurrentWord()
        => currentWord;

    public void IncreaseLetterIndex() => currentIndex++;


    public int GetSimultaneousBurstCount()
    {
        if (levelConfig == null || currentWaveIndex >= levelConfig.waves.Count) return 1;

        var wave = levelConfig.waves[currentWaveIndex];
        int count = 1; // The current one
        int checkIndex = currentIndex;

        // Look ahead to see how many more are simultaneous
        while (checkIndex < wave.clientsInWave.Count && wave.clientsInWave[checkIndex].simultaneousToast)
        {
            count++;
            checkIndex++;
        }
        return count;
    }

    void SpawnClient(LevelConfiguration.ClientData data, LevelConfiguration.Wave wave)
    {
        List<Transform> availableSeats = new List<Transform>();
        for (int i = 0; i < seatingPositions.Count; i++)
        {
            if (activeClients.ContainsKey(seatingPositions[i])) continue;

            bool isBottom = i < 3;
            // Checking the row permissions from the specific wave passed in
            if (isBottom && wave.allowBottomRow) availableSeats.Add(seatingPositions[i]);
            else if (!isBottom && wave.allowTopRow) availableSeats.Add(seatingPositions[i]);
        }

        if (availableSeats.Count == 0) return;

        currentWord += data.customLetter;
        availableIndexes.Add(currentWord.Length - 1);
        Transform chosenSeat = availableSeats[Random.Range(0, availableSeats.Count)];

        GameObject randomClientPrefab = clientPrefab[Random.Range(0, clientPrefab.Length)];

        GameObject newClientObj = Instantiate(randomClientPrefab, chosenSeat.position, chosenSeat.rotation);
        Client clientScript = newClientObj.GetComponent<Client>();

        Sprite chosenSprite;

        switch(data.jamFlavor)
        {
            case JamFlavor.Butter: chosenSprite = butterIcon; break;
            case JamFlavor.StrawberryJam: chosenSprite = strawberryIcon; break;
            case JamFlavor.GrapeJam: chosenSprite = grapeIcon; break;
            case JamFlavor.PeanutButter: chosenSprite = chocolateIcon; break;
            default: chosenSprite = null; break;
        }

        clientScript.Initialize(chosenSeat, chosenSprite);
        activeClients.Add(chosenSeat, clientScript);

        JamFlavor finalFlavor = data.jamFlavor;
        if (finalFlavor == JamFlavor.Random) finalFlavor = JamDecider.Instance.activeJams[Random.Range(0, JamDecider.Instance.activeJams.Count)].flavor;
        clientScript.SetOrder(finalFlavor.ToString(), JamDecider.Instance.GetColorFromFlavor(finalFlavor));
    }

    public void OnClientFinished()
    {
        clientsFinishedInWave++;
        totalClientsSatisfied++;
        UpdateUI();
        if (clientsFinishedInWave >= levelConfig.waves[currentWaveIndex].clientsInWave.Count)
        {
            currentWaveIndex++;
            StartCoroutine(SpawnWave(currentWaveIndex));
        }
    }

    private IEnumerator FinishLevelRoutine()
    {
        levelFinished = true;
        yield return new WaitForSeconds(2f);
        float totalTime = Time.time - levelStartTime;
        int stars = CalculateStars(totalTime);
        levelCompleteUI.gameObject.SetActive(true);
        levelCompleteUI.Initialize(stars, totalTime);
        clientCounterUI.SetActive(false);
    }

    private int CalculateStars(float time)
    {
        if (time <= levelConfig.fiveStarTime) return 5;
        if (time <= levelConfig.fourStarTime) return 4;
        if (time <= levelConfig.threeStarTime) return 3;
        if (time <= levelConfig.twoStarTime) return 2;
        return 1;
    }

    public Transform GetBestTarget(string currentJamInHand)
    {
        var matchingKvp = activeClients.FirstOrDefault(kvp => kvp.Value.desiredCondiment == currentJamInHand && !kvp.Value.isSatisfied);
        if (matchingKvp.Value != null) { matchingKvp.Value.OpenMouth(); return matchingKvp.Value.transform; }
        if (activeClients.Count > 0) return activeClients.Keys.ElementAt(Random.Range(0, activeClients.Count));
        return seatingPositions[Random.Range(0, seatingPositions.Count)];
    }

    public void ClearSeat(Transform seat)
    {
        if (activeClients.Remove(seat) && currentWaveIndex >= levelConfig.waves.Count && activeClients.Count == 0 && !levelFinished)
            StartCoroutine(FinishLevelRoutine());
    }
}