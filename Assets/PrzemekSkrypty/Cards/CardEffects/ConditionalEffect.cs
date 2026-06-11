using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Card that grants a damage bonus only when a runtime predicate is true.
    /// Predicate is one of <see cref="PlayerModifierStack.ConditionalContext"/>:
    ///   VsBoss / VsNormal / LowPlayerHp / WaveOpening / WaveClosing / UnderdogGold
    /// 
    /// Damage queries that want to honor conditional bonuses call
    /// <see cref="PlayerModifierStack.GetConditionalDamageBonus"/> at the
    /// damage site (e.g. TurretShooter). Cards without a callable hook still
    /// register their entry — callers that don't ask for bonus simply ignore it.
    /// </summary>
    [CreateAssetMenu(fileName = "ConditionalCard_Effect", menuName = "Tower Defense/Cards/Effects/Conditional Bonus")]
    public class ConditionalEffect : CardEffectBase
    {
        [Header("Predicate")]
        [Tooltip("When does the bonus damage apply?")]
        public PlayerModifierStack.ConditionalContext context = PlayerModifierStack.ConditionalContext.VsBoss;

        [Tooltip("Threshold value, meaning depends on context:\n" +
                 " - LowPlayerHp: HP percent (0..1, e.g. 0.3 = below 30%)\n" +
                 " - WaveOpening: seconds since wave start (e.g. 10)\n" +
                 " - WaveClosing: trailing seconds at end of wave (e.g. 10)\n" +
                 " - other contexts: ignored")]
        public float thresholdValue = 0.3f;

        [Header("Bonus")]
        [Tooltip("Bonus damage % when predicate is true. 30 = +30%.")]
        [Range(0f, 200f)]
        public float bonusDamagePercent = 30f;

        [Header("Tradeoff (Boss Slayer / Apex Predator)")]
        [Tooltip("ONLY for VsBoss cards: penalty applied when target is NOT a boss. " +
                 "Adds an extra VsNormal entry with negative bonus.")]
        [Range(0f, 100f)]
        public float normalEnemyPenaltyPercent = 0f;

        public override void Activate(PhotonView ownerPhotonView)
        {
            LogActivation(ownerPhotonView,
                $"{context} -> +{bonusDamagePercent}% dmg (penalty {normalEnemyPenaltyPercent}%)");
        }

        public override string GetEffectDescription()
        {
            string label = context switch
            {
                PlayerModifierStack.ConditionalContext.VsBoss => "vs Boss",
                PlayerModifierStack.ConditionalContext.VsNormal => "vs Normal enemies",
                PlayerModifierStack.ConditionalContext.LowPlayerHp => $"below {thresholdValue * 100:0}% HP",
                PlayerModifierStack.ConditionalContext.WaveOpening => $"first {thresholdValue:0}s of wave",
                PlayerModifierStack.ConditionalContext.WaveClosing => $"last {thresholdValue:0}s of wave",
                PlayerModifierStack.ConditionalContext.UnderdogGold => "while behind in gold",
                _ => "?"
            };

            string desc = $"⚔️ +{bonusDamagePercent:0}% damage {label}\n";
            if (normalEnemyPenaltyPercent > 0f)
                desc += $"⚠️ -{normalEnemyPenaltyPercent:0}% damage vs non-boss enemies\n";
            return desc.TrimEnd('\n');
        }
    }
}
