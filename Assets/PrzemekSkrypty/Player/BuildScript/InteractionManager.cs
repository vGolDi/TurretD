using UnityEngine;
using Photon.Pun;

/// <summary>
/// Handles mouse click interactions with turrets
/// Uses raycast to detect clickable objects
/// </summary>
public class InteractionManager : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField, Tooltip("Layer containing turrets")]
    private LayerMask interactableLayers;

    [SerializeField, Tooltip("Maximum interaction distance")]
    private float maxInteractionDistance = 100f;

    private Camera cam;
    private BuildManager buildManager;
    private PhotonView photonView;

    private void Awake()
    {
        buildManager = GetComponent<BuildManager>();
        photonView = GetComponent<PhotonView>();
    }

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        // Only process for local player
        if (!photonView.IsMine) return;

        // Don't process clicks during build mode
        if (buildManager != null && buildManager.IsInBuildMode())
        {
            return;
        }

        // Check for left mouse click
        if (Input.GetMouseButtonDown(0))
        {
            TryInteract();
        }
    }

    /// <summary>
    /// Casts ray from mouse position and attempts to interact with hit object
    /// </summary>
    private void TryInteract()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxInteractionDistance, interactableLayers))
        {
            // Check if hit object has interactable component
            var interactable = hit.collider.GetComponentInParent<TurretInteract>();

            if (interactable != null)
            {
                interactable.OnClicked();
            }
        }
    }

    // Debug visualization
    private void OnDrawGizmos()
    {
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(ray.origin, ray.direction * maxInteractionDistance);
    }
}