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

            var bg = root.Q<VisualElement>("draft-window");
            StarfieldInjector.Instance?.Register(bg);
        }

        public void HidePanel()
        {
            if (root != null)
            {
                var bg = root.Q<VisualElement>("draft-window");
                StarfieldInjector.Instance?.Unregister(bg);
            }

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
