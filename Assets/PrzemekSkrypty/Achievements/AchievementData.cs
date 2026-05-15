// Assets/PrzemekSkrypty/Achievements/AchievementData.cs
using UnityEngine;

namespace ElementumDefense.Achievements
{
    /// <summary>
    /// Defines how progress toward an achievement is tracked.
    /// Each type maps to a specific stat from the game systems.
    /// </summary>
    public enum AchievementTrackType
    {
        /// <summary>Manual — only unlocked by calling AchievementManager.Unlock(id)</summary>
        Manual,

        /// <summary>Total wins (PlayerCollection.GetWins)</summary>
        Wins,

        /// <summary>Total losses (PlayerCollection.GetLosses)</summary>
        Losses,

        /// <summary>Total matches played (wins + losses)</summary>
        MatchesPlayed,

        /// <summary>Player level (PlayerCollection.GetLevel)</summary>
        PlayerLevel,

        /// <summary>Number of unlocked cards (PlayerCollection.GetUnlockedCards.Count)</summary>
        CardsUnlocked,

        /// <summary>Number of legendary cards unlocked</summary>
        LegendaryCardsUnlocked,

        /// <summary>Number of decks created (PlayerCollection.GetPlayerDecks.Count)</summary>
        DecksCreated,

        /// <summary>Total gold earned (cumulative, tracked by AchievementManager)</summary>
        GoldEarned,

        /// <summary>Total gold spent (cumulative, tracked by AchievementManager)</summary>
        GoldSpent,

        /// <summary>Total crystals earned (cumulative)</summary>
        CrystalsEarned,

        /// <summary>Total lootboxes opened (cumulative)</summary>
        LootboxesOpened,

        /// <summary>ELO rating reached (PlayerCollection.GetElo)</summary>
        EloReached,

        /// <summary>Number of owned skins</summary>
        SkinsOwned,

        /// <summary>Quests completed (cumulative)</summary>
        QuestsCompleted,
    }

    public enum AchievementRarity
    {
        Bronze,
        Silver,
        Gold,
        Platinum,
        Diamond
    }

    /// <summary>
    /// ScriptableObject defining a single achievement.
    /// Create via: Right Click → Create → Tower Defense → Achievements → Achievement
    /// Place in Assets/Resources/Achievements/
    /// </summary>
    [CreateAssetMenu(fileName = "New Achievement", menuName = "Tower Defense/Achievements/Achievement")]
    public class AchievementData : ScriptableObject
    {
        [Header("=== IDENTITY ===")]
        [Tooltip("Unique ID for save/load. Auto-fills from asset name.")]
        public string achievementId;

        public string achievementName = "New Achievement";

        [TextArea(2, 4)]
        public string description = "Description of what to do.";

        [Tooltip("Icon emoji/character for UI display")]
        public string iconEmoji = "★";

        [Tooltip("Optional sprite icon")]
        public Sprite icon;

        [Header("=== TRACKING ===")]
        [Tooltip("What stat does this achievement track?")]
        public AchievementTrackType trackType = AchievementTrackType.Manual;

        [Tooltip("Target value to complete (e.g., 10 wins, 5 decks)")]
        public int targetValue = 1;

        [Header("=== TIERS (optional) ===")]
        [Tooltip("If true, this achievement has multiple tiers (e.g., 1 win, 10 wins, 100 wins)")]
        public bool hasTiers = false;

        [Tooltip("Target values for each tier. Leave empty for single-tier.")]
        public int[] tierTargets;

        [Header("=== REWARDS ===")]
        [Tooltip("Gold reward on completion")]
        public int rewardGold = 0;

        [Tooltip("Crystals reward on completion")]
        public int rewardCrystals = 0;

        [Tooltip("XP reward on completion")]
        public int rewardXP = 0;

        [Header("=== DISPLAY ===")]
        public AchievementRarity rarity = AchievementRarity.Bronze;

        [Tooltip("Sort order in UI (lower = earlier)")]
        public int sortOrder = 0;

        [Tooltip("Is this achievement hidden until unlocked?")]
        public bool isHidden = false;

        // ==========================================
        // HELPERS
        // ==========================================

        /// <summary>Get the target for a specific tier (0-indexed). Returns targetValue for single-tier.</summary>
        public int GetTargetForTier(int tier)
        {
            if (!hasTiers || tierTargets == null || tierTargets.Length == 0)
                return targetValue;
            return tier < tierTargets.Length ? tierTargets[tier] : tierTargets[tierTargets.Length - 1];
        }

        /// <summary>Total number of tiers (1 for single-tier)</summary>
        public int TierCount => (hasTiers && tierTargets != null && tierTargets.Length > 0)
            ? tierTargets.Length
            : 1;

        public Color GetRarityColor()
        {
            return rarity switch
            {
                AchievementRarity.Bronze => new Color(0.80f, 0.50f, 0.20f),
                AchievementRarity.Silver => new Color(0.75f, 0.75f, 0.80f),
                AchievementRarity.Gold => new Color(1.00f, 0.84f, 0.00f),
                AchievementRarity.Platinum => new Color(0.30f, 0.85f, 0.85f),
                AchievementRarity.Diamond => new Color(0.70f, 0.30f, 1.00f),
                _ => Color.white
            };
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(achievementId))
                achievementId = name;
        }
    }
}
