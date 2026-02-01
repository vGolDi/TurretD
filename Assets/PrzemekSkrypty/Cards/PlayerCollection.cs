//using UnityEngine;
//using System.Collections.Generic;
//using System.Linq;
//using System.IO;
//using ElementumDefense.Auth;

//namespace ElementumDefense.Cards
//{
//    public enum GameMode { Casual, Ranked, Custom }

//    public class PlayerCollection : MonoBehaviour
//    {
//        public static PlayerCollection Instance { get; private set; }

//        [Header("Collection Data")]
//        [SerializeField, Tooltip("All cards in the game (master list)")]
//        private List<CardData> allAvailableCards = new List<CardData>();

//        [SerializeField, Tooltip("Current unlocked cards (runtime)")]
//        private List<CardData> unlockedCards = new List<CardData>();

//        [Header("Currency")]
//        [SerializeField] private int currentGold = 0;
//        [SerializeField] private int currentCrystals = 0;

//        [Header("Starter Cards")]
//        [SerializeField, Tooltip("Auto-unlock these cards on first launch")]
//        private List<CardData> starterCards = new List<CardData>();

//        [Header("Save Settings")]
//        [SerializeField] private bool autoSaveOnChange = true;
//        [SerializeField] private string saveFileName = "PlayerCollection.json";

//        [Header("Progression")]
//        [SerializeField] private int currentLevel = 1;
//        [SerializeField] private int currentXP = 0;
//        // Wymagane XP na poziom: Level * 1000 (np. lvl 1 -> 1000, lvl 2 -> 2000)
//        private const int BASE_XP_REQ = 1000;

//        [Header("Ranked System")]
//        [SerializeField] private int currentELO = 1000; // Startowe ELO
//        public GameMode SelectedGameMode = GameMode.Casual;

//        // Events
//        public System.Action<int> OnLevelChanged;
//        public System.Action<int, int> OnXPChanged;
//        public System.Action<int> OnEloChanged;
//        public System.Action<CardData> OnCardUnlocked;
//        public System.Action<int> OnGoldChanged;
//        public System.Action<int> OnCrystalsChanged;
//        public System.Action OnCollectionLoaded;

//        // ==========================================
//        // INITIALIZATION
//        // ==========================================

//        private void Awake()
//        {
//            // Singleton setup
//            if (Instance != null && Instance != this)
//            {
//                Destroy(gameObject);
//                return;
//            }

//            Instance = this;
//            DontDestroyOnLoad(gameObject);
//            AutoLoadAllCards();
//            LoadCollection();
//        }

//        private void Start()
//        {
//            // Auto-load all cards from Resources if not assigned
//            if (allAvailableCards == null || allAvailableCards.Count == 0)
//            {
//                AutoLoadAllCards();
//            }

//            // First time setup - unlock starter cards
//            if (unlockedCards.Count == 0)
//            {
//                UnlockStarterCards();
//            }

//            Debug.Log($"[PlayerCollection] Initialized with {unlockedCards.Count}/{allAvailableCards.Count} unlocked cards");
//        }

//        /// <summary>
//        /// Auto-loads all cards from Resources/Cards/
//        /// </summary>
//        private void AutoLoadAllCards()
//        {
//            CardData[] loadedCards = Resources.LoadAll<CardData>("Cards");

//            if (loadedCards.Length == 0)
//            {
//                Debug.LogWarning("[PlayerCollection] No cards found in Resources/Cards/");
//                return;
//            }

//            allAvailableCards.Clear();
//            allAvailableCards.AddRange(loadedCards);

//            Debug.Log($"[PlayerCollection] Auto-loaded {loadedCards.Length} cards from Resources");
//        }

//        /// <summary>
//        /// Unlocks starter cards on first launch
//        /// </summary>
//        /// <summary>
//        /// Unlocks starter cards on first launch
//        /// </summary>
//        private void UnlockStarterCards()
//        {
//            int unlockedCount = 0;

//            // 1. SprawdŸ listê przypisan¹ w Inspektorze (jeœli istnieje)
//            if (starterCards != null && starterCards.Count > 0)
//            {
//                foreach (CardData card in starterCards)
//                {
//                    if (card != null && !IsUnlocked(card))
//                    {
//                        UnlockCard(card, silent: true);
//                        unlockedCount++;
//                    }
//                }
//            }

//            // 2. SprawdŸ wszystkie karty pod k¹tem flagi "isStarterCard" (To jest to, czego brakowa³o!)
//            if (allAvailableCards != null)
//            {
//                foreach (CardData card in allAvailableCards)
//                {
//                    // Jeœli karta ma zaznaczone "Is Starter Card" I nie jest jeszcze odblokowana
//                    if (card != null && card.isStarterCard && !IsUnlocked(card))
//                    {
//                        UnlockCard(card, silent: true);
//                        unlockedCount++;
//                    }
//                }
//            }

//            // 3. Wyœwietl logi dopiero na koñcu
//            if (unlockedCount > 0)
//            {
//                Debug.Log($"[PlayerCollection] Unlocked {unlockedCount} starter cards (Total owned: {unlockedCards.Count})");
//                SaveCollection();
//            }
//            else
//            {
//                // Ostrze¿enie tylko jeœli NIC nie znaleziono ani w liœcie, ani przez flagi
//                if (unlockedCards.Count == 0)
//                {
//                    Debug.LogWarning("[PlayerCollection] No starter cards found! Check Inspector list OR 'Is Starter Card' bools.");
//                }
//            }
//        }
//        // ==========================================
//        // RANKED SYSTEM
//        // ==========================================

//        public void AddElo(int amount)
//        {
//            currentELO += amount;
//            if (currentELO < 0) currentELO = 0; // Nie schodzimy poni¿ej 0

//            OnEloChanged?.Invoke(currentELO);
//            if (autoSaveOnChange) SaveCollection();

//            Debug.Log($"[Ranked] ELO changed by {amount}. New ELO: {currentELO}");
//        }

//        public int GetElo() => currentELO;

//        public string GetRankName()
//        {
//            if (currentELO < 1200) return "BRONZE";
//            if (currentELO < 1500) return "SILVER";
//            if (currentELO < 1800) return "GOLD";
//            if (currentELO < 2200) return "PLATINUM";
//            return "DIAMOND";
//        }

//        public Color GetRankColor()
//        {
//            if (currentELO < 1200) return new Color(0.8f, 0.5f, 0.2f); // Bronze
//            if (currentELO < 1500) return new Color(0.75f, 0.75f, 0.75f); // Silver
//            if (currentELO < 1800) return new Color(1f, 0.84f, 0f); // Gold
//            if (currentELO < 2200) return new Color(0f, 1f, 1f); // Platinum
//            return new Color(0.7f, 0.2f, 1f); // Diamond
//        }
//        // ==========================================
//        // UNLOCKING CARDS
//        // ==========================================

//        /// <summary>
//        /// Unlocks a card (adds to collection)
//        /// </summary>
//        /// <param name="card">Card to unlock</param>
//        /// <param name="silent">Skip event trigger (for batch unlocks)</param>
//        /// <returns>True if unlocked successfully</returns>
//        public bool UnlockCard(CardData card, bool silent = false)
//        {
//            if (card == null)
//            {
//                Debug.LogError("[PlayerCollection] Cannot unlock null card!");
//                return false;
//            }

//            if (IsUnlocked(card))
//            {
//                Debug.LogWarning($"[PlayerCollection] Card '{card.cardName}' already unlocked!");
//                return false;
//            }

//            unlockedCards.Add(card);

//            if (!silent)
//            {
//                OnCardUnlocked?.Invoke(card);
//                Debug.Log($"[PlayerCollection] Unlocked card: {card.cardName}");
//            }

//            if (autoSaveOnChange)
//            {
//                SaveCollection();
//            }

//            return true;
//        }

//        /// <summary>
//        /// Attempts to purchase and unlock a card with gold
//        /// </summary>
//        public bool PurchaseCard(CardData card)
//        {
//            if (card == null) return false;

//            if (IsUnlocked(card))
//            {
//                Debug.LogWarning($"[PlayerCollection] Already own '{card.cardName}'!");
//                return false;
//            }

//            if (currentGold < card.unlockCost)
//            {
//                Debug.Log($"[PlayerCollection] Not enough gold! Need {card.unlockCost}, have {currentGold}");
//                return false;
//            }

//            // Deduct cost
//            AddGold(-card.unlockCost);

//            // Unlock card
//            UnlockCard(card);

//            Debug.Log($"[PlayerCollection] Purchased '{card.cardName}' for {card.unlockCost} gold");

//            return true;
//        }

//        /// <summary>
//        /// Checks if card is unlocked
//        /// </summary>
//        public bool IsUnlocked(CardData card)
//        {
//            return unlockedCards.Contains(card);
//        }

//        /// <summary>
//        /// Gets all unlocked cards
//        /// </summary>
//        public List<CardData> GetUnlockedCards()
//        {
//            return new List<CardData>(unlockedCards);
//        }

//        /// <summary>
//        /// Gets all locked cards
//        /// </summary>
//        public List<CardData> GetLockedCards()
//        {
//            return allAvailableCards.Where(card => !IsUnlocked(card)).ToList();
//        }
//        /// <summary>
//        /// Gets ALL cards in the game (owned and unowned)
//        /// </summary>
//        public List<CardData> GetAllCards()
//        {
//            return new List<CardData>(allAvailableCards);
//        }

//        /// <summary>
//        /// Gets cards of specific rarity from ALL cards
//        /// </summary>
//        public List<CardData> GetAllCardsByRarity(CardRarity rarity)
//        {
//            return allAvailableCards.Where(card => card.rarity == rarity).ToList();
//        }
//        /// <summary>
//        /// Gets unlocked cards of specific type
//        /// </summary>
//        public List<CardData> GetUnlockedCardsByType(CardType cardType)
//        {
//            return unlockedCards.Where(card => card.cardType == cardType).ToList();
//        }

//        /// <summary>
//        /// Gets unlocked cards of specific rarity
//        /// </summary>
//        public List<CardData> GetUnlockedCardsByRarity(CardRarity rarity)
//        {
//            return unlockedCards.Where(card => card.rarity == rarity).ToList();
//        }

//        // ==========================================
//        // CURRENCY SYSTEM
//        // ==========================================

//        /// <summary>
//        /// Adds gold to player's balance
//        /// </summary>
//        public void AddGold(int amount)
//        {
//            currentGold += amount;
//            currentGold = Mathf.Max(0, currentGold); // Can't go negative

//            OnGoldChanged?.Invoke(currentGold);

//            if (autoSaveOnChange)
//            {
//                SaveCollection();
//            }
//        }

//        /// <summary>
//        /// Adds crystals to player's balance
//        /// </summary>
//        public void AddCrystals(int amount)
//        {
//            currentCrystals += amount;
//            currentCrystals = Mathf.Max(0, currentCrystals);

//            OnCrystalsChanged?.Invoke(currentCrystals);

//            if (autoSaveOnChange)
//            {
//                SaveCollection();
//            }
//        }

//        /// <summary>
//        /// Checks if player can afford cost
//        /// </summary>
//        public bool CanAffordGold(int cost)
//        {
//            return currentGold >= cost;
//        }

//        public bool CanAffordCrystals(int cost)
//        {
//            return currentCrystals >= cost;
//        }

//        public int GetGold() => currentGold;
//        public int GetCrystals() => currentCrystals;


//        // ==========================================
//        // PROGRESSION SYSTEM
//        // ==========================================

//        public void AddXP(int amount)
//        {
//            currentXP += amount;
//            int xpNeeded = GetXPForNextLevel();

//            // SprawdŸ czy level up (mo¿e byæ kilka na raz)
//            while (currentXP >= xpNeeded)
//            {
//                currentXP -= xpNeeded;
//                LevelUp();
//                xpNeeded = GetXPForNextLevel();
//            }

//            OnXPChanged?.Invoke(currentXP, xpNeeded);
//            if (autoSaveOnChange) SaveCollection();

//            Debug.Log($"[Progression] Gained {amount} XP. Current: {currentXP}/{xpNeeded}");
//        }

//        private void LevelUp()
//        {
//            currentLevel++;
//            OnLevelChanged?.Invoke(currentLevel);
//            Debug.Log($"[Progression] LEVEL UP! Now level {currentLevel}");

//            // NAGRODA ZA LEVEL UP: Np. Lootbox
//            // Tutaj mo¿esz dodaæ darmow¹ skrzynkê, z³oto lub kryszta³y
//            AddGold(500);
//            AddCrystals(5);

//            // Jeœli masz LootboxManager, mo¿esz wywo³aæ otwarcie (opcjonalne)
//            // LootboxManager.Instance.GiveFreeBox(); 
//        }

//        public int GetLevel() => currentLevel;
//        public int GetCurrentXP() => currentXP;
//        public int GetXPForNextLevel() => currentLevel * BASE_XP_REQ;
//        // ==========================================
//        // LOOTBOX SYSTEM
//        // ==========================================

//        /// <summary>
//        /// Opens a lootbox and unlocks random card
//        /// </summary>
//        /// <param name="guaranteedRarity">Minimum rarity (optional)</param>
//        /// <returns>Unlocked card (or null if all owned)</returns>
//        public CardData OpenLootbox(CardRarity? guaranteedRarity = null)
//        {
//            // Get locked cards that can drop from lootboxes
//            List<CardData> eligibleCards = allAvailableCards
//                .Where(card => !IsUnlocked(card) && card.canDropFromLootbox)
//                .ToList();

//            if (eligibleCards.Count == 0)
//            {
//                Debug.Log("[PlayerCollection] All cards unlocked! No more lootbox drops.");
//                return null;
//            }

//            // Filter by guaranteed rarity if specified
//            if (guaranteedRarity.HasValue)
//            {
//                List<CardData> filteredCards = eligibleCards
//                    .Where(card => card.rarity == guaranteedRarity.Value)
//                    .ToList();

//                if (filteredCards.Count > 0)
//                {
//                    eligibleCards = filteredCards;
//                }
//            }

//            // Weighted random selection
//            CardData droppedCard = GetWeightedRandomCard(eligibleCards);

//            if (droppedCard != null)
//            {
//                UnlockCard(droppedCard);
//                Debug.Log($"[PlayerCollection] Lootbox dropped: {droppedCard.cardName} ({droppedCard.rarity})");
//            }

//            return droppedCard;
//        }

//        /// <summary>
//        /// Weighted random card selection (based on rarity)
//        /// </summary>
//        private CardData GetWeightedRandomCard(List<CardData> cards)
//        {
//            if (cards.Count == 0) return null;
//            if (cards.Count == 1) return cards[0];

//            // Assign weights: Legendary = 5, Rare = 25, Common = 70
//            Dictionary<CardData, float> weights = new Dictionary<CardData, float>();

//            foreach (CardData card in cards)
//            {
//                float weight = card.rarity switch
//                {
//                    CardRarity.Legendary => 5f,
//                    CardRarity.Rare => 25f,
//                    CardRarity.Common => 70f,
//                    _ => 1f
//                };

//                weights[card] = weight;
//            }

//            // Random selection
//            float totalWeight = weights.Values.Sum();
//            float randomValue = Random.Range(0f, totalWeight);
//            float cumulativeWeight = 0f;

//            foreach (var kvp in weights)
//            {
//                cumulativeWeight += kvp.Value;

//                if (randomValue <= cumulativeWeight)
//                {
//                    return kvp.Key;
//                }
//            }

//            // Fallback
//            return cards[cards.Count - 1];
//        }

//        // ==========================================
//        // SAVE/LOAD SYSTEM
//        // ==========================================

//        /// <summary>
//        /// Saves collection to file (JSON)
//        /// </summary>
//        public void SaveCollection()
//        {
//            CollectionSaveData saveData = new CollectionSaveData
//            {
//                unlockedCardNames = unlockedCards.Select(card => card.name).ToList(),
//                currentGold = this.currentGold,
//                currentCrystals = this.currentCrystals,
//                // NOWE
//                currentLevel = this.currentLevel,
//                currentXP = this.currentXP,
//                currentELO = this.currentELO
//            };
//            string json = JsonUtility.ToJson(saveData, true); // Zamieñ obiekt na tekst
//            File.WriteAllText(GetSavePath(), json);
//        }

//        /// <summary>
//        /// Loads collection from file
//        /// </summary>
//        public void LoadCollection()
//        {
//            string path = GetSavePath();

//            if (!File.Exists(path))
//            {
//                Debug.Log("[PlayerCollection] No save file found - starting fresh");
//                return;
//            }

//            try
//            {
//                string json = File.ReadAllText(path);
//                CollectionSaveData saveData = JsonUtility.FromJson<CollectionSaveData>(json);

//                // Load currency
//                currentGold = saveData.currentGold;
//                currentCrystals = saveData.currentCrystals;

//                currentLevel = saveData.currentLevel > 0 ? saveData.currentLevel : 1;
//                currentXP = saveData.currentXP;
//                currentELO = saveData.currentELO != -1 ? saveData.currentELO : 1000;

//                // Load unlocked cards
//                unlockedCards.Clear();

//                foreach (string cardName in saveData.unlockedCardNames)
//                {
//                    CardData card = allAvailableCards.FirstOrDefault(c => c.name == cardName);

//                    if (card != null)
//                    {
//                        unlockedCards.Add(card);
//                    }
//                    else
//                    {
//                        Debug.LogWarning($"[PlayerCollection] Saved card '{cardName}' not found!");
//                    }
//                }

//                OnCollectionLoaded?.Invoke();

//                Debug.Log($"[PlayerCollection] Loaded: {unlockedCards.Count} cards, {currentGold} gold, {currentCrystals} crystals");
//            }
//            catch (System.Exception e)
//            {
//                Debug.LogError($"[PlayerCollection] Failed to load save file: {e.Message}");
//            }
//        }

//        /// <summary>
//        /// Resets collection (for testing)
//        /// </summary>
//        [ContextMenu("Reset Collection")]
//        public void ResetCollection()
//        {
//            unlockedCards.Clear();
//            currentGold = 0;
//            currentCrystals = 0;

//            UnlockStarterCards();

//            SaveCollection();

//            Debug.Log("[PlayerCollection] Collection reset!");
//        }

//        private string GetSavePath()
//        {
//            return Path.Combine(Application.persistentDataPath, saveFileName);
//        }

//        // ==========================================
//        // DEBUG
//        // ==========================================

//        [ContextMenu("Unlock All Cards")]
//        private void UnlockAllCards()
//        {
//            foreach (CardData card in allAvailableCards)
//            {
//                if (card != null && !IsUnlocked(card))
//                {
//                    UnlockCard(card, silent: true);
//                }
//            }

//            SaveCollection();
//            Debug.Log("[PlayerCollection] Unlocked all cards!");
//        }

//        [ContextMenu("Add 1000 Gold")]
//        private void AddTestGold()
//        {
//            AddGold(1000);
//        }

//        [ContextMenu("Add 100 Crystals")]
//        private void AddTestCrystals()
//        {
//            AddCrystals(100);
//        }

//        [ContextMenu("Test Open Lootbox")]
//        private void TestLootbox()
//        {
//            CardData dropped = OpenLootbox();

//            if (dropped != null)
//            {
//                Debug.Log($"[TEST] Lootbox dropped: {dropped.cardName}");
//            }
//        }
//        [ContextMenu("Force Re-Check Starter Cards")]
//        public void ForceCheckStarterCards()
//        {
//            // Upewnij siê, ¿e mamy za³adowane wszystkie karty
//            if (allAvailableCards == null || allAvailableCards.Count == 0)
//            {
//                AutoLoadAllCards();
//            }

//            int addedCount = 0;
//            foreach (CardData card in allAvailableCards)
//            {
//                // Jeœli karta jest oznaczona jako startowa I jej nie mamy
//                if (card != null && card.isStarterCard && !IsUnlocked(card))
//                {
//                    UnlockCard(card, silent: true);
//                    addedCount++;
//                }
//            }

//            Debug.Log($"[PlayerCollection] Manually added {addedCount} missing starter cards.");
//            SaveCollection();
//        }
//        [ContextMenu("Reset Currency Only")]
//        public void ResetCurrency()
//        {
//            currentGold = 0;
//            currentCrystals = 0;

//            // Odœwie¿amy UI
//            OnGoldChanged?.Invoke(currentGold);
//            OnCrystalsChanged?.Invoke(currentCrystals);

//            SaveCollection();
//            Debug.Log("[Debug] Gold and Crystals reset to 0.");
//        }

//        [ContextMenu("Add 100 ELO")]
//        public void DebugAddElo()
//        {
//            AddElo(100); // Twoja funkcja ju¿ obs³uguje logi i zapis
//        }

//        [ContextMenu("Remove 100 ELO")]
//        public void DebugRemoveElo()
//        {
//            AddElo(-100); // Twoja funkcja AddElo przyjmuje int, wiêc minus zadzia³a poprawnie
//        }

//        [ContextMenu("Add 1 Level (Test XP)")]
//        public void DebugLevelUp()
//        {
//            // Dodajemy dok³adnie tyle XP, ile brakuje do nastêpnego poziomu
//            int xpNeeded = GetXPForNextLevel() - currentXP;
//            AddXP(xpNeeded);
//        }
//        private void OnDestroy()
//        {
//            if (Instance == this)
//            {
//                Instance = null;
//            }
//        }
//    }

//    // ==========================================
//    // SAVE DATA STRUCTURE
//    // ==========================================

//    [System.Serializable]
//    public class CollectionSaveData
//    {
//        public List<string> unlockedCardNames; // ScriptableObject.name
//        public int currentGold;
//        public int currentCrystals;

//        public int currentLevel = 1;
//        public int currentXP = 0;
//        public int currentELO = -1;
//    }
//}

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ElementumDefense.Auth;
using ElementumDefense.Elements;

namespace ElementumDefense.Cards
{
    public enum GameMode { Casual, Ranked, Custom }

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

        [Header("Progression")]
        [SerializeField] private int currentLevel = 1;
        [SerializeField] private int currentXP = 0;
        // Wymagane XP na poziom: Level * 1000 (np. lvl 1 -> 1000, lvl 2 -> 2000)
        private const int BASE_XP_REQ = 1000;

        [Header("Ranked System")]
        [SerializeField] private int currentELO = 1000; // Startowe ELO
        public GameMode SelectedGameMode = GameMode.Casual;

        [Header("Default Decks")]
        [SerializeField, Tooltip("Decks given to every new player")]
        private List<DeckData> defaultDecks = new List<DeckData>(); // Przypisz tutaj swoje bazowe talie w Inspektorze!

        // Runtime cache talii gracza
        private List<DeckData> playerDecks = new List<DeckData>();

        // Events
        public System.Action<int> OnLevelChanged;
        public System.Action<int, int> OnXPChanged;
        public System.Action<int> OnEloChanged;
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

            // Wstêpne ³adowanie definicji kart (to jest bezpieczne, bo to tylko assety)
            AutoLoadAllCards();
        }

        private void Start()
        {
            // CZEKAMY NA LOGOWANIE!
            // Jeœli AuthManager ju¿ istnieje, podpinamy siê pod event
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnLoginSuccess += OnUserLoggedIn;
            }
            else
            {
                Debug.LogError("[PlayerCollection] AuthManager missing in scene!");
            }
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
        /// <summary>
        /// Unlocks starter cards on first launch
        /// </summary>
        private void UnlockStarterCards()
        {
            int unlockedCount = 0;

            // 1. SprawdŸ listê przypisan¹ w Inspektorze (jeœli istnieje)
            if (starterCards != null && starterCards.Count > 0)
            {
                foreach (CardData card in starterCards)
                {
                    if (card != null && !IsUnlocked(card))
                    {
                        UnlockCard(card, silent: true);
                        unlockedCount++;
                    }
                }
            }

            // 2. SprawdŸ wszystkie karty pod k¹tem flagi "isStarterCard" (To jest to, czego brakowa³o!)
            if (allAvailableCards != null)
            {
                foreach (CardData card in allAvailableCards)
                {
                    // Jeœli karta ma zaznaczone "Is Starter Card" I nie jest jeszcze odblokowana
                    if (card != null && card.isStarterCard && !IsUnlocked(card))
                    {
                        UnlockCard(card, silent: true);
                        unlockedCount++;
                    }
                }
            }

            // 3. Wyœwietl logi dopiero na koñcu
            if (unlockedCount > 0)
            {
                Debug.Log($"[PlayerCollection] Unlocked {unlockedCount} starter cards (Total owned: {unlockedCards.Count})");
                SaveCollection();
            }
            else
            {
                // Ostrze¿enie tylko jeœli NIC nie znaleziono ani w liœcie, ani przez flagi
                if (unlockedCards.Count == 0)
                {
                    Debug.LogWarning("[PlayerCollection] No starter cards found! Check Inspector list OR 'Is Starter Card' bools.");
                }
            }
        }
        // ==========================================
        // RANKED SYSTEM
        // ==========================================

        public void AddElo(int amount)
        {
            currentELO += amount;
            if (currentELO < 0) currentELO = 0; // Nie schodzimy poni¿ej 0

            OnEloChanged?.Invoke(currentELO);
            if (autoSaveOnChange) SaveCollection();

            Debug.Log($"[Ranked] ELO changed by {amount}. New ELO: {currentELO}");
        }

        public int GetElo() => currentELO;

        public string GetRankName()
        {
            if (currentELO < 1200) return "BRONZE";
            if (currentELO < 1500) return "SILVER";
            if (currentELO < 1800) return "GOLD";
            if (currentELO < 2200) return "PLATINUM";
            return "DIAMOND";
        }

        public Color GetRankColor()
        {
            if (currentELO < 1200) return new Color(0.8f, 0.5f, 0.2f); // Bronze
            if (currentELO < 1500) return new Color(0.75f, 0.75f, 0.75f); // Silver
            if (currentELO < 1800) return new Color(1f, 0.84f, 0f); // Gold
            if (currentELO < 2200) return new Color(0f, 1f, 1f); // Platinum
            return new Color(0.7f, 0.2f, 1f); // Diamond
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
        /// Gets ALL cards in the game (owned and unowned)
        /// </summary>
        public List<CardData> GetAllCards()
        {
            return new List<CardData>(allAvailableCards);
        }

        /// <summary>
        /// Gets cards of specific rarity from ALL cards
        /// </summary>
        public List<CardData> GetAllCardsByRarity(CardRarity rarity)
        {
            return allAvailableCards.Where(card => card.rarity == rarity).ToList();
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
        // PROGRESSION SYSTEM
        // ==========================================

        public void AddXP(int amount)
        {
            currentXP += amount;
            int xpNeeded = GetXPForNextLevel();

            // SprawdŸ czy level up (mo¿e byæ kilka na raz)
            while (currentXP >= xpNeeded)
            {
                currentXP -= xpNeeded;
                LevelUp();
                xpNeeded = GetXPForNextLevel();
            }

            OnXPChanged?.Invoke(currentXP, xpNeeded);
            if (autoSaveOnChange) SaveCollection();

            Debug.Log($"[Progression] Gained {amount} XP. Current: {currentXP}/{xpNeeded}");
        }

        private void LevelUp()
        {
            currentLevel++;
            OnLevelChanged?.Invoke(currentLevel);
            Debug.Log($"[Progression] LEVEL UP! Now level {currentLevel}");

            // NAGRODA ZA LEVEL UP: Np. Lootbox
            // Tutaj mo¿esz dodaæ darmow¹ skrzynkê, z³oto lub kryszta³y
            AddGold(500);
            AddCrystals(5);

            // Jeœli masz LootboxManager, mo¿esz wywo³aæ otwarcie (opcjonalne)
            // LootboxManager.Instance.GiveFreeBox(); 
        }

        public int GetLevel() => currentLevel;
        public int GetCurrentXP() => currentXP;
        public int GetXPForNextLevel() => currentLevel * BASE_XP_REQ;
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
        // DECK MANAGEMENT (NOWE)
        // ==========================================

        public List<DeckData> GetPlayerDecks()
        {
            return new List<DeckData>(playerDecks);
        }

        public void SaveUserDeck(DeckData deck)
        {
            // 1. SprawdŸ czy to nowa talia czy aktualizacja istniej¹cej
            DeckData existing = playerDecks.FirstOrDefault(d => d.deckName == deck.deckName);

            if (existing != null)
            {
                // Aktualizuj istniej¹c¹ (kopiuj karty)
                existing.cards = new List<CardData>(deck.cards);
                existing.preferredArena = deck.preferredArena;
            }
            else
            {
                // Dodaj now¹ (musimy stworzyæ osobn¹ instancjê, ¿eby nie nadpisywaæ edytora)
                DeckData newDeck = Instantiate(deck);
                newDeck.name = deck.deckName; // Wa¿ne dla nazwy instancji
                playerDecks.Add(newDeck);
            }

            Debug.Log($"[PlayerCollection] Saved deck: {deck.deckName}");
            SaveCollection(); // Zapisz wszystko do JSON
        }

        public void DeleteUserDeck(DeckData deck)
        {
            if (playerDecks.Contains(deck))
            {
                playerDecks.Remove(deck);
                SaveCollection();
            }
        }
        // ==========================================
        // SAVE/LOAD SYSTEM
        // ==========================================

        public void SaveCollection()
        {
            // Konwersja runtime DeckData -> Serializable SavedDeck
            List<SavedDeck> serializedDecks = new List<SavedDeck>();
            foreach (var deck in playerDecks)
            {
                SavedDeck sd = new SavedDeck
                {
                    deckName = deck.deckName,
                    preferredArena = deck.preferredArena,
                    cardNames = deck.cards.Select(c => c != null ? c.name : "").ToList()
                };
                serializedDecks.Add(sd);
            }

            CollectionSaveData saveData = new CollectionSaveData
            {
                unlockedCardNames = unlockedCards.Select(card => card.name).ToList(),
                currentGold = this.currentGold,
                currentCrystals = this.currentCrystals,
                currentLevel = this.currentLevel,
                currentXP = this.currentXP,
                currentELO = this.currentELO,
                savedDecks = serializedDecks // ZAPISUJEMY TALIE
            };

            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(GetSavePath(), json);
        }

        public void LoadCollection()
        {
            string path = GetSavePath();
            playerDecks.Clear(); // Wyczyœæ stare talie przed ³adowaniem

            if (!File.Exists(path))
            {
                Debug.Log("[PlayerCollection] New user - initializing defaults.");
                // Jeœli nie ma zapisu, daj talie startowe
                AssignDefaultDecks();
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                CollectionSaveData saveData = JsonUtility.FromJson<CollectionSaveData>(json);

                // ... (£adowanie waluty i kart bez zmian) ...
                currentGold = saveData.currentGold;
                currentCrystals = saveData.currentCrystals;
                currentLevel = saveData.currentLevel > 0 ? saveData.currentLevel : 1;
                currentXP = saveData.currentXP;
                currentELO = saveData.currentELO != -1 ? saveData.currentELO : 1000;

                unlockedCards.Clear();
                foreach (string cardName in saveData.unlockedCardNames)
                {
                    CardData card = allAvailableCards.FirstOrDefault(c => c.name == cardName);
                    if (card != null) unlockedCards.Add(card);
                }

                // £ADOWANIE TALII
                if (saveData.savedDecks != null && saveData.savedDecks.Count > 0)
                {
                    foreach (SavedDeck sd in saveData.savedDecks)
                    {
                        // Odtwórz ScriptableObject w pamiêci
                        DeckData runtimeDeck = ScriptableObject.CreateInstance<DeckData>();
                        runtimeDeck.deckName = sd.deckName;
                        runtimeDeck.name = sd.deckName; // Nazwa assetu w pamiêci
                        runtimeDeck.preferredArena = sd.preferredArena;

                        foreach (string cName in sd.cardNames)
                        {
                            CardData card = allAvailableCards.FirstOrDefault(c => c.name == cName);
                            if (card != null) runtimeDeck.cards.Add(card);
                        }
                        playerDecks.Add(runtimeDeck);
                    }
                }
                else
                {
                    // Jeœli gracz ma zapis, ale 0 talii (np. stare konto) -> daj mu startowe
                    AssignDefaultDecks();
                }

                OnCollectionLoaded?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerCollection] Load Error: {e.Message}");
                AssignDefaultDecks(); // Fallback
            }
        }

        private void AssignDefaultDecks()
        {
            if (defaultDecks == null) return;

            foreach (DeckData defDeck in defaultDecks)
            {
                if (defDeck == null) continue;

                // Klonujemy taliê startow¹ do pamiêci gracza
                DeckData newDeck = Instantiate(defDeck);
                newDeck.name = defDeck.name; // Zachowaj nazwê
                newDeck.deckName = defDeck.deckName;

                playerDecks.Add(newDeck);
            }
            Debug.Log($"[PlayerCollection] Assigned {playerDecks.Count} default decks.");
            // Nie zapisujemy od razu, zapisze siê przy pierwszej zmianie waluty/XP/talii
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
        private void OnUserLoggedIn(string username)
        {
            Debug.Log($"[PlayerCollection] User {username} logged in. Loading distinct save file...");

            // Resetujemy stan (wa¿ne przy przelogowywaniu!)
            currentGold = 0;
            currentCrystals = 0;
            currentLevel = 1;
            currentXP = 0;
            currentELO = 1000;
            unlockedCards.Clear();

            // £adujemy plik konkretnego gracza
            LoadCollection();

            // Jeœli to nowe konto (brak unlockedCards po load), daj starter pack
            if (unlockedCards.Count == 0)
            {
                UnlockStarterCards();
            }
        }
        private string GetSavePath()
        {
            string username = "Guest";

            if (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn)
            {
                username = AuthManager.Instance.CurrentUsername;
            }

            // Plik bêdzie siê nazywa³ np. "Save_GolDi.json"
            return Path.Combine(Application.persistentDataPath, $"Save_{username}.json");
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
        [ContextMenu("Force Re-Check Starter Cards")]
        public void ForceCheckStarterCards()
        {
            // Upewnij siê, ¿e mamy za³adowane wszystkie karty
            if (allAvailableCards == null || allAvailableCards.Count == 0)
            {
                AutoLoadAllCards();
            }

            int addedCount = 0;
            foreach (CardData card in allAvailableCards)
            {
                // Jeœli karta jest oznaczona jako startowa I jej nie mamy
                if (card != null && card.isStarterCard && !IsUnlocked(card))
                {
                    UnlockCard(card, silent: true);
                    addedCount++;
                }
            }

            Debug.Log($"[PlayerCollection] Manually added {addedCount} missing starter cards.");
            SaveCollection();
        }
        [ContextMenu("Reset Currency Only")]
        public void ResetCurrency()
        {
            currentGold = 0;
            currentCrystals = 0;

            // Odœwie¿amy UI
            OnGoldChanged?.Invoke(currentGold);
            OnCrystalsChanged?.Invoke(currentCrystals);

            SaveCollection();
            Debug.Log("[Debug] Gold and Crystals reset to 0.");
        }

        [ContextMenu("Add 100 ELO")]
        public void DebugAddElo()
        {
            AddElo(100); // Twoja funkcja ju¿ obs³uguje logi i zapis
        }

        [ContextMenu("Remove 100 ELO")]
        public void DebugRemoveElo()
        {
            AddElo(-100); // Twoja funkcja AddElo przyjmuje int, wiêc minus zadzia³a poprawnie
        }

        [ContextMenu("Add 1 Level (Test XP)")]
        public void DebugLevelUp()
        {
            // Dodajemy dok³adnie tyle XP, ile brakuje do nastêpnego poziomu
            int xpNeeded = GetXPForNextLevel() - currentXP;
            AddXP(xpNeeded);
        }
        private void OnDestroy()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnLoginSuccess -= OnUserLoggedIn;
            }

            if (Instance == this) Instance = null;
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

        public int currentLevel = 1;
        public int currentXP = 0;
        public int currentELO = -1;

        public List<SavedDeck> savedDecks = new List<SavedDeck>();
    }

    [System.Serializable]
    public class SavedDeck
    {
        public string deckName;
        public ElementType preferredArena;
        public List<string> cardNames = new List<string>(); // Lista nazw ScriptableObjectów
    }
}

