// File: InventoryCursorState.cs
using System;

[Serializable]
public sealed class InventoryCursorState
{
    public ItemStack CursorStack;

    // For "cancel menu close" and "return remainder to origin" behavior.
    public IItemContainer OriginContainer;
    public int OriginIndex = -1;

    public bool HasItem => !CursorStack.IsEmpty;

    public void Clear()
    {
        CursorStack.Clear();
        OriginContainer = null;
        OriginIndex = -1;
    }

    public void SetOrigin(IItemContainer container, int index)
    {
        OriginContainer = container;
        OriginIndex = index;
    }

    public bool HasOrigin => OriginContainer != null && OriginIndex >= 0;
}