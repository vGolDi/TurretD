using UnityEngine;
using UnityEngine.UIElements;
using ElementumDefense.Cards;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class DraftUI : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioClip selectSound;
        [SerializeField] private AudioClip mulliganSound;

        private AudioSource audioSource;
        private VisualElement root;

        private Label draftTitle;
        private Label draftSubtitle;
        private Label draftTimer;
        private VisualElement draftCards;
        private VisualElement confirmSection;
        private VisualElement rerollInfo;
        private Button btnConfirm;

        private DraftManager draftManager;
        private bool isInitialized;

        private float retryTimer;
        private const float RETRY_INTERVAL = 0.5f;

        private bool[] rerolledSlots;

        // Track if starter draft is already showing
        // so we don't reset on mulligan refresh
        private bool starterDraftActive;

        private enum DraftMode
        {
            None,
            Starter,
            MidGame
        }

        private DraftMode currentMode = DraftMode.None;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource =
                    gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null) return;

            root = uiDoc.rootVisualElement;
            QueryElements();
            HidePanel();
        }

        private void Update()
        {
            if (!isInitialized)
            {
                retryTimer += Time.deltaTime;
                if (retryTimer >= RETRY_INTERVAL)
                {
                    retryTimer = 0f;
                    TryInitialize();
                }
            }
        }

        private void TryInitialize()
        {
            draftManager = DraftManager.Instance;
            if (draftManager == null) return;

            draftManager.OnStarterDraftOffered +=
                OnStarterDraftOffered;
            draftManager.OnMidGameDraftOffered +=
                OnMidGameDraftOffered;
            draftManager.OnDraftTimerUpdate +=
                UpdateTimer;
            draftManager.OnMidGameCardMulliganed +=
                OnMidGameSlotMulliganed;

            isInitialized = true;
            Debug.Log("[DraftUI] Initialized");
        }

        private void OnDestroy()
        {
            if (draftManager != null)
            {
                draftManager.OnStarterDraftOffered -=
                    OnStarterDraftOffered;
                draftManager.OnMidGameDraftOffered -=
                    OnMidGameDraftOffered;
                draftManager.OnDraftTimerUpdate -=
                    UpdateTimer;
                draftManager.OnMidGameCardMulliganed -=
                    OnMidGameSlotMulliganed;
            }
        }

        private void QueryElements()
        {
            draftTitle =
                root.Q<Label>("draft-title");
            draftSubtitle =
                root.Q<Label>("draft-subtitle");
            draftTimer =
                root.Q<Label>("draft-timer");
            draftCards =
                root.Q<VisualElement>("draft-cards");
            confirmSection =
                root.Q<VisualElement>(
                    "draft-confirm-section");
            rerollInfo =
                root.Q<VisualElement>(
                    "draft-reroll-info");
            btnConfirm =
                root.Q<Button>("btn-confirm-draft");

            btnConfirm?
                .RegisterCallback<ClickEvent>(evt =>
                {
                    PlaySound(selectSound);
                    ConfirmStarterDraft();
                    evt.StopPropagation();
                });
        }

        // ==========================================
        // STARTER DRAFT
        // ==========================================

        private void OnStarterDraftOffered(
            CardData[] cards)
        {
            if (starterDraftActive)
            {
                // This is a mulligan refresh from
                // DraftManager — rebuild cards but
                // KEEP rerolledSlots intact
                Debug.Log(
                    "[DraftUI] Starter mulligan " +
                    "refresh — rebuilding cards");
                PopulateCards(cards, true);
                return;
            }

            // First time showing starter draft
            starterDraftActive = true;
            currentMode = DraftMode.Starter;
            rerolledSlots = new bool[cards.Length];
            ShowPanel();

            if (draftTitle != null)
                draftTitle.text = "THE OFFERING";
            if (draftSubtitle != null)
                draftSubtitle.text =
                    "YOUR STARTING HAND";
            if (confirmSection != null)
                confirmSection.style.display =
                    DisplayStyle.Flex;
            if (rerollInfo != null)
                rerollInfo.style.display =
                    DisplayStyle.Flex;

            PopulateCards(cards, true);
        }

        private void ConfirmStarterDraft()
        {
            draftManager?.ConfirmStarterDraft();
            HidePanel();
        }

        // ==========================================
        // MID-GAME DRAFT
        // ==========================================

        private void OnMidGameDraftOffered(
            CardData[] cards)
        {
            // If mid-game draft is already active,
            // this is a mulligan refresh — just
            // rebuild with current rerolledSlots
            if (currentMode == DraftMode.MidGame &&
                rerolledSlots != null)
            {
                Debug.Log(
                    "[DraftUI] Mid-game mulligan " +
                    "refresh — rebuilding cards");
                PopulateCards(cards, false);
                return;
            }

            currentMode = DraftMode.MidGame;
            rerolledSlots = new bool[cards.Length];
            ShowPanel();

            if (draftTitle != null)
                draftTitle.text = "THE ARCANA";
            if (draftSubtitle != null)
                draftSubtitle.text =
                    "CHOOSE ONE CARD";
            if (confirmSection != null)
                confirmSection.style.display =
                    DisplayStyle.None;
            if (rerollInfo != null)
                rerollInfo.style.display =
                    DisplayStyle.Flex;

            PopulateCards(cards, false);
        }

        private void OnCardSelected(int index)
        {
            if (draftManager == null) return;
            PlaySound(selectSound);
            draftManager.SelectMidGameCard(index);
            HidePanel();
        }

        private void OnMidGameSlotMulliganed(
            int slotIndex, CardData newCard)
        {
            // Mark as rerolled — the full refresh
            // via OnMidGameDraftOffered will use this
            if (rerolledSlots != null &&
                slotIndex < rerolledSlots.Length)
                rerolledSlots[slotIndex] = true;

            // Card gets rebuilt via
            // OnMidGameDraftOffered which fires
            // right after this in DraftManager
            Debug.Log(
                $"[DraftUI] Slot {slotIndex} " +
                "marked as rerolled");
        }

        // ==========================================
        // CARD BUILDING
        // ==========================================

        private void PopulateCards(
            CardData[] cards, bool isStarter)
        {
            if (draftCards == null) return;
            draftCards.Clear();

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;
                var card = BuildDraftCard(
                    cards[i], i, isStarter);
                draftCards.Add(card);
            }
        }

        private VisualElement BuildDraftCard(
            CardData card, int index, bool isStarter)
        {
            string rk = card.rarity switch
            {
                CardRarity.Legendary => "legendary",
                CardRarity.Rare => "rare",
                _ => "common"
            };

            var slot = new VisualElement();
            slot.AddToClassList("draft-card");
            slot.AddToClassList($"draft-card-{rk}");

            // Inner border
            var inner = new VisualElement();
            inner.AddToClassList("draft-card-inner");
            slot.Add(inner);

            // Corners
            foreach (var pos in new[] {
                "tl", "tr", "bl", "br" })
            {
                var c = new VisualElement();
                c.AddToClassList("dc-corner");
                c.AddToClassList($"dc-corner-{pos}");
                slot.Add(c);
            }

            // Icon
            if (card.cardIcon != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList("draft-card-icon");
                icon.style.backgroundImage =
                    new StyleBackground(card.cardIcon);
                slot.Add(icon);
            }

            // Rarity line
            var line = new VisualElement();
            line.AddToClassList(
                "draft-card-rarity-line");
            line.AddToClassList(
                $"rarity-line-{rk}");
            slot.Add(line);

            // Name
            var nameLabel = new Label(card.cardName);
            nameLabel.AddToClassList("draft-card-name");
            slot.Add(nameLabel);

            // Rarity text
            var rarity = new Label(
                card.rarity.ToString().ToUpper());
            rarity.AddToClassList(
                "draft-card-rarity-text");
            rarity.AddToClassList(
                $"rarity-text-{rk}");
            slot.Add(rarity);

            // Description
            if (!string.IsNullOrEmpty(card.description))
            {
                var desc = new Label(card.description);
                desc.AddToClassList("draft-card-desc");
                slot.Add(desc);
            }

            // Buttons container
            var buttonsContainer = new VisualElement();
            buttonsContainer.AddToClassList(
                "draft-card-buttons");

            // Select button (mid-game only)
            if (!isStarter)
            {
                var selBtn = new Button();
                selBtn.text = "SELECT";
                selBtn.AddToClassList(
                    "draft-select-btn");

                int idx = index;
                selBtn.RegisterCallback<ClickEvent>(
                    evt =>
                    {
                        OnCardSelected(idx);
                        evt.StopPropagation();
                    });

                buttonsContainer.Add(selBtn);
            }

            // Check if already rerolled
            bool alreadyRerolled =
                rerolledSlots != null &&
                index < rerolledSlots.Length &&
                rerolledSlots[index];

            if (alreadyRerolled)
            {
                // Show greyed out used label
                var usedLabel = new Label(
                    "REROLL USED");
                usedLabel.AddToClassList(
                    "draft-mulligan-used");
                buttonsContainer.Add(usedLabel);
            }
            else
            {
                // Check if DraftManager allows it
                bool canMull = isStarter
                    ? true
                    : (draftManager
                        ?.CanMulliganMidGameSlot(
                            index) ?? false);

                if (canMull)
                {
                    var mullBtn = new Button();
                    mullBtn.text = "REROLL";
                    mullBtn.AddToClassList(
                        "draft-mulligan-btn");

                    int capturedIdx = index;
                    bool capturedStarter = isStarter;

                    mullBtn
                        .RegisterCallback<ClickEvent>(
                        evt =>
                        {
                            PlaySound(mulliganSound);

                            // Mark rerolled BEFORE
                            // calling DraftManager
                            if (rerolledSlots != null &&
                                capturedIdx <
                                rerolledSlots.Length)
                                rerolledSlots[
                                    capturedIdx] = true;

                            if (capturedStarter)
                                draftManager
                                    ?.MulliganCard(
                                        capturedIdx);
                            else
                                draftManager
                                    ?.MulliganMidGameCard(
                                        capturedIdx);

                            // DraftManager will fire
                            // OnStarterDraftOffered or
                            // OnMidGameDraftOffered
                            // which rebuilds UI with
                            // rerolledSlots intact

                            evt.StopPropagation();
                        });

                    buttonsContainer.Add(mullBtn);

                    // Warning (mid-game only)
                    if (!isStarter)
                    {
                        var warn = new Label(
                            "Random rarity");
                        warn.AddToClassList(
                            "draft-mulligan-warning");
                        buttonsContainer.Add(warn);
                    }
                }
                else
                {
                    var usedLabel = new Label(
                        "REROLL USED");
                    usedLabel.AddToClassList(
                        "draft-mulligan-used");
                    buttonsContainer.Add(usedLabel);
                }
            }

            slot.Add(buttonsContainer);

            // Hover accent
            var accent = new VisualElement();
            accent.AddToClassList(
                "draft-card-hover-accent");
            accent.AddToClassList(
                $"hover-accent-{rk}");
            slot.Add(accent);

            return slot;
        }

        // ==========================================
        // TIMER
        // ==========================================

        private void UpdateTimer(float remaining)
        {
            if (draftTimer == null) return;

            draftTimer.text =
                Mathf.CeilToInt(remaining).ToString();

            if (remaining <= 5f)
                draftTimer.AddToClassList(
                    "draft-timer-critical");
            else
                draftTimer.RemoveFromClassList(
                    "draft-timer-critical");
        }

        // ==========================================
        // SHOW / HIDE
        // ==========================================

        private void ShowPanel()
        {
            var draftRoot =
                root.Q<VisualElement>("draft-root");
            draftRoot?.RemoveFromClassList("hidden");
        }

        public void HidePanel()
        {
            var draftRoot =
                root.Q<VisualElement>("draft-root");
            draftRoot?.AddToClassList("hidden");
            currentMode = DraftMode.None;
            starterDraftActive = false;
            rerolledSlots = null;
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip, 0.7f);
        }
    }
}
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using Unity.VisualScripting;

//namespace ElementumDefense.Cards
//{
//    public class DraftUI : MonoBehaviour
//    {
//        [Header("Panels")]
//        [SerializeField] private GameObject starterDraftPanel;
//        [SerializeField] private GameObject midGameDraftPanel;

//        [Header("Starter Draft UI")]
//        [SerializeField] private GameObject[] starterSlotObjects = new GameObject[5];
//        [SerializeField] private Button confirmStarterButton;
//        [SerializeField] private TextMeshProUGUI starterTimerText;

//        [Header("Mid-Game Draft UI")]
//        [SerializeField] private GameObject[] midGameSlotObjects = new GameObject[3];
//        [SerializeField] private TextMeshProUGUI midGameTimerText;

//        private DraftManager draftManager;
//        private bool isInitialized = false;

//        private float initializationRetryTimer = 0f;
//        private const float RETRY_INTERVAL = 0.5f;

//        private void Start()
//        {
//            HideAllPanels();
//        }

//        private void Update()
//        {
//            if (!isInitialized)
//            {
//                initializationRetryTimer += Time.deltaTime;

//                if (initializationRetryTimer >= RETRY_INTERVAL)
//                {
//                    initializationRetryTimer = 0f;
//                    TryInitialize();
//                }

//                return;
//            }
//        }

//        private void TryInitialize()
//        {
//            draftManager = DraftManager.Instance;

//            if (draftManager == null) return;

//            draftManager.OnStarterDraftOffered += ShowStarterDraft;
//            draftManager.OnMidGameDraftOffered += ShowMidGameDraft;
//            draftManager.OnDraftTimerUpdate += UpdateTimer;

//            // ========== NOWE: Subscribe to mid-game mulligan ==========
//            draftManager.OnMidGameCardMulliganed += OnMidGameSlotMulliganed;
//            // ==========================================================

//            if (confirmStarterButton != null)
//            {
//                confirmStarterButton.onClick.AddListener(ConfirmStarterDraft);
//            }

//            isInitialized = true;
//            Debug.Log("[DraftUI] ✅ Initialized!");
//        }

//        private void OnDestroy()
//        {
//            if (draftManager != null)
//            {
//                draftManager.OnStarterDraftOffered -= ShowStarterDraft;
//                draftManager.OnMidGameDraftOffered -= ShowMidGameDraft;
//                draftManager.OnDraftTimerUpdate -= UpdateTimer;

//                // ========== NOWE ==========
//                draftManager.OnMidGameCardMulliganed -= OnMidGameSlotMulliganed;
//                // =========================
//            }
//        }

//        // ==========================================
//        // STARTER DRAFT (bez zmian)
//        // ==========================================

//        private void ShowStarterDraft(CardData[] cards)
//        {
//            HideAllPanels();

//            if (starterDraftPanel != null)
//            {
//                starterDraftPanel.SetActive(true);
//            }

//            for (int i = 0; i < starterSlotObjects.Length && i < cards.Length; i++)
//            {
//                if (starterSlotObjects[i] != null && cards[i] != null)
//                {
//                    UpdateCardSlot(starterSlotObjects[i], cards[i], i, true);
//                }
//            }
//        }

//        // ==========================================
//        // MID-GAME DRAFT (Z MULLIGAN)
//        // ==========================================

//        private void ShowMidGameDraft(CardData[] cards)
//        {
//            HideAllPanels();

//            if (midGameDraftPanel != null)
//            {
//                midGameDraftPanel.SetActive(true);
//            }

//            for (int i = 0; i < midGameSlotObjects.Length && i < cards.Length; i++)
//            {
//                if (midGameSlotObjects[i] != null && cards[i] != null)
//                {
//                    UpdateMidGameSlot(midGameSlotObjects[i], cards[i], i);
//                }
//            }

//            Debug.Log("[DraftUI] Showing mid-game draft with mulligan");
//        }

//        /// <summary>
//        /// Updates a mid-game card slot with SELECT + MULLIGAN buttons
//        /// </summary>
//        private void UpdateMidGameSlot(GameObject slotObj, CardData card, int index)
//        {
//            // Find UI elements
//            Image cardIcon = slotObj.transform.Find("CardIcon")?.GetComponent<Image>();
//            TextMeshProUGUI cardName = slotObj.transform.Find("CardName")?.GetComponent<TextMeshProUGUI>();
//            TextMeshProUGUI description = slotObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
//            Image rarityBorder = slotObj.transform.Find("RarityBorder")?.GetComponent<Image>();
//            Image topLine = slotObj.transform.Find("LineTop")?.GetComponent<Image>();
//            Image botLine = slotObj.transform.Find("LineBottom")?.GetComponent<Image>();
//            TextMeshProUGUI rarityText = slotObj.transform.Find("RarityText")?.GetComponent<TextMeshProUGUI>();

//            // ========== NOWE: Mulligan warning text ==========
//            TextMeshProUGUI mulliganWarning = slotObj.transform
//                .Find("MulliganWarning")?.GetComponent<TextMeshProUGUI>();
//            // =================================================

//            Button selectBtn = slotObj.GetComponent<Button>();
//            Button mulliganBtn = slotObj.transform
//                .Find("MulliganButton")?.GetComponent<Button>();

//            // Populate card info
//            if (cardIcon != null && card.cardIcon != null)
//                cardIcon.sprite = card.cardIcon;

//            if (cardName != null)
//                cardName.text = card.cardName;

//            if (description != null)
//                description.text = card.description;

//            if (rarityBorder != null)
//                rarityBorder.color = card.GetRarityColor().WithAlpha(0.2f);

//            if (topLine != null && botLine != null)
//            {
//                topLine.color = card.GetRarityColor();
//                botLine.color = card.GetRarityColor();
//            }

//            if (rarityText != null)
//                rarityText.text = card.GetRarityName();

//            // ========== SELECT BUTTON ==========
//            if (selectBtn != null)
//            {
//                selectBtn.onClick.RemoveAllListeners();
//                int capturedIndex = index;
//                selectBtn.onClick.AddListener(() => OnCardSelected(capturedIndex));
//            }

//            // ========== NOWE: MULLIGAN BUTTON ==========
//            if (mulliganBtn != null)
//            {
//                mulliganBtn.onClick.RemoveAllListeners();
//                int capturedIndex = index;
//                mulliganBtn.onClick.AddListener(() =>
//                    OnMidGameMulliganClicked(capturedIndex));

//                // Check if already used
//                bool canMulligan = draftManager != null &&
//                                   draftManager.CanMulliganMidGameSlot(index);

//                mulliganBtn.interactable = canMulligan;

//                TextMeshProUGUI btnText =
//                    mulliganBtn.GetComponentInChildren<TextMeshProUGUI>();

//                if (btnText != null)
//                {
//                    btnText.text = canMulligan ? "🎲 Reroll" : "Used";
//                }
//            }
//            // ============================================

//            // ========== NOWE: Warning text ==========
//            if (mulliganWarning != null)
//            {
//                bool canMulligan = draftManager != null &&
//                                   draftManager.CanMulliganMidGameSlot(index);

//                mulliganWarning.gameObject.SetActive(canMulligan);
//                mulliganWarning.text = "⚠️ Random rarity!";
//                mulliganWarning.color = Color.yellow;
//            }
//            // ========================================
//        }

//        // ==========================================
//        // NOWE: Mid-game Mulligan handlers
//        // ==========================================

//        private void OnMidGameMulliganClicked(int slotIndex)
//        {
//            if (draftManager == null) return;

//            // Get old card info for animation/feedback
//            string oldCardName = "Unknown";
//            CardRarity oldRarity = CardRarity.Common;

//            if (slotIndex < midGameSlotObjects.Length)
//            {
//                TextMeshProUGUI nameText = midGameSlotObjects[slotIndex]
//                    .transform.Find("CardName")?.GetComponent<TextMeshProUGUI>();

//                if (nameText != null)
//                    oldCardName = nameText.text;
//            }

//            bool success = draftManager.MulliganMidGameCard(slotIndex);

//            if (success)
//            {
//                // Disable mulligan button immediately
//                Button mulliganBtn = midGameSlotObjects[slotIndex]
//                    .transform.Find("MulliganButton")?.GetComponent<Button>();

//                if (mulliganBtn != null)
//                {
//                    mulliganBtn.interactable = false;

//                    TextMeshProUGUI btnText =
//                        mulliganBtn.GetComponentInChildren<TextMeshProUGUI>();

//                    if (btnText != null)
//                        btnText.text = "Used";
//                }

//                // Hide warning
//                TextMeshProUGUI warning = midGameSlotObjects[slotIndex]
//                    .transform.Find("MulliganWarning")?.GetComponent<TextMeshProUGUI>();

//                if (warning != null)
//                    warning.gameObject.SetActive(false);

//                Debug.Log($"[DraftUI] Mid-game mulligan slot {slotIndex}");
//            }
//        }

//        /// <summary>
//        /// Called when a specific mid-game slot is mulliganed (event from DraftManager)
//        /// Updates only that slot's UI
//        /// </summary>
//        private void OnMidGameSlotMulliganed(int slotIndex, CardData newCard)
//        {
//            if (slotIndex < 0 || slotIndex >= midGameSlotObjects.Length) return;
//            if (midGameSlotObjects[slotIndex] == null || newCard == null) return;

//            // Update the specific slot
//            UpdateMidGameSlot(midGameSlotObjects[slotIndex], newCard, slotIndex);

//            Debug.Log($"[DraftUI] Updated mid-game slot {slotIndex} → {newCard.cardName} " +
//                      $"({newCard.rarity})");
//        }

//        // ==========================================
//        // CARD SELECTION
//        // ==========================================

//        private void OnCardSelected(int choiceIndex)
//        {
//            if (draftManager == null) return;

//            draftManager.SelectMidGameCard(choiceIndex);

//            if (midGameDraftPanel != null)
//            {
//                midGameDraftPanel.SetActive(false);
//            }

//            Debug.Log($"[DraftUI] Selected mid-game card {choiceIndex}");
//        }

//        // ==========================================
//        // STARTER DRAFT METHODS (bez zmian)
//        // ==========================================

//        private void UpdateCardSlot(GameObject slotObj, CardData card,
//            int index, bool isStarter)
//        {
//            Image cardIcon = slotObj.transform.Find("CardIcon")?.GetComponent<Image>();
//            TextMeshProUGUI cardName = slotObj.transform
//                .Find("CardName")?.GetComponent<TextMeshProUGUI>();
//            TextMeshProUGUI description = slotObj.transform
//                .Find("Description")?.GetComponent<TextMeshProUGUI>();
//            Image rarityBorder = slotObj.transform
//                .Find("RarityBorder")?.GetComponent<Image>();
//            Image topLine = slotObj.transform
//                .Find("LineTop")?.GetComponent<Image>();
//            Image botLine = slotObj.transform
//                .Find("LineBottom")?.GetComponent<Image>();
//            TextMeshProUGUI rarityText = slotObj.transform
//                .Find("RarityText")?.GetComponent<TextMeshProUGUI>();

//            Button selectBtn = slotObj.GetComponent<Button>();
//            Button mulliganBtn = slotObj.transform
//                .Find("MulliganButton")?.GetComponent<Button>();

//            if (cardIcon != null && card.cardIcon != null)
//                cardIcon.sprite = card.cardIcon;

//            if (cardName != null)
//                cardName.text = card.cardName;

//            if (description != null)
//                description.text = card.description;

//            if (rarityBorder != null)
//                rarityBorder.color = card.GetRarityColor().WithAlpha(0.2f);

//            if (topLine != null && botLine != null)
//            {
//                topLine.color = card.GetRarityColor();
//                botLine.color = card.GetRarityColor();
//            }

//            if (rarityText != null)
//                rarityText.text = card.GetRarityName();

//            if (isStarter)
//            {
//                if (mulliganBtn != null)
//                {
//                    mulliganBtn.onClick.RemoveAllListeners();
//                    int capturedIndex = index;
//                    mulliganBtn.onClick.AddListener(() =>
//                        OnMulliganClicked(capturedIndex));
//                    mulliganBtn.interactable = true;
//                }
//            }
//        }

//        private void OnMulliganClicked(int slotIndex)
//        {
//            bool success = draftManager.MulliganCard(slotIndex);

//            if (success)
//            {
//                Button mulliganBtn = starterSlotObjects[slotIndex]
//                    .transform.Find("MulliganButton")?.GetComponent<Button>();

//                if (mulliganBtn != null)
//                {
//                    mulliganBtn.interactable = false;

//                    TextMeshProUGUI btnText =
//                        mulliganBtn.GetComponentInChildren<TextMeshProUGUI>();

//                    if (btnText != null)
//                        btnText.text = "Used";
//                }
//            }
//        }

//        private void ConfirmStarterDraft()
//        {
//            if (draftManager != null)
//            {
//                draftManager.ConfirmStarterDraft();
//            }

//            if (starterDraftPanel != null)
//            {
//                starterDraftPanel.SetActive(false);
//            }
//        }

//        // ==========================================
//        // TIMER
//        // ==========================================

//        private void UpdateTimer(float timeRemaining)
//        {
//            string timeText = Mathf.CeilToInt(timeRemaining).ToString();

//            if (starterDraftPanel != null && starterDraftPanel.activeSelf &&
//                starterTimerText != null)
//            {
//                starterTimerText.text = $"Time: {timeText}s";
//            }

//            if (midGameDraftPanel != null && midGameDraftPanel.activeSelf &&
//                midGameTimerText != null)
//            {
//                midGameTimerText.text = $"Time: {timeText}s";

//                // ========== NOWE: Red timer when low ==========
//                if (timeRemaining <= 5f)
//                    midGameTimerText.color = Color.red;
//                else
//                    midGameTimerText.color = Color.white;
//                // ==============================================
//            }
//        }

//        private void HideAllPanels()
//        {
//            if (starterDraftPanel != null) starterDraftPanel.SetActive(false);
//            if (midGameDraftPanel != null) midGameDraftPanel.SetActive(false);
//        }
//    }
//}