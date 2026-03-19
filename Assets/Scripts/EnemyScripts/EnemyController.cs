using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour, IInteractable, IAttackable
{
    [Header("Enemy Reference")]
    [SerializeField] float enemyWanderSpeed = 5f;
    [SerializeField] float enemyChaseSpeed = 5f;
    [SerializeField] float stopDistance = 5f;
    public float direction = 1f;
    public float wanderRadius = 20f;
    private Vector3 spawnPosition;

    [Header("Player Reference")]
    public Transform player;
    public float detectionRadius = 20f;

    [Header("Grounding")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundSampleHeight = 50f;

    private Vector3 wanderTarget;
    private bool playerDetected = false;

    [Header("Attack")]
    public float attackRange = 5f;
    public float attackDamage = 5f;
    public float attackCooldown = 10f; // controls the amount of time before an enemy can attack again

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

        // Align spawn position to ground if possible
        Vector3 sampleOrigin = transform.position + Vector3.up * groundSampleHeight;

        RaycastHit groundHit;
        int groundMask = groundLayer.value == 0 ? ~0 : groundLayer.value;
        if (Physics.Raycast(sampleOrigin, Vector3.down, out groundHit, groundSampleHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            spawnPosition = groundHit.point;
            transform.position = spawnPosition;
        }
        else
        {
            spawnPosition = transform.position;
        }

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
        
        Vector3 randomDir = Random.insideUnitSphere * wanderRadius; // picks a random point on the ground within wanderRadius
        Vector3 candidate = transform.position + randomDir;
        Vector3 sampleOrigin = candidate + Vector3.up * groundSampleHeight; // sample ground height at candidate 

        RaycastHit hit;
        int groundMask = groundLayer.value == 0 ? ~0 : groundLayer.value;
        if (Physics.Raycast(sampleOrigin, Vector3.down, out hit, groundSampleHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            wanderTarget = hit.point;
        }
        else
        {
            candidate.y = transform.position.y;
            wanderTarget = candidate;
        }
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

            Vector3 targetPos = transform.position + directionToPlayer * enemyChaseSpeed * Time.deltaTime; // computes the target horizontal position
            Vector3 sampleOrigin = targetPos + Vector3.up * groundSampleHeight; // sample ground height at target 

            RaycastHit hit;
            int groundMask = groundLayer.value == 0 ? ~0 : groundLayer.value;
            if (Physics.Raycast(sampleOrigin, Vector3.down, out hit, groundSampleHeight * 2f, groundMask, QueryTriggerInteraction.Ignore))
            {
                targetPos.y = hit.point.y;
            }

            transform.position = targetPos;

            directionToPlayer.y = 0;
            if (directionToPlayer != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(directionToPlayer), 0.2f);
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
        
        transform.position = spawnPosition; // moves the enemy back to original placement position
        currentHealth = maxHealth;
        ChooseTarget();

        gameObject.SetActive(true);
    }

    void RespawnEnemy()
    {
        transform.position = spawnPosition;
        ChooseTarget();
    }

    // IInteractable
    public string GetPrompt()
    {
        return "Attack";
    }

    public void Interact(GameObject interactor)
    {
        // When the player presses the generic interact key while pointing at the enemy,
        // perform a simple attack using the player's currently selected hotbar item if available,
        // otherwise use a small default damage.
        if (interactor == null) return;

        var inv = interactor.GetComponent<PlayerInventoryComponent>();
        float damage = 5f; // default unarmed/interact damage

        if (inv != null)
        {
            var item = inv.GetSelectedHotbarItem();
            if (item != null && item.isWeapon)
            {
                damage = item.WeaponValue;
            }
        }

        TakeDamage(damage);
    }

    // IAttackable
    public string GetAttackPrompt()
    {
        // Popup message for when the player is aiming at the enemy
        return "E to Attack";
    }

}
