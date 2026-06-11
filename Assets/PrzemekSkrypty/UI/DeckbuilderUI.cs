using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ElementumDefense.Cards;
using ElementumDefense.UI;

[RequireComponent(typeof(UIDocument))]
public class DeckbuilderUI : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private MainMenuController mainMenuController;

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    private AudioSource audioSource;

    private VisualElement root;

    // UI Elements
    private TextField deckNameInput;
    private Label deckSizeLabel;
    private Label legendaryCountLabel;
    private Label rareCountLabel;
    private Label commonCountLabel;
    private Label validationLabel;
    private VisualElement collectionGrid;
    private VisualElement deckList;
    private VisualElement loadDeckPopup;
    private VisualElement loadDeckList;
    private DropdownField filterRarity;
    private DropdownField filterType;

    private Button btnBack;
    private Button btnSave;
    private Button btnLoad;
    private Button btnNew;
    private Button btnClear;
    private Button btnAutoFill;
    private Button btnCloseLoad;

    private PlayerCollection playerCollection;
    private DeckData currentDeck;

    private void OnEnable()
    {
        if (mainMenuController == null)
            mainMenuController =
                FindFirstObjectByType<MainMenuController>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource =
                    gameObject.AddComponent<AudioSource>();
        }
    }

    private void QueryElements()
    {
        deckNameInput = root.Q<TextField>("deck-name-input");
        deckSizeLabel = root.Q<Label>("deck-size");
        legendaryCountLabel = root.Q<Label>("legendary-count");
        rareCountLabel = root.Q<Label>("rare-count");
        commonCountLabel = root.Q<Label>("common-count");
        validationLabel = root.Q<Label>("validation-text");
        collectionGrid = root.Q<VisualElement>("collection-grid");
        deckList = root.Q<VisualElement>("deck-list");
        loadDeckPopup = root.Q<VisualElement>("load-deck-popup");
        loadDeckList = root.Q<VisualElement>("load-deck-list");
        filterRarity = root.Q<DropdownField>("filter-rarity");
        filterType = root.Q<DropdownField>("filter-type");

        btnBack = root.Q<Button>("btn-back");
        btnSave = root.Q<Button>("btn-save");
        btnLoad = root.Q<Button>("btn-load");
        btnNew = root.Q<Button>("btn-new");
        btnClear = root.Q<Button>("btn-clear");
        btnAutoFill = root.Q<Button>("btn-autofill");
        btnCloseLoad = root.Q<Button>("btn-close-load");
    }

    private void SetupFilters()
    {
        if (filterRarity != null)
        {
            filterRarity.choices = new List<string>
                { "All", "Common", "Rare", "Legendary" };
            filterRarity.index = 0;
            filterRarity.RegisterValueChangedCallback(
                evt => RefreshCollectionDisplay());
        }

        if (filterType != null)
        {
            filterType.choices = new List<string>
                { "All", "Tower", "Spell", "Trap" };
            filterType.index = 0;
            filterType.RegisterValueChangedCallback(
                evt => RefreshCollectionDisplay());
        }
    }

    private void BindButtons()
    {
        btnBack?.RegisterCallback<ClickEvent>(evt =>
        {
            PlayClick();
            mainMenuController?.BackToMainMenu();
        });

        btnSave?.RegisterCallback<ClickEvent>(evt =>
        {
            PlayClick();
            SaveDeck();
        });

        btnLoad?.RegisterCallback<ClickEvent>(evt =>
        {
            PlayClick();
            OpenLoadDeckPanel();
        });

        btnNew?.RegisterCallback<ClickEvent>(evt =>
        {
            PlayClick();
            CreateNewDeck();
        });

        btnClear?.RegisterCallback<ClickEvent>(evt =>
        {
            PlayClick();
            ClearDeck();
        });

        btnAutoFill?.RegisterCallback<ClickEvent>(evt =>
        {
            PlayClick();
            AutoFillDeck();
        });

        btnCloseLoad?.RegisterCallback<ClickEvent>(evt =>
        {
            PlayClick();
            CloseLoadDeckPanel();
        });

        deckNameInput?.RegisterValueChangedCallback(evt =>
        {
            if (currentDeck != null)
            {
                currentDeck.deckName = evt.newValue;
                ValidateDeck();
            }
        });
    }

    // ==========================================
    // DECK MANAGEMENT
    // ==========================================

    private void CreateNewDeck()
    {
        currentDeck = ScriptableObject.CreateInstance<DeckData>();
        currentDeck.deckName = "New Deck";
        currentDeck.preferredArena =
            ElementumDefense.Elements.ElementType.Fire;

        if (deckNameInput != null)
            deckNameInput.value = currentDeck.deckName;

        RefreshCollectionDisplay();
        RefreshDeckDisplay();
        ValidateDeck();
    }

    private void SaveDeck()
    {
        if (currentDeck == null) return;

        if (deckNameInput != null)
            currentDeck.deckName = deckNameInput.value;

        if (!currentDeck.IsValid(out string error))
        {
            Debug.LogWarning(
                $"[DeckbuilderUI] Invalid: {error}");
            return;
        }

        playerCollection?.SaveUserDeck(currentDeck);
        Debug.Log(
            $"[DeckbuilderUI] Saved '{currentDeck.deckName}'");
    }

    private void ClearDeck()
    {
        if (currentDeck == null) return;
        currentDeck.Clear();
        RefreshDeckDisplay();
        ValidateDeck();
    }

    private void AutoFillDeck()
    {
        if (currentDeck == null || playerCollection == null)
            return;

        ClearDeck();

        var unlocked = playerCollection.GetUnlockedCards()
            .OrderBy(x => Random.value).ToList();

        int leg = DeckData.MAX_LEGENDARY;
        int rar = DeckData.MAX_RARE;
        int com = DeckData.MAX_COMMON;

        foreach (var card in unlocked)
        {
            if (currentDeck.cards.Count >= DeckData.MAX_DECK_SIZE)
                break;

            switch (card.rarity)
            {
                case CardRarity.Legendary:
                    if (leg > 0 && currentDeck.AddCard(card)) leg--;
                    break;
                case CardRarity.Rare:
                    if (rar > 0 && currentDeck.AddCard(card)) rar--;
                    break;
                case CardRarity.Common:
                    if (com > 0 && currentDeck.AddCard(card)) com--;
                    break;
            }
        }

        RefreshDeckDisplay();
        ValidateDeck();
    }

    public void AddCardToDeck(CardData card)
    {
        if (currentDeck == null || card == null) return;
        if (!playerCollection.IsUnlocked(card)) return;

        if (playerCollection != null && playerCollection.GetPlayerDecks().Any(d => d.deckName == currentDeck.deckName && d.isDefaultDeck))
        {
            return;
        }

        if (currentDeck.AddCard(card))
        {
            PlayClick();
            RefreshDeckDisplay();
            ValidateDeck();
        }
    }

    public void RemoveCardFromDeck(CardData card)
    {
        if (currentDeck == null || card == null) return;

        if (playerCollection != null && playerCollection.GetPlayerDecks().Any(d => d.deckName == currentDeck.deckName && d.isDefaultDeck))
        {
            return;
        }

        if (currentDeck.RemoveCard(card))
        {
            PlayClick();
            RefreshDeckDisplay();
            ValidateDeck();
        }
    }

    // ==========================================
    // COLLECTION DISPLAY
    // ==========================================

    private void RefreshCollectionDisplay()
    {
        if (collectionGrid == null || playerCollection == null)
            return;

        collectionGrid.Clear();

        var cards = GetFilteredCards();

        if (cards.Count == 0)
        {
            var empty = BuildEmptyState("NO CARDS FOUND");
            collectionGrid.Add(empty);
            return;
        }

        foreach (var card in cards)
        {
            var element = BuildCollectionCard(card);
            collectionGrid.Add(element);
        }
    }

    private VisualElement BuildCollectionCard(CardData card)
    {
        var slot = new VisualElement();
        slot.AddToClassList("collection-card");

        string rarityKey = card.rarity switch
        {
            CardRarity.Legendary => "legendary",
            CardRarity.Rare => "rare",
            _ => "common"
        };

        slot.AddToClassList($"rarity-{rarityKey}");

        bool unlocked = playerCollection.IsUnlocked(card);
        if (!unlocked) slot.AddToClassList("locked");

        // Wewnętrzna ramka dekoracyjna
        var innerFrame = new VisualElement();
        innerFrame.AddToClassList("card-inner-frame");
        slot.Add(innerFrame);

        // Ikona
        if (card.cardIcon != null)
        {
            var icon = new VisualElement();
            icon.AddToClassList("card-icon-image");
            icon.style.backgroundImage =
                new StyleBackground(card.cardIcon);
            slot.Add(icon);
        }

        // Górna linia rarity
        var lineTop = new VisualElement();
        lineTop.AddToClassList("card-rarity-line-top");
        lineTop.AddToClassList(
            $"card-rarity-line-{rarityKey}");
        slot.Add(lineTop);

        // Nazwa
        var nameLabel = new Label(card.cardName);
        nameLabel.AddToClassList("card-slot-name");
        slot.Add(nameLabel);

        // Rarity text
        var rarityLabel = new Label(
            card.rarity.ToString().ToUpper());
        rarityLabel.AddToClassList("card-slot-rarity");
        rarityLabel.AddToClassList(
            $"card-slot-rarity-{rarityKey}");
        slot.Add(rarityLabel);

        // Dolna linia rarity
        var lineBottom = new VisualElement();
        lineBottom.AddToClassList("card-rarity-line-bottom");
        lineBottom.AddToClassList(
            $"card-rarity-line-{rarityKey}");
        slot.Add(lineBottom);

        // Description
        if (!string.IsNullOrEmpty(card.description))
        {
            var desc = new Label(card.description);
            desc.AddToClassList("card-slot-description");
            slot.Add(desc);
        }

        // Hover accent line na dole
        var hoverAccent = new VisualElement();
        hoverAccent.AddToClassList("card-hover-accent");
        hoverAccent.AddToClassList(
            $"card-hover-accent-{rarityKey}");
        slot.Add(hoverAccent);

        // Kliknięcie
        if (unlocked)
        {
            slot.RegisterCallback<ClickEvent>(evt =>
            {
                AddCardToDeck(card);
                evt.StopPropagation();
            });
        }

        return slot;
    }

    // ==========================================
    // DECK DISPLAY
    // ==========================================

    private void RefreshDeckDisplay()
    {
        if (deckList == null || currentDeck == null) return;

        deckList.Clear();

        var grouped = currentDeck.cards
            .Where(c => c != null)
            .GroupBy(c => c)
            .ToDictionary(g => g.Key, g => g.Count());

        if (grouped.Count == 0)
        {
            var empty = BuildEmptyState("DECK EMPTY");
            deckList.Add(empty);
            UpdateCounters();
            return;
        }

        foreach (var entry in grouped)
        {
            var cardSlot = BuildDeckSlot(
                entry.Key, entry.Value);
            deckList.Add(cardSlot);
        }

        UpdateCounters();
    }

    private VisualElement BuildDeckSlot(
        CardData card, int count)
    {
        var slot = new VisualElement();
        slot.AddToClassList("deck-card-slot");

        string rarityKey = card.rarity switch
        {
            CardRarity.Legendary => "legendary",
            CardRarity.Rare => "rare",
            _ => "common"
        };

        // Rarity accent linia na lewej krawędzi
        var accent = new VisualElement();
        accent.AddToClassList("deck-card-rarity-accent");
        accent.AddToClassList(
            $"deck-card-rarity-accent-{rarityKey}");
        slot.Add(accent);

        // Ikona
        if (card.cardIcon != null)
        {
            var icon = new VisualElement();
            icon.AddToClassList("deck-card-icon");
            icon.style.backgroundImage =
                new StyleBackground(card.cardIcon);
            slot.Add(icon);
        }

        // Info
        var info = new VisualElement();
        info.AddToClassList("deck-card-info");

        var nameLabel = new Label(card.cardName);
        nameLabel.AddToClassList("deck-card-name");
        info.Add(nameLabel);

        var rarityLabel = new Label(
            card.rarity.ToString().ToUpper());
        rarityLabel.AddToClassList("deck-card-rarity");
        rarityLabel.style.color =
            new StyleColor(card.GetRarityColor());
        info.Add(rarityLabel);

        slot.Add(info);

        // Count
        var countLabel = new Label($"x{count}");
        countLabel.AddToClassList("deck-card-count");
        slot.Add(countLabel);

        // Remove hint
        var removeHint = new Label("\u2715");
        removeHint.AddToClassList("deck-card-remove-hint");
        slot.Add(removeHint);

        // Kliknięcie = usuń
        slot.RegisterCallback<ClickEvent>(evt =>
        {
            RemoveCardFromDeck(card);
            evt.StopPropagation();
        });

        return slot;
    }

    // ==========================================
    // EMPTY STATE
    // ==========================================

    private VisualElement BuildEmptyState(string message)
    {
        var container = new VisualElement();
        container.AddToClassList("empty-state");

        var icon = new Label("\u2727");
        icon.AddToClassList("empty-state-icon");
        container.Add(icon);

        var text = new Label(message);
        text.AddToClassList("empty-state-text");
        container.Add(text);

        return container;
    }

    // ==========================================
    // FILTERS
    // ==========================================

    private List<CardData> GetFilteredCards()
    {
        if (playerCollection == null)
            return new List<CardData>();

        var cards = playerCollection.GetUnlockedCards();

        if (filterRarity != null && filterRarity.index > 0)
        {
            CardRarity target =
                (CardRarity)(filterRarity.index - 1);
            cards = cards
                .Where(c => c.rarity == target).ToList();
        }

        if (filterType != null && filterType.index > 0)
        {
            CardType target =
                (CardType)(filterType.index - 1);
            cards = cards
                .Where(c => c.cardType == target).ToList();
        }

        return cards;
    }

    // ==========================================
    // COUNTERS & VALIDATION
    // ==========================================

    private void UpdateCounters()
    {
        if (currentDeck == null) return;

        int total = currentDeck.cards.Count;

        if (deckSizeLabel != null)
        {
            deckSizeLabel.text =
                $"{total}/{DeckData.MAX_DECK_SIZE} CARDS";
            deckSizeLabel.style.color = new StyleColor(
                total >= DeckData.MIN_DECK_SIZE
                    ? new Color(0.29f, 0.87f, 0.5f)
                    : new Color(0.97f, 0.44f, 0.44f));
        }

        var (leg, rare, com) =
            currentDeck.GetRarityCounts();

        if (legendaryCountLabel != null)
            legendaryCountLabel.text =
                $"LEGENDARY  {leg}/{DeckData.MAX_LEGENDARY}";

        if (rareCountLabel != null)
            rareCountLabel.text =
                $"RARE  {rare}/{DeckData.MAX_RARE}";

        if (commonCountLabel != null)
            commonCountLabel.text =
                $"COMMON  {com}/{DeckData.MAX_COMMON}";
    }

    private void ValidateDeck()
    {
        if (currentDeck == null) return;

        bool valid = currentDeck.IsValid(out string error);

        bool isDefault = false;
        if (playerCollection != null)
        {
            isDefault = playerCollection.GetPlayerDecks().Any(d => d.deckName == currentDeck.deckName && d.isDefaultDeck);
        }

        if (validationLabel != null)
        {
            if (isDefault)
            {
                validationLabel.text = "\u2717 LOCKED (DEFAULT DECK)";
                validationLabel.RemoveFromClassList("validation-valid");
                validationLabel.AddToClassList("validation-invalid");
            }
            else
            {
                validationLabel.text = valid
                    ? "\u2713 DECK VALID"
                    : $"\u2717 {error.ToUpper()}";

                validationLabel.RemoveFromClassList(
                    "validation-valid");
                validationLabel.RemoveFromClassList(
                    "validation-invalid");
                validationLabel.AddToClassList(
                    valid ? "validation-valid"
                          : "validation-invalid");
            }
        }

        if (btnSave != null)
            btnSave.SetEnabled(valid && !isDefault);

        if (btnClear != null)
            btnClear.SetEnabled(!isDefault);

        if (btnAutoFill != null)
            btnAutoFill.SetEnabled(!isDefault);

        UpdateCounters();
    }

    // ==========================================
    // LOAD POPUP
    // ==========================================

    private void OpenLoadDeckPanel()
    {
        if (loadDeckPopup == null) return;

        loadDeckPopup.RemoveFromClassList("hidden");

        if (loadDeckList == null || playerCollection == null)
            return;

        loadDeckList.Clear();

        var decks = playerCollection.GetPlayerDecks();

        if (decks.Count == 0)
        {
            var empty = BuildEmptyState("NO SAVED DECKS");
            loadDeckList.Add(empty);
            return;
        }

        foreach (var deck in decks)
        {
            var deckRef = deck;

            var item = new VisualElement();
            item.AddToClassList("popup-deck-item");

            // Ikona dekoracyjna
            var deckIcon = new Label("\u2726");
            deckIcon.AddToClassList("popup-deck-icon");
            item.Add(deckIcon);

            // Nazwa
            var nameLabel = new Label(deck.deckName);
            nameLabel.AddToClassList("popup-deck-name");
            item.Add(nameLabel);

            // Ilość kart
            var countLabel = new Label(
                $"{deck.cards.Count} cards");
            countLabel.AddToClassList("popup-deck-count");
            item.Add(countLabel);

            item.RegisterCallback<ClickEvent>(evt =>
            {
                PlayClick();
                LoadDeck(deckRef);
                evt.StopPropagation();
            });

            loadDeckList.Add(item);
        }
    }

    private void LoadDeck(DeckData deckToLoad)
    {
        if (deckToLoad == null) return;

        currentDeck = Instantiate(deckToLoad);
        currentDeck.name = deckToLoad.deckName;

        if (deckNameInput != null)
            deckNameInput.value = currentDeck.deckName;

        RefreshDeckDisplay();
        ValidateDeck();
        CloseLoadDeckPanel();
    }

    private void CloseLoadDeckPanel()
    {
        loadDeckPopup?.AddToClassList("hidden");
    }

    // ==========================================
    // SHOW / HIDE
    // ==========================================

    public void Show()
    {
        var uiDoc = GetComponent<UIDocument>();

        if (uiDoc == null)
        {
            Debug.LogError("[DeckbuilderUI] No UIDocument!");
            return;
        }

        uiDoc.enabled = true;
        gameObject.SetActive(true);

        root = uiDoc.rootVisualElement;

        if (root != null)
            root.style.display = DisplayStyle.Flex;

        StartCoroutine(ShowDelayed());
    }

    private System.Collections.IEnumerator ShowDelayed()
    {
        // Wait 2 frames — UIDocument needs time to
        // fully initialize layout after display change
        yield return new WaitForSeconds(0.1f);

        var uiDoc = GetComponent<UIDocument>();
        root = uiDoc.rootVisualElement;

        if (root == null)
        {
            Debug.LogError(
                "[DeckbuilderUI] root still null!");
            yield break;
        }

        root.style.display = DisplayStyle.Flex;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource =
                    gameObject.AddComponent<AudioSource>();
        }

        if (mainMenuController == null)
            mainMenuController =
                FindFirstObjectByType<MainMenuController>();

        playerCollection = PlayerCollection.Instance;

        QueryElements();
        SetupFilters();
        BindButtons();
        CreateNewDeck();

        // Force layout rebuild
        root.MarkDirtyRepaint();

        Debug.Log(
            $"[DeckbuilderUI] Show complete. " +
            $"root={root != null}, " +
            $"grid={collectionGrid != null}, " +
            $"player={playerCollection != null}, " +
            $"cards={playerCollection?.GetUnlockedCards()?.Count ?? 0}");
    }

    public void Hide()
    {
        StopAllCoroutines();

        // Force hide even if root wasn't queried yet
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc != null && uiDoc.rootVisualElement != null)
        {
            uiDoc.rootVisualElement.style.display =
                DisplayStyle.None;
        }

        root = uiDoc?.rootVisualElement;
        CloseLoadDeckPanel();
    }

    public DeckData GetCurrentDeck() => currentDeck;

    private void PlayClick()
    {
        if (buttonClickSound != null && audioSource != null)
            audioSource.PlayOneShot(buttonClickSound, 0.7f);
    }
}
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections.Generic;
//using System.Linq;
//using Unity.VisualScripting;

//namespace ElementumDefense.Cards
//{
//    /// <summary>
//    /// Full deckbuilder UI system
//    /// Allows creating/editing decks with drag & drop
//    /// Shows rarity limits, validation, save/load
//    /// </summary>
//    public class DeckbuilderUI : MonoBehaviour
//    {
//        [Header("Main Panels")]
//        [SerializeField] private GameObject deckbuilderPanel;

//        [Header("Deck Info")]
//        [SerializeField] private TMP_InputField deckNameInput;
//        [SerializeField] private TMP_Dropdown arenaTypeDropdown;
//        [SerializeField] private TextMeshProUGUI deckSizeText; // "25/30 cards"

//        [Header("Rarity Counters")]
//        [SerializeField] private TextMeshProUGUI legendaryCountText; // "3/5"
//        [SerializeField] private TextMeshProUGUI rareCountText;      // "7/10"
//        [SerializeField] private TextMeshProUGUI commonCountText;    // "15/15"

//        [Header("Collection Display")]
//        [SerializeField] private Transform collectionContent; // ScrollView content
//        [SerializeField] private GameObject cardSlotPrefab;   // Prefab for card display
//        [SerializeField] private TMP_Dropdown filterRarityDropdown;
//        [SerializeField] private TMP_Dropdown filterTypeDropdown;

//        [Header("Deck Display")]
//        [SerializeField] private Transform deckContent; // ScrollView content for current deck
//        [SerializeField] private GameObject deckCardSlotPrefab;

//        [Header("Buttons")]
//        [SerializeField] private Button saveDeckButton;
//        [SerializeField] private Button loadDeckButton;
//        [SerializeField] private Button newDeckButton;
//        [SerializeField] private Button clearDeckButton;
//        [SerializeField] private Button autoFillButton;

//        [Header("Validation")]
//        [SerializeField] private TextMeshProUGUI validationText;
//        [SerializeField] private Image validationIcon;
//        [SerializeField] private Sprite validIcon;
//        [SerializeField] private Sprite invalidIcon;

//        [Header("Load Deck UI")]
//        [SerializeField] private GameObject loadDeckPanel;
//        [SerializeField] private Transform loadDeckContent; // Content z DeckListScrollView
//        [SerializeField] private GameObject loadDeckSlotPrefab;
//        [SerializeField] private Button closeLoadPanelButton;
//        // Runtime data
//        private DeckData currentDeck;
//        private PlayerCollection playerCollection;
//        private List<CardData> filteredCards = new List<CardData>();

//        // ==========================================
//        // INITIALIZATION
//        // ==========================================

//        private void Start()
//        {
//            playerCollection = PlayerCollection.Instance;

//            if (playerCollection == null)
//            {
//                Debug.LogError("[DeckbuilderUI] PlayerCollection not found!");
//                return;
//            }

//            // Setup buttons
//            if (saveDeckButton != null)
//                saveDeckButton.onClick.AddListener(SaveDeck);

//            if (loadDeckButton != null)
//                loadDeckButton.onClick.AddListener(OpenLoadDeckPanel); // Zmieniamy z LoadDeck na OpenLoadDeckPanel

//            if (closeLoadPanelButton != null)
//                closeLoadPanelButton.onClick.AddListener(CloseLoadDeckPanel);

//            if (newDeckButton != null)
//                newDeckButton.onClick.AddListener(CreateNewDeck);

//            if (clearDeckButton != null)
//                clearDeckButton.onClick.AddListener(ClearDeck);

//            if (autoFillButton != null)
//                autoFillButton.onClick.AddListener(AutoFillDeck);

//            // Setup dropdowns
//            if (filterRarityDropdown != null)
//                filterRarityDropdown.onValueChanged.AddListener((_) => RefreshCollectionDisplay());

//            if (filterTypeDropdown != null)
//                filterTypeDropdown.onValueChanged.AddListener((_) => RefreshCollectionDisplay());

//            if (loadDeckPanel != null)
//                loadDeckPanel.SetActive(false);
//            // Create new deck
//            CreateNewDeck();

//            // Initial display
//            RefreshCollectionDisplay();
//            RefreshDeckDisplay();

//        }

//        // ==========================================
//        // DECK MANAGEMENT
//        // ==========================================

//        /// <summary>
//        /// Creates new empty deck
//        /// </summary>
//        private void CreateNewDeck()
//        {
//            currentDeck = ScriptableObject.CreateInstance<DeckData>();
//            currentDeck.deckName = "New Deck";
//            currentDeck.preferredArena = ElementumDefense.Elements.ElementType.Fire;

//            if (deckNameInput != null)
//            {
//                deckNameInput.text = currentDeck.deckName;
//            }

//            RefreshDeckDisplay();
//            ValidateDeck();

//            Debug.Log("[DeckbuilderUI] Created new deck");
//        }

//        /// <summary>
//        /// Saves current deck to file
//        /// </summary>
//        private void SaveDeck()
//        {
//            if (currentDeck == null) return;

//            if (deckNameInput != null)
//                currentDeck.deckName = deckNameInput.text;

//            if (!currentDeck.IsValid(out string errorMessage))
//            {
//                Debug.LogWarning($"Invalid deck: {errorMessage}");
//                return;
//            }

//            // ZMIANA: Zapisujemy przez PlayerCollection (do JSON gracza)
//            playerCollection.SaveUserDeck(currentDeck);

//            Debug.Log($"[DeckbuilderUI] Saved deck '{currentDeck.deckName}' to user profile.");
//            //            if (currentDeck == null)
//            //            {
//            //                Debug.LogError("[DeckbuilderUI] No deck to save!");
//            //                return;
//            //            }

//            //            // Update deck name from input
//            //            if (deckNameInput != null)
//            //            {
//            //                currentDeck.deckName = deckNameInput.text;
//            //            }

//            //            // Validate before saving
//            //            if (!currentDeck.IsValid(out string errorMessage))
//            //            {
//            //                Debug.LogWarning($"[DeckbuilderUI] Cannot save invalid deck: {errorMessage}");
//            //                // TODO: Show error popup
//            //                return;
//            //            }

//            //            // Save to Resources (or custom save system)
//            //            string path = $"Assets/Resources/Decks/{currentDeck.deckName}.asset";

//            //#if UNITY_EDITOR
//            //            UnityEditor.AssetDatabase.CreateAsset(currentDeck, path);
//            //            UnityEditor.AssetDatabase.SaveAssets();
//            //            Debug.Log($"[DeckbuilderUI] Saved deck to: {path}");
//            //#else
//            //            // Runtime save (to JSON)
//            //            string json = JsonUtility.ToJson(new DeckSaveData(currentDeck), true);
//            //            string savePath = System.IO.Path.Combine(Application.persistentDataPath, $"{currentDeck.deckName}.deck");
//            //            System.IO.File.WriteAllText(savePath, json);
//            //            Debug.Log($"[DeckbuilderUI] Saved deck to: {savePath}");
//            //#endif
//        }

//        /// <summary>
//        /// Loads deck from file (TODO: show file browser)
//        /// </summary>
//        private void LoadDeck(DeckData deckToLoad)
//        {
//            if (deckToLoad == null) return;

//            // Klonujemy talię z pamięci PlayerCollection do edytora
//            currentDeck = Instantiate(deckToLoad);
//            currentDeck.name = deckToLoad.deckName;

//            if (deckNameInput != null) deckNameInput.text = currentDeck.deckName;

//            RefreshDeckDisplay();
//            ValidateDeck();
//            CloseLoadDeckPanel();
//        }

//        /// <summary>
//        /// Clears current deck
//        /// </summary>
//        private void ClearDeck()
//        {
//            if (currentDeck != null)
//            {
//                currentDeck.Clear();
//                RefreshDeckDisplay();
//                ValidateDeck();

//                Debug.Log("[DeckbuilderUI] Deck cleared");
//            }
//        }

//        /// <summary>
//        /// Auto-fills deck with random unlocked cards
//        /// </summary>
//        private void AutoFillDeck()
//        {
//            if (currentDeck == null) return;

//            ClearDeck();

//            List<CardData> unlockedCards = playerCollection.GetUnlockedCards();

//            if (unlockedCards.Count == 0)
//            {
//                Debug.LogWarning("[DeckbuilderUI] No unlocked cards to fill deck!");
//                return;
//            }

//            // Add cards respecting rarity limits
//            int legendariesNeeded = DeckData.MAX_LEGENDARY;
//            int raresNeeded = DeckData.MAX_RARE;
//            int commonsNeeded = DeckData.MAX_COMMON;

//            // Shuffle cards
//            List<CardData> shuffled = unlockedCards.OrderBy(x => Random.value).ToList();

//            foreach (CardData card in shuffled)
//            {
//                if (currentDeck.cards.Count >= DeckData.MAX_DECK_SIZE)
//                    break;

//                bool added = false;

//                switch (card.rarity)
//                {
//                    case CardRarity.Legendary:
//                        if (legendariesNeeded > 0)
//                        {
//                            added = currentDeck.AddCard(card);
//                            if (added) legendariesNeeded--;
//                        }
//                        break;

//                    case CardRarity.Rare:
//                        if (raresNeeded > 0)
//                        {
//                            added = currentDeck.AddCard(card);
//                            if (added) raresNeeded--;
//                        }
//                        break;

//                    case CardRarity.Common:
//                        if (commonsNeeded > 0)
//                        {
//                            added = currentDeck.AddCard(card);
//                            if (added) commonsNeeded--;
//                        }
//                        break;
//                }
//            }

//            RefreshDeckDisplay();
//            ValidateDeck();

//            Debug.Log($"[DeckbuilderUI] Auto-filled deck with {currentDeck.cards.Count} cards");
//        }
//        /// <summary>
//        /// Otwiera panel wczytywania decków i wypełnia go zapisanymi plikami.
//        /// </summary>
//        private void OpenLoadDeckPanel()
//        {
//            //if (loadDeckPanel == null || loadDeckContent == null || loadDeckSlotPrefab == null)
//            //{
//            //    Debug.LogError("[DeckbuilderUI] Load Deck UI nie jest skonfigurowane!");
//            //    return;
//            //}

//            //// Wyczyść starą listę
//            //foreach (Transform child in loadDeckContent)
//            //{
//            //    Destroy(child.gameObject);
//            //}

//            //// Wczytaj wszystkie DeckData z folderu Resources/Decks
//            //DeckData[] savedDecks = Resources.LoadAll<DeckData>("Decks");

//            //Debug.Log($"[DeckbuilderUI] Znaleziono {savedDecks.Length} zapisanych decków.");

//            //if (savedDecks.Length == 0)
//            //{
//            //    Debug.LogWarning("[DeckbuilderUI] Nie znaleziono żadnych zapisanych decków w folderze Resources/Decks/");
//            //    // Opcjonalnie: Pokaż tekst "No saved decks"
//            //}

//            //// Stwórz przycisk dla każdego zapisanego decku
//            //foreach (DeckData deck in savedDecks)
//            //{
//            //    // ========== POPRAWKA KRYTYCZNA ==========
//            //    // Stwórz lokalną kopię zmiennej, aby lambda ją poprawnie przechwyciła
//            //    DeckData currentDeckToLoad = deck;
//            //    // =========================================

//            //    GameObject slotObj = Instantiate(loadDeckSlotPrefab, loadDeckContent);

//            //    // Znajdź elementy i ustaw dane
//            //    TextMeshProUGUI deckNameText = slotObj.GetComponentInChildren<TextMeshProUGUI>();
//            //    Button selectButton = slotObj.GetComponent<Button>();

//            //    if (deckNameText != null)
//            //    {
//            //        deckNameText.text = currentDeckToLoad.deckName;
//            //    }

//            //    if (selectButton != null)
//            //    {
//            //        // Usuń poprzednie listenery na wszelki wypadek
//            //        selectButton.onClick.RemoveAllListeners();

//            //        // Dodaj listener z lokalną kopią zmiennej
//            //        selectButton.onClick.AddListener(() => {
//            //            Debug.Log($"[DeckbuilderUI] Kliknięto przycisk, aby załadować deck: {currentDeckToLoad.deckName}");
//            //            LoadDeck(currentDeckToLoad);
//            //        });
//            //    }
//            //}

//            //// Pokaż panel
//            //loadDeckPanel.SetActive(true);
//            if (loadDeckPanel == null || loadDeckContent == null || loadDeckSlotPrefab == null) return;

//            foreach (Transform child in loadDeckContent) Destroy(child.gameObject);

//            // ZMIANA: Pobieramy talie z PlayerCollection (konkretnego gracza)
//            List<DeckData> playerDecks = playerCollection.GetPlayerDecks();

//            Debug.Log($"[DeckbuilderUI] Found {playerDecks.Count} player decks.");

//            foreach (DeckData deck in playerDecks)
//            {
//                DeckData currentDeckToLoad = deck;
//                GameObject slotObj = Instantiate(loadDeckSlotPrefab, loadDeckContent);

//                TextMeshProUGUI deckNameText = slotObj.GetComponentInChildren<TextMeshProUGUI>();
//                Button selectButton = slotObj.GetComponent<Button>();

//                if (deckNameText != null) deckNameText.text = currentDeckToLoad.deckName;

//                if (selectButton != null)
//                {
//                    selectButton.onClick.RemoveAllListeners();
//                    selectButton.onClick.AddListener(() => {
//                        LoadDeck(currentDeckToLoad);
//                    });
//                }
//            }

//            loadDeckPanel.SetActive(true);
//        }

//        /// <summary>
//        /// Zamyka panel wczytywania decków.
//        /// </summary>
//        private void CloseLoadDeckPanel()
//        {
//            if (loadDeckPanel != null)
//            {
//                loadDeckPanel.SetActive(false);
//            }
//        }


//        // ==========================================
//        // CARD ADDING/REMOVING
//        // ==========================================

//        /// <summary>
//        /// Adds card to deck (called by card slot click)
//        /// </summary>
//        public void AddCardToDeck(CardData card)
//        {
//            if (currentDeck == null || card == null) return;

//            if (!playerCollection.IsUnlocked(card))
//            {
//                Debug.LogWarning($"[DeckbuilderUI] Card '{card.cardName}' not unlocked!");
//                return;
//            }

//            bool success = currentDeck.AddCard(card);

//            if (success)
//            {
//                RefreshDeckDisplay();
//                ValidateDeck();
//                Debug.Log($"[DeckbuilderUI] Added '{card.cardName}' to deck");
//            }
//            else
//            {
//                Debug.LogWarning($"[DeckbuilderUI] Cannot add '{card.cardName}' (limit reached)");
//            }
//        }

//        /// <summary>
//        /// Removes card from deck (called by deck card slot click)
//        /// </summary>
//        public void RemoveCardFromDeck(CardData card)
//        {
//            if (currentDeck == null || card == null) return;

//            bool success = currentDeck.RemoveCard(card);

//            if (success)
//            {
//                RefreshDeckDisplay();
//                ValidateDeck();
//                Debug.Log($"[DeckbuilderUI] Removed '{card.cardName}' from deck");
//            }
//        }

//        // ==========================================
//        // DISPLAY REFRESH
//        // ==========================================

//        /// <summary>
//        /// Refreshes collection display (left side)
//        /// </summary>
//private void RefreshCollectionDisplay()
//        {
//            if (collectionContent == null || cardSlotPrefab == null || playerCollection == null) return;

//            foreach (Transform child in collectionContent)
//            {
//                Destroy(child.gameObject);
//            }

//            filteredCards = GetFilteredCards();

//            foreach (CardData card in filteredCards)
//            {
//                GameObject slotObj = Instantiate(cardSlotPrefab, collectionContent);

//                // Spróbuj użyć DeckbuilderCardSlot jeśli istnieje
//                DeckbuilderCardSlot cardSlot = slotObj.GetComponent<DeckbuilderCardSlot>();

//                if (cardSlot != null)
//                {
//                    // Użyj metody SetCard() która prawidłowo ustawia wszystkie elementy
//                    bool unlocked = playerCollection.IsUnlocked(card);
//                    cardSlot.SetCard(card, unlocked);
//                    cardSlot.SetClickCallback(() => AddCardToDeck(card));
//                }
//                else
//                {
//                    // Fallback - ręczne ustawienie
//                    Image cardIcon = slotObj.transform.Find("CardIcon")?.GetComponent<Image>();
//                    TextMeshProUGUI cardName = slotObj.transform.Find("CardName")?.GetComponent<TextMeshProUGUI>();
//                    TextMeshProUGUI rarityText = slotObj.transform.Find("RarityText")?.GetComponent<TextMeshProUGUI>();
//                    TextMeshProUGUI description = slotObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();
//                    Image rarityBorder = slotObj.transform.Find("RarityBorder")?.GetComponent<Image>();
//                    Image topLine = slotObj.transform.Find("LineTop")?.GetComponent<Image>();
//                    Image bottomLine = slotObj.transform.Find("LineBottom")?.GetComponent<Image>();
//                    Button clickButton = slotObj.GetComponent<Button>();

//                    // Ustaw dane karty
//                    if (cardIcon != null) cardIcon.sprite = card.cardIcon;
//                    if (cardName != null) cardName.text = card.cardName;

//                    // DODANE: Ustaw rarity text
//                    if (rarityText != null)
//                    {
//                        rarityText.text = card.rarity.ToString();
//                        //rarityText.color = card.GetRarityColor();
//                    }

//                    // DODANE: Ustaw ramkę rzadkości
//                    if (rarityBorder != null)
//                    {
//                        rarityBorder.color = card.GetRarityColor().WithAlpha(0.2f);
//                    }
//                    if (description != null)
//                    {
//                        description.text = card.description;
//                    }
//                    // DODANE: Ustaw linie (jeśli istnieją)
//                    if (topLine != null)
//                    {
//                        topLine.color = card.GetRarityColor();
//                    }
//                    if (bottomLine != null)
//                    {
//                        bottomLine.color = card.GetRarityColor();
//                    }

//                    // Ustaw kliknięcie
//                    if (clickButton != null)
//                    {
//                        clickButton.onClick.RemoveAllListeners();
//                        clickButton.onClick.AddListener(() => AddCardToDeck(card));
//                    }
//                }
//            }
//        }

//        /// <summary>
//        /// Refreshes deck display (right side)
//        /// </summary>
//private void RefreshDeckDisplay()
//        {
//            if (deckContent == null || deckCardSlotPrefab == null || currentDeck == null) return;

//            foreach (Transform child in deckContent)
//            {
//                Destroy(child.gameObject);
//            }

//            var groupedCards = currentDeck.cards
//         .Where(c => c != null)
//         .GroupBy(c => c)
//         .ToDictionary(g => g.Key, g => g.Count());

//            foreach (var cardEntry in groupedCards)
//            {
//                CardData card = cardEntry.Key;
//                int count = cardEntry.Value;

//                GameObject slotObj = Instantiate(deckCardSlotPrefab, deckContent);

//                // Znajdź wszystkie potrzebne elementy
//                Image fullCardImage = slotObj.transform.Find("Header/IconMask/FullCardImage")?.GetComponent<Image>();
//                TextMeshProUGUI cardNameText = slotObj.transform.Find("Header/CardName")?.GetComponent<TextMeshProUGUI>();
//                TextMeshProUGUI rarityText = slotObj.transform.Find("Header/RarityText")?.GetComponent<TextMeshProUGUI>();
//                Image rarityBorder = slotObj.transform.Find("RarityBorder")?.GetComponent<Image>();

//                // Spróbuj różnych ścieżek dla LineTop i LineBottom
//                Image lineTop = slotObj.transform.Find("LineTop")?.GetComponent<Image>();
//                Image lineBottom = slotObj.transform.Find("LineBottom")?.GetComponent<Image>();
//                TextMeshProUGUI description = slotObj.transform.Find("Description")?.GetComponent<TextMeshProUGUI>();

//                Button removeButton = slotObj.GetComponent<Button>();

//                if (fullCardImage != null)
//                {
//                    if (card.cardIcon != null)
//                    {
//                        fullCardImage.sprite = card.cardIcon;
//                        fullCardImage.color = Color.white;
//                    }
//                }

//                if (cardNameText != null)
//                {
//                    cardNameText.text = $"{card.cardName} x{count}";
//                }

//                if (rarityText != null)
//                {
//                    rarityText.text = card.rarity.ToString();
//                   // rarityText.color = card.GetRarityColor();
//                }

//                // Ustaw kolor ramki rzadkości
//                if (rarityBorder != null)
//                {
//                    rarityBorder.color = card.GetRarityColor().WithAlpha(0.2f);
//                }
//                if (description != null)
//                {
//                    description.text = card.description;
//                }
//                // Ustaw kolor linii górnej i dolnej
//                if (lineTop != null)
//                {
//                    lineTop.color = card.GetRarityColor();
//                }

//                if (lineBottom != null)
//                {
//                    lineBottom.color = card.GetRarityColor();
//                }

//                if (removeButton != null)
//                {
//                    removeButton.onClick.RemoveAllListeners();
//                    removeButton.onClick.AddListener(() => RemoveCardFromDeck(card));
//                }
//            }

//            UpdateCounters();
//        }

//        /// <summary>
//        /// Gets filtered cards based on dropdown selections
//        /// </summary>
//        private List<CardData> GetFilteredCards()
//        {
//            List<CardData> cards = playerCollection.GetUnlockedCards();

//            // Filter by rarity
//            if (filterRarityDropdown != null && filterRarityDropdown.value > 0)
//            {
//                CardRarity targetRarity = (CardRarity)(filterRarityDropdown.value - 1);
//                cards = cards.Where(c => c.rarity == targetRarity).ToList();
//            }

//            // Filter by type
//            if (filterTypeDropdown != null && filterTypeDropdown.value > 0)
//            {
//                CardType targetType = (CardType)(filterTypeDropdown.value - 1);
//                cards = cards.Where(c => c.cardType == targetType).ToList();
//            }

//            return cards;
//        }

//        // ==========================================
//        // VALIDATION & COUNTERS
//        // ==========================================

//        /// <summary>
//        /// Updates deck size and rarity counters
//        /// </summary>
//private void UpdateCounters()
//        {
//            if (currentDeck == null) return;

//            // Deck size
//            if (deckSizeText != null)
//            {
//                int current = currentDeck.cards.Count;
//                int max = DeckData.MAX_DECK_SIZE;
//                deckSizeText.text = $"{current}/{max} cards";

//                // Color coding
//                if (current < DeckData.MIN_DECK_SIZE)
//                    deckSizeText.color = Color.red;
//                else if (current >= DeckData.MIN_DECK_SIZE && current <= max)
//                    deckSizeText.color = Color.green;
//                else
//                    deckSizeText.color = Color.red;
//            }

//            // Rarity counts
//            var (leg, rare, com) = currentDeck.GetRarityCounts();

//            // LEGENDARY COUNTER
//            if (legendaryCountText != null)
//            {
//                legendaryCountText.text = $"LEGENDARY  {leg}/{DeckData.MAX_LEGENDARY}";
//                legendaryCountText.color = new Color(1f, 0.8f, 0f); // Gold
//            }

//            // RARE COUNTER
//            if (rareCountText != null)
//            {
//                rareCountText.text = $"RARE  {rare}/{DeckData.MAX_RARE}";
//                rareCountText.color = new Color(0.3f, 0.6f, 1f); // Blue
//            }

//            // COMMON COUNTER
//            if (commonCountText != null)
//            {
//                commonCountText.text = $"COMMON  {com}/{DeckData.MAX_COMMON}";
//                commonCountText.color = new Color(0.8f, 0.8f, 0.8f); // Gray
//            }
//        }

//        /// <summary>
//        /// Validates current deck and updates UI
//        /// </summary>
//        private void ValidateDeck()
//        {
//            if (currentDeck == null) return;

//            bool isValid = currentDeck.IsValid(out string errorMessage);

//            // Update validation UI
//            if (validationText != null)
//            {
//                validationText.text = isValid ? "✓ Deck Valid" : $"✗ {errorMessage}";
//                validationText.color = isValid ? Color.green : Color.red;
//            }

//            if (validationIcon != null)
//            {
//                validationIcon.sprite = isValid ? validIcon : invalidIcon;
//            }

//            // Enable/disable save button
//            if (saveDeckButton != null)
//            {
//                saveDeckButton.interactable = isValid;
//            }

//            UpdateCounters();
//        }

//        // ==========================================
//        // UTILITY
//        // ==========================================

//        /// <summary>
//        /// Gets current deck (for loading into match)
//        /// </summary>
//        public DeckData GetCurrentDeck()
//        {
//            return currentDeck;
//        }
//    }

//    // ==========================================
//    // HELPER CLASS - Card Slot in Deckbuilder
//    // ==========================================

//    /// <summary>
//    /// Single card slot in deckbuilder UI
//    /// Shows card info, locked/unlocked state, click to add/remove
//    /// Attach to card slot prefab
//    /// </summary>
//    public class DeckbuilderCardSlot : MonoBehaviour
//    {
//        [Header("UI Elements")]
//        public Image cardIcon;
//        public TextMeshProUGUI cardNameText;
//        public TextMeshProUGUI rarityText;
//        public Image rarityBorder;
//        public GameObject lockedOverlay;
//        public Button clickButton;

//        //test
//        public Image TopLine;
//        public Image BottomLine;

//        private CardData currentCard;
//        private bool isUnlocked;

//        public void SetCard(CardData card, bool unlocked)
//        {
//            currentCard = card;
//            isUnlocked = unlocked;

//            // Update visuals
//            if (cardIcon != null)
//            {
//                cardIcon.sprite = card.cardIcon;
//            }

//            if (cardNameText != null)
//            {
//                cardNameText.text = card.cardName;
//            }

//            if (rarityText != null)
//            {
//                rarityText.text = card.rarity.ToString();
//            }

//            if (rarityBorder != null)
//            {
//                rarityBorder.color = card.GetRarityColor().WithAlpha(0.2f);
//            }
//            //test
//            if (TopLine != null && BottomLine != null)
//            {
//                TopLine.color = card.GetRarityColor();
//                BottomLine.color = card.GetRarityColor();
//            }
//            // Locked state
//            if (lockedOverlay != null)
//            {
//                lockedOverlay.SetActive(!unlocked);
//            }

//            // Disable button if locked
//            if (clickButton != null)
//            {
//                clickButton.interactable = unlocked;
//            }
//        }

//        public void SetClickCallback(System.Action callback)
//        {
//            if (clickButton != null)
//            {
//                clickButton.onClick.RemoveAllListeners();
//                clickButton.onClick.AddListener(() => callback?.Invoke());
//            }
//        }
//    }

//    // ==========================================
//    // SAVE DATA FOR RUNTIME DECK SAVING
//    // ==========================================

//    [System.Serializable]
//    public class DeckSaveData
//    {
//        public string deckName;
//        public ElementumDefense.Elements.ElementType preferredArena;
//        public List<string> cardNames; // ScriptableObject.name

//        public DeckSaveData(DeckData deck)
//        {
//            deckName = deck.deckName;
//            preferredArena = deck.preferredArena;
//            cardNames = deck.cards.Select(c => c.name).ToList();
//        }
//    }
//}