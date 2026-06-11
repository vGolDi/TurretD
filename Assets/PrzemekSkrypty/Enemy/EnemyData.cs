using UnityEngine;
using ElementumDefense.Elements;

namespace ElementumDefense.Enemies
{
    /// <summary>
    /// Centralized stat block for an enemy archetype. Optional — if assigned
    /// to <see cref="EnemyHealth"/> and/or <see cref="EnemyMovement"/>, the
    /// component reads values from this asset in Awake instead of from its
    /// own inspector fields.
    /// 
    /// Why optional? Existing prefabs already have HP/speed/gold tuned in their
    /// component inspectors. Forcing EnemyData on day 1 would break every
    /// prefab variant. With this design you can migrate one enemy at a time:
    /// create a SO, drag it onto the prefab, test, move to the next.
    /// 
    /// What's NOT here: trait flags (Split / Revive / Armor), VFX prefabs,
    /// healthbar refs. Those stay on individual components because:
    ///  - Each prefab variant typically wants different VFX / scaling.
    ///  - Splits and revives need their own prefab references.
    ///  - Healthbar is a child UI element, not a stat.
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy", menuName = "Tower Defense/Enemy/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Display name (UI / debug logs)")]
        public string enemyName = "Enemy";

        [Tooltip("Optional icon for UI / quest entries")]
        public Sprite icon;

        [Header("Combat Stats")]
        [Tooltip("Starting HP. Wave HP multiplier and SetMaxHP overrides apply on top.")]
        [Min(1)]
        public int maxHP = 100;

        [Tooltip("Gold awarded to whoever lands the killing blow")]
        [Min(0)]
        public int goldReward = 10;

        [Tooltip("Damage dealt to player when this enemy reaches the end of the path")]
        [Min(0)]
        public int damageToPlayer = 10;

        [Tooltip("Elemental type — affects damage taken (see ElementUtility)")]
        public ElementType elementType = ElementType.None;

        [Tooltip("Marks this archetype as a boss for cards (e.g. Boss Slayer, Tax Collector). " +
                 "Bosses count separately for damage/gold conditional bonuses.")]
        public bool isBoss = false;

        [Header("Movement")]
        [Tooltip("Base movement speed before status modifiers")]
        [Min(0.1f)]
        public float baseSpeed = 3.5f;

        [Tooltip("How close to a waypoint the agent must get before advancing")]
        [Min(0.05f)]
        public float waypointReachDistance = 0.2f;

        [Header("NavMesh Agent Tuning")]
        [Tooltip("Agent collision radius — smaller = denser packing without push")]
        [Range(0.05f, 2f)]
        public float agentRadius = 0.25f;

        [Tooltip("Vertical offset from navmesh to model pivot. Match to model height / 2.")]
        public float agentBaseOffset = 1f;

        [Tooltip("How fast the agent reaches its target speed")]
        [Min(0f)]
        public float agentAcceleration = 12f;

        [Tooltip("Max degrees per second the agent rotates")]
        [Min(0f)]
        public float agentAngularSpeed = 180f;
    }
}
