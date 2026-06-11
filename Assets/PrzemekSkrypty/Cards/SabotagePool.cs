using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    public class SabotagePool : MonoBehaviourPunCallbacks
    {
        public static SabotagePool Instance { get; private set; }

        [Header("Sabotage Card Pool")]
        [SerializeField] private List<SabotageCardData> allSabotageCards = new List<SabotageCardData>();

        [Header("Drop Rate Settings")]
        [SerializeField] private float legendaryBaseWeight = 5f;
        [SerializeField] private float rareBaseWeight = 25f;
        [SerializeField] private float commonBaseWeight = 70f;

        [Header("Anti-Spam Settings")]
        [SerializeField] private int maxSameTagInDraft = 2;

        [Header("Debug")]
        [SerializeField] private bool logSelections = true;

        // Cache
        private Dictionary<CardRarity, List<SabotageCardData>> cardsByRarity;
        // ========== NOWE: Cache by name for RPC lookup ==========
        private Dictionary<string, SabotageCardData> cardsByName;
        // ========================================================
        private bool isInitialized = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializePool();
        }

        private void InitializePool()
        {
            if (isInitialized) return;

            if (allSabotageCards == null || allSabotageCards.Count == 0)
            {
                Debug.LogError("[SabotagePool] No sabotage cards assigned!");
                return;
            }

            allSabotageCards.RemoveAll(card => card == null);

            // Cache by rarity
            cardsByRarity = new Dictionary<CardRarity, List<SabotageCardData>>
            {
                { CardRarity.Common, new List<SabotageCardData>() },
                { CardRarity.Rare, new List<SabotageCardData>() },
                { CardRarity.Legendary, new List<SabotageCardData>() }
            };

            // ========== NOWE: Cache by name ==========
            cardsByName = new Dictionary<string, SabotageCardData>();
            // =========================================

            foreach (var card in allSabotageCards)
            {
                if (cardsByRarity.ContainsKey(card.rarity))
                {
                    cardsByRarity[card.rarity].Add(card);
                }

                // ========== NOWE: Add to name cache ==========
                if (!cardsByName.ContainsKey(card.name))
                {
                    cardsByName[card.name] = card;
                }
                else
                {
                    Debug.LogWarning($"[SabotagePool] Duplicate card name: {card.name}");
                }
                // =============================================
            }

            isInitialized = true;

            Debug.Log($"[SabotagePool] ✅ Initialized with {allSabotageCards.Count} cards:");
            Debug.Log($"  - Common: {cardsByRarity[CardRarity.Common].Count}");
            Debug.Log($"  - Rare: {cardsByRarity[CardRarity.Rare].Count}");
            Debug.Log($"  - Legendary: {cardsByRarity[CardRarity.Legendary].Count}");
        }

        // ==========================================
        // MAIN API
        // ==========================================

        public CardRarity[] GenerateRarityCombination()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[SabotagePool] Only Master Client can generate rarity combo!");
                return null;
            }

            CardRarity[] combination = new CardRarity[3];

            for (int i = 0; i < 3; i++)
            {
                combination[i] = GetRandomRarity();
            }

            if (logSelections)
            {
                Debug.Log($"[SabotagePool] Generated rarity combo: " +
                          $"[{string.Join(", ", combination)}]");
            }

            return combination;
        }

        /// <summary>
        /// Generates a rarity combination WITHOUT the master-client gate. Used by
        /// the reconnect catch-up path, where the rejoining (possibly non-master)
        /// player runs a draft for a wave the opponent already passed — so the
        /// master won't broadcast rarities. The opponent isn't viewing this draft,
        /// so local rarities are fine.
        /// </summary>
        public CardRarity[] GenerateRarityCombinationLocal()
        {
            if (!isInitialized) InitializePool();
            CardRarity[] combination = new CardRarity[3];
            for (int i = 0; i < 3; i++)
                combination[i] = GetRandomRarity();
            return combination;
        }

        public SabotageCardData[] DrawSabotageCards(CardRarity[] rarityCombination)
        {
            // ========== NOWE: Ensure initialized ==========
            if (!isInitialized)
            {
                InitializePool();
            }
            // ==============================================

            if (rarityCombination == null || rarityCombination.Length == 0)
            {
                Debug.LogError("[SabotagePool] Invalid rarity combination!");
                return null;
            }

            // ========== NOWE: Support variable length (not just 3) ==========
            SabotageCardData[] drawnCards = new SabotageCardData[rarityCombination.Length];
            // ================================================================

            Dictionary<SabotageTag, int> tagCounts = new Dictionary<SabotageTag, int>();

            for (int i = 0; i < rarityCombination.Length; i++)
            {
                CardRarity targetRarity = rarityCombination[i];

                SabotageCardData card = DrawCardWithTagLimit(targetRarity, tagCounts);

                if (card == null)
                {
                    Debug.LogWarning($"[SabotagePool] Failed to draw {targetRarity} card (slot {i}). " +
                                     $"Trying fallback...");

                    // ========== NOWE: Fallback - try any rarity ==========
                    card = DrawCardWithTagLimit(CardRarity.Common, tagCounts);
                    if (card == null)
                        card = DrawCardWithTagLimit(CardRarity.Rare, tagCounts);
                    if (card == null)
                        card = DrawCardWithTagLimit(CardRarity.Legendary, tagCounts);
                    // ====================================================

                    if (card == null)
                    {
                        Debug.LogError($"[SabotagePool] No cards available at all for slot {i}!");
                        continue;
                    }
                }

                drawnCards[i] = card;

                // Update tag count
                if (!tagCounts.ContainsKey(card.sabotageTag))
                    tagCounts[card.sabotageTag] = 0;

                tagCounts[card.sabotageTag]++;
            }

            if (logSelections)
            {
                Debug.Log($"[SabotagePool] Drew cards: " +
                          $"{string.Join(", ", drawnCards.Select(c => c?.sabotageName ?? "NULL"))}");
            }

            return drawnCards;
        }

        // ==========================================
        // NOWE: FIND BY NAME (for RPC lookup)
        // ==========================================

        /// <summary>
        /// Finds sabotage card by ScriptableObject name.
        /// Used by SabotageDraftManager when receiving RPC.
        /// </summary>
        public SabotageCardData FindByName(string cardName)
        {
            if (!isInitialized)
            {
                InitializePool();
            }

            if (string.IsNullOrEmpty(cardName))
            {
                Debug.LogError("[SabotagePool] FindByName called with null/empty name!");
                return null;
            }

            // Fast lookup from cache
            if (cardsByName != null && cardsByName.TryGetValue(cardName, out SabotageCardData cached))
            {
                return cached;
            }

            // Fallback: linear search (in case cache missed it)
            foreach (var card in allSabotageCards)
            {
                if (card != null && card.name == cardName)
                {
                    // Add to cache for next time
                    if (cardsByName != null)
                    {
                        cardsByName[cardName] = card;
                    }
                    return card;
                }
            }

            // Last resort: try sabotageName field
            foreach (var card in allSabotageCards)
            {
                if (card != null && card.sabotageName == cardName)
                {
                    Debug.LogWarning($"[SabotagePool] Found '{cardName}' by sabotageName, " +
                                     $"not SO name. Consider using card.name for RPC.");
                    return card;
                }
            }

            Debug.LogWarning($"[SabotagePool] Card '{cardName}' not found in pool!");
            return null;
        }

        /// <summary>
        /// Gets all cards (for debugging or UI listing)
        /// </summary>
        public List<SabotageCardData> GetAllCards()
        {
            return new List<SabotageCardData>(allSabotageCards);
        }

        // ==========================================
        // WEIGHTED RANDOM SELECTION
        // ==========================================

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

        private SabotageCardData DrawCardWithTagLimit(
            CardRarity rarity,
            Dictionary<SabotageTag, int> currentTagCounts)
        {
            if (!cardsByRarity.ContainsKey(rarity) || cardsByRarity[rarity].Count == 0)
            {
                Debug.LogWarning($"[SabotagePool] No {rarity} cards available!");
                return null;
            }

            List<SabotageCardData> availableCards = cardsByRarity[rarity];

            // Filter by tag limit
            List<SabotageCardData> validCards = availableCards.Where(card =>
            {
                int currentCount = currentTagCounts.ContainsKey(card.sabotageTag)
                    ? currentTagCounts[card.sabotageTag]
                    : 0;

                return currentCount < maxSameTagInDraft;
            }).ToList();

            if (validCards.Count == 0)
            {
                Debug.LogWarning($"[SabotagePool] No valid {rarity} cards after tag filtering. " +
                                 $"Allowing duplicate tag.");
                validCards = availableCards;
            }

            return GetWeightedRandomCard(validCards);
        }

        private SabotageCardData GetWeightedRandomCard(List<SabotageCardData> cards)
        {
            if (cards.Count == 0) return null;
            if (cards.Count == 1) return cards[0];

            float totalWeight = cards.Sum(card => card.dropWeight);

            // ========== NOWE: Safety check ==========
            if (totalWeight <= 0f)
            {
                Debug.LogWarning("[SabotagePool] Total weight is 0! Using uniform random.");
                return cards[Random.Range(0, cards.Count)];
            }
            // ========================================

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

            return cards[cards.Count - 1];
        }

        // ==========================================
        // UTILITY
        // ==========================================

        public int GetTotalCardCount() => allSabotageCards.Count;

        public int GetCardCount(CardRarity rarity)
        {
            return cardsByRarity != null && cardsByRarity.ContainsKey(rarity)
                ? cardsByRarity[rarity].Count
                : 0;
        }

        [ContextMenu("Auto-Load Sabotage Cards from Resources")]
        public void AutoLoadFromResources()
        {
            SabotageCardData[] loadedCards = Resources.LoadAll<SabotageCardData>("Cards/Sabotages");

            if (loadedCards.Length == 0)
            {
                Debug.LogWarning("[SabotagePool] No cards found in Resources/Cards/Sabotages/");
                return;
            }

            allSabotageCards.Clear();
            allSabotageCards.AddRange(loadedCards);

            Debug.Log($"[SabotagePool] Auto-loaded {loadedCards.Length} sabotage cards");

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
                    Debug.Log($"  [{i}] {cards[i].sabotageName} " +
                              $"({cards[i].rarity}, {cards[i].sabotageTag}) " +
                              $"SO.name={cards[i].name}");
                }
            }
        }

        [ContextMenu("Debug Pool State")]
        private void DebugPoolState()
        {
            Debug.Log($"[SabotagePool] isInitialized={isInitialized}");
            Debug.Log($"[SabotagePool] Total cards: {allSabotageCards.Count}");

            if (cardsByRarity != null)
            {
                foreach (var kvp in cardsByRarity)
                {
                    Debug.Log($"  {kvp.Key}: {kvp.Value.Count} cards");
                }
            }

            if (cardsByName != null)
            {
                Debug.Log($"[SabotagePool] Name cache: {cardsByName.Count} entries");
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