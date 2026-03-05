using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class EnemyManager : MonoBehaviour
{
    // Enemy reference   
    public GameObject _enemyPrefab;
    public float spawnRate = 5f;

    // Spawn area bounds
    public float areaBoundMinX = 600f;
    public float areaBoundMaxX = 700f;
    public float areaBoundMinZ = 490f;
    public float areaBoundMaxZ = 570f;
    public float areaBoundY = 1f;

    // Player reference 
    public Transform player;
    public float minSpawnDistanceFromPlayer = 5000f;

    void Awake()
    {
        Debug.Log("EnemyManager Awake called");
        StartCoroutine(SpawnEnemyCoroutine());
    }

    void SpawnEnemy()
    {
        Vector3 spawnPosition;

        int spawnAttempts = 0;
        do
        {
            float randomPosX = Random.Range(areaBoundMinX, areaBoundMaxX);
            float randomPosZ = Random.Range(areaBoundMinZ, areaBoundMaxZ);

            spawnPosition = new Vector3(randomPosX, areaBoundY, randomPosZ);
            spawnAttempts++;
        } while (Vector3.Distance(spawnPosition, player.position) < minSpawnDistanceFromPlayer && spawnAttempts < 100);

        Debug.Log($"Spawning enemy at {spawnPosition}");

        GameObject gameObject = Instantiate(_enemyPrefab, spawnPosition, Quaternion.identity);
        
        EnemyController enemyController = gameObject.GetComponent<EnemyController>();
        enemyController.player = player;
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
