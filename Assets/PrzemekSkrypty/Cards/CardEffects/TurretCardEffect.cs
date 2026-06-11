    using UnityEngine;
using Photon.Pun;
using ElementumDefense.Elements;
using ElementumDefense.Turrets;

    namespace ElementumDefense.Cards
    {
        /// <summary>
        /// Card that modifies turret stats/behavior
        /// Example: "Rapid Fire" - Fire turrets +50% attack speed, -20% damage
        /// </summary>
        [CreateAssetMenu(fileName = "TurretCardEffect", menuName = "Tower Defense/Cards/Effects/Turret Modifier")]
        public class TurretCardEffect : CardEffectBase
        {
            [Header("Target Turret")]
            [Tooltip("Which turret type to modify (None = all turrets)")]
            public ElementType targetElement = ElementType.None;

            [Tooltip("If true, affects ALL turrets; if false, only specified element")]
            public bool affectsAllTurrets = false;

            [Header("Stat Modifiers (Multiplicative)")]
            [Tooltip("Damage multiplier (1.0 = no change, 1.5 = +50%, 0.8 = -20%)")]
            public float damageMultiplier = 1f;

            [Tooltip("Fire rate multiplier (1.5 = +50% faster, 0.7 = -30% slower)")]
            public float fireRateMultiplier = 1f;

            [Tooltip("Range multiplier")]
            public float rangeMultiplier = 1f;

            [Header("Special Effects")]
            [Tooltip("Adds AOE radius (0 = no change)")]
            public float addAOERadius = 0f;

            [Tooltip("Projectile pierce count (0 = no pierce)")]
            public int addPierceCount = 0;

            [Tooltip("Chain lightning targets (0 = no chain)")]
            public int addChainTargets = 0;

            [Header("Tradeoff (Element Avatar / Mastery)")]
            [Tooltip("If targetElement is set, OTHER element families lose this % damage. " +
                     "Used by 'Element Avatar' style cards: huge boost for chosen element, " +
                     "penalty for the rest. 0 = disabled.")]
            [Range(0f, 50f)]
            public float otherElementsPenaltyPercent = 0f;

            public override void Activate(PhotonView ownerPhotonView)
            {
                // TODO: Apply modifier to TurretStatsManager (nowy system)
                // Przykład:
                // TurretStatsManager.Instance.AddModifier(ownerPhotonView, this);

                string targets = affectsAllTurrets ? "ALL turrets" : $"{targetElement} turrets";
                string mods = GetModifiersSummary();

                LogActivation(ownerPhotonView, $"{targets}: {mods}");
            }

            private string GetModifiersSummary()
            {
                string summary = "";

                if (damageMultiplier != 1f)
                    summary += $"DMG x{damageMultiplier:F2} ";

                if (fireRateMultiplier != 1f)
                    summary += $"Fire Rate x{fireRateMultiplier:F2} ";

                if (rangeMultiplier != 1f)
                    summary += $"Range x{rangeMultiplier:F2} ";

                if (addAOERadius > 0)
                    summary += $"+{addAOERadius}m AOE ";

                if (addPierceCount > 0)
                    summary += $"+{addPierceCount} pierce ";

                if (addChainTargets > 0)
                    summary += $"+{addChainTargets} chain ";

                return summary.TrimEnd();
            }

            public override string GetEffectDescription()
            {
                string target = affectsAllTurrets ? "All Turrets" : $"{targetElement} Turrets";
                string desc = $"<b>{target}</b>\n";

                if (damageMultiplier != 1f)
                {
                    float percent = (damageMultiplier - 1f) * 100f;
                    desc += $"⚔️ Damage: {percent:+0;-0}%\n";
                }

                if (fireRateMultiplier != 1f)
                {
                    float percent = (fireRateMultiplier - 1f) * 100f;
                    desc += $"⚡ Fire Rate: {percent:+0;-0}%\n";
                }

                if (rangeMultiplier != 1f)
                {
                    float percent = (rangeMultiplier - 1f) * 100f;
                    desc += $"🎯 Range: {percent:+0;-0}%\n";
                }

                if (addAOERadius > 0)
                    desc += $"💥 +{addAOERadius}m AOE radius\n";

                if (addPierceCount > 0)
                    desc += $"🎯 Projectiles pierce {addPierceCount} enemies\n";

                if (addChainTargets > 0)
                    desc += $"⚡ Chains to {addChainTargets} additional targets\n";

                return desc.TrimEnd('\n');
            }
        }
    }