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
    public GameObject clientPrefab;
    public List<Transform> seatingPositions; // MAKE SURE THIS HAS AT LEAST 5 SLOTS!
    private Dictionary<Transform, Client> activeClients = new Dictionary<Transform, Client>();
    public bool areThereClients => activeClients.Values.Count(c => c.isSat) > 0;

    private string currentWord = "";
    private int currentIndex = 0;

    // New variables for Level Completion
    private float levelStartTime;
    private bool levelFinished = false;

    [SerializeField] private LevelComplete levelCompleteUI;
    [SerializeField] private GameObject clientCounterUI;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        
    }

    public void StartLevel(LevelConfiguration config)
    {
        levelStartTime = Time.time; // Track start time
        levelConfig = config;

        currentWaveIndex = 0;
        totalClientsSatisfied = 0;

        clientCounterUI.SetActive(true);

        CalculateTotalLevelClients();
        UpdateUI();

        ConfigureJamsForLevel();
        StartCoroutine(SpawnWave(currentWaveIndex));
    }

    private void CalculateTotalLevelClients()
    {
        totalClientsInLevel = 0;
        foreach (var wave in levelConfig.waves)
        {
            totalClientsInLevel += wave.clientsInWave.Count;
        }
    }

    private void UpdateUI()
    {
        if (maxClientsText != null)
            maxClientsText.text = totalClientsInLevel.ToString();

        if (satisfiedClientsText != null)
            satisfiedClientsText.text = totalClientsSatisfied.ToString();
    }

    void ConfigureJamsForLevel()
    {
        HashSet<JamFlavor> neededFlavors = new HashSet<JamFlavor>();

        foreach (var wave in levelConfig.waves)
        {
            foreach (var client in wave.clientsInWave)
            {
                if (client.jamFlavor == JamFlavor.Random)
                {
                    neededFlavors.Add(JamFlavor.Butter);
                    neededFlavors.Add(JamFlavor.StrawberryJam);
                    neededFlavors.Add(JamFlavor.GrapeJam);
                    neededFlavors.Add(JamFlavor.PeanutButter);
                }
                else if (client.jamFlavor != JamFlavor.None)
                {
                    neededFlavors.Add(client.jamFlavor);
                }
            }
        }
        JamDecider.Instance.SetupLevelJams(neededFlavors);
    }

    public bool IsLastClientOfWave(Client client)
    {
        if (levelConfig == null || currentWaveIndex >= levelConfig.waves.Count)
            return false;

        var currentWave = levelConfig.waves[currentWaveIndex];

        // 1. Count how many clients are ALREADY satisfied in the scene
        int satisfiedInScene = activeClients.Values.Count(c => c.isSatisfied);

        // 2. Total finished so far (those who already left) + those currently eating
        int totalProgress = clientsFinishedInWave + satisfiedInScene;

        // 3. If this total matches the wave goal, this is the final sequence
        bool isLast = totalProgress >= currentWave.clientsInWave.Count;

        // Debug to track the logic in the console
        if (isLast) Debug.Log("<color=gold>Final Client Detected!</color>");

        return isLast;
    }

    IEnumerator SpawnWave(int index)
    {
        if (index >= levelConfig.waves.Count)
        {
            Debug.Log("ALL WAVES CLEAR! Level Won.");

            StartCoroutine(FinishLevelRoutine());

            yield break;
        }

        if (index > 0)
        {
            Debug.Log($"Waiting 2.5s for Wave {index + 1}...");
            yield return new WaitForSeconds(0.2f);
            
        }

        Debug.Log($"--- Starting Wave {index + 1} --- Clients to spawn: {levelConfig.waves[index].clientsInWave.Count}");
        clientsFinishedInWave = 0;
        var wave = levelConfig.waves[index];

        currentWord = "";
        currentIndex = 0;


        foreach (var clientData in wave.clientsInWave)
        {
            // If the restaurant is full, wait until a seat opens up
            while (activeClients.Count >= seatingPositions.Count)
            {
                Debug.Log($"Waiting for seat: ActiveClients({activeClients.Count}) >= SeatingPositions({seatingPositions.Count})");
                yield return null;
            }

            
            SpawnClient(clientData);
            yield return new WaitForSeconds(0.1f);
        }
    }

    public char GetCurrentLetter()
    {
        if (currentIndex < currentWord.Length)
            return char.ToUpper(currentWord[currentIndex]);

        else // Return random letter
            return (char)('A' + Random.Range(0, 26));
    }

    public void IncreaseLetterIndex()
    {
        currentIndex++;
        Debug.Log($"Letter Index Increased: {currentIndex}/{currentWord.Length}");
    }

    void SpawnClient(LevelConfiguration.ClientData data)
    {
        var emptySeats = seatingPositions.Where(s => !activeClients.ContainsKey(s)).ToList();

        if (emptySeats.Count == 0)
        {
            Debug.LogWarning("No seats available to spawn client! Dictionary thinks all seats are occupied.");
            return;
        }

        currentWord += data.customLetter;

        Transform chosenSeat = emptySeats[Random.Range(0, emptySeats.Count)];
        GameObject newClientObj = Instantiate(clientPrefab, chosenSeat.position, chosenSeat.rotation);

        Client clientScript = newClientObj.GetComponent<Client>();
        clientScript.Initialize(chosenSeat);
        activeClients.Add(chosenSeat, clientScript);

        Debug.Log($"Spawned client at {chosenSeat.name}. Current Active Count: {activeClients.Count}");

        JamFlavor finalFlavor = data.jamFlavor;
        if (finalFlavor == JamFlavor.Random)
        {
            finalFlavor = JamDecider.Instance.activeJams[Random.Range(0, JamDecider.Instance.activeJams.Count)].flavor;
        }

        clientScript.SetOrder(finalFlavor.ToString(), JamDecider.Instance.GetColorFromFlavor(finalFlavor));
    }

    public void OnClientFinished()
    {
        clientsFinishedInWave++;
        totalClientsSatisfied++; // Increment the overall total

        UpdateUI();

        Debug.Log($"Client Finished! Progress in Wave: {clientsFinishedInWave}/{levelConfig.waves[currentWaveIndex].clientsInWave.Count}");

        if (clientsFinishedInWave >= levelConfig.waves[currentWaveIndex].clientsInWave.Count)
        {
            Debug.Log($"Wave {currentWaveIndex + 1} cleared. Moving to next index.");
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

        SaveProgression(levelConfig.levelNumber, stars, totalTime);

        Debug.Log($"<color=green>Level Complete!</color> Time: {totalTime}s, Stars: {stars}");
        // Trigger win menu ui
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

    private void SaveProgression(int levelID, int stars, float time)
    {
        float oldTime = PlayerPrefs.GetFloat($"Level_{levelID}_Time", Mathf.Infinity);
        if (time < oldTime)
        {
            PlayerPrefs.SetInt($"Level_{levelID}_Stars", stars);
            PlayerPrefs.SetFloat($"Level_{levelID}_Time", time);
            PlayerPrefs.Save();
        }
    }

    public Transform GetBestTarget(string currentJamInHand)
    {
        foreach (var kvp in activeClients)
        {
            Debug.Log($"Checking client at {kvp.Key.name} with {currentJamInHand} in Hand: Desired={kvp.Value.desiredCondiment}, Satisfied={kvp.Value.isSatisfied}");
        }

        var matchingKvp = activeClients.FirstOrDefault(kvp =>
            kvp.Value.desiredCondiment == currentJamInHand && !kvp.Value.isSatisfied);

        if (matchingKvp.Value != null)
        {
            matchingKvp.Value.OpenMouth();
            return matchingKvp.Value.transform;
        }

        if (activeClients.Count > 0)
        {
            int randomIndex = Random.Range(0, activeClients.Count);
            return activeClients.Keys.ElementAt(randomIndex);
        }

        return seatingPositions[Random.Range(0, seatingPositions.Count)];
    }

    public void ClearSeat(Transform seat)
    {
        if (seat == null)
        {
            Debug.LogWarning("ClearSeat called with null transform.");
            return;
        }

        if (activeClients.ContainsKey(seat))
        {
            activeClients.Remove(seat);
            Debug.Log($"Seat {seat.name} removed from dictionary. Remaining active: {activeClients.Count}");

            // Double check if this was the last client of the last wave
            if (currentWaveIndex >= levelConfig.waves.Count && activeClients.Count == 0 && !levelFinished)
            {
                StartCoroutine(FinishLevelRoutine());
            }
        }
        else
        {
            Debug.LogWarning($"Attempted to clear seat {seat.name}, but it wasn't in the activeClients dictionary!");
        }
    }
}