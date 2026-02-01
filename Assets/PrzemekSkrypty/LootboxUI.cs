using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace ElementumDefense.Cards
{
    public class LootboxUI : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private LootboxData standardBox;
        [SerializeField] private LootboxData legendaryBox;

        [Header("UI Panels")]
        [SerializeField] private GameObject menuPanel;   // Buttons to buy boxes
        [SerializeField] private GameObject resultPanel; // Shows cards

        [Header("Result Display")]
        [SerializeField] private Transform cardsContainer;
        [SerializeField] private GameObject cardResultPrefab; // Prefab similar to DeckbuilderCardSlot
        [SerializeField] private Button closeResultButton;

        [Header("Menu Buttons")]
        [SerializeField] private Button openStandardButton;
        [SerializeField] private Button openLegendaryButton;

        private PlayerCollection playerCollection;
        private void Start()
        {
            playerCollection = PlayerCollection.Instance;
            // Link buttons
            if (openStandardButton != null)
                openStandardButton.onClick.AddListener(() => OpenBox(standardBox));

            if (openLegendaryButton != null)
                openLegendaryButton.onClick.AddListener(() => OpenBox(legendaryBox));

            if (closeResultButton != null)
                closeResultButton.onClick.AddListener(CloseResults);

            CloseResults(); // Ensure we start in menu state
        }

        private void Update()
        {
            // Opcjonalnie: Blokowanie przycisków jeśli nie stać gracza
            if (playerCollection != null)
            {
                if (openStandardButton != null && standardBox != null)
                    openStandardButton.interactable = playerCollection.CanAffordGold(standardBox.priceGold);

                if (openLegendaryButton != null && legendaryBox != null)
                    openLegendaryButton.interactable = playerCollection.CanAffordGold(legendaryBox.priceGold);
            }
        }

        private void OpenBox(LootboxData boxData)
        {
            if (LootboxManager.Instance == null || playerCollection == null) return;

            // 1. Sprawdź czy gracza stać
            if (!playerCollection.CanAffordGold(boxData.priceGold))
            {
                Debug.LogWarning($"[LootboxUI] Not enough gold! Need {boxData.priceGold}, have {playerCollection.GetGold()}");
                // Tutaj możesz dodać np. dźwięk błędu lub komunikat "Brak złota"
                return;
            }

            // 2. Pobierz opłatę (AddGold z wartością ujemną)
            playerCollection.AddGold(-boxData.priceGold);
            Debug.Log($"[LootboxUI] Paid {boxData.priceGold} gold for {boxData.boxName}");

            // 3. Wygeneruj nagrody (Logika bez zmian)
            List<LootboxManager.LootResult> rewards = LootboxManager.Instance.OpenLootbox(boxData);

            // 4. Pokaż wyniki
            ShowResults(rewards);
        }

        private void ShowResults(List<LootboxManager.LootResult> rewards)
        {
            menuPanel.SetActive(false);
            resultPanel.SetActive(true);

            // Clear previous
            foreach (Transform child in cardsContainer)
            {
                Destroy(child.gameObject);
            }

            // Spawn cards
            foreach (var loot in rewards)
            {
                GameObject slotObj = Instantiate(cardResultPrefab, cardsContainer);

                // Setup Visuals (Assumes prefab structure similar to ShopCardSlot or DeckbuilderCardSlot)
                // Find components manually based on your existing structure
                Image icon = slotObj.transform.Find("CardIcon")?.GetComponent<Image>();
                Image topLine = slotObj.transform.Find("LineTop")?.GetComponent<Image>();
                Image bottomLine = slotObj.transform.Find("LineBottom")?.GetComponent<Image>();

                TextMeshProUGUI rarityText = slotObj.transform.Find("RarityText")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI description = slotObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
                TextMeshProUGUI nameText = slotObj.GetComponentInChildren<TextMeshProUGUI>();

                // Optional: Border color based on rarity
                Image border = slotObj.transform.Find("RarityBorder")?.GetComponent<Image>();

                // Set Data
                if (icon != null) icon.sprite = loot.Card.cardIcon;
                if (nameText != null) nameText.text = loot.Card.cardName;
                if (description != null) description.text = loot.Card.description;
                if (rarityText != null) rarityText.text = loot.Card.rarity.ToString();
                if (border != null) border.color = loot.Card.GetRarityColor();
                if (topLine &&  bottomLine != null)
                {
                    topLine.color = loot.Card.GetRarityColor();
                    bottomLine.color = loot.Card.GetRarityColor();
                }

                // Handle Duplicate / New Status
                GameObject newTag = slotObj.transform.Find("NewTag")?.gameObject; // Create this in prefab!
                GameObject refundTag = slotObj.transform.Find("RefundTag")?.gameObject; // Create this in prefab!
                TextMeshProUGUI refundText = refundTag?.GetComponentInChildren<TextMeshProUGUI>();

                if (loot.IsNew)
                {
                    if (newTag != null) newTag.SetActive(true);
                    if (refundTag != null) refundTag.SetActive(false);
                }
                else
                {
                    if (newTag != null) newTag.SetActive(false);
                    if (refundTag != null)
                    {
                        refundTag.SetActive(true);
                        if (refundText != null)
                        {
                            string currencyIcon = loot.RefundIsCrystals ? "💎" : "💰";
                            refundText.text = $"Duplicate!\n+{loot.RefundAmount} {currencyIcon}";
                        }
                    }
                }
            }
        }

        private void CloseResults()
        {
            resultPanel.SetActive(false);
            menuPanel.SetActive(true);
        }
    }
}