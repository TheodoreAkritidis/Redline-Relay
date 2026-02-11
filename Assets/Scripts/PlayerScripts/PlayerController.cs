using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class SimpleFpsController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private Transform groundCheck;

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

    private GUIStyle speedStyle;

    private Rigidbody rb;

    private Vector2 moveInput;
    private Vector2 lookDelta;
    private bool sprintHeld;
    private bool jumpQueued;

    private float pitch;
    private float yaw;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints |= RigidbodyConstraints.FreezeRotation;

        yaw = transform.eulerAngles.y;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        yaw += lookDelta.x * lookSensitivity;
        pitch -= lookDelta.y * lookSensitivity;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        rb.MoveRotation(Quaternion.Euler(0f, yaw, 0f));

        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void FixedUpdate()
    {
        bool grounded = IsGrounded();
        Vector3 v = rb.linearVelocity;

        if (grounded)
        {
            float speed = moveSpeed * (sprintHeld ? sprintMultiplier : 1f);

            Vector3 wishDir =
                (transform.right * moveInput.x + transform.forward * moveInput.y);
            wishDir = Vector3.ClampMagnitude(wishDir, 1f);

            Vector3 targetHorizontal = wishDir * speed;

            rb.linearVelocity = new Vector3(targetHorizontal.x, v.y, targetHorizontal.z);
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

    // --- Input System (PlayerInput: Send Messages) ---
    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => lookDelta = value.Get<Vector2>();
    public void OnSprint(InputValue value) => sprintHeld = value.Get<float>() > 0.1f;
    public void OnJump(InputValue value)
    {
        if (value.isPressed) jumpQueued = true;
    }

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
