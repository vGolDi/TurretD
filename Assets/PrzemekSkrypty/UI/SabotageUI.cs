using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using ElementumDefense.Cards;
using Photon.Pun;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SabotageUI : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioClip selectSound;
        [SerializeField] private AudioClip revealSound;

        private AudioSource audioSource;
        private VisualElement root;

        private VisualElement sabDraftPanel;
        private VisualElement sabCards;
        private Label sabTimer;
        private VisualElement sabWaiting;
        private Label sabWaitingText;

        private VisualElement sabRevealPanel;
        private VisualElement sabRevealCards;
        private Label sabRevealTimer;

        private SabotageDraftManager sabManager;
        private bool isInitialized;
        private bool hasSelected;

        private float retryTimer;
        private const float RETRY_INTERVAL = 0.5f;

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
            HideAll();
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
            sabManager =
                SabotageDraftManager.Instance;
            if (sabManager == null) return;

            sabManager.OnSabotageOffered +=
                ShowDraft;
            sabManager.OnDraftTimerUpdate +=
                UpdateTimer;
            sabManager.OnRevealPhaseStart +=
                ShowReveal;
            sabManager.OnRevealPhaseEnd +=
                HideReveal;
            sabManager.OnDraftTimeout +=
                OnTimeout;
            sabManager.OnSabotageDraftComplete +=
                OnDraftComplete;

            isInitialized = true;
            Debug.Log("[SabotageUI] Initialized");
        }

        private void OnDestroy()
        {
            if (sabManager != null)
            {
                sabManager.OnSabotageOffered -=
                    ShowDraft;
                sabManager.OnDraftTimerUpdate -=
                    UpdateTimer;
                sabManager.OnRevealPhaseStart -=
                    ShowReveal;
                sabManager.OnRevealPhaseEnd -=
                    HideReveal;
                sabManager.OnDraftTimeout -=
                    OnTimeout;
                sabManager.OnSabotageDraftComplete -=
                    OnDraftComplete;
            }
        }

        private void QueryElements()
        {
            sabDraftPanel =
                root.Q<VisualElement>(
                    "sab-draft-panel");
            sabCards =
                root.Q<VisualElement>("sab-cards");
            sabTimer =
                root.Q<Label>("sab-timer");
            sabWaiting =
                root.Q<VisualElement>("sab-waiting");
            sabWaitingText =
                root.Q<Label>("sab-waiting-text");

            sabRevealPanel =
                root.Q<VisualElement>(
                    "sab-reveal-panel");
            sabRevealCards =
                root.Q<VisualElement>(
                    "sab-reveal-cards");
            sabRevealTimer =
                root.Q<Label>("sab-reveal-timer");
        }

        // ==========================================
        // DRAFT
        // ==========================================

        private void ShowDraft(
            SabotageCardData[] cards)
        {
            hasSelected = false;
            ShowSabRoot();

            sabDraftPanel?
                .RemoveFromClassList("hidden");
            sabRevealPanel?
                .AddToClassList("hidden");
            sabWaiting?.AddToClassList("hidden");

            if (sabCards == null) return;
            sabCards.Clear();

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;
                var card = BuildSabCard(cards[i], i);
                sabCards.Add(card);
            }
        }

        private VisualElement BuildSabCard(
            SabotageCardData sab, int index)
        {
            string rk = sab.rarity switch
            {
                CardRarity.Legendary => "legendary",
                CardRarity.Rare => "rare",
                _ => "common"
            };

            var slot = new VisualElement();
            slot.AddToClassList("sab-card");
            slot.AddToClassList($"sab-card-{rk}");

            // Inner border
            var inner = new VisualElement();
            inner.AddToClassList("sab-card-inner");
            slot.Add(inner);

            // Corners
            foreach (var pos in new[] {
                "tl", "tr", "bl", "br" })
            {
                var c = new VisualElement();
                c.AddToClassList("sc-corner");
                c.AddToClassList($"sc-corner-{pos}");
                slot.Add(c);
            }

            // Icon
            if (sab.sabotageIcon != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList("sab-card-icon");
                icon.style.backgroundImage =
                    new StyleBackground(
                        sab.sabotageIcon);
                slot.Add(icon);
            }

            // Rarity line
            var line = new VisualElement();
            line.AddToClassList("sab-rarity-line");
            line.AddToClassList(
                $"sab-rarity-line-{rk}");
            slot.Add(line);

            // Name
            var name = new Label(sab.sabotageName);
            name.AddToClassList("sab-card-name");
            slot.Add(name);

            // Rarity
            var rarity = new Label(
                sab.rarity.ToString().ToUpper());
            rarity.AddToClassList(
                "sab-card-rarity-text");
            rarity.AddToClassList(
                $"sab-rarity-text-{rk}");
            slot.Add(rarity);

            // Description
            if (!string.IsNullOrEmpty(sab.description))
            {
                var desc = new Label(sab.description);
                desc.AddToClassList("sab-card-desc");
                slot.Add(desc);
            }

            // Duration
            var dur = new Label(sab.GetDurationText());
            dur.AddToClassList("sab-card-duration");
            slot.Add(dur);

            // Buttons container — pushed to bottom
            var btns = new VisualElement();
            btns.AddToClassList("sab-card-buttons");

            var btn = new Button();
            btn.text = "INFLICT";
            btn.AddToClassList("sab-select-btn");

            int idx = index;
            btn.RegisterCallback<ClickEvent>(evt =>
            {
                PlaySound(selectSound);
                OnSabotageSelected(idx);
                evt.StopPropagation();
            });
            btns.Add(btn);

            slot.Add(btns);

            // Hover accent
            var accent = new VisualElement();
            accent.AddToClassList(
                "sab-card-hover-accent");
            accent.AddToClassList(
                $"sab-hover-{rk}");
            slot.Add(accent);

            return slot;
        }

        private void OnSabotageSelected(int index)
        {
            if (hasSelected) return;
            hasSelected = true;

            sabManager?.SelectSabotage(index);

            var allCards = sabCards
                .Query<VisualElement>(
                    className: "sab-card").ToList();

            for (int i = 0; i < allCards.Count; i++)
            {
                if (i == index)
                    allCards[i].AddToClassList(
                        "sab-card-selected");
                else
                    allCards[i].AddToClassList(
                        "sab-card-dimmed");

                var btn = allCards[i].Q<Button>(
                    className: "sab-select-btn");
                btn?.SetEnabled(false);
            }

            sabWaiting?
                .RemoveFromClassList("hidden");
        }

        // ==========================================
        // TIMER
        // ==========================================

        private void UpdateTimer(float remaining)
        {
            if (hasSelected) return;
            if (sabTimer == null) return;

            sabTimer.text =
                Mathf.CeilToInt(remaining).ToString();

            if (remaining <= 5f)
                sabTimer.AddToClassList(
                    "sab-timer-critical");
            else
                sabTimer.RemoveFromClassList(
                    "sab-timer-critical");
        }

        // ==========================================
        // REVEAL
        // ==========================================

        private void ShowReveal(
            Dictionary<int, SabotageCardData>
                selections)
        {
            sabDraftPanel?.AddToClassList("hidden");
            sabRevealPanel?
                .RemoveFromClassList("hidden");

            PlaySound(revealSound);

            if (sabRevealCards == null) return;
            sabRevealCards.Clear();

            int localActor =
                PhotonNetwork.LocalPlayer.ActorNumber;

            foreach (var kvp in selections)
            {
                int actorNumber = kvp.Key;
                SabotageCardData sab = kvp.Value;
                bool isMe = actorNumber == localActor;

                var player = PhotonNetwork.CurrentRoom
                    .GetPlayer(actorNumber);
                string playerName =
                    player?.NickName ??
                    $"Player{actorNumber}";

                var card = BuildRevealCard(
                    playerName, sab, isMe);
                sabRevealCards.Add(card);
            }

            StartCoroutine(RevealCountdown());
        }

        private VisualElement BuildRevealCard(
            string playerName,
            SabotageCardData sab,
            bool isMe)
        {
            var card = new VisualElement();
            card.AddToClassList("sab-reveal-card");
            card.AddToClassList(
                isMe
                    ? "sab-reveal-card-self"
                    : "sab-reveal-card-enemy");

            var name = new Label(
                playerName.ToUpper());
            name.AddToClassList(
                "sab-reveal-player");
            name.AddToClassList(
                isMe
                    ? "sab-reveal-player-self"
                    : "sab-reveal-player-enemy");
            card.Add(name);

            if (sab?.sabotageIcon != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList(
                    "sab-reveal-icon");
                icon.style.backgroundImage =
                    new StyleBackground(
                        sab.sabotageIcon);
                card.Add(icon);
            }

            var sabName = new Label(
                sab?.sabotageName ?? "Unknown");
            sabName.AddToClassList(
                "sab-reveal-sab-name");
            card.Add(sabName);

            var target = new Label(
                isMe
                    ? "(Your pick)"
                    : "→ TARGETS YOU!");
            target.AddToClassList(
                "sab-reveal-target");
            target.AddToClassList(
                isMe
                    ? "sab-reveal-target-self"
                    : "sab-reveal-target-enemy");
            card.Add(target);

            return card;
        }

        private IEnumerator RevealCountdown()
        {
            float time = 5f;

            while (time > 0f)
            {
                if (sabRevealTimer != null)
                    sabRevealTimer.text =
                        $"Wave starting in: " +
                        $"{Mathf.CeilToInt(time)}s";

                time -= Time.deltaTime;
                yield return null;
            }
        }

        private void HideReveal()
        {
            sabRevealPanel?
                .AddToClassList("hidden");
        }

        private void OnTimeout()
        {
            Debug.Log("[SabotageUI] Timeout");
        }

        private void OnDraftComplete()
        {
            HideAll();
            hasSelected = false;
        }

        private void ShowSabRoot()
        {
            var sabRoot =
                root.Q<VisualElement>(
                    "sabotage-root");
            sabRoot?.RemoveFromClassList("hidden");
        }

        private void HideAll()
        {
            var sabRoot =
                root.Q<VisualElement>(
                    "sabotage-root");
            sabRoot?.AddToClassList("hidden");
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip, 0.8f);
        }
    }
}
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections;
//using System.Collections.Generic;
//using Unity.VisualScripting;

//namespace ElementumDefense.Cards
//{
//    public class SabotageUI : MonoBehaviour
//    {
//        [Header("Panels")]
//        [SerializeField] private GameObject sabotageDraftPanel;
//        [SerializeField] private GameObject revealPanel;

//        [Header("Draft UI (3 choices)")]
//        [SerializeField] private GameObject[] sabotageSlotObjects = new GameObject[3];
//        [SerializeField] private TextMeshProUGUI draftTimerText;

//        [Header("Reveal UI")]
//        [SerializeField] private Transform revealContainer;
//        [SerializeField] private GameObject revealCardPrefab;
//        [SerializeField] private TextMeshProUGUI revealTimerText;
//        [SerializeField] private TextMeshProUGUI revealHeaderText;

//        private SabotageDraftManager sabotageDraftManager;
//        private bool isInitialized = false;
//        private bool hasSelectedSabotage = false; // ← NOWE: prevent double-click

//        // Retry system
//        private float initializationRetryTimer = 0f;
//        private const float RETRY_INTERVAL = 0.5f;

//        private void Start()
//        {
//            Debug.Log("[SabotageUI] Waiting for SabotageDraftManager...");
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
//            sabotageDraftManager = SabotageDraftManager.Instance;

//            if (sabotageDraftManager == null)
//            {
//                return; // Silent retry
//            }

//            // Subscribe to events
//            sabotageDraftManager.OnSabotageOffered += ShowSabotageDraft;
//            sabotageDraftManager.OnDraftTimerUpdate += UpdateDraftTimer;
//            sabotageDraftManager.OnRevealPhaseStart += ShowRevealPhase;
//            sabotageDraftManager.OnRevealPhaseEnd += HideRevealPhase;
//            sabotageDraftManager.OnDraftTimeout += OnTimeout;

//            // ========== NOWE: Subscribe to draft complete ==========
//            sabotageDraftManager.OnSabotageDraftComplete += OnDraftComplete;
//            // ======================================================

//            isInitialized = true;

//            Debug.Log("[SabotageUI] ✅ Initialized!");
//        }

//        private void OnDestroy()
//        {
//            if (sabotageDraftManager != null)
//            {
//                sabotageDraftManager.OnSabotageOffered -= ShowSabotageDraft;
//                sabotageDraftManager.OnDraftTimerUpdate -= UpdateDraftTimer;
//                sabotageDraftManager.OnRevealPhaseStart -= ShowRevealPhase;
//                sabotageDraftManager.OnRevealPhaseEnd -= HideRevealPhase;
//                sabotageDraftManager.OnDraftTimeout -= OnTimeout;

//                // ========== NOWE ==========
//                sabotageDraftManager.OnSabotageDraftComplete -= OnDraftComplete;
//                // =========================
//            }
//        }

//        // ==========================================
//        // SHOW SABOTAGE DRAFT
//        // ==========================================

//        private void ShowSabotageDraft(SabotageCardData[] cards)
//        {
//            HideAllPanels();

//            // ========== NOWE: Reset selection state ==========
//            hasSelectedSabotage = false;
//            // ================================================

//            if (sabotageDraftPanel != null)
//            {
//                sabotageDraftPanel.SetActive(true);
//            }



//            for (int i = 0; i < sabotageSlotObjects.Length && i < cards.Length; i++)
//            {
//                if (sabotageSlotObjects[i] != null && cards[i] != null)
//                {
//                    UpdateSabotageSlot(sabotageSlotObjects[i], cards[i], i);
//                    sabotageSlotObjects[i].SetActive(true); // ← Ensure visible
//                }
//                else if (sabotageSlotObjects[i] != null)
//                {
//                    // ========== NOWE: Hide empty slots ==========
//                    sabotageSlotObjects[i].SetActive(false);
//                    // ============================================
//                }
//            }

//            Debug.Log($"[SabotageUI] Showing sabotage draft with {cards.Length} cards");
//        }

//        private void UpdateSabotageSlot(GameObject slotObj, SabotageCardData sabotage, int index)
//        {
//            Image sabotageIcon = slotObj.transform.Find("SabotageIcon")?.GetComponent<Image>();
//            TextMeshProUGUI sabotageName = slotObj.transform.Find("SabotageName")?.GetComponent<TextMeshProUGUI>();
//            TextMeshProUGUI description = slotObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
//            TextMeshProUGUI durationText = slotObj.transform.Find("DurationText")?.GetComponent<TextMeshProUGUI>();
//            Image rarityBorder = slotObj.transform.Find("RarityBorder")?.GetComponent<Image>();
//            Image topLine = slotObj.transform.Find("LineTop")?.GetComponent<Image>();
//            Image botLine = slotObj.transform.Find("LineBottom")?.GetComponent<Image>();
//            Button selectBtn = slotObj.GetComponent<Button>();

//            if (sabotageIcon != null && sabotage.sabotageIcon != null)
//                sabotageIcon.sprite = sabotage.sabotageIcon;

//            if (sabotageName != null)
//                sabotageName.text = sabotage.sabotageName;

//            if (description != null)
//                description.text = sabotage.description;

//            if (durationText != null)
//                durationText.text = sabotage.GetDurationText();

//            if (rarityBorder != null)
//                rarityBorder.color = sabotage.GetRarityColor().WithAlpha(0.2f);

//            if (topLine != null && botLine != null)
//            {
//                topLine.color = sabotage.GetRarityColor();
//                botLine.color = sabotage.GetRarityColor();
//            }
//            if (selectBtn != null)
//            {
//                selectBtn.onClick.RemoveAllListeners();
//                int capturedIndex = index;
//                selectBtn.onClick.AddListener(() => OnSabotageSelected(capturedIndex));

//                // ========== NOWE: Ensure button is interactable ==========
//                selectBtn.interactable = true;
//                // ========================================================
//            }
//        }

//        // ==========================================
//        // PLAYER SELECTION
//        // ==========================================

//        private void OnSabotageSelected(int choiceIndex)
//        {
//            // ========== NOWE: Prevent double-click ==========
//            if (hasSelectedSabotage)
//            {
//                Debug.LogWarning("[SabotageUI] Already selected a sabotage!");
//                return;
//            }

//            if (sabotageDraftManager == null)
//            {
//                Debug.LogError("[SabotageUI] SabotageDraftManager is null!");
//                return;
//            }

//            hasSelectedSabotage = true;
//            // ================================================

//            sabotageDraftManager.SelectSabotage(choiceIndex);

//            // ========== NOWE: Disable all buttons instead of hiding panel ==========
//            // Panel stays visible but buttons are grayed out
//            foreach (var slot in sabotageSlotObjects)
//            {
//                if (slot != null)
//                {
//                    Button btn = slot.GetComponent<Button>();
//                    if (btn != null)
//                    {
//                        btn.interactable = false;
//                    }
//                }
//            }

//            // Show waiting message
//            if (draftTimerText != null)
//            {
//                draftTimerText.text = "Waiting for others...";
//                draftTimerText.color = Color.yellow;
//            }

//            // ========== NOWE: Highlight selected card ==========
//            if (choiceIndex >= 0 && choiceIndex < sabotageSlotObjects.Length &&
//                sabotageSlotObjects[choiceIndex] != null)
//            {
//                Image rarityBorder = sabotageSlotObjects[choiceIndex]
//                    .transform.Find("RarityBorder")?.GetComponent<Image>();

//                if (rarityBorder != null)
//                {
//                    // Make selected card glow/brighter
//                    Color selectedColor = rarityBorder.color;
//                    selectedColor.a = 1f;
//                    rarityBorder.color = selectedColor;
//                }

//                // Dim non-selected cards
//                for (int i = 0; i < sabotageSlotObjects.Length; i++)
//                {
//                    if (i != choiceIndex && sabotageSlotObjects[i] != null)
//                    {
//                        CanvasGroup cg = sabotageSlotObjects[i].GetComponent<CanvasGroup>();
//                        if (cg == null)
//                            cg = sabotageSlotObjects[i].AddComponent<CanvasGroup>();

//                        cg.alpha = 0.4f;
//                    }
//                }
//            }
//            // ===================================================

//            Debug.Log($"[SabotageUI] ✅ Selected sabotage {choiceIndex}");
//        }

//        // ==========================================
//        // TIMER
//        // ==========================================

//        private void UpdateDraftTimer(float timeRemaining)
//        {
//            // ========== NOWE: Don't update if already selected ==========
//            if (hasSelectedSabotage) return;
//            // ===========================================================

//            if (draftTimerText != null && sabotageDraftPanel != null &&
//                sabotageDraftPanel.activeSelf)
//            {
//                draftTimerText.text = $"Time: {Mathf.CeilToInt(timeRemaining)}s";

//                if (timeRemaining <= 5f)
//                    draftTimerText.color = Color.red;
//                else
//                    draftTimerText.color = Color.white;
//            }
//        }

//        // ==========================================
//        // REVEAL PHASE
//        // ==========================================

//        private void ShowRevealPhase(Dictionary<int, SabotageCardData> playerSelections)
//        {
//            HideAllPanels();

//            if (revealPanel != null)
//            {
//                revealPanel.SetActive(true);
//            }

//            if (revealHeaderText != null)
//            {
//                revealHeaderText.text = "SABOTAGES REVEALED!";
//            }

//            // Clear old reveal cards
//            if (revealContainer != null)
//            {
//                foreach (Transform child in revealContainer)
//                {
//                    Destroy(child.gameObject);
//                }
//            }

//            // Create reveal cards for each player
//            foreach (var kvp in playerSelections)
//            {
//                int actorNumber = kvp.Key;
//                SabotageCardData sabotage = kvp.Value;

//                Photon.Realtime.Player player =
//                    Photon.Pun.PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
//                string playerName = player?.NickName ?? $"Player{actorNumber}";

//                // ========== NOWE: Check if this sabotage targets ME ==========
//                bool isMe = actorNumber == Photon.Pun.PhotonNetwork.LocalPlayer.ActorNumber;
//                bool targetsMe = !isMe; // Other players' sabotages target me
//                // ============================================================

//                if (revealCardPrefab != null && revealContainer != null)
//                {
//                    GameObject revealCard = Instantiate(revealCardPrefab, revealContainer);

//                    TextMeshProUGUI playerNameText =
//                        revealCard.transform.Find("PlayerName")?.GetComponent<TextMeshProUGUI>();
//                    TextMeshProUGUI sabotageNameText =
//                        revealCard.transform.Find("SabotageName")?.GetComponent<TextMeshProUGUI>();
//                    Image sabotageIcon =
//                        revealCard.transform.Find("Icon")?.GetComponent<Image>();

//                    // ========== NOWE: Target indicator ==========
//                    TextMeshProUGUI targetText =
//                        revealCard.transform.Find("TargetText")?.GetComponent<TextMeshProUGUI>();
//                    // ============================================

//                    if (playerNameText != null)
//                    {
//                        playerNameText.text = playerName;

//                        // ========== NOWE: Color coding ==========
//                        if (isMe)
//                            playerNameText.color = Color.cyan; // Your own pick
//                        else
//                            playerNameText.color = Color.red; // Enemy sabotage
//                        // ========================================
//                    }

//                    if (sabotageNameText != null)
//                        sabotageNameText.text = sabotage?.sabotageName ?? "Unknown";

//                    if (sabotageIcon != null && sabotage?.sabotageIcon != null)
//                        sabotageIcon.sprite = sabotage.sabotageIcon;

//                    // ========== NOWE: Show target info ==========
//                    if (targetText != null)
//                    {
//                        if (isMe)
//                            targetText.text = "(Your pick)";
//                        else
//                            targetText.text = "→ Targets YOU!";
//                    }
//                    // ============================================
//                }
//            }

//            Debug.Log($"[SabotageUI] Showing reveal phase ({playerSelections.Count} players)");
//            StartCoroutine(RevealCountdown());
//        }

//        private IEnumerator RevealCountdown()
//        {
//            float timeRemaining = 5f;

//            while (timeRemaining > 0f && revealPanel != null && revealPanel.activeSelf)
//            {
//                if (revealTimerText != null)
//                {
//                    revealTimerText.text = $"Wave starting in: {Mathf.CeilToInt(timeRemaining)}s";
//                }

//                timeRemaining -= Time.deltaTime;
//                yield return null;
//            }
//        }

//        private void HideRevealPhase()
//        {
//            if (revealPanel != null)
//            {
//                revealPanel.SetActive(false);
//            }

//            // ========== NOWE: Reset dimmed cards ==========
//            ResetCardVisuals();
//            // ==============================================

//            Debug.Log("[SabotageUI] Hiding reveal phase");
//        }

//        // ==========================================
//        // EVENTS
//        // ==========================================

//        private void OnTimeout()
//        {
//            Debug.Log("[SabotageUI] Sabotage draft TIMEOUT!");

//            // ========== NOWE: Auto-hide draft panel on timeout ==========
//            // Panel will be replaced by reveal panel soon
//            // ==========================================================
//        }

//        // ========== NOWE: Draft complete handler ==========
//        private void OnDraftComplete()
//        {
//            HideAllPanels();
//            ResetCardVisuals();
//            hasSelectedSabotage = false;

//            Debug.Log("[SabotageUI] Draft complete - all panels hidden");
//        }
//        // ==================================================

//        // ==========================================
//        // UTILITY
//        // ==========================================

//        // ========== NOWE: Reset card visuals after draft ==========
//        private void ResetCardVisuals()
//        {
//            foreach (var slot in sabotageSlotObjects)
//            {
//                if (slot != null)
//                {
//                    // Reset alpha
//                    CanvasGroup cg = slot.GetComponent<CanvasGroup>();
//                    if (cg != null)
//                    {
//                        cg.alpha = 1f;
//                    }

//                    // Re-enable button
//                    Button btn = slot.GetComponent<Button>();
//                    if (btn != null)
//                    {
//                        btn.interactable = true;
//                    }

//                    // Show slot
//                    slot.SetActive(true);
//                }
//            }
//        }
//        // =========================================================

//        private void HideAllPanels()
//        {
//            if (sabotageDraftPanel != null) sabotageDraftPanel.SetActive(false);
//            if (revealPanel != null) revealPanel.SetActive(false);
//        }
//    }
//}