using UnityEngine;
using UnityEngine.InputSystem;

public sealed class ItemUsage : MonoBehaviour
{
    [SerializeField] private PlayerInventoryComponent inv;
    [SerializeField] private PlayerManager player;

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

        if (item.DestroyOnUse && consumed)
        {
            inv.UseSelectedHotbarItem();
            inv.NotifyInventoryChanged();
        }
    }
}