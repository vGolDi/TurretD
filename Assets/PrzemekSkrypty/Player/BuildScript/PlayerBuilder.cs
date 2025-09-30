using UnityEngine;
using System.Collections;
using Photon.Pun;

public class PlayerBuilder : MonoBehaviour
{
    [Header("Konfiguracja Budowania")]
    [SerializeField] private GameObject turretLogicPrefab;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private LayerMask buildableLayers;
    [SerializeField] private float maxBuildDistance = 100f;

    [Header("Materia³y dla 'Ducha'")]
    [SerializeField] private Material validPlacementMaterial;
    [SerializeField] private Material invalidPlacementMaterial;

    [Header("Zasiêg wie¿yczki (Prefab)")] // ADDED
    [SerializeField] private GameObject rangeIndicatorPrefab; // ADDED

    private Camera cam;
    private GameObject ghostTurretInstance;
    private GameObject rangeIndicatorInstance; // ADDED
    private TurretData currentTurretToBuild;
    private bool canPlaceTurret = false;


    private PhotonView photonView;
    private BuildManager buildManager;
    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        cam = Camera.main;
       
        buildManager = GetComponent<BuildManager>();
    }
    private void Start()
    {
        // --- DODAJ TEN FRAGMENT ---
        // Jeœli ten obiekt gracza nale¿y do mnie (jestem lokalnym graczem),
        // to wyœlij RPC do wszystkich, ¿eby siê zameldowaæ.
        if (photonView.IsMine)
        {
            photonView.RPC("AnnouncePlayerJoined", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.NickName);
        }
    }

    // Ta metoda zostanie wywo³ana na wszystkich instancjach gry (u wszystkich graczy)
    [PunRPC]
    void AnnouncePlayerJoined(string nickName)
    {
        // Sprawdzamy, czy nickName nie jest pusty, i przypisujemy domyœlny, jeœli tak
        if (string.IsNullOrEmpty(nickName))
        {
            nickName = $"Gracz_{photonView.Owner.ActorNumber}";
        }

        Debug.Log($"<color=green><b>GRACZ DO£¥CZY£ DO GRY:</b> {nickName} (ID: {photonView.ViewID})</color>");
    }

    public void ActivateBuildMode(TurretData turretData)
    {
        if (ghostTurretInstance != null) Destroy(ghostTurretInstance);
        if (rangeIndicatorInstance != null) Destroy(rangeIndicatorInstance); // ADDED

        currentTurretToBuild = turretData;
        ghostTurretInstance = Instantiate(currentTurretToBuild.displayPrefab);

        foreach (var collider in ghostTurretInstance.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        // ADDED: Stwórz wizualizacjê zasiêgu
        if (rangeIndicatorPrefab != null)
        {
            rangeIndicatorInstance = Instantiate(rangeIndicatorPrefab);
            float diameter = currentTurretToBuild.range * 2f;
            rangeIndicatorInstance.transform.localScale = new Vector3(diameter, 0.01f, diameter);
        }

        // Ustaw przezroczysty materia³
        Renderer renderer = rangeIndicatorInstance.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = validPlacementMaterial; // Lub osobny materia³ do zasiêgu
        }

        // Wy³¹cz kolizje
        Collider colider = rangeIndicatorInstance.GetComponent<Collider>();
        if (colider != null)
        {
            colider.enabled = false;
        }

    }

    public void DeactivateBuildMode()
    {
        if (ghostTurretInstance != null) Destroy(ghostTurretInstance);
        if (rangeIndicatorInstance != null) Destroy(rangeIndicatorInstance); // ADDED
        currentTurretToBuild = null;
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }
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

            if (rangeIndicatorInstance != null) // ADDED
            {
                rangeIndicatorInstance.transform.position = buildPosition;
            }

            Collider[] overlaps = Physics.OverlapBox(
                ghostTurretInstance.transform.position,
                ghostTurretInstance.transform.localScale / 2f,
                Quaternion.identity,
                LayerMask.GetMask("Path")
            );

            if (overlaps.Length > 0)
            {
                canPlaceTurret = false;
                SetGhostMaterial(invalidPlacementMaterial);
            }
            else
            {
                canPlaceTurret = true;
                SetGhostMaterial(validPlacementMaterial);
            }
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

    private void SetGhostMaterial(Material material)
    {
        foreach (var renderer in ghostTurretInstance.GetComponentsInChildren<Renderer>())
        {
            renderer.material = material;
        }
    }

    private void PlaceTurret()
    {
        if (!PlayerGold.Instance.SpendGold(currentTurretToBuild.cost))
        {
            Debug.Log("Za ma³o z³ota!");
            buildManager.ExitBuildMode();
            return;
        }

        GameObject turret = Instantiate(turretLogicPrefab, ghostTurretInstance.transform.position, Quaternion.identity);
        StartCoroutine(DelayedInitializeTurret(turret, currentTurretToBuild));

        Debug.Log($"[Builder] Postawiono {currentTurretToBuild.turretName}.");

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
            turretScript.Initialize(data);
        }
    }
}
