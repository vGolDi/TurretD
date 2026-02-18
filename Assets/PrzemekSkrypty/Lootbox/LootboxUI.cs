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
            Debug.Log("[LootboxUI] Show() called");

            if (root == null)
            {
                var uiDoc = GetComponent<UIDocument>();
                Debug.Log($"[LootboxUI] UIDocument: {uiDoc != null}");

                if (uiDoc != null)
                {
                    Debug.Log($"[LootboxUI] UXML asset: {uiDoc.visualTreeAsset != null}");
                    Debug.Log($"[LootboxUI] rootVisualElement: {uiDoc.rootVisualElement != null}");
                    root = uiDoc.rootVisualElement;
                }
            }

            if (root == null)
            {
                Debug.LogError("[LootboxUI] ROOT IS STILL NULL!");
                return;
            }

            Debug.Log($"[LootboxUI] root childCount: {root.childCount}");
            Debug.Log($"[LootboxUI] root display BEFORE: {root.resolvedStyle.display}");

            root.style.display = DisplayStyle.Flex;

            Debug.Log($"[LootboxUI] root display AFTER: {root.style.display.value}");
            Debug.Log($"[LootboxUI] root panel: {root.panel != null}");

            // Check parent chain visibility
            var current = root.parent;
            int depth = 0;
            while (current != null && depth < 5)
            {
                Debug.Log($"[LootboxUI] Parent[{depth}] display: {current.resolvedStyle.display}, name: {current.name}");
                current = current.parent;
                depth++;
            }

            if (inventoryView == null)
            {
                QueryElements();
                BindButtons();
            }

            OpenLootboxMenu();
        }

        public void Hide()
        {
            // Ensure root is valid
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
//// Assets/PrzemekSkrypty/UI/LootboxUI.cs
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections;
//using System.Collections.Generic;
//using ElementumDefense.Lootbox;
//using ElementumDefense.Cards;

//namespace ElementumDefense.UI
//{
//    /// <summary>
//    /// UI for lootbox opening screen
//    /// Shows inventory, handles opening animation, displays results
//    /// </summary>
//    public class LootboxUI : MonoBehaviour
//    {
//        [Header("Panels")]
//        [SerializeField] private GameObject lootboxPanel;
//        [SerializeField] private GameObject inventoryPanel;
//        [SerializeField] private GameObject openingPanel;
//        [SerializeField] private GameObject resultsPanel;

//        [Header("Inventory Display")]
//        [SerializeField] private Transform lootboxListContainer;
//        [SerializeField] private GameObject lootboxSlotPrefab;

//        [Header("Opening Animation")]
//        [SerializeField] private Image lootboxImage;
//        [SerializeField] private Image glowEffect;
//        [SerializeField] private Animator lootboxAnimator;
//        [SerializeField] private float shakeDuration = 1f;
//        [SerializeField] private float shakeIntensity = 10f;

//        [Header("Results Display")]
//        [SerializeField] private Transform cardResultsContainer;
//        [SerializeField] private GameObject cardResultPrefab;
//        [SerializeField] private TMP_Text summaryText;
//        [SerializeField] private TMP_Text duplicateCurrencyText;
//        [SerializeField] private Button continueButton;
//        [SerializeField] private Button openAnotherButton;

//        [Header("Card Reveal")]
//        [SerializeField] private float timeBetweenCards = 0.5f;
//        [SerializeField] private AudioClip cardRevealSound;
//        [SerializeField] private AudioClip legendaryRevealSound;
//        [SerializeField] private AudioClip duplicateSound;

//        [Header("Colors")]
//        [SerializeField] private Color commonColor = new Color(0.7f, 0.7f, 0.7f);
//        [SerializeField] private Color rareColor = new Color(0.3f, 0.5f, 1f);
//        [SerializeField] private Color legendaryColor = new Color(1f, 0.8f, 0f);
//        [SerializeField] private Color duplicateColor = new Color(0.5f, 0.5f, 0.5f);

//        private AudioSource audioSource;
//        private LootboxData currentLootbox;
//        private LootboxResult currentResult;
//        private List<GameObject> spawnedSlots = new List<GameObject>();
//        private List<GameObject> spawnedCards = new List<GameObject>();

//        // ==========================================
//        // INITIALIZATION
//        // ==========================================

//        private void Awake()
//        {
//            audioSource = GetComponent<AudioSource>();
//            if (audioSource == null)
//            {
//                audioSource = gameObject.AddComponent<AudioSource>();
//            }
//        }

//        private void Start()
//        {
//            // Subscribe to events
//            if (LootboxManager.Instance != null)
//            {
//                LootboxManager.Instance.OnLootboxOpened += OnLootboxOpened;
//                LootboxManager.Instance.OnCardRevealed += OnCardRevealed;
//            }

//            if (LootboxInventory.Instance != null)
//            {
//                LootboxInventory.Instance.OnInventoryChanged += RefreshInventoryDisplay;
//            }

//            // Setup buttons
//            if (continueButton != null)
//            {
//                continueButton.onClick.AddListener(CloseResults);
//            }

//            if (openAnotherButton != null)
//            {
//                openAnotherButton.onClick.AddListener(OpenAnotherLootbox);
//            }

//            // Initial state
//            ShowPanel(inventoryPanel);
//            RefreshInventoryDisplay();
//        }

//        private void OnDestroy()
//        {
//            if (LootboxManager.Instance != null)
//            {
//                LootboxManager.Instance.OnLootboxOpened -= OnLootboxOpened;
//                LootboxManager.Instance.OnCardRevealed -= OnCardRevealed;
//            }

//            if (LootboxInventory.Instance != null)
//            {
//                LootboxInventory.Instance.OnInventoryChanged -= RefreshInventoryDisplay;
//            }
//        }

//        // ==========================================
//        // PUBLIC API
//        // ==========================================

//        /// <summary>
//        /// Opens the lootbox UI panel
//        /// </summary>
//        public void OpenLootboxMenu()
//        {
//            lootboxPanel.SetActive(true);
//            ShowPanel(inventoryPanel);
//            RefreshInventoryDisplay();
//        }

//        /// <summary>
//        /// Closes the lootbox UI
//        /// </summary>
//        public void CloseLootboxMenu()
//        {
//            lootboxPanel.SetActive(false);
//        }

//        /// <summary>
//        /// Attempts to open specified lootbox
//        /// </summary>
//        public void TryOpenLootbox(LootboxData lootboxType)
//        {
//            if (lootboxType == null) return;

//            if (!LootboxManager.Instance.CanOpenLootbox(lootboxType))
//            {
//                Debug.LogWarning("[LootboxUI] Cannot open this lootbox!");
//                return;
//            }

//            currentLootbox = lootboxType;
//            StartCoroutine(OpenLootboxSequence(lootboxType));
//        }

//        // ==========================================
//        // INVENTORY DISPLAY
//        // ==========================================

//        /// <summary>
//        /// Refreshes lootbox inventory list
//        /// </summary>
//        private void RefreshInventoryDisplay()
//        {
//            // Clear old slots
//            foreach (var slot in spawnedSlots)
//            {
//                Destroy(slot);
//            }
//            spawnedSlots.Clear();

//            if (LootboxInventory.Instance == null) return;

//            // Get owned lootboxes
//            List<LootboxInventoryEntry> owned = LootboxInventory.Instance.GetOwnedLootboxes();

//            // Spawn slots
//            foreach (var entry in owned)
//            {
//                GameObject slot = Instantiate(lootboxSlotPrefab, lootboxListContainer);
//                spawnedSlots.Add(slot);

//                // Setup slot UI
//                LootboxSlotUI slotUI = slot.GetComponent<LootboxSlotUI>();
//                if (slotUI != null)
//                {
//                    slotUI.Setup(entry.lootboxType, entry.count, this);
//                }
//                else
//                {
//                    // Fallback: manual setup
//                    SetupSlotManually(slot, entry);
//                }
//            }

//            // Show "no lootboxes" message if empty
//            if (owned.Count == 0)
//            {
//                Debug.Log("[LootboxUI] No lootboxes to display");
//            }
//        }

//        private void SetupSlotManually(GameObject slot, LootboxInventoryEntry entry)
//        {
//            // Icon
//            Image icon = slot.transform.Find("Icon")?.GetComponent<Image>();
//            if (icon != null && entry.lootboxType.lootboxIcon != null)
//            {
//                icon.sprite = entry.lootboxType.lootboxIcon;
//            }

//            // Name
//            TMP_Text nameText = slot.transform.Find("Name")?.GetComponent<TMP_Text>();
//            if (nameText != null)
//            {
//                nameText.text = entry.lootboxType.lootboxName;
//            }

//            // Count
//            TMP_Text countText = slot.transform.Find("Count")?.GetComponent<TMP_Text>();
//            if (countText != null)
//            {
//                countText.text = $"x{entry.count}";
//            }

//            // Button
//            Button openButton = slot.GetComponentInChildren<Button>();
//            if (openButton != null)
//            {
//                LootboxData capturedLootbox = entry.lootboxType;
//                openButton.onClick.AddListener(() => TryOpenLootbox(capturedLootbox));
//            }
//        }

//        // ==========================================
//        // OPENING ANIMATION
//        // ==========================================

//        private IEnumerator OpenLootboxSequence(LootboxData lootboxType)
//        {
//            ShowPanel(openingPanel);

//            // Setup visuals
//            if (lootboxImage != null && lootboxType.lootboxIcon != null)
//            {
//                lootboxImage.sprite = lootboxType.lootboxIcon;
//            }

//            if (glowEffect != null)
//            {
//                glowEffect.color = lootboxType.GetRarityColor();
//            }

//            // Shake animation
//            yield return StartCoroutine(ShakeLootbox());

//            // Open animation (trigger animator if exists)
//            if (lootboxAnimator != null)
//            {
//                lootboxAnimator.SetTrigger("Open");
//                yield return new WaitForSeconds(0.5f);
//            }

//            // Actually open the lootbox
//            currentResult = LootboxManager.Instance.OpenLootbox(lootboxType);

//            // Wait for opening sound
//            yield return new WaitForSeconds(0.3f);

//            // Show results
//            ShowPanel(resultsPanel);
//            StartCoroutine(RevealCardsSequence());
//        }

//        private IEnumerator ShakeLootbox()
//        {
//            if (lootboxImage == null) yield break;

//            Vector3 originalPosition = lootboxImage.transform.localPosition;
//            float elapsed = 0f;

//            while (elapsed < shakeDuration)
//            {
//                float x = Random.Range(-1f, 1f) * shakeIntensity;
//                float y = Random.Range(-1f, 1f) * shakeIntensity;

//                lootboxImage.transform.localPosition = originalPosition + new Vector3(x, y, 0);

//                // Increase glow
//                if (glowEffect != null)
//                {
//                    float t = elapsed / shakeDuration;
//                    glowEffect.color = new Color(
//                        glowEffect.color.r,
//                        glowEffect.color.g,
//                        glowEffect.color.b,
//                        Mathf.Lerp(0.3f, 1f, t)
//                    );
//                }

//                elapsed += Time.deltaTime;
//                yield return null;
//            }

//            lootboxImage.transform.localPosition = originalPosition;
//        }

//        // ==========================================
//        // RESULTS DISPLAY
//        // ==========================================

//        private IEnumerator RevealCardsSequence()
//        {
//            // Clear old cards
//            foreach (var card in spawnedCards)
//            {
//                Destroy(card);
//            }
//            spawnedCards.Clear();

//            if (currentResult == null) yield break;

//            // Reveal each card with delay
//            for (int i = 0; i < currentResult.cardDrops.Count; i++)
//            {
//                CardDrop drop = currentResult.cardDrops[i];

//                // Spawn card
//                GameObject cardObj = Instantiate(cardResultPrefab, cardResultsContainer);
//                spawnedCards.Add(cardObj);

//                // Setup card display
//                SetupCardResult(cardObj, drop);

//                // Play sound
//                PlayRevealSound(drop);

//                // Scale animation
//                StartCoroutine(CardPopAnimation(cardObj.transform));

//                yield return new WaitForSeconds(timeBetweenCards);
//            }

//            // Show summary
//            if (summaryText != null)
//            {
//                summaryText.text = $"New Cards: {currentResult.newCardsUnlocked}";
//            }

//            if (duplicateCurrencyText != null)
//            {
//                if (currentResult.duplicatesConverted > 0)
//                {
//                    duplicateCurrencyText.gameObject.SetActive(true);
//                    duplicateCurrencyText.text = $"Duplicates: +{currentResult.totalDuplicateCurrency} 💰";
//                }
//                else
//                {
//                    duplicateCurrencyText.gameObject.SetActive(false);
//                }
//            }

//            // Show open another button if player has more
//            if (openAnotherButton != null && currentLootbox != null)
//            {
//                bool hasMore = LootboxManager.Instance.CanOpenLootbox(currentLootbox);
//                openAnotherButton.gameObject.SetActive(hasMore);
//            }
//        }

//        private void SetupCardResult(GameObject cardObj, CardDrop drop)
//        {
//            // Icon
//            Image icon = cardObj.transform.Find("CardIcon")?.GetComponent<Image>();
//            if (icon != null && drop.card.cardIcon != null)
//            {
//                icon.sprite = drop.card.cardIcon;
//            }

//            // Name
//            TMP_Text nameText = cardObj.transform.Find("CardName")?.GetComponent<TMP_Text>();
//            if (nameText != null)
//            {
//                nameText.text = drop.card.cardName;
//            }

//            // Rarity
//            TMP_Text rarityText = cardObj.transform.Find("Rarity")?.GetComponent<TMP_Text>();
//            Image border = cardObj.transform.Find("RarityBorder")?.GetComponent<Image>();
//            Image botLine = cardObj.transform.Find("LineBot")?.GetComponent<Image>();
//            Image topLine = cardObj.transform.Find("LineTop")?.GetComponent<Image>();
//            if (rarityText != null)
//            {
//                rarityText.text = drop.card.rarity.ToString();
//                rarityText.color = GetRarityColor(drop.card.rarity);
//                border.color = GetRarityColor(drop.card.rarity);
//                botLine.color = GetRarityColor(drop.card.rarity);
//                topLine.color = GetRarityColor(drop.card.rarity);
//            }

//            // Duplicate indicator
//            GameObject duplicateBadge = cardObj.transform.Find("DuplicateBadge")?.gameObject;
//            if (duplicateBadge != null)
//            {
//                duplicateBadge.SetActive(drop.wasDuplicate);
//            }

//            TMP_Text currencyText = cardObj.transform.Find("CurrencyEarned")?.GetComponent<TMP_Text>();
//            if (currencyText != null)
//            {
//                if (drop.wasDuplicate)
//                {
//                    currencyText.gameObject.SetActive(true);
//                    currencyText.text = $"+{drop.currencyEarned}";
//                }
//                else
//                {
//                    currencyText.gameObject.SetActive(false);
//                }
//            }

//            // Background color
//            Image background = cardObj.GetComponent<Image>();
//            if (background != null)
//            {
//                if (drop.wasDuplicate)
//                {
//                    background.color = duplicateColor;
//                }
//                else
//                {
//                    background.color = GetRarityColor(drop.card.rarity);
//                }
//            }

//            // NEW badge
//            GameObject newBadge = cardObj.transform.Find("NewBadge")?.gameObject;
//            if (newBadge != null)
//            {
//                newBadge.SetActive(!drop.wasDuplicate);
//            }
//        }

//        private IEnumerator CardPopAnimation(Transform cardTransform)
//        {
//            cardTransform.localScale = Vector3.zero;

//            float duration = 0.3f;
//            float elapsed = 0f;

//            while (elapsed < duration)
//            {
//                float t = elapsed / duration;
//                float scale = Mathf.LerpUnclamped(0f, 1f, EaseOutBack(t));
//                cardTransform.localScale = Vector3.one * scale;

//                elapsed += Time.deltaTime;
//                yield return null;
//            }

//            cardTransform.localScale = Vector3.one;
//        }

//        private float EaseOutBack(float t)
//        {
//            float c1 = 1.70158f;
//            float c3 = c1 + 1f;
//            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
//        }

//        private void PlayRevealSound(CardDrop drop)
//        {
//            if (audioSource == null) return;

//            AudioClip clip = null;

//            if (drop.wasDuplicate)
//            {
//                clip = duplicateSound;
//            }
//            else if (drop.card.rarity == CardRarity.Legendary)
//            {
//                clip = legendaryRevealSound;
//            }
//            else
//            {
//                clip = cardRevealSound;
//            }

//            if (clip != null)
//            {
//                audioSource.PlayOneShot(clip);
//            }
//        }

//        private Color GetRarityColor(CardRarity rarity)
//        {
//            return rarity switch
//            {
//                CardRarity.Common => commonColor,
//                CardRarity.Rare => rareColor,
//                CardRarity.Legendary => legendaryColor,
//                _ => Color.white
//            };
//        }

//        // ==========================================
//        // UI CALLBACKS
//        // ==========================================

//        private void CloseResults()
//        {
//            ShowPanel(inventoryPanel);
//            RefreshInventoryDisplay();
//            currentResult = null;
//        }

//        private void OpenAnotherLootbox()
//        {
//            if (currentLootbox != null)
//            {
//                TryOpenLootbox(currentLootbox);
//            }
//        }

//        private void ShowPanel(GameObject panel)
//        {
//            inventoryPanel?.SetActive(panel == inventoryPanel);
//            openingPanel?.SetActive(panel == openingPanel);
//            resultsPanel?.SetActive(panel == resultsPanel);
//        }

//        // ==========================================
//        // EVENT HANDLERS
//        // ==========================================

//        private void OnLootboxOpened(LootboxResult result)
//        {
//            Debug.Log($"[LootboxUI] Lootbox opened: {result.newCardsUnlocked} new, {result.duplicatesConverted} duplicates");
//        }

//        private void OnCardRevealed(CardDrop drop, int index)
//        {
//            Debug.Log($"[LootboxUI] Card revealed [{index}]: {drop.card.cardName} (duplicate: {drop.wasDuplicate})");
//        }
//    }
//}