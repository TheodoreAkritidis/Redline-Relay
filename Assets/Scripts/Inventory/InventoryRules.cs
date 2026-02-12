// File: InventoryRules.cs
using UnityEngine;

public static class InventoryRules
{
    // Decision: "same type" means same ItemDefinition reference.
    public static bool IsSameType(in ItemStack a, in ItemStack b)
        => !a.IsEmpty && !b.IsEmpty && a.Item == b.Item;

    public static int GetMaxStack(ItemDefinition item)
        => item != null ? Mathf.Max(1, item.MaxStack) : 1;

    // Auto-pickup:
    // - fill existing stacks in hotbar+backpack
    // - create new stacks ONLY in backpack
    // - return remainder that didn't fit
    public static int TryAutoAdd(ItemDefinition item, int amount, IItemContainer hotbar, IItemContainer backpack)
    {
        if (item == null || amount <= 0) return amount;

        // 1) Fill existing stacks (hotbar then backpack; consistent order)
        amount = FillExistingStacks(hotbar, item, amount);
        amount = FillExistingStacks(backpack, item, amount);

        // 2) Create new stacks in backpack only
        while (amount > 0)
        {
            int empty = FindFirstEmptySlot(backpack);
            if (empty < 0) break;

            int max = GetMaxStack(item);
            int put = Mathf.Min(max, amount);
            backpack.SetSlot(empty, new ItemStack(item, put));
            amount -= put;
        }

        return amount; // remainder left in world
    }

    // Left click behavior:
    // If cursor empty -> pick up entire stack from slot into cursor
    // If cursor has item -> drop cursor stack into slot using drop rules
    public static bool TryLeftClickSlot(InventoryCursorState cursor, IItemContainer container, int slotIndex)
    {
        if (cursor == null || container == null) return false;

        if (!cursor.HasItem)
            return TryPickUpStack(cursor, container, slotIndex);

        return TryDropCursorStack(cursor, container, slotIndex);
    }

    // Right click behavior:
    // If cursor empty -> split stack into backpack
    // If cursor has item -> place/add exactly one
    public static bool TryRightClickSlot(InventoryCursorState cursor, IItemContainer container, int slotIndex, IItemContainer backpack)
    {
        if (cursor == null || container == null) return false;

        if (!cursor.HasItem)
            return TrySplitStackToBackpack(container, slotIndex, backpack);

        return TryPlaceOneFromCursor(cursor, container, slotIndex);
    }

    public static bool TryPickUpStack(InventoryCursorState cursor, IItemContainer fromContainer, int fromIndex)
    {
        ItemStack from = fromContainer.GetSlot(fromIndex);
        if (from.IsEmpty) return false;

        cursor.CursorStack = from;
        cursor.SetOrigin(fromContainer, fromIndex);

        from.Clear();
        fromContainer.SetSlot(fromIndex, from);

        return true;
    }

    // Core drop rule set:
    // - empty target => move
    // - different item => swap
    // - same item:
    //    - if target has room => merge as much as possible
    //    - if target full => no-op
    // - remainder after merge stays in origin (not cursor)
    public static bool TryDropCursorStack(InventoryCursorState cursor, IItemContainer toContainer, int toIndex)
    {
        if (!cursor.HasItem) return false;

        ItemStack to = toContainer.GetSlot(toIndex);
        ItemStack held = cursor.CursorStack;

        // Target empty -> move held into target, clear cursor.
        if (to.IsEmpty)
        {
            toContainer.SetSlot(toIndex, held);
            cursor.Clear();
            return true;
        }

        // Different type -> swap (cursor holds target, target gets held).
        if (!IsSameType(held, to))
        {
            toContainer.SetSlot(toIndex, held);
            cursor.CursorStack = to;
            // Origin stays as whatever it was (still "holding something" originating somewhere)
            // This makes cancel-on-close still behave sensibly.
            return true;
        }

        // Same type -> merge if target has room, else do nothing.
        int max = GetMaxStack(held.Item);
        if (to.Quantity >= max)
            return false; // do nothing

        int space = max - to.Quantity;
        int moved = Mathf.Min(space, held.Quantity);

        to.Quantity += moved;
        held.Quantity -= moved;

        toContainer.SetSlot(toIndex, to);

        if (held.Quantity <= 0)
        {
            cursor.Clear();
            return true;
        }

        // Remainder should go back to origin (not stay on cursor).
        if (cursor.HasOrigin)
        {
            ItemStack origin = cursor.OriginContainer.GetSlot(cursor.OriginIndex);
            // Origin should be empty in normal flow (we picked it up), but handle safely.
            if (origin.IsEmpty)
            {
                cursor.OriginContainer.SetSlot(cursor.OriginIndex, held);
                cursor.Clear();
                return true;
            }
        }

        // Fallback: keep remainder on cursor if no valid origin (shouldn’t happen in normal UI flow).
        cursor.CursorStack = held;
        return true;
    }

    // Right click split:
    // - if qty <= 1 => no-op
    // - odd split: origin keeps majority (ceil), new gets floor
    // - new stack goes to first empty backpack slot ONLY (never hotbar)
    public static bool TrySplitStackToBackpack(IItemContainer source, int sourceIndex, IItemContainer backpack)
    {
        if (source == null || backpack == null) return false;

        ItemStack stack = source.GetSlot(sourceIndex);
        if (stack.IsEmpty || stack.Quantity <= 1) return false;

        int empty = FindFirstEmptySlot(backpack);
        if (empty < 0) return false;

        int originalKeeps = (stack.Quantity + 1) / 2; // ceil
        int newGets = stack.Quantity / 2;             // floor

        stack.Quantity = originalKeeps;
        source.SetSlot(sourceIndex, stack);

        backpack.SetSlot(empty, new ItemStack(stack.Item, newGets));
        return true;
    }

    // Cursor right-click behavior:
    // - empty target => place 1
    // - same-type target with room => add 1
    // - different type or full => no-op
    public static bool TryPlaceOneFromCursor(InventoryCursorState cursor, IItemContainer toContainer, int toIndex)
    {
        if (!cursor.HasItem) return false;

        ItemStack held = cursor.CursorStack;
        ItemStack to = toContainer.GetSlot(toIndex);

        if (to.IsEmpty)
        {
            toContainer.SetSlot(toIndex, new ItemStack(held.Item, 1));
            held.Quantity -= 1;
            cursor.CursorStack = held;
            if (held.Quantity <= 0) cursor.Clear();
            return true;
        }

        if (!IsSameType(held, to))
            return false;

        int max = GetMaxStack(held.Item);
        if (to.Quantity >= max)
            return false;

        to.Quantity += 1;
        held.Quantity -= 1;

        toContainer.SetSlot(toIndex, to);
        cursor.CursorStack = held;
        if (held.Quantity <= 0) cursor.Clear();

        return true;
    }

    // Menu close behavior: snap back to origin
    public static bool CancelCursorToOrigin(InventoryCursorState cursor)
    {
        if (cursor == null || !cursor.HasItem || !cursor.HasOrigin) return false;

        ItemStack origin = cursor.OriginContainer.GetSlot(cursor.OriginIndex);

        // If origin is empty, return directly.
        if (origin.IsEmpty)
        {
            cursor.OriginContainer.SetSlot(cursor.OriginIndex, cursor.CursorStack);
            cursor.Clear();
            return true;
        }

        // If origin is occupied (shouldn't happen), try to swap back.
        ItemStack held = cursor.CursorStack;
        cursor.OriginContainer.SetSlot(cursor.OriginIndex, held);
        cursor.CursorStack = origin;
        // Keep origin reference as-is; user can try again or UI can handle.
        return true;
    }

    // Drop outside UI behavior: return stack for world spawn, clear cursor
    public static ItemStack DropCursorToWorld(InventoryCursorState cursor)
    {
        if (cursor == null || !cursor.HasItem) return default;

        ItemStack dropped = cursor.CursorStack;
        cursor.Clear();
        return dropped;
    }

    private static int FillExistingStacks(IItemContainer container, ItemDefinition item, int amount)
    {
        if (container == null || item == null || amount <= 0) return amount;

        int max = GetMaxStack(item);

        for (int i = 0; i < container.SlotCount && amount > 0; i++)
        {
            ItemStack s = container.GetSlot(i);
            if (s.IsEmpty || s.Item != item) continue;
            if (s.Quantity >= max) continue;

            int space = max - s.Quantity;
            int add = Mathf.Min(space, amount);

            s.Quantity += add;
            container.SetSlot(i, s);

            amount -= add;
        }

        return amount;
    }

    private static int FindFirstEmptySlot(IItemContainer container)
    {
        if (container is ArrayItemContainer a)
            return a.FindFirstEmptySlot();

        // Generic fallback
        for (int i = 0; i < container.SlotCount; i++)
        {
            if (container.GetSlot(i).IsEmpty) return i;
        }
        return -1;
    }
}