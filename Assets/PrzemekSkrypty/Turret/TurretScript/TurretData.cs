using UnityEngine;

[CreateAssetMenu(fileName = "New Turret", menuName = "Tower Defense/Turret")]
public class TurretData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Display name of this turret")]
    public string turretName;

    [TextArea(3, 5)]
    [Tooltip("Description of turret's functionality")]
    public string description;

    [Header("Visual")]
    [Tooltip("Visual prefab for THIS turret level (not the logic GameObject)")]
    public GameObject displayPrefab;

    [Header("Combat Stats")]
    [Tooltip("Damage dealt per shot")]
    public float damage = 10f;

    [Tooltip("Shots per second")]
    public float fireRate = 1f;

    [Tooltip("Attack range in units")]
    public float range = 5f;

    [Header("Economy")]
    [Tooltip("Cost to build this turret from scratch")]
    public int cost = 50;

    [Tooltip("Cost to upgrade FROM previous level TO this one")]
    public int upgradeCost = 75;

    [Header("Upgrade Paths")]
    [Tooltip("Available upgrade options (typically 2-3 paths)")]
    public TurretData[] upgradePaths;

    // TODO: Add elemental type
    // public ElementType elementType;

    // TODO: Add special effects
    // public TurretEffect[] specialEffects; // (slow, burn, freeze, etc.)

    // TODO: Add synergy bonuses
    // public SynergyData[] synergyWith;
}

