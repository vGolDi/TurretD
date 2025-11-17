using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Manages player's card collection (unlocked/locked cards)
    /// Handles saving/loading, currency, and unlocking mechanics
    /// Singleton - persists between scenes
    /// </summary>
    public class PlayerCollection : MonoBehaviour
    {
        public static PlayerCollection Instance { get; private set; }

        [Header("Collection Data")]
        [SerializeField, Tooltip("All cards in the game (master list)")]
        private List<CardData> allAvailableCards = new List<CardData>();

        [SerializeField, Tooltip("Current unlocked cards (runtime)")]
        private List<CardData> unlockedCards = new List<CardData>();

        [Header("Currency")]
        [SerializeField] private int currentGold = 0;
        [SerializeField] private int currentCrystals = 0;

        [Header("Starter Cards")]
        [SerializeField, Tooltip("Auto-unlock these cards on first launch")]
        private List<CardData> starterCards = new List<CardData>();

        [Header("Save Settings")]
        [SerializeField] private bool autoSaveOnChange = true;
        [SerializeField] private string saveFileName = "PlayerCollection.json";

        // Events
        public System.Action<CardData> OnCardUnlocked;
        public System.Action<int> OnGoldChanged;
        public System.Action<int> OnCrystalsChanged;
        public System.Action OnCollectionLoaded;

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
            DontDestroyOnLoad(gameObject);
            AutoLoadAllCards();
            LoadCollection();
        }

        private void Start()
        {
            // Auto-load all cards from Resources if not assigned
            if (allAvailableCards == null || allAvailableCards.Count == 0)
            {
                AutoLoadAllCards();
            }

            // First time setup - unlock starter cards
            if (unlockedCards.Count == 0)
            {
                UnlockStarterCards();
            }

            Debug.Log($"[PlayerCollection] Initialized with {unlockedCards.Count}/{allAvailableCards.Count} unlocked cards");
        }

        /// <summary>
        /// Auto-loads all cards from Resources/Cards/
        /// </summary>
        private void AutoLoadAllCards()
        {
            CardData[] loadedCards = Resources.LoadAll<CardData>("Cards");

            if (loadedCards.Length == 0)
            {
                Debug.LogWarning("[PlayerCollection] No cards found in Resources/Cards/");
                return;
            }

            allAvailableCards.Clear();
            allAvailableCards.AddRange(loadedCards);

            Debug.Log($"[PlayerCollection] Auto-loaded {loadedCards.Length} cards from Resources");
        }

        /// <summary>
        /// Unlocks starter cards on first launch
        /// </summary>
        private void UnlockStarterCards()
        {
            if (starterCards == null || starterCards.Count == 0)
            {
                Debug.LogWarning("[PlayerCollection] No starter cards assigned!");
                return;
            }

            foreach (CardData card in starterCards)
            {
                if (card != null && !IsUnlocked(card))
                {
                    UnlockCard(card, silent: true);
                }
            }

            // Also unlock cards marked as "isStarterCard"
            foreach (CardData card in allAvailableCards)
            {
                if (card != null && card.isStarterCard && !IsUnlocked(card))
                {
                    UnlockCard(card, silent: true);
                }
            }

            Debug.Log($"[PlayerCollection] Unlocked {unlockedCards.Count} starter cards");

            SaveCollection();
        }

        // ==========================================
        // UNLOCKING CARDS
        // ==========================================

        /// <summary>
        /// Unlocks a card (adds to collection)
        /// </summary>
        /// <param name="card">Card to unlock</param>
        /// <param name="silent">Skip event trigger (for batch unlocks)</param>
        /// <returns>True if unlocked successfully</returns>
        public bool UnlockCard(CardData card, bool silent = false)
        {
            if (card == null)
            {
                Debug.LogError("[PlayerCollection] Cannot unlock null card!");
                return false;
            }

            if (IsUnlocked(card))
            {
                Debug.LogWarning($"[PlayerCollection] Card '{card.cardName}' already unlocked!");
                return false;
            }

            unlockedCards.Add(card);

            if (!silent)
            {
                OnCardUnlocked?.Invoke(card);
                Debug.Log($"[PlayerCollection] Unlocked card: {card.cardName}");
            }

            if (autoSaveOnChange)
            {
                SaveCollection();
            }

            return true;
        }

        /// <summary>
        /// Attempts to purchase and unlock a card with gold
        /// </summary>
        public bool PurchaseCard(CardData card)
        {
            if (card == null) return false;

            if (IsUnlocked(card))
            {
                Debug.LogWarning($"[PlayerCollection] Already own '{card.cardName}'!");
                return false;
            }

            if (currentGold < card.unlockCost)
            {
                Debug.Log($"[PlayerCollection] Not enough gold! Need {card.unlockCost}, have {currentGold}");
                return false;
            }

            // Deduct cost
            AddGold(-card.unlockCost);

            // Unlock card
            UnlockCard(card);

            Debug.Log($"[PlayerCollection] Purchased '{card.cardName}' for {card.unlockCost} gold");

            return true;
        }

        /// <summary>
        /// Checks if card is unlocked
        /// </summary>
        public bool IsUnlocked(CardData card)
        {
            return unlockedCards.Contains(card);
        }

        /// <summary>
        /// Gets all unlocked cards
        /// </summary>
        public List<CardData> GetUnlockedCards()
        {
            return new List<CardData>(unlockedCards);
        }

        /// <summary>
        /// Gets all locked cards
        /// </summary>
        public List<CardData> GetLockedCards()
        {
            return allAvailableCards.Where(card => !IsUnlocked(card)).ToList();
        }

        /// <summary>
        /// Gets unlocked cards of specific type
        /// </summary>
        public List<CardData> GetUnlockedCardsByType(CardType cardType)
        {
            return unlockedCards.Where(card => card.cardType == cardType).ToList();
        }

        /// <summary>
        /// Gets unlocked cards of specific rarity
        /// </summary>
        public List<CardData> GetUnlockedCardsByRarity(CardRarity rarity)
        {
            return unlockedCards.Where(card => card.rarity == rarity).ToList();
        }

        // ==========================================
        // CURRENCY SYSTEM
        // ==========================================

        /// <summary>
        /// Adds gold to player's balance
        /// </summary>
        public void AddGold(int amount)
        {
            currentGold += amount;
            currentGold = Mathf.Max(0, currentGold); // Can't go negative

            OnGoldChanged?.Invoke(currentGold);

            if (autoSaveOnChange)
            {
                SaveCollection();
            }
        }

        /// <summary>
        /// Adds crystals to player's balance
        /// </summary>
        public void AddCrystals(int amount)
        {
            currentCrystals += amount;
            currentCrystals = Mathf.Max(0, currentCrystals);

            OnCrystalsChanged?.Invoke(currentCrystals);

            if (autoSaveOnChange)
            {
                SaveCollection();
            }
        }

        /// <summary>
        /// Checks if player can afford cost
        /// </summary>
        public bool CanAffordGold(int cost)
        {
            return currentGold >= cost;
        }

        public bool CanAffordCrystals(int cost)
        {
            return currentCrystals >= cost;
        }

        public int GetGold() => currentGold;
        public int GetCrystals() => currentCrystals;

        // ==========================================
        // LOOTBOX SYSTEM
        // ==========================================

        /// <summary>
        /// Opens a lootbox and unlocks random card
        /// </summary>
        /// <param name="guaranteedRarity">Minimum rarity (optional)</param>
        /// <returns>Unlocked card (or null if all owned)</returns>
        public CardData OpenLootbox(CardRarity? guaranteedRarity = null)
        {
            // Get locked cards that can drop from lootboxes
            List<CardData> eligibleCards = allAvailableCards
                .Where(card => !IsUnlocked(card) && card.canDropFromLootbox)
                .ToList();

            if (eligibleCards.Count == 0)
            {
                Debug.Log("[PlayerCollection] All cards unlocked! No more lootbox drops.");
                return null;
            }

            // Filter by guaranteed rarity if specified
            if (guaranteedRarity.HasValue)
            {
                List<CardData> filteredCards = eligibleCards
                    .Where(card => card.rarity == guaranteedRarity.Value)
                    .ToList();

                if (filteredCards.Count > 0)
                {
                    eligibleCards = filteredCards;
                }
            }

            // Weighted random selection
            CardData droppedCard = GetWeightedRandomCard(eligibleCards);

            if (droppedCard != null)
            {
                UnlockCard(droppedCard);
                Debug.Log($"[PlayerCollection] Lootbox dropped: {droppedCard.cardName} ({droppedCard.rarity})");
            }

            return droppedCard;
        }

        /// <summary>
        /// Weighted random card selection (based on rarity)
        /// </summary>
        private CardData GetWeightedRandomCard(List<CardData> cards)
        {
            if (cards.Count == 0) return null;
            if (cards.Count == 1) return cards[0];

            // Assign weights: Legendary = 5, Rare = 25, Common = 70
            Dictionary<CardData, float> weights = new Dictionary<CardData, float>();

            foreach (CardData card in cards)
            {
                float weight = card.rarity switch
                {
                    CardRarity.Legendary => 5f,
                    CardRarity.Rare => 25f,
                    CardRarity.Common => 70f,
                    _ => 1f
                };

                weights[card] = weight;
            }

            // Random selection
            float totalWeight = weights.Values.Sum();
            float randomValue = Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;

            foreach (var kvp in weights)
            {
                cumulativeWeight += kvp.Value;

                if (randomValue <= cumulativeWeight)
                {
                    return kvp.Key;
                }
            }

            // Fallback
            return cards[cards.Count - 1];
        }

        // ==========================================
        // SAVE/LOAD SYSTEM
        // ==========================================

        /// <summary>
        /// Saves collection to file (JSON)
        /// </summary>
        public void SaveCollection()
        {
            CollectionSaveData saveData = new CollectionSaveData
            {
                unlockedCardNames = unlockedCards.Select(card => card.name).ToList(),
                currentGold = this.currentGold,
                currentCrystals = this.currentCrystals
            };

            string json = JsonUtility.ToJson(saveData, true);
            string path = GetSavePath();

            File.WriteAllText(path, json);

            Debug.Log($"[PlayerCollection] Saved to: {path}");
        }

        /// <summary>
        /// Loads collection from file
        /// </summary>
        public void LoadCollection()
        {
            string path = GetSavePath();

            if (!File.Exists(path))
            {
                Debug.Log("[PlayerCollection] No save file found - starting fresh");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                CollectionSaveData saveData = JsonUtility.FromJson<CollectionSaveData>(json);

                // Load currency
                currentGold = saveData.currentGold;
                currentCrystals = saveData.currentCrystals;

                // Load unlocked cards
                unlockedCards.Clear();

                foreach (string cardName in saveData.unlockedCardNames)
                {
                    CardData card = allAvailableCards.FirstOrDefault(c => c.name == cardName);

                    if (card != null)
                    {
                        unlockedCards.Add(card);
                    }
                    else
                    {
                        Debug.LogWarning($"[PlayerCollection] Saved card '{cardName}' not found!");
                    }
                }

                OnCollectionLoaded?.Invoke();

                Debug.Log($"[PlayerCollection] Loaded: {unlockedCards.Count} cards, {currentGold} gold, {currentCrystals} crystals");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerCollection] Failed to load save file: {e.Message}");
            }
        }

        /// <summary>
        /// Resets collection (for testing)
        /// </summary>
        [ContextMenu("Reset Collection")]
        public void ResetCollection()
        {
            unlockedCards.Clear();
            currentGold = 0;
            currentCrystals = 0;

            UnlockStarterCards();

            SaveCollection();

            Debug.Log("[PlayerCollection] Collection reset!");
        }

        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, saveFileName);
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Unlock All Cards")]
        private void UnlockAllCards()
        {
            foreach (CardData card in allAvailableCards)
            {
                if (card != null && !IsUnlocked(card))
                {
                    UnlockCard(card, silent: true);
                }
            }

            SaveCollection();
            Debug.Log("[PlayerCollection] Unlocked all cards!");
        }

        [ContextMenu("Add 1000 Gold")]
        private void AddTestGold()
        {
            AddGold(1000);
        }

        [ContextMenu("Add 100 Crystals")]
        private void AddTestCrystals()
        {
            AddCrystals(100);
        }

        [ContextMenu("Test Open Lootbox")]
        private void TestLootbox()
        {
            CardData dropped = OpenLootbox();

            if (dropped != null)
            {
                Debug.Log($"[TEST] Lootbox dropped: {dropped.cardName}");
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

    // ==========================================
    // SAVE DATA STRUCTURE
    // ==========================================

    [System.Serializable]
    public class CollectionSaveData
    {
        public List<string> unlockedCardNames; // ScriptableObject.name
        public int currentGold;
        public int currentCrystals;
    }
}