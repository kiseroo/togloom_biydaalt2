using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 0.5f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float airJumpDelay = 0.5f; // Time before allowing another jump in air
    [SerializeField] private float groundCheckDistance = 0.5f; // For visual feedback only now
    [SerializeField] private LayerMask groundLayer = ~0; // Default to "Everything" layer
    
    private Vector2 moveInput;
    private Vector2 lookInput;
    private Rigidbody rb;
    private Camera playerCamera;
    private float xRotation = 0f;
    private bool isGrounded = false; // For visual feedback only
    private bool isJumping = false;
    private float lastJumpTime = 0f;
    private float timeInAir = 0f;
    private PlayerInput playerInput; // Reference to the PlayerInput component

    void Awake()
    {
        // Find and set up the required components
        rb = GetComponent<Rigidbody>();
        playerInput = GetComponent<PlayerInput>();
          // Check if we need to add the PlayerInput component
        if (playerInput == null)
        {
            playerInput = gameObject.AddComponent<PlayerInput>();
            // Try to load from the Resources folder if it exists, otherwise use direct reference
            playerInput.actions = Resources.Load<InputActionAsset>("InputSystem_Actions");
            if (playerInput.actions == null)
            {
                // Direct reference to the asset in the project
                string assetPath = "Assets/InputSystem_Actions.inputactions";
                playerInput.actions = UnityEditor.AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
                Debug.Log("Loaded InputSystem_Actions directly from Assets folder");
            }
            playerInput.defaultActionMap = "Player";
            Debug.Log("Added PlayerInput component with InputSystem_Actions");
        }
        
        // Set up the camera
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            GameObject cameraObject = new GameObject("PlayerCamera");
            cameraObject.transform.parent = transform;
            cameraObject.transform.localPosition = new Vector3(0, 1.7f, 0);
            playerCamera = cameraObject.AddComponent<Camera>();
            Debug.Log("Created new camera for player");
        }
    }

    void Start()
    {
        // Setup Rigidbody
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure Rigidbody for character movement
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.mass = 70f;
        rb.linearDamping = 0.1f;
        
        // Ensure there's a collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.5f;
            capsule.center = new Vector3(0, 1f, 0);
            Debug.Log("Added CapsuleCollider component");
        }

        // Lock cursor for FPS control
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        Debug.Log("PlayerMovement initialized. Controls: WASD to move, Space to jump, Mouse to look");
    }

    // Input System callbacks
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        Debug.Log("Move Input: " + moveInput);
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
        Debug.Log("Look Input: " + lookInput);
    }    public void OnJump(InputValue value)
    {
        // Check if we should jump, allow regardless of ground status
        Debug.Log("Jump button pressed: " + value.isPressed + ", time in air: " + timeInAir);
        
        // Allow jump if: just pressed button AND either (on ground OR enough time has passed since last jump)
        if (value.isPressed && (Time.time - lastJumpTime > airJumpDelay))
        {
            isJumping = true;
            lastJumpTime = Time.time;
            Debug.Log("Jump executed - setting isJumping to true, time since last jump: " + (Time.time - lastJumpTime));
        }
    }    void Update()
    {
        // Check if we're grounded (for visual feedback only)
        bool wasGrounded = isGrounded;
        
        // Use simple ground check for visual feedback
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundCheckDistance + 0.1f, groundLayer);
        
        // Track time in air for jump cooldown
        if (isGrounded)
        {
            timeInAir = 0;
        }
        else
        {
            timeInAir += Time.deltaTime;
        }
        
        // Log when grounded state changes (for debugging)
        if (wasGrounded != isGrounded)
        {
            Debug.Log("Grounded state changed to: " + isGrounded + ", time in air: " + timeInAir);
        }
        
        // Handle mouse look
        if (playerCamera != null)
        {
            // Apply mouse look (multiplying by Time.deltaTime for frame rate independence)
            float mouseX = lookInput.x * mouseSensitivity * 100f * Time.deltaTime;
            float mouseY = lookInput.y * mouseSensitivity * 100f * Time.deltaTime;

            // Vertical rotation (looking up and down)
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

            // Horizontal rotation (turning left and right)
            transform.Rotate(Vector3.up * mouseX);
        }

        // Reset position if fallen too far
        if (transform.position.y < -20f)
        {
            transform.position = new Vector3(0, 2, 0);
            rb.linearVelocity = Vector3.zero;
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // Handle WASD movement
        Vector3 movement = transform.right * moveInput.x + transform.forward * moveInput.y;
        movement = movement.normalized * moveSpeed;

        // Apply horizontal movement while preserving vertical velocity
        Vector3 velocity = rb.linearVelocity;
        velocity.x = movement.x;
        velocity.z = movement.z;
        
        // Apply jump force
        if (isJumping)
        {
            velocity.y = jumpForce;
            isJumping = false;
            Debug.Log("Jump executed");
        }
        // Apply a small downward force to help stick to the ground
        else if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        rb.linearVelocity = velocity;
    }
      // Helper to visualize the ground check in the editor
    void OnDrawGizmos()
    {
        // Color changes based on ground status (green = grounded, red = in air)
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 0.1f + Vector3.down * (groundCheckDistance + 0.1f));
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f + Vector3.down * (groundCheckDistance + 0.1f), 0.1f);
        
        // Show jump cooldown status (blue = can jump again)
        if (Time.time - lastJumpTime > airJumpDelay)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.2f);
        }
    }
}
