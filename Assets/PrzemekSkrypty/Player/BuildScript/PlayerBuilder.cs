using UnityEngine;
using System.Collections;
using Photon.Pun;

/// <summary>
/// Handles visual ghost turret placement and building logic
/// Works only for local player
/// </summary>
public class PlayerBuilder : MonoBehaviour
{
    [Header("Build Configuration")]
    [SerializeField] private GameObject turretLogicPrefab;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private LayerMask buildableLayers;
    [SerializeField] private float maxBuildDistance = 100f;

    [Header("Ghost Materials")]
    [SerializeField] private Material validPlacementMaterial;
    [SerializeField] private Material invalidPlacementMaterial;

    [Header("Range Indicator")]
    [SerializeField] private GameObject rangeIndicatorPrefab;

    // Runtime variables
    private Camera cam;
    private GameObject ghostTurretInstance;
    private GameObject rangeIndicatorInstance;
    private TurretData currentTurretToBuild;
    private bool canPlaceTurret = false;

    private PhotonView photonView;
    private BuildManager buildManager;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        buildManager = GetComponent<BuildManager>();
        cam = Camera.main;
    }

    private void Start()
    {
        // Announce player join (for debugging multiplayer)
        if (photonView.IsMine)
        {
            string nickName = PhotonNetwork.LocalPlayer.NickName;
            if (string.IsNullOrEmpty(nickName))
            {
                nickName = $"Player_{photonView.Owner.ActorNumber}";
            }

            Debug.Log($"<color=green>[PlayerBuilder] Player joined: {nickName}</color>");
        }
    }

    /// <summary>
    /// Enters build mode with ghost turret preview
    /// </summary>
    public void ActivateBuildMode(TurretData turretData)
    {
        // Clean up previous ghost
        if (ghostTurretInstance != null) Destroy(ghostTurretInstance);
        if (rangeIndicatorInstance != null) Destroy(rangeIndicatorInstance);

        currentTurretToBuild = turretData;

        // Create ghost turret
        ghostTurretInstance = Instantiate(currentTurretToBuild.displayPrefab);

        // Disable all colliders on ghost
        foreach (var collider in ghostTurretInstance.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        // Create range indicator
        if (rangeIndicatorPrefab != null)
        {
            rangeIndicatorInstance = Instantiate(rangeIndicatorPrefab);
            float diameter = currentTurretToBuild.range * 2f;
            rangeIndicatorInstance.transform.localScale = new Vector3(diameter, 0.01f, diameter);

            // Set transparent material
            Renderer renderer = rangeIndicatorInstance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = validPlacementMaterial;
            }

            // Disable collider
            Collider collider = rangeIndicatorInstance.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }

    /// <summary>
    /// Exits build mode and destroys ghost
    /// </summary>
    public void DeactivateBuildMode()
    {
        if (ghostTurretInstance != null) Destroy(ghostTurretInstance);
        if (rangeIndicatorInstance != null) Destroy(rangeIndicatorInstance);
        currentTurretToBuild = null;
    }

    private void Update()
    {
        // Only for local player
        if (!photonView.IsMine) return;

        if (ghostTurretInstance != null)
        {
            MoveGhostTurret();

            // Place turret on LMB
            if (Input.GetMouseButtonDown(0) && canPlaceTurret)
            {
                PlaceTurret();
            }
        }
    }

    /// <summary>
    /// Updates ghost turret position based on mouse raycast
    /// Checks for valid placement (no overlap with paths, other turrets)
    /// </summary>
    private void MoveGhostTurret()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, buildableLayers))
        {
            Vector3 buildPosition = hit.point + new Vector3(0, yOffset, 0);

            // Update ghost position
            ghostTurretInstance.transform.position = buildPosition;
            ghostTurretInstance.transform.rotation = Quaternion.identity;

            // Update range indicator
            if (rangeIndicatorInstance != null)
            {
                rangeIndicatorInstance.transform.position = buildPosition;
            }

            // Check for overlaps with invalid areas (paths, other turrets)
            canPlaceTurret = IsValidPlacement(buildPosition);

            // Update ghost material
            SetGhostMaterial(canPlaceTurret ? validPlacementMaterial : invalidPlacementMaterial);
        }
        else
        {
            // Hide ghost when not aiming at buildable surface
            ghostTurretInstance.transform.position = new Vector3(0, -1000, 0);
            if (rangeIndicatorInstance != null)
            {
                rangeIndicatorInstance.transform.position = new Vector3(0, -1000, 0);
            }
            canPlaceTurret = false;
            SetGhostMaterial(invalidPlacementMaterial);
        }
    }

    /// <summary>
    /// Checks if placement is valid (no overlaps with paths or other turrets)
    /// </summary>
    private bool IsValidPlacement(Vector3 position)
    {
        // Check overlap with Path layer
        Collider[] pathOverlaps = Physics.OverlapBox(
            position,
            ghostTurretInstance.transform.localScale / 2f,
            Quaternion.identity,
            LayerMask.GetMask("Path")
        );

        if (pathOverlaps.Length > 0)
        {
            return false; // Blocking path
        }

        // Check overlap with other turrets
        Collider[] turretOverlaps = Physics.OverlapSphere(position, 1f);
        foreach (var col in turretOverlaps)
        {
            if (col.GetComponent<Turret>() != null)
            {
                return false; // Too close to another turret
            }
        }

        return true;
    }

    /// <summary>
    /// Applies material to all renderers in ghost turret
    /// </summary>
    private void SetGhostMaterial(Material material)
    {
        foreach (var renderer in ghostTurretInstance.GetComponentsInChildren<Renderer>())
        {
            renderer.material = material;
        }
    }

    /// <summary>
    /// Actually places the turret (deducts gold, spawns GameObject)
    /// </summary>
    private void PlaceTurret()
    {
        // Double-check gold
        if (!PlayerGold.LocalInstance.SpendGold(currentTurretToBuild.cost))
        {
            Debug.Log("[PlayerBuilder] Not enough gold!");
            buildManager.ExitBuildMode();
            return;
        }

        // Instantiate turret logic prefab
        GameObject turret = Instantiate(
            turretLogicPrefab,
            ghostTurretInstance.transform.position,
            Quaternion.identity
        );

        // Initialize with data after next frame (ensures all components are ready)
        StartCoroutine(DelayedInitializeTurret(turret, currentTurretToBuild));

        Debug.Log($"[PlayerBuilder] Built {currentTurretToBuild.turretName} at {turret.transform.position}");

        // Notify BuildManager
        if (buildManager != null)
        {
            buildManager.OnTurretBuilt();
        }

        // TODO: Play build sound/VFX
    }

    /// <summary>
    /// Waits one frame then initializes turret (ensures components are ready)
    /// </summary>
    private IEnumerator DelayedInitializeTurret(GameObject turret, TurretData data)
    {
        yield return null;

        Turret turretScript = turret.GetComponent<Turret>();
        if (turretScript != null)
        {
            turretScript.Initialize(data, photonView); // Pass owner's PhotonView
        }
    }
}