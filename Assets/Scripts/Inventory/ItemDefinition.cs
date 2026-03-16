using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item Definition")]
public class ItemDefinition : ScriptableObject
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
    public bool AppliesStatus;
    public float FoodValue = 0;
    public string Status = "None";

    [Space(20)]
    public bool IsWater;
    public float WaterValue = 0;

    [Header("Smelting")]
    [Tooltip("If true, this item can be smelted in the Smelter ore slot.")]
    public bool IsOre;

    [Tooltip("What this ore becomes when smelted (e.g., CopperOre -> CopperIngot).")]
    public ItemDefinition SmeltResult;

    [Tooltip("Seconds per 1 ore item.")]
    public float SmeltSecondsPerItem = 5f;

    [Header("Fuel")]
    [Tooltip("If true, this item can be used in the Smelter fuel slot.")]
    public bool IsFuel;

    [Tooltip("Seconds of burn time provided by ONE unit of this fuel item.")]
    public float FuelSeconds = 10f;

    [Header("Status Effects")]
    public bool AppliesStatus = false;
    public string Status;
}