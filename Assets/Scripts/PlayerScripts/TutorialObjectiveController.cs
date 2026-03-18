using UnityEngine;

public sealed class TutorialObjectiveController : MonoBehaviour
{
    private enum TutorialStage
    {
        Intro,
        SmeltingAndRadio,
        ReturnToPod
    }

    [Header("References")]
    [SerializeField] private InventoryUITKView inventoryUI;
    [SerializeField] private PlayerInventoryComponent playerInventory;

    [Header("Required Items")]
    [SerializeField] private ItemDefinition stoneAxeItem;
    [SerializeField] private ItemDefinition stonePickaxeItem;
    [SerializeField] private ItemDefinition radioItem;

    private TutorialStage currentStage;

    private const string IntroText =
        "Press Esc to pause or leave any menu\n" +
        "W, A, S, D to move (left gamepad stick), \n" +
        "hold Shift to sprint (L3), Space to jump (A)\n" +
        "C to craft (=), E to interact (Y) \n\n" +
        "First, craft a stone axe and stone pickaxe.";

    private const string SmeltingText =
        "Interact with your campfire to smelt ores\n" +
        "Smelting ores requires fuel (wood) and takes time\n\n" +
        "Use smelted ores to craft better items, and make your way to crafting a radio.";

    private const string ReturnToPodText =
        "Go near your pod, where there's a spot for you to place your radio.";

    private void Awake()
    {
        TryResolveReferences();
    }

    private void TryResolveReferences()
    {
        if (inventoryUI == null || !inventoryUI.isActiveAndEnabled)
            inventoryUI = FindFirstObjectByType<InventoryUITKView>();

        if (playerInventory == null || !playerInventory.isActiveAndEnabled)
            playerInventory = FindFirstObjectByType<PlayerInventoryComponent>();
    }

    private void Update()
    {
        TutorialStage nextStage = EvaluateStage();

        if (nextStage != currentStage)
        {
            currentStage = nextStage;
            ApplyStageText();
        }
        else if (inventoryUI != null)
        {
            // Re-apply in case UI rebuilt after scene startup timing.
            ApplyStageText();
        }
    }

    private TutorialStage EvaluateStage()
    {
        if (HasItem(radioItem))
            return TutorialStage.ReturnToPod;

        if (HasItem(stoneAxeItem) && HasItem(stonePickaxeItem))
            return TutorialStage.SmeltingAndRadio;

        return TutorialStage.Intro;
    }

    private bool HasItem(ItemDefinition item)
    {
        if (item == null || playerInventory == null || playerInventory.Model == null)
            return false;

        return InventoryRules.CountItem(
            playerInventory.Model.Hotbar,
            playerInventory.Model.Backpack,
            item
        ) > 0;
    }

    private void ApplyStageText()
    {
        if (inventoryUI == null) return;

        switch (currentStage)
        {
            case TutorialStage.Intro:
                inventoryUI.SetTutorialText(IntroText);
                break;

            case TutorialStage.SmeltingAndRadio:
                inventoryUI.SetTutorialText(SmeltingText);
                break;

            case TutorialStage.ReturnToPod:
                inventoryUI.SetTutorialText(ReturnToPodText);
                break;
        }
    }
}