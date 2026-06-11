using System;
using Photon.Pun;
using UnityEngine;
using ElementumDefense.Elements;
using ElementumDefense.Cards;
using ElementumDefense.Turrets;
using ElementumDefense.Enemies;
using ElementumDefense.Players;


namespace ElementumDefense.Turrets
{
/// <summary>
/// Turret CONTROLLER. Owns:
///  - TurretData reference and base/current stats
///  - Card modifier subscription &amp; recalculation
///  - Upgrade flow
///  - Visual prefab spawning &amp; wiring of children (rotating part, projectile spawn, UI)
///  - Light Aura buff API
/// 
/// Combat behavior is delegated to <see cref="TurretTargeting"/> and
/// <see cref="TurretShooter"/>, which are required siblings.
/// </summary>
[RequireComponent(typeof(TurretInteract))]
[RequireComponent(typeof(TurretTargeting))]
[RequireComponent(typeof(TurretShooter))]
public class Turret : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private TurretData turretData;

    [Header("Behavior")]
    [SerializeField] private bool autoFire = true;

    // ==========================================
    // STATS
    // ==========================================

    // Base values from TurretData (NEVER modified at runtime).
    private float baseDamage;
    private float baseFireRate;
    private float baseRange;

    // Current values after card + sabotage + aura modifiers.
    private float currentDamage;
    private float currentFireRate;
    private float currentRange;

    // Light Aura additive bonuses (applied on top of card modifiers).
    private float auraDamageBonus = 0f;
    private float auraRangeBonus = 0f;
    private float auraFireRateBonus = 0f;

    // Runtime
    private float fireCooldown = 0f;
    private PhotonView ownerPhotonView;
    private PlayerCardManager cardManager;

    // Cached siblings
    private TurretInteract turretInteract;
    private TurretTargeting targeting;
    private TurretShooter shooter;

    public event Action OnUpgraded;

    // ==========================================
    // PUBLIC ACCESSORS (used by UI, merge, aura)
    // ==========================================

    public float CurrentDamage => currentDamage;
    public float CurrentFireRate => currentFireRate;
    public float CurrentRange => currentRange;
    public float BaseDamage => baseDamage;
    public float BaseFireRate => baseFireRate;
    public float BaseRange => baseRange;
    public TurretData TurretData => turretData;

    public PhotonView GetOwner() => ownerPhotonView;
    public TurretData[] GetAvailableUpgrades() => turretData != null ? turretData.upgradePaths : null;

    // ==========================================
    // LIFECYCLE
    // ==========================================

    private void Awake()
    {
        turretInteract = GetComponent<TurretInteract>();
        targeting = GetComponent<TurretTargeting>();
        shooter = GetComponent<TurretShooter>();
    }

    private void OnDestroy()
    {
        if (cardManager != null)
        {
            cardManager.OnModifiersChanged -= RecalculateStats;
        }
    }

    private void Update()
    {
        if (!autoFire || turretData == null) return;

        fireCooldown -= Time.deltaTime;

        EnemyHealth target = targeting.AcquireTarget();
        if (target == null) return;

        targeting.RotateTowardsTarget();

        if (fireCooldown <= 0f)
        {
            int ownerID = ownerPhotonView != null ? ownerPhotonView.ViewID : -1;
            shooter.Shoot(target, turretData, currentDamage, ownerID);
            fireCooldown = 1f / Mathf.Max(0.01f, currentFireRate);
        }
    }

    // ==========================================
    // INITIALIZATION
    // ==========================================

    public void Initialize(TurretData data, PhotonView owner)
    {
        turretData = data;
        ownerPhotonView = owner;

        // Cache base values (never mutate the SO).
        baseDamage = data.damage;
        baseFireRate = data.fireRate;
        baseRange = data.range;

        // Subscribe to card modifier changes.
        if (owner != null)
        {
            cardManager = owner.GetComponent<PlayerCardManager>();
            if (cardManager != null)
            {
                cardManager.OnModifiersChanged += RecalculateStats;
            }
        }

        RecalculateStats();
        UpdateVisuals();

        Debug.Log($"[Turret] Initialized {data.turretName}: " +
                  $"DMG={baseDamage}->{currentDamage:F1}, " +
                  $"FR={baseFireRate}->{currentFireRate:F2}, " +
                  $"RNG={baseRange}->{currentRange:F1}");
    }

    // ==========================================
    // STAT RECALCULATION
    // ==========================================

    /// <summary>
    /// Recalculates current stats from BASE values + card modifiers + aura.
    /// Called on init, on card modifier change, on upgrade, and on aura change.
    /// KEY: always derived from BASE — never compounds onto current.
    /// </summary>
    private void RecalculateStats()
    {
        if (turretData == null) return;

        ElementType element = turretData.elementType;

        if (cardManager != null)
        {
            currentDamage = cardManager.GetModifiedDamage(baseDamage, element);
            currentFireRate = cardManager.GetModifiedFireRate(baseFireRate, element);
            currentRange = cardManager.GetModifiedRange(baseRange, element);
        }
        else
        {
            currentDamage = baseDamage;
            currentFireRate = baseFireRate;
            currentRange = baseRange;
        }

        // Apply Light Aura bonuses on top of card modifiers.
        if (auraDamageBonus > 0f) currentDamage *= (1f + auraDamageBonus);
        if (auraRangeBonus > 0f) currentRange += auraRangeBonus;
        if (auraFireRateBonus > 0f) currentFireRate *= (1f + auraFireRateBonus);

        // Push the new range to the targeting component.
        if (targeting != null) targeting.SetRange(currentRange);
    }

    // ==========================================
    // UPGRADE
    // ==========================================

    public void Upgrade(int pathIndex)
    {
        if (turretData == null) return;

        if (turretData.upgradePaths == null ||
            pathIndex < 0 ||
            pathIndex >= turretData.upgradePaths.Length)
        {
            Debug.LogWarning($"[Turret] Invalid upgrade path: {pathIndex}");
            return;
        }

        if (ownerPhotonView == null || !ownerPhotonView.IsMine) return;

        TurretData chosen = turretData.upgradePaths[pathIndex];

        int finalCost = cardManager != null
            ? cardManager.GetModifiedTurretCost(chosen.upgradeCost)
            : chosen.upgradeCost;

        PlayerGold playerGold = ownerPhotonView.GetComponent<PlayerGold>();
        if (playerGold == null || !playerGold.SpendGold(finalCost))
        {
            Debug.Log($"[Turret] Can't afford upgrade ({finalCost} gold)");
            return;
        }

        turretData = chosen;
        baseDamage = chosen.damage;
        baseFireRate = chosen.fireRate;
        baseRange = chosen.range;

        RecalculateStats();
        UpdateVisuals();

        OnUpgraded?.Invoke();
        Debug.Log($"[Turret] Upgraded to {turretData.turretName}");
    }

    // ==========================================
    // VISUALS
    // ==========================================

    /// <summary>
    /// Spawns the display prefab and wires its named children:
    ///   - "RotatingPart" -> TurretTargeting.SetRotatingPart
    ///   - "ProjectileSpawn" -> TurretShooter.SetSpawnPoint
    ///   - root TurretUiController -> TurretInteract.LinkUiController
    /// </summary>
    private void UpdateVisuals()
    {
        // Destroy previous visuals.
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        if (turretData == null || turretData.displayPrefab == null) return;

        GameObject display = Instantiate(
            turretData.displayPrefab,
            transform.position,
            transform.rotation,
            transform);

        // RotatingPart -> targeting
        Transform foundRotatingPart = display.transform.Find("RotatingPart");
        Transform rotatingPart = foundRotatingPart != null ? foundRotatingPart : display.transform;
        targeting?.SetRotatingPart(rotatingPart);

        // ProjectileSpawn -> shooter (optional)
        Transform foundSpawnPoint = display.transform.Find("ProjectileSpawn");
        if (foundSpawnPoint != null)
        {
            shooter?.SetSpawnPoint(foundSpawnPoint);
        }

        // UI controller -> interact
        TurretUiController uiController = display.GetComponent<TurretUiController>();
        if (turretInteract != null && uiController != null)
        {
            turretInteract.LinkUiController(uiController);
        }
    }

    // ==========================================
    // LIGHT AURA BUFF API
    // ==========================================

    /// <summary>Apply additive bonus from a nearby Light Aura Turret.</summary>
    public void AddAuraBuff(float damageBonus, float rangeBonus, float fireRateBonus)
    {
        auraDamageBonus = Mathf.Max(0f, auraDamageBonus + damageBonus);
        auraRangeBonus = Mathf.Max(0f, auraRangeBonus + rangeBonus);
        auraFireRateBonus = Mathf.Max(0f, auraFireRateBonus + fireRateBonus);
        RecalculateStats();
    }

    /// <summary>Remove bonus when the Light Aura Turret leaves range or is destroyed.</summary>
    public void RemoveAuraBuff(float damageBonus, float rangeBonus, float fireRateBonus)
    {
        auraDamageBonus = Mathf.Max(0f, auraDamageBonus - damageBonus);
        auraRangeBonus = Mathf.Max(0f, auraRangeBonus - rangeBonus);
        auraFireRateBonus = Mathf.Max(0f, auraFireRateBonus - fireRateBonus);
        RecalculateStats();
    }
}
}
