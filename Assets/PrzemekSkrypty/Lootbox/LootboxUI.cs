using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using ElementumDefense.Lootbox;
using ElementumDefense.Cards;

namespace ElementumDefense.UI
{
    /// <summary>
    /// Lootbox UI — UI Toolkit version
    /// Replaces old UGUI LootboxUI
    /// 3 views: Inventory → Opening → Results
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class LootboxUI : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField]
        private MainMenuController mainMenuController;

        [Header("Audio")]
        [SerializeField] private AudioClip cardRevealSound;
        [SerializeField] private AudioClip legendaryRevealSound;
        [SerializeField] private AudioClip duplicateSound;
        [SerializeField] private AudioClip openingSound;
        [SerializeField] private AudioClip buttonClickSound;

        [Header("Animation Settings")]
        [SerializeField] private float shakeDuration = 1.2f;
        [SerializeField] private float shakeIntensity = 8f;
        [SerializeField] private float timeBetweenCards = 0.4f;
        [SerializeField] private float popupDuration = 2.5f;

        [Header("Rarity Colors")]
        [SerializeField]
        private Color commonColor =
            new Color(0.7f, 0.7f, 0.7f);
        [SerializeField]
        private Color rareColor =
            new Color(0.3f, 0.5f, 1f);
        [SerializeField]
        private Color legendaryColor =
            new Color(1f, 0.8f, 0f);

        private AudioSource audioSource;
        private VisualElement root;

        // ===== Header =====
        private Label headerGold;
        private Label headerCrystals;
        private Button btnBack;

        // ===== Views =====
        private VisualElement inventoryView;
        private VisualElement openingView;
        private VisualElement resultsView;

        // ===== Inventory =====
        private VisualElement inventoryGrid;
        private Label inventoryCountLabel;
        private VisualElement emptyState;

        // ===== Opening =====
        private VisualElement openingIcon;
        private VisualElement openingGlow;
        private VisualElement openingGlowRing;
        private Label openingName;
        private Label openingRarity;
        private Label openingHint;

        // ===== Results =====
        private VisualElement resultsCardsContainer;
        private Label summaryNewCount;
        private Label summaryDupeCount;
        private Label summaryCurrency;
        private VisualElement summaryDuplicatesRow;
        private Button btnOpenAnother;
        private Button btnContinue;

        // ===== Feedback =====
        private VisualElement successPopup;
        private Label successText;
        private VisualElement failPopup;
        private Label failText;

        // ===== State =====
        private LootboxData currentLootbox;
        private LootboxResult currentResult;
        private Coroutine feedbackCoroutine;
        private Coroutine openingCoroutine;

        // ==========================================
        // LIFECYCLE
        // ==========================================

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource =
                    gameObject.AddComponent<AudioSource>();
        }

        private void OnEnable()
        {
            var uiDoc = GetComponent<UIDocument>();
            root = uiDoc.rootVisualElement;

            if (mainMenuController == null)
                mainMenuController =
                    FindFirstObjectByType<MainMenuController>();

            QueryElements();
            BindButtons();
            SubscribeEvents();
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
            // Header
            headerGold = root.Q<Label>("header-gold");
            headerCrystals =
                root.Q<Label>("header-crystals");
            btnBack = root.Q<Button>("btn-back");

            // Views
            inventoryView =
                root.Q<VisualElement>("inventory-view");
            openingView =
                root.Q<VisualElement>("opening-view");
            resultsView =
                root.Q<VisualElement>("results-view");

            // Inventory
            inventoryGrid =
                root.Q<VisualElement>("inventory-grid");
            inventoryCountLabel =
                root.Q<Label>("inventory-count-label");
            emptyState =
                root.Q<VisualElement>("empty-state");

            // Opening
            openingIcon =
                root.Q<VisualElement>("opening-icon");
            openingGlow =
                root.Q<VisualElement>("opening-glow");
            openingGlowRing =
                root.Q<VisualElement>("opening-glow-ring");
            openingName =
                root.Q<Label>("opening-name");
            openingRarity =
                root.Q<Label>("opening-rarity");
            openingHint =
                root.Q<Label>("opening-hint");

            // Results
            resultsCardsContainer =
                root.Q<VisualElement>(
                    "results-cards-container");
            summaryNewCount =
                root.Q<Label>("summary-new-count");
            summaryDupeCount =
                root.Q<Label>("summary-dupe-count");
            summaryCurrency =
                root.Q<Label>("summary-currency");
            summaryDuplicatesRow =
                root.Q<VisualElement>(
                    "summary-duplicates-row");
            btnOpenAnother =
                root.Q<Button>("btn-open-another");
            btnContinue =
                root.Q<Button>("btn-continue");

            // Feedback
            successPopup =
                root.Q<VisualElement>("success-popup");
            successText =
                root.Q<Label>("success-text");
            failPopup =
                root.Q<VisualElement>("fail-popup");
            failText = root.Q<Label>("fail-text");
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

            btnContinue?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    PlayClick();
                    CloseResults();
                });

            btnOpenAnother?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    PlayClick();
                    OpenAnotherLootbox();
                });
        }

        // ==========================================
        // EVENTS
        // ==========================================

        private void SubscribeEvents()
        {
            if (LootboxManager.Instance != null)
            {
                LootboxManager.Instance.OnLootboxOpened +=
                    OnLootboxOpened;
            }

            if (LootboxInventory.Instance != null)
            {
                LootboxInventory.Instance
                    .OnInventoryChanged +=
                    RefreshInventoryDisplay;
            }

            if (PlayerCollection.Instance != null)
            {
                PlayerCollection.Instance.OnGoldChanged +=
                    OnCurrencyChanged;
                PlayerCollection.Instance
                    .OnCrystalsChanged +=
                    OnCurrencyChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (LootboxManager.Instance != null)
            {
                LootboxManager.Instance.OnLootboxOpened -=
                    OnLootboxOpened;
            }

            if (LootboxInventory.Instance != null)
            {
                LootboxInventory.Instance
                    .OnInventoryChanged -=
                    RefreshInventoryDisplay;
            }

            if (PlayerCollection.Instance != null)
            {
                PlayerCollection.Instance.OnGoldChanged -=
                    OnCurrencyChanged;
                PlayerCollection.Instance
                    .OnCrystalsChanged -=
                    OnCurrencyChanged;
            }
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        public void Show()
        {
            if (root == null)
            {
                var uiDoc = GetComponent<UIDocument>();
                if (uiDoc != null)
                    root = uiDoc.rootVisualElement;
            }

            if (root == null)
            {
                Debug.LogError("[LootboxUI] Root is null!");
                return;
            }

            root.style.display = DisplayStyle.Flex;

            if (inventoryView == null)
            {
                QueryElements();
                BindButtons();
            }

            OpenLootboxMenu();

            // Starfield — inject into background
            var bg = root.Q<VisualElement>("lootbox-root");
            StarfieldInjector.Instance?.Register(bg);
        }

        public void Hide()
        {
            // Starfield — remove before hiding
            if (root != null)
            {
                var bg = root.Q<VisualElement>("lootbox-root");
                StarfieldInjector.Instance?.Unregister(bg);
            }

            if (root == null)
            {
                var uiDoc = GetComponent<UIDocument>();
                if (uiDoc != null)
                    root = uiDoc.rootVisualElement;
            }

            if (root != null)
            {
                root.style.display = DisplayStyle.None;
            }

            if (openingCoroutine != null)
            {
                StopCoroutine(openingCoroutine);
                openingCoroutine = null;
            }
        }

        public void OpenLootboxMenu()
        {
            ShowView(inventoryView);
            RefreshInventoryDisplay();
            RefreshCurrency();
        }

        public void TryOpenLootbox(LootboxData lootboxType)
        {
            if (lootboxType == null) return;

            if (LootboxManager.Instance == null ||
                !LootboxManager.Instance
                    .CanOpenLootbox(lootboxType))
            {
                ShowFeedback(failPopup, failText,
                    "✗ NO LOOTBOX AVAILABLE");
                return;
            }

            currentLootbox = lootboxType;
            openingCoroutine =
                StartCoroutine(
                    OpenLootboxSequence(lootboxType));
        }

        // ==========================================
        // VIEW MANAGEMENT
        // ==========================================

        private void ShowView(VisualElement view)
        {
            SetVisible(inventoryView,
                view == inventoryView);
            SetVisible(openingView,
                view == openingView);
            SetVisible(resultsView,
                view == resultsView);
        }

        private void SetVisible(
            VisualElement element, bool visible)
        {
            if (element == null) return;

            if (visible)
                element.RemoveFromClassList("hidden");
            else
                element.AddToClassList("hidden");
        }

        // ==========================================
        // INVENTORY DISPLAY
        // ==========================================

        private void RefreshInventoryDisplay()
        {
            if (inventoryGrid == null) return;

            inventoryGrid.Clear();

            if (LootboxInventory.Instance == null)
            {
                ShowEmptyState(true);
                UpdateCountLabel(0);
                return;
            }

            var owned =
                LootboxInventory.Instance
                    .GetOwnedLootboxes();

            if (owned == null || owned.Count == 0)
            {
                ShowEmptyState(true);
                UpdateCountLabel(0);
                return;
            }

            ShowEmptyState(false);

            int totalCount = 0;

            foreach (var entry in owned)
            {
                if (entry?.lootboxType == null) continue;

                var card =
                    BuildLootboxCard(
                        entry.lootboxType, entry.count);
                inventoryGrid.Add(card);
                totalCount += entry.count;
            }

            UpdateCountLabel(totalCount);
        }

        private void ShowEmptyState(bool show)
        {
            SetVisible(emptyState, show);
        }

        private void UpdateCountLabel(int count)
        {
            if (inventoryCountLabel != null)
                inventoryCountLabel.text =
                    $"YOUR COLLECTION — {count} LOOTBOX{(count != 1 ? "ES" : "")}";
        }

        // ==========================================
        // BUILD LOOTBOX CARD
        // ==========================================

        private VisualElement BuildLootboxCard(
            LootboxData lootbox, int count)
        {
            var card = new VisualElement();
            card.AddToClassList("lootbox-card");
            card.AddToClassList(
                GetRarityClass(lootbox.rarity));

            // Inner frame
            var innerFrame = new VisualElement();
            innerFrame.AddToClassList(
                "lootbox-card-inner-frame");
            card.Add(innerFrame);

            // Rarity stripe
            var stripe = new VisualElement();
            stripe.AddToClassList("lootbox-rarity-stripe");
            card.Add(stripe);

            // Icon section
            var iconSection = new VisualElement();
            iconSection.AddToClassList(
                "lootbox-icon-section");

            var iconGlow = new VisualElement();
            iconGlow.AddToClassList("lootbox-icon-glow");
            iconSection.Add(iconGlow);

            if (lootbox.lootboxIcon != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList("lootbox-icon");
                icon.style.backgroundImage =
                    new StyleBackground(
                        lootbox.lootboxIcon);
                iconSection.Add(icon);
            }

            // Rarity label
            var rarityLabel = new Label(
                lootbox.rarity.ToString().ToUpper());
            rarityLabel.AddToClassList(
                "lootbox-rarity-label");
            iconSection.Add(rarityLabel);

            card.Add(iconSection);

            // Info section
            var infoSection = new VisualElement();
            infoSection.AddToClassList(
                "lootbox-info-section");

            var nameLabel = new Label(lootbox.lootboxName);
            nameLabel.AddToClassList("lootbox-name");
            infoSection.Add(nameLabel);

            var cardCountLabel = new Label(
                $"{lootbox.cardCount} CARDS");
            cardCountLabel.AddToClassList(
                "lootbox-card-count");
            infoSection.Add(cardCountLabel);

            card.Add(infoSection);

            // Separator
            var sep = new VisualElement();
            sep.AddToClassList("lootbox-separator");
            card.Add(sep);

            // Bottom section
            var bottomSection = new VisualElement();
            bottomSection.AddToClassList(
                "lootbox-bottom-section");

            var qtyContainer = new VisualElement();
            qtyContainer.style.flexDirection =
                FlexDirection.Column;

            var qtyLabel = new Label($"x{count}");
            qtyLabel.AddToClassList("lootbox-quantity");
            qtyContainer.Add(qtyLabel);

            var ownedLabel = new Label("OWNED");
            ownedLabel.AddToClassList(
                "lootbox-quantity-label");
            qtyContainer.Add(ownedLabel);

            bottomSection.Add(qtyContainer);

            var openBtn = new Button();
            openBtn.text = "OPEN";
            openBtn.AddToClassList("btn-open");

            LootboxData captured = lootbox;
            openBtn.RegisterCallback<ClickEvent>(evt =>
            {
                PlayClick();
                TryOpenLootbox(captured);
                evt.StopPropagation();
            });

            bottomSection.Add(openBtn);
            card.Add(bottomSection);

            // Hover accent
            var accent = new VisualElement();
            accent.AddToClassList("lootbox-hover-accent");
            card.Add(accent);

            return card;
        }

        // ==========================================
        // OPENING SEQUENCE
        // ==========================================

        private IEnumerator OpenLootboxSequence(
            LootboxData lootboxType)
        {
            ShowView(openingView);

            // Setup visuals
            if (openingIcon != null &&
                lootboxType.lootboxIcon != null)
            {
                openingIcon.style.backgroundImage =
                    new StyleBackground(
                        lootboxType.lootboxIcon);
            }

            if (openingName != null)
                openingName.text =
                    lootboxType.lootboxName.ToUpper();

            if (openingRarity != null)
            {
                openingRarity.text =
                    lootboxType.rarity.ToString().ToUpper();
                openingRarity.style.color =
                    new StyleColor(
                        lootboxType.GetRarityColor());
            }

            if (openingHint != null)
                openingHint.text = "OPENING...";

            // Reset glow
            openingGlow?.RemoveFromClassList(
                "glow-active");
            openingGlowRing?.RemoveFromClassList(
                "glow-active");

            // Play opening sound
            if (openingSound != null)
                audioSource?.PlayOneShot(
                    openingSound, 0.6f);

            // Shake animation
            yield return StartCoroutine(
                ShakeAnimation());

            // Activate glow
            openingGlow?.AddToClassList("glow-active");
            openingGlowRing?.AddToClassList(
                "glow-active");

            yield return new WaitForSeconds(0.5f);

            // Actually open lootbox
            currentResult =
                LootboxManager.Instance
                    .OpenLootbox(lootboxType);

            if (currentResult == null)
            {
                ShowFeedback(failPopup, failText,
                    "✗ FAILED TO OPEN LOOTBOX");
                ShowView(inventoryView);
                RefreshInventoryDisplay();
                openingCoroutine = null;
                yield break;
            }

            yield return new WaitForSeconds(0.3f);

            // Transition to results
            ShowView(resultsView);
            yield return StartCoroutine(
                RevealCardsSequence());

            openingCoroutine = null;
        }

        private IEnumerator ShakeAnimation()
        {
            if (openingIcon == null) yield break;

            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float progress = elapsed / shakeDuration;
                float intensity =
                    shakeIntensity *
                    Mathf.Lerp(0.3f, 1f, progress);

                float x = Random.Range(-1f, 1f) *
                    intensity;
                float y = Random.Range(-1f, 1f) *
                    intensity;

                openingIcon.style.translate =
                    new StyleTranslate(
                        new Translate(x, y));

                // Pulse scale
                float scale = 1f +
                    Mathf.Sin(progress * Mathf.PI * 6f) *
                    0.03f * progress;
                openingIcon.style.scale =
                    new StyleScale(
                        new Scale(
                            new Vector3(
                                scale, scale, 1f)));

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Reset
            openingIcon.style.translate =
                new StyleTranslate(
                    new Translate(0, 0));
            openingIcon.style.scale =
                new StyleScale(
                    new Scale(Vector3.one));
        }

        // ==========================================
        // RESULTS
        // ==========================================

        private IEnumerator RevealCardsSequence()
        {
            if (resultsCardsContainer == null ||
                currentResult == null)
                yield break;

            resultsCardsContainer.Clear();

            // Build all cards hidden first
            var cardElements =
                new List<VisualElement>();

            foreach (var drop in currentResult.cardDrops)
            {
                if (drop?.card == null) continue;

                var card = BuildResultCard(drop);
                resultsCardsContainer.Add(card);
                cardElements.Add(card);
            }

            // Reveal one by one
            for (int i = 0; i < cardElements.Count; i++)
            {
                yield return new WaitForSeconds(
                    timeBetweenCards);

                cardElements[i].AddToClassList("revealed");

                // Play sound
                PlayRevealSound(
                    currentResult.cardDrops[i]);
            }

            // Summary
            yield return new WaitForSeconds(0.3f);
            UpdateSummary();

            // Open Another button
            if (btnOpenAnother != null &&
                currentLootbox != null)
            {
                bool hasMore =
                    LootboxManager.Instance?
                        .CanOpenLootbox(
                            currentLootbox) ?? false;
                SetVisible(btnOpenAnother, hasMore);
            }
        }

        private VisualElement BuildResultCard(
            CardDrop drop)
        {
            var card = new VisualElement();
            card.AddToClassList("result-card");

            // Rarity/duplicate class
            if (drop.wasDuplicate)
            {
                card.AddToClassList("result-duplicate");
            }
            else
            {
                card.AddToClassList(
                    GetResultRarityClass(
                        drop.card.rarity));
            }

            // Rarity stripe
            var stripe = new VisualElement();
            stripe.AddToClassList(
                "result-rarity-stripe");
            card.Add(stripe);

            // Badge
            var badge = new Label(
                drop.wasDuplicate
                    ? "DUPLICATE"
                    : "NEW");
            badge.AddToClassList("result-badge");
            badge.AddToClassList(
                drop.wasDuplicate
                    ? "result-badge-duplicate"
                    : "result-badge-new");
            card.Add(badge);

            // Icon
            var iconSection = new VisualElement();
            iconSection.AddToClassList(
                "result-card-icon-section");

            if (drop.card.cardIcon != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList("result-card-icon");
                icon.style.backgroundImage =
                    new StyleBackground(
                        drop.card.cardIcon);
                iconSection.Add(icon);
            }

            card.Add(iconSection);

            // Name
            var nameLabel =
                new Label(drop.card.cardName);
            nameLabel.AddToClassList(
                "result-card-name");
            card.Add(nameLabel);

            // Rarity
            var rarityLabel = new Label(
                drop.card.rarity.ToString().ToUpper());
            rarityLabel.AddToClassList(
                "result-card-rarity");
            rarityLabel.style.color =
                new StyleColor(
                    GetRarityColor(drop.card.rarity));
            card.Add(rarityLabel);

            // Duplicate currency
            if (drop.wasDuplicate &&
                drop.currencyEarned > 0)
            {
                var currencyLabel = new Label(
                    $"+{drop.currencyEarned}");
                currencyLabel.AddToClassList(
                    "result-currency-earned");
                card.Add(currencyLabel);
            }

            return card;
        }

        private void UpdateSummary()
        {
            if (currentResult == null) return;

            if (summaryNewCount != null)
                summaryNewCount.text =
                    currentResult.newCardsUnlocked
                        .ToString();

            if (currentResult.duplicatesConverted > 0)
            {
                SetVisible(summaryDuplicatesRow, true);

                if (summaryDupeCount != null)
                    summaryDupeCount.text =
                        currentResult.duplicatesConverted
                            .ToString();

                if (summaryCurrency != null)
                    summaryCurrency.text =
                        $"+{currentResult.totalDuplicateCurrency} GOLD";
            }
            else
            {
                SetVisible(summaryDuplicatesRow, false);
            }
        }

        // ==========================================
        // CALLBACKS
        // ==========================================

        private void CloseResults()
        {
            ShowView(inventoryView);
            RefreshInventoryDisplay();
            RefreshCurrency();
            currentResult = null;
        }

        private void OpenAnotherLootbox()
        {
            if (currentLootbox != null)
            {
                currentResult = null;
                TryOpenLootbox(currentLootbox);
            }
        }

        // ==========================================
        // EVENT HANDLERS
        // ==========================================

        private void OnLootboxOpened(
            LootboxResult result)
        {
            Debug.Log(
                $"[LootboxUI] Opened: " +
                $"{result.newCardsUnlocked} new, " +
                $"{result.duplicatesConverted} dupes");
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
                    PlayerCollection.Instance
                        .GetCrystals());
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
            yield return new WaitForSeconds(
                popupDuration);
            popup?.AddToClassList("hidden");
            feedbackCoroutine = null;
        }

        // ==========================================
        // AUDIO
        // ==========================================

        private void PlayClick()
        {
            if (buttonClickSound != null)
                audioSource?.PlayOneShot(
                    buttonClickSound, 0.7f);
        }

        private void PlayRevealSound(CardDrop drop)
        {
            if (audioSource == null) return;

            AudioClip clip = null;

            if (drop.wasDuplicate)
                clip = duplicateSound;
            else if (drop.card.rarity ==
                CardRarity.Legendary)
                clip = legendaryRevealSound;
            else
                clip = cardRevealSound;

            if (clip != null)
                audioSource.PlayOneShot(clip, 0.8f);
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private Color GetRarityColor(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Common => commonColor,
                CardRarity.Rare => rareColor,
                CardRarity.Legendary => legendaryColor,
                _ => Color.white
            };
        }

        private string GetRarityClass(
            LootboxRarity rarity)
        {
            return rarity switch
            {
                LootboxRarity.Common => "rarity-common",
                LootboxRarity.Rare => "rarity-rare",
                LootboxRarity.Epic => "rarity-epic",
                LootboxRarity.Legendary =>
                    "rarity-legendary",
                LootboxRarity.Event => "rarity-event",
                _ => "rarity-common"
            };
        }

        private string GetResultRarityClass(
            CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Common => "result-common",
                CardRarity.Rare => "result-rare",
                CardRarity.Legendary =>
                    "result-legendary",
                _ => "result-common"
            };
        }

        private string FormatNumber(int number)
        {
            if (number >= 1000000)
                return $"{number / 1000000f:F1}M";
            if (number >= 1000)
                return $"{number / 1000f:F1}K";
            return number.ToString("N0");
        }
    }
}