using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;

[RequireComponent(typeof(UIDocument))]
public sealed class InventoryUITKView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private PlayerInventoryComponent playerInventory;

    [Header("Layout")]
    [SerializeField] private int backpackColumns = 10;
    [SerializeField] private Vector2 slotSize = new Vector2(56, 56);

    [Header("Spacing")]
    [SerializeField] private float slotSpacing = 6f;

    [Header("Crosshair")]
    [SerializeField] private string idleCrosshairGlyph = ".";
    [SerializeField] private int crosshairFontSize = 26;
    [SerializeField] private int promptFontSize = 18;

    [Header("Crafting")]
    [SerializeField] private CraftingRecipeDatabase craftingDb;

    [Header("Smelter")]
    [SerializeField] private SmelterComponent smelter;

    private bool smelterOpen;
    private VisualElement smelterOverlay;

    private SlotView[] smelterBackpackViews;
    private SlotView smelterOreView;
    private SlotView smelterFuelView;
    private SlotView smelterOutputView;

    private Label smeltStackTimeLabel;
    private Label fuelTimeLabel;

    public bool IsSmelterOpen => smelterOpen;

    private struct RecipeStub
    {
        public string Name;
        public Texture2D Icon;
        public string[] Materials;
    }

    private bool IsAnyMenuOpen => backpackOpen || craftingOpen || smelterOpen;

    private bool craftingOpen;
    private VisualElement craftingOverlay;
    private VisualElement craftingInventoryGrid;
    private ScrollView recipeScroll;
    private VisualElement recipeList;

    private Image craftPreviewIcon;
    private Label craftRecipeNameLabel;
    private Label[] craftMaterialLabels;

    private CraftingRecipe selectedRecipe;
    private Button craftButton;

    private VisualElement selectedRecipeEntry;
    public bool IsCraftingOpen => craftingOpen;

    private VisualElement hotbarAnchor;
    private VisualElement hotbarPanel;

    public bool IsBackpackOpen => backpackOpen;

    private bool built;
    private bool triedBuildThisFrame;

    private VisualElement root;
    private SlotView[] craftingViews;

    private VisualElement hotbarHud;
    private SlotView[] hotbarViews;

    private VisualElement backpackOverlay;
    private VisualElement backpackGrid;
    private SlotView[] backpackViews;

    private VisualElement cursorRoot;
    private Image cursorIcon;
    private Label cursorQty;

    private VisualElement crosshairRoot;
    private Label crosshairLabel;

    private VisualElement tutorialRoot;
    private Label tutorialLabel;

    private bool backpackOpen;

    private readonly Dictionary<VisualElement, SlotView> slotLookup = new();
    private readonly Dictionary<VisualElement, CraftingRecipe> recipeLookup = new();

    private Vector2 _lastPanelPointerPos;
    private bool _hasPanelPointerPos;
    private bool _pointerHooked;

    private Vector2 _externalCursorPos;
    private bool _hasExternalCursorPos;

    public void SetCrosshairMessage(string message)
    {
        if (!built || crosshairLabel == null) return;

        string m = string.IsNullOrWhiteSpace(message) ? idleCrosshairGlyph : message.Trim();
        crosshairLabel.text = m;
        crosshairLabel.style.fontSize = promptFontSize;
    }

    private bool CanBuildNow()
    {
        if (uiDocument == null) return false;
        root = uiDocument.rootVisualElement;
        if (root == null) return false;

        if (playerInventory == null) return false;
        var m = playerInventory.Model;
        if (m == null) return false;
        if (m.Hotbar == null) return false;
        if (m.Backpack == null) return false;
        if (m.Cursor == null) return false;

        return true;
    }

    private void Awake()
    {
        if (craftingDb == null) craftingDb = FindFirstObjectByType<CraftingRecipeDatabase>();
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (playerInventory == null) playerInventory = FindFirstObjectByType<PlayerInventoryComponent>();
        if (smelter == null) smelter = FindFirstObjectByType<SmelterComponent>();
    }

    private void OnEnable()
    {
        built = false;
        triedBuildThisFrame = false;
    }

    private void OnDisable()
    {
        UnhookModelEvents();
        built = false;
        triedBuildThisFrame = false;
    }

    private void Update()
    {
        if (!built)
        {
            if (!triedBuildThisFrame && CanBuildNow())
            {
                triedBuildThisFrame = true;
                BuildUI();
                HookModelEvents();
                built = true;
                RefreshAll();
            }
            return;
        }

        if (!IsAnyMenuOpen) return;

        UpdateCursorVisual();
        if (smelterOpen) RefreshSmelterStatusText();
    }

    public void SetExternalCursorPosition(Vector2 panelPosition)
    {
        _externalCursorPos = panelPosition;
        _hasExternalCursorPos = true;
    }

    public void ClearExternalCursorPosition()
    {
        _hasExternalCursorPos = false;
    }

    public bool HandleGamepadLeftClickAt(Vector2 panelPosition)
    {
        if (!built || !IsAnyMenuOpen || root == null) return false;
        if (playerInventory == null || playerInventory.Model == null) return false;

        SetExternalCursorPosition(panelPosition);

        if (TryGetSlotViewAt(panelPosition, out var directSlot))
            return HandleLeftClickOnSlot(directSlot);

        var picked = root.panel?.Pick(panelPosition) as VisualElement;
        if (picked == null)
        {
            return TryDropCursorOutsideSafeAreas();
        }

        if (TryGetRecipeFromElement(picked, out var recipe, out var recipeEntry))
        {
            SelectRecipe(recipe, recipeEntry);
            return true;
        }

        if (TryGetSlotViewFromElement(picked, out var slotView))
        {
            return HandleLeftClickOnSlot(slotView);
        }

        Button button = FindAncestorButton(picked);
        if (button != null)
        {
            button.Focus();
            using var evt = NavigationSubmitEvent.GetPooled();
            evt.target = button;
            button.SendEvent(evt);
            return true;
        }

        return TryDropCursorOutsideSafeAreas(picked);
    }

    public bool HandleGamepadRightClickAt(Vector2 panelPosition)
    {
        if (!built || !IsAnyMenuOpen || root == null) return false;
        if (playerInventory == null || playerInventory.Model == null) return false;

        SetExternalCursorPosition(panelPosition);
        if (TryGetSlotViewAt(panelPosition, out var directSlot))
            return HandleRightClickOnSlot(directSlot);
        var picked = root.panel?.Pick(panelPosition) as VisualElement;
        if (picked == null) return false;

        if (TryGetSlotViewFromElement(picked, out var slotView))
        {
            return HandleRightClickOnSlot(slotView);
        }

        return false;
    }

    private bool HandleLeftClickOnSlot(SlotView view)
    {
        if (smelterOpen && smelter != null && view.Container == smelter.Container)
            return HandleSmelterSlotClick(view, leftClick: true);

        bool changed = InventoryRules.TryLeftClickSlot(
            playerInventory.Model.Cursor,
            view.Container,
            view.Index
        );

        if (changed)
            playerInventory.NotifyInventoryChanged();

        return changed;
    }

    private bool HandleRightClickOnSlot(SlotView view)
    {
        if (smelterOpen && smelter != null && view.Container == smelter.Container)
            return HandleSmelterSlotClick(view, leftClick: false);

        bool changed = InventoryRules.TryRightClickSlot(
            playerInventory.Model.Cursor,
            view.Container,
            view.Index,
            playerInventory.Model.Backpack
        );

        if (changed)
            playerInventory.NotifyInventoryChanged();

        return changed;
    }

    private bool HandleSmelterSlotClick(SlotView view, bool leftClick)
    {
        if (!smelterOpen || smelter == null || playerInventory == null || playerInventory.Model == null)
            return false;

        SmelterSlotType type;
        if (view.Index == SmelterComponent.OreSlot) type = SmelterSlotType.Ore;
        else if (view.Index == SmelterComponent.FuelSlot) type = SmelterSlotType.Fuel;
        else if (view.Index == SmelterComponent.OutputSlot) type = SmelterSlotType.Output;
        else return false;

        var cursor = playerInventory.Model.Cursor;

        bool changed = false;

        bool cursorHasItem = cursor != null && cursor.HasItem;
        ItemDefinition heldItem = cursorHasItem ? cursor.CursorStack.Item : null;

        bool CanAcceptHeldIntoThisSlot()
        {
            if (!cursorHasItem || heldItem == null) return false;
            if (type == SmelterSlotType.Output) return false;

            if (type == SmelterSlotType.Ore)
                return heldItem.IsOre && heldItem.SmeltResult != null && heldItem.SmeltSecondsPerItem > 0f;

            if (type == SmelterSlotType.Fuel)
                return heldItem.IsFuel && heldItem.FuelSeconds > 0f;

            return false;
        }

        if (leftClick)
        {
            if (!cursorHasItem)
            {
                changed = InventoryRules.TryPickUpStack(cursor, view.Container, view.Index);
            }
            else
            {
                if (!CanAcceptHeldIntoThisSlot())
                    return false;

                changed = InventoryRules.TryDropCursorStack(cursor, view.Container, view.Index);
            }
        }
        else
        {
            if (type == SmelterSlotType.Output)
            {
                if (!cursorHasItem)
                    changed = InventoryRules.TryPickUpStack(cursor, view.Container, view.Index);
                else
                    return false;
            }
            else
            {
                if (!cursorHasItem)
                {
                    changed = InventoryRules.TrySplitStackToBackpack(
                        view.Container,
                        view.Index,
                        playerInventory.Model.Backpack
                    );
                }
                else
                {
                    if (!CanAcceptHeldIntoThisSlot())
                        return false;

                    changed = InventoryRules.TryPlaceOneFromCursor(cursor, view.Container, view.Index);
                }
            }
        }

        if (changed)
        {
            playerInventory.NotifyInventoryChanged();
            smelter.NotifyChanged();
        }

        return changed;
    }

    private bool TryDropCursorOutsideSafeAreas(VisualElement picked = null)
    {
        if (playerInventory == null || playerInventory.Model == null) return false;
        if (!playerInventory.Model.Cursor.HasItem) return false;

        if (picked != null)
        {
            if (IsInsideInventorySlot(picked)) return false;
            if (IsInsideInventoryPanel(picked)) return false;
            if (IsInsideHotbarPanel(picked)) return false;
        }

        ItemStack dropped = InventoryRules.DropCursorToWorld(playerInventory.Model.Cursor);

        var spawner = FindFirstObjectByType<WorldItemSpawner>();
        if (spawner != null)
        {
            spawner.SpawnAtFeet(dropped, playerInventory.transform);
        }
        else
        {
            Debug.LogWarning("No WorldItemSpawner found in scene.");
        }

        playerInventory.NotifyInventoryChanged();
        return true;
    }

    private bool TryGetSlotViewFromElement(VisualElement ve, out SlotView view)
    {
        while (ve != null)
        {
            if (slotLookup.TryGetValue(ve, out view))
                return true;

            ve = ve.parent;
        }

        view = default;
        return false;
    }

    private bool TryGetRecipeFromElement(VisualElement ve, out CraftingRecipe recipe, out VisualElement recipeEntry)
    {
        while (ve != null)
        {
            if (recipeLookup.TryGetValue(ve, out recipe))
            {
                recipeEntry = FindAncestorWithClass(ve, "recipe-entry") ?? ve;
                return true;
            }

            ve = ve.parent;
        }

        recipe = null;
        recipeEntry = null;
        return false;
    }

    private static VisualElement FindAncestorWithClass(VisualElement ve, string className)
    {
        while (ve != null)
        {
            if (ve.ClassListContains(className))
                return ve;

            ve = ve.parent;
        }

        return null;
    }

    private static Button FindAncestorButton(VisualElement ve)
    {
        while (ve != null)
        {
            if (ve is Button b)
                return b;

            ve = ve.parent;
        }

        return null;
    }

    private void SelectRecipe(CraftingRecipe recipe, VisualElement entry)
    {
        if (recipe == null || entry == null) return;

        if (selectedRecipeEntry != null)
            SetBorder(selectedRecipeEntry, new Color(0, 0, 0, 0.75f));

        selectedRecipeEntry = entry;
        SetBorder(selectedRecipeEntry, new Color(1f, 0.9f, 0.2f, 1f));

        selectedRecipe = recipe;
        RefreshSelectedRecipeUI();
    }

    public void SetSmelterOpen(bool open)
    {
        smelterOpen = open;

        if (!built) return;

        if (smelterOverlay != null)
            smelterOverlay.style.display = smelterOpen ? DisplayStyle.Flex : DisplayStyle.None;

        SetCrosshairVisible(!(backpackOpen || craftingOpen || smelterOpen));

        if (!smelterOpen && playerInventory != null && playerInventory.Model != null)
            InventoryRules.CancelCursorToOrigin(playerInventory.Model.Cursor);

        RefreshAll();
    }

    public void SetBackpackOpen(bool open)
    {
        backpackOpen = open;
        if (!built) return;

        if (backpackOverlay != null)
            backpackOverlay.style.display = backpackOpen ? DisplayStyle.Flex : DisplayStyle.None;

        SetCrosshairVisible(!backpackOpen);

        if (!backpackOpen && playerInventory != null && playerInventory.Model != null)
            InventoryRules.CancelCursorToOrigin(playerInventory.Model.Cursor);

        RefreshAll();
    }

    public void SetCrosshairDefault()
    {
        if (!built || crosshairLabel == null) return;
        crosshairLabel.text = idleCrosshairGlyph;
        crosshairLabel.style.fontSize = crosshairFontSize;
    }

    public void SetCrosshairPrompt(string action)
    {
        if (!built || crosshairLabel == null) return;
        string a = string.IsNullOrWhiteSpace(action) ? "Interact" : action.Trim();
        crosshairLabel.text = $"E to {a}";
        crosshairLabel.style.fontSize = promptFontSize;
    }

    public void SetCrosshairVisible(bool visible)
    {
        if (!built || crosshairRoot == null) return;
        crosshairRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void HookModelEvents()
    {
        if (playerInventory == null) return;

        playerInventory.InventoryChanged -= RefreshAll;
        playerInventory.HotbarSelectionChanged -= OnHotbarSelectionChanged;

        playerInventory.InventoryChanged += RefreshAll;
        playerInventory.HotbarSelectionChanged += OnHotbarSelectionChanged;

        if (smelter != null)
        {
            smelter.SmelterChanged -= RefreshAll;
            smelter.SmelterChanged += RefreshAll;
        }
    }

    private void UnhookModelEvents()
    {
        if (playerInventory == null) return;
        playerInventory.InventoryChanged -= RefreshAll;
        playerInventory.HotbarSelectionChanged -= OnHotbarSelectionChanged;
        if (smelter != null)
            smelter.SmelterChanged -= RefreshAll;
    }

    private void OnHotbarSelectionChanged(int _) => RefreshHotbarSelection();

    private void BuildUI()
    {
        if (uiDocument == null) return;
        root = uiDocument.rootVisualElement;
        if (root == null) return;

        root.Clear();
        slotLookup.Clear();
        recipeLookup.Clear();
        _pointerHooked = false;
        _hasPanelPointerPos = false;

        root.style.position = Position.Relative;
        root.style.width = Length.Percent(100);
        root.style.height = Length.Percent(100);
        root.pickingMode = PickingMode.Ignore;

        BuildBackpackOverlay();
        BuildCraftingOverlay();
        BuildSmelterOverlay();
        BuildHotbarHud();
        BuildCursorVisual();
        BuildCrosshairHud();
        BuildTutorialHud();

        if (backpackOverlay != null)
            backpackOverlay.style.display = backpackOpen ? DisplayStyle.Flex : DisplayStyle.None;
        if (craftingOverlay != null)
            craftingOverlay.style.display = craftingOpen ? DisplayStyle.Flex : DisplayStyle.None;
        if (smelterOverlay != null)
            smelterOverlay.style.display = smelterOpen ? DisplayStyle.Flex : DisplayStyle.None;

        SetCrosshairVisible(!backpackOpen);
        SetCrosshairDefault();
    }

    public void SetTutorialText(string message)
    {
        if (!built || tutorialLabel == null) return;
        tutorialLabel.text = string.IsNullOrWhiteSpace(message) ? "" : message;
    }

    private void BuildTutorialHud()
    {
        tutorialRoot = new VisualElement();
        tutorialRoot.pickingMode = PickingMode.Ignore;

        tutorialRoot.style.position = Position.Absolute;
        tutorialRoot.style.left = 20;
        tutorialRoot.style.top = Length.Percent(30);
        tutorialRoot.style.width = 360;
        tutorialRoot.style.minHeight = 120;

        tutorialRoot.style.paddingLeft = 14;
        tutorialRoot.style.paddingRight = 14;
        tutorialRoot.style.paddingTop = 12;
        tutorialRoot.style.paddingBottom = 12;

        tutorialRoot.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
        tutorialRoot.style.borderTopLeftRadius = 8;
        tutorialRoot.style.borderTopRightRadius = 8;
        tutorialRoot.style.borderBottomLeftRadius = 8;
        tutorialRoot.style.borderBottomRightRadius = 8;

        tutorialRoot.style.borderTopWidth = 1;
        tutorialRoot.style.borderRightWidth = 1;
        tutorialRoot.style.borderBottomWidth = 1;
        tutorialRoot.style.borderLeftWidth = 1;

        tutorialRoot.style.borderTopColor = new Color(1f, 1f, 1f, 0.15f);
        tutorialRoot.style.borderRightColor = new Color(1f, 1f, 1f, 0.15f);
        tutorialRoot.style.borderBottomColor = new Color(1f, 1f, 1f, 0.15f);
        tutorialRoot.style.borderLeftColor = new Color(1f, 1f, 1f, 0.15f);

        root.Add(tutorialRoot);

        tutorialLabel = new Label();
        tutorialLabel.pickingMode = PickingMode.Ignore;
        tutorialLabel.style.whiteSpace = WhiteSpace.Normal;
        tutorialLabel.style.unityTextAlign = TextAnchor.UpperLeft;
        tutorialLabel.style.color = Color.white;
        tutorialLabel.style.fontSize = 16;
        tutorialLabel.style.flexGrow = 1;

        tutorialRoot.Add(tutorialLabel);
    }

    private void BuildSmelterOverlay()
    {
        if (playerInventory == null || playerInventory.Model == null) return;
        if (smelter == null || smelter.Container == null) return;

        var model = playerInventory.Model;

        smelterOverlay = new VisualElement();
        smelterOverlay.style.position = Position.Absolute;
        smelterOverlay.style.left = 0;
        smelterOverlay.style.top = 0;
        smelterOverlay.style.right = 0;
        smelterOverlay.style.bottom = 0;
        smelterOverlay.style.backgroundColor = new Color(0, 0, 0, 0.55f);
        smelterOverlay.style.justifyContent = Justify.Center;
        smelterOverlay.style.alignItems = Align.Center;

        root.Add(smelterOverlay);

        var panel = MakePanel();
        panel.style.width = 860;
        panel.style.height = 520;
        panel.style.justifyContent = Justify.Center;
        panel.style.alignItems = Align.Center;
        panel.style.paddingTop = 24;
        panel.style.paddingBottom = 28;

        smelterOverlay.Add(panel);

        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.style.justifyContent = Justify.Center;
        panel.Add(row);

        var leftPanel = MakePanel();
        leftPanel.style.width = 340;
        leftPanel.style.height = 440;
        leftPanel.style.marginRight = 18;
        leftPanel.RegisterCallback<PointerUpEvent>(OnPanelPointerUp, TrickleDown.TrickleDown);
        row.Add(leftPanel);

        var grid = new VisualElement();
        grid.style.flexDirection = FlexDirection.Row;
        grid.style.flexWrap = Wrap.Wrap;

        int cols = 5;
        int rows = 6;
        float gridWidth = cols * slotSize.x + (cols - 1) * slotSpacing;
        grid.style.width = gridWidth;

        leftPanel.Add(grid);

        int slotsToShow = Mathf.Min(cols * rows, model.Backpack.SlotCount);
        smelterBackpackViews = new SlotView[slotsToShow];

        for (int i = 0; i < slotsToShow; i++)
        {
            bool isLastInRow = ((i + 1) % cols) == 0;
            var v = CreateSlotView(model.Backpack, i, allowClicks: true, isLastInRow: isLastInRow);
            smelterBackpackViews[i] = v;
            grid.Add(v.Root);
        }

        var rightPanel = MakePanel();
        rightPanel.style.width = 420;
        rightPanel.style.height = 440;
        rightPanel.style.alignItems = Align.Center;
        rightPanel.style.justifyContent = Justify.Center;
        rightPanel.RegisterCallback<PointerUpEvent>(OnPanelPointerUp, TrickleDown.TrickleDown);
        row.Add(rightPanel);

        var topRow = new VisualElement();
        topRow.style.flexDirection = FlexDirection.Row;
        topRow.style.alignItems = Align.Center;
        topRow.style.justifyContent = Justify.Center;
        topRow.style.marginTop = 0;
        rightPanel.Add(topRow);

        smelterOreView = CreateSlotView(smelter.Container, SmelterComponent.OreSlot, allowClicks: false, isLastInRow: false);
        smelterOreView.Root.RegisterCallback<PointerDownEvent>(evt => OnSmelterSlotPointerDown(evt, smelterOreView, SmelterSlotType.Ore));
        topRow.Add(smelterOreView.Root);

        var arrow = new Label("->");
        arrow.style.fontSize = 30;
        arrow.style.unityTextAlign = TextAnchor.MiddleCenter;
        arrow.style.marginLeft = 14;
        arrow.style.marginRight = 14;
        arrow.style.color = new Color(0.9f, 0.9f, 0.2f, 1f);
        topRow.Add(arrow);

        smelterOutputView = CreateSlotView(smelter.Container, SmelterComponent.OutputSlot, allowClicks: false, isLastInRow: true);
        smelterOutputView.Root.RegisterCallback<PointerDownEvent>(evt => OnSmelterSlotPointerDown(evt, smelterOutputView, SmelterSlotType.Output));
        topRow.Add(smelterOutputView.Root);

        smeltStackTimeLabel = new Label("Time till stack is smelted: 00:00");
        smeltStackTimeLabel.style.marginTop = 14;
        smeltStackTimeLabel.style.color = Color.white;
        smeltStackTimeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        rightPanel.Add(smeltStackTimeLabel);

        var fuelRow = new VisualElement();
        fuelRow.style.flexDirection = FlexDirection.Row;
        fuelRow.style.alignItems = Align.Center;
        fuelRow.style.justifyContent = Justify.Center;
        fuelRow.style.marginTop = 20;
        rightPanel.Add(fuelRow);

        smelterFuelView = CreateSlotView(smelter.Container, SmelterComponent.FuelSlot, allowClicks: false, isLastInRow: true);
        smelterFuelView.Root.RegisterCallback<PointerDownEvent>(evt => OnSmelterSlotPointerDown(evt, smelterFuelView, SmelterSlotType.Fuel));
        fuelRow.Add(smelterFuelView.Root);

        fuelTimeLabel = new Label("Time left on fuel: 00:00");
        fuelTimeLabel.style.marginTop = 14;
        fuelTimeLabel.style.color = Color.white;
        fuelTimeLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        rightPanel.Add(fuelTimeLabel);

        smelterOverlay.style.display = smelterOpen ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnSmelterSlotPointerDown(PointerDownEvent evt, SlotView view, SmelterSlotType type)
    {
        if (!IsAnyMenuOpen) return;
        if (!smelterOpen) return;
        if (playerInventory == null || playerInventory.Model == null) return;

        var cursor = playerInventory.Model.Cursor;

        bool changed = false;

        bool cursorHasItem = cursor != null && cursor.HasItem;
        ItemDefinition heldItem = cursorHasItem ? cursor.CursorStack.Item : null;

        bool CanAcceptHeldIntoThisSlot()
        {
            if (!cursorHasItem || heldItem == null) return false;
            if (type == SmelterSlotType.Output) return false;

            if (type == SmelterSlotType.Ore)
                return heldItem.IsOre && heldItem.SmeltResult != null && heldItem.SmeltSecondsPerItem > 0f;

            if (type == SmelterSlotType.Fuel)
                return heldItem.IsFuel && heldItem.FuelSeconds > 0f;

            return false;
        }

        if (evt.button == (int)MouseButton.LeftMouse)
        {
            if (!cursorHasItem)
            {
                changed = InventoryRules.TryPickUpStack(cursor, view.Container, view.Index);
            }
            else
            {
                if (!CanAcceptHeldIntoThisSlot())
                    return;

                changed = InventoryRules.TryDropCursorStack(cursor, view.Container, view.Index);
            }
        }
        else if (evt.button == (int)MouseButton.RightMouse)
        {
            if (type == SmelterSlotType.Output)
            {
                if (!cursorHasItem)
                    changed = InventoryRules.TryPickUpStack(cursor, view.Container, view.Index);
                else
                    return;
            }
            else
            {
                if (!cursorHasItem)
                {
                    changed = InventoryRules.TrySplitStackToBackpack(view.Container, view.Index, playerInventory.Model.Backpack);
                }
                else
                {
                    if (!CanAcceptHeldIntoThisSlot())
                        return;

                    changed = InventoryRules.TryPlaceOneFromCursor(cursor, view.Container, view.Index);
                }
            }
        }

        if (changed)
        {
            playerInventory.NotifyInventoryChanged();
            smelter.NotifyChanged();
            evt.StopPropagation();
        }
    }

    private void RefreshSmelterStatusText()
    {
        if (!built || !smelterOpen || smelter == null) return;

        if (smelter.TryGetCurrentSmeltTimes(out float perItem, out float remainingThisItem))
        {
            smeltStackTimeLabel.text =
                $"Smelting: {FormatMMSS(remainingThisItem)}   (per item: {FormatMMSS(perItem)})";
        }
        else
        {
            smeltStackTimeLabel.text = "Smelting: 00:00   (per item: 00:00)";
        }

        float fuelTotal = smelter.GetTotalFuelTimeSeconds();
        fuelTimeLabel.text = $"Fuel time left: {FormatMMSS(fuelTotal)}";
    }

    private static string FormatMMSS(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int total = Mathf.FloorToInt(seconds);
        int mm = total / 60;
        int ss = total % 60;
        return $"{mm:00}:{ss:00}";
    }

    private enum SmelterSlotType { Ore, Fuel, Output }

    private void BuildCraftingOverlay()
    {
        var model = playerInventory?.Model;
        if (model == null) return;

        craftingOverlay = new VisualElement();
        craftingOverlay.style.position = Position.Absolute;
        craftingOverlay.style.left = 0;
        craftingOverlay.style.top = 0;
        craftingOverlay.style.right = 0;
        craftingOverlay.style.bottom = 0;
        craftingOverlay.style.backgroundColor = new Color(0, 0, 0, 0.55f);
        craftingOverlay.style.justifyContent = Justify.Center;
        craftingOverlay.style.alignItems = Align.Center;

        root.Add(craftingOverlay);
        craftingOverlay.RegisterCallback<PointerUpEvent>(OnOverlayPointerUp, TrickleDown.TrickleDown);

        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.Center;
        row.style.alignItems = Align.Center;
        craftingOverlay.Add(row);

        var leftPanel = MakePanel();
        leftPanel.style.marginRight = 18;
        leftPanel.RegisterCallback<PointerUpEvent>(OnPanelPointerUp, TrickleDown.TrickleDown);
        row.Add(leftPanel);

        craftingInventoryGrid = new VisualElement();
        craftingInventoryGrid.style.flexDirection = FlexDirection.Row;
        craftingInventoryGrid.style.flexWrap = Wrap.Wrap;

        int craftCols = 5;
        int craftRows = 6;

        float gridWidth = craftCols * slotSize.x + (craftCols - 1) * slotSpacing;
        craftingInventoryGrid.style.width = gridWidth;

        leftPanel.Add(craftingInventoryGrid);

        int slotsToShow = Mathf.Min(craftCols * craftRows, model.Backpack.SlotCount);
        craftingViews = new SlotView[slotsToShow];

        for (int i = 0; i < slotsToShow; i++)
        {
            bool isLastInRow = ((i + 1) % craftCols) == 0;
            var v = CreateSlotView(model.Backpack, i, allowClicks: true, isLastInRow: isLastInRow);

            craftingViews[i] = v;
            craftingInventoryGrid.Add(v.Root);
        }

        var midPanel = MakePanel();
        midPanel.style.width = 260;
        midPanel.style.alignItems = Align.Center;
        midPanel.style.marginRight = 18;
        midPanel.RegisterCallback<PointerUpEvent>(OnPanelPointerUp, TrickleDown.TrickleDown);
        row.Add(midPanel);

        var previewBox = new VisualElement();
        previewBox.style.width = 120;
        previewBox.style.height = 120;
        previewBox.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        previewBox.style.borderTopWidth = 2;
        previewBox.style.borderRightWidth = 2;
        previewBox.style.borderBottomWidth = 2;
        previewBox.style.borderLeftWidth = 2;
        SetBorder(previewBox, new Color(0, 0, 0, 0.75f));
        midPanel.Add(previewBox);

        craftPreviewIcon = new Image();
        craftPreviewIcon.style.width = Length.Percent(100);
        craftPreviewIcon.style.height = Length.Percent(100);
        craftPreviewIcon.scaleMode = ScaleMode.ScaleToFit;
        previewBox.Add(craftPreviewIcon);

        craftRecipeNameLabel = new Label("Select a recipe");
        craftRecipeNameLabel.style.marginTop = 10;
        craftRecipeNameLabel.style.unityTextAlign = TextAnchor.UpperCenter;
        craftRecipeNameLabel.style.color = Color.white;
        midPanel.Add(craftRecipeNameLabel);

        var matsContainer = new VisualElement();
        matsContainer.style.marginTop = 8;
        matsContainer.style.alignSelf = Align.Stretch;
        midPanel.Add(matsContainer);

        craftMaterialLabels = new Label[5];
        for (int i = 0; i < craftMaterialLabels.Length; i++)
        {
            var l = new Label("");
            l.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            l.style.unityTextAlign = TextAnchor.UpperLeft;
            l.style.marginTop = 2;
            matsContainer.Add(l);
            craftMaterialLabels[i] = l;
        }

        craftButton = new Button(OnCraftPressed);
        craftButton.text = "Craft";
        craftButton.style.marginTop = 14;
        craftButton.style.width = 160;
        craftButton.SetEnabled(false);
        midPanel.Add(craftButton);

        var rightPanel = MakePanel();
        rightPanel.style.width = 320;
        rightPanel.style.height = 360;
        rightPanel.RegisterCallback<PointerUpEvent>(OnPanelPointerUp, TrickleDown.TrickleDown);
        row.Add(rightPanel);

        recipeScroll = new ScrollView(ScrollViewMode.Vertical);
        recipeScroll.style.flexGrow = 1;
        rightPanel.Add(recipeScroll);

        recipeList = new VisualElement();
        recipeList.style.flexDirection = FlexDirection.Column;
        recipeScroll.Add(recipeList);

        recipeList.Clear();

        if (craftingDb == null || craftingDb.Recipes == null)
        {
            Debug.LogWarning("CraftingRecipeDatabase not set (craftingDb) or Recipes is null.");
        }
        else
        {
            foreach (var r in craftingDb.Recipes)
            {
                if (r != null)
                    recipeList.Add(MakeRecipeEntry(r));
            }
        }

        craftingOverlay.style.display = craftingOpen ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnCraftPressed()
    {
        if (playerInventory == null || playerInventory.Model == null) return;
        if (selectedRecipe == null) return;

        if (playerInventory.Model.Cursor.HasItem) return;

        var hotbar = playerInventory.Model.Hotbar;
        var backpack = playerInventory.Model.Backpack;

        foreach (var ing in selectedRecipe.Ingredients)
        {
            if (ing.Item == null || ing.Amount <= 0) continue;
            int have = InventoryRules.CountItem(hotbar, backpack, ing.Item);
            if (have < ing.Amount) return;
        }

        foreach (var ing in selectedRecipe.Ingredients)
        {
            if (ing.Item == null || ing.Amount <= 0) continue;
            InventoryRules.TryConsume(hotbar, backpack, ing.Item, ing.Amount);
        }

        var outItem = selectedRecipe.OutputItem;
        int outAmt = Mathf.Max(1, selectedRecipe.OutputAmount);
        int rem = InventoryRules.TryAutoAdd(outItem, outAmt, hotbar, backpack);

        playerInventory.NotifyInventoryChanged();
        RefreshSelectedRecipeUI();
    }

    private VisualElement MakePanel()
    {
        var panel = new VisualElement();
        panel.AddToClassList("inv-panel");
        panel.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        panel.style.paddingLeft = 16;
        panel.style.paddingRight = 16;
        panel.style.paddingTop = 16;
        panel.style.paddingBottom = 16;
        panel.style.borderTopLeftRadius = 10;
        panel.style.borderTopRightRadius = 10;
        panel.style.borderBottomLeftRadius = 10;
        panel.style.borderBottomRightRadius = 10;
        return panel;
    }

    private VisualElement MakeRecipeEntry(CraftingRecipe recipe)
    {
        var entry = new VisualElement();
        entry.AddToClassList("recipe-entry");
        entry.style.flexDirection = FlexDirection.Row;
        entry.style.alignItems = Align.Center;
        entry.style.height = 64;
        entry.style.marginBottom = 10;

        entry.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        entry.style.borderTopWidth = 2;
        entry.style.borderRightWidth = 2;
        entry.style.borderBottomWidth = 2;
        entry.style.borderLeftWidth = 2;
        SetBorder(entry, new Color(0, 0, 0, 0.75f));

        var icon = new Image();
        icon.scaleMode = ScaleMode.ScaleToFit;
        icon.style.width = 48;
        icon.style.height = 48;
        icon.style.marginLeft = 8;
        icon.image = recipe != null && recipe.OutputItem != null && recipe.OutputItem.Icon != null
            ? recipe.OutputItem.Icon.texture
            : null;
        entry.Add(icon);

        var label = new Label(recipe != null ? recipe.DisplayName : "Recipe");
        label.style.color = Color.white;
        label.style.unityTextAlign = TextAnchor.MiddleLeft;
        label.style.marginLeft = 10;
        entry.Add(label);

        recipeLookup[entry] = recipe;
        recipeLookup[icon] = recipe;
        recipeLookup[label] = recipe;

        entry.RegisterCallback<PointerDownEvent>(_ =>
        {
            SelectRecipe(recipe, entry);
        });

        return entry;
    }

    private void RefreshSelectedRecipeUI()
    {
        if (craftRecipeNameLabel == null || craftPreviewIcon == null || craftMaterialLabels == null) return;

        if (selectedRecipe == null)
        {
            craftRecipeNameLabel.text = "Select a recipe";
            craftPreviewIcon.image = null;
            for (int i = 0; i < craftMaterialLabels.Length; i++) craftMaterialLabels[i].text = "";
            craftButton?.SetEnabled(false);
            return;
        }

        craftRecipeNameLabel.text = string.IsNullOrWhiteSpace(selectedRecipe.DisplayName)
            ? selectedRecipe.name
            : selectedRecipe.DisplayName;

        craftPreviewIcon.image =
            selectedRecipe.OutputItem != null && selectedRecipe.OutputItem.Icon != null
                ? selectedRecipe.OutputItem.Icon.texture
                : null;

        var hotbar = playerInventory.Model.Hotbar;
        var backpack = playerInventory.Model.Backpack;

        bool canCraft = true;

        for (int i = 0; i < craftMaterialLabels.Length; i++)
        {
            if (selectedRecipe.Ingredients == null || i >= selectedRecipe.Ingredients.Length)
            {
                craftMaterialLabels[i].text = "";
                continue;
            }

            var ing = selectedRecipe.Ingredients[i];
            if (ing.Item == null || ing.Amount <= 0)
            {
                craftMaterialLabels[i].text = "";
                continue;
            }

            int have = InventoryRules.CountItem(hotbar, backpack, ing.Item);
            int need = ing.Amount;

            craftMaterialLabels[i].text = $"{ing.Item.ItemId} {have}/{need}";
            if (have < need) canCraft = false;
        }

        craftButton?.SetEnabled(canCraft);
    }

    private static void SetBorder(VisualElement ve, Color c)
    {
        ve.style.borderTopColor = c;
        ve.style.borderRightColor = c;
        ve.style.borderBottomColor = c;
        ve.style.borderLeftColor = c;
    }

    public void SetCraftingOpen(bool open)
    {
        craftingOpen = open;
        if (!built) return;

        if (craftingOverlay != null)
            craftingOverlay.style.display = craftingOpen ? DisplayStyle.Flex : DisplayStyle.None;

        SetCrosshairVisible(!(backpackOpen || craftingOpen || smelterOpen));

        if (!craftingOpen && playerInventory != null && playerInventory.Model != null)
            InventoryRules.CancelCursorToOrigin(playerInventory.Model.Cursor);

        RefreshAll();
    }

    private void BuildCrosshairHud()
    {
        crosshairRoot = new VisualElement();
        crosshairRoot.pickingMode = PickingMode.Ignore;

        crosshairRoot.style.position = Position.Absolute;
        crosshairRoot.style.left = 0;
        crosshairRoot.style.right = 0;
        crosshairRoot.style.top = 0;
        crosshairRoot.style.bottom = 0;

        crosshairRoot.style.justifyContent = Justify.Center;
        crosshairRoot.style.alignItems = Align.Center;

        root.Add(crosshairRoot);

        crosshairLabel = new Label(idleCrosshairGlyph);
        crosshairLabel.pickingMode = PickingMode.Ignore;

        crosshairLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        crosshairLabel.style.whiteSpace = WhiteSpace.Normal;
        crosshairLabel.style.color = Color.white;
        crosshairLabel.style.backgroundColor = Color.clear;
        crosshairLabel.style.fontSize = crosshairFontSize;

        crosshairRoot.Add(crosshairLabel);
    }

    private void BuildHotbarHud()
    {
        hotbarAnchor = new VisualElement();
        hotbarAnchor.style.position = Position.Absolute;
        hotbarAnchor.style.left = 0;
        hotbarAnchor.style.right = 0;
        hotbarAnchor.style.bottom = 18;
        hotbarAnchor.style.justifyContent = Justify.Center;
        hotbarAnchor.style.alignItems = Align.Center;

        root.Add(hotbarAnchor);

        hotbarPanel = new VisualElement();
        hotbarPanel.AddToClassList("hotbar-panel");
        hotbarPanel.style.flexDirection = FlexDirection.Row;
        hotbarPanel.style.justifyContent = Justify.Center;
        hotbarPanel.style.alignItems = Align.Center;
        hotbarPanel.style.paddingLeft = 10;
        hotbarPanel.style.paddingRight = 10;
        hotbarPanel.style.paddingTop = 10;
        hotbarPanel.style.paddingBottom = 10;
        hotbarPanel.style.backgroundColor = new Color(0, 0, 0, 0.0f);

        hotbarAnchor.Add(hotbarPanel);

        hotbarHud = new VisualElement();
        hotbarHud.style.flexDirection = FlexDirection.Row;
        hotbarHud.style.flexWrap = Wrap.NoWrap;

        var model = playerInventory.Model;
        int hotbarCount = model.Hotbar.SlotCount;

        hotbarHud.style.width = hotbarCount * slotSize.x + (hotbarCount - 1) * slotSpacing;

        hotbarPanel.Add(hotbarHud);

        hotbarViews = new SlotView[hotbarCount];
        for (int i = 0; i < hotbarCount; i++)
        {
            bool isLastInRow = (i == hotbarCount - 1);
            var v = CreateSlotView(model.Hotbar, i, allowClicks: true, isLastInRow: isLastInRow);

            hotbarViews[i] = v;
            hotbarHud.Add(v.Root);
        }
    }

    private static bool IsInsideHotbarPanel(VisualElement ve)
    {
        while (ve != null)
        {
            if (ve.ClassListContains("hotbar-panel"))
                return true;
            ve = ve.parent;
        }
        return false;
    }

    private void BuildBackpackOverlay()
    {
        var model = playerInventory.Model;
        if (model == null || model.Backpack == null || model.Cursor == null) return;

        backpackOverlay = new VisualElement();
        backpackOverlay.style.position = Position.Absolute;
        backpackOverlay.style.left = 0;
        backpackOverlay.style.top = 0;
        backpackOverlay.style.right = 0;
        backpackOverlay.style.bottom = 0;
        backpackOverlay.style.backgroundColor = new Color(0, 0, 0, 0.55f);
        backpackOverlay.style.justifyContent = Justify.Center;
        backpackOverlay.style.alignItems = Align.Center;
        root.Add(backpackOverlay);

        var panel = new VisualElement();
        panel.AddToClassList("inv-panel");
        panel.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        panel.style.paddingLeft = 16;
        panel.style.paddingRight = 16;
        panel.style.paddingTop = 16;
        panel.style.paddingBottom = 16;
        panel.style.borderTopLeftRadius = 10;
        panel.style.borderTopRightRadius = 10;
        panel.style.borderBottomLeftRadius = 10;
        panel.style.borderBottomRightRadius = 10;
        backpackOverlay.Add(panel);

        backpackGrid = new VisualElement();
        backpackGrid.style.flexDirection = FlexDirection.Row;
        backpackGrid.style.flexWrap = Wrap.Wrap;

        float gridWidth = backpackColumns * slotSize.x + (backpackColumns - 1) * slotSpacing;
        backpackGrid.style.width = gridWidth;
        panel.Add(backpackGrid);

        int count = model.Backpack.SlotCount;
        backpackViews = new SlotView[count];

        for (int i = 0; i < count; i++)
        {
            bool isLastInRow = ((i + 1) % backpackColumns) == 0;
            bool isLastSlot = i == count - 1;

            var v = CreateSlotView(model.Backpack, i, allowClicks: true, isLastInRow: (isLastInRow || isLastSlot));
            backpackViews[i] = v;
            backpackGrid.Add(v.Root);
        }

        backpackOverlay.RegisterCallback<PointerUpEvent>(OnOverlayPointerUp, TrickleDown.TrickleDown);
        panel.RegisterCallback<PointerUpEvent>(OnPanelPointerUp, TrickleDown.TrickleDown);
    }

    private void OnPanelPointerUp(PointerUpEvent evt)
    {
        if (!IsAnyMenuOpen) return;
        if (playerInventory == null || playerInventory.Model == null) return;
        if (!playerInventory.Model.Cursor.HasItem) return;

        evt.StopPropagation();
    }

    private void BuildCursorVisual()
    {
        cursorRoot = new VisualElement();
        cursorRoot.pickingMode = PickingMode.Ignore;
        cursorRoot.style.position = Position.Absolute;
        cursorRoot.style.left = 0;
        cursorRoot.style.top = 0;
        cursorRoot.style.width = 9999;
        cursorRoot.style.height = 9999;
        root.Add(cursorRoot);

        var cursorContainer = new VisualElement();
        cursorContainer.name = "CursorContainer";
        cursorContainer.pickingMode = PickingMode.Ignore;
        cursorContainer.style.position = Position.Absolute;
        cursorContainer.style.width = slotSize.x;
        cursorContainer.style.height = slotSize.y;
        cursorRoot.Add(cursorContainer);

        cursorIcon = new Image();
        cursorIcon.pickingMode = PickingMode.Ignore;
        cursorIcon.style.width = Length.Percent(100);
        cursorIcon.style.height = Length.Percent(100);
        cursorIcon.scaleMode = ScaleMode.ScaleToFit;
        cursorContainer.Add(cursorIcon);

        cursorQty = new Label();
        cursorQty.pickingMode = PickingMode.Ignore;
        cursorQty.style.position = Position.Absolute;
        cursorQty.style.right = 4;
        cursorQty.style.bottom = 2;
        cursorQty.style.unityTextAlign = TextAnchor.LowerRight;
        cursorQty.style.fontSize = 14;
        cursorQty.style.color = Color.white;
        cursorQty.style.backgroundColor = new Color(0, 0, 0, 0.55f);
        cursorQty.style.paddingLeft = 4;
        cursorQty.style.paddingRight = 4;
        cursorQty.style.paddingTop = 1;
        cursorQty.style.paddingBottom = 1;
        cursorContainer.Add(cursorQty);
    }

    private SlotView CreateSlotView(IItemContainer container, int index, bool allowClicks, bool isLastInRow = false)
    {
        var slot = new VisualElement();
        slot.AddToClassList("inv-slot");

        slot.style.width = slotSize.x;
        slot.style.height = slotSize.y;
        slot.style.marginRight = isLastInRow ? 0f : slotSpacing;
        slot.style.marginBottom = slotSpacing;

        slot.style.borderTopWidth = 2;
        slot.style.borderRightWidth = 2;
        slot.style.borderBottomWidth = 2;
        slot.style.borderLeftWidth = 2;

        slot.style.borderTopColor = new Color(0, 0, 0, 0.75f);
        slot.style.borderRightColor = new Color(0, 0, 0, 0.75f);
        slot.style.borderBottomColor = new Color(0, 0, 0, 0.75f);
        slot.style.borderLeftColor = new Color(0, 0, 0, 0.75f);

        slot.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        slot.style.position = Position.Relative;

        var icon = new Image();
        icon.style.width = Length.Percent(100);
        icon.style.height = Length.Percent(100);
        icon.scaleMode = ScaleMode.ScaleToFit;
        slot.Add(icon);

        var qty = new Label();
        qty.style.position = Position.Absolute;
        qty.style.right = 4;
        qty.style.bottom = 2;
        qty.style.unityTextAlign = TextAnchor.LowerRight;
        qty.style.fontSize = 14;
        qty.style.color = Color.white;
        qty.style.backgroundColor = new Color(0, 0, 0, 0.55f);
        qty.style.paddingLeft = 4;
        qty.style.paddingRight = 4;
        qty.style.paddingTop = 1;
        qty.style.paddingBottom = 1;
        slot.Add(qty);

        var view = new SlotView(container, index, slot, icon, qty);

        slotLookup[slot] = view;
        slotLookup[icon] = view;
        slotLookup[qty] = view;

        slot.RegisterCallback<PointerEnterEvent>(_ => slot.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f));
        slot.RegisterCallback<PointerLeaveEvent>(_ => slot.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f));

        if (allowClicks)
            slot.RegisterCallback<PointerDownEvent>(evt => OnSlotPointerDown(evt, view));

        return view;
    }

    private void OnSlotPointerDown(PointerDownEvent evt, SlotView view)
    {
        Debug.Log($"Hotbar/Slot pointer down: {view.Container} idx {view.Index} btn {evt.button}");
        if (!IsAnyMenuOpen) return;
        if (playerInventory == null || playerInventory.Model == null) return;

        bool changed = false;

        if (evt.button == (int)MouseButton.LeftMouse)
            changed = InventoryRules.TryLeftClickSlot(playerInventory.Model.Cursor, view.Container, view.Index);
        else if (evt.button == (int)MouseButton.RightMouse)
            changed = InventoryRules.TryRightClickSlot(playerInventory.Model.Cursor, view.Container, view.Index, playerInventory.Model.Backpack);

        if (changed)
        {
            playerInventory.NotifyInventoryChanged();
            evt.StopPropagation();
        }
    }

    private void OnOverlayPointerUp(PointerUpEvent evt)
    {
        if (!IsAnyMenuOpen) return;
        if (playerInventory == null || playerInventory.Model == null) return;
        if (!playerInventory.Model.Cursor.HasItem) return;

        var picked = root?.panel?.Pick(evt.position) as VisualElement;

        if (IsInsideInventorySlot(picked))
            return;

        if (IsInsideInventoryPanel(picked))
            return;

        if (IsInsideHotbarPanel(picked))
            return;

        ItemStack dropped = InventoryRules.DropCursorToWorld(playerInventory.Model.Cursor);

        var spawner = FindFirstObjectByType<WorldItemSpawner>();
        if (spawner != null)
        {
            spawner.SpawnAtFeet(dropped, playerInventory.transform);
        }
        else
        {
            Debug.LogWarning("No WorldItemSpawner found in scene.");
        }

        playerInventory.NotifyInventoryChanged();
    }

    private static bool IsInsideInventoryPanel(VisualElement ve)
    {
        while (ve != null)
        {
            if (ve.ClassListContains("inv-panel")) return true;
            ve = ve.parent;
        }
        return false;
    }

    private static bool IsInsideInventorySlot(VisualElement ve)
    {
        while (ve != null)
        {
            if (ve.ClassListContains("inv-slot")) return true;
            ve = ve.parent;
        }
        return false;
    }

    private void RefreshAll()
    {
        RefreshSelectedRecipeUI();
        if (!built) return;
        if (playerInventory == null || playerInventory.Model == null) return;

        if (hotbarViews != null)
            for (int i = 0; i < hotbarViews.Length; i++)
                if (hotbarViews[i].IsValid)
                    RefreshSlot(hotbarViews[i]);

        if (backpackViews != null)
            for (int i = 0; i < backpackViews.Length; i++)
                if (backpackViews[i].IsValid)
                    RefreshSlot(backpackViews[i]);

        if (craftingViews != null)
            for (int i = 0; i < craftingViews.Length; i++)
                if (craftingViews[i].IsValid)
                    RefreshSlot(craftingViews[i]);

        if (smelterOreView.IsValid) RefreshSlot(smelterOreView);
        if (smelterFuelView.IsValid) RefreshSlot(smelterFuelView);
        if (smelterOutputView.IsValid) RefreshSlot(smelterOutputView);

        if (smelterBackpackViews != null)
            for (int i = 0; i < smelterBackpackViews.Length; i++)
                if (smelterBackpackViews[i].IsValid)
                    RefreshSlot(smelterBackpackViews[i]);

        RefreshHotbarSelection();
        UpdateCursorVisual(force: true);
    }

    private void RefreshSlot(SlotView v)
    {
        ItemStack s = v.Container.GetSlot(v.Index);

        if (s.IsEmpty)
        {
            v.Icon.image = null;
            v.Qty.text = "";
        }
        else
        {
            v.Icon.image = s.Item.Icon != null ? s.Item.Icon.texture : null;
            v.Qty.text = s.Quantity > 1 ? s.Quantity.ToString() : "";
        }
    }

    private void RefreshHotbarSelection()
    {
        if (!built) return;
        if (playerInventory == null || hotbarViews == null) return;

        int selected = playerInventory.SelectedHotbarIndex;

        for (int i = 0; i < hotbarViews.Length; i++)
        {
            if (!hotbarViews[i].IsValid) continue;

            Color c = (i == selected) ? new Color(1f, 0.9f, 0.2f, 1f) : new Color(0, 0, 0, 0.75f);

            hotbarViews[i].Root.style.borderTopColor = c;
            hotbarViews[i].Root.style.borderRightColor = c;
            hotbarViews[i].Root.style.borderBottomColor = c;
            hotbarViews[i].Root.style.borderLeftColor = c;
        }
    }

    private void UpdateCursorVisual(bool force = false)
    {
        if (!built || cursorRoot == null || root == null) return;

        var cursorContainer = cursorRoot.Q<VisualElement>("CursorContainer");
        if (cursorContainer == null) return;

        if (!IsAnyMenuOpen || playerInventory == null || playerInventory.Model == null)
        {
            cursorContainer.style.display = DisplayStyle.None;
            return;
        }

        if (!_pointerHooked)
        {
            _pointerHooked = true;

            root.RegisterCallback<PointerMoveEvent>(e =>
            {
                _lastPanelPointerPos = e.position;
                _hasPanelPointerPos = true;
            }, TrickleDown.TrickleDown);

            root.RegisterCallback<PointerDownEvent>(e =>
            {
                _lastPanelPointerPos = e.position;
                _hasPanelPointerPos = true;
            }, TrickleDown.TrickleDown);
        }

        Vector2 cursorPos;

        if (_hasPanelPointerPos && !_hasExternalCursorPos)
        {
            cursorPos = _lastPanelPointerPos;
        }
        else if (_hasExternalCursorPos)
        {
            cursorPos = _externalCursorPos;
        }
        else if (_hasPanelPointerPos)
        {
            cursorPos = _lastPanelPointerPos;
        }
        else
        {
            cursorContainer.style.display = DisplayStyle.None;
            return;
        }

        cursorContainer.style.left = cursorPos.x - slotSize.x * 0.5f;
        cursorContainer.style.top = cursorPos.y - slotSize.y * 0.5f;

        if (!playerInventory.Model.Cursor.HasItem)
        {
            cursorIcon.image = null;
            cursorQty.text = "";
            cursorContainer.style.display = DisplayStyle.None;
            return;
        }

        cursorContainer.style.display = DisplayStyle.Flex;

        ItemStack held = playerInventory.Model.Cursor.CursorStack;
        cursorIcon.image = held.Item.Icon != null ? held.Item.Icon.texture : null;
        cursorQty.text = held.Quantity > 1 ? held.Quantity.ToString() : "";
    }

    private readonly struct SlotView
    {
        public readonly IItemContainer Container;
        public readonly int Index;
        public readonly VisualElement Root;
        public readonly Image Icon;
        public readonly Label Qty;

        public bool IsValid => Container != null && Root != null && Icon != null && Qty != null;

        public SlotView(IItemContainer container, int index, VisualElement root, Image icon, Label qty)
        {
            Container = container;
            Index = index;
            Root = root;
            Icon = icon;
            Qty = qty;
        }
    }

    //gamepad bs



    private static bool TryHitSlotArray(SlotView[] views, Vector2 panelPosition, out SlotView hit)
    {
        if (views != null)
        {
            for (int i = 0; i < views.Length; i++)
            {
                if (!views[i].IsValid) continue;
                if (views[i].Root.resolvedStyle.display == DisplayStyle.None) continue;

                if (views[i].Root.worldBound.Contains(panelPosition))
                {
                    hit = views[i];
                    return true;
                }
            }
        }

        hit = default;
        return false;
    }

    private bool TryGetSlotViewAt(Vector2 panelPosition, out SlotView hit)
    {
        if (TryHitSlotArray(hotbarViews, panelPosition, out hit)) return true;

        if (backpackOpen && TryHitSlotArray(backpackViews, panelPosition, out hit)) return true;
        if (craftingOpen && TryHitSlotArray(craftingViews, panelPosition, out hit)) return true;
        if (smelterOpen && TryHitSlotArray(smelterBackpackViews, panelPosition, out hit)) return true;

        if (smelterOpen)
        {
            if (smelterOreView.IsValid && smelterOreView.Root.worldBound.Contains(panelPosition))
            {
                hit = smelterOreView;
                return true;
            }

            if (smelterFuelView.IsValid && smelterFuelView.Root.worldBound.Contains(panelPosition))
            {
                hit = smelterFuelView;
                return true;
            }

            if (smelterOutputView.IsValid && smelterOutputView.Root.worldBound.Contains(panelPosition))
            {
                hit = smelterOutputView;
                return true;
            }
        }

        hit = default;
        return false;
    }



}