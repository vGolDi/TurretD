// Assets/PrzemekSkrypty/BattlePass/BattlePassSeasonData.cs
using UnityEngine;
using System.Collections.Generic;

namespace ElementumDefense.BattlePass
{
    /// <summary>
    /// ScriptableObject defining a full Battle Pass season.
    /// Create via: Right Click → Create → Tower Defense → Battle Pass → Season Data
    /// 
    /// This is the main config SO — edit season duration, tiers, rewards,
    /// premium price, and XP settings all from the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "New BP Season", menuName = "Tower Defense/Battle Pass/Season Data")]
    public class BattlePassSeasonData : ScriptableObject
    {
        [Header("=== SEASON IDENTITY ===")]
        [Tooltip("Unique season ID (e.g. 'season_01', 'season_summer_2025')")]
        public string seasonId = "season_01";

        public string seasonName = "Season 1";

        [TextArea(2, 4)]
        public string seasonDescription = "The first Battle Pass season!";

        public Sprite seasonBanner;

        [Header("=== TIMING ===")]
        [Tooltip("Season start date (UTC). Format: yyyy-MM-dd")]
        public string startDate = "2025-01-01";

        [Tooltip("Season end date (UTC). Format: yyyy-MM-dd")]
        public string endDate = "2025-03-01";

        [Tooltip("Duration in days (auto-calculated from dates, but can override)")]
        [Min(1)]
        public int durationDays = 60;

        [Header("=== PREMIUM PASS ===")]
        [Tooltip("Cost in Crystals to unlock premium track")]
        [Min(0)]
        public int premiumPriceCrystals = 500;

        [Tooltip("Display name in shop")]
        public string premiumDisplayName = "Premium Battle Pass";

        [Tooltip("Icon for shop listing")]
        public Sprite premiumIcon;

        [Header("=== XP SETTINGS ===")]
        [Tooltip("XP awarded for completing a match (win)")]
        [Min(0)]
        public int xpPerMatchWin = 100;

        [Tooltip("XP awarded for completing a match (loss)")]
        [Min(0)]
        public int xpPerMatchLoss = 50;

        [Tooltip("Base XP per tier (tier 1 needs this much, each next tier scales)")]
        [Min(10)]
        public int baseXPPerTier = 1000;

        [Tooltip("XP scaling per tier (multiplied by tier number). 0 = flat progression.")]
        [Min(0)]
        public int xpScalingPerTier = 100;

        [Header("=== TIERS ===")]
        [Tooltip("All tiers in this season. Order matters — index 0 = tier 1.")]
        public List<BattlePassTierData> tiers = new List<BattlePassTierData>();

        // ==========================================
        // HELPERS
        // ==========================================

        /// <summary>
        /// Total number of tiers in this season.
        /// </summary>
        public int TotalTiers => tiers.Count;

        /// <summary>
        /// Gets XP required to reach a specific tier (1-based).
        /// If tiers have custom xpRequired set, uses those.
        /// Otherwise auto-calculates from baseXPPerTier + scaling.
        /// </summary>
        public int GetXPForTier(int tierNumber)
        {
            if (tierNumber <= 0) return 0;
            if (tierNumber > tiers.Count) return int.MaxValue;

            var tier = tiers[tierNumber - 1];

            // If custom XP is set on the tier, use it
            if (tier.xpRequired > 0)
                return tier.xpRequired;

            // Auto-calculate: cumulative XP
            int totalXP = 0;
            for (int i = 1; i <= tierNumber; i++)
            {
                totalXP += baseXPPerTier + (xpScalingPerTier * (i - 1));
            }
            return totalXP;
        }

        /// <summary>
        /// Gets XP needed for JUST this tier (not cumulative).
        /// Useful for progress bars.
        /// </summary>
        public int GetXPForTierOnly(int tierNumber)
        {
            if (tierNumber <= 1) return baseXPPerTier;
            return baseXPPerTier + (xpScalingPerTier * (tierNumber - 1));
        }

        /// <summary>
        /// Gets the tier data for a specific tier number (1-based).
        /// </summary>
        public BattlePassTierData GetTier(int tierNumber)
        {
            if (tierNumber <= 0 || tierNumber > tiers.Count) return null;
            return tiers[tierNumber - 1];
        }

        /// <summary>
        /// Calculates which tier a player is at given their total XP.
        /// Returns 0 if not yet at tier 1.
        /// </summary>
        public int GetTierForXP(int totalXP)
        {
            int currentTier = 0;
            for (int i = 1; i <= tiers.Count; i++)
            {
                if (totalXP >= GetXPForTier(i))
                    currentTier = i;
                else
                    break;
            }
            return currentTier;
        }

        /// <summary>
        /// Returns remaining days in the season from now.
        /// </summary>
        public int GetRemainingDays()
        {
            if (System.DateTime.TryParse(endDate, out System.DateTime end))
            {
                int days = (end - System.DateTime.UtcNow).Days;
                return Mathf.Max(0, days);
            }
            return 0;
        }

        /// <summary>
        /// Is the season currently active?
        /// </summary>
        public bool IsActive()
        {
            var now = System.DateTime.UtcNow;

            bool hasStart = System.DateTime.TryParse(startDate, out System.DateTime start);
            bool hasEnd = System.DateTime.TryParse(endDate, out System.DateTime end);

            if (hasStart && hasEnd)
                return now >= start && now <= end;

            return true; // If dates not set, always active
        }

        // ==========================================
        // EDITOR HELPERS
        // ==========================================

        private void OnValidate()
        {
            // Auto-number tiers
            for (int i = 0; i < tiers.Count; i++)
            {
                if (tiers[i].tierNumber == 0)
                    tiers[i].tierNumber = i + 1;
            }

            // Auto-calculate duration from dates
            if (System.DateTime.TryParse(startDate, out System.DateTime s) &&
                System.DateTime.TryParse(endDate, out System.DateTime e))
            {
                durationDays = Mathf.Max(1, (e - s).Days);
            }
        }
    }
}
