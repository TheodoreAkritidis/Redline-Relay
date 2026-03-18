using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SimpleFpsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private InventoryUITKView inventoryUI;              // assign in inspector
    [SerializeField] private PlayerInventoryComponent playerInventory;   // assign in inspector (same Player object)
    [SerializeField] private DevConsole devConsole;                      // assign in inspector (optional)

    [Header("Move")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintMultiplier = 1.6f;

    [Header("Look")]
    [SerializeField] private float lookSensitivity = 0.08f;          // mouse
    [SerializeField] private float gamepadLookSensitivity = 180f;    // degrees/sec feel
    [SerializeField] private float pitchMin = -85f;
    [SerializeField] private float pitchMax = 85f;

    [SerializeField] private UnityEngine.InputSystem.UI.VirtualMouseInput virtualMouse;

    [Header("Jump")]
    [SerializeField] private float jumpImpulse = 6f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundMask;

    [Header("Air Control")]
    [SerializeField] private float airControl = 0.35f; // UNUSED, kept intentionally

    [Header("Debug")]
    [SerializeField] private bool showSpeedDebug = true;

    [Header("HUD")]
    [SerializeField] private CrosshairUITK crosshairUI;     // assign HUD object
    [SerializeField] private Interactor interactor;   // assign (optional but recommended)

    [Header("SoundEffects")]
    [SerializeField] private AudioSource jumpSound;


    private PlayerInput playerInput;

    private GUIStyle speedStyle;
    private Rigidbody rb;

    private Vector2 moveInput;
    private Vector2 lookDelta;
    private bool sprintHeld;
    private bool jumpQueued;

    private float pitch;
    private float yaw;

    private bool inventoryOpen;
    private bool craftingOpen;
    private bool smelterOpen;


    private bool UiBlocked => inventoryOpen || craftingOpen || smelterOpen || (devConsole != null && devConsole.IsOpen);

    private bool sprintAllowed = true;
    private void SetCursorMode(bool open)
    {
        Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = open;

        if (virtualMouse != null)
            virtualMouse.enabled = open;
    }
    public void SetSprintAllowed( bool allowed )
    {
        sprintAllowed = allowed;

        if ( !sprintAllowed ) sprintHeld = false; // Prevent sprinting if it just became disallowed
    }

    public bool WantsSprint => !UiBlocked && sprintHeld && moveInput.sqrMagnitude > 0.01f;
    public bool IsSprinting => WantsSprint && sprintAllowed;

    private void Awake( )
    {
        if ( crosshairUI == null ) crosshairUI = FindFirstObjectByType<CrosshairUITK>();
        if ( interactor == null ) interactor = GetComponent<Interactor>();

        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints |= RigidbodyConstraints.FreezeRotation;

        yaw = transform.eulerAngles.y;

        if ( playerInventory == null )
            playerInventory = GetComponent<PlayerInventoryComponent>();

        if ( devConsole == null )
            devConsole = FindFirstObjectByType<DevConsole>();
    }

    private void Start( )
    {
        SetInventoryOpen(false);
        playerInventory?.SetSelectedHotbarIndex(0);
    }

    private void Update( )
    {
        if ( UiBlocked )
            return;

        bool usingGamepad =
        playerInput != null &&
        !string.IsNullOrEmpty(playerInput.currentControlScheme) &&
        playerInput.currentControlScheme.IndexOf("Gamepad", System.StringComparison.OrdinalIgnoreCase) >= 0;

        float appliedLookSensitivity = usingGamepad
            ? gamepadLookSensitivity * Time.deltaTime
            : lookSensitivity;

        yaw += lookDelta.x * appliedLookSensitivity;
        pitch -= lookDelta.y * appliedLookSensitivity;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));

        if ( cameraPivot != null )
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void FixedUpdate( )
    {
        if ( UiBlocked )
        {
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, v.y, 0f);
            return;
        }

        bool grounded = IsGrounded();
        Vector3 v2 = rb.linearVelocity;

        if ( grounded )
        {
            float speed = moveSpeed * (IsSprinting ? sprintMultiplier : 1f);

            Vector3 wishDir = (transform.right * moveInput.x + transform.forward * moveInput.y);
            wishDir = Vector3.ClampMagnitude(wishDir, 1f);

            Vector3 targetHorizontal = wishDir * speed;
            rb.linearVelocity = new Vector3(targetHorizontal.x, v2.y, targetHorizontal.z);
        }

        if ( jumpQueued )
        {
            jumpQueued = false;

            if ( grounded )
            {
                if ( jumpSound != null )
                    jumpSound.Play();
                if ( rb.linearVelocity.y < 0f )
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

                rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
            }
        }
    }

    private bool IsGrounded( )
    {
        if ( groundCheck == null ) return false;

        return Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }
    private void SetSmelterOpen(bool open)
    {
        smelterOpen = open;

        if (crosshairUI != null)
            crosshairUI.SetVisible(!(inventoryOpen || craftingOpen || smelterOpen));

        if (interactor != null)
            interactor.enabled = !(inventoryOpen || craftingOpen || smelterOpen);

        if (inventoryOpen || craftingOpen || smelterOpen)
        {
            moveInput = Vector2.zero;
            lookDelta = Vector2.zero;
            sprintHeld = false;
            jumpQueued = false;

            SetCursorMode(true);
        }
        else
        {
            if (devConsole == null || !devConsole.IsOpen)
            {
                SetCursorMode(false);
            }
        }

        if (inventoryUI != null)
            inventoryUI.SetSmelterOpen(smelterOpen);
    }
    private void SetInventoryOpen( bool open )
    {
        inventoryOpen = open;
        // Hide HUD crosshair while inventory is open
        if ( crosshairUI != null )
            crosshairUI.SetVisible(!open);

        // Prevent interaction raycast / E while inventory open
        if ( interactor != null )
            interactor.enabled = !open;
        if ( inventoryOpen )
        {
            moveInput = Vector2.zero;
            lookDelta = Vector2.zero;
            sprintHeld = false;
            jumpQueued = false;

            SetCursorMode(true);
        }
        else
        {
            // If console is open, don't re-lock the cursor here.
            if ( devConsole == null || !devConsole.IsOpen )
            {
                SetCursorMode(false);
            }
        }

        if ( inventoryUI != null )
            inventoryUI.SetBackpackOpen(inventoryOpen);
    }
    private void SetCraftingOpen( bool open )
    {
        craftingOpen = open;

        // Hide HUD crosshair while any UI is open
        if ( crosshairUI != null )
            crosshairUI.SetVisible(!(inventoryOpen || craftingOpen));

        // Prevent interaction while UI open
        if ( interactor != null )
            interactor.enabled = !(inventoryOpen || craftingOpen);

        if ( inventoryOpen || craftingOpen )
        {
            moveInput = Vector2.zero;
            lookDelta = Vector2.zero;
            sprintHeld = false;
            jumpQueued = false;

            SetCursorMode(true);
        }
        else
        {
            if ( devConsole == null || !devConsole.IsOpen )
            {
                SetCursorMode(false);
            }
        }

        if ( inventoryUI != null )
            inventoryUI.SetCraftingOpen(craftingOpen);
    }

    // --- Input System (PlayerInput: Send Messages) ---
    
    public void OnMove( InputValue value )
    {
        if ( UiBlocked ) { moveInput = Vector2.zero; return; }
        moveInput = value.Get<Vector2>();
    }

    public void OnLook( InputValue value )
    {
        if ( UiBlocked ) { lookDelta = Vector2.zero; return; }
        lookDelta = value.Get<Vector2>();
    }

    public void OnSprint( InputValue value )
    {
        if ( UiBlocked ) { sprintHeld = false; return; }
        sprintHeld = value.Get<float>() > 0.1f;
    }

    public void OnJump( InputValue value )
    {
        if ( UiBlocked ) return;
        if ( value.isPressed ) jumpQueued = true;
    }

    private void StepHotbar( int delta )
    {
        if ( playerInventory == null || playerInventory.Model == null || playerInventory.Model.Hotbar == null )
            return;

        int count = playerInventory.Model.Hotbar.SlotCount;
        if ( count <= 0 ) return;

        int cur = playerInventory.SelectedHotbarIndex;
        int next = (cur + delta) % count;
        if ( next < 0 ) next += count;

        playerInventory.SetSelectedHotbarIndex(next);
    }


    public void OnHotbarNext( InputValue v )
    {
        if ( !v.isPressed ) return;
        if ( inventoryOpen ) return;
        StepHotbar(+1);
    }

    public void OnHotbarPrev( InputValue v )
    {
        if ( !v.isPressed ) return;
        if ( inventoryOpen ) return;
        StepHotbar(-1);
    }

    // Mouse scroll is a Vector2 (x,y). We care about y.
    public void OnHotbarScroll( InputValue v )
    {
        if (inventoryOpen || craftingOpen || smelterOpen) return;

        Vector2 scroll = v.Get<Vector2>();
        if ( Mathf.Abs(scroll.y) < 0.01f ) return;

        // Typical FPS convention: wheel up -> previous slot, wheel down -> next slot
        StepHotbar(scroll.y > 0f ? -1 : +1);
    }

    public void OnEscape(InputValue value)
    {
        if (!value.isPressed) return;
        if (devConsole != null && devConsole.IsOpen) return; // keep your existing console behavior

        CloseAllMenus();
    }

    public void OnInventory(InputValue value)
    {
        if (!value.isPressed) return;
        if (devConsole != null && devConsole.IsOpen) return;

        SetMenu(MenuType.Inventory);
    }

    public void OnCrafting(InputValue value)
    {
        if (!value.isPressed) return;
        if (devConsole != null && devConsole.IsOpen) return;

        SetMenu(MenuType.Crafting);
    }

    public void OnSmelter(InputValue value)
    {
        if (!value.isPressed) return;
        if (devConsole != null && devConsole.IsOpen) return;

        SetMenu(MenuType.Smelter);
    }

    // --- Menu switching helpers ---

    public void OpenSmelterMenu()
    {
        if (devConsole != null && devConsole.IsOpen) return;
        SetMenu(MenuType.Smelter);
    }

    public void OpenInventoryMenu()
    {
        if (devConsole != null && devConsole.IsOpen) return;
        SetMenu(MenuType.Inventory);
    }

    public void OpenCraftingMenu()
    {
        if (devConsole != null && devConsole.IsOpen) return;
        SetMenu(MenuType.Crafting);
    }


    private enum MenuType { None, Inventory, Crafting, Smelter }

    private void SetMenu(MenuType target)
    {
        // If you press the key for the menu that’s already open -> close everything.
        bool alreadyOpen =
            (target == MenuType.Inventory && inventoryOpen) ||
            (target == MenuType.Crafting && craftingOpen) ||
            (target == MenuType.Smelter && smelterOpen);

        if (alreadyOpen)
            target = MenuType.None;

        // Close everything first
        if (inventoryOpen) SetInventoryOpen(false);
        if (craftingOpen) SetCraftingOpen(false);
        if (smelterOpen) SetSmelterOpen(false);

        // Then open the requested menu
        if (target == MenuType.Inventory) SetInventoryOpen(true);
        else if (target == MenuType.Crafting) SetCraftingOpen(true);
        else if (target == MenuType.Smelter) SetSmelterOpen(true);
    }

    private void CloseAllMenus()
    {
        if (inventoryOpen) SetInventoryOpen(false);
        if (craftingOpen) SetCraftingOpen(false);
        if (smelterOpen) SetSmelterOpen(false);
    }


    //----------------


    // Hotbar selection (bind these to 1..0 in Input Actions)
    public void OnHotbar1( InputValue v ) { if ( !UiBlocked && v.isPressed ) playerInventory?.SetSelectedHotbarIndex(0); }
    public void OnHotbar2( InputValue v ) { if ( !UiBlocked && v.isPressed ) playerInventory?.SetSelectedHotbarIndex(1); }
    public void OnHotbar3( InputValue v ) { if ( !UiBlocked && v.isPressed ) playerInventory?.SetSelectedHotbarIndex(2); }
    public void OnHotbar4( InputValue v ) { if ( !UiBlocked && v.isPressed ) playerInventory?.SetSelectedHotbarIndex(3); }
    public void OnHotbar5( InputValue v ) { if ( !UiBlocked && v.isPressed ) playerInventory?.SetSelectedHotbarIndex(4); }
    public void OnHotbar6( InputValue v ) { if ( !UiBlocked && v.isPressed ) playerInventory?.SetSelectedHotbarIndex(5); }
    public void OnHotbar7( InputValue v ) { if ( !UiBlocked && v.isPressed ) playerInventory?.SetSelectedHotbarIndex(6); }
    public void OnHotbar8( InputValue v ) { if ( !UiBlocked && v.isPressed ) playerInventory?.SetSelectedHotbarIndex(7); }
    public void OnHotbar9( InputValue v ) { if ( !UiBlocked && v.isPressed ) playerInventory?.SetSelectedHotbarIndex(8); }
    public void OnHotbar0( InputValue v ) { if ( !UiBlocked && v.isPressed ) playerInventory?.SetSelectedHotbarIndex(9); }

    private void OnGUI( )
    {
        if ( !showSpeedDebug || rb == null ) return;

        if ( speedStyle == null )
        {
            speedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                richText = true
            };
        }

        Vector3 v = rb.linearVelocity;
        float horizontal = new Vector3(v.x, 0f, v.z).magnitude;

        GUI.Label(
            new Rect(10, 10, 320, 60),
            $"<b>Velocity</b>\nX: {v.x:0.00}  Y: {v.y:0.00}  Z: {v.z:0.00}\n<b>Speed</b>: {horizontal:0.00} m/s",
            speedStyle
        );
    }
}
