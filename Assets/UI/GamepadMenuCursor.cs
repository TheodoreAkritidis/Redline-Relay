using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public sealed class GamepadMenuCursor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private InventoryUITKView inventoryUI;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;          // Vector2
    [SerializeField] private InputActionReference submitAction;        // A
    [SerializeField] private InputActionReference alternateAction;     // B

    [Header("Cursor")]
    [SerializeField] private float cursorSpeed = 900f;
    [SerializeField] private float deadzone = 0.05f;
    [SerializeField] private Vector2 cursorSize = new Vector2(18f, 18f);

    [Header("Hover")]
    [SerializeField] private string menuButtonClass = "menu-button";
    [SerializeField] private string hoverClass = "gamepad-hover";
    [SerializeField] private AudioSource hoverAudioSource;
    [SerializeField] private AudioClip hoverClip;


    [SerializeField] private float inventoryCursorCaptureSeconds = 0.2f;

    private float lastGamepadCursorUseTime = -999f;

    private VisualElement root;
    private VisualElement cursorVisual;
    private VisualElement hoveredElement;
    private Vector2 cursorPos;
    private bool initialized;

    private readonly List<VisualElement> hoveredPath = new();

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (submitAction != null) submitAction.action.Enable();
        if (alternateAction != null) alternateAction.action.Enable();

        initialized = false;
        hoveredElement = null;
        hoveredPath.Clear();
    }

    private void OnDisable()
    {
        ClearHover();

        if (cursorVisual != null)
            cursorVisual.style.display = DisplayStyle.None;

        if (moveAction != null) moveAction.action.Disable();
        if (submitAction != null) submitAction.action.Disable();
        if (alternateAction != null) alternateAction.action.Disable();

        if (inventoryUI != null)
            inventoryUI.ClearExternalCursorPosition();
    }

    private void Update()
    {
        if (uiDocument == null) return;

        root = uiDocument.rootVisualElement;
        if (root == null) return;

        EnsureCursorBuilt();

        bool active = IsActiveForCurrentUI();
        bool showCursor = active && (Time.unscaledTime - lastGamepadCursorUseTime <= 2f);
        cursorVisual.style.display = showCursor ? DisplayStyle.Flex : DisplayStyle.None;

        if (!active)
        {
            ClearHover();
            if (inventoryUI != null)
                inventoryUI.ClearExternalCursorPosition();
            return;
        }

        if (!initialized || float.IsNaN(cursorPos.x) || float.IsNaN(cursorPos.y))
        {
            var layout = root.layout;

            float startX = layout.width > 1f ? layout.width * 0.5f : 100f;
            float startY = layout.height > 1f ? layout.height * 0.5f : 100f;

            cursorPos = new Vector2(startX, startY);
            initialized = true;
        }

        Vector2 move = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        if (move.sqrMagnitude < deadzone * deadzone)
            move = Vector2.zero;

            bool gamepadDrivingCursor =
        move != Vector2.zero ||
        (submitAction != null && submitAction.action.WasPressedThisFrame()) ||
        (alternateAction != null && alternateAction.action.WasPressedThisFrame());

        if (gamepadDrivingCursor)
            lastGamepadCursorUseTime = Time.unscaledTime;

        cursorPos.x += move.x * cursorSpeed * Time.unscaledDeltaTime;
        cursorPos.y -= move.y * cursorSpeed * Time.unscaledDeltaTime;

        var layoutRect = root.layout;

        float maxX = layoutRect.width > 1f ? layoutRect.width : Screen.width;
        float maxY = layoutRect.height > 1f ? layoutRect.height : Screen.height;

        if (float.IsNaN(cursorPos.x)) cursorPos.x = maxX * 0.5f;
        if (float.IsNaN(cursorPos.y)) cursorPos.y = maxY * 0.5f;

        cursorPos.x = Mathf.Clamp(cursorPos.x, 0f, maxX);
        cursorPos.y = Mathf.Clamp(cursorPos.y, 0f, maxY);

        cursorVisual.style.left = cursorPos.x - cursorSize.x * 0.5f;
        cursorVisual.style.top = cursorPos.y - cursorSize.y * 0.5f;

        if (inventoryUI != null)
        {
            bool keepGamepadCursorActive =
                Time.unscaledTime - lastGamepadCursorUseTime <= inventoryCursorCaptureSeconds;

            if (keepGamepadCursorActive)
                inventoryUI.SetExternalCursorPosition(cursorPos);
            else
                inventoryUI.ClearExternalCursorPosition();
        }

        var picked = root.panel?.Pick(cursorPos) as VisualElement;
        var hoverTarget = ResolveHoverTarget(picked);
        UpdateHover(hoverTarget);

        if (submitAction != null && submitAction.action.WasPressedThisFrame())
        {
            if (inventoryUI != null && inventoryUI.HandleGamepadLeftClickAt(cursorPos))
                return;

            var button = FindAncestorButton(picked);
            if (button != null)
            {
                button.Focus();
                using var evt = NavigationSubmitEvent.GetPooled();
                evt.target = button;
                button.SendEvent(evt);
            }
        }

        if (alternateAction != null && alternateAction.action.WasPressedThisFrame())
        {
            if (inventoryUI != null)
                inventoryUI.HandleGamepadRightClickAt(cursorPos);
        }
    }

    private bool IsActiveForCurrentUI()
    {
        if (Gamepad.current == null)
            return false;

        if (inventoryUI != null)
            return inventoryUI.IsBackpackOpen || inventoryUI.IsCraftingOpen || inventoryUI.IsSmelterOpen;

        if (root == null)
            return false;

        foreach (var ve in root.Query<VisualElement>(className: menuButtonClass).ToList())
        {
            if (ve.resolvedStyle.display != DisplayStyle.None &&
                ve.worldBound.width > 1f &&
                ve.worldBound.height > 1f)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureCursorBuilt()
    {
        if (root == null) return;

        bool needsRebuild =
            cursorVisual == null ||
            cursorVisual.panel == null ||
            cursorVisual.parent != root;

        if (!needsRebuild) return;

        cursorVisual = new VisualElement();
        cursorVisual.name = "GamepadMenuCursor";
        cursorVisual.pickingMode = PickingMode.Ignore;
        cursorVisual.style.position = Position.Absolute;
        cursorVisual.style.width = cursorSize.x;
        cursorVisual.style.height = cursorSize.y;
        cursorVisual.style.borderTopWidth = 2;
        cursorVisual.style.borderRightWidth = 2;
        cursorVisual.style.borderBottomWidth = 2;
        cursorVisual.style.borderLeftWidth = 2;
        cursorVisual.style.borderTopColor = Color.white;
        cursorVisual.style.borderRightColor = Color.white;
        cursorVisual.style.borderBottomColor = Color.white;
        cursorVisual.style.borderLeftColor = Color.white;
        cursorVisual.style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
        cursorVisual.style.display = DisplayStyle.None;

        root.Add(cursorVisual);
    }

    private VisualElement ResolveHoverTarget(VisualElement picked)
    {
        VisualElement ve = picked;
        while (ve != null)
        {
            if (ve is Button)
                return ve;

            if (ve.ClassListContains(menuButtonClass))
                return ve;

            if (ve.ClassListContains("inv-slot"))
                return ve;

            if (ve.ClassListContains("recipe-entry"))
                return ve;

            ve = ve.parent;
        }

        return null;
    }

    private void UpdateHover(VisualElement newHover)
    {
        if (hoveredElement == newHover)
            return;

        ClearHover();

        hoveredElement = newHover;
        if (hoveredElement == null)
            return;

        VisualElement ve = hoveredElement;
        while (ve != null)
        {
            hoveredPath.Add(ve);
            ve.AddToClassList(hoverClass);
            ve = ve.parent;
        }

        if (hoverClip != null && hoverAudioSource != null)
            hoverAudioSource.PlayOneShot(hoverClip);
    }

    private void ClearHover()
    {
        for (int i = 0; i < hoveredPath.Count; i++)
        {
            if (hoveredPath[i] != null)
                hoveredPath[i].RemoveFromClassList(hoverClass);
        }

        hoveredPath.Clear();
        hoveredElement = null;
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
}