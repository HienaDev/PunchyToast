using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EndlessModeManager : MonoBehaviour
{
    public static EndlessModeManager Instance;

    [Header("Difficulty Curve")]
    public float startingHoverTime = 4f;
    public float minimumHoverTime = 0.6f;
    public float hoverTimeDecreasePerClient = 0.05f;

    [Header("Client Spawn Cooldown")]
    public float normalCooldown = 3f;
    public float emptyCooldown = 0.8f; // Used when no clients are seated

    [Header("Simultaneous Chance")]
    [Range(0f, 1f)] public float simultaneousChance = 0.2f;

    [Header("Slappable Chance")]
    [Range(0f, 1f)] public float slappableChance = 0.15f;
    public string[] possibleSlapWords = { "SLAP", "POW", "BAM", "ZAP" };

    public bool isRunning = false;
    private int clientsSpawned = 0;

    void Awake() { if (Instance == null) Instance = this; }

    public void StartEndlessMode()
    {
        isRunning = true;
        clientsSpawned = 0;

        // Activate all available jams for endless mode
        HashSet<JamFlavor> allFlavors = new HashSet<JamFlavor>();
        foreach (var jam in JamDecider.Instance.allAvailableJams)
        {
            if (jam.flavor != JamFlavor.None && jam.flavor != JamFlavor.Random)
                allFlavors.Add(jam.flavor);
        }
        JamDecider.Instance.SetupLevelJams(allFlavors);

        ClientManager.Instance.levelConfig = null;
        ClientManager.Instance.ResetWordState();

        StartCoroutine(EndlessSpawnLoop());
    }

    public void StopEndlessMode()
    {
        isRunning = false;
        StopAllCoroutines();
    }

    public float GetCurrentHoverTime()
    {
        float t = startingHoverTime - (clientsSpawned * hoverTimeDecreasePerClient);
        return Mathf.Max(t, minimumHoverTime);
    }

    private IEnumerator EndlessSpawnLoop()
    {
        while (isRunning)
        {
            // If seats are full, just wait a short tick
            bool seatAvailable = ClientManager.Instance.HasAvailableSeat();
            if (!seatAvailable)
            {
                yield return new WaitForSeconds(0.2f);
                continue;
            }

            SpawnEndlessClient();

            // Wait shorter if scene is empty so player isn't staring at nothing
            float cooldown = ClientManager.Instance.areThereClients ? normalCooldown : emptyCooldown;
            yield return new WaitForSeconds(cooldown);
        }
    }

    private void SpawnEndlessClient()
    {
        // Build a ClientData
        LevelConfiguration.ClientData data = new LevelConfiguration.ClientData(true);

        // Random jam
        var allJams = JamDecider.Instance.activeJams;
        data.jamFlavor = allJams[Random.Range(0, allJams.Count)].flavor;

        // Simultaneous?
        bool makeSimultaneous = Random.value < simultaneousChance;
        data.simultaneousToast = makeSimultaneous;

        // Slappable? (not simultaneous — keep it simple)
        bool makeSlappable = !makeSimultaneous && Random.value < slappableChance;
        data.isSlappable = makeSlappable;

        if (makeSlappable)
        {
            data.slapString = possibleSlapWords[Random.Range(0, possibleSlapWords.Length)];
            data.customLetter = ""; // unused when slappable
        }
        else
        {
            data.customLetter = ((char)('A' + Random.Range(0, 26))).ToString();
        }

        data.toastsNeeded = 1;

        // Update hover time on the Toaster
        Toaster.Instance.hoverTime = GetCurrentHoverTime();

        // Inject into ClientManager
        ClientManager.Instance.SpawnEndlessClient(data);
        clientsSpawned++;

        // If simultaneous, immediately queue a follow-up non-simultaneous client
        if (makeSimultaneous)
        {
            LevelConfiguration.ClientData followUp = new LevelConfiguration.ClientData(true);
            followUp.jamFlavor = allJams[Random.Range(0, allJams.Count)].flavor;
            followUp.simultaneousToast = false;
            followUp.isSlappable = false;
            followUp.customLetter = ((char)('A' + Random.Range(0, 26))).ToString();
            followUp.toastsNeeded = 1;
            ClientManager.Instance.SpawnEndlessClient(followUp);
            clientsSpawned++;
        }
    }
}