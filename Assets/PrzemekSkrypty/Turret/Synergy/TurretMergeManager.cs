using UnityEngine;
using System.Collections.Generic;
using ElementumDefense.Elements;
using ElementumDefense.StatusEffects;
using ElementumDefense.Players;


namespace ElementumDefense.Turrets
{
/// <summary>
/// Manages turret merge system.
/// When two compatible level-3 turrets are within merge range,
/// shows a merge option. On confirm, destroys both and spawns
/// a merged (level-4 synergy) turret in the middle.
///
/// Attach to the local player object alongside BuildManager.
/// </summary>
public class TurretMergeManager : MonoBehaviour
{
    [Header("Merge Configuration")]
    [Tooltip("Max distance between two turrets to be eligible for merge")]
    [SerializeField] private float mergeRadius = 4f;

    [Tooltip("Max turret level required for merge (typically 3)")]
    [SerializeField] private int requiredLevel = 3;

    [Header("Merged Turret Prefabs")]
    [Tooltip("Logic prefab for merged turrets (same as normal turret logic)")]
    [SerializeField] private GameObject mergedTurretLogicPrefab;

    [Header("Synergy Definitions")]
    [SerializeField] private List<TurretSynergyDefinition> synergies = new List<TurretSynergyDefinition>();

    [Header("Light Aura Synergy Settings")]
    [Tooltip("Synergy DoT damage per tick when Light is merged")]
    [SerializeField] private float lightSynergyDotDamage = 5f;
    [SerializeField] private float lightSynergyEffectDuration = 3f;

    [Header("Visual Feedback")]
    [Tooltip("Effect shown briefly on merge")]
    [SerializeField] private GameObject mergeEffectPrefab;

    // ==========================================
    // MERGE CHECK (called by TurretInteract UI)
    // ==========================================

    /// <summary>
    /// Checks if a turret can merge with any nearby compatible turret.
    /// Returns the matching partner or null.
    /// </summary>
    public Turret FindMergePartner(Turret source)
    {
        if (source == null) return null;
        if (!IsMaxLevel(source)) return null;

        Collider[] nearby = Physics.OverlapSphere(source.transform.position, mergeRadius);

        foreach (var col in nearby)
        {
            // Support Turret on root OR on child (Turret_Logic pattern)
            Turret other = col.GetComponent<Turret>();
            if (other == null) other = col.GetComponentInChildren<Turret>();
            if (other == null) other = col.GetComponentInParent<Turret>();

            if (other == null || other == source) continue;
            if (!IsMaxLevel(other)) continue;

            // Check ownership (only merge own turrets)
            if (other.GetOwner()?.IsMine == false) continue;

            if (HasSynergy(source.TurretData.elementType, other.TurretData.elementType))
                return other;
        }

        return null;
    }

    /// <summary>
    /// Attempts merge. Returns true if successful.
    /// Destroys both turrets and spawns merged one in the middle.
    /// </summary>
    public bool TryMerge(Turret turretA, Turret turretB)
    {
        if (turretA == null || turretB == null) return false;
        if (!IsMaxLevel(turretA) || !IsMaxLevel(turretB)) return false;

        TurretSynergyDefinition synergy = GetSynergy(
            turretA.TurretData.elementType,
            turretB.TurretData.elementType);

        if (synergy == null)
        {
            Debug.LogWarning($"[Merge] No synergy between " +
                $"{turretA.TurretData.elementType} and {turretB.TurretData.elementType}");
            return false;
        }

        // Merge position = midpoint
        Vector3 mergePos = (turretA.transform.position + turretB.transform.position) / 2f;

        // Spawn merge effect
        if (mergeEffectPrefab != null)
            Instantiate(mergeEffectPrefab, mergePos, Quaternion.identity);

        // Get owner reference before destroy
        var ownerView = turretA.GetOwner();

        // Destroy both
        Destroy(turretA.gameObject);
        Destroy(turretB.gameObject);

        // Spawn merged turret
        SpawnMergedTurret(mergePos, synergy, ownerView);

        Debug.Log($"[Merge] SUCCESS: {synergy.synergyName} spawned at {mergePos}");
        return true;
    }

    // ==========================================
    // SPAWN MERGED TURRET
    // ==========================================

    private void SpawnMergedTurret(Vector3 position, TurretSynergyDefinition synergy,
                                    Photon.Pun.PhotonView ownerView)
    {
        GameObject prefab = synergy.mergedTurretPrefab != null
            ? synergy.mergedTurretPrefab
            : mergedTurretLogicPrefab;

        if (prefab == null)
        {
            Debug.LogError("[Merge] No merged turret prefab assigned!");
            return;
        }

        GameObject go = Instantiate(prefab, position, Quaternion.identity);

        // Initialize the Turret component
        Turret turret = go.GetComponent<Turret>();
        if (turret != null && synergy.mergedTurretData != null)
        {
            turret.Initialize(synergy.mergedTurretData, ownerView);
        }

        // If one element is Light, also add / configure LightAuraTurret
        LightAuraTurret lightAura = go.GetComponent<LightAuraTurret>();
        if (lightAura != null && synergy.lightSynergyEffect != StatusEffectType.Expose)
        {
            lightAura.Initialize(ownerView?.ViewID ?? -1);
            lightAura.SetSynergy(
                synergy.synergyElementA == ElementType.Light
                    ? synergy.synergyElementB
                    : synergy.synergyElementA,
                synergy.lightSynergyEffect,
                lightSynergyDotDamage,
                lightSynergyEffectDuration
            );
        }
    }

    // ==========================================
    // HELPERS
    // ==========================================

    private bool IsMaxLevel(Turret turret)
    {
        if (turret?.TurretData == null) return false;
        // Max level = brak dalszych upgradePaths ORAZ flaga canMerge = true
        // Zmergowane turrety (LV4) mają canMerge = false → nie są wykrywane jako partnerzy
        bool noUpgrades = turret.TurretData.upgradePaths == null ||
                          turret.TurretData.upgradePaths.Length == 0;
        return noUpgrades && turret.TurretData.canMerge;
    }

    private bool HasSynergy(ElementType a, ElementType b)
    {
        return GetSynergy(a, b) != null;
    }

    private TurretSynergyDefinition GetSynergy(ElementType a, ElementType b)
    {
        foreach (var s in synergies)
        {
            if ((s.synergyElementA == a && s.synergyElementB == b) ||
                (s.synergyElementA == b && s.synergyElementB == a))
                return s;
        }
        return null;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize merge radius around this manager's position
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.15f);
        Gizmos.DrawSphere(transform.position, mergeRadius);
    }
}

/// <summary>
/// Defines a single synergy pair — what elements combine
/// and what the resulting merged turret is.
/// </summary>
[System.Serializable]
public class TurretSynergyDefinition
{
    [Tooltip("First element in the pair")]
    public ElementType synergyElementA;

    [Tooltip("Second element in the pair")]
    public ElementType synergyElementB;

    [Tooltip("Display name, e.g. 'FrostFire'")]
    public string synergyName;

    [Tooltip("TurretData ScriptableObject for the merged turret")]
    public TurretData mergedTurretData;

    [Tooltip("Optional unique prefab for this merged turret (uses mergedTurretLogicPrefab if null)")]
    public GameObject mergedTurretPrefab;

    [Tooltip("If one element is Light — which status effect does the aura apply?")]
    public StatusEffectType lightSynergyEffect = StatusEffectType.Expose;
}
}
