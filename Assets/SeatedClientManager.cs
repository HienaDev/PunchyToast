using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SeatedClientManager : MonoBehaviour
{
    public static SeatedClientManager Instance;

    [Header("Prefabs & Paths")]
    public GameObject clientPrefab;
    public List<TransformPath> possiblePaths;

    [Header("Seat Management")]
    public List<Transform> allSeats;
    private List<Transform> availableSeats = new List<Transform>();

    [Header("Spawning Settings")]
    public float minSpawnDelay = 2f;
    public float maxSpawnDelay = 8f;

    [Header("Client Randomization Ranges")]
    public Vector2 speedRange = new Vector2(2f, 4f);
    public Vector2 sessionDurationRange = new Vector2(10f, 30f);
    public Vector2 mouthRotationXRange = new Vector2(15f, 75f);
    [Range(0f, 1f)] public float talkChance = 0.5f;

    void Awake()
    {
        Instance = this;
        availableSeats.AddRange(allSeats);
    }

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));

            Transform seat = GetRandomAvailableSeat();
            if (seat != null && possiblePaths.Count > 0)
            {
                SpawnClient(seat);
            }
        }
    }

    void SpawnClient(Transform seat)
    {
        Transform[] path = possiblePaths[Random.Range(0, possiblePaths.Count)].points;

        GameObject go = Instantiate(clientPrefab, path[0].position, Quaternion.identity);
        ClientPuppet puppet = go.GetComponent<ClientPuppet>();

        float speed = Random.Range(speedRange.x, speedRange.y);
        float duration = Random.Range(sessionDurationRange.x, sessionDurationRange.y);
        float mouthRot = Random.Range(mouthRotationXRange.x, mouthRotationXRange.y);

        puppet.Initialize(path, seat, speed, duration, mouthRot, talkChance);
    }

    public Transform GetRandomAvailableSeat()
    {
        if (availableSeats.Count == 0) return null;
        int index = Random.Range(0, availableSeats.Count);
        Transform seat = availableSeats[index];
        availableSeats.RemoveAt(index);
        return seat;
    }

    public void ReleaseSeat(Transform seat)
    {
        if (!availableSeats.Contains(seat)) availableSeats.Add(seat);
    }
}

[System.Serializable]
public class TransformPath
{
    public Transform[] points;
}