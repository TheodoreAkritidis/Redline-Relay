using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class EnemyManager : MonoBehaviour
{
    [Header("Enemy Reference")]
    public GameObject _enemyPrefab;
    public float spawnRate = 5f;

    [Header("Spawn Area Bounds")]
    public float areaBoundMinX = 600f;
    public float areaBoundMaxX = 700f;
    public float areaBoundMinZ = 490f;
    public float areaBoundMaxZ = 570f;
    public float areaBoundY = 1f;

    [Header("Player Reference")]
    public Transform player;
    public float minSpawnDistanceFromPlayer = 5f;

    void Awake()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

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

            float fowardAppearance = Random.value;
            Vector3 forwardOffset = player.forward * Random.Range(5f, 15f) * fowardAppearance;

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
