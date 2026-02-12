// File: PlayerInventoryModel.cs
using UnityEngine;

public sealed class PlayerInventoryModel
{
    public readonly ArrayItemContainer Hotbar;
    public readonly ArrayItemContainer Backpack;
    public readonly InventoryCursorState Cursor;

    public PlayerInventoryModel(int hotbarSlots, int backpackSlots)
    {
        Hotbar = new ArrayItemContainer("Hotbar", hotbarSlots);
        Backpack = new ArrayItemContainer("Backpack", backpackSlots);
        Cursor = new InventoryCursorState();
    }
}