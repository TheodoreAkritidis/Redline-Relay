using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class WorldItem : MonoBehaviour, IInteractable
{
    [Header("Runtime Stack")]
    [SerializeField] private ItemDefinition item;
    [SerializeField] private int quantity;

    [Header("Visual")]
    [SerializeField] private Transform visualRoot;

    private GameObject spawnedVisual;

    private void Reset()
    {
        // Must be raycastable but not blocking: trigger collider.
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    public void SetStack(ItemDefinition def, int qty)
    {
        item = def;
        quantity = Mathf.Max(0, qty);
        ApplyVisual();
        name = item != null ? $"{item.ItemId} (x{quantity})" : "WorldItem";
    }

    public string GetPrompt()
    {
        if (item == null || quantity <= 0) return "";
        return quantity > 1
            ? $"Pick up {item.ItemId} (x{quantity})"
            : $"Pick up {item.ItemId}";
    }

    public void Interact(GameObject interactor)
    {
        if (item == null || quantity <= 0) return;

        var inv = interactor.GetComponent<PlayerInventoryComponent>();
        if (inv == null || inv.Model == null) return;

        int remainder = InventoryRules.TryAutoAdd(item, quantity, inv.Model.Hotbar, inv.Model.Backpack);
        int pickedUp = quantity - remainder;

        if (pickedUp > 0)
            inv.NotifyInventoryChanged();

        if (remainder <= 0)
        {
            Destroy(gameObject);
            return;
        }

        // Not enough space: keep leftover on ground.
        quantity = remainder;
        name = $"{item.ItemId} (x{quantity})";
    }

    private void ApplyVisual()
    {
        if (visualRoot == null) return;

        if (spawnedVisual != null)
            Destroy(spawnedVisual);

        if (item == null) return;

        if (item.WorldPrefab != null)
        {
            spawnedVisual = Instantiate(item.WorldPrefab, visualRoot);
            spawnedVisual.transform.localPosition = Vector3.zero;
            spawnedVisual.transform.localRotation = Quaternion.identity;
            spawnedVisual.transform.localScale = Vector3.one;
        }
    }
}
