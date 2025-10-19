using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float gravity = -9.81f;

    private CharacterController controller;
    private PhotonView photonView;
    private PlayerInputManager inputManager;
    private Vector3 velocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        photonView = GetComponent<PhotonView>();
        inputManager = GetComponent<PlayerInputManager>();
    }

    private void Update()
    {
        // CRITICAL: Only control YOUR player!
        if (photonView != null && !photonView.IsMine)
        {
            return; // Don't process input for other players
        }

        // Don't move during build mode or free mouse mode
        if (inputManager != null && (inputManager.IsInBuildMode || inputManager.IsInFreeMouseMode))
        {
            return;
        }

        HandleMovement();
        ApplyGravity();
    }

    private void HandleMovement()
    {
        // Get input
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Calculate movement direction (relative to world, not camera)
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        // Move character
        if (moveDirection.magnitude >= 0.1f)
        {
            // Move
            controller.Move(moveDirection * moveSpeed * Time.deltaTime);

            // Rotate to face movement direction
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}