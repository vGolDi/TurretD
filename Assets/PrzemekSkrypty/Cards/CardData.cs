using UnityEngine;
using ElementumDefense.Elements;
using UnityEngine.UI;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Base ScriptableObject for all cards
    /// Contains metadata and reference to effect implementation
    /// </summary>
    [CreateAssetMenu(fileName = "New Card", menuName = "Tower Defense/Cards/Card Data")]
    public class CardData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Display name of card")]
        public string cardName = "New Card";

        [TextArea(3, 5)]
        [Tooltip("Description shown in UI")]
        public string description = "Card description here...";

        [Header("Card Properties")]
        public CardRarity rarity = CardRarity.Common;
        public CardType cardType = CardType.Turret;
        public CardActivationType activationType = CardActivationType.Continuous;

        [Header("Visual")]
        [Tooltip("Card icon/art")]
        public Sprite cardIcon;

        [Tooltip("Background color (auto-set by rarity if null)")]
        public Color cardColor = Color.white;

        [Header("Gameplay")]
        [Tooltip("Element association (for turret cards)")]
        public ElementType associatedElement = ElementType.None;


        [Header("Deck Restrictions")]
        [Tooltip("Max copies allowed in deck (0 = use rarity default)")]
        public int maxCopiesInDeck = 0;

        [Tooltip("Required player level to unlock")]
        public int requiredLevel = 1;

        [Header("F2P/Economy")]
        [Tooltip("Cost to unlock this card (in-game currency)")]
        public int unlockCost = 100;

        [Tooltip("Can drop from lootboxes?")]
        public bool canDropFromLootbox = true;

        [Tooltip("Is this card part of starter deck?")]
        public bool isStarterCard = false;

        // ==========================================
        // EFFECT SYSTEM (Pluggable)
        // ==========================================

        /// <summary>
        /// Reference to effect implementation
        /// This is a ScriptableObject implementing ICardEffect
        /// </summary>
        [Header("Effect Implementation")]
        [Tooltip("Drag CardEffect ScriptableObject here")]
        public CardEffectBase cardEffect;

        // ==========================================
        // HELPER METHODS
        // ==========================================

        /// <summary>
        /// Returns max allowed copies based on rarity (if not overridden)
        /// </summary>
        public int GetMaxCopies()
        {
            if (maxCopiesInDeck > 0)
                return maxCopiesInDeck;

            // Default limits
            return rarity switch
            {
                CardRarity.Legendary => 1,  // Unique
                CardRarity.Rare => 1,       // Max 1 copies
                CardRarity.Common => 2,     // Max 2 copies
                _ => 1
            };
        }

        /// <summary>
        /// Returns rarity color for UI
        /// </summary>
public Color GetRarityColor()
        {
            // ZAWSZE zwracaj kolor bazujący na rarity - ignoruj cardColor
            // To zapewnia spójność kolorów dla wszystkich kart tego samego rarity
            return rarity switch
            {
                CardRarity.Common => new Color(0.8f, 0.8f, 0.8f),    // Gray
                CardRarity.Rare => new Color(0.3f, 0.6f, 1f),        // Blue
                CardRarity.Legendary => new Color(1f, 0.8f, 0f),     // Gold
                _ => Color.white
            };
        }
        public string GetRarityName()
        {
            return rarity switch
            {
                CardRarity.Common => "COMMON",
                CardRarity.Rare => "RARE",
                CardRarity.Legendary => "LEGENDARY",
                _ => "UNKNOWN"
            };
        }

        /// <summary>
        /// Returns formatted tooltip text
        /// </summary>
        public string GetTooltip()
        {
            string tooltip = $"<b>{cardName}</b> [{rarity}]\n\n";
            tooltip += $"{description}\n\n";

            if (cardEffect != null)
            {
                tooltip += $"<i>{cardEffect.GetEffectDescription()}</i>\n";
            }

            // Activation type info
            string activationInfo = activationType == CardActivationType.OnDraft
                ? "⚡ Instant Effect (on draft)"
                : "🔄 Continuous Effect (whole game)";
            tooltip += $"\n{activationInfo}";

            return tooltip;
        }

        /// <summary>
        /// Validates card data (called in editor)
        /// </summary>
        private void OnValidate()
        {
            // Auto-assign color based on rarity if not set
            if (cardColor == Color.white)
            {
                cardColor = GetRarityColor();
            }

            // Validate effect
            if (cardEffect == null)
            {
                Debug.LogWarning($"[CardData] {cardName} has no effect assigned!");
            }
        }
    }
}