using UnityEngine;
using ElementumDefense.Elements;
using ElementumDefense.StatusEffects;

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
    [Header("Element")]
    [Tooltip("Elemental type of this turret")]
    public ElementType elementType = ElementType.None;

    // TODO: Add special effects
    [Header("Status Effects")]
    [Tooltip("Which status effect does this turret apply?")]
    public StatusEffectType appliedEffect = StatusEffectType.Burn;

    [Tooltip("Chance to apply effect (0-100%)")]
    [Range(0f, 100f)]
    public float effectChance = 30f;

    [Tooltip("Effect duration in seconds")]
    public float effectDuration = 3f;

    [Tooltip("Effect strength (DPS for DOT, slow % for Slow)")]
    public float effectStrength = 5f;

    [Header("Projectile")]
    [Tooltip("Projectile prefab to spawn when shooting (leave empty for instant damage)")]
    public GameObject projectilePrefab;

    [Tooltip("Projectile spawn offset from turret (local space)")]
    public Vector3 projectileSpawnOffset = new Vector3(0f, 1f, 0f);

    [Tooltip("Projectile speed multiplier")]
    public float projectileSpeedMultiplier = 1f;
    // TODO: Add synergy bonuses
    // public SynergyData[] synergyWith;
}

