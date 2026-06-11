using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ElementumDefense.Cards;
using ElementumDefense.Skins;
using ElementumDefense.Turrets;

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
        private SkinCategory? activeSkinFilter = null; // null = show all
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // REFRESH
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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

            // â”€â”€ Tier color (based on level) â”€â”€
            Color tierColor = GetTierColor(level);

            // â”€â”€ Rank color (based on ELO) â”€â”€
            string rankName = pc.GetRankName();
            Color rankColor = pc.GetRankColor();
            int elo = pc.GetElo();

            // â”€â”€ Rank label â€” rank color (ELO-based) â”€â”€
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

            // â”€â”€ Rank emblem â€” rank color (ELO-based) â”€â”€
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

            // â”€â”€ Avatar frame â€” tier color (level-based) â”€â”€
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

            // â”€â”€ Level badge â€” tier color (level-based) â”€â”€
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

            // â”€â”€ Identity card border â€” tier tint â”€â”€
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

            // â”€â”€ XP â”€â”€
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

            // â”€â”€ XP bar border â€” tier color â”€â”€
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

        // â”€â”€ Helper: set all 4 border colors at once â”€â”€
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

        // â”€â”€ Tier helpers (level-based) â”€â”€
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // TABS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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
                case "skins":
                    PopulateSkins();
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // FILTERS
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // CARDS GRID
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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

            // Lock indicator (only when not owned) — sits at the BOTTOM of the
            // info column, after name/type/element, instead of as a full-card
            // absolute overlay. The card itself dims via the
            // .collection-card-locked class on the wrapper.
            if (!owned)
            {
                var lockIcon = new Label("🔒");
                lockIcon.AddToClassList("card-lock-inline");
                info.Add(lockIcon);
            }

            wrapper.Add(info);

            // Lock overlay
            if (!owned)
            {
                // Inline lock icon is rendered inside `info` above (after name/type/element).
                // No full-card overlay — keeps the card content visible.
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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // SABOTAGES GRID
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // ACHIEVEMENTS (placeholder)
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private void PopulateAchievements()
        {
            if (achievementsGrid == null) return;
            achievementsGrid.Clear();

            var achMgr = Achievements.AchievementManager.Instance;
            if (achMgr == null)
            {
                var msg = new Label("Achievement system loading...");
                msg.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
                msg.style.unityTextAlign = TextAnchor.MiddleCenter;
                msg.style.marginTop = 40;
                msg.style.fontSize = 16;
                achievementsGrid.Add(msg);
                return;
            }

            // Re-check all achievements against LIVE stats before displaying.
            // This catches achievements completed since last check (fixes async timing).
            achMgr.CheckAllAchievements();

            var achievements = achMgr.GetAllAchievements();
            if (achievements.Count == 0)
            {
                var msg = new Label("No achievements available yet.");
                msg.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
                msg.style.unityTextAlign = TextAnchor.MiddleCenter;
                msg.style.marginTop = 40;
                msg.style.fontSize = 16;
                achievementsGrid.Add(msg);
                return;
            }

            foreach (var ach in achievements)
            {
                bool completed = achMgr.IsCompleted(ach.achievementId);
                bool claimable = achMgr.IsClaimable(ach.achievementId);
                int progress = achMgr.GetLiveProgress(ach);
                int tier = achMgr.GetCurrentTier(ach.achievementId);
                int target = ach.GetTargetForTier(tier);

                var card = new VisualElement();
                card.AddToClassList("achievement-card");

                if (completed)
                {
                    // No locked class â€” fully done
                }
                else if (claimable)
                {
                    // Highlight claimable cards
                    card.style.borderTopColor = new StyleColor(ach.GetRarityColor());
                    card.style.borderTopWidth = 2;
                    card.style.borderRightColor = new StyleColor(ach.GetRarityColor());
                    card.style.borderRightWidth = 2;
                    card.style.borderBottomColor = new StyleColor(ach.GetRarityColor());
                    card.style.borderBottomWidth = 2;
                }
                else
                {
                    card.AddToClassList("achievement-locked");
                }

                // Rarity accent
                card.style.borderLeftColor = new StyleColor(ach.GetRarityColor());
                card.style.borderLeftWidth = 3;

                // Icon
                var iconWrap = new VisualElement();
                iconWrap.AddToClassList("achievement-icon");
                if (completed || claimable)
                {
                    iconWrap.style.backgroundColor = new StyleColor(
                        new Color(ach.GetRarityColor().r, ach.GetRarityColor().g,
                                  ach.GetRarityColor().b, claimable ? 0.25f : 0.15f));
                }

                var iconText = new Label(ach.iconEmoji);
                iconText.AddToClassList("achievement-icon-text");
                iconWrap.Add(iconText);
                card.Add(iconWrap);

                // Info
                var info = new VisualElement();
                info.AddToClassList("achievement-info");

                var nameL = new Label(ach.achievementName);
                nameL.AddToClassList("achievement-name");
                if (completed)
                    nameL.style.color = new StyleColor(ach.GetRarityColor());
                else if (claimable)
                    nameL.style.color = new StyleColor(new Color(1f, 0.95f, 0.7f));
                info.Add(nameL);

                var descL = new Label(ach.description);
                descL.AddToClassList("achievement-desc");
                info.Add(descL);

                // === STATE: CLAIMABLE â€” show CLAIM button ===
                if (claimable)
                {
                    // Show progress as complete
                    var readyLabel = new Label($"✦ {progress} / {target} — READY!");
                    readyLabel.style.fontSize = 9;
                    readyLabel.style.color = new StyleColor(new Color(1f, 0.85f, 0.3f));
                    readyLabel.style.marginTop = 2;
                    info.Add(readyLabel);

                    // Reward preview
                    string rewardStr = "";
                    if (ach.rewardGold > 0) rewardStr += $"🪙{ach.rewardGold} ";
                    if (ach.rewardCrystals > 0) rewardStr += $"💎{ach.rewardCrystals} ";
                    if (ach.rewardXP > 0) rewardStr += $"⭐{ach.rewardXP}";

                    if (!string.IsNullOrEmpty(rewardStr.Trim()))
                    {
                        var rewardLabel = new Label(rewardStr.Trim());
                        rewardLabel.style.fontSize = 9;
                        rewardLabel.style.color = new StyleColor(
                            new Color(1f, 0.85f, 0.3f, 0.8f));
                        rewardLabel.style.marginTop = 2;
                        info.Add(rewardLabel);
                    }

                    // CLAIM button
                    var claimBtn = new Button(() =>
                    {
                        bool claimed = achMgr.ClaimAchievement(ach.achievementId);
                        if (claimed)
                        {
                            Debug.Log($"[ProfileUI] Claimed achievement: {ach.achievementName}");
                            RefreshCurrency();
                            PopulateAchievements(); // Re-render
                        }
                    });
                    claimBtn.text = "ODBIERZ";
                    claimBtn.style.marginTop = 6;
                    claimBtn.style.height = 24;
                    claimBtn.style.fontSize = 11;
                    claimBtn.style.color = new StyleColor(Color.white);
                    claimBtn.style.backgroundColor = new StyleColor(
                        new Color(ach.GetRarityColor().r * 0.7f,
                                  ach.GetRarityColor().g * 0.7f,
                                  ach.GetRarityColor().b * 0.7f, 0.9f));
                    claimBtn.style.borderTopLeftRadius = 4;
                    claimBtn.style.borderTopRightRadius = 4;
                    claimBtn.style.borderBottomLeftRadius = 4;
                    claimBtn.style.borderBottomRightRadius = 4;
                    claimBtn.style.borderTopWidth = 0;
                    claimBtn.style.borderBottomWidth = 0;
                    claimBtn.style.borderLeftWidth = 0;
                    claimBtn.style.borderRightWidth = 0;
                    info.Add(claimBtn);
                }
                // === STATE: COMPLETED â€” already claimed ===
                else if (completed)
                {
                    string tierText = ach.hasTiers
                        ? $"✓ ODEBRANO (Tier {tier}/{ach.TierCount})"
                        : "✓ ODEBRANO";
                    var doneLabel = new Label(tierText);
                    doneLabel.style.fontSize = 9;
                    doneLabel.style.color = new StyleColor(
                        new Color(0.2f, 0.9f, 0.4f));
                    doneLabel.style.marginTop = 4;
                    info.Add(doneLabel);
                }
                // === STATE: IN PROGRESS ===
                else if (ach.trackType != Achievements.AchievementTrackType.Manual)
                {
                    var progressBar = new VisualElement();
                    progressBar.style.height = 4;
                    progressBar.style.marginTop = 4;
                    progressBar.style.backgroundColor = new StyleColor(
                        new Color(1f, 1f, 1f, 0.08f));
                    progressBar.style.borderBottomLeftRadius = 2;
                    progressBar.style.borderBottomRightRadius = 2;
                    progressBar.style.borderTopLeftRadius = 2;
                    progressBar.style.borderTopRightRadius = 2;

                    float pct = target > 0
                        ? Mathf.Clamp01((float)progress / target) * 100f
                        : 0f;

                    var fill = new VisualElement();
                    fill.style.height = new StyleLength(Length.Percent(100));
                    fill.style.width = new StyleLength(new Length(pct, LengthUnit.Percent));
                    fill.style.backgroundColor = new StyleColor(ach.GetRarityColor());
                    fill.style.borderBottomLeftRadius = 2;
                    fill.style.borderBottomRightRadius = 2;
                    fill.style.borderTopLeftRadius = 2;
                    fill.style.borderTopRightRadius = 2;
                    progressBar.Add(fill);

                    info.Add(progressBar);

                    var progressText = new Label($"{progress} / {target}");
                    progressText.style.fontSize = 9;
                    progressText.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.6f));
                    progressText.style.marginTop = 2;
                    info.Add(progressText);

                    // Rewards hint
                    if (ach.rewardGold > 0 || ach.rewardCrystals > 0 || ach.rewardXP > 0)
                    {
                        string rewardStr = "";
                        if (ach.rewardGold > 0) rewardStr += $"🪙{ach.rewardGold} ";
                        if (ach.rewardCrystals > 0) rewardStr += $"💎{ach.rewardCrystals} ";
                        if (ach.rewardXP > 0) rewardStr += $"⭐{ach.rewardXP}";

                        var rewardLabel = new Label(rewardStr.Trim());
                        rewardLabel.style.fontSize = 9;
                        rewardLabel.style.color = new StyleColor(
                            new Color(1f, 0.85f, 0.3f, 0.5f));
                        rewardLabel.style.marginTop = 2;
                        info.Add(rewardLabel);
                    }
                }

                card.Add(info);
                achievementsGrid.Add(card);
            }
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // DETAIL POPUP
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // SKINS TAB
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

        private void PopulateSkins()
        {
            if (contentSkins == null) return;
            contentSkins.Clear();

            var skinInv = SkinInventory.Instance;
            if (skinInv == null)
            {
                var msg = new Label("Skin system loading...");
                msg.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
                msg.style.unityTextAlign = TextAnchor.MiddleCenter;
                msg.style.marginTop = 40;
                msg.style.fontSize = 16;
                contentSkins.Add(msg);
                return;
            }

            // ===== FILTER BAR =====
            var filterBar = new VisualElement();
            filterBar.style.flexDirection = FlexDirection.Row;
            filterBar.style.flexWrap = Wrap.Wrap;
            filterBar.style.marginBottom = 12;
            filterBar.style.marginLeft = 8;
            filterBar.style.marginRight = 8;

            // "All" button
            AddFilterButton(filterBar, "ALL", null);
            AddFilterButton(filterBar, "\uD83D\uDC64 Character", SkinCategory.Character);
            AddFilterButton(filterBar, "\uD83D\uDD2B Turret", SkinCategory.Turret);
            AddFilterButton(filterBar, "\uD83C\uDFE0 Base", SkinCategory.Base);
            AddFilterButton(filterBar, "\uD83D\uDDFA Map", SkinCategory.Map);
            AddFilterButton(filterBar, "\u26CF GoldMine", SkinCategory.GoldMine);
            AddFilterButton(filterBar, "\uD83C\uDF81 Bundle", SkinCategory.Bundle);

            contentSkins.Add(filterBar);

            // ===== FILTERED SKINS =====
            var allSkins = skinInv.GetAllSkins();

            // Apply category filter
            var filtered = activeSkinFilter.HasValue
                ? allSkins.Where(s => s.category == activeSkinFilter.Value).ToList()
                : allSkins;

            if (filtered.Count() == 0)
            {
                var msg = new Label("No skins in this category.");
                msg.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
                msg.style.unityTextAlign = TextAnchor.MiddleCenter;
                msg.style.marginTop = 40;
                msg.style.fontSize = 16;
                contentSkins.Add(msg);
                return;
            }

            // Group by target within filtered results
            var groups = filtered
                .GroupBy(s => string.IsNullOrEmpty(s.targetDisplayName) ? s.targetId : s.targetDisplayName)
                .OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                var groupHeaderRow = new VisualElement();
                groupHeaderRow.style.flexDirection = FlexDirection.Row;
                groupHeaderRow.style.alignItems = Align.Center;
                groupHeaderRow.style.justifyContent = Justify.SpaceBetween;
                groupHeaderRow.style.marginTop = 16;
                groupHeaderRow.style.marginBottom = 8;
                groupHeaderRow.style.marginRight = 16;

                // Section header
                var header = new Label(group.Key?.ToUpper() ?? "OTHER");
                header.AddToClassList("collection-card-type");
                header.style.fontSize = 14;
                header.style.marginLeft = 8;
                header.style.color = new StyleColor(new Color(0.7f, 0.8f, 0.9f));
                header.style.letterSpacing = 2;
                groupHeaderRow.Add(header);

                // Default button (styled like btn-back)
                var btnDefault = new Button(() => {
                    string targetToUnequip = group.First().targetId;
                    SkinInventory.Instance?.UnequipSkin(targetToUnequip);
                    PopulateSkins();
                });
                btnDefault.text = "EQUIP DEFAULT";
                
                // Copy classes from btn-back
                var btnBack = root?.Q<Button>("btn-back");
                if (btnBack != null) {
                    foreach (var cls in btnBack.GetClasses()) {
                        btnDefault.AddToClassList(cls);
                    }
                } else {
                    btnDefault.style.fontSize = 10;
                    btnDefault.style.paddingLeft = 8;
                    btnDefault.style.paddingRight = 8;
                }
                btnDefault.style.marginLeft = 16;
                btnDefault.style.alignSelf = Align.Center;
                
                groupHeaderRow.Add(btnDefault);

                contentSkins.Add(groupHeaderRow);

                // Skin grid for this group
                var grid = new VisualElement();
                grid.style.flexDirection = FlexDirection.Row;
                grid.style.flexWrap = Wrap.Wrap;
                grid.style.paddingLeft = 4;
                grid.style.paddingRight = 4;

                foreach (var skin in group.OrderBy(s => s.rarity))
                {
                    bool owned = skinInv.OwnsSkin(skin);
                    bool equipped = skinInv.IsSkinEquipped(skin.skinId);
                    var el = CreateSkinElement(skin, owned, equipped);
                    grid.Add(el);
                }

                contentSkins.Add(grid);
            }
        }

        private void AddFilterButton(VisualElement parent, string label, SkinCategory? category)
        {
            var btn = new Button(() =>
            {
                activeSkinFilter = category;
                PopulateSkins();
            });
            btn.text = label;
            btn.style.height = 28;
            btn.style.marginRight = 4;
            btn.style.marginBottom = 4;
            btn.style.paddingLeft = 10;
            btn.style.paddingRight = 10;
            btn.style.fontSize = 11;
            btn.style.borderTopLeftRadius = 6;
            btn.style.borderTopRightRadius = 6;
            btn.style.borderBottomLeftRadius = 6;
            btn.style.borderBottomRightRadius = 6;

            bool isActive = activeSkinFilter == category;
            
            // Apply btn-back classes for consistent styling
            var btnBack = root?.Q<Button>("btn-back");
            if (btnBack != null) {
                foreach (var cls in btnBack.GetClasses()) {
                    btn.AddToClassList(cls);
                }
            }
            
            // Adjust specific colors for active/inactive state
            btn.style.backgroundColor = new StyleColor(
                isActive ? new Color(0.3f, 0.5f, 0.8f) : new Color(0.15f, 0.17f, 0.22f));
            btn.style.color = new StyleColor(
                isActive ? Color.white : new Color(0.6f, 0.7f, 0.8f));

            if (isActive)
            {
                btn.style.borderBottomColor = new StyleColor(new Color(0.4f, 0.7f, 1f));
                btn.style.borderBottomWidth = 2;
            }

            parent.Add(btn);
        }


        private VisualElement CreateSkinElement(SkinData skin, bool owned, bool equipped)
        {
            var wrapper = new VisualElement();
            wrapper.AddToClassList("collection-card");
            wrapper.style.width = 140;
            wrapper.style.minHeight = 180;
            wrapper.style.height = StyleKeyword.Auto;
            wrapper.style.marginRight = 8;
            wrapper.style.marginBottom = 8;

            if (!owned)
                wrapper.AddToClassList("collection-card-locked");

            // Inner
            var inner = new VisualElement();
            inner.AddToClassList("collection-card-inner");
            wrapper.Add(inner);

            // Rarity strip
            var strip = new VisualElement();
            strip.AddToClassList("card-rarity-strip");
            strip.style.backgroundColor = new StyleColor(skin.GetRarityColor());
            wrapper.Add(strip);

            // Icon
            var iconSection = new VisualElement();
            iconSection.AddToClassList("collection-card-icon-section");
            var icon = new VisualElement();
            icon.AddToClassList("collection-card-icon");
            if (skin.previewIcon != null)
                icon.style.backgroundImage = new StyleBackground(skin.previewIcon);
            else
            {
                // Placeholder with tint color
                icon.style.backgroundColor = new StyleColor(
                    new Color(skin.skinTint.r, skin.skinTint.g, skin.skinTint.b, 0.3f));
            }
            iconSection.Add(icon);
            wrapper.Add(iconSection);

            // Info section
            var info = new VisualElement();
            info.AddToClassList("collection-card-info");

            var nameLabel = new Label(skin.skinName);
            nameLabel.AddToClassList("collection-card-name");
            nameLabel.style.fontSize = 11;
            info.Add(nameLabel);

            var rarityLabel = new Label(skin.rarity.ToString().ToUpper());
            rarityLabel.AddToClassList("collection-card-type");
            rarityLabel.style.color = new StyleColor(skin.GetRarityColor());
            rarityLabel.style.fontSize = 9;
            info.Add(rarityLabel);

            if (!skin.IsUniversal)
            {
                var compatLabel = new Label($"[{string.Join(", ", skin.compatibleArenaTypes)} ONLY]");
                compatLabel.style.color = new StyleColor(new Color(1f, 0.6f, 0f));
                compatLabel.style.fontSize = 9;
                info.Add(compatLabel);
            }

            // Status / action
            if (owned)
            {
                if (equipped)
                {
                    var badge = new Label("\u2713 EQUIPPED");
                    badge.style.color = new StyleColor(new Color(0.2f, 0.9f, 0.4f));
                    badge.style.fontSize = 10;
                    badge.style.unityTextAlign = TextAnchor.MiddleCenter;
                    badge.style.marginTop = 4;
                    info.Add(badge);

                    // Click to unequip
                    wrapper.RegisterCallback<ClickEvent>(e =>
                    {
                        SkinInventory.Instance?.UnequipSkin(skin.targetId);
                        PopulateSkins();
                    });
                }
                else
                {
                    var badge = new Label("OWNED");
                    badge.style.color = new StyleColor(new Color(0.6f, 0.7f, 0.8f));
                    badge.style.fontSize = 10;
                    badge.style.unityTextAlign = TextAnchor.MiddleCenter;
                    badge.style.marginTop = 4;
                    info.Add(badge);

                    // Click to equip
                    wrapper.RegisterCallback<ClickEvent>(e =>
                    {
                        SkinInventory.Instance?.EquipSkin(skin);
                        PopulateSkins();
                    });
                }
            }
            else
            {
                // Not owned - direct to shop
                var shopLabel = new Label("BUY IN SHOP");
                shopLabel.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.5f));
                shopLabel.style.fontSize = 10;
                shopLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                shopLabel.style.marginTop = 4;
                info.Add(shopLabel);
            }

            wrapper.Add(info);
            return wrapper;
        }

        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•
        // UTILITIES
        // â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•

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
