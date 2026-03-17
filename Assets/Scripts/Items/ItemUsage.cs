using UnityEngine;
using UnityEngine.InputSystem;

public class ItemUsage : MonoBehaviour
{
    [SerializeField] private PlayerInventoryComponent Inv;
    [SerializeField] private PlayerManager Player;

    public bool useBlocked = false;

    private void Awake()
    {
        if (Inv == null)
        {
            Inv = GetComponent<PlayerInventoryComponent>();
        }

        if (Player == null)
        {
            Player = GetComponent<PlayerManager>();
        }
    }

    public void OnUse(InputValue v)
    {
        if (useBlocked)
        {
            return;
        }

        ItemDefinition item = Inv.GetSelectedHotbarItem();
        ItemStack stack = Inv.GetSelectedHotbarStack();
        bool consumed = false;

        if (item == null)
        {
            return;
        }

        if (item.IsFood)
        {
            consumed = Player.TryEat(item.FoodValue);
        }

        if (item.IsWater)
        {
            consumed = Player.TryDrink(item.WaterValue);
        }

        if (item is CanteenItem canteen)
        {
            Debug.Log($"Capacity: {stack.CanteenCapacity} / {canteen.MaxCapacity}");
            DrinkCanteen(stack, canteen);
            Debug.Log("Drank from Canteen");
            Debug.Log($"Capacity: {stack.CanteenCapacity} / {canteen.MaxCapacity}");
        }

        if (item.AppliesStatus)
        {
            Player.TryApplyStatus(item.Status);
        }

        if (item.DestroyOnUse && consumed)
        {
            Inv.ConsumeSelectedHotbarItem();
            Inv.NotifyInventoryChanged();
        }
    }

    // Drink from canteen if not empty, and remove consumed amount.
    public void DrinkCanteen( ItemStack stack, CanteenItem item )
    {
        if ( stack.CanteenCapacity <= 0 )
        {
            return;
        }

        Player.TryDrink(item.ConsumeAmount);
        stack.CanteenCapacity = Mathf.Max(0, stack.CanteenCapacity - item.ConsumeAmount);
        Inv.SetSelectedHotbarStack(stack);
    }

}