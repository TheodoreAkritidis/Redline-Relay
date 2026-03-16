using UnityEngine;
using UnityEngine.InputSystem;

public class ItemUsage : MonoBehaviour
{
    [SerializeField] private PlayerInventoryComponent inv;
    [SerializeField] private PlayerManager player;

    public bool useBlocked = false;

    [SerializeField] private float attackRange = 3f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Unarmed Attack")]
    [SerializeField] private bool allowUnarmedAttack = true;
    [SerializeField] private float unarmedDamage = 5f;
    [SerializeField] private Key attackKey = Key.F;

    private void Awake()
    {
        if (inv == null)
        {
            inv = GetComponent<PlayerInventoryComponent>();
        }

        if (player == null)
        {
            player = GetComponent<PlayerManager>();
        }
    }

    public void OnUse(InputValue v)
    {
        if (useBlocked)
        {
            return;
        }

        ItemDefinition item = inv.GetSelectedHotbarItem();
        bool consumed = false;

        if (item == null)
        {
            return;
        }

        if (item.IsFood)
        {
            consumed = player.TryEat(item.FoodValue);
        }

        if (item.IsWater)
        {
            consumed = player.TryDrink(item.WaterValue);
        }

        if (item.isWeapon)
        {
            Attack(item.WeaponValue);
        }

        if (item.DestroyOnUse && consumed)
        {
            inv.ConsumeSelectedHotbarItem();
            inv.NotifyInventoryChanged();
        }
    }

    // InputSystem handler for a dedicated attack action (optional)
    public void OnAttack(InputValue v)
    {
        if (!v.isPressed) return;
        DoAttack();
    }

    private void Update()
    {
        if (useBlocked) return;

        bool attackPressed = false;

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            attackPressed = true;

        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard[attackKey].wasPressedThisFrame)
            attackPressed = true;

        if (attackPressed)
        {
            DoAttack();
        }
    }

    private void DoAttack()
    {        
        ItemDefinition item = inv.GetSelectedHotbarItem();

        if (item != null && item.isWeapon)
        {
            Attack(item.WeaponValue);
            return;
        }

        // If the player doesn't have a weapon, allow unarmed attack if enabled
        if (allowUnarmedAttack)
        {
            Debug.Log($"ItemUsage: performing unarmed attack (damage={unarmedDamage})");
            Attack(unarmedDamage);
        }
    }

    private void Attack(float weaponDamage)
    {
        if (Camera.main == null)
        {
            return;
        }

        Vector3 origin = Camera.main.transform.position;
        Vector3 dir = Camera.main.transform.forward;

        Debug.DrawRay(origin, dir * attackRange, Color.red, 1f);

        Ray ray = new Ray(origin, dir);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, attackRange, enemyLayer, QueryTriggerInteraction.Collide))
        {
            Debug.Log($"ItemUsage.Attack: Attack hit '{hit.collider.name}' with damage={weaponDamage}");
            EnemyController enemy = hit.collider.GetComponent<EnemyController>() ?? hit.collider.GetComponentInParent<EnemyController>(); // supports colliders placed on child objects of the enemy prefab
            if (enemy != null)
            {
                enemy.TakeDamage(weaponDamage);
            }
        }
        else
        {
            if (Physics.Raycast(ray, out hit, attackRange))
            {
                Debug.Log($"ItemUsage.Attack: Fallback hit '{hit.collider.name}' with damage={weaponDamage}");
                EnemyController enemy2 = hit.collider.GetComponent<EnemyController>() ?? hit.collider.GetComponentInParent<EnemyController>();
                if (enemy2 != null)
                {
                    enemy2.TakeDamage(weaponDamage);
                    return;
                }
            }

            Debug.Log("ItemUsage.Attack: Attack missed");
        }
    }
}