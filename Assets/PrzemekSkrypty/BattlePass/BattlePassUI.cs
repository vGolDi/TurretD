// Assets/PrzemekSkrypty/BattlePass/BattlePassUI.cs
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using ElementumDefense.Cards;

namespace ElementumDefense.BattlePass
{
    /// <summary>
    /// UI Toolkit panel for the Battle Pass.
    /// Shows season info, tier progression, rewards (free + premium tracks),
    /// and allows claiming/purchasing.
    /// 
    /// Attach to a GameObject with UIDocument component.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class BattlePassUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MainMenuController mainMenu;

        private UIDocument uiDocument;
        private VisualElement root;

        // Header
        private Label headerGold;
        private Label headerCrystals;
        private Label seasonTitle;
        private Label seasonTimer;
        private Label xpLabel;
        private VisualElement xpBarFill;
        private Label tierLabel;

        // Tracks
        private VisualElement tiersContainer;
        private ScrollView tiersScrollView;

        // Premium
        private Button btnBuyPremium;
        private VisualElement premiumBadge;

        // Claim all
        private Button btnClaimAll;

        // Back
        private Button btnBack;

        // ==========================================
        // LIFECYCLE
        // ==========================================

        private void Awake()
        {
            uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (uiDocument != null)
                root = uiDocument.rootVisualElement;

            if (root != null)
                root.style.display = DisplayStyle.None;
        }

        public void Show()
        {
            if (root == null)
            {
                uiDocument = GetComponent<UIDocument>();
                if (uiDocument != null)
                    root = uiDocument.rootVisualElement;
            }
            if (root == null) return;

            root.style.display = DisplayStyle.Flex;

            QueryElements();
            BindButtons();
            SubscribeEvents();
            RefreshAll();

            // Starfield background
            var bg = root.Q<VisualElement>("bp-root") ?? root;
            ElementumDefense.UI.StarfieldInjector.Instance?.Register(bg);
        }

        public void Hide()
        {
            // Starfield cleanup
            if (root != null)
            {
                var bg = root.Q<VisualElement>("bp-root") ?? root;
                ElementumDefense.UI.StarfieldInjector.Instance?.Unregister(bg);
            }

            UnsubscribeEvents();

            if (root == null)
            {
                uiDocument = GetComponent<UIDocument>();
                if (uiDocument != null)
                    root = uiDocument.rootVisualElement;
            }
            if (root != null)
                root.style.display = DisplayStyle.None;
        }

        // ==========================================
        // QUERY & BIND
        // ==========================================

        private void QueryElements()
        {
            headerGold = root.Q<Label>("header-gold");
            headerCrystals = root.Q<Label>("header-crystals");
            seasonTitle = root.Q<Label>("bp-season-title");
            seasonTimer = root.Q<Label>("bp-season-timer");
            xpLabel = root.Q<Label>("bp-xp-label");
            xpBarFill = root.Q<VisualElement>("bp-xp-bar-fill");
            tierLabel = root.Q<Label>("bp-tier-label");
            tiersContainer = root.Q<VisualElement>("bp-tiers-container");
            tiersScrollView = root.Q<ScrollView>("bp-tiers-scroll");
            btnBuyPremium = root.Q<Button>("btn-buy-premium");
            premiumBadge = root.Q<VisualElement>("bp-premium-badge");
            btnClaimAll = root.Q<Button>("btn-claim-all");
            btnBack = root.Q<Button>("btn-back");
        }

        private void BindButtons()
        {
            btnBack?.RegisterCallback<ClickEvent>(e => mainMenu?.BackToMainMenu());

            btnBuyPremium?.RegisterCallback<ClickEvent>(e => OnBuyPremiumClicked());

            btnClaimAll?.RegisterCallback<ClickEvent>(e => OnClaimAllClicked());
        }

        private void SubscribeEvents()
        {
            var bp = BattlePassManager.Instance;
            if (bp != null)
            {
                bp.OnXPChanged += HandleXPChanged;
                bp.OnRewardClaimed += HandleRewardClaimed;
                bp.OnPremiumPurchased += HandlePremiumPurchased;
            }
        }

        private void UnsubscribeEvents()
        {
            var bp = BattlePassManager.Instance;
            if (bp != null)
            {
                bp.OnXPChanged -= HandleXPChanged;
                bp.OnRewardClaimed -= HandleRewardClaimed;
                bp.OnPremiumPurchased -= HandlePremiumPurchased;
            }
        }

        // ==========================================
        // REFRESH
        // ==========================================

        private void RefreshAll()
        {
            RefreshCurrency();
            RefreshSeasonInfo();
            RefreshXPBar();
            RefreshPremiumState();
            RefreshTiers();
        }

        private void RefreshCurrency()
        {
            var pc = PlayerCollection.Instance;
            if (pc == null) return;

            if (headerGold != null)
                headerGold.text = pc.GetGold().ToString();
            if (headerCrystals != null)
                headerCrystals.text = pc.GetCrystals().ToString();
        }

        private void RefreshSeasonInfo()
        {
            var bp = BattlePassManager.Instance;
            if (bp == null || bp.CurrentSeason == null) return;

            var season = bp.CurrentSeason;

            if (seasonTitle != null)
                seasonTitle.text = season.seasonName.ToUpper();

            if (seasonTimer != null)
            {
                int days = season.GetRemainingDays();
                seasonTimer.text = days > 0 ? $"{days} DAYS LEFT" : "SEASON ENDED";
            }
        }

        private void RefreshXPBar()
        {
            var bp = BattlePassManager.Instance;
            if (bp == null) return;

            int currentTier = bp.CurrentTier;
            var (progressCurrent, progressTotal) = bp.GetTierProgress();

            if (tierLabel != null)
                tierLabel.text = $"TIER {currentTier}";

            if (xpLabel != null)
            {
                if (progressTotal <= 0)
                    xpLabel.text = "MAX TIER";
                else
                    xpLabel.text = $"{progressCurrent} / {progressTotal} XP";
            }

            if (xpBarFill != null)
            {
                float percent = progressTotal > 0 ? (float)progressCurrent / progressTotal * 100f : 100f;
                xpBarFill.style.width = new StyleLength(new Length(percent, LengthUnit.Percent));
            }
        }

        private void RefreshPremiumState()
        {
            var bp = BattlePassManager.Instance;
            if (bp == null) return;

            bool hasPremium = bp.HasPremium;

            if (btnBuyPremium != null)
                btnBuyPremium.style.display = hasPremium ? DisplayStyle.None : DisplayStyle.Flex;

            if (premiumBadge != null)
                premiumBadge.style.display = hasPremium ? DisplayStyle.Flex : DisplayStyle.None;

            // Update premium button text with price
            if (!hasPremium && btnBuyPremium != null && bp.CurrentSeason != null)
            {
                btnBuyPremium.text = $"UNLOCK PREMIUM — {bp.CurrentSeason.premiumPriceCrystals} 💎";
            }
        }

        private void RefreshTiers()
        {
            var bp = BattlePassManager.Instance;
            if (bp == null || bp.CurrentSeason == null) return;
            if (tiersContainer == null && tiersScrollView == null) return;

            var container = tiersScrollView?.contentContainer ?? tiersContainer;
            if (container == null) return;

            container.Clear();

            var season = bp.CurrentSeason;
            int currentTier = bp.CurrentTier;
            bool hasPremium = bp.HasPremium;

            for (int i = 0; i < season.TotalTiers; i++)
            {
                int tierNum = i + 1;
                var tierData = season.GetTier(tierNum);
                if (tierData == null) continue;

                var tierElement = CreateTierElement(tierNum, tierData, currentTier, hasPremium);
                container.Add(tierElement);
            }

            // Scroll to current tier
            if (tiersScrollView != null && currentTier > 0)
            {
                // Delay scroll to let layout settle
                root.schedule.Execute(() =>
                {
                    float scrollTarget = (currentTier - 1) * 120f; // approximate tier width
                    tiersScrollView.scrollOffset = new Vector2(scrollTarget, 0);
                }).ExecuteLater(50);
            }
        }

        // ==========================================
        // TIER ELEMENT CREATION
        // ==========================================

        private VisualElement CreateTierElement(int tierNum, BattlePassTierData tierData, int currentTier, bool hasPremium)
        {
            var bp = BattlePassManager.Instance;

            var wrapper = new VisualElement();
            wrapper.AddToClassList("bp-tier");

            bool isReached = tierNum <= currentTier;
            bool isCurrent = tierNum == currentTier + 1;

            if (isReached) wrapper.AddToClassList("bp-tier-reached");
            if (isCurrent) wrapper.AddToClassList("bp-tier-current");

            // Tier number header
            var tierHeader = new Label($"{tierNum}");
            tierHeader.AddToClassList("bp-tier-number");
            wrapper.Add(tierHeader);

            // Premium reward (top track)
            var premiumSlot = CreateRewardSlot(tierNum, tierData.premiumReward, true, isReached, hasPremium, bp);
            wrapper.Add(premiumSlot);

            // Connector line
            var connector = new VisualElement();
            connector.AddToClassList("bp-tier-connector");
            if (isReached) connector.AddToClassList("bp-tier-connector-active");
            wrapper.Add(connector);

            // Free reward (bottom track)
            var freeSlot = CreateRewardSlot(tierNum, tierData.freeReward, false, isReached, hasPremium, bp);
            wrapper.Add(freeSlot);

            return wrapper;
        }

        private VisualElement CreateRewardSlot(int tierNum, BattlePassRewardData reward, bool isPremium, bool isReached, bool hasPremium, BattlePassManager bp)
        {
            var slot = new VisualElement();
            slot.AddToClassList("bp-reward-slot");
            if (isPremium) slot.AddToClassList("bp-reward-premium");
            else slot.AddToClassList("bp-reward-free");

            if (reward == null)
            {
                slot.AddToClassList("bp-reward-empty");
                return slot;
            }

            // Icon
            var icon = new VisualElement();
            icon.AddToClassList("bp-reward-icon");
            var sprite = reward.GetIcon();
            if (sprite != null)
                icon.style.backgroundImage = new StyleBackground(sprite);
            slot.Add(icon);

            // Reward name
            var nameLabel = new Label(reward.GetDisplayName());
            nameLabel.AddToClassList("bp-reward-name");
            slot.Add(nameLabel);

            // State overlay
            bool claimed = isPremium ? bp.IsPremiumClaimed(tierNum) : bp.IsFreeClaimed(tierNum);
            bool canClaim = isPremium ? bp.CanClaimPremiumReward(tierNum) : bp.CanClaimFreeReward(tierNum);

            if (claimed)
            {
                slot.AddToClassList("bp-reward-claimed");
                var checkmark = new Label("✓");
                checkmark.AddToClassList("bp-reward-checkmark");
                slot.Add(checkmark);
            }
            else if (canClaim)
            {
                slot.AddToClassList("bp-reward-claimable");
                var claimBtn = new Button(() => OnClaimReward(tierNum, isPremium));
                claimBtn.text = "CLAIM";
                claimBtn.AddToClassList("bp-claim-btn");
                slot.Add(claimBtn);
            }
            else if (isPremium && !hasPremium && isReached)
            {
                // Reached but locked behind premium
                slot.AddToClassList("bp-reward-locked");
                var lockIcon = new Label("🔒");
                lockIcon.AddToClassList("bp-reward-lock");
                slot.Add(lockIcon);
            }

            return slot;
        }

        // ==========================================
        // ACTIONS
        // ==========================================

        private void OnBuyPremiumClicked()
        {
            var bp = BattlePassManager.Instance;
            if (bp == null) return;

            bool success = bp.PurchasePremium();
            if (success)
            {
                RefreshAll();
                Debug.Log("[BattlePassUI] Premium purchased!");
            }
            else
            {
                Debug.Log("[BattlePassUI] Premium purchase failed (not enough crystals or already owned).");
                // TODO: Show error popup
            }
        }

        private void OnClaimReward(int tierNum, bool isPremium)
        {
            var bp = BattlePassManager.Instance;
            if (bp == null) return;

            // Save scroll position before refresh
            Vector2 scrollPos = tiersScrollView != null ? tiersScrollView.scrollOffset : Vector2.zero;

            bool success = isPremium ? bp.ClaimPremiumReward(tierNum) : bp.ClaimFreeReward(tierNum);
            if (success)
            {
                RefreshCurrency();
                RefreshTiersKeepScroll(scrollPos);
            }
        }

        private void OnClaimAllClicked()
        {
            var bp = BattlePassManager.Instance;
            if (bp == null) return;

            // Save scroll position before refresh
            Vector2 scrollPos = tiersScrollView != null ? tiersScrollView.scrollOffset : Vector2.zero;

            bp.ClaimAllAvailable();
            RefreshCurrency();
            RefreshTiersKeepScroll(scrollPos);
        }

        /// <summary>
        /// Rebuilds tiers but restores the given scroll position instead of jumping to current tier.
        /// </summary>
        private void RefreshTiersKeepScroll(Vector2 scrollPos)
        {
            var bp = BattlePassManager.Instance;
            if (bp == null || bp.CurrentSeason == null) return;
            if (tiersContainer == null && tiersScrollView == null) return;

            var container = tiersScrollView?.contentContainer ?? tiersContainer;
            if (container == null) return;

            container.Clear();

            var season = bp.CurrentSeason;
            int currentTier = bp.CurrentTier;
            bool hasPremium = bp.HasPremium;

            for (int i = 0; i < season.TotalTiers; i++)
            {
                int tierNum = i + 1;
                var tierData = season.GetTier(tierNum);
                if (tierData == null) continue;

                var tierElement = CreateTierElement(tierNum, tierData, currentTier, hasPremium);
                container.Add(tierElement);
            }

            // Restore scroll position
            if (tiersScrollView != null)
            {
                root.schedule.Execute(() =>
                {
                    tiersScrollView.scrollOffset = scrollPos;
                }).ExecuteLater(50);
            }
        }

        // ==========================================
        // EVENT HANDLERS
        // ==========================================

        private void HandleXPChanged(int xp, int tier)
        {
            RefreshXPBar();
            RefreshTiers();
        }

        private void HandleRewardClaimed(int tier, bool isPremium)
        {
            RefreshCurrency();
            RefreshTiers();
        }

        private void HandlePremiumPurchased()
        {
            RefreshAll();
        }
    }
}
