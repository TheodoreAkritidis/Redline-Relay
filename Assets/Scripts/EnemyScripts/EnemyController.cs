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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
        } else if (distanceToPlayer > detectionRadius)
        {
            playerDetected = false;
        }

        if (playerDetected)
        {
            ChasePlayer();
        } else
        {
            Wander();
        }

        //Vector3 direction = (player.position - transform.position).normalized;
        
        //transform.position += direction * enemySpeed * Time.deltaTime;
        //transform.LookAt(player);
    }

    void ChooseTarget()
    {
        Vector3 randomDir = Random.insideUnitSphere * wanderRadius;
        randomDir.y = 0;
        wanderTarget = transform.position + randomDir;
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

}
