using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ElementumDefense.Cards;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class ProfileUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MainMenuController mainMenu;

        private UIDocument uiDocument;
        private VisualElement root;

        // Header
        private Label headerGold;
        private Label headerCrystals;

        // Identity card
        private Label profileName;
        private Label profileLevel;
        private Label profileRank;
        private Label profileElo;
        private Label profileXpText;
        private VisualElement profileXpFill;
        private Label profileNextLevel;
        private VisualElement rankEmblemBg;
        private Label rankIcon;

        // Stats
        private Label statCards;
        private Label statSabotages;
        private Label statDecks;
        private Label statWins;
        private Label statLosses;

        // Tabs
        private Button tabCards;
        private Button tabSabotages;
        private Button tabSkins;
        private Button tabAchievements;

        private VisualElement contentCards;
        private VisualElement contentSabotages;
        private VisualElement contentSkins;
        private VisualElement contentAchievements;

        // Filters
        private Button filterAll;
        private Button filterCommon;
        private Button filterRare;
        private Button filterLegendary;
        private Label collectionCount;
        private VisualElement filterBar;

        // Grids
        private VisualElement cardsGrid;
        private VisualElement sabotagesGrid;
        private VisualElement achievementsGrid;

        // Detail popup
        private VisualElement detailPopup;
        private Label detailName;
        private Label detailRarity;
        private VisualElement detailRarityLine;
        private Label detailType;
        private Label detailElement;
        private Label detailDescription;
        private VisualElement detailIcon;
        private Label detailMaxCopies;
        private Label detailActivation;
        private Label detailOwnedText;
        private VisualElement detailOwnership;
        private VisualElement detailBox;
        private Button btnDetailClose;

        // State
        private string activeTab = "cards";
        private CardRarity? activeFilter = null;

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
            RefreshAll();

            // Starfield
            var bg = root.Q<VisualElement>(
                "profile-root") ?? root;
            StarfieldInjector.Instance?.Register(bg);
        }

        public void Hide()
        {
            if (root != null)
            {
                var bg = root.Q<VisualElement>(
                    "profile-root") ?? root;
                StarfieldInjector.Instance?.Unregister(bg);
            }

            if (root == null)
            {
                uiDocument = GetComponent<UIDocument>();
                if (uiDocument != null)
                    root = uiDocument.rootVisualElement;
            }

            if (root != null)
                root.style.display = DisplayStyle.None;
        }

        private void QueryElements()
        {
            // Header
            headerGold =
                root.Q<Label>("header-gold");
            headerCrystals =
                root.Q<Label>("header-crystals");

            // Identity
            profileName =
                root.Q<Label>("profile-name");
            profileLevel =
                root.Q<Label>("profile-level");
            profileRank =
                root.Q<Label>("profile-rank");
            profileElo =
                root.Q<Label>("profile-elo");
            profileXpText =
                root.Q<Label>("profile-xp-text");
            profileXpFill =
                root.Q<VisualElement>(
                    "profile-xp-bar-fill");
            profileNextLevel =
                root.Q<Label>("profile-next-level");
            rankEmblemBg =
                root.Q<VisualElement>(
                    "rank-emblem-bg");
            rankIcon =
                root.Q<Label>("rank-icon");

            // Stats
            statCards =
                root.Q<Label>("stat-cards");
            statSabotages =
                root.Q<Label>("stat-sabotages");
            statDecks =
                root.Q<Label>("stat-decks");
            statWins =
                root.Q<Label>("stat-wins");
            statLosses =
                root.Q<Label>("stat-losses");

            // Tabs
            tabCards =
                root.Q<Button>("tab-cards");
            tabSabotages =
                root.Q<Button>("tab-sabotages");
            tabSkins =
                root.Q<Button>("tab-skins");
            tabAchievements =
                root.Q<Button>("tab-achievements");

            contentCards =
                root.Q<VisualElement>(
                    "tab-content-cards");
            contentSabotages =
                root.Q<VisualElement>(
                    "tab-content-sabotages");
            contentSkins =
                root.Q<VisualElement>(
                    "tab-content-skins");
            contentAchievements =
                root.Q<VisualElement>(
                    "tab-content-achievements");

            // Filters
            filterAll =
                root.Q<Button>("filter-all");
            filterCommon =
                root.Q<Button>("filter-common");
            filterRare =
                root.Q<Button>("filter-rare");
            filterLegendary =
                root.Q<Button>("filter-legendary");
            collectionCount =
                root.Q<Label>("collection-count");
            filterBar =
                root.Q<VisualElement>("filter-bar");

            // Grids
            cardsGrid =
                root.Q<VisualElement>("cards-grid");
            sabotagesGrid =
                root.Q<VisualElement>(
                    "sabotages-grid");
            achievementsGrid =
                root.Q<VisualElement>(
                    "achievements-grid");

            // Detail popup
            detailPopup =
                root.Q<VisualElement>(
                    "card-detail-popup");
            detailName =
                root.Q<Label>("detail-name");
            detailRarity =
                root.Q<Label>("detail-rarity");
            detailRarityLine =
                root.Q<VisualElement>(
                    "detail-rarity-line");
            detailType =
                root.Q<Label>("detail-type");
            detailElement =
                root.Q<Label>("detail-element");
            detailDescription =
                root.Q<Label>("detail-description");
            detailIcon =
                root.Q<VisualElement>("detail-icon");
            detailMaxCopies =
                root.Q<Label>("detail-max-copies");
            detailActivation =
                root.Q<Label>("detail-activation");
            detailOwnedText =
                root.Q<Label>("detail-owned-text");
            detailOwnership =
                root.Q<VisualElement>(
                    "detail-ownership");
            detailBox =
                detailPopup?.Q<VisualElement>(
                    className: "detail-box");
            btnDetailClose =
                root.Q<Button>("btn-detail-close");
        }

        private void BindButtons()
        {
            // Back
            var btnBack =
                root.Q<Button>("btn-back");
            btnBack?.RegisterCallback
                <ClickEvent>(e =>
                {
                    mainMenu?.BackToMainMenu();
                });

            // Tabs
            tabCards?.RegisterCallback
                <ClickEvent>(e =>
                    SwitchTab("cards"));
            tabSabotages?.RegisterCallback
                <ClickEvent>(e =>
                    SwitchTab("sabotages"));
            tabSkins?.RegisterCallback
                <ClickEvent>(e =>
                    SwitchTab("skins"));
            tabAchievements?.RegisterCallback
                <ClickEvent>(e =>
                    SwitchTab("achievements"));

            // Filters
            filterAll?.RegisterCallback
                <ClickEvent>(e =>
                    SetFilter(null));
            filterCommon?.RegisterCallback
                <ClickEvent>(e =>
                    SetFilter(CardRarity.Common));
            filterRare?.RegisterCallback
                <ClickEvent>(e =>
                    SetFilter(CardRarity.Rare));
            filterLegendary?.RegisterCallback
                <ClickEvent>(e =>
                    SetFilter(CardRarity.Legendary));

            // Detail close
            btnDetailClose?.RegisterCallback
                <ClickEvent>(e =>
                    CloseDetailPopup());

            // Click overlay to close
            detailPopup?.RegisterCallback
                <ClickEvent>(e =>
                {
                    if (e.target == detailPopup)
                        CloseDetailPopup();
                });
        }

        // ══════════════════════════════
        // REFRESH
        // ══════════════════════════════

        private void RefreshAll()
        {
            RefreshIdentity();
            RefreshCurrency();
            RefreshStats();
            RefreshCurrentTab();
        }

        private void RefreshIdentity()
        {
            var pc = PlayerCollection.Instance;
            if (pc == null) return;

            // Name
            string username = "TRAVELER";
            if (Auth.AuthManager.Instance != null &&
                Auth.AuthManager.Instance.IsLoggedIn)
            {
                username = Auth.AuthManager.Instance
                    .CurrentUsername.ToUpper();
            }
            if (profileName != null)
                profileName.text = username;

            // Level
            int level = pc.GetLevel();
            if (profileLevel != null)
                profileLevel.text = level.ToString();

            // ── Tier color (based on level) ──
            Color tierColor = GetTierColor(level);

            // ── Rank color (based on ELO) ──
            string rankName = pc.GetRankName();
            Color rankColor = pc.GetRankColor();
            int elo = pc.GetElo();

            // ── Rank label — rank color (ELO-based) ──
            if (profileRank != null)
            {
                profileRank.text = rankName;
                profileRank.style.color =
                    new StyleColor(rankColor);
            }

            if (profileElo != null)
            {
                profileElo.text = $"{elo} ELO";
                profileElo.style.color =
                    new StyleColor(
                        new Color(
                            rankColor.r,
                            rankColor.g,
                            rankColor.b, 0.5f));
            }

            // ── Rank emblem — rank color (ELO-based) ──
            if (rankEmblemBg != null)
            {
                SetBorderColor(rankEmblemBg,
                    new Color(
                        rankColor.r,
                        rankColor.g,
                        rankColor.b, 0.4f));

                rankEmblemBg.style.backgroundColor =
                    new StyleColor(
                        new Color(
                            rankColor.r,
                            rankColor.g,
                            rankColor.b, 0.1f));
            }

            if (rankIcon != null)
                rankIcon.style.color =
                    new StyleColor(
                        new Color(
                            rankColor.r,
                            rankColor.g,
                            rankColor.b, 0.8f));

            // ── Avatar frame — tier color (level-based) ──
            var avatarFrame =
                root.Q<VisualElement>(
                    className: "avatar-frame");
            if (avatarFrame != null)
            {
                SetBorderColor(avatarFrame,
                    new Color(
                        tierColor.r,
                        tierColor.g,
                        tierColor.b, 0.35f));

                var corners = avatarFrame
                    .Query<VisualElement>(
                        className: "avatar-frame-corner")
                    .ToList();
                foreach (var corner in corners)
                {
                    SetBorderColor(corner,
                        new Color(
                            tierColor.r,
                            tierColor.g,
                            tierColor.b, 0.5f));
                }
            }

            // ── Level badge — tier color (level-based) ──
            var avatarSection =
                root.Q<VisualElement>(
                    className: "identity-avatar-section");
            if (avatarSection != null)
            {
                var badgeBg = avatarSection
                    .Q<VisualElement>(
                        className: "level-badge-bg");
                if (badgeBg != null)
                {
                    badgeBg.style.backgroundColor =
                        new StyleColor(
                            new Color(
                                tierColor.r,
                                tierColor.g,
                                tierColor.b, 0.15f));
                    SetBorderColor(badgeBg, tierColor);
                }

                var badgeText = avatarSection
                    .Q<Label>(
                        className: "level-badge-text");
                if (badgeText != null && level >= 26)
                {
                    badgeText.style.color =
                        new StyleColor(tierColor);
                }
            }

            // ── Identity card border — tier tint ──
            var identityCard =
                root.Q<VisualElement>(
                    className: "identity-card");
            if (identityCard != null)
            {
                SetBorderColor(identityCard,
                    new Color(
                        tierColor.r,
                        tierColor.g,
                        tierColor.b, 0.15f));
            }

            // ── XP ──
            int currentXP = pc.GetCurrentXP();
            int xpNeeded = pc.GetXPForNextLevel();
            float xpPercent = xpNeeded > 0
                ? (float)currentXP / xpNeeded * 100f
                : 0f;

            if (profileXpText != null)
                profileXpText.text =
                    $"{currentXP} / {xpNeeded}";

            if (profileXpFill != null)
            {
                profileXpFill.style.width =
                    new StyleLength(
                        new Length(
                            xpPercent,
                            LengthUnit.Percent));
                profileXpFill.style.backgroundColor =
                    new StyleColor(tierColor);
            }

            // ── XP bar border — tier color ──
            var xpBarBg =
                root.Q<VisualElement>(
                    className: "profile-xp-bar-bg");
            if (xpBarBg != null)
            {
                SetBorderColor(xpBarBg,
                    new Color(
                        tierColor.r,
                        tierColor.g,
                        tierColor.b, 0.15f));
            }

            if (profileNextLevel != null)
                profileNextLevel.text =
                    $"NEXT: LEVEL {level + 1}";
        }

        // ── Helper: set all 4 border colors at once ──
        private void SetBorderColor(
            VisualElement el, Color color)
        {
            if (el == null) return;
            var sc = new StyleColor(color);
            el.style.borderTopColor = sc;
            el.style.borderBottomColor = sc;
            el.style.borderLeftColor = sc;
            el.style.borderRightColor = sc;
        }

        // ── Tier helpers (level-based) ──
        private string GetTierName(int level)
        {
            if (level >= 51) return "BESTIE";
            if (level >= 26) return "MORE BETTER";
            if (level >= 11) return "BETTER";
            return "NOWBIE";
        }

        private Color GetTierColor(int level)
        {
            if (level >= 51)
                return new Color(0.13f, 0.83f, 0.93f);
            if (level >= 26)
                return new Color(0.96f, 0.62f, 0.04f);
            if (level >= 11)
                return new Color(0.71f, 0.78f, 0.86f);
            return new Color(0.71f, 0.51f, 0.31f);
        }

        private void RefreshCurrency()
        {
            var pc = PlayerCollection.Instance;
            if (pc == null) return;

            if (headerGold != null)
                headerGold.text =
                    pc.GetGold().ToString();
            if (headerCrystals != null)
                headerCrystals.text =
                    pc.GetCrystals().ToString();
        }

        private void RefreshStats()
        {
            var pc = PlayerCollection.Instance;
            if (pc == null) return;

            var allCards = pc.GetAllCards();
            var unlocked = pc.GetUnlockedCards();
            var decks = pc.GetPlayerDecks();
            var allSabotages = pc.GetAllSabotages();

            if (statCards != null)
                statCards.text =
                    $"{unlocked.Count} / " +
                    $"{allCards.Count}";

            if (statSabotages != null)
                statSabotages.text =
                    allSabotages.Count.ToString();

            if (statDecks != null)
                statDecks.text =
                    decks.Count.ToString();

            if (statWins != null)
                statWins.text =
                    pc.GetWins().ToString();

            if (statLosses != null)
                statLosses.text =
                    pc.GetLosses().ToString();
        }

        // ══════════════════════════════
        // TABS
        // ══════════════════════════════

        private void SwitchTab(string tab)
        {
            activeTab = tab;

            // Update tab buttons
            SetTabActive(tabCards,
                tab == "cards");
            SetTabActive(tabSabotages,
                tab == "sabotages");
            SetTabActive(tabSkins,
                tab == "skins");
            SetTabActive(tabAchievements,
                tab == "achievements");

            // Show/hide content
            SetVisible(contentCards,
                tab == "cards");
            SetVisible(contentSabotages,
                tab == "sabotages");
            SetVisible(contentSkins,
                tab == "skins");
            SetVisible(contentAchievements,
                tab == "achievements");

            // Show filter bar only for cards
            // and sabotages
            bool showFilter =
                tab == "cards" ||
                tab == "sabotages";
            SetVisible(filterBar, showFilter);

            RefreshCurrentTab();
        }

        private void RefreshCurrentTab()
        {
            switch (activeTab)
            {
                case "cards":
                    PopulateCards();
                    break;
                case "sabotages":
                    PopulateSabotages();
                    break;
                case "achievements":
                    PopulateAchievements();
                    break;
            }
        }

        private void SetTabActive(
            Button btn, bool active)
        {
            if (btn == null) return;
            if (active)
                btn.AddToClassList("tab-active");
            else
                btn.RemoveFromClassList("tab-active");
        }

        // ══════════════════════════════
        // FILTERS
        // ══════════════════════════════

        private void SetFilter(CardRarity? rarity)
        {
            activeFilter = rarity;

            SetFilterActive(filterAll,
                rarity == null);
            SetFilterActive(filterCommon,
                rarity == CardRarity.Common);
            SetFilterActive(filterRare,
                rarity == CardRarity.Rare);
            SetFilterActive(filterLegendary,
                rarity == CardRarity.Legendary);

            RefreshCurrentTab();
        }

        private void SetFilterActive(
            Button btn, bool active)
        {
            if (btn == null) return;
            if (active)
                btn.AddToClassList("filter-active");
            else
                btn.RemoveFromClassList(
                    "filter-active");
        }

        // ══════════════════════════════
        // CARDS GRID
        // ══════════════════════════════

        private void PopulateCards()
        {
            if (cardsGrid == null) return;
            cardsGrid.Clear();

            var pc = PlayerCollection.Instance;
            if (pc == null) return;

            var allCards = pc.GetAllCards();
            var unlocked = pc.GetUnlockedCards();

            // Apply filter
            IEnumerable<CardData> filtered =
                allCards;
            if (activeFilter.HasValue)
            {
                filtered = allCards.Where(
                    c => c.rarity == activeFilter);
            }

            var cardList = filtered
                .OrderBy(c => c.rarity)
                .ThenBy(c => c.cardName)
                .ToList();

            // Update count
            int ownedInFilter = cardList.Count(
                c => unlocked.Contains(c));
            if (collectionCount != null)
                collectionCount.text =
                    $"{ownedInFilter} / " +
                    $"{cardList.Count} COLLECTED";

            foreach (var card in cardList)
            {
                bool owned = unlocked.Contains(card);
                var cardEl = CreateCardElement(
                    card, owned);
                cardsGrid.Add(cardEl);
            }
        }

        private VisualElement CreateCardElement(
            CardData card, bool owned)
        {
            var wrapper = new VisualElement();
            wrapper.AddToClassList("collection-card");
            if (!owned)
                wrapper.AddToClassList(
                    "collection-card-locked");

            // Inner border
            var inner = new VisualElement();
            inner.AddToClassList(
                "collection-card-inner");
            wrapper.Add(inner);

            // Rarity strip
            var strip = new VisualElement();
            strip.AddToClassList("card-rarity-strip");
            string rarityClass = card.rarity switch
            {
                CardRarity.Common => "rarity-common",
                CardRarity.Rare => "rarity-rare",
                CardRarity.Legendary =>
                    "rarity-legendary",
                _ => "rarity-common"
            };
            strip.AddToClassList(rarityClass);
            wrapper.Add(strip);

            // Icon
            var iconSection = new VisualElement();
            iconSection.AddToClassList(
                "collection-card-icon-section");
            var icon = new VisualElement();
            icon.AddToClassList(
                "collection-card-icon");
            if (card.cardIcon != null)
                icon.style.backgroundImage =
                    new StyleBackground(
                        card.cardIcon);
            iconSection.Add(icon);
            wrapper.Add(iconSection);

            // Info
            var info = new VisualElement();
            info.AddToClassList(
                "collection-card-info");

            var nameLabel = new Label(
                card.cardName);
            nameLabel.AddToClassList(
                "collection-card-name");
            info.Add(nameLabel);

            var typeLabel = new Label(
                card.cardType.ToString()
                    .ToUpper());
            typeLabel.AddToClassList(
                "collection-card-type");
            info.Add(typeLabel);

            if (card.associatedElement !=
                Elements.ElementType.None)
            {
                var elemLabel = new Label(
                    card.associatedElement
                        .ToString().ToUpper());
                elemLabel.AddToClassList(
                    "collection-card-element");
                info.Add(elemLabel);
            }

            wrapper.Add(info);

            // Lock overlay
            if (!owned)
            {
                var lockOverlay = new VisualElement();
                lockOverlay.AddToClassList(
                    "card-lock-overlay");
                var lockIcon = new Label("🔒");
                lockIcon.AddToClassList(
                    "card-lock-icon");
                lockOverlay.Add(lockIcon);
                wrapper.Add(lockOverlay);
            }
            else
            {
                // Owned dot
                var dot = new VisualElement();
                dot.AddToClassList("card-owned-dot");
                wrapper.Add(dot);
            }

            // Hover accent
            var hoverLine = new VisualElement();
            hoverLine.AddToClassList(
                "collection-card-hover-line");
            wrapper.Add(hoverLine);

            // Click to detail
            wrapper.RegisterCallback<ClickEvent>(
                e => ShowCardDetail(card, owned));

            return wrapper;
        }

        // ══════════════════════════════
        // SABOTAGES GRID
        // ══════════════════════════════

        private void PopulateSabotages()
        {
            if (sabotagesGrid == null) return;
            sabotagesGrid.Clear();

            var allSabotages =
                PlayerCollection.Instance?.GetAllSabotages()
                ?? new List<SabotageCardData>();

            IEnumerable<SabotageCardData> filtered =
                allSabotages;
            if (activeFilter.HasValue)
            {
                filtered = allSabotages.Where(
                    s => s.rarity == activeFilter);
            }

            var sabList = filtered
                .OrderBy(s => s.rarity)
                .ThenBy(s => s.sabotageName)
                .ToList();

            if (collectionCount != null)
                collectionCount.text =
                    $"{sabList.Count} SABOTAGES";

            foreach (var sab in sabList)
            {
                var el = CreateSabotageElement(sab);
                sabotagesGrid.Add(el);
            }
        }

        private VisualElement CreateSabotageElement(
            SabotageCardData sab)
        {
            var wrapper = new VisualElement();
            wrapper.AddToClassList("sabotage-card");

            var inner = new VisualElement();
            inner.AddToClassList(
                "sabotage-card-inner");
            wrapper.Add(inner);

            // Rarity strip
            var strip = new VisualElement();
            strip.AddToClassList(
                "sabotage-rarity-strip");
            Color rarityCol = sab.GetRarityColor();
            strip.style.backgroundColor =
                new StyleColor(
                    new Color(
                        rarityCol.r, rarityCol.g,
                        rarityCol.b, 0.5f));
            wrapper.Add(strip);

            // Icon
            var iconSection = new VisualElement();
            iconSection.AddToClassList(
                "sabotage-icon-section");
            var icon = new VisualElement();
            icon.AddToClassList("sabotage-icon");
            if (sab.sabotageIcon != null)
                icon.style.backgroundImage =
                    new StyleBackground(
                        sab.sabotageIcon);
            iconSection.Add(icon);
            wrapper.Add(iconSection);

            // Info
            var info = new VisualElement();
            info.AddToClassList("sabotage-info");

            var nameLabel = new Label(
                sab.sabotageName);
            nameLabel.AddToClassList(
                "sabotage-name");
            info.Add(nameLabel);

            var tagLabel = new Label(
                sab.sabotageTag.ToString()
                    .ToUpper());
            tagLabel.AddToClassList("sabotage-tag");
            info.Add(tagLabel);

            var durLabel = new Label(
                sab.GetDurationText());
            durLabel.AddToClassList(
                "sabotage-duration");
            info.Add(durLabel);

            wrapper.Add(info);

            // Hover line
            var hoverLine = new VisualElement();
            hoverLine.AddToClassList(
                "sabotage-hover-line");
            wrapper.Add(hoverLine);

            // Click to show description
            wrapper.RegisterCallback<ClickEvent>(
                e => ShowSabotageDetail(sab));

            return wrapper;
        }

        // ══════════════════════════════
        // ACHIEVEMENTS (placeholder)
        // ══════════════════════════════

        private void PopulateAchievements()
        {
            if (achievementsGrid == null) return;
            achievementsGrid.Clear();

            // Placeholder achievements
            var achievements =
                new (string icon, string name,
                    string desc, bool unlocked)[]
            {
                ("⚔", "FIRST BLOOD",
                    "Win your first match", false),
                ("◆", "COLLECTOR",
                    "Unlock 10 cards", false),
                ("♛", "RANKED WARRIOR",
                    "Reach Silver rank", false),
                ("✦", "DECK MASTER",
                    "Create 3 custom decks", false),
                ("☉", "FORTUNE SEEKER",
                    "Open 10 lootboxes", false),
                ("⚖", "BIG SPENDER",
                    "Spend 10000 gold total", false),
                ("◇", "LEGENDARY FIND",
                    "Unlock a legendary card", false),
                ("♚", "DIAMOND LEAGUE",
                    "Reach Diamond rank", false),
            };

            foreach (var ach in achievements)
            {
                var card = new VisualElement();
                card.AddToClassList(
                    "achievement-card");
                if (!ach.unlocked)
                    card.AddToClassList(
                        "achievement-locked");

                var iconWrap = new VisualElement();
                iconWrap.AddToClassList(
                    "achievement-icon");
                var iconText = new Label(ach.icon);
                iconText.AddToClassList(
                    "achievement-icon-text");
                iconWrap.Add(iconText);
                card.Add(iconWrap);

                var info = new VisualElement();
                info.AddToClassList(
                    "achievement-info");
                var nameL = new Label(ach.name);
                nameL.AddToClassList(
                    "achievement-name");
                info.Add(nameL);
                var descL = new Label(ach.desc);
                descL.AddToClassList(
                    "achievement-desc");
                info.Add(descL);
                card.Add(info);

                achievementsGrid.Add(card);
            }
        }

        // ══════════════════════════════
        // DETAIL POPUP
        // ══════════════════════════════

        private void ShowCardDetail(
            CardData card, bool owned)
        {
            if (detailPopup == null) return;

            Color rarityCol = card.GetRarityColor();

            if (detailRarityLine != null)
                detailRarityLine
                    .style.backgroundColor =
                    new StyleColor(rarityCol);

            if (detailRarity != null)
            {
                detailRarity.text =
                    card.GetRarityName();
                detailRarity.style.color =
                    new StyleColor(rarityCol);
            }

            if (detailName != null)
                detailName.text =
                    card.cardName.ToUpper();

            if (detailType != null)
                detailType.text =
                    card.cardType.ToString()
                        .ToUpper();

            if (detailElement != null)
            {
                if (card.associatedElement !=
                    Elements.ElementType.None)
                {
                    detailElement.text =
                        card.associatedElement
                            .ToString().ToUpper();
                    detailElement.style.display =
                        DisplayStyle.Flex;
                }
                else
                {
                    detailElement.style.display =
                        DisplayStyle.None;
                }
            }

            if (detailDescription != null)
                detailDescription.text =
                    card.description;

            if (detailIcon != null &&
                card.cardIcon != null)
                detailIcon.style.backgroundImage =
                    new StyleBackground(
                        card.cardIcon);

            if (detailMaxCopies != null)
                detailMaxCopies.text =
                    card.GetMaxCopies().ToString();

            if (detailActivation != null)
                detailActivation.text =
                    card.activationType.ToString()
                        .ToUpper();

            // Ownership
            if (detailOwnedText != null)
                detailOwnedText.text = owned
                    ? "✦ OWNED"
                    : "✕ NOT OWNED";

            if (detailBox != null)
            {
                if (owned)
                    detailBox.RemoveFromClassList(
                        "detail-not-owned");
                else
                    detailBox.AddToClassList(
                        "detail-not-owned");
            }

            detailPopup.RemoveFromClassList(
                "hidden");
        }

        private void ShowSabotageDetail(
            SabotageCardData sab)
        {
            if (detailPopup == null) return;

            Color rarityCol = sab.GetRarityColor();

            if (detailRarityLine != null)
                detailRarityLine
                    .style.backgroundColor =
                    new StyleColor(rarityCol);

            if (detailRarity != null)
            {
                detailRarity.text =
                    sab.rarity.ToString().ToUpper();
                detailRarity.style.color =
                    new StyleColor(rarityCol);
            }

            if (detailName != null)
                detailName.text =
                    sab.sabotageName.ToUpper();

            if (detailType != null)
                detailType.text = "SABOTAGE";

            if (detailElement != null)
            {
                detailElement.text =
                    sab.sabotageTag.ToString()
                        .ToUpper();
                detailElement.style.display =
                    DisplayStyle.Flex;
            }

            if (detailDescription != null)
                detailDescription.text =
                    sab.description;

            if (detailIcon != null &&
                sab.sabotageIcon != null)
                detailIcon.style.backgroundImage =
                    new StyleBackground(
                        sab.sabotageIcon);

            if (detailMaxCopies != null)
                detailMaxCopies.text = "—";

            if (detailActivation != null)
                detailActivation.text =
                    sab.GetDurationText().ToUpper();

            if (detailOwnedText != null)
                detailOwnedText.text =
                    "GLOBAL POOL";

            if (detailBox != null)
                detailBox.RemoveFromClassList(
                    "detail-not-owned");

            detailPopup.RemoveFromClassList(
                "hidden");
        }

        private void CloseDetailPopup()
        {
            detailPopup?.AddToClassList("hidden");
        }

        // ══════════════════════════════
        // UTILITIES
        // ══════════════════════════════

        private void SetVisible(
            VisualElement el, bool visible)
        {
            if (el == null) return;
            if (visible)
                el.RemoveFromClassList("hidden");
            else
                el.AddToClassList("hidden");
        }
    }
}