using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Shop UI for purchasing cards with gold/crystals
    /// </summary>
    public class CardShopUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Transform shopContent;
        [SerializeField] private GameObject shopCardSlotPrefab;

        [Header("Currency Display")]
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI crystalsText;

        [Header("Filters")]
        [SerializeField] private TMP_Dropdown rarityFilterDropdown;

        private PlayerCollection playerCollection;

        // ==========================================
        // INITIALIZATION
        // ==========================================

        private void Start()
        {
            playerCollection = PlayerCollection.Instance;

            if (playerCollection == null)
            {
                Debug.LogError("[CardShopUI] PlayerCollection not found!");
                return;
            }

            // Subscribe to currency changes
            playerCollection.OnGoldChanged += UpdateGoldDisplay;
            playerCollection.OnCrystalsChanged += UpdateCrystalsDisplay;

            // Setup filters
            if (rarityFilterDropdown != null)
            {
                rarityFilterDropdown.onValueChanged.AddListener((_) => RefreshShop());
            }

            RefreshShop();
            UpdateCurrencyDisplay();
        }

        private void OnDestroy()
        {
            if (playerCollection != null)
            {
                playerCollection.OnGoldChanged -= UpdateGoldDisplay;
                playerCollection.OnCrystalsChanged -= UpdateCrystalsDisplay;
            }
        }

        // ==========================================
        // SHOP DISPLAY
        // ==========================================

        /// <summary>
        /// Refreshes shop with locked cards
        /// </summary>
        private void RefreshShop()
        {
            if (shopContent == null || shopCardSlotPrefab == null) return;

            // Clear existing
            foreach (Transform child in shopContent)
            {
                Destroy(child.gameObject);
            }

            // Get locked cards
            List<CardData> lockedCards = playerCollection.GetLockedCards();

            // Filter by rarity if selected
            if (rarityFilterDropdown != null && rarityFilterDropdown.value > 0)
            {
                CardRarity targetRarity = (CardRarity)(rarityFilterDropdown.value - 1);
                lockedCards = lockedCards.FindAll(c => c.rarity == targetRarity);
            }

            // Sort by cost
            lockedCards.Sort((a, b) => a.unlockCost.CompareTo(b.unlockCost));

            // Create shop slots
            foreach (CardData card in lockedCards)
            {
                GameObject slotObj = Instantiate(shopCardSlotPrefab, shopContent);

                ShopCardSlot slot = slotObj.GetComponent<ShopCardSlot>();
                if (slot != null)
                {
                    bool canAfford = playerCollection.CanAffordGold(card.unlockCost);
                    slot.SetCard(card, canAfford);
                    slot.SetPurchaseCallback(() => PurchaseCard(card));
                }
            }

            Debug.Log($"[CardShopUI] Showing {lockedCards.Count} locked cards in shop");
        }

        // ==========================================
        // PURCHASING
        // ==========================================

        /// <summary>
        /// Attempts to purchase card
        /// </summary>
        private void PurchaseCard(CardData card)
        {
            if (playerCollection.PurchaseCard(card))
            {
                Debug.Log($"[CardShopUI] Purchased {card.cardName}!");

                // Refresh shop (remove purchased card)
                RefreshShop();
            }
            else
            {
                Debug.LogWarning($"[CardShopUI] Failed to purchase {card.cardName}");
                // TODO: Show error message
            }
        }

        // ==========================================
        // CURRENCY DISPLAY
        // ==========================================

        private void UpdateCurrencyDisplay()
        {
            UpdateGoldDisplay(playerCollection.GetGold());
            UpdateCrystalsDisplay(playerCollection.GetCrystals());
        }

        private void UpdateGoldDisplay(int gold)
        {
            if (goldText != null)
            {
                goldText.text = $"💰 {gold}";
            }
        }

        private void UpdateCrystalsDisplay(int crystals)
        {
            if (crystalsText != null)
            {
                crystalsText.text = $"💎 {crystals}";
            }
        }
    }

    // ==========================================
    // HELPER CLASS - Shop Card Slot
    // ==========================================

    public class ShopCardSlot : MonoBehaviour
    {
        [Header("UI Elements")]
        public Image cardIcon;
        public TextMeshProUGUI cardNameText;
        public TextMeshProUGUI priceText;
        public Button purchaseButton;
        public GameObject cannotAffordOverlay;

        private CardData currentCard;

        public void SetCard(CardData card, bool canAfford)
        {
            currentCard = card;

            if (cardIcon != null)
            {
                cardIcon.sprite = card.cardIcon;
            }

            if (cardNameText != null)
            {
                cardNameText.text = card.cardName;
            }

            if (priceText != null)
            {
                priceText.text = $"💰 {card.unlockCost}";
            }

            if (purchaseButton != null)
            {
                purchaseButton.interactable = canAfford;
            }

            if (cannotAffordOverlay != null)
            {
                cannotAffordOverlay.SetActive(!canAfford);
            }
        }

        public void SetPurchaseCallback(System.Action callback)
        {
            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveAllListeners();
                purchaseButton.onClick.AddListener(() => callback?.Invoke());
            }
        }
    }
}