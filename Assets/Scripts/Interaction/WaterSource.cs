using UnityEngine;

public sealed class WaterSource : MonoBehaviour, IInteractable
{
    [SerializeField] private float WaterValue;
    [SerializeField] private PlayerInventoryComponent Inv;
    
    void Awake( )
    {
        if ( Inv == null )
        {
            Inv = FindFirstObjectByType<PlayerInventoryComponent>();
        }
    }

    public string GetPrompt()
    {
        ItemDefinition item = Inv.GetSelectedHotbarItem();
        if ( item is CanteenItem canteen )
        {
            return "Fill Canteen";
        }

        return "Drink Water";
    }

    public void Interact( GameObject interactor )
    {
        if ( interactor != null )
        {
            var player = interactor.GetComponent<PlayerManager>();

            if (player == null)
            {
                return;
            }

            ItemDefinition item = Inv.GetSelectedHotbarItem();

            if ( item is CanteenItem canteen )
            {
                ItemStack stack = Inv.GetSelectedHotbarStack();

                if ( stack.CanteenCapacity >= canteen.MaxCapacity - 1)
                {
                    Debug.Log("Canteen Full");
                    Debug.Log($"Capacity: {stack.CanteenCapacity} / {canteen.MaxCapacity}");
                    return;
                }

                stack.CanteenCapacity = Mathf.Min(canteen.MaxCapacity, stack.CanteenCapacity + canteen.FillAmount);
                Inv.SetSelectedHotbarStack(stack);
                Debug.Log("Filled Canteen");
                Debug.Log($"Capacity: {stack.CanteenCapacity} / {canteen.MaxCapacity}");
                return;
            }

            player.TryDrink(WaterValue);
        }
    }
}