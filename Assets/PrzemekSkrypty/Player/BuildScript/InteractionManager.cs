using UnityEngine;
using Photon.Pun;
using ElementumDefense.Enemies;
using ElementumDefense.Turrets;


namespace ElementumDefense.Players
{
/// <summary>
/// Handles mouse click interactions with turrets
/// Uses raycast to detect clickable objects
/// </summary>
public class InteractionManager : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField, Tooltip("Layer containing turrets")]
    private LayerMask interactableLayers;

    [SerializeField, Tooltip("Layer containing enemies (do klikania w opancerzonych)")]
    private LayerMask enemyLayers;

    [SerializeField, Tooltip("Maximum interaction distance")]
    private float maxInteractionDistance = 100f;

    [Header("Click Tolerance")]
    [SerializeField, Tooltip("Promień klikania w wroga (SphereCast). Większy = łatwiej trafić małego/ruszającego się wroga. " +
                              "0 = tylko Raycast (precyzyjnie). 0.4 = standard.")]
    private float enemyClickRadius = 0.4f;

    [SerializeField, Tooltip("Maks. odległość kursora od ikony wroga na EKRANIE (w pikselach), " +
                              "żeby zaliczyć fallback. 0 = wyłącz fallback. Standard: 50px.")]
    private float enemyScreenFallbackPixels = 50f;

    [Header("Debug")]
    [SerializeField, Tooltip("Włącz logi diagnostyczne dla kliknięć")]
    private bool debugLogs = false;

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

        // ============================================================
        // 1) PRÓBA TRAFIENIA OPANCERZONEGO WROGA
        // ============================================================
        // Trzy poziomy detekcji - gdy poprzedni nie znajduje, próbujemy szerszego.

        EnemyArmor armorTarget = null;
        string detectionMode = "none";

        // Poziom A: precyzyjny Raycast (najszybszy, gdy myszka idealnie nad wrogiem)
        if (enemyLayers.value != 0 &&
            Physics.Raycast(ray, out RaycastHit precisionHit, maxInteractionDistance, enemyLayers))
        {
            EnemyArmor a = precisionHit.collider.GetComponentInParent<EnemyArmor>();
            if (a != null && a.IsArmored)
            {
                armorTarget = a;
                detectionMode = "Raycast";
            }
        }

        // Poziom B: SphereCast z tolerancją (łapie ruszających się/małych wrogów)
        if (armorTarget == null && enemyLayers.value != 0 && enemyClickRadius > 0f)
        {
            if (Physics.SphereCast(ray, enemyClickRadius, out RaycastHit sphereHit,
                                    maxInteractionDistance, enemyLayers))
            {
                EnemyArmor a = sphereHit.collider.GetComponentInParent<EnemyArmor>();
                if (a != null && a.IsArmored)
                {
                    armorTarget = a;
                    detectionMode = "SphereCast";
                }
            }
        }

        // Poziom C: fallback - przeszukaj WIDOCZNYCH na ekranie opancerzonych wrogów
        //          i wybierz tego, którego ekranowa pozycja jest najbliżej kursora myszy.
        //          Daje to dobre dopasowanie z perspektywy gracza ("widzę wroga obok kursora").
        if (armorTarget == null && enemyScreenFallbackPixels > 0f)
        {
            Vector2 mousePos2D = Input.mousePosition;
            float bestDistPx = enemyScreenFallbackPixels;
            EnemyArmor bestArmor = null;

            foreach (var a in EnemyArmor.AllArmored)
            {
                if (a == null || !a.IsArmored) continue;

                Vector3 worldPos = a.transform.position + Vector3.up * 0.7f; // mniej więcej środek modelu
                Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

                // Pomiń wrogów za kamerą lub poza ekranem
                if (screenPos.z < 0f) continue;

                float distPx = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), mousePos2D);
                if (distPx < bestDistPx)
                {
                    bestDistPx = distPx;
                    bestArmor = a;
                }
            }

            if (bestArmor != null)
            {
                armorTarget = bestArmor;
                detectionMode = $"ScreenFallback@{bestDistPx:F0}px";
            }
        }

        if (armorTarget != null)
        {
            if (debugLogs)
                Debug.Log($"[InteractionManager] Klik trafił opancerzonego wroga {armorTarget.name} (tryb: {detectionMode})");
            armorTarget.OnPlayerClicked();
            return;
        }

        // ============================================================
        // 2) STANDARDOWA LOGIKA - KLIK W TURRET
        // ============================================================
        if (Physics.Raycast(ray, out RaycastHit hit, maxInteractionDistance, interactableLayers))
        {
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
}
