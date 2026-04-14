using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class ClientManager : MonoBehaviour
{
    public static ClientManager Instance;

    [Header("Level Configuration")]
    public LevelConfiguration levelConfig;
    private int currentWaveIndex = 0;
    private int clientsFinishedInWave = 0;

    [Header("Setup")]
    public GameObject clientPrefab;
    public List<Transform> seatingPositions; // MAKE SURE THIS HAS AT LEAST 5 SLOTS!
    private Dictionary<Transform, Client> activeClients = new Dictionary<Transform, Client>();

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        Debug.Log($"ClientManager started. Total seating positions assigned: {seatingPositions.Count}");
        if (levelConfig != null)
        {
            ConfigureJamsForLevel();
            StartCoroutine(SpawnWave(currentWaveIndex));
        }
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
            yield break;
        }

        if (index > 0)
        {
            Debug.Log($"Waiting 2.5s for Wave {index + 1}...");
            yield return new WaitForSeconds(1f);
        }

        Debug.Log($"--- Starting Wave {index + 1} --- Clients to spawn: {levelConfig.waves[index].clientsInWave.Count}");
        clientsFinishedInWave = 0;
        var wave = levelConfig.waves[index];

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

    void SpawnClient(LevelConfiguration.ClientData data)
    {
        var emptySeats = seatingPositions.Where(s => !activeClients.ContainsKey(s)).ToList();

        if (emptySeats.Count == 0)
        {
            Debug.LogWarning("No seats available to spawn client! Dictionary thinks all seats are occupied.");
            return;
        }

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
        Debug.Log($"Client Finished! Progress in Wave: {clientsFinishedInWave}/{levelConfig.waves[currentWaveIndex].clientsInWave.Count}");

        if (clientsFinishedInWave >= levelConfig.waves[currentWaveIndex].clientsInWave.Count)
        {
            Debug.Log($"Wave {currentWaveIndex + 1} cleared. Moving to next index.");
            currentWaveIndex++;
            StartCoroutine(SpawnWave(currentWaveIndex));
        }
    }

    public Transform GetBestTarget(string currentJamInHand)
    {
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
        }
        else
        {
            Debug.LogWarning($"Attempted to clear seat {seat.name}, but it wasn't in the activeClients dictionary!");
        }
    }
}