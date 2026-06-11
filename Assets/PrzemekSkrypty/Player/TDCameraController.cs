using UnityEngine;
using Unity.Cinemachine;
using Photon.Pun;


namespace ElementumDefense.Players
{
/// <summary>
/// Tower Defense camera controller.
/// Features: Q/E rotation, edge scrolling, zoom.
/// Works with Cinemachine 3.x (Unity 6)
/// </summary>
public class TDCameraController : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float keyRotationSpeed = 90f;      // Degrees per second (Q/E)
    [SerializeField] private float edgeRotationSpeed = 60f;     // Edge scrolling speed
    [SerializeField] private float edgeScrollZone = 50f;        // Pixels from edge
    [SerializeField] private bool enableEdgeScrolling = true;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;
    [SerializeField] private float zoomSmoothTime = 0.1f;

    [Header("Camera Orbit")]
    [SerializeField] private float orbitRadius = 10f;
    [SerializeField] private float orbitHeight = 8f;
    [SerializeField] private float lookDownAngle = 45f;

    [Header("References")]
    [SerializeField] private Transform followTarget;

    // Runtime
    private float currentYaw = 0f;
    private float currentZoom;
    private float targetZoom;
    private float zoomVelocity;

    private PhotonView photonView;
    private Camera mainCamera;

    private void Awake()
    {
        photonView = GetComponentInParent<PhotonView>();
        mainCamera = GetComponentInChildren<Camera>();
    }

    private void Start()
    {
        // Disable for remote players
        if (photonView != null && !photonView.IsMine)
        {
            if (mainCamera != null)
            {
                mainCamera.enabled = false;

                // Disable audio listener
                AudioListener listener = mainCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
            enabled = false;
            return;
        }

        // Initialize zoom
        currentZoom = orbitRadius;
        targetZoom = orbitRadius;

        // Auto-find follow target (player root)
        if (followTarget == null)
        {
            followTarget = transform.parent;
        }
    }

    private void LateUpdate()
    {
        if (photonView != null && !photonView.IsMine) return;
        if (followTarget == null) return;

        HandleRotationInput();
        HandleZoomInput();
        UpdateCameraPosition();
    }

    private void HandleRotationInput()
    {
        float rotationInput = 0f;

        // Keyboard rotation (Q/E)
        if (Input.GetKey(KeyCode.Q))
        {
            rotationInput -= 1f;
        }
        if (Input.GetKey(KeyCode.E))
        {
            rotationInput += 1f;
        }

        // Apply keyboard rotation
        if (Mathf.Abs(rotationInput) > 0.1f)
        {
            currentYaw += rotationInput * keyRotationSpeed * Time.deltaTime;
        }

        // Edge scrolling rotation
        if (enableEdgeScrolling)
        {
            float edgeInput = 0f;
            Vector3 mousePos = Input.mousePosition;

            // Check if mouse is at screen edges
            if (mousePos.x < edgeScrollZone)
            {
                edgeInput = -1f;
            }
            else if (mousePos.x > Screen.width - edgeScrollZone)
            {
                edgeInput = 1f;
            }

            // Only apply if mouse is inside game window
            if (mousePos.x >= 0 && mousePos.x <= Screen.width &&
                mousePos.y >= 0 && mousePos.y <= Screen.height)
            {
                currentYaw += edgeInput * edgeRotationSpeed * Time.deltaTime;
            }
        }

        // Keep yaw in 0-360 range
        currentYaw = Mathf.Repeat(currentYaw, 360f);
    }

    private void HandleZoomInput()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            targetZoom -= scrollInput * zoomSpeed;
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
        }

        // Smooth zoom
        currentZoom = Mathf.SmoothDamp(currentZoom, targetZoom, ref zoomVelocity, zoomSmoothTime);
    }

    private void UpdateCameraPosition()
    {
        // Calculate orbit position
        float yawRad = currentYaw * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            Mathf.Sin(yawRad) * currentZoom,
            orbitHeight,
            Mathf.Cos(yawRad) * currentZoom
        );

        // Apply position
        transform.position = followTarget.position + offset;

        // Look at target (with adjustable angle)
        Vector3 lookTarget = followTarget.position + Vector3.up * 1.5f; // Look slightly above feet
        transform.LookAt(lookTarget);
    }

    /// <summary>
    /// Returns camera's forward direction on XZ plane (for movement)
    /// </summary>
    public Vector3 GetCameraForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0;
        return forward.normalized;
    }

    /// <summary>
    /// Returns camera's right direction on XZ plane
    /// </summary>
    public Vector3 GetCameraRight()
    {
        Vector3 right = transform.right;
        right.y = 0;
        return right.normalized;
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        if (followTarget == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(followTarget.position, 0.5f);
        Gizmos.DrawLine(transform.position, followTarget.position);
    }
}
}
