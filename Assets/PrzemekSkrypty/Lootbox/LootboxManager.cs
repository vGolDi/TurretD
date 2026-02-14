// Assets/PrzemekSkrypty/Lootbox/LootboxManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ElementumDefense.Cards;

namespace ElementumDefense.Lootbox
{
    /// <summary>
    /// Handles lootbox opening logic
    /// Rolls cards based on drop rates, handles duplicates
    /// </summary>
    public class LootboxManager : MonoBehaviour
    {
        public static LootboxManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private LootboxInventory lootboxInventory;

        [Header("Duplicate Currency Type")]
        [SerializeField, Tooltip("What currency do duplicates convert to?")]
        private DuplicateCurrencyType duplicateCurrency = DuplicateCurrencyType.Gold;

        [Header("Debug")]
        [SerializeField] private bool logDrops = true;

        // Events
        public System.Action<LootboxData> OnLootboxOpening;                // Before opening animation
        public System.Action<LootboxResult> OnLootboxOpened;               // After all cards revealed
        public System.Action<CardDrop, int> OnCardRevealed;                // Each card (card, index)
        public System.Action<int> OnDuplicateCurrencyEarned;               // Total currency from duplicates

        // ==========================================
        // INITIALIZATION
        // ==========================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (lootboxInventory == null)
            {
                lootboxInventory = LootboxInventory.Instance;
            }
        }

        private void Start()
        {
            if (lootboxInventory == null)
            {
                lootboxInventory = FindObjectOfType<LootboxInventory>();
            }
        }

        // ==========================================
        // MAIN API - OPEN LOOTBOX
        // ==========================================

        /// <summary>
        /// Opens a lootbox and returns result
        /// </summary>
        /// <param name="lootboxType">Type of lootbox to open</param>
        /// <returns>Result with all dropped cards</returns>
        public LootboxResult OpenLootbox(LootboxData lootboxType)
        {
            if (lootboxType == null)
            {
                Debug.LogError("[LootboxManager] Cannot open null lootbox!");
                return null;
            }

            // Check if player owns this lootbox
            if (lootboxInventory != null && !lootboxInventory.HasLootbox(lootboxType))
            {
                Debug.LogWarning($"[LootboxManager] Player doesn't own any {lootboxType.lootboxName}!");
                return null;
            }

            // Remove from inventory
            if (lootboxInventory != null)
            {
                lootboxInventory.RemoveLootbox(lootboxType, 1);
            }

            OnLootboxOpening?.Invoke(lootboxType);

            // Create result
            LootboxResult result = new LootboxResult
            {
                lootboxType = lootboxType
            };

            // Get player's collection
            PlayerCollection playerCollection = PlayerCollection.Instance;

            if (playerCollection == null)
            {
                Debug.LogError("[LootboxManager] PlayerCollection not found!");
                return null;
            }

            // Roll cards
            List<CardData> rolledCards = RollCards(lootboxType);

            // Process each card
            for (int i = 0; i < rolledCards.Count; i++)
            {
                CardData card = rolledCards[i];

                if (card == null) continue;

                bool isDuplicate = playerCollection.IsUnlocked(card);
                int currencyEarned = 0;

                if (isDuplicate)
                {
                    // Convert to currency
                    currencyEarned = lootboxType.GetDuplicateValue(card.rarity);

                    // Add currency to player
                    if (duplicateCurrency == DuplicateCurrencyType.Gold)
                    {
                        playerCollection.AddGold(currencyEarned);
                    }
                    else
                    {
                        playerCollection.AddCrystals(currencyEarned);
                    }

                    result.duplicatesConverted++;
                    result.totalDuplicateCurrency += currencyEarned;

                    if (logDrops)
                    {
                        Debug.Log($"[LootboxManager] DUPLICATE: {card.cardName} → +{currencyEarned} {duplicateCurrency}");
                    }
                }
                else
                {
                    // Unlock new card
                    playerCollection.UnlockCard(card);
                    result.newCardsUnlocked++;

                    if (logDrops)
                    {
                        Debug.Log($"[LootboxManager] NEW CARD: {card.cardName} ({card.rarity})");
                    }
                }

                // Create drop entry
                CardDrop drop = new CardDrop(card, isDuplicate, currencyEarned);
                result.cardDrops.Add(drop);

                // Trigger event for UI animation
                OnCardRevealed?.Invoke(drop, i);
            }

            // Final events
            if (result.totalDuplicateCurrency > 0)
            {
                OnDuplicateCurrencyEarned?.Invoke(result.totalDuplicateCurrency);
            }

            OnLootboxOpened?.Invoke(result);

            if (logDrops)
            {
                Debug.Log($"[LootboxManager] === LOOTBOX OPENED ===\n{result.GetSummary()}");
            }

            return result;
        }

        /// <summary>
        /// Opens lootbox without inventory check (for rewards)
        /// </summary>
        public LootboxResult OpenLootboxDirect(LootboxData lootboxType)
        {
            if (lootboxType == null) return null;

            // Temporarily bypass inventory check
            LootboxInventory tempInventory = lootboxInventory;
            lootboxInventory = null;

            LootboxResult result = OpenLootbox(lootboxType);

            lootboxInventory = tempInventory;

            return result;
        }

        // ==========================================
        // CARD ROLLING LOGIC
        // ==========================================

        /// <summary>
        /// Rolls cards based on lootbox configuration
        /// </summary>
        private List<CardData> RollCards(LootboxData lootbox)
        {
            List<CardData> rolledCards = new List<CardData>();

            // Get all available cards
            PlayerCollection playerCollection = PlayerCollection.Instance;
            List<CardData> allCards = GetAllDroppableCards();

            if (allCards.Count == 0)
            {
                Debug.LogError("[LootboxManager] No droppable cards found!");
                return rolledCards;
            }

            // Separate by rarity
            List<CardData> commonCards = allCards.Where(c => c.rarity == CardRarity.Common).ToList();
            List<CardData> rareCards = allCards.Where(c => c.rarity == CardRarity.Rare).ToList();
            List<CardData> legendaryCards = allCards.Where(c => c.rarity == CardRarity.Legendary).ToList();

            int cardsToRoll = lootbox.cardCount;

            // First: Add guaranteed drops
            for (int i = 0; i < lootbox.guaranteedCommon && cardsToRoll > 0; i++)
            {
                CardData card = GetRandomCard(commonCards);
                if (card != null)
                {
                    rolledCards.Add(card);
                    cardsToRoll--;
                }
            }

            for (int i = 0; i < lootbox.guaranteedRare && cardsToRoll > 0; i++)
            {
                CardData card = GetRandomCard(rareCards);
                if (card != null)
                {
                    rolledCards.Add(card);
                    cardsToRoll--;
                }
            }

            for (int i = 0; i < lootbox.guaranteedLegendary && cardsToRoll > 0; i++)
            {
                CardData card = GetRandomCard(legendaryCards);
                if (card != null)
                {
                    rolledCards.Add(card);
                    cardsToRoll--;
                }
            }

            // Then: Roll remaining cards based on drop rates
            for (int i = 0; i < cardsToRoll; i++)
            {
                CardRarity rolledRarity = RollRarity(lootbox);

                List<CardData> pool = rolledRarity switch
                {
                    CardRarity.Legendary => legendaryCards.Count > 0 ? legendaryCards : rareCards,
                    CardRarity.Rare => rareCards.Count > 0 ? rareCards : commonCards,
                    _ => commonCards
                };

                // Fallback if pool is empty
                if (pool.Count == 0) pool = allCards;

                CardData card = GetRandomCard(pool);
                if (card != null)
                {
                    rolledCards.Add(card);
                }
            }

            return rolledCards;
        }

        /// <summary>
        /// Rolls a random rarity based on drop rates
        /// </summary>
        private CardRarity RollRarity(LootboxData lootbox)
        {
            float roll = Random.Range(0f, 100f);

            if (roll < lootbox.legendaryDropRate)
            {
                return CardRarity.Legendary;
            }

            if (roll < lootbox.legendaryDropRate + lootbox.rareDropRate)
            {
                return CardRarity.Rare;
            }

            return CardRarity.Common;
        }

        /// <summary>
        /// Gets random card from pool
        /// </summary>
        private CardData GetRandomCard(List<CardData> pool)
        {
            if (pool == null || pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }

        /// <summary>
        /// Gets all cards that can drop from lootboxes
        /// </summary>
        private List<CardData> GetAllDroppableCards()
        {
            CardData[] allCards = Resources.LoadAll<CardData>("Cards");
            return allCards.Where(c => c != null && c.canDropFromLootbox).ToList();
        }

        // ==========================================
        // UTILITY
        // ==========================================

        /// <summary>
        /// Checks if player can open specific lootbox
        /// </summary>
        public bool CanOpenLootbox(LootboxData lootboxType)
        {
            if (lootboxType == null) return false;
            if (lootboxInventory == null) return true; // No inventory check

            return lootboxInventory.HasLootbox(lootboxType);
        }

        /// <summary>
        /// Simulates opening (for preview/testing)
        /// </summary>
        public LootboxResult SimulateOpen(LootboxData lootboxType)
        {
            if (lootboxType == null) return null;

            LootboxResult result = new LootboxResult { lootboxType = lootboxType };
            List<CardData> cards = RollCards(lootboxType);

            PlayerCollection collection = PlayerCollection.Instance;

            foreach (var card in cards)
            {
                bool isDupe = collection != null && collection.IsUnlocked(card);
                int currency = isDupe ? lootboxType.GetDuplicateValue(card.rarity) : 0;

                result.cardDrops.Add(new CardDrop(card, isDupe, currency));

                if (isDupe)
                {
                    result.duplicatesConverted++;
                    result.totalDuplicateCurrency += currency;
                }
                else
                {
                    result.newCardsUnlocked++;
                }
            }

            return result;
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Test Open Random Lootbox")]
        private void TestOpenRandom()
        {
            if (lootboxInventory == null)
            {
                Debug.LogError("[LootboxManager] No inventory!");
                return;
            }

            var owned = lootboxInventory.GetOwnedLootboxes();
            if (owned.Count == 0)
            {
                Debug.LogWarning("[LootboxManager] No lootboxes to open!");
                return;
            }

            OpenLootbox(owned[0].lootboxType);
        }
    }

    public enum DuplicateCurrencyType
    {
        Gold,
        Crystals
    }
}