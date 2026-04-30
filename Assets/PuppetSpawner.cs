using UnityEngine;
using System.Collections;

public class PuppetSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject puppetPrefab;
    [SerializeField] private float minSpawnDelay = 2.0f;
    [SerializeField] private float maxSpawnDelay = 8.0f;

    [Header("Spawn State")]
    [SerializeField] private bool isSpawning = true;

    private void OnEnable()
    {
        isSpawning = true;
        StartCoroutine(SpawnRoutine());
    }

    private void OnDisable()
    {
        isSpawning = false;
        StopAllCoroutines();
    }

    private IEnumerator SpawnRoutine()
    {
        // Wait a small amount of time before the very first spawn 
        // so they don't all appear instantly when the scene starts
        yield return new WaitForSeconds(Random.Range(0.5f, minSpawnDelay));

        while (isSpawning)
        {
            SpawnPuppet();

            // Calculate a random wait time for the next one
            float nextDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(nextDelay);
        }
    }

    private void SpawnPuppet()
    {
        if (puppetPrefab == null)
        {
            return;
        }

        // Spawn the prefab at the spawner's current position and rotation
        Instantiate(puppetPrefab, transform.position, transform.rotation);
    }
}