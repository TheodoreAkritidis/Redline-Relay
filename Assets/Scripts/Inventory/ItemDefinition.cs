using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item Definition")]
public sealed class ItemDefinition : ScriptableObject
{
    public string ItemId;
    public int MaxStack = 1;
    public Sprite Icon;

    [Header("World")]
    public GameObject WorldPrefab; // <-- assign a mesh/model prefab for this item (log, rock, etc)
}
