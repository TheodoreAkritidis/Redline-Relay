using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item Definition")]
public sealed class ItemDefinition : ScriptableObject
{
    public string ItemId;
    public int MaxStack = 1;
    public Sprite Icon;
    public string ItemType;

    [Header("World")]
    public GameObject WorldPrefab; // <-- assign a mesh/model prefab for this item (log, rock, etc)

    public string GetItemType()
    {
        Debug.Log($"Fetched Item Type; '{ItemType}'");
        return ItemType;
    }
}
