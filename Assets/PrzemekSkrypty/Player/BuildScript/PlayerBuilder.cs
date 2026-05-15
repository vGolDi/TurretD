using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using ElementumDefense.Cards;

/// <summary>
/// Handles turret ghost preview, placement validation, and building.
/// Ghost uses transparent pulsing materials. Range shown via LineRenderer circle.
/// Validates: path collision, turret overlap, slope angle, terrain bounds.
/// </summary>
public class PlayerBuilder : MonoBehaviour
{
    [Header("Build Configuration")]
    [SerializeField] private GameObject turretLogicPrefab;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private LayerMask buildableLayers;
    [SerializeField] private float maxBuildDistance = 100f;

    [Header("Placement Rules")]
    [Tooltip("Grid snap size (0 = no snapping)")]
    [SerializeField] private float gridSnapSize = 1f;

    [Tooltip("Maximum slope angle in degrees (prevents building on walls/cliffs)")]
    [SerializeField] private float maxSlopeAngle = 30f;

    [Tooltip("Minimum distance between turrets")]
    [SerializeField] private float minTurretSpacing = 2f;

    [Tooltip("Layers that block placement (Path, Water, etc.)")]
    [SerializeField] private LayerMask blockedLayers;

    [Header("Ghost Visual")]
    [Tooltip("Valid placement color")]
    [SerializeField] private Color validColor = new Color(0.1f, 0.9f, 0.3f, 0.4f);

    [Tooltip("Invalid placement color")]
    [SerializeField] private Color invalidColor = new Color(0.9f, 0.15f, 0.1f, 0.4f);

    [Tooltip("Pulse speed (how fast the ghost throbs)")]
    [SerializeField] private float pulseSpeed = 2f;

    [Tooltip("Pulse intensity range")]
    [SerializeField] private float pulseMin = 0.25f;
    [SerializeField] private float pulseMax = 0.5f;

    [Header("Range Indicator")]
    [Tooltip("Number of segments in the range circle")]
    [SerializeField] private int rangeSegments = 64;

    [Tooltip("Range circle line width")]
    [SerializeField] private float rangeLineWidth = 0.06f;

    [Tooltip("Range circle valid color")]
    [SerializeField] private Color rangeValidColor = new Color(0.2f, 0.8f, 0.4f, 0.6f);

    [Tooltip("Range circle invalid color")]
    [SerializeField] private Color rangeInvalidColor = new Color(0.8f, 0.2f, 0.15f, 0.4f);

    // Runtime
    private Camera cam;
    private GameObject ghostInstance;
    private LineRenderer rangeLineRenderer;
    private TurretData currentTurretData;
    private bool canPlace = false;
    private string invalidReason = "";

    private PhotonView photonView;
    private BuildManager buildManager;
    private PlayerCardManager cardManager;

    // Cached materials per ghost instance
    private List<Material> ghostMaterials = new List<Material>();
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();

    // ==========================================
    // INITIALIZATION
    // ==========================================

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        buildManager = GetComponent<BuildManager>();
        cardManager = GetComponent<PlayerCardManager>();
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            // Camera.main can be null on remote players; find our own
            cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
        }
    }

    // ==========================================
    // BUILD MODE ACTIVATION
    // ==========================================

    public void ActivateBuildMode(TurretData turretData)
    {
        if (turretData == null) return;

        // Clean up any existing ghost
        DeactivateBuildMode();

        currentTurretData = turretData;

        // --- GHOST ---
        if (currentTurretData.displayPrefab != null)
        {
            ghostInstance = Instantiate(currentTurretData.displayPrefab);
            ghostInstance.name = "Ghost_" + turretData.turretName;

            // Disable ALL colliders on ghost (so raycasts don't hit it)
            foreach (var col in ghostInstance.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }

            // Disable any scripts on ghost that shouldn't run
            foreach (var mb in ghostInstance.GetComponentsInChildren<MonoBehaviour>())
            {
                if (mb != null && mb.GetType() != typeof(Transform))
                    mb.enabled = false;
            }

            // Cache original materials and create ghost versions
            SetupGhostMaterials();
        }

        // --- RANGE INDICATOR (LineRenderer circle) ---
        CreateRangeIndicator();

        // Hide until first valid raycast
        if (ghostInstance != null) ghostInstance.SetActive(false);
    }

    public void DeactivateBuildMode()
    {
        if (ghostInstance != null)
        {
            // Clean up cloned materials
            foreach (var mat in ghostMaterials)
            {
                if (mat != null) Destroy(mat);
            }
            ghostMaterials.Clear();
            originalMaterials.Clear();

            Destroy(ghostInstance);
            ghostInstance = null;
        }

        if (rangeLineRenderer != null)
        {
            Destroy(rangeLineRenderer.gameObject);
            rangeLineRenderer = null;
        }

        currentTurretData = null;
        canPlace = false;
        invalidReason = "";
    }

    // ==========================================
    // UPDATE LOOP
    // ==========================================

    private void Update()
    {
        if (!photonView.IsMine) return;
        if (ghostInstance == null) return;

        UpdateGhostPosition();
        UpdateGhostVisual();
        UpdateRangeIndicator();

        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            PlaceTurret();
        }
    }

    // ==========================================
    // GHOST POSITION (RAYCAST + SNAP)
    // ==========================================

    private void UpdateGhostPosition()
    {
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, buildableLayers))
        {
            Vector3 pos = hit.point + new Vector3(0, yOffset, 0);

            // Grid snapping
            if (gridSnapSize > 0.01f)
            {
                pos.x = Mathf.Round(pos.x / gridSnapSize) * gridSnapSize;
                pos.z = Mathf.Round(pos.z / gridSnapSize) * gridSnapSize;
            }

            ghostInstance.SetActive(true);
            ghostInstance.transform.position = pos;
            ghostInstance.transform.rotation = Quaternion.identity;

            // Validate placement
            canPlace = ValidatePlacement(pos, hit.normal, out invalidReason);
        }
        else
        {
            // Mouse is off buildable terrain
            ghostInstance.SetActive(false);
            canPlace = false;
            invalidReason = "Out of range";
        }
    }

    // ==========================================
    // PLACEMENT VALIDATION
    // ==========================================

    private bool ValidatePlacement(Vector3 position, Vector3 surfaceNormal, out string reason)
    {
        reason = "";

        // 1. Slope check — prevent building on walls/steep terrain
        float slopeAngle = Vector3.Angle(Vector3.up, surfaceNormal);
        if (slopeAngle > maxSlopeAngle)
        {
            reason = "Too steep";
            return false;
        }

        // 2. Path/blocked layer overlap — use the ghost's actual bounds
        Bounds ghostBounds = CalculateGhostBounds();
        Vector3 halfExtents = ghostBounds.extents;
        halfExtents.y = Mathf.Max(halfExtents.y, 0.5f); // Minimum height for detection

        // Check blocked layers (Path, Water, etc.)
        if (blockedLayers.value != 0)
        {
            Collider[] blocked = Physics.OverlapBox(
                position + new Vector3(0, halfExtents.y, 0),
                halfExtents,
                Quaternion.identity,
                blockedLayers
            );
            if (blocked.Length > 0)
            {
                reason = "Blocked area";
                return false;
            }
        }

        // Also check "Path" layer by name as fallback
        Collider[] pathHits = Physics.OverlapBox(
            position + new Vector3(0, halfExtents.y, 0),
            halfExtents,
            Quaternion.identity,
            LayerMask.GetMask("Path")
        );
        if (pathHits.Length > 0)
        {
            reason = "Path blocked";
            return false;
        }

        // 3. Turret spacing — prevent stacking
        Collider[] nearby = Physics.OverlapSphere(position, minTurretSpacing);
        foreach (var col in nearby)
        {
            if (col.GetComponent<Turret>() != null)
            {
                reason = "Too close to turret";
                return false;
            }
        }

        return true;
    }

    private Bounds CalculateGhostBounds()
    {
        if (ghostInstance == null) return new Bounds(Vector3.zero, Vector3.one);

        Renderer[] renderers = ghostInstance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(ghostInstance.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        // Convert to local space extents
        return new Bounds(Vector3.zero, bounds.size);
    }

    // ==========================================
    // GHOST VISUAL (PULSE + COLOR)
    // ==========================================

    private void SetupGhostMaterials()
    {
        ghostMaterials.Clear();
        originalMaterials.Clear();

        Renderer[] renderers = ghostInstance.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            // Skip particle systems
            if (rend is ParticleSystemRenderer) continue;

            Material[] newMats = new Material[rend.materials.Length];
            for (int i = 0; i < rend.materials.Length; i++)
            {
                // Create a ghost material clone
                Material ghostMat = new Material(rend.materials[i]);
                ghostMat.name = rend.materials[i].name + "_Ghost";

                // Force transparent rendering
                SetMaterialTransparent(ghostMat);

                newMats[i] = ghostMat;
                ghostMaterials.Add(ghostMat);
            }
            rend.materials = newMats;
        }
    }

    private void SetMaterialTransparent(Material mat)
    {
        // Handle URP/Built-in shader transparency
        if (mat.HasProperty("_Surface"))
        {
            // URP Lit
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 0f);   // Alpha
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        else
        {
            // Standard/Built-in
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
    }

    private void UpdateGhostVisual()
    {
        if (ghostInstance == null || !ghostInstance.activeSelf) return;

        // Pulse alpha
        float pulse = Mathf.Lerp(pulseMin, pulseMax,
            (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) * 0.5f);

        Color targetColor = canPlace ? validColor : invalidColor;
        targetColor.a = pulse;

        // Apply to all ghost materials
        foreach (var mat in ghostMaterials)
        {
            if (mat == null) continue;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", targetColor);
            else if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", targetColor);
        }
    }

    // ==========================================
    // RANGE INDICATOR (LineRenderer Circle)
    // ==========================================

    private void CreateRangeIndicator()
    {
        if (currentTurretData == null) return;

        GameObject rangeGo = new GameObject("RangeIndicator");
        rangeLineRenderer = rangeGo.AddComponent<LineRenderer>();

        // Setup LineRenderer
        rangeLineRenderer.useWorldSpace = true;
        rangeLineRenderer.loop = true;
        rangeLineRenderer.positionCount = rangeSegments;
        rangeLineRenderer.startWidth = rangeLineWidth;
        rangeLineRenderer.endWidth = rangeLineWidth;
        rangeLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rangeLineRenderer.receiveShadows = false;

        // Material — simple unlit line
        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        lineMat.color = rangeValidColor;
        rangeLineRenderer.material = lineMat;

        rangeLineRenderer.startColor = rangeValidColor;
        rangeLineRenderer.endColor = rangeValidColor;
    }

    private void UpdateRangeIndicator()
    {
        if (rangeLineRenderer == null) return;
        if (ghostInstance == null || !ghostInstance.activeSelf)
        {
            rangeLineRenderer.enabled = false;
            return;
        }

        rangeLineRenderer.enabled = true;

        // Get modified range
        float displayRange = currentTurretData.range;
        if (cardManager != null)
        {
            displayRange = cardManager.GetModifiedRange(
                currentTurretData.range,
                currentTurretData.elementType
            );
        }

        // Build circle points
        Vector3 center = ghostInstance.transform.position;
        float angleStep = 360f / rangeSegments;

        for (int i = 0; i < rangeSegments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = center.x + Mathf.Cos(angle) * displayRange;
            float z = center.z + Mathf.Sin(angle) * displayRange;

            // Sample terrain height at each point for a ground-hugging circle
            float y = center.y + 0.05f;
            if (Physics.Raycast(new Vector3(x, center.y + 10f, z), Vector3.down,
                    out RaycastHit groundHit, 20f, buildableLayers))
            {
                y = groundHit.point.y + 0.05f;
            }

            rangeLineRenderer.SetPosition(i, new Vector3(x, y, z));
        }

        // Color
        Color col = canPlace ? rangeValidColor : rangeInvalidColor;

        // Subtle pulse on range too
        float pulse = (Mathf.Sin(Time.time * pulseSpeed * 0.5f * Mathf.PI) + 1f) * 0.5f;
        col.a = Mathf.Lerp(col.a * 0.6f, col.a, pulse);

        rangeLineRenderer.startColor = col;
        rangeLineRenderer.endColor = col;
        rangeLineRenderer.material.color = col;
    }

    // ==========================================
    // PLACEMENT
    // ==========================================

    private void PlaceTurret()
    {
        if (currentTurretData == null || ghostInstance == null) return;

        // Calculate final cost with card modifiers
        int finalCost = currentTurretData.cost;
        if (cardManager != null)
        {
            finalCost = cardManager.GetModifiedTurretCost(currentTurretData.cost);
        }

        if (!PlayerGold.LocalInstance.SpendGold(finalCost))
        {
            Debug.Log("[PlayerBuilder] Not enough gold!");
            buildManager?.ExitBuildMode();
            return;
        }

        Vector3 buildPos = ghostInstance.transform.position;

        // Spawn turret logic
        GameObject turret = Instantiate(
            turretLogicPrefab,
            buildPos,
            Quaternion.identity
        );

        StartCoroutine(DelayedInitializeTurret(turret, currentTurretData));

        Debug.Log($"[PlayerBuilder] Built {currentTurretData.turretName} " +
                  $"for {finalCost} gold (base: {currentTurretData.cost}) " +
                  $"at {buildPos}");

        if (buildManager != null)
        {
            buildManager.OnTurretBuilt();
        }
    }

    private IEnumerator DelayedInitializeTurret(GameObject turret, TurretData data)
    {
        yield return null;

        Turret turretScript = turret.GetComponent<Turret>();
        if (turretScript != null)
        {
            turretScript.Initialize(data, photonView);
        }
    }

    // ==========================================
    // PUBLIC API
    // ==========================================

    /// <summary>Whether the ghost is currently over a valid build position</summary>
    public bool CanPlace => canPlace;

    /// <summary>Reason why placement is invalid (empty if valid)</summary>
    public string InvalidReason => invalidReason;

    /// <summary>Whether build mode is active</summary>
    public bool IsActive => ghostInstance != null;
}