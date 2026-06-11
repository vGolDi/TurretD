using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Card that grants combat-side modifiers: turret crit chance / multiplier,
    /// bonus gold per kill, boss kill gold multiplier.
    /// 
    /// Read at runtime by:
    ///  - <see cref="PlayerModifierStack"/> (aggregates crit chance / gold per kill)
    ///  - <see cref="ElementumDefense.Turrets.TurretShooter"/> (rolls crit per shot)
    ///  - <see cref="ElementumDefense.Enemies.EnemyHealth"/> (awards bonus gold on kill)
    /// </summary>
    [CreateAssetMenu(fileName = "CombatCard_Effect", menuName = "Tower Defense/Cards/Effects/Combat Modifier")]
    public class CombatModifierEffect : CardEffectBase
    {
        [Header("Critical Hits (Turret crit on shoot)")]
        [Tooltip("Added to total crit chance. 0.05 = +5%. Capped at 100% across all cards.")]
        [Range(0f, 1f)]
        public float critChanceAdd = 0f;

        [Tooltip("Crit damage multiplier — highest among active cards wins. 2 = ×2 dmg on crit.")]
        public float critMultiplierOverride = 0f;

        [Header("Gold Per Kill")]
        [Tooltip("Flat bonus gold per enemy killed. Stacks across cards.")]
        public int bonusGoldPerKill = 0;

        [Tooltip("Boss-only gold multiplier (1 = no change, 1.5 = +50% gold from bosses). Highest wins.")]
        public float bossKillGoldMultiplier = 1f;

        [Header("Tradeoff (per-card cost)")]
        [Tooltip("Global damage penalty % paid for the crit/gold buff.")]
        [Range(0f, 100f)]
        public float globalDamagePenaltyPercent = 0f;

        [Tooltip("Global fire rate penalty %.")]
        [Range(0f, 100f)]
        public float globalFireRatePenaltyPercent = 0f;

        public override void Activate(PhotonView ownerPhotonView)
        {
            // Effect is read by PlayerModifierStack.RecalculateFromCards.
            // Just log for debug.
            string s = "";
            if (critChanceAdd > 0f) s += $"+{critChanceAdd * 100f:0}% crit ";
            if (critMultiplierOverride > 0f) s += $"crit×{critMultiplierOverride:F1} ";
            if (bonusGoldPerKill > 0) s += $"+{bonusGoldPerKill}g/kill ";
            if (bossKillGoldMultiplier > 1f) s += $"boss×{bossKillGoldMultiplier:F2} ";
            LogActivation(ownerPhotonView, s);
        }

        public override string GetEffectDescription()
        {
            string desc = "";
            if (critChanceAdd > 0f)
                desc += $"🎯 +{critChanceAdd * 100f:0}% crit chance\n";
            if (critMultiplierOverride > 0f)
                desc += $"💥 Crit ×{critMultiplierOverride:F1} damage\n";
            if (bonusGoldPerKill > 0)
                desc += $"💰 +{bonusGoldPerKill} gold per kill\n";
            if (bossKillGoldMultiplier > 1f)
                desc += $"👑 +{(bossKillGoldMultiplier - 1f) * 100f:0}% gold from bosses\n";
            if (globalDamagePenaltyPercent > 0f)
                desc += $"⚠️ -{globalDamagePenaltyPercent:0}% global damage\n";
            if (globalFireRatePenaltyPercent > 0f)
                desc += $"⚠️ -{globalFireRatePenaltyPercent:0}% global fire rate\n";
            return desc.TrimEnd('\n');
        }
    }
}
