
using System.Collections.Generic;
using ElementumDefense.Cards;

namespace ElementumDefense.Lootbox
{
    /// <summary>
    /// Result of opening a lootbox
    /// Contains all dropped cards and duplicate conversion info
    /// </summary>
    [System.Serializable]
    public class LootboxResult
    {
        /// <summary>
        /// The lootbox that was opened
        /// </summary>
        public LootboxData lootboxType;

        /// <summary>
        /// All cards that dropped (including duplicates before conversion)
        /// </summary>
        public List<CardDrop> cardDrops = new List<CardDrop>();

        /// <summary>
        /// Total currency earned from duplicate conversions
        /// </summary>
        public int totalDuplicateCurrency = 0;

        /// <summary>
        /// Number of new cards unlocked
        /// </summary>
        public int newCardsUnlocked = 0;

        /// <summary>
        /// Number of duplicate cards converted
        /// </summary>
        public int duplicatesConverted = 0;

        // ==========================================
        // HELPER METHODS
        // ==========================================

        /// <summary>
        /// Gets only new (non-duplicate) cards
        /// </summary>
        public List<CardDrop> GetNewCards()
        {
            return cardDrops.FindAll(drop => !drop.wasDuplicate);
        }

        /// <summary>
        /// Gets only duplicate cards
        /// </summary>
        public List<CardDrop> GetDuplicates()
        {
            return cardDrops.FindAll(drop => drop.wasDuplicate);
        }

        /// <summary>
        /// Gets summary string for UI
        /// </summary>
        public string GetSummary()
        {
            string summary = $"Opened {lootboxType.lootboxName}\n";
            summary += $"New Cards: {newCardsUnlocked}\n";

            if (duplicatesConverted > 0)
            {
                summary += $"Duplicates: {duplicatesConverted} → +{totalDuplicateCurrency} 💰";
            }

            return summary;
        }
    }

    /// <summary>
    /// Single card drop from lootbox
    /// </summary>
    [System.Serializable]
    public class CardDrop
    {
        public CardData card;
        public bool wasDuplicate;
        public int currencyEarned; // Only if duplicate

        public CardDrop(CardData card, bool wasDuplicate, int currencyEarned = 0)
        {
            this.card = card;
            this.wasDuplicate = wasDuplicate;
            this.currencyEarned = currencyEarned;
        }
    }
}