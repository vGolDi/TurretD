// Assets/PrzemekSkrypty/BattlePass/BattlePassTierData.cs
using UnityEngine;

namespace ElementumDefense.BattlePass
{
    /// <summary>
    /// Defines a single tier (level) in the Battle Pass.
    /// Each tier has a free reward and optionally a premium reward.
    /// </summary>
    [System.Serializable]
    public class BattlePassTierData
    {
        [Header("Tier Info")]
        [Tooltip("Tier number (1-based). Auto-set from list index if 0.")]
        public int tierNumber;

        [Tooltip("XP required to reach THIS tier (cumulative from tier 1)")]
        [Min(0)]
        public int xpRequired;

        [Header("Free Track Reward")]
        public BattlePassRewardData freeReward;

        [Header("Premium Track Reward")]
        [Tooltip("Only claimable if player owns the premium pass")]
        public BattlePassRewardData premiumReward;
    }
}
