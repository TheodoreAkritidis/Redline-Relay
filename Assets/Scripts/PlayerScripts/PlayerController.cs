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
    [SerializeField] private float lookSensitivity = 0.08f;
    [SerializeField] private float pitchMin = -85f;
    [SerializeField] private float pitchMax = 85f;

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


    private GUIStyle speedStyle;
    private Rigidbody rb;

    private Vector2 moveInput;
    private Vector2 lookDelta;
    private bool sprintHeld;
    private bool jumpQueued;

    private float pitch;
    private float yaw;

    private bool inventoryOpen;

    private bool UiBlocked => inventoryOpen || (devConsole != null && devConsole.IsOpen);

    private void Awake()
    {
        if (crosshairUI == null) crosshairUI = FindFirstObjectByType<CrosshairUITK>();
        if (interactor == null) interactor = GetComponent<Interactor>();

        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints |= RigidbodyConstraints.FreezeRotation;

        yaw = transform.eulerAngles.y;

        if (playerInventory == null)
            playerInventory = GetComponent<PlayerInventoryComponent>();

        if (devConsole == null)
            devConsole = FindFirstObjectByType<DevConsole>();
    }

    private void Start()
    {
        SetInventoryOpen(false);
        playerInventory?.SetSelectedHotbarIndex(0);
    }

    private void Update()
    {
        if (UiBlocked)
            return;

        yaw += lookDelta.x * lookSensitivity;
        pitch -= lookDelta.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void FixedUpdate()
    {
        if (UiBlocked)
        {
            Vector3 v = rb.linearVelocity;
            rb.linearVelocity = new Vector3(0f, v.y, 0f);
            return;
        }

        bool grounded = IsGrounded();
        Vector3 v2 = rb.linearVelocity;

        if (grounded)
        {
            float speed = moveSpeed * (sprintHeld ? sprintMultiplier : 1f);

            Vector3 wishDir = (transform.right * moveInput.x + transform.forward * moveInput.y);
            wishDir = Vector3.ClampMagnitude(wishDir, 1f);

            Vector3 targetHorizontal = wishDir * speed;
            rb.linearVelocity = new Vector3(targetHorizontal.x, v2.y, targetHorizontal.z);
        }

        if (jumpQueued)
        {
            jumpQueued = false;

            if (grounded)
            {
                if (rb.linearVelocity.y < 0f)
                    rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

                rb.AddForce(Vector3.up * jumpImpulse, ForceMode.Impulse);
            }
        }
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;

        return Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void SetInventoryOpen(bool open)
    {
        inventoryOpen = open;
        // Hide HUD crosshair while inventory is open
        if (crosshairUI != null)
            crosshairUI.SetVisible(!open);

        // Prevent interaction raycast / E while inventory open
        if (interactor != null)
            interactor.enabled = !open;
        if (inventoryOpen)
        {
            moveInput = Vector2.zero;
            lookDelta = Vector2.zero;
            sprintHeld = false;
            jumpQueued = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // If console is open, don't re-lock the cursor here.
            if (devConsole == null || !devConsole.IsOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (inventoryUI != null)
            inventoryUI.SetBackpackOpen(inventoryOpen);
    }

    // --- Input System (PlayerInput: Send Messages) ---
    public void OnMove(InputValue value)
    {
        if (UiBlocked) { moveInput = Vector2.zero; return; }
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        if (UiBlocked) { lookDelta = Vector2.zero; return; }
        lookDelta = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        if (UiBlocked) { sprintHeld = false; return; }
        sprintHeld = value.Get<float>() > 0.1f;
    }

    public void OnJump(InputValue value)
    {
        if (UiBlocked) return;
        if (value.isPressed) jumpQueued = true;
    }

    public void OnInventory(InputValue value)
    {
        if (!value.isPressed) return;
        if (devConsole != null && devConsole.IsOpen) return; // block tab while console open
        SetInventoryOpen(!inventoryOpen);
    }



    // Hotbar selection (bind these to 1..0 in Input Actions)
    public void OnHotbar1(InputValue v) { if (!UiBlocked && v.isPressed) playerInventory?.SetSelectedHotbarIndex(0); }
    public void OnHotbar2(InputValue v) { if (!UiBlocked && v.isPressed) playerInventory?.SetSelectedHotbarIndex(1); }
    public void OnHotbar3(InputValue v) { if (!UiBlocked && v.isPressed) playerInventory?.SetSelectedHotbarIndex(2); }
    public void OnHotbar4(InputValue v) { if (!UiBlocked && v.isPressed) playerInventory?.SetSelectedHotbarIndex(3); }
    public void OnHotbar5(InputValue v) { if (!UiBlocked && v.isPressed) playerInventory?.SetSelectedHotbarIndex(4); }
    public void OnHotbar6(InputValue v) { if (!UiBlocked && v.isPressed) playerInventory?.SetSelectedHotbarIndex(5); }
    public void OnHotbar7(InputValue v) { if (!UiBlocked && v.isPressed) playerInventory?.SetSelectedHotbarIndex(6); }
    public void OnHotbar8(InputValue v) { if (!UiBlocked && v.isPressed) playerInventory?.SetSelectedHotbarIndex(7); }
    public void OnHotbar9(InputValue v) { if (!UiBlocked && v.isPressed) playerInventory?.SetSelectedHotbarIndex(8); }
    public void OnHotbar0(InputValue v) { if (!UiBlocked && v.isPressed) playerInventory?.SetSelectedHotbarIndex(9); }

    private void OnGUI()
    {
        if (!showSpeedDebug || rb == null) return;

        if (speedStyle == null)
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
