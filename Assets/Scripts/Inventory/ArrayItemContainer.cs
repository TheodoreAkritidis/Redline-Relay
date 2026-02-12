// File: ArrayItemContainer.cs
using System;
using UnityEngine;

[Serializable]
public sealed class ArrayItemContainer : IItemContainer
{
    [SerializeField] private string containerId;
    [SerializeField] private ItemStack[] slots;

    public string ContainerId => containerId;
    public int SlotCount => slots.Length;

    public ArrayItemContainer(string id, int slotCount)
    {
        containerId = id;
        slots = new ItemStack[slotCount];
    }

    public ItemStack GetSlot(int index)
    {
        ValidateIndex(index);
        return slots[index];
    }

    public void SetSlot(int index, ItemStack stack)
    {
        ValidateIndex(index);
        slots[index] = stack;
        if (slots[index].Quantity < 0) slots[index].Quantity = 0;
        if (slots[index].IsEmpty) slots[index].Clear();
    }

    public int FindFirstEmptySlot()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty) return i;
        }
        return -1;
    }

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)slots.Length)
            throw new IndexOutOfRangeException($"{containerId}: slot index {index} out of range (0..{slots.Length - 1})");
    }
}