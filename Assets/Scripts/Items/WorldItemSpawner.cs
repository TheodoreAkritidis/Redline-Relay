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
        if (playerRoot == null) return false;
        return SpawnAtWorldPosition(stack, playerRoot.position);
    }

    public bool SpawnAtWorldPosition(ItemStack stack, Vector3 worldPos)
    {
        if (worldItemPilePrefab == null) { Debug.LogWarning("WorldItemSpawner: pile prefab not set."); return false; }
        if (stack.IsEmpty) return false;

        // Step A: find ground under/around the target point (Ground layer only)
        if (!TryFindGroundPoint(worldPos, out Vector3 groundPoint, out Vector3 groundNormal))
            return false;

        // Start slightly above ground
        Vector3 pos = groundPoint + Vector3.up * footUpOffset;

        // Step B: instantiate
        WorldItem wi = Instantiate(worldItemPilePrefab, pos, Quaternion.identity);
        wi.SetStack(stack.Item, stack.Quantity);

        // Step C: validate placement using the spawned collider size
        if (!TryResolvePenetrationUp(wi, ref pos))
        {
            // If we fail to resolve, don’t leave broken items around
            Destroy(wi.gameObject);
            return false;
        }

        // Step D: final “no floating” snap (in case we lifted it)
        if (TryFindGroundPoint(pos, out Vector3 groundPoint2, out _))
        {
            // keep any lift we needed, but ensure we’re not hovering due to missing snap
            float minY = groundPoint2.y + footUpOffset;
            if (pos.y < minY) pos.y = minY;
        }

        wi.transform.position = pos;
        return true;
    }

    private bool TryFindGroundPoint(Vector3 aroundPos, out Vector3 groundPoint, out Vector3 groundNormal)
    {
        // Start high enough that we’re almost always above terrain even if caller point is weird
        Vector3 rayStart = aroundPos + Vector3.up * Mathf.Max(groundRayUp, 2.0f);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit,
                groundRayUp + groundRayDown + 10f, // extra forgiveness
                groundMask,
                QueryTriggerInteraction.Ignore))
        {
            groundPoint = hit.point;
            groundNormal = hit.normal;
            return true;
        }

        groundPoint = default;
        groundNormal = Vector3.up;
        return false;
    }

    private bool TryResolvePenetrationUp(WorldItem wi, ref Vector3 pos)
    {
        // We’ll lift the item if its trigger collider overlaps ground.
        // This works best if your WorldItem uses a BoxCollider fitted to the visual.
        Collider col = wi.GetComponent<Collider>();
        if (col == null) return true; // nothing to validate

        const int maxSteps = 12;
        const float stepUp = 0.05f;

        // Temporarily move to pos for accurate overlap test
        wi.transform.position = pos;

        for (int i = 0; i < maxSteps; i++)
        {
            if (!IsOverlappingGround(col))
                return true;

            pos += Vector3.up * stepUp;
            wi.transform.position = pos;
        }

        return false;
    }

    private bool IsOverlappingGround(Collider itemCollider)
    {
        // Use an overlap check that matches collider type reasonably well.
        // (BoxCollider is ideal; SphereCollider fallback supported.)
        if (itemCollider is BoxCollider box)
        {
            Vector3 center = box.transform.TransformPoint(box.center);
            Vector3 halfExtents = Vector3.Scale(box.size * 0.5f, box.transform.lossyScale);
            Quaternion rot = box.transform.rotation;

            // Only check against Ground layer
            return Physics.OverlapBox(center, halfExtents, rot, groundMask, QueryTriggerInteraction.Ignore).Length > 0;
        }

        if (itemCollider is SphereCollider sphere)
        {
            Vector3 center = sphere.transform.TransformPoint(sphere.center);
            float radius = sphere.radius * Mathf.Max(
                sphere.transform.lossyScale.x,
                sphere.transform.lossyScale.y,
                sphere.transform.lossyScale.z);

            return Physics.OverlapSphere(center, radius, groundMask, QueryTriggerInteraction.Ignore).Length > 0;
        }

        // Fallback: bounds-based box
        Bounds b = itemCollider.bounds;
        return Physics.OverlapBox(b.center, b.extents, itemCollider.transform.rotation, groundMask, QueryTriggerInteraction.Ignore).Length > 0;
    }

}
