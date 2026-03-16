using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Reference")]
    [SerializeField] float enemyWanderSpeed = 5f;
    [SerializeField] float enemyChaseSpeed = 5f;
    [SerializeField] float stopDistance = 2f;
    public float direction = 1f;
    public float wanderRadius = 20f;
    private Vector3 spawnPosition;

    [Header("Player Reference")]
    public Transform player;
    public float detectionRadius = 20f;

    private Vector3 wanderTarget;
    private bool playerDetected = false;

    [Header("Attack")]
    public float attackRange = 2f;
    public float attackDamage = 5f;
    public float attackCooldown = 5f; // controls the amount of time before an enemy can attack again

    [Header("Health")]
    public float maxHealth = 20f;
    private float currentHealth;
    public float respawnDelay = 1f; // seconds before respawning after death

    private float attackTimer = 0f;
    private PlayerManager playerManager;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Allows the enemy to automatically find the player
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        if (player != null)
            playerManager = player.GetComponent<PlayerManager>();

        currentHealth = maxHealth;

        ChooseTarget();

        spawnPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)
        {
            playerDetected = true;
        } else {
            playerDetected = false;
        }

        if (playerDetected)
        {
            if (distanceToPlayer <= attackRange)
            {
                AttackPlayer();
            } else {
                ChasePlayer();
            }
        } else {
            Wander();
        }

        attackTimer -= Time.deltaTime;
    }

    void ChooseTarget()
    {
        Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
        randomDir.y = 0;
        wanderTarget = transform.position + randomDir;
    }

    void AttackPlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position);
        directionToPlayer.y = 0;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directionToPlayer), 0.2f);

        if (attackTimer <= 0f)
        {
            if (playerManager != null)
            {
                playerManager.health -= attackDamage;
                Debug.Log($"Enemy attacked player! Player health: {playerManager.health}");
            }

            attackTimer = attackCooldown;
        }
    }

    void ChasePlayer()
    {
        Vector3 directionToPlayer = (player.position - transform.position);
        float distanceToPlayer = directionToPlayer.magnitude;
        if (distanceToPlayer > stopDistance)
        {
            directionToPlayer.Normalize();
            transform.position += directionToPlayer * enemyChaseSpeed * Time.deltaTime;

            directionToPlayer.y = 0;
            if (directionToPlayer != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directionToPlayer), 0.2f);
            }
        }
    }

    void Wander()
    {
        float step = enemyWanderSpeed * Time.deltaTime;

        transform.position = Vector3.MoveTowards(transform.position, wanderTarget, step);

        Vector3 lookDirection = (wanderTarget - transform.position).normalized;
        if (lookDirection != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), 0.1f);

        if (Vector3.Distance(transform.position, wanderTarget) < 0.5f)
        {
            ChooseTarget();
        }
    }

    public void TakeDamage(float damage)
    {
        Debug.Log($"Enemy took {damage} damage!");

        currentHealth -= damage;
        if (currentHealth <= 0f)
        {
            StartCoroutine(HandleDeath());
        }
    }

    private IEnumerator HandleDeath()
    {
        Debug.Log("Enemy died, respawning...");

        gameObject.SetActive(false);

        yield return new WaitForSeconds(respawnDelay);

        // Moves the enemy back to original placement position
        transform.position = spawnPosition;
        currentHealth = maxHealth;
        ChooseTarget();

        gameObject.SetActive(true);
    }

    void RespawnEnemy()
    {
        transform.position = spawnPosition;
        ChooseTarget();
    }

}
