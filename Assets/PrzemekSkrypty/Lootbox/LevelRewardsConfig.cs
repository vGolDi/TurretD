// Assets/PrzemekSkrypty/Progression/LevelRewardsConfig.cs
using UnityEngine;
using System.Collections.Generic;
using ElementumDefense.Lootbox;

namespace ElementumDefense.Progression
{
    /// <summary>
    /// Configuration for level-up rewards
    /// </summary>
    [CreateAssetMenu(fileName = "LevelRewardsConfig", menuName = "Tower Defense/Progression/Level Rewards Config")]
    public class LevelRewardsConfig : ScriptableObject
    {
        [Header("Default Rewards (Every Level)")]
        public int goldPerLevel = 500;
        public int crystalsPerLevel = 10;

        [Header("Lootbox Rewards")]
        [Tooltip("Lootbox given every X levels")]
        public int lootboxEveryXLevels = 5;

        [Tooltip("Default lootbox for milestone levels")]
        public LootboxData milestoneLootbox;

        [Header("Special Level Rewards")]
        [Tooltip("Custom rewards for specific levels")]
        public List<LevelReward> specialRewards = new List<LevelReward>();

        [Header("Streak Bonus")]
        [Tooltip("Extra rewards for consecutive days playing")]
        public bool enableDailyStreak = true;
        public int streakBonusGold = 100;
        public LootboxData streakLootbox;
        public int daysForStreakLootbox = 7;

        /// <summary>
        /// Gets rewards for specific level
        /// </summary>
        public LevelReward GetRewardsForLevel(int level)
        {
            // Check for special reward first
            LevelReward special = specialRewards.Find(r => r.level == level);
            if (special != null && special.level > 0)
            {
                return special;
            }

            // Default rewards
            LevelReward defaultReward = new LevelReward
            {
                level = level,
                gold = goldPerLevel,
                crystals = crystalsPerLevel,
                lootbox = null
            };

            // Check milestone lootbox
            if (lootboxEveryXLevels > 0 && level % lootboxEveryXLevels == 0)
            {
                defaultReward.lootbox = milestoneLootbox;
            }

            return defaultReward;
        }
    }

    [System.Serializable]
    public class LevelReward
    {
        public int level;
        public int gold;
        public int crystals;
        public LootboxData lootbox;

        [Tooltip("Special card unlock for this level")]
        public ElementumDefense.Cards.CardData unlockCard;

        [Tooltip("Custom message for this level")]
        public string customMessage;
    }
}