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

    [Header("Player")]
    [SerializeField] private PlayerInventoryComponent playerInventory;

    private IInteractable current;
    private bool currentBlocked;

    private void Awake()
    {
        if (terrainHarvest == null) terrainHarvest = FindFirstObjectByType<TerrainHarvestPainter>();
        if (inventoryUI == null) inventoryUI = FindFirstObjectByType<InventoryUITKView>();
        if (playerInventory == null) playerInventory = GetComponent<PlayerInventoryComponent>();

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
        currentBlocked = false;

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

                // Tool gate support
                if (interactable is IToolGatedInteractable gated &&
                    !gated.CanInteractWith(gameObject, out string blockedPrompt))
                {
                    currentBlocked = true;

                    // IMPORTANT: message WITHOUT "E to"
                    inventoryUI?.SetCrosshairMessage(blockedPrompt);
                }
                else
                {
                    inventoryUI?.SetCrosshairPrompt(current.GetPrompt());
                }

                return;
            }
        }

        // 2) Terrain fallback
        if (terrainHarvest != null)
        {
            terrainHarvest.ResolveFromRay(r, interactRange);
            if (terrainHarvest.HasTarget)
            {
                current = terrainHarvest;

                if (current is IToolGatedInteractable gated &&
                    !gated.CanInteractWith(gameObject, out string blockedPrompt))
                {
                    currentBlocked = true;
                    inventoryUI?.SetCrosshairMessage(blockedPrompt);
                }
                else
                {
                    inventoryUI?.SetCrosshairPrompt(current.GetPrompt());
                }

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
        if (currentBlocked) return;

        current.Interact(gameObject);
    }
}