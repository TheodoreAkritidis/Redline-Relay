using System;
using UnityEngine;

public sealed class PlayerInventoryComponent : MonoBehaviour
{
    [Header("Sizes")]
    [SerializeField] private int backpackSlots = 30;

    [Header("Debug Seed (optional)")]
    [SerializeField] private ItemDefinition wood;
    [SerializeField] private ItemDefinition stone;

    public PlayerInventoryModel Model { get; private set; }

    // 0..9
    public int SelectedHotbarIndex { get; private set; }

    public event Action InventoryChanged;
    public event Action<int> HotbarSelectionChanged;

    private void Awake()
    {
        Model = new PlayerInventoryModel(hotbarSlots: 10, backpackSlots: backpackSlots);
        SelectedHotbarIndex = 0;

        // Optional seed so you can see things immediately
        if (wood != null) Model.Hotbar.SetSlot(0, new ItemStack(wood, 12));
        if (wood != null) Model.Backpack.SetSlot(0, new ItemStack(wood, 20));
        if (stone != null) Model.Backpack.SetSlot(1, new ItemStack(stone, 7));
    }

    public void NotifyInventoryChanged()
    {
        InventoryChanged?.Invoke();
    }

    public void SetSelectedHotbarIndex(int index)
    {
        index = Mathf.Clamp(index, 0, 9);
        if (SelectedHotbarIndex == index) return;

        SelectedHotbarIndex = index;
        HotbarSelectionChanged?.Invoke(index);
    }

    public ItemStack GetSelectedHotbarStack()
    {
        return Model.Hotbar.GetSlot(SelectedHotbarIndex);
    }
}
