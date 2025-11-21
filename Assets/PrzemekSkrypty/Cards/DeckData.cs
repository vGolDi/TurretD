using ElementumDefense.Elements;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Represents a player's deck (25-30 cards)
    /// Used in deckbuilder and loaded into match
    /// </summary>
    [CreateAssetMenu(fileName = "New Deck", menuName = "Tower Defense/Cards/Deck")]
    public class DeckData : ScriptableObject
    {
        [Header("Deck Info")]
        public string deckName = "New Deck";

        [Tooltip("Which arena is this deck optimized for?")]
        public ElementType preferredArena = ElementType.Fire;

        [Header("Deck Composition")]
        [Tooltip("List of cards in this deck (25-30 cards)")]
        public List<CardData> cards = new List<CardData>();

        [Header("Limits")]
        public const int MIN_DECK_SIZE = 30;
        public const int MAX_DECK_SIZE = 30;

        public const int MAX_LEGENDARY = 5;
        public const int MAX_RARE = 10;
        public const int MAX_COMMON = 15;

        // ==========================================
        // VALIDATION
        // ==========================================

        /// <summary>
        /// Is this deck valid? (can be used in match)
        /// </summary>
        public bool IsValid(out string errorMessage)
        {
            errorMessage = "";

            // Check size
            if (cards.Count < MIN_DECK_SIZE)
            {
                errorMessage = $"Deck too small ({cards.Count}/{MIN_DECK_SIZE})";
                return false;
            }

            if (cards.Count > MAX_DECK_SIZE)
            {
                errorMessage = $"Deck too large ({cards.Count}/{MAX_DECK_SIZE})";
                return false;
            }

            // Count rarity
            int legendaryCount = 0;
            int rareCount = 0;
            int commonCount = 0;

            Dictionary<CardData, int> cardCounts = new Dictionary<CardData, int>();

            foreach (CardData card in cards)
            {
                if (card == null)
                {
                    errorMessage = "Deck contains null card!";
                    return false;
                }

                // Count rarity
                switch (card.rarity)
                {
                    case CardRarity.Legendary: legendaryCount++; break;
                    case CardRarity.Rare: rareCount++; break;
                    case CardRarity.Common: commonCount++; break;
                }

                // Count duplicates
                if (!cardCounts.ContainsKey(card))
                    cardCounts[card] = 0;

                cardCounts[card]++;

                // Check max copies
                if (cardCounts[card] > card.GetMaxCopies())
                {
                    errorMessage = $"Too many copies of '{card.cardName}' (max {card.GetMaxCopies()})";
                    return false;
                }
            }

            // Check rarity limits
            if (legendaryCount > MAX_LEGENDARY)
            {
                errorMessage = $"Too many Legendary cards ({legendaryCount}/{MAX_LEGENDARY})";
                return false;
            }

            if (rareCount > MAX_RARE)
            {
                errorMessage = $"Too many Rare cards ({rareCount}/{MAX_RARE})";
                return false;
            }

            if (commonCount > MAX_COMMON)
            {
                errorMessage = $"Too many Common cards ({commonCount}/{MAX_COMMON})";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns rarity counts
        /// </summary>
        public (int legendary, int rare, int common) GetRarityCounts()
        {
            int leg = 0, rare = 0, com = 0;

            foreach (CardData card in cards)
            {
                if (card == null) continue;

                switch (card.rarity)
                {
                    case CardRarity.Legendary: leg++; break;
                    case CardRarity.Rare: rare++; break;
                    case CardRarity.Common: com++; break;
                }
            }

            return (leg, rare, com);
        }

        /// <summary>
        /// Can we add this card to deck?
        /// </summary>
        public bool CanAddCard(CardData card, out string reason)
        {
            reason = "";

            if (card == null)
            {
                reason = "Card is null";
                return false;
            }

            // Check deck size
            if (cards.Count >= MAX_DECK_SIZE)
            {
                reason = $"Deck full ({MAX_DECK_SIZE}/{MAX_DECK_SIZE})";
                return false;
            }

            // Check rarity limit
            var (leg, rare, com) = GetRarityCounts();

            switch (card.rarity)
            {
                case CardRarity.Legendary:
                    if (leg >= MAX_LEGENDARY)
                    {
                        reason = $"Max Legendary cards reached ({MAX_LEGENDARY})";
                        return false;
                    }
                    break;

                case CardRarity.Rare:
                    if (rare >= MAX_RARE)
                    {
                        reason = $"Max Rare cards reached ({MAX_RARE})";
                        return false;
                    }
                    break;

                case CardRarity.Common:
                    if (com >= MAX_COMMON)
                    {
                        reason = $"Max Common cards reached ({MAX_COMMON})";
                        return false;
                    }
                    break;
            }

            // Check max copies
            int currentCopies = cards.Count(c => c == card);
            if (currentCopies >= card.GetMaxCopies())
            {
                reason = $"Max copies of '{card.cardName}' ({card.GetMaxCopies()})";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds card to deck (with validation)
        /// </summary>
        public bool AddCard(CardData card)
        {
            if (CanAddCard(card, out string reason))
            {
                cards.Add(card);
                return true;
            }

            Debug.LogWarning($"[DeckData] Cannot add card: {reason}");
            return false;
        }

        /// <summary>
        /// Removes card from deck
        /// </summary>
        public bool RemoveCard(CardData card)
        {
            return cards.Remove(card);
        }

        /// <summary>
        /// Clears all cards
        /// </summary>
        public void Clear()
        {
            cards.Clear();
        }
    }
}