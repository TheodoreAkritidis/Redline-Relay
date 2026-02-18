using UnityEngine;

public sealed class TerrainPrototypeDebugger : MonoBehaviour
{
    [SerializeField] private Terrain terrain;

    private void Start()
    {
        if (terrain == null) terrain = FindFirstObjectByType<Terrain>();
        if (terrain == null || terrain.terrainData == null) { Debug.LogWarning("No terrain/terrainData."); return; }

        var protos = terrain.terrainData.treePrototypes;
        Debug.Log($"Tree Prototypes: {protos.Length}");
        for (int i = 0; i < protos.Length; i++)
        {
            var prefab = protos[i].prefab;
            Debug.Log($"{i}: {(prefab != null ? prefab.name : "<null prefab>")}");
        }
    }
}
