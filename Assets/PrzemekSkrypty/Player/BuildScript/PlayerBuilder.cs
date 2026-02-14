using UnityEngine;
using System.Collections;
using Photon.Pun;
using ElementumDefense.Cards;

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

    private Camera cam;
    private GameObject ghostTurretInstance;
    private GameObject rangeIndicatorInstance;
    private TurretData currentTurretToBuild;
    private bool canPlaceTurret = false;

    private PhotonView photonView;
    private BuildManager buildManager;
    private PlayerCardManager cardManager; // ← NOWE

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        buildManager = GetComponent<BuildManager>();
        cardManager = GetComponent<PlayerCardManager>(); // ← NOWE
        cam = Camera.main;
    }

    private void Start()
    {
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

    public void ActivateBuildMode(TurretData turretData)
    {
        if (ghostTurretInstance != null) Destroy(ghostTurretInstance);
        if (rangeIndicatorInstance != null) Destroy(rangeIndicatorInstance);

        currentTurretToBuild = turretData;

        ghostTurretInstance = Instantiate(currentTurretToBuild.displayPrefab);

        foreach (var collider in ghostTurretInstance.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        if (rangeIndicatorPrefab != null)
        {
            rangeIndicatorInstance = Instantiate(rangeIndicatorPrefab);

            // ========== NAPRAWIONE: Range indicator uses modified range ==========
            float displayRange = currentTurretToBuild.range;
            if (cardManager != null)
            {
                displayRange = cardManager.GetModifiedRange(
                    currentTurretToBuild.range,
                    currentTurretToBuild.elementType
                );
            }

            float diameter = displayRange * 2f;
            rangeIndicatorInstance.transform.localScale = new Vector3(diameter, 0.01f, diameter);
            // ===================================================================

            Renderer renderer = rangeIndicatorInstance.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = validPlacementMaterial;
            }

            Collider collider = rangeIndicatorInstance.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }
        }
    }

    public void DeactivateBuildMode()
    {
        if (ghostTurretInstance != null) Destroy(ghostTurretInstance);
        if (rangeIndicatorInstance != null) Destroy(rangeIndicatorInstance);
        currentTurretToBuild = null;
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        if (ghostTurretInstance != null)
        {
            MoveGhostTurret();

            if (Input.GetMouseButtonDown(0) && canPlaceTurret)
            {
                PlaceTurret();
            }
        }
    }

    private void MoveGhostTurret()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxBuildDistance, buildableLayers))
        {
            Vector3 buildPosition = hit.point + new Vector3(0, yOffset, 0);

            ghostTurretInstance.transform.position = buildPosition;
            ghostTurretInstance.transform.rotation = Quaternion.identity;

            if (rangeIndicatorInstance != null)
            {
                rangeIndicatorInstance.transform.position = buildPosition;
            }

            canPlaceTurret = IsValidPlacement(buildPosition);
            SetGhostMaterial(canPlaceTurret ? validPlacementMaterial : invalidPlacementMaterial);
        }
        else
        {
            ghostTurretInstance.transform.position = new Vector3(0, -1000, 0);
            if (rangeIndicatorInstance != null)
            {
                rangeIndicatorInstance.transform.position = new Vector3(0, -1000, 0);
            }
            canPlaceTurret = false;
            SetGhostMaterial(invalidPlacementMaterial);
        }
    }

    private bool IsValidPlacement(Vector3 position)
    {
        Collider[] pathOverlaps = Physics.OverlapBox(
            position,
            ghostTurretInstance.transform.localScale / 2f,
            Quaternion.identity,
            LayerMask.GetMask("Path")
        );

        if (pathOverlaps.Length > 0) return false;

        Collider[] turretOverlaps = Physics.OverlapSphere(position, 1f);
        foreach (var col in turretOverlaps)
        {
            if (col.GetComponent<Turret>() != null)
            {
                return false;
            }
        }

        return true;
    }

    private void SetGhostMaterial(Material material)
    {
        foreach (var renderer in ghostTurretInstance.GetComponentsInChildren<Renderer>())
        {
            renderer.material = material;
        }
    }

    private void PlaceTurret()
    {
        // ========== NAPRAWIONE: Use modified cost ==========
        int finalCost = currentTurretToBuild.cost;
        if (cardManager != null)
        {
            finalCost = cardManager.GetModifiedTurretCost(currentTurretToBuild.cost);
        }

        if (!PlayerGold.LocalInstance.SpendGold(finalCost))
        {
            Debug.Log("[PlayerBuilder] Not enough gold!");
            buildManager.ExitBuildMode();
            return;
        }
        // ===================================================

        GameObject turret = Instantiate(
            turretLogicPrefab,
            ghostTurretInstance.transform.position,
            Quaternion.identity
        );

        StartCoroutine(DelayedInitializeTurret(turret, currentTurretToBuild));

        Debug.Log($"[PlayerBuilder] Built {currentTurretToBuild.turretName} " +
                  $"for {finalCost} gold (base: {currentTurretToBuild.cost})");

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
}