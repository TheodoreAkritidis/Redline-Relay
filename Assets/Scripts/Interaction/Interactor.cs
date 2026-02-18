using UnityEngine;
using UnityEngine.InputSystem;


public sealed class Interactor : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float interactRange = 3.0f;
    [SerializeField] private LayerMask interactMask = ~0;

    [Header("UI")]
    [SerializeField] private InventoryUITKView inventoryUI;

    private IInteractable current;


    private void Awake()
    {
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
        if (inventoryUI != null && inventoryUI.IsBackpackOpen)
        {
            inventoryUI.SetCrosshairDefault();
            current = null;
            return;
        }

        if (cameraTransform == null)
        {
            inventoryUI?.SetCrosshairDefault();
            return;
        }

        Ray r = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(r, out RaycastHit hit, interactRange, interactMask, QueryTriggerInteraction.Collide))
        {
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
                current = interactable;
        }

        if (current == null)
        {
            inventoryUI?.SetCrosshairDefault();
        }
        else
        {
            inventoryUI?.SetCrosshairPrompt(current.GetPrompt());
        }
    }

    // PlayerInput (Send Messages): Action name must be "Interact" -> calls OnInteract
    public void OnInteract(InputValue v)
    {
        Debug.Log("Interactor.OnInteract fired");
        if (!v.isPressed) return;

        if (current == null)
        {
            Debug.Log("Interact pressed, but current is null.");
            return;
        }

        Debug.Log($"Interacting with: {current}");
        current.Interact(gameObject);
    }
}
