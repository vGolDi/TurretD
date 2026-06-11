using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ElementumDefense.Shop;
using ElementumDefense.Cards;
using ElementumDefense.Lootbox;
using ElementumDefense.Skins;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class ShopUI : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private MainMenuController mainMenuController;

        [Header("Audio")]
        [SerializeField] private AudioClip purchaseSuccessSound;
        [SerializeField] private AudioClip purchaseFailSound;
        [SerializeField] private AudioClip tabClickSound;
        [SerializeField] private AudioClip buttonClickSound;

        [Header("Settings")]
        [SerializeField] private float popupDuration = 2.5f;

        private AudioSource audioSource;
        private VisualElement root;

        // Header
        private Label headerGold;
        private Label headerCrystals;

        // Tabs
        private Button tabAll;
        private Button tabLootboxes;
        private Button tabSkins;
        private Button tabOther;
        private List<Button> allTabs;

        // Grid
        private VisualElement itemGrid;

        // Confirm popup
        private VisualElement confirmPopup;
        private VisualElement confirmIcon;
        private Label confirmName;
        private Label confirmType;
        private Label confirmDescription;
        private Label confirmRewards;
        private Label confirmPrice;
        private Label confirmOldPrice;
        private Label confirmCurrencyLabel;
        private Label confirmLimit;
        private Button btnConfirmBuy;
        private Button btnConfirmCancel;

        // Feedback
        private VisualElement successPopup;
        private Label successText;
        private VisualElement failPopup;
        private Label failText;

        // Back
        private Button btnBack;

        // State
        private ShopItemData pendingItem;
        private ShopItemType? currentFilter = null;
        private ShopItemType[] multiFilter = null;
        private List<VisualElement> spawnedCards = new();
        private Coroutine feedbackCoroutine;


        private VisualElement confirmOldPriceContainer;
        private VisualElement confirmOldPriceStrike;
        // ==========================================
        // LIFECYCLE
        // ==========================================

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void OnEnable()
        {
            // Don't query here — Show() handles it
            // This prevents null issues when object
            // is enabled but UIDocument isn't ready yet

            if (mainMenuController == null)
                mainMenuController =
                    FindFirstObjectByType<MainMenuController>();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        // ==========================================
        // QUERY
        // ==========================================

        private void QueryElements()
        {
            headerGold = root.Q<Label>("header-gold");
            headerCrystals = root.Q<Label>("header-crystals");

            tabAll = root.Q<Button>("tab-all");
            tabLootboxes = root.Q<Button>("tab-lootboxes");
            tabSkins = root.Q<Button>("tab-skins");
            tabOther = root.Q<Button>("tab-other");
            allTabs = new List<Button>
                { tabAll, tabLootboxes, tabSkins, tabOther };

            itemGrid = root.Q<VisualElement>("item-grid");

            confirmPopup =
                root.Q<VisualElement>("confirm-popup");
            confirmIcon =
                root.Q<VisualElement>("confirm-icon");
            confirmName = root.Q<Label>("confirm-name");
            confirmType = root.Q<Label>("confirm-type");
            confirmDescription =
                root.Q<Label>("confirm-description");
            confirmRewards =
                root.Q<Label>("confirm-rewards");
            confirmPrice = root.Q<Label>("confirm-price");
            confirmOldPrice =
                root.Q<Label>("confirm-old-price");
            confirmCurrencyLabel =
                root.Q<Label>("confirm-currency-label");
            confirmLimit = root.Q<Label>("confirm-limit");
            btnConfirmBuy =
                root.Q<Button>("btn-confirm-buy");
            btnConfirmCancel =
                root.Q<Button>("btn-confirm-cancel");

            successPopup =
                root.Q<VisualElement>("success-popup");
            successText = root.Q<Label>("success-text");
            failPopup = root.Q<VisualElement>("fail-popup");
            failText = root.Q<Label>("fail-text");

            btnBack = root.Q<Button>("btn-back");

            SetupConfirmOldPriceStrike();
        }

        // ==========================================
        // BIND
        // ==========================================

        private void BindButtons()
        {
            btnBack?.RegisterCallback<ClickEvent>(evt =>
            {
                PlayClick();
                mainMenuController?.BackToMainMenu();
            });

            tabAll?.RegisterCallback<ClickEvent>(evt =>
            {
                PlayTab();
                SetFilter(null);
            });

            tabLootboxes?.RegisterCallback<ClickEvent>(evt =>
            {
                PlayTab();
                SetFilter(ShopItemType.Lootbox);
            });

            tabSkins?.RegisterCallback<ClickEvent>(evt =>
            {
                PlayTab();
                SetFilter(ShopItemType.Skin);
            });

            tabOther?.RegisterCallback<ClickEvent>(evt =>
            {
                PlayTab();
                SetFilterMultiple(
                    ShopItemType.Consumable,
                    ShopItemType.Bundle,
                    ShopItemType.CurrencyPack);
            });

            btnConfirmBuy?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    PlayClick();
                    ExecutePurchase();
                });

            btnConfirmCancel?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    PlayClick();
                    CloseConfirmation();
                });
        }

        // ==========================================
        // EVENTS
        // ==========================================

        private void SubscribeEvents()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnPurchaseSuccess +=
                    OnPurchaseSuccess;
                ShopManager.Instance.OnPurchaseFailed +=
                    OnPurchaseFailed;
                ShopManager.Instance.OnShopRefreshed +=
                    RefreshShop;
            }

            if (PlayerCollection.Instance != null)
            {
                PlayerCollection.Instance.OnGoldChanged +=
                    OnCurrencyChanged;
                PlayerCollection.Instance.OnCrystalsChanged +=
                    OnCurrencyChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (ShopManager.Instance != null)
            {
                ShopManager.Instance.OnPurchaseSuccess -=
                    OnPurchaseSuccess;
                ShopManager.Instance.OnPurchaseFailed -=
                    OnPurchaseFailed;
                ShopManager.Instance.OnShopRefreshed -=
                    RefreshShop;
            }

            if (PlayerCollection.Instance != null)
            {
                PlayerCollection.Instance.OnGoldChanged -=
                    OnCurrencyChanged;
                PlayerCollection.Instance.OnCrystalsChanged -=
                    OnCurrencyChanged;
            }
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        public void OpenShop()
        {
            if (root == null) return;
            if (allTabs == null) return;

            currentFilter = null;
            multiFilter = null;
            RefreshShop();
            RefreshCurrency();
            UpdateTabs();
        }

        public void Show()
        {
            var uiDoc = GetComponent<UIDocument>();

            if (uiDoc == null)
            {
                Debug.LogError("[ShopUI] No UIDocument!");
                return;
            }

            // Force enable the document first
            uiDoc.enabled = true;
            gameObject.SetActive(true);

            root = uiDoc.rootVisualElement;

            if (root == null)
            {
                Debug.LogError("[ShopUI] rootVisualElement is null!");
                return;
            }

            root.style.display = DisplayStyle.Flex;

            // Re-query every time we show
            QueryElements();
            BindButtons();
            SubscribeEvents();
            OpenShop();
        }

        public void Hide()
        {
            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc != null && uiDoc.rootVisualElement != null)
            {
                uiDoc.rootVisualElement.style.display =
                    DisplayStyle.None;
            }

            root = uiDoc?.rootVisualElement;
            CloseConfirmation();
        }

        // ==========================================
        // FILTERING
        // ==========================================

        private void SetFilter(ShopItemType? type)
        {
            currentFilter = type;
            multiFilter = null;
            RefreshShop();
            UpdateTabs();
        }

        private void SetFilterMultiple(
            params ShopItemType[] types)
        {
            currentFilter = null;
            multiFilter = types;
            RefreshShop();
            UpdateTabs();
        }

        private void UpdateTabs()
        {
            if (allTabs == null) return;

            foreach (var tab in allTabs)
            {
                if (tab == null) continue;
                tab.RemoveFromClassList("tab-active");
            }

            Button active = null;
            if (multiFilter != null) active = tabOther;
            else if (currentFilter == null) active = tabAll;
            else if (currentFilter == ShopItemType.Lootbox)
                active = tabLootboxes;
            else if (currentFilter == ShopItemType.Skin)
                active = tabSkins;

            active?.AddToClassList("tab-active");
        }

        // ==========================================
        // SHOP DISPLAY
        // ==========================================

        private void RefreshShop()
        {
            if (itemGrid == null || ShopManager.Instance == null)
                return;

            itemGrid.Clear();
            spawnedCards.Clear();

            List<ShopItemData> items;

            if (multiFilter != null)
                items = ShopManager.Instance
                    .GetItemsByTypes(multiFilter);
            else if (currentFilter.HasValue)
                items = ShopManager.Instance
                    .GetItemsByType(currentFilter.Value);
            else
                items = ShopManager.Instance
                    .GetAllVisibleItems();

            if (items == null || items.Count == 0)
            {
                var empty = BuildEmptyState(
                    "NO ITEMS AVAILABLE");
                itemGrid.Add(empty);
                return;
            }

            foreach (var item in items)
            {
                if (item == null) continue;
                var card = BuildItemCard(item);
                itemGrid.Add(card);
                spawnedCards.Add(card);
            }
        }

        // ==========================================
        // BUILD ITEM CARD
        // ==========================================

        private VisualElement BuildItemCard(ShopItemData item)
        {
            var card = new VisualElement();
            card.AddToClassList("shop-item-card");
            card.userData = item;

            // Stan
            bool canAfford = CanAfford(item);
            bool canBuy =
                ShopManager.Instance?.CanPurchase(item) ?? false;
            int remaining = ShopManager.Instance?
                .GetSmallestRemainingLimit(item) ?? -1;
            bool limitReached = remaining == 0;
            int playerLevel =
                PlayerCollection.Instance?.GetLevel() ?? 1;
            bool levelLocked = item.requiredLevel > 0
                && playerLevel < item.requiredLevel;

            if (!canAfford && !limitReached && !levelLocked)
                card.AddToClassList("cant-afford");
            if (limitReached)
                card.AddToClassList("limit-reached");
            if (levelLocked)
                card.AddToClassList("level-locked");

            // Wewnętrzna ramka
            var innerFrame = new VisualElement();
            innerFrame.AddToClassList("item-inner-frame");
            card.Add(innerFrame);

            // Badge
            if (!string.IsNullOrEmpty(item.badgeText))
            {
                var badge = new Label(
                    item.badgeText.ToUpper());
                badge.AddToClassList("item-badge");

                string badgeClass =
                    item.badgeText.ToUpper() switch
                    {
                        "SALE" => "item-badge-sale",
                        "NEW" => "item-badge-new",
                        "HOT" => "item-badge-hot",
                        "LIMITED" => "item-badge-limited",
                        _ => "item-badge-hot"
                    };
                badge.AddToClassList(badgeClass);
                card.Add(badge);
            }

            // Discount badge
            if (item.HasDiscount())
            {
                var discBadge = new Label(
                    $"-{Mathf.RoundToInt(item.GetDiscountPercent())}%");
                discBadge.AddToClassList(
                    "item-discount-badge");
                card.Add(discBadge);
            }

            // Ikona
            var iconSection = new VisualElement();
            iconSection.AddToClassList("item-icon-section");

            if (item.icon != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList("item-icon");
                icon.style.backgroundImage =
                    new StyleBackground(item.icon);
                iconSection.Add(icon);
            }

            var typeLabel = new Label(
                GetTypeDisplayName(item.itemType));
            typeLabel.AddToClassList("item-type-label");
            iconSection.Add(typeLabel);

            card.Add(iconSection);

            // Nazwa + opis
            var nameSection = new VisualElement();
            nameSection.AddToClassList("item-name-section");

            var nameLabel = new Label(item.itemName);
            nameLabel.AddToClassList("item-name");
            nameSection.Add(nameLabel);

            if (!string.IsNullOrEmpty(item.description))
            {
                var descLabel = new Label(item.description);
                descLabel.AddToClassList("item-description");
                nameSection.Add(descLabel);
            }

            card.Add(nameSection);

            // Separator
            var sep = new VisualElement();
            sep.AddToClassList("item-separator");
            card.Add(sep);

            // Limit text
            if (remaining >= 0 && remaining < 999)
            {
                string limitInfo = ShopManager.Instance?
                    .GetLimitDisplayText(item) ?? "";
                if (!string.IsNullOrEmpty(limitInfo))
                {
                    var limitLabel = new Label(limitInfo);
                    limitLabel.AddToClassList(
                        "item-limit-text");
                    card.Add(limitLabel);
                }
            }

            // Cena
            var priceSection = new VisualElement();
            priceSection.AddToClassList(
                "item-price-section");

            var priceLeft = new VisualElement();
            priceLeft.AddToClassList("item-price-left");

            var priceDot = new VisualElement();
            priceDot.AddToClassList("item-price-dot");
            priceDot.AddToClassList(
                item.currencyType == ShopCurrencyType.Gold
                    ? "item-price-dot-gold"
                    : "item-price-dot-crystal");
            priceLeft.Add(priceDot);


            if (item.HasDiscount())
            {
                var oldPriceContainer = new VisualElement();
                oldPriceContainer.AddToClassList(
                    "item-old-price-container");

                var oldPriceLabel = new Label(
                    item.price.ToString("N0"));
                oldPriceLabel.AddToClassList("item-old-price");
                oldPriceContainer.Add(oldPriceLabel);

                var strikethrough = new VisualElement();
                strikethrough.AddToClassList("item-old-price-line");
                oldPriceContainer.Add(strikethrough);

                priceLeft.Add(oldPriceContainer);
            }

            var priceLabel = new Label(
                item.GetEffectivePrice().ToString("N0"));
            priceLabel.AddToClassList("item-price");
            priceLabel.AddToClassList(
                item.currencyType == ShopCurrencyType.Gold
                    ? "item-price-gold"
                    : "item-price-crystal");

            if (item.HasDiscount())
                priceLabel.style.color =
                    new StyleColor(
                        new Color(0.29f, 0.87f, 0.5f));

            priceLeft.Add(priceLabel);
            priceSection.Add(priceLeft);

            var buyHint = new Label("BUY");
            buyHint.AddToClassList("item-buy-hint");
            priceSection.Add(buyHint);

            card.Add(priceSection);

            // Lock overlay
            if (levelLocked)
            {
                var lockOverlay = new VisualElement();
                lockOverlay.AddToClassList(
                    "item-lock-overlay");

                var lockText = new Label(
                    $"LEVEL {item.requiredLevel}");
                lockText.AddToClassList("item-lock-text");
                lockOverlay.Add(lockText);

                card.Add(lockOverlay);
            }

            // Hover accent
            var accent = new VisualElement();
            accent.AddToClassList("item-hover-accent");
            card.Add(accent);

            // Click
            if (!levelLocked && !limitReached)
            {
                card.RegisterCallback<ClickEvent>(evt =>
                {
                    PlayClick();
                    ShowConfirmation(item);
                    evt.StopPropagation();
                });
            }

            return card;
        }

        // ==========================================
        // CONFIRMATION
        // ==========================================

        private void ShowConfirmation(ShopItemData item)
        {
            if (confirmPopup == null || item == null) return;

            pendingItem = item;

            if (confirmIcon != null && item.icon != null)
                confirmIcon.style.backgroundImage =
                    new StyleBackground(item.icon);

            if (confirmName != null)
                confirmName.text = item.itemName;

            if (confirmType != null)
                confirmType.text =
                    GetTypeDisplayName(item.itemType);

            if (confirmDescription != null)
                confirmDescription.text = item.description;

            if (confirmRewards != null)
                confirmRewards.text =
                    BuildRewardPreview(item);

            // Cena
            if (confirmPrice != null)
            {
                confirmPrice.text =
                    item.GetEffectivePrice().ToString("N0");

                if (item.HasDiscount())
                    confirmPrice.style.color =
                        new StyleColor(
                            new Color(0.29f, 0.87f, 0.5f));
                else
                    confirmPrice.style.color =
                        new StyleColor(
                            new Color(0.98f, 0.75f, 0.14f));
            }

            // Stara cena z przekreśleniem
            if (confirmOldPriceContainer != null)
            {
                if (item.HasDiscount())
                {
                    confirmOldPrice.text =
                        item.price.ToString("N0");
                    confirmOldPriceContainer.style.display =
                        DisplayStyle.Flex;
                }
                else
                {
                    confirmOldPriceContainer.style.display =
                        DisplayStyle.None;
                }
            }

            if (confirmCurrencyLabel != null)
                confirmCurrencyLabel.text =
                    item.currencyType.ToString().ToUpper();

            if (confirmLimit != null)
            {
                string limitInfo = ShopManager.Instance?
                    .GetLimitDisplayText(item) ?? "";
                confirmLimit.text = limitInfo;
                confirmLimit.style.display =
                    string.IsNullOrEmpty(limitInfo)
                        ? DisplayStyle.None
                        : DisplayStyle.Flex;
            }

            bool canBuy =
                ShopManager.Instance?.CanPurchase(item)
                ?? false;

            if (btnConfirmBuy != null)
            {
                btnConfirmBuy.SetEnabled(canBuy);
                btnConfirmBuy.text = canBuy
                    ? "PURCHASE"
                    : (ShopManager.Instance?
                        .GetCannotPurchaseReason(item)
                        ?? "UNAVAILABLE");
            }

            confirmPopup.RemoveFromClassList("hidden");
        }

        private void ExecutePurchase()
        {
            if (pendingItem == null) return;

            ShopManager.Instance?.TryPurchase(pendingItem);
            CloseConfirmation();
            pendingItem = null;
        }

        private void CloseConfirmation()
        {
            confirmPopup?.AddToClassList("hidden");
            pendingItem = null;
        }

        // ==========================================
        // EVENT HANDLERS
        // ==========================================

        private void OnPurchaseSuccess(ShopItemData item)
        {
            if (purchaseSuccessSound != null)
                audioSource?.PlayOneShot(
                    purchaseSuccessSound);

            ShowFeedback(successPopup, successText,
                $"\u2713 Purchased: {item.itemName}!");

            RefreshCurrency();
            RefreshShop();
        }

        private void OnPurchaseFailed(
            ShopItemData item, string reason)
        {
            if (purchaseFailSound != null)
                audioSource?.PlayOneShot(purchaseFailSound);

            ShowFeedback(failPopup, failText,
                $"\u2717 {reason}");
        }

        private void OnCurrencyChanged(int _)
        {
            RefreshCurrency();
        }

        // ==========================================
        // CURRENCY
        // ==========================================

        private void RefreshCurrency()
        {
            if (PlayerCollection.Instance == null) return;

            if (headerGold != null)
                headerGold.text = FormatNumber(
                    PlayerCollection.Instance.GetGold());

            if (headerCrystals != null)
                headerCrystals.text = FormatNumber(
                    PlayerCollection.Instance.GetCrystals());
        }

        // ==========================================
        // FEEDBACK POPUPS
        // ==========================================

        private void ShowFeedback(
            VisualElement popup, Label text, string msg)
        {
            if (popup == null) return;

            if (text != null) text.text = msg;
            popup.RemoveFromClassList("hidden");

            if (feedbackCoroutine != null)
                StopCoroutine(feedbackCoroutine);

            feedbackCoroutine =
                StartCoroutine(HideFeedback(popup));
        }

        private IEnumerator HideFeedback(
            VisualElement popup)
        {
            yield return new WaitForSeconds(popupDuration);
            popup?.AddToClassList("hidden");
            feedbackCoroutine = null;
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private bool CanAfford(ShopItemData item)
        {
            if (item == null ||
                PlayerCollection.Instance == null)
                return false;

            int price = item.GetEffectivePrice();
            return item.currencyType switch
            {
                ShopCurrencyType.Gold =>
                    PlayerCollection.Instance
                        .CanAffordGold(price),
                ShopCurrencyType.Crystals =>
                    PlayerCollection.Instance
                        .CanAffordCrystals(price),
                _ => false
            };
        }

        private string GetTypeDisplayName(ShopItemType type)
        {
            return type switch
            {
                ShopItemType.Lootbox => "LOOTBOX",
                ShopItemType.Skin => "SKIN",
                ShopItemType.Consumable => "CONSUMABLE",
                ShopItemType.Bundle => "BUNDLE",
                ShopItemType.CurrencyPack => "CURRENCY",
                _ => type.ToString().ToUpper()
            };
        }

        private string BuildRewardPreview(ShopItemData item)
        {
            var rewards = new List<string>();

            if (item.lootboxReward != null)
                rewards.Add(
                    $"{item.lootboxQuantity}x " +
                    item.lootboxReward.lootboxName);

            if (item.skinReward != null)
            {
                rewards.Add(item.skinReward.skinName);
                if (!item.skinReward.IsUniversal)
                {
                    rewards.Add($"<color=#FF9900>[Requires Map: {string.Join(", ", item.skinReward.compatibleArenaTypes)}]</color>");
                }
            }

            if (item.consumableReward != null)
                rewards.Add(
                    $"{item.consumableQuantity}x " +
                    item.consumableReward.consumableName);

            if (item.bonusGold > 0)
                rewards.Add($"+{item.bonusGold} Gold");

            if (item.bonusCrystals > 0)
                rewards.Add(
                    $"+{item.bonusCrystals} Crystals");

            return rewards.Count > 0
                ? string.Join("\n", rewards)
                : "No rewards defined";
        }

        private VisualElement BuildEmptyState(string msg)
        {
            var container = new VisualElement();
            container.AddToClassList("empty-state");

            var icon = new Label("\u2727");
            icon.AddToClassList("empty-state-icon");
            container.Add(icon);

            var text = new Label(msg);
            text.AddToClassList("empty-state-text");
            container.Add(text);

            return container;
        }

        private string FormatNumber(int number)
        {
            if (number >= 1000000)
                return $"{number / 1000000f:F1}M";
            if (number >= 1000)
                return $"{number / 1000f:F1}K";
            return number.ToString("N0");
        }

        private void PlayClick()
        {
            if (buttonClickSound != null)
                audioSource?.PlayOneShot(
                    buttonClickSound, 0.7f);
        }

        private void PlayTab()
        {
            if (tabClickSound != null)
                audioSource?.PlayOneShot(
                    tabClickSound, 0.5f);
        }

        private void SetupConfirmOldPriceStrike()
        {
            if (confirmOldPrice == null) return;

            var parent = confirmOldPrice.parent;
            if (parent == null) return;

            int index = parent.IndexOf(confirmOldPrice);

            // Kontener
            confirmOldPriceContainer = new VisualElement();
            confirmOldPriceContainer.style.position =
                Position.Relative;
            confirmOldPriceContainer.style.flexDirection =
                FlexDirection.Row;
            confirmOldPriceContainer.style.alignItems =
                Align.Center;
            confirmOldPriceContainer.style.marginRight = 12;

            // Przenosimy label
            parent.Remove(confirmOldPrice);
            confirmOldPriceContainer.Add(confirmOldPrice);

            // Linia
            confirmOldPriceStrike = new VisualElement();
            confirmOldPriceStrike.style.position =
                Position.Absolute;
            confirmOldPriceStrike.style.left = -1;
            confirmOldPriceStrike.style.right = -1;
            confirmOldPriceStrike.style.top = new StyleLength(
                new Length(50, LengthUnit.Percent));
            confirmOldPriceStrike.style.height = 1;
            confirmOldPriceStrike.style.backgroundColor =
                new StyleColor(
                    new Color(0.97f, 0.44f, 0.44f, 0.7f));
            confirmOldPriceContainer.Add(confirmOldPriceStrike);

            // Wstawiamy kontener w miejsce labela
            parent.Insert(index, confirmOldPriceContainer);
        }
    }




}

//// Assets/PrzemekSkrypty/UI/ShopUI.cs
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections;
//using System.Collections.Generic;
//using ElementumDefense.Shop;
//using ElementumDefense.Cards;

//namespace ElementumDefense.UI
//{
//    /// <summary>
//    /// Main Shop UI controller.
//    /// Manages tabs, item grid, confirmation dialog, and purchase feedback.
//    /// Attach to the Shop panel GameObject.
//    /// </summary>
//    public class ShopUI : MonoBehaviour
//    {
//        [Header("=== ITEM GRID ===")]
//        [SerializeField, Tooltip("Parent transform for spawned item slots")]
//        private Transform itemContainer;

//        [SerializeField, Tooltip("Prefab for a single shop item slot")]
//        private GameObject shopItemSlotPrefab;

//        [Header("=== CATEGORY TABS ===")]
//        [SerializeField] private Button tabAll;
//        [SerializeField] private Button tabLootboxes;
//        [SerializeField] private Button tabSkins;
//        [SerializeField] private Button tabOther;

//        [Header("Tab Colors")]
//        [SerializeField] private Color activeTabColor = new Color(1f, 0.85f, 0.3f);
//        [SerializeField] private Color inactiveTabColor = new Color(0.4f, 0.4f, 0.4f);

//        [Header("=== CURRENCY DISPLAY ===")]
//        [SerializeField] private TMP_Text goldText;
//        [SerializeField] private TMP_Text crystalsText;

//        [Header("=== CONFIRMATION DIALOG ===")]
//        [SerializeField] private GameObject confirmationPanel;
//        [SerializeField] private Image confirmItemIcon;
//        [SerializeField] private TMP_Text confirmItemName;
//        [SerializeField] private TMP_Text confirmItemDescription;
//        [SerializeField] private TMP_Text confirmPriceText;
//        [SerializeField] private TMP_Text confirmOldPriceText;
//        [SerializeField] private TMP_Text confirmLimitText;
//        [SerializeField] private TMP_Text confirmRewardText;
//        [SerializeField] private Button confirmBuyButton;
//        [SerializeField] private Button confirmCancelButton;

//        [Header("=== FEEDBACK POPUPS ===")]
//        [SerializeField] private GameObject successPopup;
//        [SerializeField] private TMP_Text successText;
//        [SerializeField] private GameObject failPopup;
//        [SerializeField] private TMP_Text failText;
//        [SerializeField] private float popupDuration = 2.5f;

//        [Header("=== EMPTY STATE ===")]
//        [SerializeField] private GameObject emptyStateMessage;
//        [SerializeField] private TMP_Text emptyStateText;

//        [Header("=== AUDIO ===")]
//        [SerializeField] private AudioClip purchaseSuccessSound;
//        [SerializeField] private AudioClip purchaseFailSound;
//        [SerializeField] private AudioClip tabClickSound;
//        [SerializeField] private AudioClip buttonHoverSound;

//        // Runtime
//        private AudioSource audioSource;
//        private ShopItemData pendingPurchaseItem;
//        private List<GameObject> spawnedSlots = new List<GameObject>();
//        private ShopItemType? currentFilter = null;
//        private Coroutine activePopupCoroutine;

//        // ==========================================
//        // INITIALIZATION
//        // ==========================================

//        private void Awake()
//        {
//            audioSource = GetComponent<AudioSource>();
//            if (audioSource == null)
//                audioSource = gameObject.AddComponent<AudioSource>();
//        }

//        private void Start()
//        {
//            SetupButtons();
//            SubscribeToEvents();
//            RefreshCurrencyDisplay();

//            // Hide panels
//            if (confirmationPanel != null) confirmationPanel.SetActive(false);
//            if (successPopup != null) successPopup.SetActive(false);
//            if (failPopup != null) failPopup.SetActive(false);
//        }

//        private void OnEnable()
//        {
//            SubscribeToEvents();
//            RefreshShop();
//        }

//        private void OnDisable()
//        {
//            UnsubscribeFromEvents();
//        }

//        private void OnDestroy()
//        {
//            UnsubscribeFromEvents();
//        }

//        private void SetupButtons()
//        {
//            // Tab buttons
//            if (tabAll != null)
//                tabAll.onClick.AddListener(() => { PlayTabSound(); SetFilter(null); });

//            if (tabLootboxes != null)
//                tabLootboxes.onClick.AddListener(() => { PlayTabSound(); SetFilter(ShopItemType.Lootbox); });

//            if (tabSkins != null)
//                tabSkins.onClick.AddListener(() => { PlayTabSound(); SetFilter(ShopItemType.Skin); });

//            if (tabOther != null)
//                tabOther.onClick.AddListener(() => { PlayTabSound(); SetFilterMultiple(ShopItemType.Consumable, ShopItemType.Bundle, ShopItemType.CurrencyPack); });

//            // Confirmation buttons
//            if (confirmBuyButton != null)
//                confirmBuyButton.onClick.AddListener(ExecutePendingPurchase);

//            if (confirmCancelButton != null)
//                confirmCancelButton.onClick.AddListener(CancelPurchase);
//        }

//        private void SubscribeToEvents()
//        {
//            if (ShopManager.Instance != null)
//            {
//                ShopManager.Instance.OnPurchaseSuccess -= OnPurchaseSuccess;
//                ShopManager.Instance.OnPurchaseSuccess += OnPurchaseSuccess;

//                ShopManager.Instance.OnPurchaseFailed -= OnPurchaseFailed;
//                ShopManager.Instance.OnPurchaseFailed += OnPurchaseFailed;

//                ShopManager.Instance.OnShopRefreshed -= RefreshShop;
//                ShopManager.Instance.OnShopRefreshed += RefreshShop;
//            }

//            if (PlayerCollection.Instance != null)
//            {
//                PlayerCollection.Instance.OnGoldChanged -= OnCurrencyChanged;
//                PlayerCollection.Instance.OnGoldChanged += OnCurrencyChanged;

//                PlayerCollection.Instance.OnCrystalsChanged -= OnCurrencyChanged;
//                PlayerCollection.Instance.OnCrystalsChanged += OnCurrencyChanged;
//            }
//        }

//        private void UnsubscribeFromEvents()
//        {
//            if (ShopManager.Instance != null)
//            {
//                ShopManager.Instance.OnPurchaseSuccess -= OnPurchaseSuccess;
//                ShopManager.Instance.OnPurchaseFailed -= OnPurchaseFailed;
//                ShopManager.Instance.OnShopRefreshed -= RefreshShop;
//            }

//            if (PlayerCollection.Instance != null)
//            {
//                PlayerCollection.Instance.OnGoldChanged -= OnCurrencyChanged;
//                PlayerCollection.Instance.OnCrystalsChanged -= OnCurrencyChanged;
//            }
//        }

//        // ==========================================
//        // PUBLIC API
//        // ==========================================

//        /// <summary>
//        /// Opens the shop and refreshes display (call from MainMenuController)
//        /// </summary>
//        public void OpenShop()
//        {
//            gameObject.SetActive(true);
//            currentFilter = null;
//            RefreshShop();
//            RefreshCurrencyDisplay();
//            UpdateTabVisuals();

//            Debug.Log("[ShopUI] Shop opened");
//        }

//        /// <summary>
//        /// Called by ShopItemSlotUI when player clicks "Buy"
//        /// Shows confirmation dialog
//        /// </summary>
//        public void ShowPurchaseConfirmation(ShopItemData item)
//        {
//            if (item == null) return;

//            pendingPurchaseItem = item;

//            // Fill confirmation dialog
//            if (confirmItemIcon != null && item.icon != null)
//                confirmItemIcon.sprite = item.icon;

//            if (confirmItemName != null)
//                confirmItemName.text = item.itemName;

//            if (confirmItemDescription != null)
//                confirmItemDescription.text = item.description;

//            // Price display
//            if (confirmPriceText != null)
//            {
//                string icon = ShopIcons.GetCurrencyIcon(item.currencyType);

//                if (item.HasDiscount())
//                {
//                    confirmPriceText.text = $"{icon} {item.GetEffectivePrice()}";
//                    confirmPriceText.color = new Color(0.2f, 0.9f, 0.2f);

//                    if (confirmOldPriceText != null)
//                    {
//                        confirmOldPriceText.gameObject.SetActive(true);
//                        confirmOldPriceText.text = $"<s>{icon} {item.price}</s>";
//                    }
//                }
//                else
//                {
//                    confirmPriceText.text = $"{icon} {item.price}";
//                    confirmPriceText.color = Color.white;

//                    if (confirmOldPriceText != null)
//                        confirmOldPriceText.gameObject.SetActive(false);
//                }
//            }

//            // Limit text
//            if (confirmLimitText != null)
//            {
//                string limitInfo = ShopManager.Instance?.GetLimitDisplayText(item) ?? "";
//                confirmLimitText.text = limitInfo;
//                confirmLimitText.gameObject.SetActive(!string.IsNullOrEmpty(limitInfo));
//            }

//            // Reward preview
//            if (confirmRewardText != null)
//            {
//                confirmRewardText.text = BuildRewardPreviewText(item);
//            }

//            // Enable/disable buy button
//            bool canBuy = ShopManager.Instance?.CanPurchase(item) ?? false;
//            if (confirmBuyButton != null)
//            {
//                confirmBuyButton.interactable = canBuy;

//                // Change button text based on state
//                TMP_Text btnText = confirmBuyButton.GetComponentInChildren<TMP_Text>();
//                if (btnText != null)
//                {
//                    if (canBuy)
//                        btnText.text = "BUY";
//                    else
//                        btnText.text = ShopManager.Instance?.GetCannotPurchaseReason(item) ?? "UNAVAILABLE";
//                }
//            }

//            // Show dialog
//            if (confirmationPanel != null)
//                confirmationPanel.SetActive(true);
//        }

//        // ==========================================
//        // FILTERING & DISPLAY
//        // ==========================================

//        /// <summary>
//        /// Sets filter to single item type
//        /// </summary>
//        private void SetFilter(ShopItemType? type)
//        {
//            currentFilter = type;
//            RefreshShop();
//            UpdateTabVisuals();
//        }

//        // Store multi-filter types
//        private ShopItemType[] multiFilterTypes = null;

//        /// <summary>
//        /// Sets filter to multiple item types (for "Other" tab)
//        /// </summary>
//        private void SetFilterMultiple(params ShopItemType[] types)
//        {
//            currentFilter = null; // Clear single filter
//            multiFilterTypes = types;
//            RefreshShopWithMultiFilter();
//            UpdateTabVisuals();
//        }

//        /// <summary>
//        /// Refreshes the entire shop item grid
//        /// </summary>
//        public void RefreshShop()
//        {
//            multiFilterTypes = null; // Clear multi-filter when using single filter

//            if (ShopManager.Instance == null)
//            {
//                Debug.LogWarning("[ShopUI] ShopManager not found!");
//                return;
//            }

//            // Get items based on filter
//            List<ShopItemData> items;

//            if (currentFilter.HasValue)
//            {
//                items = ShopManager.Instance.GetItemsByType(currentFilter.Value);
//            }
//            else
//            {
//                items = ShopManager.Instance.GetAllVisibleItems();
//            }

//            RebuildItemGrid(items);
//        }

//        /// <summary>
//        /// Refreshes shop with multi-type filter
//        /// </summary>
//        private void RefreshShopWithMultiFilter()
//        {
//            if (ShopManager.Instance == null) return;

//            List<ShopItemData> items;

//            if (multiFilterTypes != null && multiFilterTypes.Length > 0)
//            {
//                items = ShopManager.Instance.GetItemsByTypes(multiFilterTypes);
//            }
//            else
//            {
//                items = ShopManager.Instance.GetAllVisibleItems();
//            }

//            RebuildItemGrid(items);
//        }

//        /// <summary>
//        /// Destroys old slots and creates new ones
//        /// </summary>
//        private void RebuildItemGrid(List<ShopItemData> items)
//        {
//            // Destroy old
//            foreach (var slot in spawnedSlots)
//            {
//                if (slot != null) Destroy(slot);
//            }
//            spawnedSlots.Clear();

//            // Show empty state if needed
//            if (emptyStateMessage != null)
//            {
//                bool isEmpty = items == null || items.Count == 0;
//                emptyStateMessage.SetActive(isEmpty);

//                if (isEmpty && emptyStateText != null)
//                {
//                    emptyStateText.text = "No items available in this category.";
//                }
//            }

//            if (items == null || items.Count == 0) return;

//            // Spawn new slots
//            foreach (ShopItemData item in items)
//            {
//                if (item == null) continue;

//                // Check level visibility (items below level req can be hidden or shown as locked)
//                int playerLevel = PlayerCollection.Instance?.GetLevel() ?? 1;

//                GameObject slotObj = Instantiate(shopItemSlotPrefab, itemContainer);
//                spawnedSlots.Add(slotObj);

//                ShopItemSlotUI slotUI = slotObj.GetComponent<ShopItemSlotUI>();

//                if (slotUI != null)
//                {
//                    slotUI.Setup(item, this);
//                }
//                else
//                {
//                    Debug.LogError($"[ShopUI] shopItemSlotPrefab is missing ShopItemSlotUI component!");
//                }
//            }
//        }

//        /// <summary>
//        /// Updates tab button colors to show which is active
//        /// </summary>
//        private void UpdateTabVisuals()
//        {
//            SetTabColor(tabAll, currentFilter == null && multiFilterTypes == null);
//            SetTabColor(tabLootboxes, currentFilter == ShopItemType.Lootbox);
//            SetTabColor(tabSkins, currentFilter == ShopItemType.Skin);
//            SetTabColor(tabOther, multiFilterTypes != null);
//        }

//        private void SetTabColor(Button tab, bool isActive)
//        {
//            if (tab == null) return;

//            Image tabImage = tab.GetComponent<Image>();
//            if (tabImage != null)
//            {
//                tabImage.color = isActive ? activeTabColor : inactiveTabColor;
//            }

//            TMP_Text tabText = tab.GetComponentInChildren<TMP_Text>();
//            if (tabText != null)
//            {
//                tabText.color = isActive ? Color.black : Color.white;
//            }
//        }

//        // ==========================================
//        // CURRENCY DISPLAY
//        // ==========================================

//        private void RefreshCurrencyDisplay()
//        {
//            if (PlayerCollection.Instance == null) return;

//            if (goldText != null)
//                goldText.text = FormatNumber(PlayerCollection.Instance.GetGold());

//            if (crystalsText != null)
//                crystalsText.text = FormatNumber(PlayerCollection.Instance.GetCrystals());
//        }

//        private void OnCurrencyChanged(int _)
//        {
//            RefreshCurrencyDisplay();

//            // Also refresh slot states (afford/can't afford may have changed)
//            foreach (var slotObj in spawnedSlots)
//            {
//                if (slotObj == null) continue;
//                ShopItemSlotUI slot = slotObj.GetComponent<ShopItemSlotUI>();
//                if (slot != null) slot.RefreshState();
//            }
//        }

//        // ==========================================
//        // PURCHASE FLOW
//        // ==========================================

//        /// <summary>
//        /// Executes the pending purchase from confirmation dialog
//        /// </summary>
//        private void ExecutePendingPurchase()
//        {
//            if (pendingPurchaseItem == null) return;

//            ShopManager.Instance?.TryPurchase(pendingPurchaseItem);

//            // Close confirmation
//            if (confirmationPanel != null)
//                confirmationPanel.SetActive(false);

//            pendingPurchaseItem = null;
//        }

//        /// <summary>
//        /// Cancels the pending purchase
//        /// </summary>
//        private void CancelPurchase()
//        {
//            pendingPurchaseItem = null;

//            if (confirmationPanel != null)
//                confirmationPanel.SetActive(false);
//        }

//        // ==========================================
//        // EVENT HANDLERS
//        // ==========================================

//        private void OnPurchaseSuccess(ShopItemData item)
//        {
//            if (purchaseSuccessSound != null && audioSource != null)
//                audioSource.PlayOneShot(purchaseSuccessSound);

//            ShowPopup(successPopup, successText, $"{ShopIcons.CHECK} Purchased: {item.itemName}!");

//            RefreshCurrencyDisplay();
//            RefreshShopSlots();
//        }

//        private void OnPurchaseFailed(ShopItemData item, string reason)
//        {
//            if (purchaseFailSound != null && audioSource != null)
//                audioSource.PlayOneShot(purchaseFailSound);

//            ShowPopup(failPopup, failText, $"{ShopIcons.CROSS} {reason}");
//        }

//        /// <summary>
//        /// Refreshes all existing slots without rebuilding
//        /// </summary>
//        private void RefreshShopSlots()
//        {
//            foreach (var slotObj in spawnedSlots)
//            {
//                if (slotObj == null) continue;
//                ShopItemSlotUI slot = slotObj.GetComponent<ShopItemSlotUI>();
//                if (slot != null) slot.RefreshState();
//            }
//        }

//        // ==========================================
//        // POPUPS
//        // ==========================================

//        private void ShowPopup(GameObject popup, TMP_Text text, string message)
//        {
//            if (popup == null) return;

//            if (text != null) text.text = message;
//            popup.SetActive(true);

//            if (activePopupCoroutine != null)
//                StopCoroutine(activePopupCoroutine);

//            activePopupCoroutine = StartCoroutine(HidePopupAfterDelay(popup));
//        }

//        private IEnumerator HidePopupAfterDelay(GameObject popup)
//        {
//            yield return new WaitForSeconds(popupDuration);

//            if (popup != null) popup.SetActive(false);
//            activePopupCoroutine = null;
//        }

//        // ==========================================
//        // HELPERS
//        // ==========================================

//        private string BuildRewardPreviewText(ShopItemData item)
//        {
//            List<string> rewards = new List<string>();

//            switch (item.itemType)
//            {
//                case ShopItemType.Lootbox:
//                    if (item.lootboxReward != null)
//                        rewards.Add($"{ShopIcons.LOOTBOX} {item.lootboxQuantity}x {item.lootboxReward.lootboxName}");
//                    break;

//                case ShopItemType.Skin:
//                    if (item.skinReward != null)
//                        rewards.Add($"{ShopIcons.SKIN} {item.skinReward.skinName}");
//                    break;

//                case ShopItemType.Consumable:
//                    if (item.consumableReward != null)
//                        rewards.Add($"{ShopIcons.CONSUMABLE} {item.consumableQuantity}x {item.consumableReward.consumableName}");
//                    break;

//                case ShopItemType.Bundle:
//                    if (item.lootboxReward != null)
//                        rewards.Add($"{ShopIcons.LOOTBOX} {item.lootboxQuantity}x {item.lootboxReward.lootboxName}");
//                    if (item.skinReward != null)
//                        rewards.Add($"{ShopIcons.SKIN} {item.skinReward.skinName}");
//                    if (item.consumableReward != null)
//                        rewards.Add($"{ShopIcons.CONSUMABLE} {item.consumableQuantity}x {item.consumableReward.consumableName}");
//                    break;
//            }

//            if (item.bonusGold > 0)
//                rewards.Add($"{ShopIcons.GOLD} +{item.bonusGold} Gold");
//            if (item.bonusCrystals > 0)
//                rewards.Add($"{ShopIcons.CRYSTAL} +{item.bonusCrystals} Crystals");

//            return rewards.Count > 0 ? string.Join("\n", rewards) : "No rewards defined";
//        }

//        private string FormatNumber(int number)
//        {
//            if (number >= 1000000) return $"{number / 1000000f:F1}M";
//            if (number >= 1000) return $"{number / 1000f:F1}K";
//            return number.ToString("N0");
//        }

//        private void PlayTabSound()
//        {
//            if (tabClickSound != null && audioSource != null)
//                audioSource.PlayOneShot(tabClickSound, 0.5f);
//        }
//    }
//}