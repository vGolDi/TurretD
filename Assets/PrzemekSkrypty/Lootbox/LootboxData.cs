
using UnityEngine;
using System.Collections.Generic;

namespace ElementumDefense.Lootbox
{
    /// <summary>
    /// Defines a lootbox type with drop rates and rewards
    /// </summary>
    [CreateAssetMenu(fileName = "New Lootbox", menuName = "Tower Defense/Lootbox/Lootbox Data")]
    public class LootboxData : ScriptableObject
    {
        [Header("Basic Info")]
        public string lootboxName = "Standard Crate";

        [TextArea(2, 4)]
        public string description = "Contains 3 random cards.";

        public LootboxRarity rarity = LootboxRarity.Common;

        [Header("Visuals")]
        public Sprite lootboxIcon;
        //public Sprite lootboxOpenedIcon;
        //public Color glowColor = Color.white;

        [Header("Rewards Configuration")]
        [Tooltip("How many cards this lootbox gives")]
        [Range(1, 10)]
        public int cardCount = 3;

        [Header("Rarity Drop Rates (must sum to 100)")]
        [Range(0f, 100f)]
        public float commonDropRate = 70f;

        [Range(0f, 100f)]
        public float rareDropRate = 25f;

        [Range(0f, 100f)]
        public float legendaryDropRate = 5f;

        [Header("Guaranteed Drops")]
        [Tooltip("Minimum guaranteed cards of each rarity")]
        public int guaranteedCommon = 0;
        public int guaranteedRare = 0;
        public int guaranteedLegendary = 0;

        [Header("Duplicate Conversion")]
        [Tooltip("Currency given when card is duplicate")]
        public int commonDuplicateValue = 10;
        public int rareDuplicateValue = 50;
        public int legendaryDuplicateValue = 200;

        [Header("Lootbox Value")]
        [Tooltip("How much this lootbox costs in shop (0 = not purchasable)")]
        public int shopPriceGold = 0;
        public int shopPriceCrystals = 0;

        [Header("Acquisition")]
        [Tooltip("Can be earned from level completion?")]
        public bool dropsFromLevels = true;

        [Tooltip("Can be earned from daily quests?")]
        public bool dropsFromQuests = true;

        [Tooltip("Is this a special/event lootbox?")]
        public bool isEventLootbox = false;

        // ==========================================
        // VALIDATION
        // ==========================================

        private void OnValidate()
        {
            // Ensure drop rates sum to 100
            float total = commonDropRate + rareDropRate + legendaryDropRate;

            if (Mathf.Abs(total - 100f) > 0.1f)
            {
                Debug.LogWarning($"[LootboxData] {lootboxName}: Drop rates sum to {total}%, should be 100%!");
            }

            // Ensure guaranteed drops don't exceed card count
            int totalGuaranteed = guaranteedCommon + guaranteedRare + guaranteedLegendary;
            if (totalGuaranteed > cardCount)
            {
                Debug.LogError($"[LootboxData] {lootboxName}: Guaranteed drops ({totalGuaranteed}) exceed card count ({cardCount})!");
            }
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        /// <summary>
        /// Gets duplicate value for specific rarity
        /// </summary>
        public int GetDuplicateValue(Cards.CardRarity rarity)
        {
            return rarity switch
            {
                Cards.CardRarity.Common => commonDuplicateValue,
                Cards.CardRarity.Rare => rareDuplicateValue,
                Cards.CardRarity.Legendary => legendaryDuplicateValue,
                _ => commonDuplicateValue
            };
        }

        /// <summary>
        /// Gets color based on lootbox rarity
        /// </summary>
        public Color GetRarityColor()
        {
            return rarity switch
            {
                LootboxRarity.Common => new Color(0.7f, 0.7f, 0.7f),      // Gray
                LootboxRarity.Rare => new Color(0.3f, 0.5f, 1f),          // Blue
                LootboxRarity.Epic => new Color(0.7f, 0.3f, 1f),          // Purple
                LootboxRarity.Legendary => new Color(1f, 0.8f, 0f),       // Gold
                LootboxRarity.Event => new Color(1f, 0.4f, 0.4f),         // Red
                _ => Color.white
            };
        }
    }

    /// <summary>
    /// Lootbox rarity tiers
    /// </summary>
    public enum LootboxRarity
    {
        Common,     // Basic crate - 3 cards
        Rare,       // Better rates - 3 cards
        Epic,       // Good rates - 4 cards
        Legendary,  // Best rates - 5 cards
        Event       // Special event crates
    }
}