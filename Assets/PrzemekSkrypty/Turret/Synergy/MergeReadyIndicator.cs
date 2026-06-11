using UnityEngine;
using Photon.Pun;


namespace ElementumDefense.Turrets
{
/// <summary>
/// Attach this to the ROOT turret GameObject (same level as TurretInteract/Collider).
/// Searches for the Turret component on self, parent, or children (Turret_Logic pattern).
/// Periodically checks if a compatible level-max partner is in range and shows a VFX ring.
/// TurretUiController can read CanMerge and call TryMerge().
/// </summary>
public class MergeReadyIndicator : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Ring/halo GameObject to show when merge is available (can be a particle / projector / LineRenderer ring)")]
    [SerializeField] private GameObject mergeReadyVFX;

    [Tooltip("Color of the ring when merge is ready")]
    [SerializeField] private Color readyColor = new Color(1f, 0.9f, 0.2f, 0.9f);

    [Header("Check Interval")]
    [SerializeField] private float checkInterval = 1f;

    private Turret turret;
    private TurretMergeManager mergeManager;
    private Turret currentPartner;
    private float timer;

    private void Awake()
    {
        // Support both "Turret on same GO" and "Turret on child Turret_Logic" patterns
        turret = GetComponent<Turret>();
        if (turret == null) turret = GetComponentInChildren<Turret>();
        if (turret == null) turret = GetComponentInParent<Turret>();

        if (mergeReadyVFX != null) mergeReadyVFX.SetActive(false);
    }

    private void Start()
    {
        // Only run on local player's turrets
        var ownerView = turret?.GetOwner();
        if (ownerView != null && !ownerView.IsMine)
        {
            enabled = false;
            return;
        }

        mergeManager = FindFirstObjectByType<TurretMergeManager>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer < checkInterval) return;
        timer = 0f;

        if (mergeManager == null) return;

        Turret partner = mergeManager.FindMergePartner(turret);

        if (partner != currentPartner)
        {
            currentPartner = partner;

            bool ready = currentPartner != null;
            if (mergeReadyVFX != null) mergeReadyVFX.SetActive(ready);

            Debug.Log(ready
                ? $"[MergeIndicator] {name} can merge with {partner.name}!"
                : $"[MergeIndicator] {name}: no merge partner nearby");
        }
    }

    /// <summary>
    /// Called by TurretUiController when player clicks "Merge" button.
    /// </summary>
    public bool TryMerge()
    {
        if (mergeManager == null || currentPartner == null) return false;
        return mergeManager.TryMerge(turret, currentPartner);
    }

    public bool HasMergePartner => currentPartner != null;

    /// <summary>True when this turret has no upgradePaths AND canMerge is true</summary>
    private bool IsMaxLevel => turret != null &&
                               turret.TurretData != null &&
                               turret.TurretData.canMerge &&
                               (turret.TurretData.upgradePaths == null ||
                                turret.TurretData.upgradePaths.Length == 0);

    [ContextMenu("DEBUG: Try Merge")]
    public void DebugMerge() => TryMerge();
}
}
