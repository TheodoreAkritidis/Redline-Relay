using UnityEngine;
using UnityEngine.InputSystem;

public class ItemUsage : MonoBehaviour
{
    [SerializeField] private PlayerInventoryComponent inv;
    [SerializeField] private PlayerManager player;

    public bool useBlocked = false;

    [SerializeField] private float attackRange;
    [SerializeField] private LayerMask enemyLayer;

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
        // Only handle press events from the Input System
        if (!v.isPressed) return;
        DoUse();
    }

    private void Update()
    {
        // Support legacy input (mouse left button and Enter) in case Input System isn't
        // sending messages or the player expects these keys to work.
        if (useBlocked) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            DoUse();
        }
    }

    private void DoUse()
    {
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

    private void Attack(float weaponDamage)
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, attackRange, enemyLayer))
        {
            EnemyController enemy = hit.collider.GetComponent<EnemyController>();

            if (enemy != null)
            {
                enemy.TakeDamage(weaponDamage);
            }
        }
    }
}