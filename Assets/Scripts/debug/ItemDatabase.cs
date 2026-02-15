using System.Collections.Generic;
using UnityEngine;

public sealed class ItemDatabase : MonoBehaviour
{
    [SerializeField] private List<ItemDefinition> items = new();

    private Dictionary<string, ItemDefinition> byId;

    private void Awake()
    {
        byId = new Dictionary<string, ItemDefinition>(items.Count);
        foreach (var it in items)
        {
            if (it == null) continue;
            if (string.IsNullOrWhiteSpace(it.ItemId)) continue;

            // last one wins if duplicates
            byId[it.ItemId.Trim()] = it;
        }
    }

    public bool TryGet(string itemId, out ItemDefinition def)
    {
        def = null;
        if (byId == null) return false;
        if (string.IsNullOrWhiteSpace(itemId)) return false;

        return byId.TryGetValue(itemId.Trim(), out def);
    }

    public IEnumerable<string> GetAllIds()
    {
        if (byId == null) yield break;
        foreach (var k in byId.Keys) yield return k;
    }
}
