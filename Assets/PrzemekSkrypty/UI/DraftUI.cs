using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

        // ========== NOWE: Retry system ==========
        private float initializationRetryTimer = 0f;
        private const float RETRY_INTERVAL = 0.5f; // Check every 0.5s
        // ========================================

        private void Start()
        {
            Debug.Log("[DraftUI] Waiting for DraftManager to spawn...");
            HideAllPanels();
        }

        private void Update()
        {
            // ========== NOWE: Retry initialization ==========
            if (!isInitialized)
            {
                initializationRetryTimer += Time.deltaTime;

                if (initializationRetryTimer >= RETRY_INTERVAL)
                {
                    initializationRetryTimer = 0f;
                    TryInitialize();
                }

                return; // Don't do anything else until initialized
            }
            // ================================================
        }

        /// <summary>
        /// Attempts to initialize (called repeatedly until successful)
        /// </summary>
        private void TryInitialize()
        {
            draftManager = DraftManager.Instance;

            if (draftManager == null)
            {
                Debug.LogWarning("[DraftUI] Still waiting for DraftManager...");
                return;
            }

            // Success! Subscribe to events
            draftManager.OnStarterDraftOffered += ShowStarterDraft;
            draftManager.OnMidGameDraftOffered += ShowMidGameDraft;
            draftManager.OnDraftTimerUpdate += UpdateTimer;

            // Setup buttons
            if (confirmStarterButton != null)
            {
                confirmStarterButton.onClick.AddListener(ConfirmStarterDraft);
            }

            isInitialized = true;

            Debug.Log("[DraftUI] ✅ Successfully initialized!");
        }

        private void OnDestroy()
        {
            if (draftManager != null)
            {
                draftManager.OnStarterDraftOffered -= ShowStarterDraft;
                draftManager.OnMidGameDraftOffered -= ShowMidGameDraft;
                draftManager.OnDraftTimerUpdate -= UpdateTimer;
            }
        }

        // ... rest of code stays the same ...

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

            Debug.Log("[DraftUI] Showing starter draft");
        }

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
                    UpdateCardSlot(midGameSlotObjects[i], cards[i], i, false);
                }
            }

            Debug.Log("[DraftUI] Showing mid-game draft");
        }

        private void UpdateCardSlot(GameObject slotObj, CardData card, int index, bool isStarter)
        {
            Image cardIcon = slotObj.transform.Find("CardIcon")?.GetComponent<Image>();
            TextMeshProUGUI cardName = slotObj.transform.Find("CardName")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI description = slotObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
            Image rarityBorder = slotObj.transform.Find("RarityBorder")?.GetComponent<Image>();
            //test
            Image topLine = slotObj.transform.Find("LineTop")?.GetComponent<Image>();
            Image botLine = slotObj.transform.Find("LineBottom")?.GetComponent<Image>();
            TextMeshProUGUI rarityText = slotObj.transform.Find("RarityText")?.GetComponent<TextMeshProUGUI>();

            Button selectBtn = slotObj.GetComponent<Button>();
            Button mulliganBtn = slotObj.transform.Find("MulliganButton")?.GetComponent<Button>();

            if (cardIcon != null)
                cardIcon.sprite = card.cardIcon;

            if (cardName != null)
                cardName.text = card.cardName;

            if (description != null)
                description.text = card.description;

            if (rarityBorder != null)
                rarityBorder.color = card.GetRarityColor();

            //test 
            if (topLine != null && botLine != null)
            {
                topLine.color = card.GetRarityColor();
                botLine.color = card.GetRarityColor();
            }
            if (rarityText != null)
            {
                rarityText.text = card.GetRarityName();
                rarityText.color = card.GetRarityColor(); // Opcjonalnie: ten sam kolor co border
            }

            if (isStarter)
            {
                if (mulliganBtn != null)
                {
                    mulliganBtn.onClick.RemoveAllListeners();
                    int capturedIndex = index;
                    mulliganBtn.onClick.AddListener(() => OnMulliganClicked(capturedIndex));
                    mulliganBtn.interactable = true;
                }
            }
            else
            {
                if (selectBtn != null)
                {
                    selectBtn.onClick.RemoveAllListeners();
                    int capturedIndex = index;
                    selectBtn.onClick.AddListener(() => OnCardSelected(capturedIndex));
                }
            }
        }

        private void OnMulliganClicked(int slotIndex)
        {
            bool success = draftManager.MulliganCard(slotIndex);

            if (success)
            {
                Button mulliganBtn = starterSlotObjects[slotIndex].transform.Find("MulliganButton")?.GetComponent<Button>();
                if (mulliganBtn != null)
                {
                    mulliganBtn.interactable = false;

                    TextMeshProUGUI btnText = mulliganBtn.GetComponentInChildren<TextMeshProUGUI>();
                    if (btnText != null)
                        btnText.text = "Used";
                }

                Debug.Log($"[DraftUI] Mulliganed slot {slotIndex}");
            }
        }

        private void OnCardSelected(int choiceIndex)
        {
            draftManager.SelectMidGameCard(choiceIndex);

            if (midGameDraftPanel != null)
            {
                midGameDraftPanel.SetActive(false);
            }

            Debug.Log($"[DraftUI] Selected card {choiceIndex}");
        }

        private void ConfirmStarterDraft()
        {
            Debug.Log("[DraftUI] Starter draft confirmed - calling DraftManager..."); // ← TEN LOG POWINIEN BYĆ!

            // ========== TO MUSI BYĆ! ==========
            if (draftManager != null)
            {
                draftManager.ConfirmStarterDraft(); // ← WYWOŁANIE!
                Debug.Log("[DraftUI] Called draftManager.ConfirmStarterDraft()");
            }
            else
            {
                Debug.LogError("[DraftUI] draftManager is NULL! Cannot confirm!");
            }
            // ==================================

            // Hide panel
            if (starterDraftPanel != null)
            {
                starterDraftPanel.SetActive(false);
            }
        }

        private void UpdateTimer(float timeRemaining)
        {
            string timeText = Mathf.CeilToInt(timeRemaining).ToString();

            if (starterDraftPanel != null && starterDraftPanel.activeSelf && starterTimerText != null)
            {
                starterTimerText.text = $"Time: {timeText}s";
            }

            if (midGameDraftPanel != null && midGameDraftPanel.activeSelf && midGameTimerText != null)
            {
                midGameTimerText.text = $"Time: {timeText}s";
            }
        }

        private void HideAllPanels()
        {
            if (starterDraftPanel != null) starterDraftPanel.SetActive(false);
            if (midGameDraftPanel != null) midGameDraftPanel.SetActive(false);
        }
    }
}