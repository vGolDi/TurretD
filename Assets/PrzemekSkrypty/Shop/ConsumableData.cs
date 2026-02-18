// Assets/PrzemekSkrypty/Shop/ConsumableData.cs
using UnityEngine;

namespace ElementumDefense.Shop
{
    /// <summary>
    /// Placeholder ScriptableObject for consumable item definitions.
    /// Create via: Right Click → Create → Tower Defense → Shop → Consumable Data
    /// </summary>
    public enum ConsumableType
    {
        XPBoost,        // Multiplies XP earned
        GoldBoost,      // Multiplies gold earned
        ExtraLife,      // Extra life in a match
        DeckReroll,     // Reroll starting hand
        SpeedUp,        // Temporary game speed boost
        Shield,         // Damage reduction
        ElementSwap,    // Change tower element mid-game
        Other
    }

    [CreateAssetMenu(fileName = "New Consumable", menuName = "Tower Defense/Shop/Consumable Data")]
    public class ConsumableData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Unique identifier for save/load")]
        public string consumableId;

        public string consumableName = "Boost";

        [TextArea(2, 3)]
        public string description = "A helpful consumable item.";

        public Sprite icon;
        public ConsumableType type = ConsumableType.Other;

        [Header("Effect Configuration")]
        [Tooltip("Effect multiplier (e.g. 1.5 = 50% boost, 2.0 = 100% boost)")]
        public float effectValue = 1.5f;

        [Tooltip("Duration in minutes. 0 = instant/one-time use")]
        public float durationMinutes = 30f;

        [Tooltip("Can be stacked with other consumables of same type?")]
        public bool stackable = false;

        [Tooltip("Max stack count per match")]
        public int maxStacksPerMatch = 1;

        [Header("Usage Context")]
        [Tooltip("Can be used before match starts?")]
        public bool useBeforeMatch = true;

        [Tooltip("Can be activated during match?")]
        public bool useDuringMatch = false;

        // ==========================================
        // FUTURE EXPANSION
        // ==========================================
        // public int usesPerPurchase = 1;
        // public bool consumedOnMatchEnd = true;

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(consumableId))
                consumableId = name;
        }
    }
}