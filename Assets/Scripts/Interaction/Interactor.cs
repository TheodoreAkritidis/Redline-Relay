using System;
using UnityEngine;
using UnityEngine.InputSystem;


public sealed class Interactor : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float interactRange = 3.0f;
    [SerializeField] private LayerMask interactMask = ~0;

    [Header("terrain")]
    [SerializeField] private TerrainHarvestPainter terrainHarvest;


    [Header("UI")]
    [SerializeField] private InventoryUITKView inventoryUI;

    private IInteractable current;


    private void Awake()
    {
        if (terrainHarvest == null) terrainHarvest = FindFirstObjectByType<TerrainHarvestPainter>();

        if (inventoryUI == null) inventoryUI = FindFirstObjectByType<InventoryUITKView>();
        if (cameraTransform == null)
        {
            var cam = Camera.main;
            if (cam != null) cameraTransform = cam.transform;
        }

    }

    private void Update()
    {
        ResolveTarget();
    }

    private void ResolveTarget()
    {
        current = null;

        if (inventoryUI != null && (inventoryUI.IsBackpackOpen || inventoryUI.IsCraftingOpen))
        {
            inventoryUI.SetCrosshairDefault();
            return;
        }

        if (cameraTransform == null)
        {
            inventoryUI?.SetCrosshairDefault();
            return;
        }

        Ray r = new Ray(cameraTransform.position, cameraTransform.forward);

        // 1) Find the nearest physics hit that actually has an IInteractable
        var hits = Physics.RaycastAll(r, interactRange, interactMask, QueryTriggerInteraction.Collide);
        if (hits != null && hits.Length > 0)
        {
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                var interactable = hits[i].collider.GetComponentInParent<IInteractable>();
                if (interactable == null) continue;

                current = interactable;
                inventoryUI?.SetCrosshairPrompt(current.GetPrompt());
                return;
            }
        }

        // 2) Fallback: painted terrain trees/rocks via TerrainHarvestPainter
        if (terrainHarvest != null)
        {
            terrainHarvest.ResolveFromRay(r, interactRange);
            if (terrainHarvest.HasTarget)
            {
                current = terrainHarvest;
                inventoryUI?.SetCrosshairPrompt(current.GetPrompt());
                return;
            }
        }

        inventoryUI?.SetCrosshairDefault();
    }

    public void OnInteract(InputValue v)
    {
        if (!v.isPressed) return;

        if (inventoryUI != null && (inventoryUI.IsBackpackOpen || inventoryUI.IsCraftingOpen))
            return;

        if (current == null) return;

        current.Interact(gameObject);
    }

}
