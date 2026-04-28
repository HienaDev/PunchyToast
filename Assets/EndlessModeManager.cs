using UnityEngine;
using System.Collections.Generic;

public class EndlessModeManager : MonoBehaviour
{
    public static EndlessModeManager Instance;

    [Header("Easy Mode")]
    public bool easyMode = false;
    public float easyModeHoverTime = 10f;

    [Header("Difficulty Thresholds (clients spawned)")]
    public int tier2At = 5;
    public int tier3At = 12;
    public int tier4At = 22;

    [Header("Difficulty Curve - Hover Time")]
    public float startingHoverTime = 4f;
    public float minimumHoverTime = 0.6f;
    public float hoverTimeDecreasePerClient = 0.05f;

    [Header("Difficulty Curve - Slappable Chance")]
    public float slappableChanceStart = 0f;
    public float slappableChanceMax = 0.25f;

    [Header("Client Spawn Cooldown")]
    public float normalCooldown = 3f;
    public float minCooldown = 0.2f;
    public float cooldownDecreasePerClient = 0.05f;
    public float emptyCooldown = 0.8f;
    private float currentCooldown;

    [Header("Endless Extra Logic")]
    [Range(0f, 1f)] public float instantDoubleSpawnChance = 0.2f; // 20% chance to spawn another immediately

    [Header("Slap Words")]
    public string[] possibleSlapWords = { "SLAP", "POW", "BAM", "ZAP" };

    [Header("Jam Unlock Order")]
    public List<JamFlavor> jamUnlockOrder = new List<JamFlavor>();

    public bool isRunning = false;
    private int clientsSpawned = 0;
    private float justSpawnedClient = -Mathf.Infinity;
    // Tracks whether we have spawned at least one client this session,
    // so the empty cooldown only kicks in after the first client leaves,
    // not on startup (where we want to spawn immediately)
    private bool hasEverSpawned = false;

    void Awake() { if (Instance == null) Instance = this; }

    public void StartEndlessMode()
    {
        isRunning = true;
        clientsSpawned = 0;
        hasEverSpawned = false;

        SetupJamsForTier();
        ClientManager.Instance.levelConfig = null;
        ClientManager.Instance.ResetWordState();
    }

    public void StopEndlessMode()
    {
        isRunning = false;
    }

    void Update()
    {
        if (!isRunning) return;

        if (ClientManager.Instance.areThereActiveClients)
            currentCooldown = normalCooldown;
        else
            currentCooldown = Mathf.Min(normalCooldown, emptyCooldown);

        if (justSpawnedClient + currentCooldown > Time.time) return;
        if (!ClientManager.Instance.HasAvailableSeat()) return;

        // Only block on "are there clients" AFTER the first spawn,
        // and only to apply the correct cooldown — not to gate spawning entirely.
        // The actual gate is: did enough time pass?
        // That time was already set correctly when we scheduled justSpawnedClient.

        SetupJamsForTier();
        SpawnEndlessClient();
    }

    private float GetCurrentHoverTime()
    {
        if (easyMode) return easyModeHoverTime;
        float t = startingHoverTime - (clientsSpawned * hoverTimeDecreasePerClient);
        return Mathf.Max(t, minimumHoverTime);
    }

    private float GetCurrentSlappableChance()
    {
        if (easyMode) return 0f;
        return Mathf.Lerp(slappableChanceStart, slappableChanceMax,
                          Mathf.Clamp01((float)clientsSpawned / tier4At));
    }

    private void SetupJamsForTier()
    {
        int jamCount;
        if (clientsSpawned >= tier4At) jamCount = 4;
        else if (clientsSpawned >= tier3At) jamCount = 3;
        else if (clientsSpawned >= tier2At) jamCount = 2;
        else jamCount = 1;

        jamCount = Mathf.Min(jamCount, jamUnlockOrder.Count);

        HashSet<JamFlavor> flavors = new HashSet<JamFlavor>();
        for (int i = 0; i < jamCount; i++)
            flavors.Add(jamUnlockOrder[i]);

        JamDecider.Instance.SetupLevelJams(flavors);
    }

    private void SpawnEndlessClient()
    {
        var activeJams = JamDecider.Instance.activeJams;
        bool makeSlappable = Random.value < GetCurrentSlappableChance();

        LevelConfiguration.ClientData data = new LevelConfiguration.ClientData(true);
        data.jamFlavor = activeJams[Random.Range(0, activeJams.Count)].flavor;
        data.simultaneousToast = false;
        data.isSlappable = makeSlappable;
        data.toastsNeeded = 1;

        if (makeSlappable)
        {
            data.slapString = possibleSlapWords[Random.Range(0, possibleSlapWords.Length)];
            data.customLetter = "";
        }
        else
        {
            data.customLetter = ((char)('A' + Random.Range(0, 26))).ToString();
        }

        Toaster.Instance.hoverTime = GetCurrentHoverTime();
        ClientManager.Instance.SpawnEndlessClient(data);
        clientsSpawned++;
        hasEverSpawned = true;

        normalCooldown -= cooldownDecreasePerClient;
        normalCooldown = Mathf.Max(normalCooldown, minCooldown);

        justSpawnedClient = Time.time;

        // --- NEW LOGIC: Instant Double Spawn Chance ---
        if (Random.value < instantDoubleSpawnChance && ClientManager.Instance.HasAvailableSeat())
        {
            // We call this again. Because justSpawnedClient is already updated, 
            // the Update loop won't trigger a third one until the cooldown passes.
            SpawnEndlessClient();
        }
    }
}