// File: InventoryUITKView.cs
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class InventoryUITKView : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Item Definitions (assign your real assets)")]
    [SerializeField] private ItemDefinition wood;
    [SerializeField] private ItemDefinition stone;

    [Header("Layout")]
    [SerializeField] private int backpackSlots = 30;
    [SerializeField] private int backpackColumns = 10;
    [SerializeField] private Vector2 slotSize = new Vector2(56, 56);

    [Header("Spacing")]
    [SerializeField] private float slotSpacing = 6f;
    [SerializeField] private float sectionSpacing = 10f;

    [Header("Visibility")]
    [SerializeField] private bool startHidden = true;

    private PlayerInventoryModel player;

    private VisualElement root;
    private VisualElement hotbarRow;
    private VisualElement backpackGrid;

    private VisualElement cursorRoot;
    private Image cursorIcon;
    private Label cursorQty;

    private SlotView[] hotbarViews;
    private SlotView[] backpackViews;

    private bool uiOpen;

    private void Awake()
    {
        if (uiDocument == null) uiDocument = GetComponent<UIDocument>();

        player = new PlayerInventoryModel(hotbarSlots: 10, backpackSlots: backpackSlots);

        // Optional test seed (uses your real ItemDefinition assets)
        if (wood != null) player.Hotbar.SetSlot(0, new ItemStack(wood, 12));
        if (wood != null) player.Backpack.SetSlot(0, new ItemStack(wood, 20));
        if (stone != null) player.Backpack.SetSlot(1, new ItemStack(stone, 7));
    }

    private void OnEnable()
    {
        root = uiDocument.rootVisualElement;

        BuildUI();

        uiOpen = !startHidden;
        root.style.display = uiOpen ? DisplayStyle.Flex : DisplayStyle.None;

        RefreshAll();
    }

    private void Update()
    {
        if (!uiOpen) return;
        UpdateCursorVisual();
    }

    public void SetOpen(bool open)
    {
        if (uiOpen == open) return;

        uiOpen = open;
        root.style.display = uiOpen ? DisplayStyle.Flex : DisplayStyle.None;

        // Per your rule: leaving menu snaps held item back, never drops.
        if (!uiOpen)
        {
            InventoryRules.CancelCursorToOrigin(player.Cursor);
        }

        RefreshAll();
    }

    private void BuildUI()
    {
        root.Clear();
        root.style.flexDirection = FlexDirection.Column;
        root.style.paddingLeft = 12;
        root.style.paddingTop = 12;

        // --- Hotbar ---
        hotbarRow = new VisualElement();
        hotbarRow.style.flexDirection = FlexDirection.Row;
        hotbarRow.style.flexWrap = Wrap.NoWrap;
        hotbarRow.style.marginBottom = sectionSpacing;
        root.Add(hotbarRow);

        hotbarViews = new SlotView[player.Hotbar.SlotCount];
        for (int i = 0; i < player.Hotbar.SlotCount; i++)
        {
            var v = CreateSlotView(player.Hotbar, i);
            hotbarViews[i] = v;
            hotbarRow.Add(v.Root);
        }

        // --- Backpack ---
        backpackGrid = new VisualElement();
        backpackGrid.style.flexDirection = FlexDirection.Row;
        backpackGrid.style.flexWrap = Wrap.Wrap;
        backpackGrid.style.width = backpackColumns * slotSize.x + (backpackColumns - 1) * slotSpacing;
        root.Add(backpackGrid);

        backpackViews = new SlotView[player.Backpack.SlotCount];
        for (int i = 0; i < player.Backpack.SlotCount; i++)
        {
            var v = CreateSlotView(player.Backpack, i);
            backpackViews[i] = v;
            backpackGrid.Add(v.Root);
        }

        // --- Cursor Visual (on top) ---
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

        // Drop-to-world on pointer-up outside slots
        root.RegisterCallback<PointerUpEvent>(OnRootPointerUp, TrickleDown.TrickleDown);
    }

    private SlotView CreateSlotView(IItemContainer container, int index)
    {
        var slot = new VisualElement();
        slot.AddToClassList("inv-slot"); // IMPORTANT: used to detect pointer up on slot

        slot.style.width = slotSize.x;
        slot.style.height = slotSize.y;
        slot.style.marginRight = slotSpacing;
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
        slot.RegisterCallback<PointerDownEvent>(evt => OnSlotPointerDown(evt, view));

        return view;
    }

    private void OnSlotPointerDown(PointerDownEvent evt, SlotView view)
    {
        if (!uiOpen) return;

        bool changed = false;

        if (evt.button == (int)MouseButton.LeftMouse)
        {
            changed = InventoryRules.TryLeftClickSlot(player.Cursor, view.Container, view.Index);
        }
        else if (evt.button == (int)MouseButton.RightMouse)
        {
            changed = InventoryRules.TryRightClickSlot(player.Cursor, view.Container, view.Index, player.Backpack);
        }

        if (changed)
        {
            RefreshAll();
            evt.StopPropagation();
        }
    }

    private void OnRootPointerUp(PointerUpEvent evt)
    {
        if (!uiOpen) return;
        if (!player.Cursor.HasItem) return;

        // If pointer-up happened on a slot (or any child of a slot), do NOT drop to world.
        if (IsInsideInventorySlot(evt.target as VisualElement))
            return;

        if (evt.button != (int)MouseButton.LeftMouse && evt.button != (int)MouseButton.RightMouse)
            return;

        ItemStack dropped = InventoryRules.DropCursorToWorld(player.Cursor);
        Debug.Log($"Dropped to world: {(dropped.IsEmpty ? "(empty)" : dropped.Item.ItemId + " x" + dropped.Quantity)}");

        RefreshAll();
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
        for (int i = 0; i < hotbarViews.Length; i++)
            RefreshSlot(hotbarViews[i]);

        for (int i = 0; i < backpackViews.Length; i++)
            RefreshSlot(backpackViews[i]);

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

    private void UpdateCursorVisual(bool force = false)
    {
        var cursorContainer = cursorRoot.Q<VisualElement>("CursorContainer");
        if (cursorContainer == null) return;

        if (!uiOpen)
        {
            cursorContainer.style.display = DisplayStyle.None;
            return;
        }

        var mouseDevice = Mouse.current;
        if (mouseDevice == null)
            return;

        Vector2 mouse = mouseDevice.position.ReadValue();
        cursorContainer.style.left = mouse.x - slotSize.x * 0.5f;
        cursorContainer.style.top = (Screen.height - mouse.y) - slotSize.y * 0.5f;

        if (!player.Cursor.HasItem)
        {
            cursorIcon.image = null;
            cursorQty.text = "";
            cursorContainer.style.display = DisplayStyle.None;
            return;
        }

        cursorContainer.style.display = DisplayStyle.Flex;

        ItemStack held = player.Cursor.CursorStack;
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
