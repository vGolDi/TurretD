using UnityEngine;
using ElementumDefense.Elements;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Sabotage card - affects opponent(s)
    /// Drawn from global pool (not player's deck)
    /// </summary>
    [CreateAssetMenu(fileName = "New Sabotage", menuName = "Tower Defense/Cards/Sabotage Card")]
    public class SabotageCardData : ScriptableObject
    {
        [Header("Basic Info")]
        public string sabotageName = "New Sabotage";

        [TextArea(3, 5)]
        [Tooltip("What this sabotage does")]
        public string description = "Sabotage description...";

        [Header("Sabotage Properties")]
        public CardRarity rarity = CardRarity.Common;

        [Tooltip("Category/tag of sabotage (for anti-spam)")]
        public SabotageTag sabotageTag = SabotageTag.Economy;

        [Header("Duration")]
        public SabotageDurationType durationType = SabotageDurationType.Temporary;

        [Tooltip("Duration in seconds (0 = instant, -1 = permanent)")]
        public float duration = 10f;

        [Tooltip("Duration in rounds/waves (alternative to seconds)")]
        public int durationRounds = 0;

        [Header("Visual")]
        public Sprite sabotageIcon;
        public Color sabotageColor = Color.red;

        [Header("Drop Rate (for weighted random)")]
        [Tooltip("Weight for random selection (higher = more common)")]
        [Range(1f, 100f)]
        public float dropWeight = 50f;

        // ==========================================
        // EFFECT SYSTEM
        // ==========================================

        [Header("Effect Implementation")]
        [Tooltip("Drag SabotageEffect ScriptableObject here")]
        public SabotageEffectBase sabotageEffect;

        // ==========================================
        // HELPER METHODS
        // ==========================================

        /// <summary>
        /// Returns formatted tooltip
        /// </summary>
        public string GetTooltip()
        {
            string tooltip = $"<b>{sabotageName}</b> [{rarity}]\n\n";
            tooltip += $"{description}\n\n";

            if (sabotageEffect != null)
            {
                tooltip += $"<i>{sabotageEffect.GetEffectDescription()}</i>\n";
            }

            // Duration info
            string durationInfo = GetDurationText();
            tooltip += $"\n⏱️ {durationInfo}";

            return tooltip;
        }

        /// <summary>
        /// Returns human-readable duration
        /// </summary>
        public string GetDurationText()
        {
            if (durationType == SabotageDurationType.Permanent)
            {
                return "Permanent (rest of game)";
            }

            if (durationType == SabotageDurationType.Instant)
            {
                return "Instant (one-time)";
            }

            // Temporary
            if (durationRounds > 0)
            {
                return $"{durationRounds} round{(durationRounds > 1 ? "s" : "")}";
            }

            return $"{duration:F0} seconds";
        }

        /// <summary>
        /// Returns rarity color
        /// </summary>
        public Color GetRarityColor()
        {
            return rarity switch
            {
                CardRarity.Common => new Color(0.8f, 0.8f, 0.8f),
                CardRarity.Rare => new Color(0.3f, 0.6f, 1f),
                CardRarity.Legendary => new Color(1f, 0.8f, 0f),
                _ => Color.white
            };
        }

        private void OnValidate()
        {
            if (sabotageEffect == null)
            {
                Debug.LogWarning($"[SabotageCardData] {sabotageName} has no effect assigned!");
            }

            // Auto-set instant duration
            if (durationType == SabotageDurationType.Instant)
            {
                duration = 0f;
                durationRounds = 0;
            }
        }
    }

    // ==========================================
    // ENUMS
    // ==========================================

    /// <summary>
    /// Sabotage category (for anti-spam)
    /// Max 2 of same tag in one draft
    /// </summary>
    public enum SabotageTag
    {
        Economy,    // Gold generation, costs, income
        Turrets,    // Turret damage, upgrades, building
        Enemies,    // Enemy HP, speed, element
        Arena,      // Build spots, fog, path manipulation
        Player      // Player HP, movement, vision
    }

    /// <summary>
    /// How long does sabotage last?
    /// </summary>
    public enum SabotageDurationType
    {
        Instant,    // One-time effect (e.g., destroy random turret)
        Temporary,  // Lasts X seconds/rounds
        Permanent   // Lasts until end of game
    }
}