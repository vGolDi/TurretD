using System;
using Photon.Pun;
using UnityEngine;
using ElementumDefense.Elements;
using ElementumDefense.StatusEffects;
using ElementumDefense.Projectiles;
using ElementumDefense.Cards;

[RequireComponent(typeof(TurretInteract))]
public class Turret : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private TurretData turretData;

    [Header("Rotating Part")]
    [SerializeField] private Transform rotatingPart;

    [Header("Projectile Settings")]
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("Behavior")]
    [SerializeField] private bool autoFire = true;

    // ========== NOWE: Bazowe i aktualne staty ==========
    // Bazowe wartości z TurretData (NIGDY nie modyfikowane)
    private float baseDamage;
    private float baseFireRate;
    private float baseRange;

    // Aktualne wartości po modyfikatorach (używane w combat)
    private float currentDamage;
    private float currentFireRate;
    private float currentRange;
    // ===================================================

    private float fireCooldown = 0f;
    private EnemyHealth currentTarget;
    private TurretInteract turretInteract;
    private PhotonView ownerPhotonView;
    private PlayerCardManager cardManager; // ← NOWE: cache reference

    public event Action OnUpgraded;

    // ========== NOWE: Public getters for UI ==========
    public float CurrentDamage => currentDamage;
    public float CurrentFireRate => currentFireRate;
    public float CurrentRange => currentRange;
    public float BaseDamage => baseDamage;
    public float BaseFireRate => baseFireRate;
    public float BaseRange => baseRange;
    public TurretData TurretData => turretData;
    // =================================================

    private void Awake()
    {
        turretInteract = GetComponent<TurretInteract>();
    }

    private void OnDestroy()
    {
        // ========== NOWE: Unsubscribe from modifier changes ==========
        if (cardManager != null)
        {
            cardManager.OnModifiersChanged -= RecalculateStats;
        }
        // =============================================================
    }

    private void Update()
    {
        if (!autoFire || turretData == null) return;

        fireCooldown -= Time.deltaTime;

        if (currentTarget == null || !IsTargetInRange(currentTarget))
        {
            currentTarget = FindNewTarget();
        }

        if (currentTarget != null)
        {
            RotateTowards(currentTarget.transform);

            if (fireCooldown <= 0f)
            {
                Shoot(currentTarget);
                // ========== NAPRAWIONE: Używaj currentFireRate ==========
                fireCooldown = 1f / currentFireRate;
                // =======================================================
            }
        }
    }

    // ==========================================
    // INITIALIZATION (NAPRAWIONE)
    // ==========================================

    public void Initialize(TurretData data, PhotonView owner)
    {
        this.turretData = data;
        this.ownerPhotonView = owner;

        // ========== NOWE: Cache BASE values from ScriptableObject ==========
        // NIGDY nie modyfikujemy turretData bezpośrednio!
        baseDamage = data.damage;
        baseFireRate = data.fireRate;
        baseRange = data.range;
        // ==================================================================

        // ========== NOWE: Find and subscribe to CardManager ==========
        if (owner != null)
        {
            cardManager = owner.GetComponent<PlayerCardManager>();

            if (cardManager != null)
            {
                // Subscribe to future modifier changes (new cards drafted)
                cardManager.OnModifiersChanged += RecalculateStats;
            }
        }
        // =============================================================

        // Calculate current stats with modifiers
        RecalculateStats();

        // Update visuals
        UpdateVisuals();

        Debug.Log($"[Turret] Initialized {data.turretName}: " +
                  $"Base DMG={baseDamage}, Current DMG={currentDamage}, " +
                  $"Base FR={baseFireRate}, Current FR={currentFireRate}, " +
                  $"Base RNG={baseRange}, Current RNG={currentRange}");
    }

    // ==========================================
    // NOWE: STAT RECALCULATION
    // ==========================================

    /// <summary>
    /// Recalculates all stats from BASE values + current card modifiers.
    /// Called on init AND whenever player drafts new cards.
    /// KEY: Always calculates from BASE, never from current!
    /// </summary>
    private void RecalculateStats()
    {
        if (turretData == null) return;

        ElementType element = turretData.elementType;

        if (cardManager != null)
        {
            // ========== KLUCZOWE: base * modifier, nie current * modifier ==========
            currentDamage = cardManager.GetModifiedDamage(baseDamage, element);
            currentFireRate = cardManager.GetModifiedFireRate(baseFireRate, element);
            currentRange = cardManager.GetModifiedRange(baseRange, element);
            // ======================================================================
        }
        else
        {
            // No card manager - use base stats
            currentDamage = baseDamage;
            currentFireRate = baseFireRate;
            currentRange = baseRange;
        }

        Debug.Log($"[Turret] {turretData.turretName} stats recalculated: " +
                  $"DMG={baseDamage}→{currentDamage:F1}, " +
                  $"FR={baseFireRate}→{currentFireRate:F2}, " +
                  $"RNG={baseRange}→{currentRange:F1}");
    }

    // ==========================================
    // SHOOTING (NAPRAWIONE - uses currentDamage)
    // ==========================================

    private void Shoot(EnemyHealth target)
    {
        if (target == null || turretData == null) return;

        int ownerID = ownerPhotonView != null ? ownerPhotonView.ViewID : -1;

        if (turretData.projectilePrefab != null)
        {
            ShootProjectile(target);
        }
        else
        {
            // ========== NAPRAWIONE: Używaj currentDamage ==========
            target.TakeDamage(
                (int)currentDamage,
                ownerID,
                turretData.elementType
            );
            // =====================================================

            TryApplyStatusEffect(target);
        }
    }

    private void ShootProjectile(EnemyHealth target)
    {
        Vector3 spawnPos;

        if (projectileSpawnPoint != null)
        {
            spawnPos = projectileSpawnPoint.position;
        }
        else if (rotatingPart != null)
        {
            spawnPos = rotatingPart.position +
                       rotatingPart.TransformDirection(turretData.projectileSpawnOffset);
        }
        else
        {
            spawnPos = transform.position +
                       transform.TransformDirection(turretData.projectileSpawnOffset);
        }

        // Prediction
        Vector3 enemyPosition = target.transform.position + Vector3.up * 0.5f;
        UnityEngine.AI.NavMeshAgent enemyAgent =
            target.GetComponent<UnityEngine.AI.NavMeshAgent>();

        Vector3 predictedPosition = enemyPosition;

        if (enemyAgent != null && enemyAgent.velocity.magnitude > 0.1f)
        {
            float distanceToEnemy = Vector3.Distance(spawnPos, enemyPosition);
            float projectileSpeed = 60f;

            if (turretData.projectileSpeedMultiplier > 0)
            {
                projectileSpeed *= turretData.projectileSpeedMultiplier;
            }

            float timeToReach = distanceToEnemy / projectileSpeed;

            float extraLead = 1.0f + (distanceToEnemy / 10f);
            extraLead = Mathf.Clamp(extraLead, 1.0f, 1.5f);
            timeToReach *= extraLead;

            predictedPosition = enemyPosition + (enemyAgent.velocity * timeToReach);
        }

        Vector3 directionToTarget = (predictedPosition - spawnPos).normalized;

        Quaternion spawnRot = directionToTarget != Vector3.zero
            ? Quaternion.LookRotation(directionToTarget)
            : transform.rotation;

        Projectile projectile = ProjectileManager.Instance.SpawnProjectile(
            turretData.projectilePrefab,
            spawnPos,
            spawnRot
        );

        if (projectile == null)
        {
            Debug.LogError($"[Turret] Failed to spawn projectile!");
            return;
        }

        ProjectileStatsManager.Instance?.RegisterShotFired();

        // ========== NAPRAWIONE: Używaj currentDamage ==========
        projectile.Initialize(
            target,
            (int)currentDamage,
            turretData.elementType,
            turretData.appliedEffect,
            turretData.effectChance,
            turretData.effectDuration,
            turretData.effectStrength,
            null
        );
        // =====================================================

        if (turretData.projectileSpeedMultiplier != 1f)
        {
            projectile.SetSpeed(projectile.speed * turretData.projectileSpeedMultiplier);
        }
    }

    // ==========================================
    // TARGET FINDING (NAPRAWIONE - uses currentRange)
    // ==========================================

    private bool IsTargetInRange(EnemyHealth target)
    {
        if (target == null) return false;
        // ========== NAPRAWIONE: Używaj currentRange ==========
        return Vector3.Distance(transform.position, target.transform.position) <= currentRange;
        // =====================================================
    }

    private EnemyHealth FindNewTarget()
    {
        // ========== NAPRAWIONE: Używaj currentRange ==========
        Collider[] hits = Physics.OverlapSphere(transform.position, currentRange);
        // =====================================================

        EnemyHealth nearestEnemy = null;
        float nearestDistance = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            EnemyHealth potential = hit.GetComponent<EnemyHealth>();
            if (potential != null)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = potential;
                }
            }
        }

        return nearestEnemy;
    }

    // ==========================================
    // ROTATION
    // ==========================================

    private void RotateTowards(Transform target)
    {
        if (rotatingPart == null || target == null) return;

        Vector3 direction = target.position - rotatingPart.position;
        direction.y = 0f;

        if (direction == Vector3.zero) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        rotatingPart.rotation = Quaternion.Slerp(
            rotatingPart.rotation,
            lookRotation,
            Time.deltaTime * 5f
        );
    }

    // ==========================================
    // UPGRADE (NAPRAWIONE)
    // ==========================================

    public void Upgrade(int pathIndex)
    {
        if (turretData.upgradePaths == null ||
           pathIndex < 0 ||
           pathIndex >= turretData.upgradePaths.Length)
        {
            Debug.LogWarning($"[Turret] Invalid upgrade path: {pathIndex}");
            return;
        }

        TurretData chosenUpgrade = turretData.upgradePaths[pathIndex];

        if (ownerPhotonView != null && ownerPhotonView.IsMine)
        {
            // ========== NOWE: Apply cost modifier ==========
            int finalCost = chosenUpgrade.upgradeCost;
            if (cardManager != null)
            {
                finalCost = cardManager.GetModifiedTurretCost(chosenUpgrade.upgradeCost);
            }
            // ===============================================

            PlayerGold playerGold = ownerPhotonView.GetComponent<PlayerGold>();
            if (playerGold != null && playerGold.SpendGold(finalCost))
            {
                turretData = chosenUpgrade;

                // ========== NOWE: Update BASE values from new TurretData ==========
                baseDamage = chosenUpgrade.damage;
                baseFireRate = chosenUpgrade.fireRate;
                baseRange = chosenUpgrade.range;
                // =================================================================

                // Recalculate with modifiers
                RecalculateStats();

                UpdateVisuals();
                OnUpgraded?.Invoke();

                Debug.Log($"[Turret] Upgraded to {turretData.turretName}: " +
                          $"DMG={baseDamage}→{currentDamage:F1}, " +
                          $"FR={baseFireRate}→{currentFireRate:F2}");
            }
            else
            {
                Debug.Log($"[Turret] Can't afford upgrade ({finalCost} gold)");
            }
        }
    }

    // ==========================================
    // VISUALS
    // ==========================================

    private void UpdateVisuals()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        if (turretData.displayPrefab != null)
        {
            GameObject display = Instantiate(
                turretData.displayPrefab,
                transform.position,
                transform.rotation,
                transform
            );

            rotatingPart = display.transform.Find("RotatingPart");
            if (rotatingPart == null)
            {
                rotatingPart = display.transform;
            }

            Transform foundSpawnPoint = display.transform.Find("ProjectileSpawn");
            if (foundSpawnPoint != null)
            {
                projectileSpawnPoint = foundSpawnPoint;
            }

            TurretUiController uiController = display.GetComponent<TurretUiController>();
            if (turretInteract != null && uiController != null)
            {
                turretInteract.LinkUiController(uiController);
            }
        }
    }

    // ==========================================
    // STATUS EFFECTS
    // ==========================================

    private void TryApplyStatusEffect(EnemyHealth target)
    {
        if (turretData.effectChance <= 0f) return;

        float roll = UnityEngine.Random.Range(0f, 100f);
        if (roll > turretData.effectChance) return;

        StatusEffectManager effectManager = target.GetComponent<StatusEffectManager>();
        if (effectManager == null) return;

        StatusEffect newEffect = CreateStatusEffect(turretData.appliedEffect);

        if (newEffect != null)
        {
            effectManager.ApplyEffect(newEffect);
        }
    }

    private StatusEffect CreateStatusEffect(StatusEffectType type)
    {
        return type switch
        {
            StatusEffectType.Burn => new BurnEffect(turretData.effectStrength, turretData.effectDuration),
            StatusEffectType.Freeze => new FreezeEffect(turretData.effectDuration),
            StatusEffectType.Slow => new SlowEffect(turretData.effectStrength, turretData.effectDuration),
            StatusEffectType.Poison => new PoisonEffect(turretData.effectStrength, turretData.effectDuration),
            _ => null
        };
    }

    // ==========================================
    // PUBLIC API
    // ==========================================

    public TurretData[] GetAvailableUpgrades()
    {
        return turretData.upgradePaths;
    }

    public PhotonView GetOwner()
    {
        return ownerPhotonView;
    }

    // ==========================================
    // DEBUG
    // ==========================================

    private void OnDrawGizmosSelected()
    {
        // ========== NAPRAWIONE: Show currentRange when playing ==========
        float drawRange = Application.isPlaying ? currentRange :
            (turretData != null ? turretData.range : 5f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, drawRange);
        // ===============================================================

        if (projectileSpawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(projectileSpawnPoint.position, 0.2f);
            Gizmos.DrawRay(projectileSpawnPoint.position, projectileSpawnPoint.forward * 2f);
        }
    }
}