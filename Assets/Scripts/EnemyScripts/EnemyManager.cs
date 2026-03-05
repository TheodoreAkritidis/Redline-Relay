using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class EnemyManager : MonoBehaviour
{
    public GameObject _enemyPrefab; // Enemy in the prefab
    public float spawnRate = 5f;

    // Spawn area bounds
    public float areaBoundMinX = 500f;
    public float areaBoundMaxX = 740f;
    public float areaBoundMinZ = 400f;
    public float areaBoundMaxZ = 600f;
    public float areaBoundY = 1f;

    void Awake()
    {
        Debug.Log("EnemyManager Awake called");
        StartCoroutine(SpawnEnemyCoroutine());
    }

    void SpawnEnemy()
    {
        float randomPosX = Random.Range(areaBoundMinX, areaBoundMaxX);
        float randomPosZ = Random.Range(areaBoundMinZ, areaBoundMaxZ);

        Vector3 spawnPosition = new Vector3(randomPosX, areaBoundY, randomPosZ);

        Debug.Log($"Spawning enemy at {spawnPosition}");

        GameObject gameObject = Instantiate(_enemyPrefab, spawnPosition, Quaternion.identity);
    }

    IEnumerator SpawnEnemyCoroutine()
    {
        Debug.Log("Coroutine started");

        while (true)
        {
            Debug.Log("Waiting...");
            yield return new WaitForSeconds(spawnRate);

            Debug.Log("Calling SpawnEnemy()");
            SpawnEnemy();
        }

    }
}
