// File: IItemContainer.cs
public interface IItemContainer
{
    int SlotCount { get; }
    ItemStack GetSlot(int index);
    void SetSlot(int index, ItemStack stack);

    // Optional: for debugging/UI
    string ContainerId { get; }
}