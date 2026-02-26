using UnityEngine;

public sealed class WaterSource : MonoBehaviour, IInteractable, IToolGatedInteractable
{
    [SerializeField] private Canteen Canteen;
    [SerializeField] private ItemDefinition CanteenItem;
    [SerializeField] private float FillValue;
    
    public string GetPrompt()
    {
        float CurrentCanteenValue = Canteen.GetCurrentLevel();
        return $"Fill Canteen ({CurrentCanteenValue})";
    }

    public void Interact(GameObject interactor)
    {
        if (!CanInteractWith(interactor, out _))
            return;

        if ((interactor != null) && (Canteen != null))
        {
            Canteen.FillCanteen(25);
        }
    }

    public bool CanInteractWith(GameObject interactor, out string blockedPrompt)
    {
        blockedPrompt = "";

        var inv = interactor.GetComponent<PlayerInventoryComponent>();
        if (inv == null || inv.Model == null || inv.Model.Hotbar == null)
        {
            blockedPrompt = "Canteen required";
            return false;
        }

        int idx = inv.SelectedHotbarIndex;
        if (idx < 0 || idx >= inv.Model.Hotbar.SlotCount)
        {
            blockedPrompt = "Canteen required";
            return false;
        }

        ItemStack s = inv.Model.Hotbar.GetSlot(idx);
        bool canteenSelected = !s.IsEmpty && s.Item == CanteenItem;

        if (!canteenSelected)
        {
            blockedPrompt = "Canteen required";
            return false;
        }

        return true;
    }
    
}