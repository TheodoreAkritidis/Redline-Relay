using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class InventoryUITKView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private PlayerInventoryComponent playerInventory; // assign Player here (or auto-find)

    [Header("Layout")]
    [SerializeField] private int backpackColumns = 10;
    [SerializeField] private Vector2 slotSize = new Vector2(56, 56);

    [Header("Spacing")]
    [SerializeField] private float slotSpacing = 6f;

    private VisualElement root;

    // Always-visible hotbar HUD
    private VisualElement hotbarHud;
    private SlotView[] hotbarViews;

    // Backpack overlay (only when open)
    private VisualElement backpackOverlay;
    private VisualElement backpackGrid;
    private SlotView[] backpackViews;

    // Cursor visual (only relevant when backpack open)
    private VisualElement cursorRoot;
    private Image cursorIcon;
    private Label cursorQty;

    private bool backpackOpen;

    private void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();
        if (playerInventory == null) playerInventory = FindFirstObjectByType<PlayerInventoryComponent>();
    }

    private void OnEnable()
    {
        root = uiDocument.rootVisualElement;
        BuildUI();

        // Start with backpack hidden; hotbar always visible.
        SetBackpackOpen(false);

        HookModelEvents();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnhookModelEvents();
    }

    private void Update()
    {
        if (!backpackOpen) return;
        UpdateCursorVisual();
    }

    public void SetBackpackOpen(bool open)
    {
        backpackOpen = open;

        if (backpackOverlay != null)
            backpackOverlay.style.display = backpackOpen ? DisplayStyle.Flex : DisplayStyle.None;

        if (!backpackOpen && playerInventory != null)
        {
            // leaving menu snaps held item back
            InventoryRules.CancelCursorToOrigin(playerInventory.Model.Cursor);
        }

        RefreshAll();
    }

    private void HookModelEvents()
    {
        if (playerInventory == null) return;
        playerInventory.InventoryChanged += RefreshAll;
        playerInventory.HotbarSelectionChanged += _ => RefreshHotbarSelection();
    }

    private void UnhookModelEvents()
    {
        if (playerInventory == null) return;
        playerInventory.InventoryChanged -= RefreshAll;
        playerInventory.HotbarSelectionChanged -= _ => RefreshHotbarSelection();
    }

    private void BuildUI()
    {
        root.Clear();

        // Root is full screen container
        root.style.position = Position.Relative;
        root.style.width = Length.Percent(100);
        root.style.height = Length.Percent(100);

        BuildHotbarHud();
        BuildBackpackOverlay();
        BuildCursorVisual();
    }

    private void BuildHotbarHud()
    {
        // Outer container spans the screen width and centers its child.
        var hotbarAnchor = new VisualElement();
        hotbarAnchor.style.position = Position.Absolute;
        hotbarAnchor.style.left = 0;
        hotbarAnchor.style.right = 0;
        hotbarAnchor.style.bottom = 18;
        hotbarAnchor.style.height = slotSize.y + 2; // just enough height
        hotbarAnchor.style.justifyContent = Justify.Center;
        hotbarAnchor.style.alignItems = Align.Center;

        root.Add(hotbarAnchor);

        // Inner container is the actual hotbar row with a fixed width.
        hotbarHud = new VisualElement();
        hotbarHud.style.flexDirection = FlexDirection.Row;
        hotbarHud.style.flexWrap = Wrap.NoWrap;

        var model = playerInventory.Model;
        int hotbarCount = model.Hotbar.SlotCount;
        
        // Fixed width: slots + gaps (no trailing gap)
        hotbarHud.style.width = hotbarCount * slotSize.x + (hotbarCount - 1) * slotSpacing;

        hotbarAnchor.Add(hotbarHud);

        hotbarViews = new SlotView[hotbarCount];
        for (int i = 0; i < hotbarCount; i++)
        {
            bool isLastInRow = (i == hotbarCount - 1);
            var v = CreateSlotView(model.Hotbar, i, allowClicks: false, isLastInRow: isLastInRow);
            hotbarViews[i] = v;
            hotbarHud.Add(v.Root);
        }
    }


    private void BuildBackpackOverlay()
    {
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

        float gridWidth = backpackColumns * (slotSize.x + slotSpacing);
        backpackGrid.style.width = gridWidth;
        panel.Add(backpackGrid);

        var model = playerInventory.Model;

        backpackViews = new SlotView[model.Backpack.SlotCount];
        for (int i = 0; i < model.Backpack.SlotCount; i++)
        {
            var v = CreateSlotView(model.Backpack, i, allowClicks: true);
            backpackViews[i] = v;
            backpackGrid.Add(v.Root);
        }

        // Drop-to-world only when backpack is open and releasing outside slots
        backpackOverlay.RegisterCallback<PointerUpEvent>(OnOverlayPointerUp, TrickleDown.TrickleDown);
        panel.RegisterCallback<PointerUpEvent>(OnPanelPointerUp, TrickleDown.TrickleDown);
    }
    private void OnPanelPointerUp(PointerUpEvent evt)
    {
        if (!backpackOpen) return;
        if (playerInventory == null) return;
        if (!playerInventory.Model.Cursor.HasItem) return;

        // Releasing inside the gray panel (including gaps) should NOT drop.
        // Do nothing; item stays on cursor.
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

        // Hover highlight only meaningful while backpack is open (but harmless for hotbar)
        slot.RegisterCallback<PointerEnterEvent>(_ => slot.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f, 1f));
        slot.RegisterCallback<PointerLeaveEvent>(_ => slot.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f));

        if (allowClicks)
            slot.RegisterCallback<PointerDownEvent>(evt => OnSlotPointerDown(evt, view));

        return view;
    }

    private void OnSlotPointerDown(PointerDownEvent evt, SlotView view)
    {
        if (!backpackOpen) return;
        if (playerInventory == null) return;

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
        if (!backpackOpen) return;
        if (playerInventory == null) return;
        if (!playerInventory.Model.Cursor.HasItem) return;

        // If pointer-up happened on a slot (or a child), don't drop.
        if (IsInsideInventorySlot(evt.target as VisualElement))
            return;

        // If pointer-up happened inside the gray panel (including gaps), don't drop.
        if (IsInsideInventoryPanel(evt.target as VisualElement))
            return;

        // Otherwise: released outside the panel -> drop to world.
        ItemStack dropped = InventoryRules.DropCursorToWorld(playerInventory.Model.Cursor);
        Debug.Log($"Dropped to world: {(dropped.IsEmpty ? "(empty)" : dropped.Item.ItemId + " x" + dropped.Quantity)}");

        playerInventory.NotifyInventoryChanged();
    }
    private static bool IsInsideInventoryPanel(VisualElement ve)
    {
        while (ve != null)
        {
            if (ve.ClassListContains("inv-panel"))
                return true;
            ve = ve.parent;
        }
        return false;
    }


    private static bool IsInsideInventorySlot(VisualElement ve)
    {
        while (ve != null)
        {
            if (ve.ClassListContains("inv-slot"))
                return true;
            ve = ve.parent;
        }
        return false;
    }

    private void RefreshAll()
    {
        if (playerInventory == null) return;

        // Hotbar
        for (int i = 0; i < hotbarViews.Length; i++)
            RefreshSlot(hotbarViews[i]);

        // Backpack (only if built)
        if (backpackViews != null)
        {
            for (int i = 0; i < backpackViews.Length; i++)
                RefreshSlot(backpackViews[i]);
        }

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
        if (playerInventory == null || hotbarViews == null) return;

        int selected = playerInventory.SelectedHotbarIndex;

        for (int i = 0; i < hotbarViews.Length; i++)
        {
            // highlight selected by changing border color
            Color c = (i == selected) ? new Color(1f, 0.9f, 0.2f, 1f) : new Color(0, 0, 0, 0.75f);

            hotbarViews[i].Root.style.borderTopColor = c;
            hotbarViews[i].Root.style.borderRightColor = c;
            hotbarViews[i].Root.style.borderBottomColor = c;
            hotbarViews[i].Root.style.borderLeftColor = c;
        }
    }

    private void UpdateCursorVisual(bool force = false)
    {
        var cursorContainer = cursorRoot.Q<VisualElement>("CursorContainer");
        if (cursorContainer == null) return;

        if (!backpackOpen || playerInventory == null)
        {
            cursorContainer.style.display = DisplayStyle.None;
            return;
        }

        var mouseDevice = Mouse.current;
        if (mouseDevice == null) return;

        Vector2 mouse = mouseDevice.position.ReadValue();
        cursorContainer.style.left = mouse.x - slotSize.x * 0.5f;
        cursorContainer.style.top = (Screen.height - mouse.y) - slotSize.y * 0.5f;

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
