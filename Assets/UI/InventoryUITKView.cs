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

    private struct RecipeStub
    {
        public string Name;
        public Texture2D Icon;        // UI Toolkit Image uses Texture2D
        public string[] Materials;    // up to 4 strings
    }


    private bool IsAnyMenuOpen => backpackOpen || craftingOpen;

    // Crafting overlay
    private bool craftingOpen;
    private VisualElement craftingOverlay;
    private VisualElement craftingInventoryGrid; // left grid container (5x6)
    private ScrollView recipeScroll;
    private VisualElement recipeList;

    // Crafting UI - middle panel bits
    private Image craftPreviewIcon;
    private Label craftRecipeNameLabel;
    private Label[] craftMaterialLabels; // size 4

    // Temporary recipe data
    //private RecipeStub[] recipeStubs;
    //private RecipeStub selectedRecipe;
    private CraftingRecipe selectedRecipe;
    private Button craftButton;


    // optional: selected recipe highlight
    private VisualElement selectedRecipeEntry;
    public bool IsCraftingOpen => craftingOpen;


    private VisualElement hotbarAnchor;
    private VisualElement hotbarPanel;

    public bool IsBackpackOpen => backpackOpen;

    private bool built;
    private bool triedBuildThisFrame;

    private VisualElement root;
    // Crafting overlay
    private SlotView[] craftingViews;



    // Always-visible hotbar HUD
    private VisualElement hotbarHud;
    private SlotView[] hotbarViews;

    // Backpack overlay
    private VisualElement backpackOverlay;
    private VisualElement backpackGrid;
    private SlotView[] backpackViews;

    // Cursor visual
    private VisualElement cursorRoot;
    private Image cursorIcon;
    private Label cursorQty;

    // Crosshair UI (same UIDocument)
    private VisualElement crosshairRoot;
    private Label crosshairLabel;

    private bool backpackOpen;

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
    }

    public void SetBackpackOpen(bool open)
    {
        backpackOpen = open;
        if (!built) return;

        if (backpackOverlay != null)
            backpackOverlay.style.display = backpackOpen ? DisplayStyle.Flex : DisplayStyle.None;

        // Hide crosshair whenever inventory/backpack is open
        SetCrosshairVisible(!backpackOpen);

        if (!backpackOpen && playerInventory != null && playerInventory.Model != null)
            InventoryRules.CancelCursorToOrigin(playerInventory.Model.Cursor);

        RefreshAll();
    }

    // Called by PlayerInteractor
    public void SetCrosshairDefault()
    {
        if (!built || crosshairLabel == null) return;
        crosshairLabel.text = idleCrosshairGlyph;
        crosshairLabel.style.fontSize = crosshairFontSize;
    }

    // Called by PlayerInteractor
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
    }

    private void UnhookModelEvents()
    {
        if (playerInventory == null) return;
        playerInventory.InventoryChanged -= RefreshAll;
        playerInventory.HotbarSelectionChanged -= OnHotbarSelectionChanged;
    }

    private void OnHotbarSelectionChanged(int _) => RefreshHotbarSelection();

    private void BuildUI()
    {
        if (uiDocument == null) return;
        root = uiDocument.rootVisualElement;
        if (root == null) return;

        root.Clear();

        root.style.position = Position.Relative;
        root.style.width = Length.Percent(100);
        root.style.height = Length.Percent(100);
        root.pickingMode = PickingMode.Ignore;

        
        
        BuildBackpackOverlay();
        BuildHotbarHud();
        BuildCraftingOverlay();
        BuildCursorVisual();
        BuildCrosshairHud();  

        if (backpackOverlay != null)
            backpackOverlay.style.display = backpackOpen ? DisplayStyle.Flex : DisplayStyle.None;
        if (craftingOverlay != null)
            craftingOverlay.style.display = craftingOpen ? DisplayStyle.Flex : DisplayStyle.None;

        SetCrosshairVisible(!backpackOpen);
        SetCrosshairDefault();
    }


    private void BuildCraftingOverlay()
    {
        var model = playerInventory?.Model;
        if (model == null) return;

        // Full-screen dark overlay
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

        // Center row that holds the three panels
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.Center;
        row.style.alignItems = Align.Center;
        craftingOverlay.Add(row);

        // -------- LEFT PANEL (5x6 grid) --------
        var leftPanel = MakePanel();
        leftPanel.style.marginRight = 18; // replaces row gap
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

        // Inventory slots
        int slotsToShow = Mathf.Min(craftCols * craftRows, model.Backpack.SlotCount);
        craftingViews = new SlotView[slotsToShow];

        for (int i = 0; i < slotsToShow; i++)
        {
            bool isLastInRow = ((i + 1) % craftCols) == 0;
            var v = CreateSlotView(model.Backpack, i, allowClicks: true, isLastInRow: isLastInRow);

            craftingViews[i] = v;
            craftingInventoryGrid.Add(v.Root);
        }


        // -------- MIDDLE PANEL (preview + name + materials + button) --------
        var midPanel = MakePanel();
        midPanel.style.width = 260;
        midPanel.style.alignItems = Align.Center;
        midPanel.style.marginRight = 18;
        midPanel.RegisterCallback<PointerUpEvent>(OnPanelPointerUp, TrickleDown.TrickleDown);
        row.Add(midPanel);

        // Preview box
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

        // Icon inside preview box
        craftPreviewIcon = new Image();
        craftPreviewIcon.style.width = Length.Percent(100);
        craftPreviewIcon.style.height = Length.Percent(100);
        craftPreviewIcon.scaleMode = ScaleMode.ScaleToFit;
        previewBox.Add(craftPreviewIcon);

        // Recipe name label
        craftRecipeNameLabel = new Label("Select a recipe");
        craftRecipeNameLabel.style.marginTop = 10;
        craftRecipeNameLabel.style.unityTextAlign = TextAnchor.UpperCenter;
        craftRecipeNameLabel.style.color = Color.white;
        midPanel.Add(craftRecipeNameLabel);

        // Materials list (up to 4)
        var matsContainer = new VisualElement();
        matsContainer.style.marginTop = 8;
        matsContainer.style.alignSelf = Align.Stretch;
        midPanel.Add(matsContainer);

        craftMaterialLabels = new Label[4];
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



        // -------- RIGHT PANEL (scrollable recipe list) --------
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

        // Default hidden unless already open
        craftingOverlay.style.display = craftingOpen ? DisplayStyle.Flex : DisplayStyle.None;
    }
    private void OnCraftPressed()
    {
        if (playerInventory == null || playerInventory.Model == null) return;
        if (selectedRecipe == null) return;

        // Must have no cursor item (optional rule, but recommended)
        if (playerInventory.Model.Cursor.HasItem) return;

        // Check + consume ingredients from hotbar+backpack (so crafting works no matter where mats are)
        var hotbar = playerInventory.Model.Hotbar;
        var backpack = playerInventory.Model.Backpack;

        foreach (var ing in selectedRecipe.Ingredients)
        {
            if (ing.Item == null || ing.Amount <= 0) continue;
            int have = InventoryRules.CountItem(hotbar, backpack, ing.Item);
            if (have < ing.Amount) return; // not enough
        }

        foreach (var ing in selectedRecipe.Ingredients)
        {
            if (ing.Item == null || ing.Amount <= 0) continue;
            InventoryRules.TryConsume(hotbar, backpack, ing.Item, ing.Amount);
        }

        // Add output (auto-add uses hotbar first then backpack, new stacks in backpack)
        var outItem = selectedRecipe.OutputItem;
        int outAmt = Mathf.Max(1, selectedRecipe.OutputAmount);
        int rem = InventoryRules.TryAutoAdd(outItem, outAmt, hotbar, backpack);

        // If rem > 0, you can decide what to do (drop it to world, etc). For now ignore.
        playerInventory.NotifyInventoryChanged();

        // Refresh requirement text + button enabled state
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

        entry.RegisterCallback<PointerDownEvent>(_ =>
        {
            if (selectedRecipeEntry != null)
                SetBorder(selectedRecipeEntry, new Color(0, 0, 0, 0.75f));

            selectedRecipeEntry = entry;
            SetBorder(entry, new Color(1f, 0.9f, 0.2f, 1f));

            selectedRecipe = recipe;
            RefreshSelectedRecipeUI();
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

        // Hide crosshair whenever ANY UI is open
        SetCrosshairVisible(!(backpackOpen || craftingOpen));

        // If closing crafting, return cursor item like inventory does
        if (!craftingOpen && playerInventory != null && playerInventory.Model != null)
            InventoryRules.CancelCursorToOrigin(playerInventory.Model.Cursor);

        RefreshAll();
    }


    private void BuildCrosshairHud()
    {
        crosshairRoot = new VisualElement();
        crosshairRoot.pickingMode = PickingMode.Ignore;

        // Full-screen overlay that centers its children.
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

        // True center alignment (no background box)
        crosshairLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        crosshairLabel.style.whiteSpace = WhiteSpace.Normal;
        crosshairLabel.style.color = Color.white;
        crosshairLabel.style.backgroundColor = Color.clear;
        crosshairLabel.style.fontSize = crosshairFontSize;

        crosshairRoot.Add(crosshairLabel);
    }


    private void BuildHotbarHud()
    {
        // Outer container spans the screen width and centers its child.
        hotbarAnchor = new VisualElement();
        hotbarAnchor.style.position = Position.Absolute;
        hotbarAnchor.style.left = 0;
        hotbarAnchor.style.right = 0;
        hotbarAnchor.style.bottom = 18;
        hotbarAnchor.style.justifyContent = Justify.Center;
        hotbarAnchor.style.alignItems = Align.Center;

        root.Add(hotbarAnchor);

        // A padded panel around the hotbar that counts as "safe drop area"
        hotbarPanel = new VisualElement();
        hotbarPanel.AddToClassList("hotbar-panel");
        hotbarPanel.style.flexDirection = FlexDirection.Row;
        hotbarPanel.style.justifyContent = Justify.Center;
        hotbarPanel.style.alignItems = Align.Center;

        // "buffer" around the slots so dropping there does nothing
        hotbarPanel.style.paddingLeft = 10;
        hotbarPanel.style.paddingRight = 10;
        hotbarPanel.style.paddingTop = 10;
        hotbarPanel.style.paddingBottom = 10;

        // optional subtle background (you can remove if you want it invisible)
        hotbarPanel.style.backgroundColor = new Color(0, 0, 0, 0.0f);

        hotbarAnchor.Add(hotbarPanel);

        // Inner container is the actual hotbar row with a fixed width.
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

            // IMPORTANT: allowClicks true so hotbar accepts drops while inventory is open
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

        // inside the gray panel => never drop
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

        // IMPORTANT: evt.target will often be the overlay (because of trickle-down).
        // We need the element actually under the pointer.
        var picked = root?.panel?.Pick(evt.position) as VisualElement;

        // If pointer-up happened on a slot (or a child), don't drop.
        if (IsInsideInventorySlot(picked))
            return;

        // If pointer-up happened inside the gray inventory panel (including gaps), don't drop.
        if (IsInsideInventoryPanel(picked))
            return;

        // If pointer-up happened inside the hotbar safe area, don't drop.
        if (IsInsideHotbarPanel(picked))
            return;

        // Otherwise: released outside -> drop to world.

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

    private Vector2 _lastPanelPointerPos;
    private bool _hasPanelPointerPos;
    private bool _pointerHooked;

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

        // Hook once: capture pointer position in PANEL space (no Y inversion, no scaling offsets).
        if (!_pointerHooked)
        {
            _pointerHooked = true;

            // TrickleDown so we still get move events when hovering child elements (slots, panels, etc.)
            root.RegisterCallback<PointerMoveEvent>(e =>
            {
                _lastPanelPointerPos = e.position;   // panel coords (top-left origin)
                _hasPanelPointerPos = true;
            }, TrickleDown.TrickleDown);

            // Also update on pointer down so the icon snaps instantly when you click a slot
            root.RegisterCallback<PointerDownEvent>(e =>
            {
                _lastPanelPointerPos = e.position;
                _hasPanelPointerPos = true;
            }, TrickleDown.TrickleDown);
        }

        if (!_hasPanelPointerPos)
        {
            // No pointer data yet; hide until we get a move/down event
            cursorContainer.style.display = DisplayStyle.None;
            return;
        }

        // Position the held icon centered on the pointer
        cursorContainer.style.left = _lastPanelPointerPos.x - slotSize.x * 0.5f;
        cursorContainer.style.top = _lastPanelPointerPos.y - slotSize.y * 0.5f;

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
}
