using UnityEngine;
using Photon.Pun;

/// <summary>
/// Simple player movement controller for Tower Defense game.
/// WASD movement relative to camera, character rotates to face movement direction.
/// Cursor is always visible for building.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class SimplePlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float rotationSmoothTime = 0.1f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float groundedGravity = -2f;

    [Header("Ground Check")]
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private float groundCheckOffset = 0.1f;
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    // Components
    private CharacterController controller;
    private PhotonView photonView;
    private SimpleInputManager inputManager; 
    private Animator animator;

    // Runtime
    private Vector3 velocity;
    private float rotationVelocity;
    private bool isGrounded;

    // Public accessors
    public bool IsMoving { get; private set; }
    public Vector3 MoveDirection { get; private set; }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        photonView = GetComponent<PhotonView>();
        inputManager = GetComponent<SimpleInputManager>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // Auto-find camera if not assigned
        if (cameraTransform == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                cameraTransform = mainCam.transform;
            }
        }

        // Ensure cursor is always visible
        if (photonView != null && photonView.IsMine)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
    }

    private void Update()
    {
        // Only process for local player
        if (photonView != null && !photonView.IsMine) return;

        GroundCheck();
        HandleMovement();
        ApplyGravity();
    }

    private void GroundCheck()
    {
        Vector3 spherePosition = transform.position + Vector3.down * groundCheckOffset;
        isGrounded = Physics.CheckSphere(spherePosition, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void HandleMovement()
    {
        // Get input
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = new Vector3(horizontal, 0, vertical).normalized;
        float moveAmount = Mathf.Clamp01(Mathf.Abs(horizontal) + Mathf.Abs(vertical));

        IsMoving = inputDirection.magnitude > 0.1f;

        if (IsMoving)
        {
            // Calculate direction relative to camera
            float cameraYaw = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + cameraYaw;

            // Smooth rotation
            float angle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y,
                targetAngle,
                ref rotationVelocity,
                rotationSmoothTime
            );
            transform.rotation = Quaternion.Euler(0, angle, 0);

            // Calculate move direction
            MoveDirection = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;

            // Sprint check
            float currentSpeed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentSpeed *= sprintMultiplier;
            }

            // Apply movement
            controller.Move(MoveDirection * currentSpeed * Time.deltaTime);
            animator.SetFloat("moveAmount", moveAmount, 0.2f, Time.deltaTime);
        }
        else
        {
            MoveDirection = Vector3.zero;
            animator.SetFloat("moveAmount", moveAmount, 0.2f, Time.deltaTime);
        }
    }

    private void ApplyGravity()
    {
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = groundedGravity;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        controller.Move(velocity * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 spherePosition = transform.position + Vector3.down * groundCheckOffset;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(spherePosition, groundCheckRadius);
    }
}