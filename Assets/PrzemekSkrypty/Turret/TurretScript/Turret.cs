//using System;
//using Photon.Pun;
//using UnityEngine;
//using ElementumDefense.StatusEffects;

//[RequireComponent(typeof(TurretInteract))]
//public class Turret : MonoBehaviour
//{
//    [Header("Configuration")]
//    [SerializeField, Tooltip("Data for this turret instance")]
//    private TurretData turretData;

//    [Header("Rotating Part")]
//    [SerializeField, Tooltip("Transform that rotates towards target (e.g., turret head)")]
//    private Transform rotatingPart;

//    [Header("Behavior")]
//    [SerializeField] private bool autoFire = true;

//    // Runtime variables
//    private float fireCooldown = 0f;
//    private EnemyHealth currentTarget;
//    private TurretInteract turretInteract;
//    private PhotonView ownerPhotonView; // Player who built this turret

//    // Events
//    public event Action OnUpgraded;

//    // TODO: Add elemental type
//    // public ElementType elementType;

//    private void Awake()
//    {
//        turretInteract = GetComponent<TurretInteract>();
//    }
//    private void Update()
//    {
//        if (!autoFire || turretData == null) return;

//        fireCooldown -= Time.deltaTime;

//        if (currentTarget == null || !IsTargetInRange(currentTarget))
//        {
//            currentTarget = FindNewTarget();
//        }

//        if (currentTarget != null)
//        {
//            RotateTowards(currentTarget.transform);

//            if (fireCooldown <= 0f)
//            {
//                Shoot(currentTarget);
//                fireCooldown = 1f / turretData.fireRate;
//            }
//        }

//    }

//    private void Shoot(EnemyHealth target)
//    {
//        if (target == null || turretData == null) return;

//        int ownerID = ownerPhotonView != null ? ownerPhotonView.ViewID : -1;

//        target.TakeDamage(
//            (int)turretData.damage,
//            ownerID,
//            turretData.elementType 
//        );
//        TryApplyStatusEffect(target);
//        Debug.Log($"[Turret] {turretData.turretName} ({turretData.elementType}) dealt damage to {target.name}");
//    }
//    /// <summary>
//    /// Attempts to apply status effect based on turret configuration
//    /// </summary>
//    private void TryApplyStatusEffect(EnemyHealth target)
//    {
//        // Check if turret applies effects
//        if (turretData.effectChance <= 0f) return;

//        // Roll chance
//        float roll = UnityEngine.Random.Range(0f, 100f);
//        if (roll > turretData.effectChance) return;

//        // Get StatusEffectManager from enemy
//        StatusEffectManager effectManager = target.GetComponent<StatusEffectManager>();
//        if (effectManager == null)
//        {
//            Debug.LogWarning($"[Turret] Enemy {target.name} has no StatusEffectManager!");
//            return;
//        }

//        // Create appropriate effect based on type
//        StatusEffect newEffect = CreateStatusEffect(turretData.appliedEffect);

//        if (newEffect != null)
//        {
//            effectManager.ApplyEffect(newEffect);
//            Debug.Log($"[Turret] Applied {newEffect.Icon} {newEffect.DisplayName} to {target.name}");
//        }
//    }
//    /// <summary>
///// Factory method - creates status effect instance
///// </summary>
//private StatusEffect CreateStatusEffect(StatusEffectType type)
//{
//    return type switch
//    {
//        StatusEffectType.Burn => new BurnEffect(turretData.effectStrength, turretData.effectDuration),
//        StatusEffectType.Freeze => new FreezeEffect(turretData.effectDuration),
//        StatusEffectType.Slow => new SlowEffect(turretData.effectStrength, turretData.effectDuration),
//        StatusEffectType.Poison => new PoisonEffect(turretData.effectStrength, turretData.effectDuration),
//        _ => null
//    };
//}
//    private bool IsTargetInRange(EnemyHealth target)
//    {
//        if (target == null) return false;
//        return Vector3.Distance(transform.position, target.transform.position) <= turretData.range;
//    }

//    private EnemyHealth FindNewTarget()
//    {
//        Collider[] hits = Physics.OverlapSphere(transform.position, turretData.range);

//        EnemyHealth nearestEnemy = null;
//        float nearestDistance = Mathf.Infinity;

//        foreach (Collider hit in hits)
//        {
//            EnemyHealth potential = hit.GetComponent<EnemyHealth>();
//            if (potential != null)
//            {
//                float distance = Vector3.Distance(transform.position, hit.transform.position);
//                if (distance < nearestDistance)
//                {
//                    nearestDistance = distance;
//                    nearestEnemy = potential;
//                }
//            }
//        }

//        return nearestEnemy;
//    }

//    private void RotateTowards(Transform target)
//    {
//        if (rotatingPart == null || target == null) return;

//        Vector3 direction = target.position - rotatingPart.position;
//        direction.y = 0f; // Keep rotation on horizontal plane only

//        if (direction == Vector3.zero) return;

//        Quaternion lookRotation = Quaternion.LookRotation(direction);
//        rotatingPart.rotation = Quaternion.Slerp(
//            rotatingPart.rotation,
//            lookRotation,
//            Time.deltaTime * 5f
//        );
//    }
//    public void Initialize(TurretData data, PhotonView owner)
//    {
//        this.turretData = data;
//        this.ownerPhotonView = owner;
//        UpdateVisuals();
//    }

//    private void UpdateVisuals()
//    {
//        // 1. Destroy old model
//        foreach (Transform child in transform)
//        {
//            Destroy(child.gameObject);
//        }

//        // 2. Instantiate new visual prefab
//        if (turretData.displayPrefab != null)
//        {
//            GameObject display = Instantiate(
//                turretData.displayPrefab,
//                transform.position,
//                transform.rotation,
//                transform
//            );

//            // Find rotating part (convention: child named "RotatingPart")
//            rotatingPart = display.transform.Find("RotatingPart");
//            if (rotatingPart == null)
//            {
//                rotatingPart = display.transform; // Fallback to root
//            }

//            // 3. Link UI controller to TurretInteract
//            TurretUiController uiController = display.GetComponent<TurretUiController>();
//            if (turretInteract != null && uiController != null)
//            {
//                turretInteract.LinkUiController(uiController);
//            }
//        }
//    }

//    public void Upgrade(int pathIndex)
//    {
//        if (turretData.upgradePaths == null ||
//           pathIndex < 0 ||
//           pathIndex >= turretData.upgradePaths.Length)
//        {
//            Debug.LogWarning($"[Turret] Invalid upgrade path index: {pathIndex}");
//            return;
//        }

//        TurretData chosenUpgrade = turretData.upgradePaths[pathIndex];

//        // Check if owner can afford it
//        if (ownerPhotonView != null && ownerPhotonView.IsMine)
//        {
//            PlayerGold playerGold = ownerPhotonView.GetComponent<PlayerGold>();
//            if (playerGold != null && playerGold.SpendGold(chosenUpgrade.upgradeCost))
//            {
//                turretData = chosenUpgrade;
//                UpdateVisuals();
//                OnUpgraded?.Invoke();

//                Debug.Log($"[Turret] Upgraded to: {turretData.turretName}");
//            }
//            else
//            {
//                Debug.Log($"[Turret] Cannot afford upgrade ({chosenUpgrade.upgradeCost} gold)");
//            }
//        }
//    }

//    // ADD this new public method so other scripts can see the available upgrades
//    public TurretData[] GetAvailableUpgrades()
//    {
//        return turretData.upgradePaths;
//    }
//    public PhotonView GetOwner()
//    {
//        return ownerPhotonView;
//    }
//    private void OnDrawGizmosSelected()
//    {
//        if (turretData != null)
//        {
//            Gizmos.color = Color.red;
//            Gizmos.DrawWireSphere(transform.position, turretData.range);
//        }
//    }
//}
using System;
using Photon.Pun;
using UnityEngine;
using ElementumDefense.Elements;
using ElementumDefense.StatusEffects;
using ElementumDefense.Projectiles; // NOWE!

[RequireComponent(typeof(TurretInteract))]
public class Turret : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField, Tooltip("Data for this turret instance")]
    private TurretData turretData;

    [Header("Rotating Part")]
    [SerializeField, Tooltip("Transform that rotates towards target (e.g., turret head)")]
    private Transform rotatingPart;

    // ========== NOWE: Projectile Spawn Point ==========
    [Header("Projectile Settings")]
    [SerializeField, Tooltip("Where projectiles spawn (optional - uses turret position if null)")]
    private Transform projectileSpawnPoint;
    // ===================================================

    [Header("Behavior")]
    [SerializeField] private bool autoFire = true;

    private float fireCooldown = 0f;
    private EnemyHealth currentTarget;
    private TurretInteract turretInteract;
    private PhotonView ownerPhotonView;

    public event Action OnUpgraded;

    private void Awake()
    {
        turretInteract = GetComponent<TurretInteract>();
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
                fireCooldown = 1f / turretData.fireRate;
            }
        }
    }

    // ========== ZMODYFIKOWANA FUNKCJA SHOOT ==========
    private void Shoot(EnemyHealth target)
    {
        if (target == null || turretData == null) return;

        int ownerID = ownerPhotonView != null ? ownerPhotonView.ViewID : -1;

        // Check if turret uses projectiles
        if (turretData.projectilePrefab != null)
        {
            // PROJECTILE MODE - Spawn projectile
            ShootProjectile(target);
        }
        else
        {
            // INSTANT MODE - Old behavior (backwards compatible)
            target.TakeDamage(
                (int)turretData.damage,
                ownerID,
                turretData.elementType
            );

            TryApplyStatusEffect(target);
        }

        Debug.Log($"[Turret] {turretData.turretName} ({turretData.elementType}) attacked {target.name}");
    }

    // ========== NOWA FUNKCJA: Spawn Projectile ==========
    /// <summary>
    /// Spawns and initializes projectile
    /// </summary>
    private void ShootProjectile(EnemyHealth target)
    {
        // Determine spawn position
        Vector3 spawnPos;

        if (projectileSpawnPoint != null)
        {
            spawnPos = projectileSpawnPoint.position;
        }
        else if (rotatingPart != null)
        {
            spawnPos = rotatingPart.position + rotatingPart.TransformDirection(turretData.projectileSpawnOffset);
        }
        else
        {
            spawnPos = transform.position + transform.TransformDirection(turretData.projectileSpawnOffset);
        }

        // ========== NOWE: Prediction/Lead Targeting ==========
        // Get enemy's current velocity
        Vector3 enemyPosition = target.transform.position + Vector3.up * 0.5f;

        // Try to get NavMeshAgent for velocity prediction
        UnityEngine.AI.NavMeshAgent enemyAgent = target.GetComponent<UnityEngine.AI.NavMeshAgent>();

        Vector3 predictedPosition = enemyPosition;

        if (enemyAgent != null && enemyAgent.velocity.magnitude > 0.1f)
        {
            // Calculate projectile travel time
            float distanceToEnemy = Vector3.Distance(spawnPos, enemyPosition);
            float projectileSpeed = 15f; // Default speed - should match your projectile

            // Apply speed multiplier from TurretData
            if (turretData.projectileSpeedMultiplier > 0)
            {
                projectileSpeed *= turretData.projectileSpeedMultiplier;
            }

            float timeToReach = distanceToEnemy / projectileSpeed;

            // Predict where enemy will be
            predictedPosition = enemyPosition + (enemyAgent.velocity * timeToReach);

            Debug.Log($"[Turret] Prediction: Enemy moving at {enemyAgent.velocity.magnitude:F2} m/s, leading by {timeToReach:F2}s");
        }

        Vector3 directionToTarget = (predictedPosition - spawnPos).normalized;
        // ====================================================

        // Set rotation to face predicted target
        Quaternion spawnRot;
        if (directionToTarget != Vector3.zero)
        {
            spawnRot = Quaternion.LookRotation(directionToTarget);
        }
        else
        {
            spawnRot = transform.rotation;
        }

        // Spawn projectile from pool
        Projectile projectile = ProjectileManager.Instance.SpawnProjectile(
            turretData.projectilePrefab,
            spawnPos,
            spawnRot
        );

        if (projectile == null)
        {
            Debug.LogError($"[Turret] Failed to spawn projectile for {turretData.turretName}!");
            return;
        }

        // Initialize projectile with combat data
        projectile.Initialize(
            target,
            (int)turretData.damage,
            turretData.elementType,
            turretData.appliedEffect,
            turretData.effectChance,
            turretData.effectDuration,
            turretData.effectStrength,
            null
        );

        // Apply speed multiplier
        if (turretData.projectileSpeedMultiplier != 1f)
        {
            projectile.SetSpeed(projectile.speed * turretData.projectileSpeedMultiplier);
        }

        Debug.Log($"[Turret] Fired at {target.name} - Current: {enemyPosition}, Predicted: {predictedPosition}");
    }
    // ====================================================

    private bool IsTargetInRange(EnemyHealth target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.transform.position) <= turretData.range;
    }

    private EnemyHealth FindNewTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, turretData.range);

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

    public void Initialize(TurretData data, PhotonView owner)
    {
        this.turretData = data;
        this.ownerPhotonView = owner;
        UpdateVisuals();
    }

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

            // ========== NOWE: Auto-find projectile spawn point ==========
            Transform foundSpawnPoint = display.transform.Find("ProjectileSpawn");
            if (foundSpawnPoint != null)
            {
                projectileSpawnPoint = foundSpawnPoint;
                Debug.Log($"[Turret] Found ProjectileSpawn point in prefab");
            }
            // ============================================================

            TurretUiController uiController = display.GetComponent<TurretUiController>();
            if (turretInteract != null && uiController != null)
            {
                turretInteract.LinkUiController(uiController);
            }
        }
    }

    public void Upgrade(int pathIndex)
    {
        if (turretData.upgradePaths == null ||
           pathIndex < 0 ||
           pathIndex >= turretData.upgradePaths.Length)
        {
            Debug.LogWarning($"[Turret] Invalid upgrade path index: {pathIndex}");
            return;
        }

        TurretData chosenUpgrade = turretData.upgradePaths[pathIndex];

        if (ownerPhotonView != null && ownerPhotonView.IsMine)
        {
            PlayerGold playerGold = ownerPhotonView.GetComponent<PlayerGold>();
            if (playerGold != null && playerGold.SpendGold(chosenUpgrade.upgradeCost))
            {
                turretData = chosenUpgrade;
                UpdateVisuals();
                OnUpgraded?.Invoke();

                Debug.Log($"[Turret] Upgraded to: {turretData.turretName}");
            }
            else
            {
                Debug.Log($"[Turret] Cannot afford upgrade ({chosenUpgrade.upgradeCost} gold)");
            }
        }
    }

    // ========== BACKWARD COMPATIBILITY: Keep old status effect method ==========
    private void TryApplyStatusEffect(EnemyHealth target)
    {
        if (turretData.effectChance <= 0f) return;

        float roll = UnityEngine.Random.Range(0f, 100f);
        if (roll > turretData.effectChance) return;

        StatusEffectManager effectManager = target.GetComponent<StatusEffectManager>();
        if (effectManager == null)
        {
            Debug.LogWarning($"[Turret] Enemy {target.name} has no StatusEffectManager!");
            return;
        }

        StatusEffect newEffect = CreateStatusEffect(turretData.appliedEffect);

        if (newEffect != null)
        {
            effectManager.ApplyEffect(newEffect);
            Debug.Log($"[Turret] Applied {newEffect.Icon} {newEffect.DisplayName} to {target.name}");
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
    // ============================================================================

    public TurretData[] GetAvailableUpgrades()
    {
        return turretData.upgradePaths;
    }

    public PhotonView GetOwner()
    {
        return ownerPhotonView;
    }

    private void OnDrawGizmosSelected()
    {
        if (turretData != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, turretData.range);
        }

        // ========== NOWE: Visualize projectile spawn ==========
        if (projectileSpawnPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(projectileSpawnPoint.position, 0.2f);
            Gizmos.DrawRay(projectileSpawnPoint.position, projectileSpawnPoint.forward * 2f);
        }
        // ======================================================
    }
}