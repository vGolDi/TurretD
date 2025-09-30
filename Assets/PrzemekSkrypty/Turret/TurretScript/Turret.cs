using System;
using UnityEngine;

[RequireComponent(typeof(TurretInteract))]
public class Turret : MonoBehaviour
{
    [Header("Konfiguracja z ScriptableObject")]
    [Tooltip("Dane tej instancji wie¿yczki")]
    [SerializeField] private TurretData turretData;

    [Header("Czêœæ obiektowa do obracania (np. g³owa wie¿yczki)")]
    [Tooltip("Element wie¿y, który ma siê obracaæ za celem")]
    [SerializeField] private Transform rotatingPart;

    [Header("Tryb dzia³ania")]
    [SerializeField] private bool autoFire = true;

    private float fireCooldown = 0f;
    private EnemyHealth currentTarget;

    private TurretInteract turretInteract;
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

    private void Shoot(EnemyHealth target)
    {
        if (target == null) return;
        //target.TakeDamage((int)turretData.damage);
        target.TakeDamage((int)turretData.damage, true);
        Debug.Log($"[Turret] Zada³em {turretData.damage} dmg przeciwnikowi {target.name}");
    }

    private bool IsTargetInRange(EnemyHealth target)
    {
        if (target == null) return false;
        return Vector3.Distance(transform.position, target.transform.position) <= turretData.range;
    }

    private EnemyHealth FindNewTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, turretData.range);
        foreach (Collider hit in hits)
        {
            EnemyHealth potential = hit.GetComponent<EnemyHealth>();
            if (potential != null) return potential;
        }
        return null;
    }

    private void RotateTowards(Transform target)
    {
        if (rotatingPart == null) return;

        Vector3 direction = target.position - rotatingPart.position;
        direction.y = 0f; 
        if (direction == Vector3.zero) return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        rotatingPart.rotation = Quaternion.Slerp(rotatingPart.rotation, lookRotation, Time.deltaTime * 5f);
    }
    public void Initialize(TurretData data)
    {
        this.turretData = data;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // 1. Zniszcz stary model, jeœli istnieje
        if (transform.childCount > 0)
        {
            foreach (Transform child in transform) { Destroy(child.gameObject); }
        }

        // 2. Stwórz nowy model wizualny
        if (turretData.displayPrefab != null)
        {
            GameObject display = Instantiate(turretData.displayPrefab, transform.position, transform.rotation, transform);

            // Logika znajdowania czêœci obrotowej
            rotatingPart = display.transform.Find("RotatingPart") ?? display.transform;

            // 3. ZnajdŸ kontroler UI i przeka¿ go do skryptu interakcji
            TurretUiController uiController = display.GetComponent<TurretUiController>();

            if (turretInteract != null && uiController != null)
            {
                // To jest kluczowy moment - Turret "mówi" TurretInteract, z którym UI ma pracowaæ
                turretInteract.LinkUiController(uiController);


            }
        }
    }

    public void Upgrade(int pathIndex)
    {
        if (turretData.upgradePaths == null || pathIndex < 0 || pathIndex >= turretData.upgradePaths.Length) return;

        TurretData chosenUpgrade = turretData.upgradePaths[pathIndex];

        if (PlayerGold.Instance.SpendGold(chosenUpgrade.upgradeCost))
        {
            turretData = chosenUpgrade;
            UpdateVisuals();
            OnUpgraded?.Invoke(); // Og³oœ ulepszenie
            Debug.Log($"Ulepszono do: {turretData.turretName}");
        }
    }

    // ADD this new public method so other scripts can see the available upgrades
    public TurretData[] GetAvailableUpgrades()
    {
        return turretData.upgradePaths;
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
