using UnityEngine;
using Cinemachine;
using Photon.Pun;

/// <summary>
/// Controls camera behavior during build mode
/// Implements edge scrolling (mouse at screen edges rotates camera)
/// Requires Cinemachine Virtual Camera with Orbital Transposer
/// </summary>
[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CameraController : MonoBehaviour
{
    [Header("Edge Scrolling")]
    [SerializeField, Tooltip("Screen edge detection zone size (pixels)")]
    private float edgeScrollSize = 40f;

    [SerializeField, Tooltip("Camera rotation speed")]
    private float edgeScrollSpeed = 20f;

    [Header("Zoom Settings")]
    [SerializeField] private bool enableZoom = true;
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;

    private PlayerInputManager inputManager;
    private CinemachineVirtualCamera virtualCamera;
    private CinemachineOrbitalTransposer orbit;
    private PhotonView photonView;

    private void Awake()
    {
        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        orbit = virtualCamera.GetCinemachineComponent<CinemachineOrbitalTransposer>();
        inputManager = GetComponentInParent<PlayerInputManager>();
        photonView = GetComponentInParent<PhotonView>();
    }

    private void Start()
    {
        if (photonView != null && !photonView.IsMine)
        {
            virtualCamera.Priority = 0;
            gameObject.SetActive(false);

            // NOWE: Disable audio listener
            AudioListener listener = GetComponent<AudioListener>();
            if (listener != null)
            {
                listener.enabled = false;
            }
        }
        else
        {
            virtualCamera.Priority = 10;
        }
    }

    private void Update()
    {
        if (!photonView.IsMine || inputManager == null) return;

        // Edge scrolling only works in build mode
        if (inputManager.IsInBuildMode)
        {
            HandleEdgeScrolling();
        }

        // Zoom works always
        if (enableZoom)
        {
            HandleZoom();
        }
    }

    /// <summary>
    /// Rotates camera when mouse is near screen edges
    /// </summary>
    private void HandleEdgeScrolling()
    {
        float horizontalInput = 0f;

        // Check mouse position relative to screen edges
        if (Input.mousePosition.x < edgeScrollSize)
        {
            horizontalInput = -1f; // Rotate left
        }
        else if (Input.mousePosition.x > Screen.width - edgeScrollSize)
        {
            horizontalInput = 1f; // Rotate right
        }

        // Apply rotation to Cinemachine orbit
        if (orbit != null && horizontalInput != 0f)
        {
            orbit.m_XAxis.Value += horizontalInput * edgeScrollSpeed * Time.deltaTime;
        }
    }

    /// <summary>
    /// Handles mouse wheel zoom
    /// </summary>
    private void HandleZoom()
    {
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");

        if (orbit != null && Mathf.Abs(scrollInput) > 0.01f)
        {
            // Adjust camera distance (zooming in/out)
            Vector3 offset = orbit.m_FollowOffset;
            offset.y -= scrollInput * zoomSpeed;
            offset.y = Mathf.Clamp(offset.y, minZoom, maxZoom);
            orbit.m_FollowOffset = offset;
        }
    }
}