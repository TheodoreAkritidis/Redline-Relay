using NUnit.Framework.Internal.Execution;
using UnityEngine;

public sealed class WorldItemSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private WorldItem worldItemPilePrefab; // one generic pile prefab

    [Header("Drop At Feet")]
    [SerializeField] private float footUpOffset = 0.05f;
    [SerializeField] private float groundRayUp = 0.5f;
    [SerializeField] private float groundRayDown = 2.0f;
    [SerializeField] private LayerMask groundMask = ~0;

    public bool SpawnAtFeet(ItemStack stack, Transform playerRoot)
    {
        if (worldItemPilePrefab == null) { Debug.LogWarning("WorldItemSpawner: pile prefab not set."); return false; }
        if (playerRoot == null) return false;
        if (stack.IsEmpty) return false;

        Vector3 pos = playerRoot.position;

        // snap to ground beneath player
        Vector3 rayStart = pos + Vector3.up * groundRayUp;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRayUp + groundRayDown, groundMask, QueryTriggerInteraction.Ignore))
            pos = hit.point;

        pos += Vector3.up * footUpOffset;

        WorldItem wi = Instantiate(worldItemPilePrefab, pos, Quaternion.identity);
        wi.SetStack(stack.Item, stack.Quantity);
        return true;
    }
}
