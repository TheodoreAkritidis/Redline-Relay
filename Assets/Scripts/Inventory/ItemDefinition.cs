using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item Definition")]
public sealed class ItemDefinition : ScriptableObject
{
    public string ItemId;
    public int MaxStack = 1;
    public Sprite Icon;

    [Header("World")]
    public GameObject WorldPrefab; // <-- assign a mesh/model prefab for this item (log, rock, etc)

    [Header("Consumable")]
    public bool DestroyOnUse;
    [Space(10)]
    public bool IsFood;
    public float FoodValue = 0;
    [Space(20)]
    public bool IsWater;
    public float WaterValue = 0;
}
