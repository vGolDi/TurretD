using UnityEngine;
using UnityEngine.UIElements;
using ElementumDefense.Cards;

[RequireComponent(typeof(UIDocument))]
public class ArtDecoTarotOverlay : MonoBehaviour
{
    [Header("Reference to existing menu controller")]
    [SerializeField] private MainMenuController mainMenuController;

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    private AudioSource audioSource;

    [Header("Version")]
    [SerializeField] private string gameVersion = "0.1.0 Alpha";

    private PlayerCollection playerData;
    private VisualElement root;

    // Header - Player Info
    private Label playerNameLabel;
    private Label levelLabel;
    private Label playerTierLabel;
    private Label eloRankLabel;
    private Label eloValueLabel;
    private Label goldLabel;
    private Label crystalLabel;
    private Label versionLabel;

    // Header - Hexagon
    private VisualElement hexagonBg;
    private VisualElement hexagonBorder;

    // Header - XP Bar
    private VisualElement xpBarFill;
    private Label xpLabel;

    // Cards
    private VisualElement cardMultiplayer;
    private VisualElement cardDeckbuilder;
    private VisualElement cardShop;
    private VisualElement cardLootbox;
    private VisualElement cardSettings;

    // Bottom menu
    private Button btnCredits;
    private Button btnProfile;
    private Button btnQuit;

    // ==========================================
    // LIFECYCLE
    // ==========================================

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        root = uiDocument.rootVisualElement;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource =
                gameObject.AddComponent<AudioSource>();

        if (mainMenuController == null)
            mainMenuController =
                FindFirstObjectByType<MainMenuController>();

        playerData = PlayerCollection.Instance;

        QueryElements();
        BindCards();
        BindBottomMenu();
        UpdateAllDisplayData();
        SubscribeToPlayerEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayerEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromPlayerEvents();
    }

    private void Update()
    {
        if (playerData == null &&
            PlayerCollection.Instance != null)
        {
            playerData = PlayerCollection.Instance;
            SubscribeToPlayerEvents();
            UpdateAllDisplayData();
            Debug.Log(
                "[ArtDeco] Late-bound to PlayerCollection");
        }
    }

    // ==========================================
    // EVENT SUBSCRIPTION
    // ==========================================

    private void SubscribeToPlayerEvents()
    {
        if (playerData == null) return;

        playerData.OnGoldChanged +=
            HandleGoldChanged;
        playerData.OnCrystalsChanged +=
            HandleCrystalsChanged;
        playerData.OnLevelChanged +=
            HandleLevelChanged;
        playerData.OnXPChanged +=
            HandleXPChanged;
        playerData.OnEloChanged +=
            HandleEloChanged;
        playerData.OnCollectionLoaded +=
            HandleCollectionReloaded;
    }

    private void UnsubscribeFromPlayerEvents()
    {
        if (playerData == null) return;

        playerData.OnGoldChanged -=
            HandleGoldChanged;
        playerData.OnCrystalsChanged -=
            HandleCrystalsChanged;
        playerData.OnLevelChanged -=
            HandleLevelChanged;
        playerData.OnXPChanged -=
            HandleXPChanged;
        playerData.OnEloChanged -=
            HandleEloChanged;
        playerData.OnCollectionLoaded -=
            HandleCollectionReloaded;
    }

    // ==========================================
    // EVENT HANDLERS
    // ==========================================

    private void HandleGoldChanged(int newGold)
    {
        if (goldLabel != null)
            goldLabel.text = FormatNumber(newGold);
    }

    private void HandleCrystalsChanged(int newCrystals)
    {
        if (crystalLabel != null)
            crystalLabel.text =
                FormatNumber(newCrystals);
    }

    private void HandleLevelChanged(int newLevel)
    {
        if (levelLabel != null)
            levelLabel.text = newLevel.ToString();

        UpdateTierDisplay(newLevel);
        UpdateEloDisplay();
    }

    private void HandleXPChanged(
        int currentXP, int xpNeeded)
    {
        if (xpBarFill != null)
        {
            float progress =
                (float)currentXP / xpNeeded;
            xpBarFill.style.width =
                new StyleLength(
                    Length.Percent(progress * 100f));
        }

        if (xpLabel != null)
            xpLabel.text =
                $"{currentXP}/{xpNeeded} XP";
    }

    private void HandleEloChanged(int newElo)
    {
        UpdateEloDisplay();
    }

    private void HandleCollectionReloaded()
    {
        Debug.Log(
            "[ArtDeco] Collection reloaded, " +
            "refreshing UI");
        UpdateAllDisplayData();
    }

    // ==========================================
    // QUERY ELEMENTS
    // ==========================================

    private void QueryElements()
    {
        // Player info
        playerNameLabel =
            root.Q<Label>("player-name");
        levelLabel =
            root.Q<Label>("level-number");
        playerTierLabel =
            root.Q<Label>("player-rank");
        eloRankLabel =
            root.Q<Label>("player-elo-rank");
        eloValueLabel =
            root.Q<Label>("player-elo-value");
        goldLabel =
            root.Q<Label>("gold-value");
        crystalLabel =
            root.Q<Label>("crystal-value");
        versionLabel =
            root.Q<Label>("version-text");

        // Hexagon
        hexagonBg =
            root.Q<VisualElement>("hexagon-bg");
        hexagonBorder =
            root.Q<VisualElement>("hexagon-border");

        // XP Bar
        xpBarFill =
            root.Q<VisualElement>("xp-bar-fill");
        xpLabel =
            root.Q<Label>("xp-text");

        // Cards
        cardMultiplayer =
            root.Q<VisualElement>("card-multiplayer");
        cardDeckbuilder =
            root.Q<VisualElement>("card-deckbuilder");
        cardShop =
            root.Q<VisualElement>("card-shop");
        cardLootbox =
            root.Q<VisualElement>("card-lootbox");
        cardSettings =
            root.Q<VisualElement>("card-settings");

        // Bottom menu
        btnCredits =
            root.Q<Button>("btn-credits");
        btnProfile =
            root.Q<Button>("btn-profile");
        btnQuit =
            root.Q<Button>("btn-quit");
    }

    // ==========================================
    // UPDATE ALL DATA
    // ==========================================

    private void UpdateAllDisplayData()
    {
        if (playerData == null)
            playerData = PlayerCollection.Instance;

        if (playerData != null)
        {
            int level = playerData.GetLevel();

            // Player name
            string displayName =
                GetPlayerDisplayName();
            if (playerNameLabel != null)
                playerNameLabel.text = displayName;

            // Level number
            if (levelLabel != null)
                levelLabel.text = level.ToString();

            // Currency
            if (goldLabel != null)
                goldLabel.text =
                    FormatNumber(playerData.GetGold());

            if (crystalLabel != null)
                crystalLabel.text =
                    FormatNumber(
                        playerData.GetCrystals());

            // Tier (based on level)
            UpdateTierDisplay(level);

            // ELO rank
            UpdateEloDisplay();

            // XP bar
            int currentXP = playerData.GetCurrentXP();
            int xpNeeded =
                playerData.GetXPForNextLevel();
            HandleXPChanged(currentXP, xpNeeded);

            // Subscribe if not yet
            SubscribeToPlayerEvents();

            Debug.Log(
                $"[ArtDeco] UI updated: " +
                $"Lvl {level} ({GetTierName(level)}), " +
                $"Gold {playerData.GetGold()}, " +
                $"Crystals {playerData.GetCrystals()}, " +
                $"ELO {playerData.GetElo()} " +
                $"({playerData.GetRankName()})");
        }
        else
        {
            Debug.LogWarning(
                "[ArtDeco] PlayerCollection not found!");
        }

        if (versionLabel != null)
            versionLabel.text = $"v{gameVersion}";
    }

    // ==========================================
    // TIER DISPLAY (based on LEVEL)
    // ==========================================

    private void UpdateTierDisplay(int level)
    {
        Color tierColor = GetTierColor(level);
        string tierName = GetTierName(level);

        // Tier label
        if (playerTierLabel != null)
        {
            playerTierLabel.text = tierName;
            playerTierLabel.style.color =
                new StyleColor(tierColor);
        }

        // Hexagon background
        if (hexagonBg != null)
        {
            Color bgColor = tierColor;
            bgColor.a = 0.12f;
            hexagonBg.style.backgroundColor =
                new StyleColor(bgColor);
        }

        // Hexagon border
        if (hexagonBorder != null)
        {
            hexagonBorder.style.borderTopColor =
                new StyleColor(tierColor);
            hexagonBorder.style.borderBottomColor =
                new StyleColor(tierColor);
            hexagonBorder.style.borderLeftColor =
                new StyleColor(tierColor);
            hexagonBorder.style.borderRightColor =
                new StyleColor(tierColor);
        }

        // Level number color
        if (levelLabel != null)
        {
            if (level >= 26)
                levelLabel.style.color =
                    new StyleColor(tierColor);
            else
                levelLabel.style.color =
                    new StyleColor(
                        new Color(
                            0.996f, 0.953f, 0.78f));
        }

        // Name divider color
        var divider = root.Q<VisualElement>(
            "player-name-divider");
        if (divider != null)
        {
            Color divColor = tierColor;
            divColor.a = 0.3f;
            divider.style.backgroundColor =
                new StyleColor(divColor);
        }
    }

    // ==========================================
    // ELO DISPLAY (based on ELO rating)
    // ==========================================

    private void UpdateEloDisplay()
    {
        if (playerData == null) return;

        string rankName = playerData.GetRankName();
        Color rankColor = playerData.GetRankColor();
        int elo = playerData.GetElo();

        // ELO rank name
        if (eloRankLabel != null)
        {
            eloRankLabel.text = rankName;
            eloRankLabel.style.color =
                new StyleColor(rankColor);
        }

        // ELO value
        if (eloValueLabel != null)
        {
            eloValueLabel.text = $"{elo} ELO";
            Color eloColor = rankColor;
            eloColor.a = 0.5f;
            eloValueLabel.style.color =
                new StyleColor(eloColor);
        }
    }

    // ==========================================
    // TIER HELPERS
    // ==========================================

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

    // ==========================================
    // COMBINED RANK UPDATE (backward compat)
    // ==========================================

    private void UpdateRankDisplay()
    {
        if (playerData == null) return;

        int level = playerData.GetLevel();
        UpdateTierDisplay(level);
        UpdateEloDisplay();
    }

    // ==========================================
    // CARD BINDING
    // ==========================================

    private void BindCards()
    {
        BindCard(cardMultiplayer, () =>
        {
            Debug.Log("[ArtDeco] Card: MULTIPLAYER");
            mainMenuController?.OpenMultiplayer();
        });

        BindCard(cardDeckbuilder, () =>
        {
            Debug.Log("[ArtDeco] Card: DECKBUILDER");
            mainMenuController?.OpenDeckbuilder();
        });

        BindCard(cardShop, () =>
        {
            Debug.Log("[ArtDeco] Card: SHOP");
            mainMenuController?.OpenShop();
        });

        BindCard(cardLootbox, () =>
        {
            Debug.Log("[ArtDeco] Card: LOOTBOX");
            mainMenuController?.OpenLootboxMenu();
        });

        BindCard(cardSettings, () =>
        {
            Debug.Log("[ArtDeco] Card: SETTINGS");
            mainMenuController?.OpenSettings();
        });
    }

    private void BindBottomMenu()
    {
        if (btnCredits != null)
        {
            btnCredits.clicked += () =>
            {
                PlayClickSound();
                mainMenuController?.OpenCredits();
            };
        }

        if (btnQuit != null)
        {
            btnQuit.clicked += () =>
            {
                PlayClickSound();
                mainMenuController?.QuitGame();
            };
        }

        if (btnProfile != null)
        {
            btnProfile.clicked += () =>
            {
                PlayClickSound();
                Debug.Log("[ArtDeco] Profile clicked");
            };
        }
    }

    private void BindCard(
        VisualElement card, System.Action onClick)
    {
        if (card == null) return;

        card.RegisterCallback<ClickEvent>(evt =>
        {
            PlayClickSound();
            HighlightCard(card);
            onClick?.Invoke();
            evt.StopPropagation();
        });
    }

    // ==========================================
    // CARD HIGHLIGHTS
    // ==========================================

    private void ClearAllCardHighlights()
    {
        cardMultiplayer?.RemoveFromClassList(
            "card-selected");
        cardDeckbuilder?.RemoveFromClassList(
            "card-selected");
        cardShop?.RemoveFromClassList(
            "card-selected");
        cardLootbox?.RemoveFromClassList(
            "card-selected");
        cardSettings?.RemoveFromClassList(
            "card-selected");
    }

    private void HighlightCard(
        VisualElement selectedCard)
    {
        ClearAllCardHighlights();
        selectedCard?.AddToClassList("card-selected");
    }

    // ==========================================
    // VISIBILITY
    // ==========================================

    public void Show()
    {
        root.style.display = DisplayStyle.Flex;
        ClearAllCardHighlights();
        UpdateAllDisplayData();
    }

    public void Hide()
    {
        root.style.display = DisplayStyle.None;
    }

    public void ForceRefresh()
    {
        UpdateAllDisplayData();
    }

    // ==========================================
    // HELPERS
    // ==========================================

    private string GetPlayerDisplayName()
    {
        if (ElementumDefense.Auth.AuthManager
                .Instance != null &&
            ElementumDefense.Auth.AuthManager
                .Instance.IsLoggedIn)
        {
            return ElementumDefense.Auth.AuthManager
                .Instance.CurrentUsername;
        }
        return "Traveler";
    }

    private string FormatNumber(int number)
    {
        return number.ToString("N0");
    }

    private void PlayClickSound()
    {
        if (buttonClickSound != null &&
            audioSource != null)
        {
            audioSource.PlayOneShot(
                buttonClickSound, 0.7f);
        }
    }
}