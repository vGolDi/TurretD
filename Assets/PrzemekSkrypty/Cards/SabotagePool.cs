using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Singleton managing global pool of sabotage cards
    /// Handles weighted random selection with rarity sync and tag filtering
    /// </summary>
    public class SabotagePool : MonoBehaviourPunCallbacks
    {
        public static SabotagePool Instance { get; private set; }

        [Header("Sabotage Card Pool")]
        [Tooltip("All available sabotage cards (~50 cards)")]
        [SerializeField] private List<SabotageCardData> allSabotageCards = new List<SabotageCardData>();

        [Header("Drop Rate Settings")]
        [Tooltip("Base drop weights by rarity (if card has no custom weight)")]
        [SerializeField] private float legendaryBaseWeight = 5f;
        [SerializeField] private float rareBaseWeight = 25f;
        [SerializeField] private float commonBaseWeight = 70f;

        [Header("Anti-Spam Settings")]
        [Tooltip("Maximum same-tag cards in one draft (1-3)")]
        [SerializeField] private int maxSameTagInDraft = 2;

        [Header("Debug")]
        [SerializeField] private bool logSelections = true;

        // Cache for performance
        private Dictionary<CardRarity, List<SabotageCardData>> cardsByRarity;
        private bool isInitialized = false;

        // ==========================================
        // INITIALIZATION
        // ==========================================

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional - depends on your scene structure

            InitializePool();
        }

        /// <summary>
        /// Initializes card pool and caches by rarity
        /// </summary>
        private void InitializePool()
        {
            if (isInitialized) return;

            // Validate pool
            if (allSabotageCards == null || allSabotageCards.Count == 0)
            {
                Debug.LogError("[SabotagePool] No sabotage cards assigned! Create some in Resources.");
                return;
            }

            // Remove null entries
            allSabotageCards.RemoveAll(card => card == null);

            // Cache by rarity
            cardsByRarity = new Dictionary<CardRarity, List<SabotageCardData>>
            {
                { CardRarity.Common, new List<SabotageCardData>() },
                { CardRarity.Rare, new List<SabotageCardData>() },
                { CardRarity.Legendary, new List<SabotageCardData>() }
            };

            foreach (var card in allSabotageCards)
            {
                if (cardsByRarity.ContainsKey(card.rarity))
                {
                    cardsByRarity[card.rarity].Add(card);
                }
            }

            isInitialized = true;

            Debug.Log($"[SabotagePool] Initialized with {allSabotageCards.Count} cards:");
            Debug.Log($"  - Common: {cardsByRarity[CardRarity.Common].Count}");
            Debug.Log($"  - Rare: {cardsByRarity[CardRarity.Rare].Count}");
            Debug.Log($"  - Legendary: {cardsByRarity[CardRarity.Legendary].Count}");
        }

        // ==========================================
        // MAIN API - DRAFT SELECTION
        // ==========================================

        /// <summary>
        /// Master Client: Generates rarity combination for all players
        /// Returns array of 3 rarities (e.g., [Rare, Rare, Common])
        /// </summary>
        public CardRarity[] GenerateRarityCombination()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[SabotagePool] Only Master Client can generate rarity combo!");
                return null;
            }

            // Weighted random for 3 cards
            CardRarity[] combination = new CardRarity[3];

            for (int i = 0; i < 3; i++)
            {
                combination[i] = GetRandomRarity();
            }

            if (logSelections)
            {
                Debug.Log($"[SabotagePool] Generated rarity combo: [{combination[0]}, {combination[1]}, {combination[2]}]");
            }

            return combination;
        }

        /// <summary>
        /// Draws 3 sabotage cards based on rarity combination
        /// Ensures max 2 cards of same tag (anti-spam)
        /// </summary>
        /// <param name="rarityCombination">Array of 3 rarities from Master Client</param>
        /// <returns>3 sabotage cards for draft UI</returns>
        public SabotageCardData[] DrawSabotageCards(CardRarity[] rarityCombination)
        {
            if (rarityCombination == null || rarityCombination.Length != 3)
            {
                Debug.LogError("[SabotagePool] Invalid rarity combination!");
                return null;
            }

            SabotageCardData[] drawnCards = new SabotageCardData[3];
            Dictionary<SabotageTag, int> tagCounts = new Dictionary<SabotageTag, int>();

            for (int i = 0; i < 3; i++)
            {
                CardRarity targetRarity = rarityCombination[i];

                // Try draw card with tag filtering
                SabotageCardData card = DrawCardWithTagLimit(targetRarity, tagCounts);

                if (card == null)
                {
                    Debug.LogWarning($"[SabotagePool] Failed to draw {targetRarity} card (slot {i})");
                    continue;
                }

                drawnCards[i] = card;

                // Update tag count
                if (!tagCounts.ContainsKey(card.sabotageTag))
                    tagCounts[card.sabotageTag] = 0;

                tagCounts[card.sabotageTag]++;
            }

            if (logSelections)
            {
                Debug.Log($"[SabotagePool] Drew cards: {string.Join(", ", drawnCards.Select(c => c?.sabotageName ?? "NULL"))}");
            }

            return drawnCards;
        }

        // ==========================================
        // WEIGHTED RANDOM SELECTION
        // ==========================================

        /// <summary>
        /// Selects random rarity using weighted probabilities
        /// </summary>
        private CardRarity GetRandomRarity()
        {
            float totalWeight = legendaryBaseWeight + rareBaseWeight + commonBaseWeight;
            float randomValue = Random.Range(0f, totalWeight);

            if (randomValue < legendaryBaseWeight)
                return CardRarity.Legendary;

            if (randomValue < legendaryBaseWeight + rareBaseWeight)
                return CardRarity.Rare;

            return CardRarity.Common;
        }

        /// <summary>
        /// Draws random card of specific rarity with tag filtering
        /// </summary>
        private SabotageCardData DrawCardWithTagLimit(CardRarity rarity, Dictionary<SabotageTag, int> currentTagCounts)
        {
            if (!cardsByRarity.ContainsKey(rarity) || cardsByRarity[rarity].Count == 0)
            {
                Debug.LogError($"[SabotagePool] No {rarity} cards available!");
                return null;
            }

            List<SabotageCardData> availableCards = cardsByRarity[rarity];

            // Filter out cards that would exceed tag limit
            List<SabotageCardData> validCards = availableCards.Where(card =>
            {
                int currentCount = currentTagCounts.ContainsKey(card.sabotageTag)
                    ? currentTagCounts[card.sabotageTag]
                    : 0;

                return currentCount < maxSameTagInDraft;
            }).ToList();

            if (validCards.Count == 0)
            {
                Debug.LogWarning($"[SabotagePool] No valid {rarity} cards after tag filtering! Allowing duplicate tag.");
                validCards = availableCards; // Fallback - allow duplicate
            }

            // Weighted random selection
            return GetWeightedRandomCard(validCards);
        }

        /// <summary>
        /// Selects random card using dropWeight
        /// </summary>
        private SabotageCardData GetWeightedRandomCard(List<SabotageCardData> cards)
        {
            if (cards.Count == 0) return null;
            if (cards.Count == 1) return cards[0];

            // Calculate total weight
            float totalWeight = cards.Sum(card => card.dropWeight);

            // Random selection
            float randomValue = Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;

            foreach (var card in cards)
            {
                cumulativeWeight += card.dropWeight;

                if (randomValue <= cumulativeWeight)
                {
                    return card;
                }
            }

            // Fallback (shouldn't happen)
            return cards[cards.Count - 1];
        }

        // ==========================================
        // UTILITY METHODS
        // ==========================================

        /// <summary>
        /// Gets total count of sabotage cards
        /// </summary>
        public int GetTotalCardCount()
        {
            return allSabotageCards.Count;
        }

        /// <summary>
        /// Gets count of cards by rarity
        /// </summary>
        public int GetCardCount(CardRarity rarity)
        {
            return cardsByRarity.ContainsKey(rarity) ? cardsByRarity[rarity].Count : 0;
        }

        /// <summary>
        /// Auto-loads sabotage cards from Resources folder (optional)
        /// Call this if you want to auto-populate pool
        /// </summary>
        [ContextMenu("Auto-Load Sabotage Cards from Resources")]
        public void AutoLoadFromResources()
        {
            SabotageCardData[] loadedCards = Resources.LoadAll<SabotageCardData>("Cards/Sabotages");

            if (loadedCards.Length == 0)
            {
                Debug.LogWarning("[SabotagePool] No sabotage cards found in Resources/Cards/Sabotages/");
                return;
            }

            allSabotageCards.Clear();
            allSabotageCards.AddRange(loadedCards);

            Debug.Log($"[SabotagePool] Auto-loaded {loadedCards.Length} sabotage cards from Resources");

            // Re-initialize
            isInitialized = false;
            InitializePool();
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Test Draw 3 Cards")]
        private void TestDraw()
        {
            if (!isInitialized) InitializePool();

            CardRarity[] testCombo = new CardRarity[]
            {
                CardRarity.Rare,
                CardRarity.Common,
                CardRarity.Common
            };

            SabotageCardData[] cards = DrawSabotageCards(testCombo);

            Debug.Log("=== TEST DRAW ===");
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null)
                {
                    Debug.Log($"  [{i}] {cards[i].sabotageName} ({cards[i].rarity}, {cards[i].sabotageTag})");
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}