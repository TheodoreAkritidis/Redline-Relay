using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TerrainHarvestPainter : MonoBehaviour, IInteractable
{
    private TerrainCollider terrainCollider;


    [Serializable]
    public struct DropMapping
    {
        public int PrototypeIndex;              // terrain tree prototype index
        public ItemDefinition DropItem;
        public int DropAmount;
        public string PromptNameOverride;       // optional
    }

    [Header("References")]
    [SerializeField] private Terrain terrain;
    [SerializeField] private WorldItemSpawner spawner;

    [Header("Harvesting")]
    [SerializeField] private float harvestRadiusMeters = 1.25f;
    [SerializeField] private float spawnUpOffset = 0.15f;

    [Header("Prototype -> Drop mapping")]
    [SerializeField] private List<DropMapping> drops = new();

    // current resolved target
    private bool hasTarget;
    private int targetTreeIndex = -1;
    private int targetPrototypeIndex = -1;
    private Vector3 targetWorldPos;
    private DropMapping targetDrop;

    public bool HasTarget => hasTarget;

    private void Awake()
    {
        if (terrain == null) terrain = FindFirstObjectByType<Terrain>();
        if (spawner == null) spawner = GetComponent<WorldItemSpawner>();

        if (terrain != null)
            terrainCollider = terrain.GetComponent<TerrainCollider>();
    }



    public void ResolveFromRay(Ray ray, float maxRange)
    {
        hasTarget = false;
        targetTreeIndex = -1;
        targetPrototypeIndex = -1;

        if (terrain == null) return;
        var tc = terrain.GetComponent<TerrainCollider>();
        if (tc == null) return;

        if (!tc.Raycast(ray, out RaycastHit hit, maxRange))
            return;

        var data = terrain.terrainData;
        if (data == null) return;

        // Convert hit world position to terrain-normalized coordinates (0..1)
        Vector3 local = hit.point - terrain.transform.position;
        Vector3 size = data.size;
        if (size.x <= 0.001f || size.z <= 0.001f) return;

        Vector2 hitNorm = new Vector2(local.x / size.x, local.z / size.z);

        // Find nearest tree instance within radius
        float bestDistSqr = float.PositiveInfinity;
        int bestIndex = -1;

        TreeInstance[] trees = data.treeInstances;
        float radiusSqr = harvestRadiusMeters * harvestRadiusMeters;

        for (int i = 0; i < trees.Length; i++)
        {
            Vector3 p = trees[i].position; // normalized x,z (y also normalized height)
            Vector2 treeNorm = new Vector2(p.x, p.z);

            // quick reject using approx world distance on XZ
            Vector2 dNorm = treeNorm - hitNorm;
            float dx = dNorm.x * size.x;
            float dz = dNorm.y * size.z;
            float distSqr = dx * dx + dz * dz;

            if (distSqr <= radiusSqr && distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                bestIndex = i;
            }
        }

        if (bestIndex < 0)
            return;

        int proto = trees[bestIndex].prototypeIndex;
        if (!TryGetDrop(proto, out targetDrop))
            return;

        Vector3 tNormPos = trees[bestIndex].position;
        Vector3 tLocal = new Vector3(tNormPos.x * size.x, 0f, tNormPos.z * size.z);
        Vector3 world = terrain.transform.position + tLocal;

        // Correct height on terrain
        world.y = terrain.SampleHeight(world) + terrain.transform.position.y;

        hasTarget = true;
        targetTreeIndex = bestIndex;
        targetPrototypeIndex = proto;
        targetWorldPos = world + Vector3.up * spawnUpOffset;

    }

    public string GetPrompt()
    {
        if (!hasTarget) return "";
        if (!string.IsNullOrWhiteSpace(targetDrop.PromptNameOverride))
            return targetDrop.PromptNameOverride;

        if (targetDrop.DropItem != null)
            return $"Harvest {targetDrop.DropItem.ItemId}";

        return "Harvest";
    }


    public void Interact(GameObject interactor)
    {
        if (!hasTarget) return;
        if (terrain == null || terrain.terrainData == null) return;
        if (spawner == null) return;
        if (targetDrop.DropItem == null || targetDrop.DropAmount <= 0) return;

        var data = terrain.terrainData;
        var list = new System.Collections.Generic.List<TreeInstance>(data.treeInstances);
        if (targetTreeIndex < 0 || targetTreeIndex >= list.Count) return;

        list.RemoveAt(targetTreeIndex);
        data.treeInstances = list.ToArray();

        // IMPORTANT: force refresh so tree colliders get rebuilt
        terrain.Flush();
        if (terrainCollider != null)
        {
            terrainCollider.enabled = false;
            terrainCollider.enabled = true;
        }
        Physics.SyncTransforms();

        spawner.SpawnAtWorldPosition(new ItemStack(targetDrop.DropItem, targetDrop.DropAmount), targetWorldPos);

        hasTarget = false;
        targetTreeIndex = -1;
        targetPrototypeIndex = -1;
    }


    private bool TryGetDrop(int prototypeIndex, out DropMapping mapping)
    {
        for (int i = 0; i < drops.Count; i++)
        {
            if (drops[i].PrototypeIndex == prototypeIndex)
            {
                mapping = drops[i];
                return true;
            }
        }

        mapping = default;
        return false;
    }
}
