using UnityEngine;
using TMPro;
using System.Collections;
using ElementumDefense.Cards;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class PreGameManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private GameObject preGamePanel;
    [SerializeField] private TextMeshProUGUI arenaInfoText;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private Transform deckSelectionContainer;
    [SerializeField] private GameObject deckSelectionButtonPrefab;

    [Header("Settings")]
    [SerializeField] private float deckSelectionTime = 10f;

    private GameStartCountdown gameStartCountdown;
    private const string ARENA_TYPE_KEY = "arenaType";
    private const string PREGAME_START_TIME_KEY = "PreGameStartTime";
    private const string DECK_SELECTED_KEY = "DeckSelected"; // ✅ NOWE
    private const string ALL_DECKS_READY_KEY = "AllDecksReady"; // ✅ NOWE

    private bool deckWasSelected = false;
    private bool preGamePhaseActive = false;
    private bool waitingForOthers = false; // ✅ NOWE

    public static PreGameManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (preGamePanel != null)
            preGamePanel.SetActive(false);
    }

    // ✅ MONITOROWANIE ROOM PROPERTIES
    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        // Start PreGame phase
        if (propertiesThatChanged.ContainsKey(PREGAME_START_TIME_KEY))
        {
            double startTime = (double)propertiesThatChanged[PREGAME_START_TIME_KEY];

            if (!preGamePhaseActive)
            {
                Debug.Log($"[PreGameManager] Otrzymano sygnał startu PreGame. StartTime: {startTime}");
                StartCoroutine(PreGameSequence(startTime));
            }
        }

        // ✅ WSZYSCY WYBRALI DECKI - START COUNTDOWN
        if (propertiesThatChanged.ContainsKey(ALL_DECKS_READY_KEY) &&
            (bool)propertiesThatChanged[ALL_DECKS_READY_KEY])
        {
            if (waitingForOthers)
            {
                Debug.Log("[PreGameManager] Wszyscy wybrali decki! Uruchamiam countdown.");
                StartGameCountdown();
            }
        }
    }

    // ✅ MONITOROWANIE PLAYER PROPERTIES
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        // Master Client sprawdza czy wszyscy wybrali decki
        if (PhotonNetwork.IsMasterClient && changedProps.ContainsKey(DECK_SELECTED_KEY))
        {
            CheckIfAllDecksSelected();
        }
    }

    public void StartPreGamePhase(GameStartCountdown countdown)
    {
        this.gameStartCountdown = countdown;
        if (this.gameStartCountdown != null)
        {
            this.gameStartCountdown.enabled = false;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            double startTime = PhotonNetwork.Time + 1.0;

            var roomProps = new ExitGames.Client.Photon.Hashtable();
            roomProps[PREGAME_START_TIME_KEY] = startTime;
            roomProps[ALL_DECKS_READY_KEY] = false; // ✅ Reset
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

            Debug.Log($"[PreGameManager - Master] Ustawiłem czas startu PreGame: {startTime}");
        }
        else
        {
            Debug.Log("[PreGameManager - Client] Czekam na sygnał startu od Master Clienta...");
        }
    }

    private IEnumerator PreGameSequence(double networkStartTime)
    {
        preGamePhaseActive = true;

        while (PhotonNetwork.Time < networkStartTime)
        {
            yield return null;
        }

        Debug.Log($"[PreGameManager] START odliczania wyboru decku (synchronizowany)");

        ShowArenaInfo();
        PopulateDeckSelection();

        if (preGamePanel != null)
            preGamePanel.SetActive(true);

        double endTime = networkStartTime + deckSelectionTime;

        while (PhotonNetwork.Time < endTime && !deckWasSelected)
        {
            float remaining = (float)(endTime - PhotonNetwork.Time);

            if (countdownText != null)
                countdownText.text = $"{Mathf.CeilToInt(remaining)}s";

            yield return null;
        }

        // ✅ TIMEOUT - auto-wybierz deck
        if (!deckWasSelected)
        {
            Debug.LogWarning("[PreGameManager] TIMEOUT - auto-wybór decku.");
            AutoSelectDeck();
        }
    }

    // ✅ NOWA METODA - AUTO-WYBÓR PRZY TIMEOUT
    private void AutoSelectDeck()
    {
        if (deckWasSelected) return;

        // ZMIANA: Pobierz z PlayerCollection
        List<DeckData> myDecks = PlayerCollection.Instance.GetPlayerDecks();

        if (myDecks.Count > 0)
        {
            OnDeckSelected(myDecks[0]);
        }
        else
        {
            // Fallback
            DeckData[] resDecks = Resources.LoadAll<DeckData>("Decks");
            if (resDecks.Length > 0) OnDeckSelected(resDecks[0]);
            else MarkDeckAsSelected();
        }
        //if (deckWasSelected) return;

        //DeckData[] savedDecks = Resources.LoadAll<DeckData>("Decks");

        //if (savedDecks.Length > 0)
        //{
        //    OnDeckSelected(savedDecks[0]);
        //}
        //else
        //{
        //    Debug.LogError("[PreGameManager] Brak decków do auto-wyboru!");
        //    // Fallback - oznacz jako gotowy bez decku
        //    MarkDeckAsSelected();
        //}
    }

    // ✅ ZMODYFIKOWANA - NIE URUCHAMIA OD RAZU COUNTDOWN
    private void OnDeckSelected(DeckData selectedDeck)
    {
        if (deckWasSelected) return;
        deckWasSelected = true;

        Debug.Log($"[PreGameManager] Wybrano deck: {selectedDeck.deckName}");

        if (DraftManager.Instance != null)
        {
            DraftManager.Instance.SetDeck(selectedDeck);
        }

        StopAllCoroutines(); // Zatrzymaj odliczanie wyboru

        MarkDeckAsSelected();
    }

    // ✅ NOWA METODA - OZNACZ GOTOWOŚĆ GRACZA
    private void MarkDeckAsSelected()
    {
        // Ukryj panel wyboru
        if (preGamePanel != null)
            preGamePanel.SetActive(false);

        // Pokaż wiadomość o czekaniu
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = "Czekam na innych graczy...";
        }

        waitingForOthers = true;

        // Ustaw Custom Property
        var playerProps = new ExitGames.Client.Photon.Hashtable();
        playerProps[DECK_SELECTED_KEY] = true;
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

        Debug.Log("[PreGameManager] Oznaczono deck jako wybrany. Czekam na innych...");
    }

    // ✅ NOWA METODA - MASTER SPRAWDZA CZY WSZYSCY GOTOWI
    private void CheckIfAllDecksSelected()
    {
        int readyCount = 0;
        int totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.ContainsKey(DECK_SELECTED_KEY) &&
                (bool)player.CustomProperties[DECK_SELECTED_KEY])
            {
                readyCount++;
            }
        }

        Debug.Log($"[PreGameManager - Master] Gotowych: {readyCount}/{totalPlayers}");

        if (readyCount >= totalPlayers)
        {
            Debug.Log("[PreGameManager - Master] Wszyscy wybrali decki! Wysyłam sygnał.");

            var roomProps = new ExitGames.Client.Photon.Hashtable();
            roomProps[ALL_DECKS_READY_KEY] = true;
            PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
        }
    }

    // ✅ NOWA METODA - URUCHOM COUNTDOWN (ZSYNCHRONIZOWANE)
    private void StartGameCountdown()
    {
        waitingForOthers = false;
        preGamePhaseActive = false;

        // Ukryj tekst czekania
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        Debug.Log("[PreGameManager] Wszyscy wybrali decki. Uruchamiam Draft...");

        // ✅ ZAMIAST COUNTDOWN → OD RAZU DRAFT
        if (DraftManager.Instance != null)
        {
            DraftManager.Instance.StartStarterDraft();
        }
        else
        {
            Debug.LogError("[PreGameManager] DraftManager nie znaleziony!");
        }

        // Reset Player Properties
        var playerProps = new ExitGames.Client.Photon.Hashtable();
        playerProps[DECK_SELECTED_KEY] = false;
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
    }

    private void ShowArenaInfo()
    {
        if (arenaInfoText != null && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ARENA_TYPE_KEY, out object arenaTypeObj))
        {
            string arenaType = (string)arenaTypeObj;
            arenaInfoText.text = $"Arena: {arenaType}";
        }
    }

    private void PopulateDeckSelection()
    {
        if (deckSelectionContainer == null || deckSelectionButtonPrefab == null) return;

        foreach (Transform child in deckSelectionContainer) Destroy(child.gameObject);

        // ZMIANA: Pobierz talie z PlayerCollection
        List<DeckData> myDecks = PlayerCollection.Instance.GetPlayerDecks();

        Debug.Log($"[PreGameManager] Found {myDecks.Count} decks for user.");

        if (myDecks.Count == 0)
        {
            // Fallback - jeśli gracz nie ma talii (błąd?), spróbuj załadować domyślną z Resources
            // To tylko zabezpieczenie
            Debug.LogWarning("No user decks found! Checking Resources backup...");
            DeckData[] resDecks = Resources.LoadAll<DeckData>("Decks");
            if (resDecks.Length > 0) CreateDeckButton(resDecks[0]);
            return;
        }

        foreach (DeckData deck in myDecks)
        {
            CreateDeckButton(deck);
        }
        //if (deckSelectionContainer == null || deckSelectionButtonPrefab == null) return;

        //foreach (Transform child in deckSelectionContainer)
        //{
        //    Destroy(child.gameObject);
        //}

        //DeckData[] savedDecks = Resources.LoadAll<DeckData>("Decks");

        //Debug.Log($"[PreGameManager] Znaleziono {savedDecks.Length} zapisanych decków.");

        //if (savedDecks.Length == 0) return;

        //foreach (DeckData deck in savedDecks)
        //{
        //    CreateDeckButton(deck);
        //}
    }

    private void CreateDeckButton(DeckData deck)
    {
        GameObject buttonObj = Instantiate(deckSelectionButtonPrefab, deckSelectionContainer);
        buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = deck.deckName;

        UnityEngine.UI.Button button = buttonObj.GetComponent<UnityEngine.UI.Button>();
        button.onClick.AddListener(() => OnDeckSelected(deck));
    }
}