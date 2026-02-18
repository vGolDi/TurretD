using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using ElementumDefense.Cards;
using ElementumDefense.Progression;

namespace ElementumDefense.UI
{
    /// <summary>
    /// Level-up popup — UI Toolkit.
    /// 4 visual tiers: Bronze(1-10), Silver(11-25),
    /// Gold(26-50), Diamond(51+).
    /// Listens to PlayerCollection.OnLevelChanged.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class LevelUpPopup : MonoBehaviour
    {
        public static LevelUpPopup Instance
        { get; private set; }

        [Header("Audio")]
        [SerializeField] private AudioClip levelUpSound;
        [SerializeField]
        private AudioClip legendaryLevelUpSound;

        [Header("Settings")]
        [SerializeField] private float autoHideDelay = 0f;

        [Header("Level Rewards Config")]
        [SerializeField]
        private LevelRewardsConfig rewardsConfig;

        private AudioSource audioSource;
        private VisualElement root;

        // Elements
        private VisualElement popupRoot;
        private VisualElement popupBackdrop;
        private VisualElement popupBox;
        private Label titleLabel;
        private Label levelNumber;
        private Label tierName;
        private VisualElement glowOuter;
        private VisualElement glowInner;
        private VisualElement hexBg;
        private VisualElement hexBorder;

        // Rewards
        private VisualElement rewardGoldRow;
        private Label rewardGoldText;
        private VisualElement rewardCrystalsRow;
        private Label rewardCrystalsText;
        private VisualElement rewardLootboxRow;
        private Label rewardLootboxText;
        private VisualElement rewardCardRow;
        private Label rewardCardText;
        private Label customMessage;

        // XP
        private VisualElement xpFill;
        private Label xpText;

        // Button
        private Button btnOk;

        // State
        private int lastKnownLevel = 0;
        private bool isShowing = false;

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
            DontDestroyOnLoad(gameObject);

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

            HideImmediate();

            if (PlayerCollection.Instance != null)
                lastKnownLevel =
                    PlayerCollection.Instance
                        .GetLevel();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            if (Instance == this)
                Instance = null;
        }

        // ==========================================
        // QUERY
        // ==========================================

        private void QueryElements()
        {
            popupRoot =
                root.Q<VisualElement>("levelup-root");
            popupBackdrop =
                root.Q<VisualElement>(
                    "levelup-backdrop");
            popupBox =
                root.Q<VisualElement>("levelup-box");
            titleLabel =
                root.Q<Label>("levelup-title");
            levelNumber =
                root.Q<Label>(
                    "levelup-level-number");
            tierName =
                root.Q<Label>("levelup-tier-name");
            glowOuter =
                root.Q<VisualElement>(
                    "levelup-glow-outer");
            glowInner =
                root.Q<VisualElement>(
                    "levelup-glow-inner");
            hexBg =
                root.Q<VisualElement>(
                    "levelup-hex-bg");
            hexBorder =
                root.Q<VisualElement>(
                    "levelup-hex-border");

            rewardGoldRow =
                root.Q<VisualElement>(
                    "reward-gold-row");
            rewardGoldText =
                root.Q<Label>("reward-gold-text");
            rewardCrystalsRow =
                root.Q<VisualElement>(
                    "reward-crystals-row");
            rewardCrystalsText =
                root.Q<Label>(
                    "reward-crystals-text");
            rewardLootboxRow =
                root.Q<VisualElement>(
                    "reward-lootbox-row");
            rewardLootboxText =
                root.Q<Label>(
                    "reward-lootbox-text");
            rewardCardRow =
                root.Q<VisualElement>(
                    "reward-card-row");
            rewardCardText =
                root.Q<Label>("reward-card-text");
            customMessage =
                root.Q<Label>(
                    "levelup-custom-message");

            xpFill =
                root.Q<VisualElement>(
                    "levelup-xp-fill");
            xpText =
                root.Q<Label>("levelup-xp-text");

            btnOk =
                root.Q<Button>("btn-levelup-ok");
        }

        // ==========================================
        // BIND
        // ==========================================

        private void BindButtons()
        {
            btnOk?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    HidePopup();
                    evt.StopPropagation();
                });

            popupBackdrop?
                .RegisterCallback<ClickEvent>(
                evt =>
                {
                    HidePopup();
                    evt.StopPropagation();
                });
        }

        // ==========================================
        // EVENTS
        // ==========================================

        private void SubscribeEvents()
        {
            if (PlayerCollection.Instance != null)
            {
                PlayerCollection.Instance
                    .OnLevelChanged -=
                    OnLevelChanged;
                PlayerCollection.Instance
                    .OnLevelChanged +=
                    OnLevelChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (PlayerCollection.Instance != null)
            {
                PlayerCollection.Instance
                    .OnLevelChanged -=
                    OnLevelChanged;
            }
        }

        // ==========================================
        // EVENT HANDLER
        // ==========================================

        private void OnLevelChanged(int newLevel)
        {
            if (newLevel > lastKnownLevel &&
                lastKnownLevel > 0)
            {
                ShowLevelUp(newLevel);
            }
            lastKnownLevel = newLevel;
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        public void ShowLevelUp(int level)
        {
            if (popupRoot == null) return;

            isShowing = true;

            // Level number
            if (levelNumber != null)
                levelNumber.text = level.ToString();

            // Tier visuals
            ApplyTier(level);

            // Rewards
            ShowRewards(level);

            // XP bar (reset for new level)
            if (xpFill != null)
                xpFill.style.width =
                    new StyleLength(
                        new Length(0, LengthUnit.Percent));

            if (xpText != null)
            {
                int required =
                    PlayerCollection.Instance?
                        .GetXPForNextLevel() ?? 1000;
                xpText.text = $"0 / {required} XP";
            }

            // Show
            popupRoot.RemoveFromClassList("hidden");

            // Sound
            PlayLevelUpSound(level);

            // Auto hide
            if (autoHideDelay > 0)
                StartCoroutine(
                    AutoHideCoroutine());

            Debug.Log(
                $"[LevelUpPopup] Showing level " +
                $"{level} ({GetTierName(level)})");
        }

        public void HidePopup()
        {
            HideImmediate();
            isShowing = false;
        }

        private void HideImmediate()
        {
            popupRoot?.AddToClassList("hidden");
        }

        // ==========================================
        // TIER SYSTEM
        // ==========================================

        private void ApplyTier(int level)
        {
            if (popupBox == null) return;

            // Remove all tier classes
            popupBox.RemoveFromClassList(
                "tier-bronze");
            popupBox.RemoveFromClassList(
                "tier-silver");
            popupBox.RemoveFromClassList(
                "tier-gold");
            popupBox.RemoveFromClassList(
                "tier-diamond");

            // Apply new tier
            string tierClass = GetTierClass(level);
            if (!string.IsNullOrEmpty(tierClass))
                popupBox.AddToClassList(tierClass);

            // Tier name label
            if (tierName != null)
                tierName.text = GetTierName(level);
        }

        private string GetTierClass(int level)
        {
            if (level >= 51) return "tier-diamond";
            if (level >= 26) return "tier-gold";
            if (level >= 11) return "tier-silver";
            return ""; // Bronze = default style
        }

        private string GetTierName(int level)
        {
            if (level >= 51) return "DIAMOND";
            if (level >= 26) return "GOLD";
            if (level >= 11) return "SILVER";
            return "BRONZE";
        }

        // ==========================================
        // REWARDS DISPLAY
        // ==========================================

        private void ShowRewards(int level)
        {
            LevelReward reward = null;

            if (rewardsConfig != null)
                reward =
                    rewardsConfig
                        .GetRewardsForLevel(level);

            // Gold
            if (rewardGoldRow != null)
            {
                int gold = reward?.gold ?? 500;
                if (gold > 0)
                {
                    SetVisible(rewardGoldRow, true);
                    if (rewardGoldText != null)
                        rewardGoldText.text =
                            $"+{gold} Gold";
                }
                else
                {
                    SetVisible(rewardGoldRow, false);
                }
            }

            // Crystals
            if (rewardCrystalsRow != null)
            {
                int crystals = reward?.crystals ?? 10;
                if (crystals > 0)
                {
                    SetVisible(
                        rewardCrystalsRow, true);
                    if (rewardCrystalsText != null)
                        rewardCrystalsText.text =
                            $"+{crystals} Crystals";
                }
                else
                {
                    SetVisible(
                        rewardCrystalsRow, false);
                }
            }

            // Lootbox
            if (rewardLootboxRow != null)
            {
                bool hasLootbox =
                    reward?.lootbox != null;
                SetVisible(
                    rewardLootboxRow, hasLootbox);
                if (hasLootbox &&
                    rewardLootboxText != null)
                    rewardLootboxText.text =
                        $"+1 {reward.lootbox.lootboxName}";
            }

            // Card unlock
            if (rewardCardRow != null)
            {
                bool hasCard =
                    reward?.unlockCard != null;
                SetVisible(rewardCardRow, hasCard);
                if (hasCard &&
                    rewardCardText != null)
                    rewardCardText.text =
                        $"Unlocked: " +
                        $"{reward.unlockCard.cardName}!";
            }

            // Custom message
            if (customMessage != null)
            {
                bool hasMessage =
                    reward != null &&
                    !string.IsNullOrEmpty(
                        reward.customMessage);
                SetVisible(
                    customMessage, hasMessage);
                if (hasMessage)
                    customMessage.text =
                        reward.customMessage;
            }
        }

        // ==========================================
        // AUDIO
        // ==========================================

        private void PlayLevelUpSound(int level)
        {
            if (audioSource == null) return;

            AudioClip clip = levelUpSound;

            // Diamond tier gets special sound
            if (level >= 51 &&
                legendaryLevelUpSound != null)
                clip = legendaryLevelUpSound;

            if (clip != null)
                audioSource.PlayOneShot(clip);
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private void SetVisible(
            VisualElement element, bool visible)
        {
            if (element == null) return;
            if (visible)
                element.RemoveFromClassList("hidden");
            else
                element.AddToClassList("hidden");
        }

        private IEnumerator AutoHideCoroutine()
        {
            yield return new WaitForSecondsRealtime(
                autoHideDelay);
            if (isShowing)
                HidePopup();
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Test Bronze (Level 5)")]
        private void TestBronze()
        { ShowLevelUp(5); }

        [ContextMenu("Test Silver (Level 15)")]
        private void TestSilver()
        { ShowLevelUp(15); }

        [ContextMenu("Test Gold (Level 30)")]
        private void TestGold()
        { ShowLevelUp(30); }

        [ContextMenu("Test Diamond (Level 55)")]
        private void TestDiamond()
        { ShowLevelUp(55); }
    }
}