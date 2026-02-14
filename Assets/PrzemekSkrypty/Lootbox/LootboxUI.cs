// Assets/PrzemekSkrypty/UI/LootboxUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using ElementumDefense.Lootbox;
using ElementumDefense.Cards;

namespace ElementumDefense.UI
{
    /// <summary>
    /// UI for lootbox opening screen
    /// Shows inventory, handles opening animation, displays results
    /// </summary>
    public class LootboxUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject lootboxPanel;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject openingPanel;
        [SerializeField] private GameObject resultsPanel;

        [Header("Inventory Display")]
        [SerializeField] private Transform lootboxListContainer;
        [SerializeField] private GameObject lootboxSlotPrefab;

        [Header("Opening Animation")]
        [SerializeField] private Image lootboxImage;
        [SerializeField] private Image glowEffect;
        [SerializeField] private Animator lootboxAnimator;
        [SerializeField] private float shakeDuration = 1f;
        [SerializeField] private float shakeIntensity = 10f;

        [Header("Results Display")]
        [SerializeField] private Transform cardResultsContainer;
        [SerializeField] private GameObject cardResultPrefab;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text duplicateCurrencyText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button openAnotherButton;

        [Header("Card Reveal")]
        [SerializeField] private float timeBetweenCards = 0.5f;
        [SerializeField] private AudioClip cardRevealSound;
        [SerializeField] private AudioClip legendaryRevealSound;
        [SerializeField] private AudioClip duplicateSound;

        [Header("Colors")]
        [SerializeField] private Color commonColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] private Color rareColor = new Color(0.3f, 0.5f, 1f);
        [SerializeField] private Color legendaryColor = new Color(1f, 0.8f, 0f);
        [SerializeField] private Color duplicateColor = new Color(0.5f, 0.5f, 0.5f);

        private AudioSource audioSource;
        private LootboxData currentLootbox;
        private LootboxResult currentResult;
        private List<GameObject> spawnedSlots = new List<GameObject>();
        private List<GameObject> spawnedCards = new List<GameObject>();

        // ==========================================
        // INITIALIZATION
        // ==========================================

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        private void Start()
        {
            // Subscribe to events
            if (LootboxManager.Instance != null)
            {
                LootboxManager.Instance.OnLootboxOpened += OnLootboxOpened;
                LootboxManager.Instance.OnCardRevealed += OnCardRevealed;
            }

            if (LootboxInventory.Instance != null)
            {
                LootboxInventory.Instance.OnInventoryChanged += RefreshInventoryDisplay;
            }

            // Setup buttons
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(CloseResults);
            }

            if (openAnotherButton != null)
            {
                openAnotherButton.onClick.AddListener(OpenAnotherLootbox);
            }

            // Initial state
            ShowPanel(inventoryPanel);
            RefreshInventoryDisplay();
        }

        private void OnDestroy()
        {
            if (LootboxManager.Instance != null)
            {
                LootboxManager.Instance.OnLootboxOpened -= OnLootboxOpened;
                LootboxManager.Instance.OnCardRevealed -= OnCardRevealed;
            }

            if (LootboxInventory.Instance != null)
            {
                LootboxInventory.Instance.OnInventoryChanged -= RefreshInventoryDisplay;
            }
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        /// <summary>
        /// Opens the lootbox UI panel
        /// </summary>
        public void OpenLootboxMenu()
        {
            lootboxPanel.SetActive(true);
            ShowPanel(inventoryPanel);
            RefreshInventoryDisplay();
        }

        /// <summary>
        /// Closes the lootbox UI
        /// </summary>
        public void CloseLootboxMenu()
        {
            lootboxPanel.SetActive(false);
        }

        /// <summary>
        /// Attempts to open specified lootbox
        /// </summary>
        public void TryOpenLootbox(LootboxData lootboxType)
        {
            if (lootboxType == null) return;

            if (!LootboxManager.Instance.CanOpenLootbox(lootboxType))
            {
                Debug.LogWarning("[LootboxUI] Cannot open this lootbox!");
                return;
            }

            currentLootbox = lootboxType;
            StartCoroutine(OpenLootboxSequence(lootboxType));
        }

        // ==========================================
        // INVENTORY DISPLAY
        // ==========================================

        /// <summary>
        /// Refreshes lootbox inventory list
        /// </summary>
        private void RefreshInventoryDisplay()
        {
            // Clear old slots
            foreach (var slot in spawnedSlots)
            {
                Destroy(slot);
            }
            spawnedSlots.Clear();

            if (LootboxInventory.Instance == null) return;

            // Get owned lootboxes
            List<LootboxInventoryEntry> owned = LootboxInventory.Instance.GetOwnedLootboxes();

            // Spawn slots
            foreach (var entry in owned)
            {
                GameObject slot = Instantiate(lootboxSlotPrefab, lootboxListContainer);
                spawnedSlots.Add(slot);

                // Setup slot UI
                LootboxSlotUI slotUI = slot.GetComponent<LootboxSlotUI>();
                if (slotUI != null)
                {
                    slotUI.Setup(entry.lootboxType, entry.count, this);
                }
                else
                {
                    // Fallback: manual setup
                    SetupSlotManually(slot, entry);
                }
            }

            // Show "no lootboxes" message if empty
            if (owned.Count == 0)
            {
                Debug.Log("[LootboxUI] No lootboxes to display");
            }
        }

        private void SetupSlotManually(GameObject slot, LootboxInventoryEntry entry)
        {
            // Icon
            Image icon = slot.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && entry.lootboxType.lootboxIcon != null)
            {
                icon.sprite = entry.lootboxType.lootboxIcon;
            }

            // Name
            TMP_Text nameText = slot.transform.Find("Name")?.GetComponent<TMP_Text>();
            if (nameText != null)
            {
                nameText.text = entry.lootboxType.lootboxName;
            }

            // Count
            TMP_Text countText = slot.transform.Find("Count")?.GetComponent<TMP_Text>();
            if (countText != null)
            {
                countText.text = $"x{entry.count}";
            }

            // Button
            Button openButton = slot.GetComponentInChildren<Button>();
            if (openButton != null)
            {
                LootboxData capturedLootbox = entry.lootboxType;
                openButton.onClick.AddListener(() => TryOpenLootbox(capturedLootbox));
            }
        }

        // ==========================================
        // OPENING ANIMATION
        // ==========================================

        private IEnumerator OpenLootboxSequence(LootboxData lootboxType)
        {
            ShowPanel(openingPanel);

            // Setup visuals
            if (lootboxImage != null && lootboxType.lootboxIcon != null)
            {
                lootboxImage.sprite = lootboxType.lootboxIcon;
            }

            if (glowEffect != null)
            {
                glowEffect.color = lootboxType.GetRarityColor();
            }

            // Shake animation
            yield return StartCoroutine(ShakeLootbox());

            // Open animation (trigger animator if exists)
            if (lootboxAnimator != null)
            {
                lootboxAnimator.SetTrigger("Open");
                yield return new WaitForSeconds(0.5f);
            }

            // Actually open the lootbox
            currentResult = LootboxManager.Instance.OpenLootbox(lootboxType);

            // Wait for opening sound
            yield return new WaitForSeconds(0.3f);

            // Show results
            ShowPanel(resultsPanel);
            StartCoroutine(RevealCardsSequence());
        }

        private IEnumerator ShakeLootbox()
        {
            if (lootboxImage == null) yield break;

            Vector3 originalPosition = lootboxImage.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                float x = Random.Range(-1f, 1f) * shakeIntensity;
                float y = Random.Range(-1f, 1f) * shakeIntensity;

                lootboxImage.transform.localPosition = originalPosition + new Vector3(x, y, 0);

                // Increase glow
                if (glowEffect != null)
                {
                    float t = elapsed / shakeDuration;
                    glowEffect.color = new Color(
                        glowEffect.color.r,
                        glowEffect.color.g,
                        glowEffect.color.b,
                        Mathf.Lerp(0.3f, 1f, t)
                    );
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            lootboxImage.transform.localPosition = originalPosition;
        }

        // ==========================================
        // RESULTS DISPLAY
        // ==========================================

        private IEnumerator RevealCardsSequence()
        {
            // Clear old cards
            foreach (var card in spawnedCards)
            {
                Destroy(card);
            }
            spawnedCards.Clear();

            if (currentResult == null) yield break;

            // Reveal each card with delay
            for (int i = 0; i < currentResult.cardDrops.Count; i++)
            {
                CardDrop drop = currentResult.cardDrops[i];

                // Spawn card
                GameObject cardObj = Instantiate(cardResultPrefab, cardResultsContainer);
                spawnedCards.Add(cardObj);

                // Setup card display
                SetupCardResult(cardObj, drop);

                // Play sound
                PlayRevealSound(drop);

                // Scale animation
                StartCoroutine(CardPopAnimation(cardObj.transform));

                yield return new WaitForSeconds(timeBetweenCards);
            }

            // Show summary
            if (summaryText != null)
            {
                summaryText.text = $"New Cards: {currentResult.newCardsUnlocked}";
            }

            if (duplicateCurrencyText != null)
            {
                if (currentResult.duplicatesConverted > 0)
                {
                    duplicateCurrencyText.gameObject.SetActive(true);
                    duplicateCurrencyText.text = $"Duplicates: +{currentResult.totalDuplicateCurrency} 💰";
                }
                else
                {
                    duplicateCurrencyText.gameObject.SetActive(false);
                }
            }

            // Show open another button if player has more
            if (openAnotherButton != null && currentLootbox != null)
            {
                bool hasMore = LootboxManager.Instance.CanOpenLootbox(currentLootbox);
                openAnotherButton.gameObject.SetActive(hasMore);
            }
        }

        private void SetupCardResult(GameObject cardObj, CardDrop drop)
        {
            // Icon
            Image icon = cardObj.transform.Find("CardIcon")?.GetComponent<Image>();
            if (icon != null && drop.card.cardIcon != null)
            {
                icon.sprite = drop.card.cardIcon;
            }

            // Name
            TMP_Text nameText = cardObj.transform.Find("CardName")?.GetComponent<TMP_Text>();
            if (nameText != null)
            {
                nameText.text = drop.card.cardName;
            }

            // Rarity
            TMP_Text rarityText = cardObj.transform.Find("Rarity")?.GetComponent<TMP_Text>();
            Image border = cardObj.transform.Find("RarityBorder")?.GetComponent<Image>();
            Image botLine = cardObj.transform.Find("LineBot")?.GetComponent<Image>();
            Image topLine = cardObj.transform.Find("LineTop")?.GetComponent<Image>();
            if (rarityText != null)
            {
                rarityText.text = drop.card.rarity.ToString();
                rarityText.color = GetRarityColor(drop.card.rarity);
                border.color = GetRarityColor(drop.card.rarity);
                botLine.color = GetRarityColor(drop.card.rarity);
                topLine.color = GetRarityColor(drop.card.rarity);
            }

            // Duplicate indicator
            GameObject duplicateBadge = cardObj.transform.Find("DuplicateBadge")?.gameObject;
            if (duplicateBadge != null)
            {
                duplicateBadge.SetActive(drop.wasDuplicate);
            }

            TMP_Text currencyText = cardObj.transform.Find("CurrencyEarned")?.GetComponent<TMP_Text>();
            if (currencyText != null)
            {
                if (drop.wasDuplicate)
                {
                    currencyText.gameObject.SetActive(true);
                    currencyText.text = $"+{drop.currencyEarned}";
                }
                else
                {
                    currencyText.gameObject.SetActive(false);
                }
            }

            // Background color
            Image background = cardObj.GetComponent<Image>();
            if (background != null)
            {
                if (drop.wasDuplicate)
                {
                    background.color = duplicateColor;
                }
                else
                {
                    background.color = GetRarityColor(drop.card.rarity);
                }
            }

            // NEW badge
            GameObject newBadge = cardObj.transform.Find("NewBadge")?.gameObject;
            if (newBadge != null)
            {
                newBadge.SetActive(!drop.wasDuplicate);
            }
        }

        private IEnumerator CardPopAnimation(Transform cardTransform)
        {
            cardTransform.localScale = Vector3.zero;

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float scale = Mathf.LerpUnclamped(0f, 1f, EaseOutBack(t));
                cardTransform.localScale = Vector3.one * scale;

                elapsed += Time.deltaTime;
                yield return null;
            }

            cardTransform.localScale = Vector3.one;
        }

        private float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private void PlayRevealSound(CardDrop drop)
        {
            if (audioSource == null) return;

            AudioClip clip = null;

            if (drop.wasDuplicate)
            {
                clip = duplicateSound;
            }
            else if (drop.card.rarity == CardRarity.Legendary)
            {
                clip = legendaryRevealSound;
            }
            else
            {
                clip = cardRevealSound;
            }

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

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

        // ==========================================
        // UI CALLBACKS
        // ==========================================

        private void CloseResults()
        {
            ShowPanel(inventoryPanel);
            RefreshInventoryDisplay();
            currentResult = null;
        }

        private void OpenAnotherLootbox()
        {
            if (currentLootbox != null)
            {
                TryOpenLootbox(currentLootbox);
            }
        }

        private void ShowPanel(GameObject panel)
        {
            inventoryPanel?.SetActive(panel == inventoryPanel);
            openingPanel?.SetActive(panel == openingPanel);
            resultsPanel?.SetActive(panel == resultsPanel);
        }

        // ==========================================
        // EVENT HANDLERS
        // ==========================================

        private void OnLootboxOpened(LootboxResult result)
        {
            Debug.Log($"[LootboxUI] Lootbox opened: {result.newCardsUnlocked} new, {result.duplicatesConverted} duplicates");
        }

        private void OnCardRevealed(CardDrop drop, int index)
        {
            Debug.Log($"[LootboxUI] Card revealed [{index}]: {drop.card.cardName} (duplicate: {drop.wasDuplicate})");
        }
    }
}