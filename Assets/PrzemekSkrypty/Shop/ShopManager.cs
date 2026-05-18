// Assets/PrzemekSkrypty/Shop/ShopManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ElementumDefense.Auth;
using ElementumDefense.Skins;
using ElementumDefense.Cards;
using ElementumDefense.Lootbox;
using ElementumDefense.BattlePass;

namespace ElementumDefense.Shop
{
    /// <summary>
    /// Core shop system manager.
    /// Handles purchasing, daily/weekly limits, per-user tracking.
    /// Singleton - lives on the same GameObject as PlayerCollection or its own.
    /// </summary>
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }

        [Header("Shop Catalog")]
        [SerializeField, Tooltip("Manually assigned shop items (optional - auto-loads from Resources/Shop/)")]
        private List<ShopItemData> shopItems = new List<ShopItemData>();

        [Header("Settings")]
        [SerializeField] private bool autoLoadFromResources = true;
        [SerializeField] private bool logPurchases = true;

        // Runtime purchase tracking (per user)
        private ShopSaveData purchaseData;

        // ==========================================
        // EVENTS
        // ==========================================

        /// <summary>Fired after successful purchase. Passes the purchased item.</summary>
        public event Action<ShopItemData> OnPurchaseSuccess;

        /// <summary>Fired when purchase fails. Passes item and reason string.</summary>
        public event Action<ShopItemData, string> OnPurchaseFailed;

        /// <summary>Fired when shop data is loaded/refreshed (e.g. after login, daily reset)</summary>
        public event Action OnShopRefreshed;

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
            DontDestroyOnLoad(gameObject);

            if (autoLoadFromResources)
            {
                AutoLoadShopItems();
            }
        }

        private void Start()
        {
            // Subscribe to login event for per-user data loading
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnCloudReady += OnUserLoggedIn;

                // If user already logged in (e.g. scene reload)
                // OnCloudReady will fire after login verification
            }
            else
            {
                // Fallback for testing without auth
                Debug.LogWarning("[ShopManager] AuthManager not found — using Guest save");
                LoadPurchaseData();
            }
        }

        private void OnDestroy()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnCloudReady -= OnUserLoggedIn;
            }

            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Called when a user logs in — loads their shop purchase history
        /// </summary>
        private void OnUserLoggedIn(string username)
        {
            Debug.Log($"[ShopManager] User '{username}' logged in — loading shop data");
            LoadPurchaseData();
            CheckAndResetLimits();
            OnShopRefreshed?.Invoke();
        }

        /// <summary>
        /// Auto-loads all ShopItemData assets from Resources/Shop/
        /// </summary>
        private void AutoLoadShopItems()
        {
            ShopItemData[] loaded = Resources.LoadAll<ShopItemData>("Shop");

            if (loaded.Length > 0)
            {
                shopItems.Clear();
                shopItems.AddRange(loaded);
                shopItems = shopItems.OrderBy(i => i.sortOrder).ThenBy(i => i.itemName).ToList();
                Debug.Log($"[ShopManager] Auto-loaded {loaded.Length} shop items from Resources/Shop/");
            }
            else
            {
                Debug.LogWarning("[ShopManager] No ShopItemData found in Resources/Shop/. Create some!");
            }
        }

        // ==========================================
        // PURCHASE API
        // ==========================================

        /// <summary>
        /// Attempts to purchase a shop item. Validates everything before executing.
        /// </summary>
        /// <returns>PurchaseResult with success/failure and message</returns>
        public PurchaseResult TryPurchase(ShopItemData item)
        {
            if (item == null)
                return CreateFailure(null, "Invalid item — null reference");

            // --- AVAILABILITY ---
            if (!item.isAvailable)
                return CreateFailure(item, "This item is currently unavailable.");

            if (item.isHidden)
                return CreateFailure(item, "This item cannot be purchased.");

            // --- LEVEL REQUIREMENT ---
            if (item.requiredLevel > 0)
            {
                int playerLevel = PlayerCollection.Instance?.GetLevel() ?? 1;
                if (playerLevel < item.requiredLevel)
                    return CreateFailure(item, $"Requires level {item.requiredLevel}. You are level {playerLevel}.");
            }

            // --- PURCHASE LIMITS ---
            CheckAndResetLimits();
            ItemPurchaseTracker tracker = GetOrCreateTracker(item.itemId);

            if (item.dailyLimit > 0 && tracker.dailyPurchases >= item.dailyLimit)
                return CreateFailure(item, $"Daily limit reached! ({tracker.dailyPurchases}/{item.dailyLimit})");

            if (item.weeklyLimit > 0 && tracker.weeklyPurchases >= item.weeklyLimit)
                return CreateFailure(item, $"Weekly limit reached! ({tracker.weeklyPurchases}/{item.weeklyLimit})");

            if (item.totalLimit > 0 && tracker.totalPurchases >= item.totalLimit)
                return CreateFailure(item, "Maximum purchase limit reached!");

            // --- CURRENCY CHECK ---
            int effectivePrice = item.GetEffectivePrice();
            PlayerCollection player = PlayerCollection.Instance;

            if (player == null)
                return CreateFailure(item, "Player data not found!");

            bool canAfford = item.currencyType switch
            {
                ShopCurrencyType.Gold => player.CanAffordGold(effectivePrice),
                ShopCurrencyType.Crystals => player.CanAffordCrystals(effectivePrice),
                _ => false
            };

            if (!canAfford)
            {
                string currencyName = item.currencyType == ShopCurrencyType.Gold ? "Gold" : "Crystals";
                int currentAmount = item.currencyType == ShopCurrencyType.Gold ? player.GetGold() : player.GetCrystals();
                return CreateFailure(item, $"Not enough {currencyName}! Need {effectivePrice}, have {currentAmount}.");
            }

            // ===========================
            //  EXECUTE PURCHASE
            // ===========================

            // 1. Deduct currency
            if (item.currencyType == ShopCurrencyType.Gold)
                player.AddGold(-effectivePrice);
            else
                player.AddCrystals(-effectivePrice);

            // 2. Grant rewards
            GrantRewards(item);

            // 3. Update purchase tracker
            tracker.dailyPurchases++;
            tracker.weeklyPurchases++;
            tracker.totalPurchases++;
            tracker.lastPurchaseTime = DateTime.UtcNow.ToString("o");

            // 4. Save
            SavePurchaseData();

            if (logPurchases)
            {
                Debug.Log($"[ShopManager] ✅ PURCHASED: {item.itemName} for {effectivePrice} {item.currencyType}" +
                          $" (Daily: {tracker.dailyPurchases}/{(item.dailyLimit > 0 ? item.dailyLimit.ToString() : "∞")}," +
                          $" Weekly: {tracker.weeklyPurchases}/{(item.weeklyLimit > 0 ? item.weeklyLimit.ToString() : "∞")}," +
                          $" Total: {tracker.totalPurchases}/{(item.totalLimit > 0 ? item.totalLimit.ToString() : "∞")})");
            }

            OnPurchaseSuccess?.Invoke(item);

            return new PurchaseResult(true, $"Purchased {item.itemName}!");
        }

        /// <summary>
        /// Grants the rewards defined in the shop item
        /// </summary>
        private void GrantRewards(ShopItemData item)
        {
            PlayerCollection player = PlayerCollection.Instance;

            switch (item.itemType)
            {
                case ShopItemType.Lootbox:
                    if (item.lootboxReward != null && LootboxInventory.Instance != null)
                    {
                        LootboxInventory.Instance.AddLootbox(item.lootboxReward, item.lootboxQuantity);

                        if (logPurchases)
                            Debug.Log($"[ShopManager] → Granted {item.lootboxQuantity}x {item.lootboxReward.lootboxName}");
                    }
                    break;

                case ShopItemType.Skin:
                    if (item.skinReward != null)
                    {
                        // === PLACEHOLDER ===
                        // When SkinInventory is implemented, do:
                        if (SkinInventory.Instance != null)
                            SkinInventory.Instance.UnlockSkin(item.skinReward.skinId);
                        Debug.Log($"[ShopManager] Skin purchased: {item.skinReward.skinName}");
                    }
                    break;

                case ShopItemType.Consumable:
                    if (item.consumableReward != null)
                    {
                        // === PLACEHOLDER ===
                        // When ConsumableInventory is implemented, do:
                        // ConsumableInventory.Instance.AddConsumable(item.consumableReward, item.consumableQuantity);
                        Debug.Log($"[ShopManager] → Consumable purchased (placeholder): " +
                                  $"{item.consumableReward.consumableName} x{item.consumableQuantity}");
                    }
                    break;

                case ShopItemType.Bundle:
                    // Bundles can contain multiple reward types
                    if (item.lootboxReward != null && LootboxInventory.Instance != null)
                    {
                        LootboxInventory.Instance.AddLootbox(item.lootboxReward, item.lootboxQuantity);
                    }
                    if (item.skinReward != null)
                    {
                        if (SkinInventory.Instance != null)
                            SkinInventory.Instance.UnlockSkin(item.skinReward.skinId);
                        Debug.Log($"[ShopManager] Bundle skin unlocked: {item.skinReward.skinName}");
                    }
                    if (item.consumableReward != null)
                    {
                        Debug.Log($"[ShopManager] → Bundle consumable (placeholder): {item.consumableReward.consumableName}");
                    }
                    break;

                case ShopItemType.CurrencyPack:
                    // Currency packs only give bonus currency (handled below)
                    break;

                case ShopItemType.BattlePass:
                    // Grants premium Battle Pass
                    if (BattlePass.BattlePassManager.Instance != null)
                    {
                        BattlePass.BattlePassManager.Instance.GrantPremium();
                        Debug.Log("[ShopManager] → Battle Pass Premium granted via shop!");
                    }
                    break;
            }

            // Bonus currency (applicable to ANY item type — useful for bundles)
            if (item.bonusGold > 0 && player != null)
            {
                player.AddGold(item.bonusGold);
                if (logPurchases) Debug.Log($"[ShopManager] → +{item.bonusGold} Bonus Gold");
            }

            if (item.bonusCrystals > 0 && player != null)
            {
                player.AddCrystals(item.bonusCrystals);
                if (logPurchases) Debug.Log($"[ShopManager] → +{item.bonusCrystals} Bonus Crystals");
            }
        }

        /// <summary>
        /// Creates a failure result and fires the OnPurchaseFailed event
        /// </summary>
        private PurchaseResult CreateFailure(ShopItemData item, string reason)
        {
            if (logPurchases && item != null)
                Debug.Log($"[ShopManager] ❌ Purchase failed ({item.itemName}): {reason}");

            OnPurchaseFailed?.Invoke(item, reason);
            return new PurchaseResult(false, reason);
        }

        // ==========================================
        // DAILY / WEEKLY LIMIT TRACKING
        // ==========================================

        /// <summary>
        /// Checks if daily or weekly counters need resetting
        /// </summary>
        private void CheckAndResetLimits()
        {
            if (purchaseData == null)
            {
                purchaseData = new ShopSaveData();
                return;
            }

            DateTime now = DateTime.UtcNow;
            bool changed = false;

            // --- DAILY RESET ---
            if (!string.IsNullOrEmpty(purchaseData.lastDailyReset))
            {
                if (DateTime.TryParse(purchaseData.lastDailyReset, null, DateTimeStyles.RoundtripKind, out DateTime lastDaily))
                {
                    if (now.Date > lastDaily.Date)
                    {
                        foreach (var tracker in purchaseData.trackers)
                        {
                            tracker.dailyPurchases = 0;
                        }

                        purchaseData.lastDailyReset = now.ToString("o");
                        changed = true;

                        if (logPurchases) Debug.Log("[ShopManager] 🔄 Daily purchase limits reset");
                    }
                }
            }
            else
            {
                purchaseData.lastDailyReset = now.ToString("o");
                changed = true;
            }

            // --- WEEKLY RESET (ISO week, Monday-based) ---
            if (!string.IsNullOrEmpty(purchaseData.lastWeeklyReset))
            {
                if (DateTime.TryParse(purchaseData.lastWeeklyReset, null, DateTimeStyles.RoundtripKind, out DateTime lastWeekly))
                {
                    int currentWeek = GetISOWeekNumber(now);
                    int lastWeek = GetISOWeekNumber(lastWeekly);

                    if (currentWeek != lastWeek || now.Year != lastWeekly.Year)
                    {
                        foreach (var tracker in purchaseData.trackers)
                        {
                            tracker.weeklyPurchases = 0;
                        }

                        purchaseData.lastWeeklyReset = now.ToString("o");
                        changed = true;

                        if (logPurchases) Debug.Log("[ShopManager] 🔄 Weekly purchase limits reset");
                    }
                }
            }
            else
            {
                purchaseData.lastWeeklyReset = now.ToString("o");
                changed = true;
            }

            if (changed)
            {
                SavePurchaseData();
                OnShopRefreshed?.Invoke();
            }
        }

        /// <summary>
        /// Gets ISO 8601 week number (Monday-based)
        /// </summary>
        private int GetISOWeekNumber(DateTime date)
        {
            CultureInfo ci = CultureInfo.InvariantCulture;
            return ci.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        /// <summary>
        /// Gets or creates a purchase tracker for given item
        /// </summary>
        private ItemPurchaseTracker GetOrCreateTracker(string itemId)
        {
            if (purchaseData == null)
                purchaseData = new ShopSaveData();

            ItemPurchaseTracker tracker = purchaseData.trackers.Find(t => t.itemId == itemId);

            if (tracker == null)
            {
                tracker = new ItemPurchaseTracker { itemId = itemId };
                purchaseData.trackers.Add(tracker);
            }

            return tracker;
        }

        // ==========================================
        // QUERY API (for UI)
        // ==========================================

        /// <summary>
        /// Gets all visible and available shop items
        /// </summary>
        public List<ShopItemData> GetAllVisibleItems()
        {
            return shopItems
                .Where(i => i != null && !i.isHidden && i.isAvailable)
                .OrderBy(i => i.sortOrder)
                .ThenBy(i => i.itemName)
                .ToList();
        }

        /// <summary>
        /// Gets visible items filtered by type
        /// </summary>
        public List<ShopItemData> GetItemsByType(ShopItemType type)
        {
            return GetAllVisibleItems().Where(i => i.itemType == type).ToList();
        }

        /// <summary>
        /// Gets items that match multiple types (for "Other" tab combining consumables + bundles)
        /// </summary>
        public List<ShopItemData> GetItemsByTypes(params ShopItemType[] types)
        {
            return GetAllVisibleItems().Where(i => types.Contains(i.itemType)).ToList();
        }

        /// <summary>
        /// How many more times can this item be purchased today? -1 = unlimited
        /// </summary>
        public int GetRemainingDaily(ShopItemData item)
        {
            if (item == null || item.dailyLimit <= 0) return -1;
            CheckAndResetLimits();
            var tracker = GetOrCreateTracker(item.itemId);
            return Mathf.Max(0, item.dailyLimit - tracker.dailyPurchases);
        }

        /// <summary>
        /// How many more times can this item be purchased this week? -1 = unlimited
        /// </summary>
        public int GetRemainingWeekly(ShopItemData item)
        {
            if (item == null || item.weeklyLimit <= 0) return -1;
            CheckAndResetLimits();
            var tracker = GetOrCreateTracker(item.itemId);
            return Mathf.Max(0, item.weeklyLimit - tracker.weeklyPurchases);
        }

        /// <summary>
        /// How many more times can this item be purchased ever? -1 = unlimited
        /// </summary>
        public int GetRemainingTotal(ShopItemData item)
        {
            if (item == null || item.totalLimit <= 0) return -1;
            var tracker = GetOrCreateTracker(item.itemId);
            return Mathf.Max(0, item.totalLimit - tracker.totalPurchases);
        }

        /// <summary>
        /// Gets the most restrictive remaining limit for display
        /// Returns the smallest non-negative value, or -1 if all unlimited
        /// </summary>
        public int GetSmallestRemainingLimit(ShopItemData item)
        {
            int daily = GetRemainingDaily(item);
            int weekly = GetRemainingWeekly(item);
            int total = GetRemainingTotal(item);

            int smallest = int.MaxValue;

            if (daily >= 0) smallest = Mathf.Min(smallest, daily);
            if (weekly >= 0) smallest = Mathf.Min(smallest, weekly);
            if (total >= 0) smallest = Mathf.Min(smallest, total);

            return smallest == int.MaxValue ? -1 : smallest;
        }

        /// <summary>
        /// Gets a human-readable limit string for UI display
        /// </summary>
        public string GetLimitDisplayText(ShopItemData item)
        {
            if (item == null) return "";

            List<string> parts = new List<string>();

            int daily = GetRemainingDaily(item);
            if (daily >= 0) parts.Add($"Daily: {daily}/{item.dailyLimit}");

            int weekly = GetRemainingWeekly(item);
            if (weekly >= 0) parts.Add($"Weekly: {weekly}/{item.weeklyLimit}");

            int total = GetRemainingTotal(item);
            if (total >= 0) parts.Add($"Total: {total}/{item.totalLimit}");

            return parts.Count > 0 ? string.Join(" | ", parts) : "";
        }

        /// <summary>
        /// Quick check if item can be purchased right now
        /// </summary>
        public bool CanPurchase(ShopItemData item)
        {
            if (item == null || !item.isAvailable || item.isHidden) return false;

            // Level
            if (item.requiredLevel > 0)
            {
                int level = PlayerCollection.Instance?.GetLevel() ?? 1;
                if (level < item.requiredLevel) return false;
            }

            // Limits
            CheckAndResetLimits();
            var tracker = GetOrCreateTracker(item.itemId);

            if (item.dailyLimit > 0 && tracker.dailyPurchases >= item.dailyLimit) return false;
            if (item.weeklyLimit > 0 && tracker.weeklyPurchases >= item.weeklyLimit) return false;
            if (item.totalLimit > 0 && tracker.totalPurchases >= item.totalLimit) return false;

            // Currency
            int price = item.GetEffectivePrice();
            return item.currencyType switch
            {
                ShopCurrencyType.Gold => PlayerCollection.Instance?.CanAffordGold(price) ?? false,
                ShopCurrencyType.Crystals => PlayerCollection.Instance?.CanAffordCrystals(price) ?? false,
                _ => false
            };
        }

        /// <summary>
        /// Returns reason WHY an item can't be purchased (for disabled button tooltip)
        /// </summary>
        public string GetCannotPurchaseReason(ShopItemData item)
        {
            if (item == null) return "Invalid item";
            if (!item.isAvailable) return "Currently unavailable";

            if (item.requiredLevel > 0)
            {
                int level = PlayerCollection.Instance?.GetLevel() ?? 1;
                if (level < item.requiredLevel) ;
                   // return $"{ShopIcons.LOCK} Requires level {item.requiredLevel}";
            }

            var tracker = GetOrCreateTracker(item.itemId);
            if (item.dailyLimit > 0 && tracker.dailyPurchases >= item.dailyLimit)
                return "Daily limit reached";
            if (item.weeklyLimit > 0 && tracker.weeklyPurchases >= item.weeklyLimit)
                return "Weekly limit reached";
            if (item.totalLimit > 0 && tracker.totalPurchases >= item.totalLimit)
                return "Max purchases reached";

            int price = item.GetEffectivePrice();
          //  string icon = ShopIcons.GetCurrencyIcon(item.currencyType);

            //if (item.currencyType == ShopCurrencyType.Gold && !PlayerCollection.Instance.CanAffordGold(price))
            //    return $"Need {icon} {price}";
            //if (item.currencyType == ShopCurrencyType.Crystals && !PlayerCollection.Instance.CanAffordCrystals(price))
            //    return $"Need {icon} {price}";

            return "";
        }

        // ==========================================
        // SAVE / LOAD (Per-User)
        // ==========================================


        private void SavePurchaseData()
        {
            if (purchaseData == null) return;

            string json = JsonUtility.ToJson(purchaseData, true);

            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.SaveData("ShopManagerData", json);
            }
        }

        private void LoadPurchaseData()
        {
            if (CloudSaveManager.Instance != null)
            {
                Debug.Log("[ShopManager] Loading shop data from PlayFab cloud...");
                CloudSaveManager.Instance.LoadData("ShopManagerData",
                    json =>
                    {
                        Debug.Log("[ShopManager] Cloud data loaded.");
                        ProcessShopJson(json);
                    },
                    () =>
                    {
                        Debug.Log("[ShopManager] No cloud data - fresh shop.");
                        purchaseData = new ShopSaveData();
                        CheckAndResetLimits();
                    });
            }
            else
            {
                purchaseData = new ShopSaveData();
                CheckAndResetLimits();
            }
        }



        private void ProcessShopJson(string json)
        {
            try
            {
                purchaseData = JsonUtility.FromJson<ShopSaveData>(json);
                Debug.Log($"[ShopManager] Loaded {purchaseData.trackers.Count} purchase trackers");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ShopManager] Load failed: {e.Message}");
                purchaseData = new ShopSaveData();
            }
            CheckAndResetLimits();
        }

        [ContextMenu("Print Shop Catalog")]
        private void DebugPrintCatalog()
        {
            Debug.Log($"=== SHOP CATALOG ({shopItems.Count} items) ===");
            foreach (var item in shopItems)
            {
                string status = item.isAvailable ? "✅" : "❌";
                string hidden = item.isHidden ? " [HIDDEN]" : "";
                Debug.Log($"  {status} {item.itemName} ({item.itemType}) — {item.GetEffectivePrice()} {item.currencyType}{hidden}");
            }
        }

        [ContextMenu("Print Purchase History")]
        private void DebugPrintHistory()
        {
            if (purchaseData == null)
            {
                Debug.Log("[ShopManager] No purchase data loaded");
                return;
            }

            Debug.Log($"=== PURCHASE HISTORY ===");
            Debug.Log($"Last Daily Reset: {purchaseData.lastDailyReset}");
            Debug.Log($"Last Weekly Reset: {purchaseData.lastWeeklyReset}");

            foreach (var t in purchaseData.trackers)
            {
                Debug.Log($"  {t.itemId}: Daily={t.dailyPurchases}, Weekly={t.weeklyPurchases}, Total={t.totalPurchases}");
            }
        }

        [ContextMenu("Force Reset Daily Limits")]
        private void DebugResetDaily()
        {
            if (purchaseData == null) return;

            foreach (var t in purchaseData.trackers)
                t.dailyPurchases = 0;

            purchaseData.lastDailyReset = DateTime.UtcNow.ToString("o");
            SavePurchaseData();
            OnShopRefreshed?.Invoke();

            Debug.Log("[ShopManager] Daily limits force-reset");
        }

        [ContextMenu("Force Reset Weekly Limits")]
        private void DebugResetWeekly()
        {
            if (purchaseData == null) return;

            foreach (var t in purchaseData.trackers)
                t.weeklyPurchases = 0;

            purchaseData.lastWeeklyReset = DateTime.UtcNow.ToString("o");
            SavePurchaseData();
            OnShopRefreshed?.Invoke();

            Debug.Log("[ShopManager] Weekly limits force-reset");
        }

        [ContextMenu("Force Reset ALL Purchase Data")]
        private void DebugResetAll()
        {
            purchaseData = new ShopSaveData();
            SavePurchaseData();
            OnShopRefreshed?.Invoke();

            Debug.Log("[ShopManager] ALL purchase data reset for current user");
        }

        [ContextMenu("Reload Shop Items from Resources")]
        private void DebugReloadItems()
        {
            AutoLoadShopItems();
            OnShopRefreshed?.Invoke();
        }
    }

    // ==========================================
    // DATA CLASSES
    // ==========================================

    /// <summary>
    /// Result of a purchase attempt
    /// </summary>
    public class PurchaseResult
    {
        public bool success;
        public string message;

        public PurchaseResult(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }
    }

    /// <summary>
    /// Serializable save data for shop purchases (per user)
    /// </summary>
    [Serializable]
    public class ShopSaveData
    {
        public List<ItemPurchaseTracker> trackers = new List<ItemPurchaseTracker>();
        public string lastDailyReset = "";
        public string lastWeeklyReset = "";
    }

    /// <summary>
    /// Tracks purchase count for a single item
    /// </summary>
    [Serializable]
    public class ItemPurchaseTracker
    {
        public string itemId;
        public int dailyPurchases;
        public int weeklyPurchases;
        public int totalPurchases;
        public string lastPurchaseTime;
    }
}
