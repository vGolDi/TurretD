using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

namespace ElementumDefense.Cards
{
    public class DraftUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject starterDraftPanel;
        [SerializeField] private GameObject midGameDraftPanel;

        [Header("Starter Draft UI")]
        [SerializeField] private GameObject[] starterSlotObjects = new GameObject[5];
        [SerializeField] private Button confirmStarterButton;
        [SerializeField] private TextMeshProUGUI starterTimerText;

        [Header("Mid-Game Draft UI")]
        [SerializeField] private GameObject[] midGameSlotObjects = new GameObject[3];
        [SerializeField] private TextMeshProUGUI midGameTimerText;

        private DraftManager draftManager;
        private bool isInitialized = false;

        private float initializationRetryTimer = 0f;
        private const float RETRY_INTERVAL = 0.5f;

        private void Start()
        {
            HideAllPanels();
        }

        private void Update()
        {
            if (!isInitialized)
            {
                initializationRetryTimer += Time.deltaTime;

                if (initializationRetryTimer >= RETRY_INTERVAL)
                {
                    initializationRetryTimer = 0f;
                    TryInitialize();
                }

                return;
            }
        }

        private void TryInitialize()
        {
            draftManager = DraftManager.Instance;

            if (draftManager == null) return;

            draftManager.OnStarterDraftOffered += ShowStarterDraft;
            draftManager.OnMidGameDraftOffered += ShowMidGameDraft;
            draftManager.OnDraftTimerUpdate += UpdateTimer;

            // ========== NOWE: Subscribe to mid-game mulligan ==========
            draftManager.OnMidGameCardMulliganed += OnMidGameSlotMulliganed;
            // ==========================================================

            if (confirmStarterButton != null)
            {
                confirmStarterButton.onClick.AddListener(ConfirmStarterDraft);
            }

            isInitialized = true;
            Debug.Log("[DraftUI] ✅ Initialized!");
        }

        private void OnDestroy()
        {
            if (draftManager != null)
            {
                draftManager.OnStarterDraftOffered -= ShowStarterDraft;
                draftManager.OnMidGameDraftOffered -= ShowMidGameDraft;
                draftManager.OnDraftTimerUpdate -= UpdateTimer;

                // ========== NOWE ==========
                draftManager.OnMidGameCardMulliganed -= OnMidGameSlotMulliganed;
                // =========================
            }
        }

        // ==========================================
        // STARTER DRAFT (bez zmian)
        // ==========================================

        private void ShowStarterDraft(CardData[] cards)
        {
            HideAllPanels();

            if (starterDraftPanel != null)
            {
                starterDraftPanel.SetActive(true);
            }

            for (int i = 0; i < starterSlotObjects.Length && i < cards.Length; i++)
            {
                if (starterSlotObjects[i] != null && cards[i] != null)
                {
                    UpdateCardSlot(starterSlotObjects[i], cards[i], i, true);
                }
            }
        }

        // ==========================================
        // MID-GAME DRAFT (Z MULLIGAN)
        // ==========================================

        private void ShowMidGameDraft(CardData[] cards)
        {
            HideAllPanels();

            if (midGameDraftPanel != null)
            {
                midGameDraftPanel.SetActive(true);
            }

            for (int i = 0; i < midGameSlotObjects.Length && i < cards.Length; i++)
            {
                if (midGameSlotObjects[i] != null && cards[i] != null)
                {
                    UpdateMidGameSlot(midGameSlotObjects[i], cards[i], i);
                }
            }

            Debug.Log("[DraftUI] Showing mid-game draft with mulligan");
        }

        /// <summary>
        /// Updates a mid-game card slot with SELECT + MULLIGAN buttons
        /// </summary>
        private void UpdateMidGameSlot(GameObject slotObj, CardData card, int index)
        {
            // Find UI elements
            Image cardIcon = slotObj.transform.Find("CardIcon")?.GetComponent<Image>();
            TextMeshProUGUI cardName = slotObj.transform.Find("CardName")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI description = slotObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
            Image rarityBorder = slotObj.transform.Find("RarityBorder")?.GetComponent<Image>();
            Image topLine = slotObj.transform.Find("LineTop")?.GetComponent<Image>();
            Image botLine = slotObj.transform.Find("LineBottom")?.GetComponent<Image>();
            TextMeshProUGUI rarityText = slotObj.transform.Find("RarityText")?.GetComponent<TextMeshProUGUI>();

            // ========== NOWE: Mulligan warning text ==========
            TextMeshProUGUI mulliganWarning = slotObj.transform
                .Find("MulliganWarning")?.GetComponent<TextMeshProUGUI>();
            // =================================================

            Button selectBtn = slotObj.GetComponent<Button>();
            Button mulliganBtn = slotObj.transform
                .Find("MulliganButton")?.GetComponent<Button>();

            // Populate card info
            if (cardIcon != null && card.cardIcon != null)
                cardIcon.sprite = card.cardIcon;

            if (cardName != null)
                cardName.text = card.cardName;

            if (description != null)
                description.text = card.description;

            if (rarityBorder != null)
                rarityBorder.color = card.GetRarityColor().WithAlpha(0.2f);

            if (topLine != null && botLine != null)
            {
                topLine.color = card.GetRarityColor();
                botLine.color = card.GetRarityColor();
            }

            if (rarityText != null)
                rarityText.text = card.GetRarityName();

            // ========== SELECT BUTTON ==========
            if (selectBtn != null)
            {
                selectBtn.onClick.RemoveAllListeners();
                int capturedIndex = index;
                selectBtn.onClick.AddListener(() => OnCardSelected(capturedIndex));
            }

            // ========== NOWE: MULLIGAN BUTTON ==========
            if (mulliganBtn != null)
            {
                mulliganBtn.onClick.RemoveAllListeners();
                int capturedIndex = index;
                mulliganBtn.onClick.AddListener(() =>
                    OnMidGameMulliganClicked(capturedIndex));

                // Check if already used
                bool canMulligan = draftManager != null &&
                                   draftManager.CanMulliganMidGameSlot(index);

                mulliganBtn.interactable = canMulligan;

                TextMeshProUGUI btnText =
                    mulliganBtn.GetComponentInChildren<TextMeshProUGUI>();

                if (btnText != null)
                {
                    btnText.text = canMulligan ? "🎲 Reroll" : "Used";
                }
            }
            // ============================================

            // ========== NOWE: Warning text ==========
            if (mulliganWarning != null)
            {
                bool canMulligan = draftManager != null &&
                                   draftManager.CanMulliganMidGameSlot(index);

                mulliganWarning.gameObject.SetActive(canMulligan);
                mulliganWarning.text = "⚠️ Random rarity!";
                mulliganWarning.color = Color.yellow;
            }
            // ========================================
        }

        // ==========================================
        // NOWE: Mid-game Mulligan handlers
        // ==========================================

        private void OnMidGameMulliganClicked(int slotIndex)
        {
            if (draftManager == null) return;

            // Get old card info for animation/feedback
            string oldCardName = "Unknown";
            CardRarity oldRarity = CardRarity.Common;

            if (slotIndex < midGameSlotObjects.Length)
            {
                TextMeshProUGUI nameText = midGameSlotObjects[slotIndex]
                    .transform.Find("CardName")?.GetComponent<TextMeshProUGUI>();

                if (nameText != null)
                    oldCardName = nameText.text;
            }

            bool success = draftManager.MulliganMidGameCard(slotIndex);

            if (success)
            {
                // Disable mulligan button immediately
                Button mulliganBtn = midGameSlotObjects[slotIndex]
                    .transform.Find("MulliganButton")?.GetComponent<Button>();

                if (mulliganBtn != null)
                {
                    mulliganBtn.interactable = false;

                    TextMeshProUGUI btnText =
                        mulliganBtn.GetComponentInChildren<TextMeshProUGUI>();

                    if (btnText != null)
                        btnText.text = "Used";
                }

                // Hide warning
                TextMeshProUGUI warning = midGameSlotObjects[slotIndex]
                    .transform.Find("MulliganWarning")?.GetComponent<TextMeshProUGUI>();

                if (warning != null)
                    warning.gameObject.SetActive(false);

                Debug.Log($"[DraftUI] Mid-game mulligan slot {slotIndex}");
            }
        }

        /// <summary>
        /// Called when a specific mid-game slot is mulliganed (event from DraftManager)
        /// Updates only that slot's UI
        /// </summary>
        private void OnMidGameSlotMulliganed(int slotIndex, CardData newCard)
        {
            if (slotIndex < 0 || slotIndex >= midGameSlotObjects.Length) return;
            if (midGameSlotObjects[slotIndex] == null || newCard == null) return;

            // Update the specific slot
            UpdateMidGameSlot(midGameSlotObjects[slotIndex], newCard, slotIndex);

            Debug.Log($"[DraftUI] Updated mid-game slot {slotIndex} → {newCard.cardName} " +
                      $"({newCard.rarity})");
        }

        // ==========================================
        // CARD SELECTION
        // ==========================================

        private void OnCardSelected(int choiceIndex)
        {
            if (draftManager == null) return;

            draftManager.SelectMidGameCard(choiceIndex);

            if (midGameDraftPanel != null)
            {
                midGameDraftPanel.SetActive(false);
            }

            Debug.Log($"[DraftUI] Selected mid-game card {choiceIndex}");
        }

        // ==========================================
        // STARTER DRAFT METHODS (bez zmian)
        // ==========================================

        private void UpdateCardSlot(GameObject slotObj, CardData card,
            int index, bool isStarter)
        {
            Image cardIcon = slotObj.transform.Find("CardIcon")?.GetComponent<Image>();
            TextMeshProUGUI cardName = slotObj.transform
                .Find("CardName")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI description = slotObj.transform
                .Find("Description")?.GetComponent<TextMeshProUGUI>();
            Image rarityBorder = slotObj.transform
                .Find("RarityBorder")?.GetComponent<Image>();
            Image topLine = slotObj.transform
                .Find("LineTop")?.GetComponent<Image>();
            Image botLine = slotObj.transform
                .Find("LineBottom")?.GetComponent<Image>();
            TextMeshProUGUI rarityText = slotObj.transform
                .Find("RarityText")?.GetComponent<TextMeshProUGUI>();

            Button selectBtn = slotObj.GetComponent<Button>();
            Button mulliganBtn = slotObj.transform
                .Find("MulliganButton")?.GetComponent<Button>();

            if (cardIcon != null && card.cardIcon != null)
                cardIcon.sprite = card.cardIcon;

            if (cardName != null)
                cardName.text = card.cardName;

            if (description != null)
                description.text = card.description;

            if (rarityBorder != null)
                rarityBorder.color = card.GetRarityColor().WithAlpha(0.2f);

            if (topLine != null && botLine != null)
            {
                topLine.color = card.GetRarityColor();
                botLine.color = card.GetRarityColor();
            }

            if (rarityText != null)
                rarityText.text = card.GetRarityName();

            if (isStarter)
            {
                if (mulliganBtn != null)
                {
                    mulliganBtn.onClick.RemoveAllListeners();
                    int capturedIndex = index;
                    mulliganBtn.onClick.AddListener(() =>
                        OnMulliganClicked(capturedIndex));
                    mulliganBtn.interactable = true;
                }
            }
        }

        private void OnMulliganClicked(int slotIndex)
        {
            bool success = draftManager.MulliganCard(slotIndex);

            if (success)
            {
                Button mulliganBtn = starterSlotObjects[slotIndex]
                    .transform.Find("MulliganButton")?.GetComponent<Button>();

                if (mulliganBtn != null)
                {
                    mulliganBtn.interactable = false;

                    TextMeshProUGUI btnText =
                        mulliganBtn.GetComponentInChildren<TextMeshProUGUI>();

                    if (btnText != null)
                        btnText.text = "Used";
                }
            }
        }

        private void ConfirmStarterDraft()
        {
            if (draftManager != null)
            {
                draftManager.ConfirmStarterDraft();
            }

            if (starterDraftPanel != null)
            {
                starterDraftPanel.SetActive(false);
            }
        }

        // ==========================================
        // TIMER
        // ==========================================

        private void UpdateTimer(float timeRemaining)
        {
            string timeText = Mathf.CeilToInt(timeRemaining).ToString();

            if (starterDraftPanel != null && starterDraftPanel.activeSelf &&
                starterTimerText != null)
            {
                starterTimerText.text = $"Time: {timeText}s";
            }

            if (midGameDraftPanel != null && midGameDraftPanel.activeSelf &&
                midGameTimerText != null)
            {
                midGameTimerText.text = $"Time: {timeText}s";

                // ========== NOWE: Red timer when low ==========
                if (timeRemaining <= 5f)
                    midGameTimerText.color = Color.red;
                else
                    midGameTimerText.color = Color.white;
                // ==============================================
            }
        }

        private void HideAllPanels()
        {
            if (starterDraftPanel != null) starterDraftPanel.SetActive(false);
            if (midGameDraftPanel != null) midGameDraftPanel.SetActive(false);
        }
    }
}