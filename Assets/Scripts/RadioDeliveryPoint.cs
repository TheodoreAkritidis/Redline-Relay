using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class RadioDeliveryPoint : MonoBehaviour, IInteractable
{
    [Header("Required Item")]
    [SerializeField] private ItemDefinition radioItem;

    [Header("Prompt")]
    [SerializeField] private string promptText = "Place Radio";

    [Header("Optional Visuals")]
    [SerializeField] private GameObject hologramVisual;
    [SerializeField] private GameObject placedRadioVisual;

    [Header("Debug")]
    [SerializeField] private bool logIfMissingRadio = false;

    private bool delivered;
    private PlayerInventoryComponent cachedPlayer;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = false;
    }

    private void Awake()
    {
        cachedPlayer = FindFirstObjectByType<PlayerInventoryComponent>();

        if (placedRadioVisual != null)
            placedRadioVisual.SetActive(false);
    }

    private void Update()
    {
        if (delivered)
        {
            if (hologramVisual != null && hologramVisual.activeSelf)
                hologramVisual.SetActive(false);
            return;
        }

        if (cachedPlayer == null)
            cachedPlayer = FindFirstObjectByType<PlayerInventoryComponent>();

        bool hasRadio = PlayerHasRadio(cachedPlayer);

        if (hologramVisual != null && hologramVisual.activeSelf != hasRadio)
            hologramVisual.SetActive(hasRadio);
    }

    public string GetPrompt()
    {
        return delivered ? "" : promptText;
    }

    public void Interact(GameObject interactor)
    {
        if (delivered) return;

        var playerInventory = interactor != null
            ? interactor.GetComponent<PlayerInventoryComponent>()
            : null;

        if (!PlayerHasRadio(playerInventory))
        {
            if (logIfMissingRadio)
                Debug.Log("RadioDeliveryPoint: player tried to place radio, but does not have it.");
            return;
        }

        bool consumed = InventoryRules.TryConsume(
            playerInventory.Model.Hotbar,
            playerInventory.Model.Backpack,
            radioItem,
            1
        );

        if (!consumed)
        {
            Debug.LogWarning("RadioDeliveryPoint: radio was detected but could not be consumed.");
            return;
        }

        playerInventory.NotifyInventoryChanged();

        delivered = true;

        if (hologramVisual != null)
            hologramVisual.SetActive(false);

        if (placedRadioVisual != null)
            placedRadioVisual.SetActive(true);

        Debug.Log("WIN PLACEHOLDER: Radio delivered successfully.");
    }

    private bool PlayerHasRadio(PlayerInventoryComponent playerInventory)
    {
        if (radioItem == null || playerInventory == null || playerInventory.Model == null)
            return false;

        return InventoryRules.CountItem(
            playerInventory.Model.Hotbar,
            playerInventory.Model.Backpack,
            radioItem
        ) > 0;
    }
}