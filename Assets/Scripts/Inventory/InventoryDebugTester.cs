// File: InventoryDebugTester.cs
using UnityEngine;

public sealed class InventoryDebugTester : MonoBehaviour
{
    [SerializeField] private int backpackSlots = 30;

    private PlayerInventoryModel player;
    private ItemDefinition wood;
    private ItemDefinition stone;

    private void Start()
    {
        player = new PlayerInventoryModel(hotbarSlots: 10, backpackSlots: backpackSlots);

        wood = ScriptableObject.CreateInstance<ItemDefinition>();
        wood.ItemId = "wood";
        wood.MaxStack = 20;

        stone = ScriptableObject.CreateInstance<ItemDefinition>();
        stone.ItemId = "stone";
        stone.MaxStack = 50;

        // Seed some data
        player.Hotbar.SetSlot(0, new ItemStack(wood, 19));     // almost full
        player.Backpack.SetSlot(0, new ItemStack(wood, 20));   // full
        player.Backpack.SetSlot(1, new ItemStack(stone, 10));

        Dump("Initial");

        // Auto pickup 10 wood -> should fill hotbar[0] by 1, then create new stacks in backpack only
        int rem = InventoryRules.TryAutoAdd(wood, 10, player.Hotbar, player.Backpack);
        Debug.Log($"AutoAdd wood(10) remainder = {rem}");
        Dump("After AutoAdd wood(10)");

        // Left click pick up backpack slot 1 (stone 10)
        InventoryRules.TryLeftClickSlot(player.Cursor, player.Backpack, 1);
        Debug.Log($"Cursor now: {Describe(player.Cursor.CursorStack)}");

        // Left click drop onto hotbar slot 2 (empty) -> move
        InventoryRules.TryLeftClickSlot(player.Cursor, player.Hotbar, 2);
        Dump("After moving stone to hotbar[2]");

        // Split hotbar slot 2 (stone 10) -> should put 5 into first empty BACKPACK slot (not hotbar)
        InventoryRules.TryRightClickSlot(player.Cursor, player.Hotbar, 2, player.Backpack);
        Dump("After splitting stone in hotbar[2]");


    
        //partial merge
        Debug.Log("=== Partial-merge remainder test ===");

        // Ensure a partially filled wood stack exists (B[6] = wood x18)
        player.Backpack.SetSlot(6, new ItemStack(wood, 18));

        // Ensure an origin slot containing wood x7 exists (B[7] = wood x7)
        player.Backpack.SetSlot(7, new ItemStack(wood, 7));

        Dump("Before partial merge: target B[6]=wood x18, origin B[7]=wood x7");

        // Pick up origin wood x7 into cursor
        InventoryRules.TryLeftClickSlot(player.Cursor, player.Backpack, 7);
        Debug.Log($"Cursor after pickup: {Describe(player.Cursor.CursorStack)} (expected wood x7)");

        // Drop cursor onto target wood x18 -> should merge 2, remainder 5 goes BACK to origin B[7], cursor clears
        bool didDrop = InventoryRules.TryLeftClickSlot(player.Cursor, player.Backpack, 6);
        Debug.Log($"Drop result = {didDrop} (expected true)");

        Dump("After partial merge: expect B[6]=wood x20, B[7]=wood x5, cursor empty");
        


        // Merge behavior test:
        // Put wood 7 in backpack[5], pick it up, drop onto backpack[0] (wood 20 full) -> do nothing
        player.Backpack.SetSlot(5, new ItemStack(wood, 7));
        InventoryRules.TryLeftClickSlot(player.Cursor, player.Backpack, 5); // pick up wood 7
        bool merged = InventoryRules.TryLeftClickSlot(player.Cursor, player.Backpack, 0); // try drop onto full wood stack
        Debug.Log($"Drop onto full same-type stack result = {merged} (expected false/no-op)");
        Dump("After trying to drop wood onto full stack");

        // Cancel menu behavior: return cursor to origin
        InventoryRules.CancelCursorToOrigin(player.Cursor);
        Dump("After cancel-to-origin");

        // Drop outside UI behavior: pick up something and drop to world
        InventoryRules.TryLeftClickSlot(player.Cursor, player.Hotbar, 2); // pick up stone stack from hotbar[2]
        ItemStack dropped = InventoryRules.DropCursorToWorld(player.Cursor);
        Debug.Log($"Dropped to world: {Describe(dropped)}");
        Dump("After drop-to-world");
    }

    private void Dump(string label)
    {
        Debug.Log($"=== {label} ===");
        Debug.Log("Hotbar:");
        for (int i = 0; i < player.Hotbar.SlotCount; i++)
            Debug.Log($"  H[{i}] = {Describe(player.Hotbar.GetSlot(i))}");

        Debug.Log("Backpack:");
        for (int i = 0; i < player.Backpack.SlotCount; i++)
        {
            var s = player.Backpack.GetSlot(i);
            if (!s.IsEmpty) Debug.Log($"  B[{i}] = {Describe(s)}");
        }

        Debug.Log($"Cursor = {Describe(player.Cursor.CursorStack)} (Origin: {player.Cursor.OriginContainer?.ContainerId ?? "None"}[{player.Cursor.OriginIndex}])");
    }

    private static string Describe(ItemStack s)
    {
        if (s.IsEmpty) return "(empty)";
        return $"{s.Item.ItemId} x{s.Quantity}";
    }
}