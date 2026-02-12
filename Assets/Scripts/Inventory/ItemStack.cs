// File: ItemStack.cs
using System;

[Serializable]
public struct ItemStack
{
    public ItemDefinition Item;
    public int Quantity;

    public bool IsEmpty => Item == null || Quantity <= 0;

    public ItemStack(ItemDefinition item, int quantity)
    {
        Item = item;
        Quantity = quantity;
    }

    public void Clear()
    {
        Item = null;
        Quantity = 0;
    }
}