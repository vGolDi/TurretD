using System;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(TurretInteract))]
public class Turret : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField, Tooltip("Data for this turret instance")]
    private TurretData turretData;

    [Header("Rotating Part")]
    [SerializeField, Tooltip("Transform that rotates towards target (e.g., turret head)")]
    private Transform rotatingPart;

    [Header("Behavior")]
    [SerializeField] private bool autoFire = true;

    // Runtime variables
    private float fireCooldown = 0f;
    private EnemyHealth currentTarget;
    private TurretInteract turretInteract;
    private PhotonView ownerPhotonView; // Player who built this turret

    // Events
    public event Action OnUpgraded;

    // TODO: Add elemental type
    // public ElementType elementType;

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

    private void Shoot(EnemyHealth target)
    {
        if (target == null) return;
        //target.TakeDamage((int)turretData.damage);
        int ownerID = ownerPhotonView != null ? ownerPhotonView.ViewID : -1;
        target.TakeDamage((int)turretData.damage, ownerID);

        // TODO: Spawn projectile instead of instant damage
        // TODO: Apply elemental effects (burn, freeze, etc.)

        Debug.Log($"[Turret] {turretData.turretName} dealt {turretData.damage} damage to {target.name}");
    }

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
        direction.y = 0f; // Keep rotation on horizontal plane only

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
        // 1. Destroy old model
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // 2. Instantiate new visual prefab
        if (turretData.displayPrefab != null)
        {
            GameObject display = Instantiate(
                turretData.displayPrefab,
                transform.position,
                transform.rotation,
                transform
            );

            // Find rotating part (convention: child named "RotatingPart")
            rotatingPart = display.transform.Find("RotatingPart");
            if (rotatingPart == null)
            {
                rotatingPart = display.transform; // Fallback to root
            }

            // 3. Link UI controller to TurretInteract
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

        // Check if owner can afford it
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

    // ADD this new public method so other scripts can see the available upgrades
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
    }
}
