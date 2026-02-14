using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

namespace ElementumDefense.Cards
{
    public class SabotageUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject sabotageDraftPanel;
        [SerializeField] private GameObject revealPanel;

        [Header("Draft UI (3 choices)")]
        [SerializeField] private GameObject[] sabotageSlotObjects = new GameObject[3];
        [SerializeField] private TextMeshProUGUI draftTimerText;

        [Header("Reveal UI")]
        [SerializeField] private Transform revealContainer;
        [SerializeField] private GameObject revealCardPrefab;
        [SerializeField] private TextMeshProUGUI revealTimerText;
        [SerializeField] private TextMeshProUGUI revealHeaderText;

        private SabotageDraftManager sabotageDraftManager;
        private bool isInitialized = false;
        private bool hasSelectedSabotage = false; // ← NOWE: prevent double-click

        // Retry system
        private float initializationRetryTimer = 0f;
        private const float RETRY_INTERVAL = 0.5f;

        private void Start()
        {
            Debug.Log("[SabotageUI] Waiting for SabotageDraftManager...");
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
            sabotageDraftManager = SabotageDraftManager.Instance;

            if (sabotageDraftManager == null)
            {
                return; // Silent retry
            }

            // Subscribe to events
            sabotageDraftManager.OnSabotageOffered += ShowSabotageDraft;
            sabotageDraftManager.OnDraftTimerUpdate += UpdateDraftTimer;
            sabotageDraftManager.OnRevealPhaseStart += ShowRevealPhase;
            sabotageDraftManager.OnRevealPhaseEnd += HideRevealPhase;
            sabotageDraftManager.OnDraftTimeout += OnTimeout;

            // ========== NOWE: Subscribe to draft complete ==========
            sabotageDraftManager.OnSabotageDraftComplete += OnDraftComplete;
            // ======================================================

            isInitialized = true;

            Debug.Log("[SabotageUI] ✅ Initialized!");
        }

        private void OnDestroy()
        {
            if (sabotageDraftManager != null)
            {
                sabotageDraftManager.OnSabotageOffered -= ShowSabotageDraft;
                sabotageDraftManager.OnDraftTimerUpdate -= UpdateDraftTimer;
                sabotageDraftManager.OnRevealPhaseStart -= ShowRevealPhase;
                sabotageDraftManager.OnRevealPhaseEnd -= HideRevealPhase;
                sabotageDraftManager.OnDraftTimeout -= OnTimeout;

                // ========== NOWE ==========
                sabotageDraftManager.OnSabotageDraftComplete -= OnDraftComplete;
                // =========================
            }
        }

        // ==========================================
        // SHOW SABOTAGE DRAFT
        // ==========================================

        private void ShowSabotageDraft(SabotageCardData[] cards)
        {
            HideAllPanels();

            // ========== NOWE: Reset selection state ==========
            hasSelectedSabotage = false;
            // ================================================

            if (sabotageDraftPanel != null)
            {
                sabotageDraftPanel.SetActive(true);
            }

           

            for (int i = 0; i < sabotageSlotObjects.Length && i < cards.Length; i++)
            {
                if (sabotageSlotObjects[i] != null && cards[i] != null)
                {
                    UpdateSabotageSlot(sabotageSlotObjects[i], cards[i], i);
                    sabotageSlotObjects[i].SetActive(true); // ← Ensure visible
                }
                else if (sabotageSlotObjects[i] != null)
                {
                    // ========== NOWE: Hide empty slots ==========
                    sabotageSlotObjects[i].SetActive(false);
                    // ============================================
                }
            }

            Debug.Log($"[SabotageUI] Showing sabotage draft with {cards.Length} cards");
        }

        private void UpdateSabotageSlot(GameObject slotObj, SabotageCardData sabotage, int index)
        {
            Image sabotageIcon = slotObj.transform.Find("SabotageIcon")?.GetComponent<Image>();
            TextMeshProUGUI sabotageName = slotObj.transform.Find("SabotageName")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI description = slotObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI durationText = slotObj.transform.Find("DurationText")?.GetComponent<TextMeshProUGUI>();
            Image rarityBorder = slotObj.transform.Find("RarityBorder")?.GetComponent<Image>();
            Image topLine = slotObj.transform.Find("LineTop")?.GetComponent<Image>();
            Image botLine = slotObj.transform.Find("LineBottom")?.GetComponent<Image>();
            Button selectBtn = slotObj.GetComponent<Button>();

            if (sabotageIcon != null && sabotage.sabotageIcon != null)
                sabotageIcon.sprite = sabotage.sabotageIcon;

            if (sabotageName != null)
                sabotageName.text = sabotage.sabotageName;

            if (description != null)
                description.text = sabotage.description;

            if (durationText != null)
                durationText.text = sabotage.GetDurationText();

            if (rarityBorder != null)
                rarityBorder.color = sabotage.GetRarityColor().WithAlpha(0.2f);

            if (topLine != null && botLine != null)
            {
                topLine.color = sabotage.GetRarityColor();
                botLine.color = sabotage.GetRarityColor();
            }
            if (selectBtn != null)
            {
                selectBtn.onClick.RemoveAllListeners();
                int capturedIndex = index;
                selectBtn.onClick.AddListener(() => OnSabotageSelected(capturedIndex));

                // ========== NOWE: Ensure button is interactable ==========
                selectBtn.interactable = true;
                // ========================================================
            }
        }

        // ==========================================
        // PLAYER SELECTION
        // ==========================================

        private void OnSabotageSelected(int choiceIndex)
        {
            // ========== NOWE: Prevent double-click ==========
            if (hasSelectedSabotage)
            {
                Debug.LogWarning("[SabotageUI] Already selected a sabotage!");
                return;
            }

            if (sabotageDraftManager == null)
            {
                Debug.LogError("[SabotageUI] SabotageDraftManager is null!");
                return;
            }

            hasSelectedSabotage = true;
            // ================================================

            sabotageDraftManager.SelectSabotage(choiceIndex);

            // ========== NOWE: Disable all buttons instead of hiding panel ==========
            // Panel stays visible but buttons are grayed out
            foreach (var slot in sabotageSlotObjects)
            {
                if (slot != null)
                {
                    Button btn = slot.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.interactable = false;
                    }
                }
            }

            // Show waiting message
            if (draftTimerText != null)
            {
                draftTimerText.text = "Waiting for others...";
                draftTimerText.color = Color.yellow;
            }

            // ========== NOWE: Highlight selected card ==========
            if (choiceIndex >= 0 && choiceIndex < sabotageSlotObjects.Length &&
                sabotageSlotObjects[choiceIndex] != null)
            {
                Image rarityBorder = sabotageSlotObjects[choiceIndex]
                    .transform.Find("RarityBorder")?.GetComponent<Image>();

                if (rarityBorder != null)
                {
                    // Make selected card glow/brighter
                    Color selectedColor = rarityBorder.color;
                    selectedColor.a = 1f;
                    rarityBorder.color = selectedColor;
                }

                // Dim non-selected cards
                for (int i = 0; i < sabotageSlotObjects.Length; i++)
                {
                    if (i != choiceIndex && sabotageSlotObjects[i] != null)
                    {
                        CanvasGroup cg = sabotageSlotObjects[i].GetComponent<CanvasGroup>();
                        if (cg == null)
                            cg = sabotageSlotObjects[i].AddComponent<CanvasGroup>();

                        cg.alpha = 0.4f;
                    }
                }
            }
            // ===================================================

            Debug.Log($"[SabotageUI] ✅ Selected sabotage {choiceIndex}");
        }

        // ==========================================
        // TIMER
        // ==========================================

        private void UpdateDraftTimer(float timeRemaining)
        {
            // ========== NOWE: Don't update if already selected ==========
            if (hasSelectedSabotage) return;
            // ===========================================================

            if (draftTimerText != null && sabotageDraftPanel != null &&
                sabotageDraftPanel.activeSelf)
            {
                draftTimerText.text = $"Time: {Mathf.CeilToInt(timeRemaining)}s";

                if (timeRemaining <= 5f)
                    draftTimerText.color = Color.red;
                else
                    draftTimerText.color = Color.white;
            }
        }

        // ==========================================
        // REVEAL PHASE
        // ==========================================

        private void ShowRevealPhase(Dictionary<int, SabotageCardData> playerSelections)
        {
            HideAllPanels();

            if (revealPanel != null)
            {
                revealPanel.SetActive(true);
            }

            if (revealHeaderText != null)
            {
                revealHeaderText.text = "SABOTAGES REVEALED!";
            }

            // Clear old reveal cards
            if (revealContainer != null)
            {
                foreach (Transform child in revealContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            // Create reveal cards for each player
            foreach (var kvp in playerSelections)
            {
                int actorNumber = kvp.Key;
                SabotageCardData sabotage = kvp.Value;

                Photon.Realtime.Player player =
                    Photon.Pun.PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
                string playerName = player?.NickName ?? $"Player{actorNumber}";

                // ========== NOWE: Check if this sabotage targets ME ==========
                bool isMe = actorNumber == Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber;
                bool targetsMe = !isMe; // Other players' sabotages target me
                // ============================================================

                if (revealCardPrefab != null && revealContainer != null)
                {
                    GameObject revealCard = Instantiate(revealCardPrefab, revealContainer);

                    TextMeshProUGUI playerNameText =
                        revealCard.transform.Find("PlayerName")?.GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI sabotageNameText =
                        revealCard.transform.Find("SabotageName")?.GetComponent<TextMeshProUGUI>();
                    Image sabotageIcon =
                        revealCard.transform.Find("Icon")?.GetComponent<Image>();

                    // ========== NOWE: Target indicator ==========
                    TextMeshProUGUI targetText =
                        revealCard.transform.Find("TargetText")?.GetComponent<TextMeshProUGUI>();
                    // ============================================

                    if (playerNameText != null)
                    {
                        playerNameText.text = playerName;

                        // ========== NOWE: Color coding ==========
                        if (isMe)
                            playerNameText.color = Color.cyan; // Your own pick
                        else
                            playerNameText.color = Color.red; // Enemy sabotage
                        // ========================================
                    }

                    if (sabotageNameText != null)
                        sabotageNameText.text = sabotage?.sabotageName ?? "Unknown";

                    if (sabotageIcon != null && sabotage?.sabotageIcon != null)
                        sabotageIcon.sprite = sabotage.sabotageIcon;

                    // ========== NOWE: Show target info ==========
                    if (targetText != null)
                    {
                        if (isMe)
                            targetText.text = "(Your pick)";
                        else
                            targetText.text = "→ Targets YOU!";
                    }
                    // ============================================
                }
            }

            Debug.Log($"[SabotageUI] Showing reveal phase ({playerSelections.Count} players)");
            StartCoroutine(RevealCountdown());
        }

        private IEnumerator RevealCountdown()
        {
            float timeRemaining = 5f;

            while (timeRemaining > 0f && revealPanel != null && revealPanel.activeSelf)
            {
                if (revealTimerText != null)
                {
                    revealTimerText.text = $"Wave starting in: {Mathf.CeilToInt(timeRemaining)}s";
                }

                timeRemaining -= Time.deltaTime;
                yield return null;
            }
        }

        private void HideRevealPhase()
        {
            if (revealPanel != null)
            {
                revealPanel.SetActive(false);
            }

            // ========== NOWE: Reset dimmed cards ==========
            ResetCardVisuals();
            // ==============================================

            Debug.Log("[SabotageUI] Hiding reveal phase");
        }

        // ==========================================
        // EVENTS
        // ==========================================

        private void OnTimeout()
        {
            Debug.Log("[SabotageUI] Sabotage draft TIMEOUT!");

            // ========== NOWE: Auto-hide draft panel on timeout ==========
            // Panel will be replaced by reveal panel soon
            // ==========================================================
        }

        // ========== NOWE: Draft complete handler ==========
        private void OnDraftComplete()
        {
            HideAllPanels();
            ResetCardVisuals();
            hasSelectedSabotage = false;

            Debug.Log("[SabotageUI] Draft complete - all panels hidden");
        }
        // ==================================================

        // ==========================================
        // UTILITY
        // ==========================================

        // ========== NOWE: Reset card visuals after draft ==========
        private void ResetCardVisuals()
        {
            foreach (var slot in sabotageSlotObjects)
            {
                if (slot != null)
                {
                    // Reset alpha
                    CanvasGroup cg = slot.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.alpha = 1f;
                    }

                    // Re-enable button
                    Button btn = slot.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.interactable = true;
                    }

                    // Show slot
                    slot.SetActive(true);
                }
            }
        }
        // =========================================================

        private void HideAllPanels()
        {
            if (sabotageDraftPanel != null) sabotageDraftPanel.SetActive(false);
            if (revealPanel != null) revealPanel.SetActive(false);
        }
    }
}