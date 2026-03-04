using UnityEngine;

public sealed class ResourceNodeInteractable : MonoBehaviour, IInteractable
{
    [Header("Drop")]
    [SerializeField] private ItemDefinition dropItem;
    [SerializeField] private int dropAmount = 1;

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

    // IInteractable
    public void Interact(GameObject interactor)
    {
        if (dropItem == null) return;

        if (spawner == null)
            spawner = FindFirstObjectByType<WorldItemSpawner>();

        if (spawner == null)
        {
            Debug.LogWarning("ResourceNodeInteractable: No WorldItemSpawner found in scene.");
            return;
        }

        int amt = Mathf.Max(1, dropAmount);

        // drop near the object (use collider center if possible)
        Vector3 dropPos = cachedCollider != null ? cachedCollider.bounds.center : transform.position;

        spawner.SpawnAtWorldPosition(new ItemStack(dropItem, amt), dropPos);

        if (destroyOnHarvest)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
