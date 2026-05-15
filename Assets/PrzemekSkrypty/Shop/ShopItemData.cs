// Assets/PrzemekSkrypty/Shop/ShopItemData.cs
using UnityEngine;
using ElementumDefense.Lootbox;
using ElementumDefense.Skins;

namespace ElementumDefense.Shop
{
    /// <summary>
    /// Defines a single item available in the shop.
    /// Create via: Right Click → Create → Tower Defense → Shop → Shop Item
    /// </summary>
    public enum ShopItemType
    {
        Lootbox,
        Skin,
        Consumable,
        Bundle,
        CurrencyPack
    }

    public enum ShopCurrencyType
    {
        Gold,
        Crystals
    }

    [CreateAssetMenu(fileName = "New Shop Item", menuName = "Tower Defense/Shop/Shop Item")]
    public class ShopItemData : ScriptableObject
    {
        [Header("=== BASIC INFO ===")]
        [Tooltip("Unique ID for save/load tracking. Auto-fills from asset name if empty.")]
        public string itemId;

        public string itemName = "Shop Item";

        [TextArea(2, 4)]
        public string description = "A mysterious item from the shop.";

        public Sprite icon;
        public ShopItemType itemType = ShopItemType.Lootbox;

        [Header("=== PRICING ===")]
        public ShopCurrencyType currencyType = ShopCurrencyType.Gold;

        [Min(0)]
        public int price = 100;

        [Tooltip("Sale price. Set to 0 for no discount.")]
        [Min(0)]
        public int discountedPrice = 0;

        [Header("=== REWARDS: Lootbox ===")]
        [Tooltip("Which lootbox type does the player receive?")]
        public LootboxData lootboxReward;

        [Tooltip("How many lootboxes per single purchase")]
        [Min(1)]
        public int lootboxQuantity = 1;

        [Header("=== REWARDS: Skin (Placeholder) ===")]
        public SkinData skinReward;

        [Header("=== REWARDS: Consumable (Placeholder) ===")]
        public ConsumableData consumableReward;

        [Min(1)]
        public int consumableQuantity = 1;

        [Header("=== REWARDS: Bonus Currency ===")]
        [Tooltip("Extra gold granted on purchase (e.g. for bundles)")]
        [Min(0)]
        public int bonusGold = 0;

        [Tooltip("Extra crystals granted on purchase")]
        [Min(0)]
        public int bonusCrystals = 0;

        [Header("=== PURCHASE LIMITS ===")]
        [Tooltip("Max purchases per day. 0 = unlimited.")]
        [Min(0)]
        public int dailyLimit = 0;

        [Tooltip("Max purchases per week. 0 = unlimited.")]
        [Min(0)]
        public int weeklyLimit = 0;

        [Tooltip("Max total purchases ever (lifetime). 0 = unlimited.")]
        [Min(0)]
        public int totalLimit = 0;

        [Header("=== REQUIREMENTS ===")]
        [Tooltip("Minimum player level to see/buy this item. 0 = no requirement.")]
        [Min(0)]
        public int requiredLevel = 0;

        [Tooltip("Is this item currently available for purchase?")]
        public bool isAvailable = true;

        [Tooltip("If true, item won't appear in shop at all (for future/removed items)")]
        public bool isHidden = false;

        [Header("=== VISUAL ===")]
        [Tooltip("Sort order in shop grid. Lower numbers appear first.")]
        public int sortOrder = 0;

        public Color borderColor = Color.white;

        [Tooltip("Badge text like NEW, HOT, SALE, LIMITED. Leave empty for no badge.")]
        public string badgeText = "";

        public Color badgeColor = new Color(1f, 0.2f, 0.2f); // Red by default

        // ==========================================
        // HELPER METHODS
        // ==========================================

        /// <summary>
        /// Returns discounted price if available, otherwise full price
        /// </summary>
        public int GetEffectivePrice()
        {
            if (discountedPrice > 0 && discountedPrice < price)
                return discountedPrice;
            return price;
        }

        /// <summary>
        /// Whether this item is currently on sale
        /// </summary>
        public bool HasDiscount()
        {
            return discountedPrice > 0 && discountedPrice < price;
        }

        /// <summary>
        /// Discount percentage (e.g. 30 for 30% off)
        /// </summary>
        public float GetDiscountPercent()
        {
            if (!HasDiscount()) return 0f;
            return (1f - (float)discountedPrice / price) * 100f;
        }

        /// <summary>
        /// Gets currency emoji/symbol for display
        /// </summary>
        //public string GetCurrencySymbol()
        //{
        //    return ShopIcons.GetCurrencyIcon(currencyType);
        //}

        // ==========================================
        // VALIDATION
        // ==========================================

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(itemId))
            {
                itemId = name;
            }

            if (itemType == ShopItemType.Lootbox && lootboxReward == null)
            {
                Debug.LogWarning($"[ShopItemData] '{itemName}' is type Lootbox but has no lootboxReward assigned!");
            }

            if (itemType == ShopItemType.Skin && skinReward == null)
            {
                Debug.LogWarning($"[ShopItemData] '{itemName}' is type Skin but has no skinReward assigned!");
            }

            if (itemType == ShopItemType.Consumable && consumableReward == null)
            {
                Debug.LogWarning($"[ShopItemData] '{itemName}' is type Consumable but has no consumableReward assigned!");
            }

            if (price <= 0 && itemType != ShopItemType.CurrencyPack)
            {
                Debug.LogWarning($"[ShopItemData] '{itemName}' has price {price}!");
            }

            if (discountedPrice > 0 && discountedPrice >= price)
            {
                Debug.LogWarning($"[ShopItemData] '{itemName}' discount ({discountedPrice}) >= full price ({price})!");
            }
        }
    }
}