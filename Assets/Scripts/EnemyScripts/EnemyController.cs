using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Enemy Reference")]
    [SerializeField] float enemyWanderSpeed = 50f;
    [SerializeField] float enemyChaseSpeed = 20f;
    [SerializeField] float stopDistance = 2f;
    public float direction = 1f;
    public float wanderRadius = 20f;

    [Header("Player Reference")]
    public Transform player;
    public float detectionRadius = 20f;

    private Vector3 wanderTarget;
    private bool playerDetected = false;

    [Header("Attack")]
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float attackCooldown = 1f;

    private float attackTimer = 0f;
    private PlayerManager playerManager;

    //[Header("Spawn Area Bounds")] 
    //public float areaBoundMinX = 600f; 
    //public float areaBoundMaxX = 700f; 
    //public float areaBoundMinZ = 490f; 
    //public float areaBoundMaxZ = 570f; 
    //public float areaBoundY = 1f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Allows the enemy to automatically find the player
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        playerManager = player.GetComponent<PlayerManager>();

        ChooseTarget();
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
            //RespawnEnemy();
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

    //void RespawnEnemy() 
    //{ 
    //    Vector3 spawnPosition; 
    //    int spawnAttempts = 0; 

    //    do 
    //    { 
    //        float randomPosX = Random.Range(areaBoundMinX, areaBoundMaxX); 
    //        float randomPosZ = Random.Range(areaBoundMinZ, areaBoundMaxZ); 

    //        spawnPosition = new Vector3(randomPosX, areaBoundY, randomPosZ); 
    //        spawnAttempts++; 
    //    } while (
    //        Vector3.Distance(spawnPosition, player.position) < detectionRadius * 2 && spawnAttempts < 50
    //    ); 

    //    transform.position = spawnPosition;

    //    ChooseTarget();
    //}
  
}
