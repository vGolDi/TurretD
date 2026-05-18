using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ElementumDefense.Progression;
using ElementumDefense.Cards;
using ElementumDefense.Lootbox;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class QuestUI : MonoBehaviour
    {
        public static QuestUI Instance
        { get; private set; }

        [Header("Audio")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip closeSound;
        [SerializeField] private AudioClip claimSound;
        [SerializeField] private AudioClip buttonClickSound;
            
        [Header("Tier Colors")]
        [SerializeField]
        private Color dailyColor =
            new Color(0.29f, 0.87f, 0.5f);
        [SerializeField]
        private Color weeklyColor =
            new Color(0.3f, 0.5f, 1f);
        [SerializeField]
        private Color specialColor =
            new Color(0.98f, 0.45f, 0.09f);

        private AudioSource audioSource;
        private VisualElement root;

        // Elements
        private VisualElement questPanel;
        private VisualElement tabButton;
        private VisualElement questList;
        private VisualElement questEmpty;
        private VisualElement notificationBadge;
        private Label notificationCount;
        private Label resetTimerLabel;
        private Label panelTitle;

        // Filter tabs
        private Button tabAll;
        private Button tabDaily;
        private Button tabWeekly;
        private Button tabSpecial;
        private List<Button> allFilterTabs;

        // State
        private bool isOpen = false;
        private QuestTier? currentFilter = null;
        private Coroutine timerCoroutine;

        public bool IsOpen => isOpen;

        public System.Action OnPanelOpened;
        public System.Action OnPanelClosed;

        // ==========================================
        // LIFECYCLE
        // ==========================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource =
                    gameObject.AddComponent<AudioSource>();
        }

        private void OnEnable()
        {
            var uiDoc = GetComponent<UIDocument>();
            root = uiDoc.rootVisualElement;

            QueryElements();
            BindButtons();
            SubscribeEvents();

            questPanel?.AddToClassList(
                "quest-panel-closed");
            isOpen = false;

            RefreshQuests();
            UpdateNotificationBadge();
            StartTimerUpdate();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
            StopTimerUpdate();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        // ==========================================
        // QUERY
        // ==========================================

        private void QueryElements()
        {
            questPanel =
                root.Q<VisualElement>("quest-panel");
            tabButton =
                root.Q<VisualElement>(
                    "quest-tab-button");
            questList =
                root.Q<VisualElement>("quest-list");
            questEmpty =
                root.Q<VisualElement>("quest-empty");
            notificationBadge =
                root.Q<VisualElement>(
                    "quest-notification-badge");
            notificationCount =
                root.Q<Label>(
                    "quest-notification-count");
            resetTimerLabel =
                root.Q<Label>("quest-reset-timer");
            panelTitle =
                root.Q<Label>("quest-panel-title");

            tabAll =
                root.Q<Button>("quest-tab-all");
            tabDaily =
                root.Q<Button>("quest-tab-daily");
            tabWeekly =
                root.Q<Button>("quest-tab-weekly");
            tabSpecial =
                root.Q<Button>("quest-tab-special");

            allFilterTabs = new List<Button>
            {
                tabAll, tabDaily, tabWeekly, tabSpecial
            };
        }

        // ==========================================
        // BIND
        // ==========================================

        private void BindButtons()
        {
            tabButton?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    Toggle();
                    evt.StopPropagation();
                });

            tabAll?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    PlayClick();
                    SetFilter(null);
                });

            tabDaily?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    PlayClick();
                    SetFilter(QuestTier.Daily);
                });

            tabWeekly?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    PlayClick();
                    SetFilter(QuestTier.Weekly);
                });

            tabSpecial?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    PlayClick();
                    SetFilter(QuestTier.Special);
                });

            root?.RegisterCallback<ClickEvent>(evt =>
            {
                if (!isOpen) return;

                var target =
                    evt.target as VisualElement;
                if (target != null &&
                    !IsChildOf(target, questPanel) &&
                    !IsChildOf(target, tabButton))
                {
                    Close();
                }
            });
        }

        // ==========================================
        // EVENTS
        // ==========================================

        private void SubscribeEvents()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance
                    .OnQuestListUpdated +=
                    OnQuestsUpdated;
            }
        }

        private void UnsubscribeEvents()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance
                    .OnQuestListUpdated -=
                    OnQuestsUpdated;
            }
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        public void Open()
        {
            if (isOpen) return;

            isOpen = true;
            questPanel?.RemoveFromClassList(
                "quest-panel-closed");

            UpdateTabButtonPosition(true);

            PlaySound(openSound);
            RefreshQuests();
            OnPanelOpened?.Invoke();
        }

        public void Close()
        {
            if (!isOpen) return;

            isOpen = false;
            questPanel?.AddToClassList(
                "quest-panel-closed");

            UpdateTabButtonPosition(false);

            PlaySound(closeSound);
            OnPanelClosed?.Invoke();
        }

        public void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }

        public void Show()
        {
            if (root == null) return;
            root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (root == null) return;
            root.style.display = DisplayStyle.None;
            if (isOpen) Close();
        }

        // ==========================================
        // TAB BUTTON POSITION
        // ==========================================

        private void UpdateTabButtonPosition(bool open)
        {
            if (tabButton == null) return;

            if (open)
            {
                tabButton.style.right =
                    new StyleLength(380f);
            }
            else
            {
                tabButton.style.right =
                    new StyleLength(0f);
            }
        }

        // ==========================================
        // FILTER
        // ==========================================

        private void SetFilter(QuestTier? tier)
        {
            currentFilter = tier;
            UpdateFilterTabs();
            RefreshQuests();
            UpdatePanelTitle();
        }

        private void UpdateFilterTabs()
        {
            foreach (var tab in allFilterTabs)
            {
                if (tab == null) continue;
                tab.RemoveFromClassList(
                    "quest-filter-active");
            }

            Button active = currentFilter switch
            {
                QuestTier.Daily => tabDaily,
                QuestTier.Weekly => tabWeekly,
                QuestTier.Special => tabSpecial,
                _ => tabAll
            };

            active?.AddToClassList(
                "quest-filter-active");
        }

        private void UpdatePanelTitle()
        {
            if (panelTitle == null) return;

            panelTitle.text = currentFilter switch
            {
                QuestTier.Daily => "DAILY QUESTS",
                QuestTier.Weekly => "WEEKLY QUESTS",
                QuestTier.Special => "SPECIAL QUESTS",
                _ => "ALL QUESTS"
            };
        }

        // ==========================================
        // QUEST DISPLAY
        // ==========================================

        private void RefreshQuests()
        {
            if (questList == null ||
                QuestManager.Instance == null)
                return;

            questList.Clear();

            List<Quest> quests;

            if (currentFilter.HasValue)
                quests = QuestManager.Instance
                    .GetQuestsByTier(
                        currentFilter.Value);
            else
                quests = QuestManager.Instance
                    .activeQuests;

            quests = quests
                .OrderBy(q => q.isClaimed ? 1 : 0)
                .ThenBy(q => q.isCompleted ? 0 : 1)
                .ThenBy(q => q.tier)
                .ToList();

            if (quests.Count == 0)
            {
                SetVisible(questEmpty, true);
                return;
            }

            SetVisible(questEmpty, false);

            foreach (var quest in quests)
            {
                var slot = BuildQuestSlot(quest);
                questList.Add(slot);
            }
        }

        // ==========================================
        // BUILD QUEST SLOT
        // ==========================================

        private VisualElement BuildQuestSlot(
            Quest quest)
        {
            var slot = new VisualElement();
            slot.AddToClassList("quest-slot");

            if (quest.isClaimed)
                slot.AddToClassList(
                    "quest-slot-claimed");
            else if (quest.isCompleted)
                slot.AddToClassList(
                    "quest-slot-claimable");

            // TOP ROW
            var topRow = new VisualElement();
            topRow.AddToClassList("quest-slot-top");

            var tierBadge = new VisualElement();
            tierBadge.AddToClassList(
                "quest-tier-badge");
            tierBadge.AddToClassList(
                GetTierClass(quest.tier));
            topRow.Add(tierBadge);

            var desc = new Label(quest.description);
            desc.AddToClassList("quest-description");
            topRow.Add(desc);

            slot.Add(topRow);

            // PROGRESS
            if (!quest.isClaimed)
            {
                var progressSection =
                    new VisualElement();
                progressSection.AddToClassList(
                    "quest-progress-section");

                var progressBg = new VisualElement();
                progressBg.AddToClassList(
                    "quest-progress-bar-bg");

                var progressFill =
                    new VisualElement();
                progressFill.AddToClassList(
                    "quest-progress-bar-fill");

                float pct =
                    quest.GetProgress01() * 100f;
                progressFill.style.width =
                    new StyleLength(
                        new Length(
                            pct, LengthUnit.Percent));

                if (quest.isCompleted)
                    progressFill.AddToClassList(
                        "quest-progress-complete");

                progressBg.Add(progressFill);
                progressSection.Add(progressBg);

                var progressText = new Label(
                    $"{quest.currentProgress}/" +
                    $"{quest.targetAmount}");
                progressText.AddToClassList(
                    "quest-progress-text");
                progressSection.Add(progressText);

                slot.Add(progressSection);
            }

            // BOTTOM ROW
            var bottomRow = new VisualElement();
            bottomRow.AddToClassList(
                "quest-slot-bottom");

            if (quest.isClaimed)
            {
                var claimedOverlay =
                    new VisualElement();
                claimedOverlay.AddToClassList(
                    "quest-claimed-overlay");

                var claimedText =
                    new Label("CLAIMED");
                claimedText.AddToClassList(
                    "quest-claimed-text");
                claimedOverlay.Add(claimedText);

                var timerText = new Label(
                    GetTimerText(quest.tier));
                timerText.AddToClassList(
                    "quest-timer-text");
                timerText.name =
                    $"timer-{quest.questID}";
                claimedOverlay.Add(timerText);

                bottomRow.Add(claimedOverlay);
            }
            else
            {
                var rewards = new VisualElement();
                rewards.AddToClassList(
                    "quest-rewards");

                if (quest.rewardGold > 0)
                {
                    rewards.Add(BuildRewardItem(
                        $"{quest.rewardGold}",
                        "quest-reward-dot-gold",
                        "quest-reward-gold"));
                }

                if (quest.rewardXP > 0)
                {
                    rewards.Add(BuildRewardItem(
                        $"{quest.rewardXP} XP",
                        "quest-reward-dot-xp",
                        "quest-reward-xp"));
                }

                if (quest.rewardBPXP > 0)
                {
                    rewards.Add(BuildRewardItem(
                        $"{quest.rewardBPXP} BP XP",
                        "quest-reward-dot-xp",
                        "quest-reward-xp"));
                }

                if (quest.HasLootboxReward)
                {
                    string lbName =
                        quest.rewardLootbox
                            ?.lootboxName
                        ?? "Lootbox";
                    rewards.Add(BuildRewardItem(
                        lbName,
                        "quest-reward-dot-lootbox",
                        "quest-reward-lootbox"));
                }

                bottomRow.Add(rewards);

                if (quest.isCompleted)
                {
                    var claimBtn = new Button();
                    claimBtn.text =
                        quest.HasLootboxReward
                            ? "CLAIM BOX!"
                            : "CLAIM";
                    claimBtn.AddToClassList(
                        "quest-btn-claim");

                    Quest captured = quest;
                    claimBtn
                        .RegisterCallback<ClickEvent>(
                        evt =>
                        {
                            PlaySound(claimSound);
                            ClaimQuest(captured);
                            evt.StopPropagation();
                        });

                    bottomRow.Add(claimBtn);
                }
            }

            slot.Add(bottomRow);

            return slot;
        }

        private VisualElement BuildRewardItem(
            string text, string dotClass,
            string textClass)
        {
            var item = new VisualElement();
            item.AddToClassList(
                "quest-reward-item");

            var dot = new VisualElement();
            dot.AddToClassList("quest-reward-dot");
            dot.AddToClassList(dotClass);
            item.Add(dot);

            var label = new Label(text);
            label.AddToClassList(
                "quest-reward-text");
            label.AddToClassList(textClass);
            item.Add(label);

            return item;
        }

        // ==========================================
        // CLAIM
        // ==========================================

        private void ClaimQuest(Quest quest)
        {
            if (quest == null || quest.isClaimed ||
                !quest.isCompleted) return;

            Debug.Log($"[QuestPanelUI] Claiming quest: {quest.description}, hasLootbox: {quest.HasLootboxReward}");
            QuestManager.Instance?.ClaimReward(quest);
        }

        // ==========================================
        // NOTIFICATION BADGE
        // ==========================================

        private void UpdateNotificationBadge()
        {
            if (QuestManager.Instance == null) return;

            int claimable =
                QuestManager.Instance.activeQuests
                    .Count(q =>
                        q.isCompleted &&
                        !q.isClaimed);

            if (claimable > 0)
            {
                SetVisible(notificationBadge, true);
                if (notificationCount != null)
                    notificationCount.text =
                        claimable > 9
                            ? "9+"
                            : claimable.ToString();
            }
            else
            {
                SetVisible(notificationBadge, false);
            }
        }

        // ==========================================
        // TIMER
        // ==========================================

        private void StartTimerUpdate()
        {
            if (timerCoroutine != null)
                StopCoroutine(timerCoroutine);
            timerCoroutine =
                StartCoroutine(TimerUpdateLoop());
        }

        private void StopTimerUpdate()
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
        }

        private IEnumerator TimerUpdateLoop()
        {
            while (true)
            {
                UpdateResetTimer();
                UpdateClaimedTimers();
                yield return
                    new WaitForSecondsRealtime(1f);
            }
        }

        private void UpdateResetTimer()
        {
            if (resetTimerLabel == null) return;

            TimeSpan untilMidnight =
                DateTime.Today.AddDays(1) -
                DateTime.Now;

            resetTimerLabel.text =
                $"DAILY RESET IN " +
                $"{untilMidnight:hh\\:mm\\:ss}";
        }

        private void UpdateClaimedTimers()
        {
            if (QuestManager.Instance == null ||
                questList == null) return;

            foreach (var quest in
                QuestManager.Instance.activeQuests)
            {
                if (!quest.isClaimed) continue;

                var timerLabel = questList.Q<Label>(
                    $"timer-{quest.questID}");

                if (timerLabel != null)
                    timerLabel.text =
                        GetTimerText(quest.tier);
            }
        }

        private string GetTimerText(QuestTier tier)
        {
            TimeSpan remaining;
            string prefix;

            switch (tier)
            {
                case QuestTier.Weekly:
                    remaining =
                        GetTimeUntilNextMonday();
                    prefix = "New weekly in:";
                    break;
                case QuestTier.Special:
                    remaining =
                        GetTimeUntilMidnight();
                    prefix = "Event ends in:";
                    break;
                default:
                    remaining =
                        GetTimeUntilMidnight();
                    prefix = "New quest in:";
                    break;
            }

            if (remaining.TotalDays >= 1)
                return $"{prefix} " +
                    $"{remaining.Days}d " +
                    $"{remaining.Hours}h";

            return $"{prefix} " +
                $"{remaining:hh\\:mm\\:ss}";
        }

        private TimeSpan GetTimeUntilMidnight()
        {
            return DateTime.Today.AddDays(1) -
                DateTime.Now;
        }

        private TimeSpan GetTimeUntilNextMonday()
        {
            DateTime now = DateTime.Now;
            int days =
                ((int)DayOfWeek.Monday -
                (int)now.DayOfWeek + 7) % 7;
            if (days == 0) days = 7;
            return DateTime.Today.AddDays(days) - now;
        }

        // ==========================================
        // EVENT HANDLERS
        // ==========================================

        private void OnQuestsUpdated()
        {
            RefreshQuests();
            UpdateNotificationBadge();
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private string GetTierClass(QuestTier tier)
        {
            return tier switch
            {
                QuestTier.Daily =>
                    "quest-tier-daily",
                QuestTier.Weekly =>
                    "quest-tier-weekly",
                QuestTier.Special =>
                    "quest-tier-special",
                _ => "quest-tier-daily"
            };
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

        private bool IsChildOf(
            VisualElement child,
            VisualElement parent)
        {
            if (child == null || parent == null)
                return false;

            var current = child;
            while (current != null)
            {
                if (current == parent) return true;
                current = current.parent;
            }
            return false;
        }

        private void PlayClick()
        {
            PlaySound(buttonClickSound);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip, 0.7f);
        }
    }
}//// Assets/PrzemekSkrypty/Quest/QuestUI.cs
 //using UnityEngine;
 //using UnityEngine.UI;
 //using TMPro;
 //using ElementumDefense.Progression;
 //using System.Collections.Generic;
 //using System.Linq;

//public class QuestUI : MonoBehaviour
//{
//    [Header("Container")]
//    [SerializeField] private Transform container;
//    [SerializeField] private GameObject questSlotPrefab;

//    [Header("Scroll View")]
//    [SerializeField] private ScrollRect scrollRect;
//    [SerializeField] private RectTransform viewportRect;

//    [Header("Scroll Indicators")]
//    [SerializeField] private GameObject scrollUpIndicator;
//    [SerializeField] private GameObject scrollDownIndicator;

//    [Header("Settings")]
//    //[SerializeField] private float slotHeight = 100f; // Wysoko�� jednego slotu
//    [SerializeField] private int visibleSlots = 3;    // Ile slot�w widocznych
//    [SerializeField] private float scrollIndicatorThreshold = 0.05f;

//    private List<GameObject> spawnedSlots = new List<GameObject>();
//    private QuestTier? currentFilter = null;

//    private void Start()
//    {
//        if (QuestManager.Instance != null)
//        {
//            QuestManager.Instance.OnQuestListUpdated += RefreshUI;
//            RefreshUI();
//        }

//        SetupScrollListeners();
//    }

//    private void OnDestroy()
//    {
//        if (QuestManager.Instance != null)
//        {
//            QuestManager.Instance.OnQuestListUpdated -= RefreshUI;
//        }
//    }

//    private void OnEnable()
//    {
//        RefreshUI();
//    }

//    private void SetupScrollListeners()
//    {
//        if (scrollRect != null)
//        {
//            scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
//        }
//    }

//    /// <summary>
//    /// Filters quests by tier
//    /// </summary>
//    public void FilterQuests(QuestTier? tier)
//    {
//        currentFilter = tier;
//        RefreshUI();

//        // Update tab visuals
//        UpdateTabVisuals();
//    }

//    private void UpdateTabVisuals()
//    {
//        // Mo�esz tu zmieni� kolory/stany przycisk�w zak�adek
//        // np. pod�wietli� aktywn� zak�adk�
//    }

//    public void RefreshUI()
//    {
//        if (QuestManager.Instance == null) return;

//        // Clear old slots
//        foreach (var slot in spawnedSlots)
//        {
//            if (slot != null)
//                Destroy(slot);
//        }
//        spawnedSlots.Clear();

//        // Get quests (filtered or all)
//        List<Quest> questsToShow;

//        if (currentFilter.HasValue)
//        {
//            questsToShow = QuestManager.Instance.GetQuestsByTier(currentFilter.Value);
//        }
//        else
//        {
//            questsToShow = QuestManager.Instance.activeQuests;
//        }

//        // Sort: unclaimed first, then by tier (daily, weekly, special)
//        questsToShow = questsToShow
//            .OrderBy(q => q.isClaimed ? 1 : 0)
//            .ThenBy(q => q.tier)
//            .ThenBy(q => q.isCompleted ? 0 : 1)
//            .ToList();

//        // Spawn quest slots
//        foreach (var quest in questsToShow)
//        {
//            GameObject slotObj = Instantiate(questSlotPrefab, container);
//            spawnedSlots.Add(slotObj);

//            QuestSlotUI slotScript = slotObj.GetComponent<QuestSlotUI>();
//            if (slotScript != null)
//            {
//                slotScript.Setup(quest);
//            }
//        }



//        // Update scroll indicators
//        UpdateScrollIndicators();

//        // Force layout rebuild
//        StartCoroutine(ForceLayoutRebuild());

//        Debug.Log($"[QuestUI] Refreshed - showing {questsToShow.Count} quests");
//    }



//    private void OnScrollValueChanged(Vector2 scrollPosition)
//    {
//        UpdateScrollIndicators();
//    }

//    private void UpdateScrollIndicators()
//    {
//        if (scrollRect == null) return;

//        int totalQuests = spawnedSlots.Count;
//        bool canScroll = totalQuests > visibleSlots;

//        // Scroll up indicator (pokazuje si� gdy nie jeste�my na g�rze)
//        if (scrollUpIndicator != null)
//        {
//            bool showUp = canScroll && scrollRect.verticalNormalizedPosition < (1f - scrollIndicatorThreshold);
//            scrollUpIndicator.SetActive(showUp);
//        }

//        // Scroll down indicator (pokazuje si� gdy nie jeste�my na dole)
//        if (scrollDownIndicator != null)
//        {
//            bool showDown = canScroll && scrollRect.verticalNormalizedPosition > scrollIndicatorThreshold;
//            scrollDownIndicator.SetActive(showDown);
//        }

//    }

//    private System.Collections.IEnumerator ForceLayoutRebuild()
//    {
//        yield return null;

//        if (container != null)
//        {
//            LayoutRebuilder.ForceRebuildLayoutImmediate(container as RectTransform);
//        }

//        // Reset scroll to top
//        if (scrollRect != null)
//        {
//            scrollRect.verticalNormalizedPosition = 1f;
//        }

//        UpdateScrollIndicators();
//    }

//    // ==========================================
//    // PUBLIC API
//    // ==========================================

//    /// <summary>
//    /// Scrolls to show a specific quest
//    /// </summary>
//    public void ScrollToQuest(Quest quest)
//    {
//        int index = QuestManager.Instance?.activeQuests.IndexOf(quest) ?? -1;
//        if (index < 0 || scrollRect == null) return;

//        float normalizedPosition = 1f - ((float)index / Mathf.Max(1, spawnedSlots.Count - visibleSlots));
//        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPosition);
//    }

//    /// <summary>
//    /// Scrolls to first unclaimed quest
//    /// </summary>
//    public void ScrollToFirstUnclaimed()
//    {
//        var unclaimed = QuestManager.Instance?.activeQuests.FirstOrDefault(q => !q.isClaimed);
//        if (unclaimed != null)
//        {
//            ScrollToQuest(unclaimed);
//        }
//    }
//}