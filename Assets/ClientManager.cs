using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ClientManager : MonoBehaviour
{
    public static ClientManager Instance;

    public GameObject clientPrefab;
    public List<Transform> seatingPositions;

    // We store the active clients mapped to their seat
    private Dictionary<Transform, Client> activeClients = new Dictionary<Transform, Client>();

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Update()
    {
        // Simple timer logic to spawn a client every few seconds if a seat is free
        if (Time.frameCount % 300 == 0 && activeClients.Count < seatingPositions.Count)
        {
            SpawnClient();
        }
    }

    void SpawnClient()
    {
        // Find empty seats
        var emptySeats = seatingPositions.Where(s => !activeClients.ContainsKey(s)).ToList();
        if (emptySeats.Count == 0) return;

        Transform chosenSeat = emptySeats[Random.Range(0, emptySeats.Count)];
        GameObject newClientObj = Instantiate(clientPrefab, chosenSeat.position, chosenSeat.rotation);

        Client clientScript = newClientObj.GetComponent<Client>();

        // --- ASSIGN RANDOM JAM FROM JAMDECIDER ---
        string randomJamName = GetRandomJamFromDecider();
        clientScript.SetOrder(randomJamName, JamDecider.Instance.GetColorFromJam(randomJamName));

        activeClients.Add(chosenSeat, clientScript);
    }

    string GetRandomJamFromDecider()
    {
        if (JamDecider.Instance == null || JamDecider.Instance.jams.Count == 0) return "None";

        // Create a pool of options: All jams + "None" (Plain Toast)
        int randomIndex = Random.Range(0, JamDecider.Instance.jams.Count);


        return JamDecider.Instance.jams[randomIndex].name;
    }


    public Transform GetBestTarget(string currentJamInHand)
    {
        // 1. Prioritize clients who want exactly what we have, sorted by least patience
        var matchingClients = activeClients.Values
            .Where(c => c.desiredCondiment == currentJamInHand && !c.isSatisfied)
            .OrderBy(c => c.currentPatience)
            .ToList();

        if (matchingClients.Count > 0)
        {
            return matchingClients[0].transform; // The seat
        }

        // 2. Fallback: If no one wants it, pick a random active client (they won't open mouth)
        if (activeClients.Count > 0)
        {
            return activeClients.Keys.ElementAt(Random.Range(0, activeClients.Count));
        }

        // 3. Absolute Fallback: Random target from the main list if no clients exist
        return seatingPositions[Random.Range(0, seatingPositions.Count)];
    }

    public void ClearSeat(Transform seat) => activeClients.Remove(seat);
}