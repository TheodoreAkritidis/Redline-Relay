using UnityEngine;

public sealed class ResourceNodeInteractable : MonoBehaviour, IInteractable, IToolGatedInteractable
{
    [Header("Drop")]
    [SerializeField] private ItemDefinition dropItem;
    [SerializeField] private int dropAmount = 1;

    [Header("Inventory")]
    [SerializeField] private bool addDirectToInventory = false;
    [SerializeField] private bool dropRemainderIfFull = true;

    [Header("Tool Requirement")]
    [SerializeField] private ItemDefinition requiredTool; // null = no requirement
    [SerializeField] private string requiredToolNameOverride = ""; // optional display name override

    [Header("UI")]
    [SerializeField] private string verb = "Harvest"; // "Chop", "Mine", etc.

    [Header("Destroy")]
    [SerializeField] private bool destroyOnHarvest = true;

    private WorldItemSpawner spawner;
    private Collider cachedCollider;

    private void Awake()
    {
        spawner = FindFirstObjectByType<WorldItemSpawner>();
        cachedCollider = GetComponentInChildren<Collider>();
    }

    // IInteractable
    public string GetPrompt()
    {
        if (dropItem == null) return verb;
        return $"{verb} {dropItem.ItemId}";
    }

    // IToolGatedInteractable
    public bool CanInteractWith(GameObject interactor, out string blockedPrompt)
    {
        blockedPrompt = "";

        // No tool required
        if (requiredTool == null)
            return true;

        // Need an interactor with an inventory + a selected hotbar item matching requiredTool
        if (interactor == null)
        {
            blockedPrompt = $"Requires {GetRequiredToolDisplayName()}";
            return false;
        }

        var inv = interactor.GetComponent<PlayerInventoryComponent>();
        if (inv == null || inv.Model == null || inv.Model.Hotbar == null)
        {
            blockedPrompt = $"Requires {GetRequiredToolDisplayName()}";
            return false;
        }

        int idx = inv.SelectedHotbarIndex;
        if (idx < 0 || idx >= inv.Model.Hotbar.SlotCount)
        {
            blockedPrompt = $"Requires {GetRequiredToolDisplayName()}";
            return false;
        }

        ItemStack s = inv.Model.Hotbar.GetSlot(idx);
        bool hasRequiredToolSelected = !s.IsEmpty && s.Item == requiredTool;

        if (!hasRequiredToolSelected)
        {
            blockedPrompt = $"Requires {GetRequiredToolDisplayName()}";
            return false;
        }

        return true;
    }

    private string GetRequiredToolDisplayName()
    {
        if (!string.IsNullOrWhiteSpace(requiredToolNameOverride))
            return requiredToolNameOverride.Trim();

        return requiredTool != null ? requiredTool.ItemId : "Tool";
    }

    // IInteractable
    public void Interact(GameObject interactor)
    {
        // Hard gate here too (so even if something bypasses Interactor UI, it still won't work)
        if (!CanInteractWith(interactor, out _))
            return;

        if (dropItem == null) return;

        int amt = Mathf.Max(1, dropAmount);

        // If configured, try to go directly into the player's inventory first.
        if (addDirectToInventory && interactor != null)
        {
            var inv = interactor.GetComponent<PlayerInventoryComponent>();
            if (inv != null && inv.Model != null)
            {
                var hotbar = inv.Model.Hotbar;
                var backpack = inv.Model.Backpack;

                int originalAmt = amt;

                // Try to add everything to inventory.
                int remaining = InventoryRules.TryAutoAdd(dropItem, amt, hotbar, backpack);

                // If NOTHING fit, treat as "inventory full" and spawn the whole stack into the world instead.
                if (remaining >= originalAmt)
                {
                    // fall through to world spawn with original amt
                }
                else
                {
                    // Some or all fit -> update inventory visuals.
                    inv.NotifyInventoryChanged();

                    // If everything fit, consume the node and exit.
                    if (remaining <= 0)
                    {
                        if (destroyOnHarvest) Destroy(gameObject);
                        else gameObject.SetActive(false);
                        return;
                    }

                    // Otherwise, drop only what didn't fit.
                    amt = remaining;

                    // Optionally suppress remainder drops.
                    if (!dropRemainderIfFull)
                    {
                        if (destroyOnHarvest) Destroy(gameObject);
                        else gameObject.SetActive(false);
                        return;
                    }
                }
            }
        }

        // World spawn (original behavior) OR "inventory full" fallback OR remainder drop.
        if (spawner == null)
            spawner = FindFirstObjectByType<WorldItemSpawner>();

        if (spawner == null)
        {
            Debug.LogWarning("ResourceNodeInteractable: No WorldItemSpawner found in scene.");
            return;
        }

        Vector3 dropPos = cachedCollider != null ? cachedCollider.bounds.center : transform.position;
        spawner.SpawnAtWorldPosition(new ItemStack(dropItem, amt), dropPos);

        if (destroyOnHarvest)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}