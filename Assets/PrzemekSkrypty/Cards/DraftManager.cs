using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Manages card drafting system for multiplayer matches
    /// Handles starter draft (5 cards) and mid-game drafts (1 of 3)
    /// </summary>
    public class DraftManager : MonoBehaviourPunCallbacks
    {
        public static DraftManager Instance { get; private set; }


        [Header("Deck Configuration")]
        [SerializeField, Tooltip("Player's deck for this match (loaded from menu)")]
        private DeckData playerDeck;

        [Header("Draft Timing")]
        [SerializeField, Tooltip("Time limit for starter draft (seconds)")]
        private float starterDraftTime = 60f;

        [SerializeField, Tooltip("Time limit for mid-game draft (seconds)")]
        private float midGameDraftTime = 30f;

        [SerializeField, Tooltip("Waves between mid-game drafts")]
        private int wavesBetweenDrafts = 5;

        [Header("Starter Draft Configuration")]
        [SerializeField, Tooltip("Starter draft rarity slots")]
        private CardRarity[] starterRaritySlots = new CardRarity[]
        {
            CardRarity.Legendary,
            CardRarity.Rare,
            CardRarity.Rare,
            CardRarity.Common,
            CardRarity.Common
        };

        [Header("Mid-Game Draft Configuration")]
        [SerializeField, Tooltip("How many cards to choose from (default 3)")]
        private int midGameChoices = 3;

        [Header("References")]
        private PlayerCardManager playerCardManager;

        // Draft state
        private bool isDrafting = false;
        private bool isStarterDraftComplete = false;
        private int nextDraftWave = 0;

        // Starter draft state
        private List<CardData> starterDraftedCards = new List<CardData>();
        private Dictionary<int, bool> starterSlotMulliganed = new Dictionary<int, bool>(); // Track which slots were rerolled

        // Mid-game draft state
        private CardData[] currentDraftChoices;

        // Events
        public System.Action<CardData[]> OnStarterDraftOffered; // 5 cards
        public System.Action<CardData[]> OnMidGameDraftOffered; // 3 cards
        public System.Action<CardData> OnCardDrafted;
        public System.Action OnDraftTimeout;
        public System.Action<float> OnDraftTimerUpdate; // Remaining time

        private bool waitingForConfirmation = false;
        public bool WaitingForConfirmation => waitingForConfirmation;
        private bool localDraftComplete = false;
        private const string IS_READY_KEY = "isReadyForWaves";
        private int playersReadyCount = 0;
        private PhotonView photonView;
        private const string CARDS_CONFIRMED_KEY = "CardsConfirmed";
        private const string ALL_CARDS_READY_KEY = "AllCardsReady";

        // ==========================================
        // INITIALIZATION
        // ==========================================

        private void Awake()
        {
            photonView = GetComponent<PhotonView>();
            if (photonView == null)
            {
                Debug.LogError("[DraftManager] PhotonView not found on this object!");
            }
            if (Instance != null && Instance != this)
            {
                Destroy(this); //gameObject
                return;
            }

            Instance = this;

            playerCardManager = GetComponent<PlayerCardManager>();

            // Initialize mulligan tracking
            for (int i = 0; i < starterRaritySlots.Length; i++)
            {
                starterSlotMulliganed[i] = false;
            }
        }

        // ==========================================
        // PUBLIC API - START DRAFTS
        // ==========================================

        /// <summary>
        /// Starts starter draft (5 cards with mulligan)
        /// Called by GameStartCountdown or lobby
        /// </summary>
        public void StartStarterDraft()
        {
            //if (isStarterDraftComplete)
            //{
            //    Debug.LogWarning("[DraftManager] Starter draft already completed!");
            //    return;
            //}

            //// ========== NOWE: Check if deck assigned ==========
            //if (playerDeck == null)
            //{
            //    Debug.LogError("[DraftManager] No deck assigned! Trying to load test deck...");
            //    TryLoadTestDeck();

            //    if (playerDeck == null)
            //    {
            //        Debug.LogError("[DraftManager] Still no deck! Cannot start draft.");
            //        return;
            //    }
            //}
            //// ==================================================

            //StartCoroutine(StarterDraftCoroutine());
            if (isStarterDraftComplete)
            {
                Debug.LogWarning("[DraftManager] Starter draft already completed!");
                return;
            }

            if (playerDeck == null)
            {
                Debug.LogWarning("[DraftManager] Gracz nie wybrał decku. Próbuję załadować domyślny/testowy deck.");
                TryLoadTestDeck();

                if (playerDeck == null)
                {
                    Debug.LogError("[DraftManager] FATAL: Brak dostępnego decku! Nie można rozpocząć draftu.");
                    // Awaryjnie zakończ fazę draftu, żeby gra mogła iść dalej bez kart
                    isDrafting = false;
                    isStarterDraftComplete = true;
                    return;
                }
            }

            StartCoroutine(StarterDraftCoroutine());
        }
        public void TryLoadTestDeck()
        {
            //// Try to load any deck from Resources/Decks/
            //DeckData[] decks = Resources.LoadAll<DeckData>("Decks");

            //if (decks.Length > 0)
            //{
            //    playerDeck = decks[0];
            //    Debug.Log($"[DraftManager] Loaded test deck: {playerDeck.deckName}");
            //}
            //else
            //{
            //    Debug.LogError("[DraftManager] No decks found in Resources/Decks/!");
            //}
            if (playerDeck != null) return; // Już mamy deck

            DeckData[] decks = Resources.LoadAll<DeckData>("Decks");
            if (decks.Length > 0)
            {
                playerDeck = decks[0]; // Ładuje pierwszy znaleziony
                Debug.Log($"[DraftManager] Załadowano domyślny deck: {playerDeck.deckName}");
            }
            else
            {
                Debug.LogError("[DraftManager] Nie znaleziono żadnych decków w folderze Resources/Decks/!");
            }
        }
        public DeckData GetPlayerDeck()
        {
            return playerDeck;
        }
        /// <summary>
        /// Checks if mid-game draft should trigger (called by WaveManager)
        /// </summary>
        public void CheckMidGameDraft(int currentWave)
        {
            if (!isStarterDraftComplete)
            {
                Debug.LogWarning("[DraftManager] Cannot start mid-game draft before starter draft!");
                return;
            }

            if (currentWave >= nextDraftWave)
            {
                nextDraftWave = currentWave + wavesBetweenDrafts;
                StartMidGameDraft();
            }
        }

        /// <summary>
        /// Manually trigger mid-game draft
        /// </summary>
        public void StartMidGameDraft()
        {
            if (isDrafting)
            {
                Debug.LogWarning("[DraftManager] Already drafting!");
                return;
            }

            StartCoroutine(MidGameDraftCoroutine());
        }

        // ==========================================
        // STARTER DRAFT (5 cards + mulligan)
        // ==========================================

        private IEnumerator StarterDraftCoroutine()
        {

            isDrafting = true;
            waitingForConfirmation = true;
            localDraftComplete = false; // ✅ RESET

            Debug.Log("[DraftManager] === STARTER DRAFT START ===");

            CardData[] offeredCards = DrawStarterCards();

            if (offeredCards == null || offeredCards.Length != 5)
            {
                Debug.LogError("[DraftManager] Failed to draw starter cards!");
                isDrafting = false;
                waitingForConfirmation = false;
                yield break;
            }

            OnStarterDraftOffered?.Invoke(offeredCards);

            float timeRemaining = starterDraftTime;

            while (timeRemaining > 0f && waitingForConfirmation)
            {
                OnDraftTimerUpdate?.Invoke(timeRemaining);
                timeRemaining -= Time.deltaTime;
                yield return null;
            }

            if (waitingForConfirmation) // Timeout
            {
                Debug.Log("[DraftManager] Starter draft TIMEOUT - auto-confirming");
                OnDraftTimeout?.Invoke();
                ConfirmStarterDraft(); // ✅ Wywołaj metodę zamiast tylko ustawiać flagę
            }

            // ✅ USUŃ STĄD - Activate cards dopiero po countdown
            // Teraz tylko czekamy na innych graczy
            Debug.Log("[DraftManager] Lokalny wybór kart zakończony. Czekam na innych...");
        }
        // ✅ DODAJ NOWĄ METODĘ - AKTYWACJA KART (wywoływana po countdown)
        public void ActivateStarterCards()
        {
            Debug.Log("[DraftManager] Aktywuję wybrane karty...");

            foreach (CardData card in starterDraftedCards)
            {
                if (card != null)
                {
                    ActivateCard(card);
                }
            }

            isStarterDraftComplete = true;
            isDrafting = false; // ✅ Dopiero teraz kończymy draft
        }
        private void Update()
        {
            // Master Client sprawdza gotowość po drafcie
            if (PhotonNetwork.IsMasterClient && isStarterDraftComplete && isDrafting)
            {
                CheckIfAllPlayersAreReady();
            }

            // ✅ NOWE - Master sprawdza potwierdzenia kart
            if (PhotonNetwork.IsMasterClient && waitingForConfirmation == false && localDraftComplete)
            {
                CheckIfAllCardsConfirmed();
            }
        }
        private void CheckIfAllCardsConfirmed()
        {
            int confirmedCount = 0;
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.ContainsKey(CARDS_CONFIRMED_KEY) &&
                    (bool)player.CustomProperties[CARDS_CONFIRMED_KEY])
                {
                    confirmedCount++;
                }
            }

            Debug.Log($"[DraftManager - Master] Potwierdzonych kart: {confirmedCount}/{PhotonNetwork.CurrentRoom.PlayerCount}");

            if (confirmedCount >= PhotonNetwork.CurrentRoom.PlayerCount)
            {
                Debug.Log("[DraftManager - Master] Wszyscy potwierdzili karty! Wysyłam sygnał.");

                var roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps[ALL_CARDS_READY_KEY] = true;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }
        }
        private void CheckIfAllPlayersAreReady()
        {
            int readyCount = 0;
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.CustomProperties.ContainsKey(IS_READY_KEY) &&
                    (bool)player.CustomProperties[IS_READY_KEY])
                {
                    readyCount++;
                }
            }

            if (readyCount >= PhotonNetwork.CurrentRoom.PlayerCount)
            {
                Debug.Log("[DraftManager - Master] Wszyscy gotowi! Ustawiam Room Property.");

                // ✅ ZAMIEŃ RPC NA ROOM PROPERTY
                var roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps["AllPlayersReadyForWaves"] = true;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }
        }

        // ✅ OBSŁUGA ZMIANY ROOM PROPERTIES
        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            // Koniec draftu (stara logika)
            if (propertiesThatChanged.ContainsKey("AllPlayersReadyForWaves") &&
                (bool)propertiesThatChanged["AllPlayersReadyForWaves"])
            {
                EndDraft();
            }

            // ✅ NOWE - Wszyscy potwierdzili karty → START COUNTDOWN
            if (propertiesThatChanged.ContainsKey(ALL_CARDS_READY_KEY) &&
                (bool)propertiesThatChanged[ALL_CARDS_READY_KEY])
            {
                Debug.Log("[DraftManager] Wszyscy potwierdzili karty! Uruchamiam countdown.");
                StartFinalCountdown();
            }
        }
        private void StartFinalCountdown()
        {
            // Reset Custom Property
            var playerProps = new ExitGames.Client.Photon.Hashtable();
            playerProps[CARDS_CONFIRMED_KEY] = false;
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

            // Znajdź GameStartCountdown
            GameStartCountdown countdown = FindObjectOfType<GameStartCountdown>();
            if (countdown != null)
            {
                countdown.StartCountdown();
            }
            else
            {
                Debug.LogError("[DraftManager] Nie znaleziono GameStartCountdown!");
            }
        }
        private void EndDraft()
        {
            if (!isDrafting) return; // Zabezpieczenie przed wielokrotnym wywołaniem

            Debug.Log($"[{PhotonNetwork.LocalPlayer.NickName}] Wszyscy gotowi - kończę fazę draftu.");
            isDrafting = false;

            // Zresetuj własną gotowość
            var playerProps = new ExitGames.Client.Photon.Hashtable { { IS_READY_KEY, false } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

            // Master resetuje Room Property
            if (PhotonNetwork.IsMasterClient)
            {
                var roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps["AllPlayersReadyForWaves"] = false;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);
            }
        }

        private void ResetReadyStatusForAllPlayers()
        {
            ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
            playerProps[IS_READY_KEY] = false;

            foreach (var player in PhotonNetwork.PlayerList)
            {
                player.SetCustomProperties(playerProps);
            }
        }
        /// <summary>
        /// Called by DraftUI when player confirms starter draft
        /// </summary>
        public void ConfirmStarterDraft()
        {
            if (!waitingForConfirmation)
            {
                Debug.LogWarning("[DraftManager] Not waiting for confirmation!");
                return;
            }

            waitingForConfirmation = false;
            localDraftComplete = true;

            Debug.Log("[DraftManager] ✅ Player confirmed starter draft!");

            // Oznacz w Custom Properties
            var playerProps = new ExitGames.Client.Photon.Hashtable();
            playerProps[CARDS_CONFIRMED_KEY] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

            // ✅ POKAŻ WIADOMOŚĆ "CZEKAM NA INNYCH"
            ShowWaitingMessage();
        }

        // ✅ NOWA METODA
        private void ShowWaitingMessage()
        {
            // Znajdź CountdownText (możesz przekazać referencję z GameStartCountdown)
            GameStartCountdown countdown = FindObjectOfType<GameStartCountdown>();
            if (countdown != null)
            {
                TextMeshProUGUI countdownText = countdown.GetComponent<TextMeshProUGUI>();
                // LUB lepiej - dodaj publiczne pole w GameStartCountdown:
                // public TextMeshProUGUI GetCountdownText() { return countdownText; }

                // Wtedy:
                TextMeshProUGUI text = countdown.GetCountdownText();
                if (text != null)
                {
                    text.gameObject.SetActive(true);
                    text.text = "Czekam na innych graczy...";
                    Debug.Log("[DraftManager] Pokazano wiadomość oczekiwania.");
                }
            }
        }
        /// <summary>
        /// Draws 5 cards based on starter rarity slots
        /// </summary>
        private CardData[] DrawStarterCards()
        {
            CardData[] cards = new CardData[5];

            for (int i = 0; i < starterRaritySlots.Length; i++)
            {
                CardRarity targetRarity = starterRaritySlots[i];
                CardData card = DrawRandomCardFromDeck(targetRarity);

                if (card == null)
                {
                    Debug.LogWarning($"[DraftManager] Failed to draw {targetRarity} card for slot {i}!");
                }

                cards[i] = card;
                starterDraftedCards.Add(card);
            }

            return cards;
        }

        /// <summary>
        /// Mulligans (rerolls) a card slot
        /// Can only be done ONCE per slot
        /// </summary>
        public bool MulliganCard(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= starterDraftedCards.Count)
            {
                Debug.LogError($"[DraftManager] Invalid slot index: {slotIndex}");
                return false;
            }

            if (starterSlotMulliganed[slotIndex])
            {
                Debug.LogWarning($"[DraftManager] Slot {slotIndex} already mulliganed!");
                return false;
            }

            // Get target rarity
            CardRarity targetRarity = starterRaritySlots[slotIndex];

            // Draw new card
            CardData newCard = DrawRandomCardFromDeck(targetRarity);

            if (newCard == null)
            {
                Debug.LogError($"[DraftManager] Failed to mulligan slot {slotIndex}!");
                return false;
            }

            // Replace card
            starterDraftedCards[slotIndex] = newCard;
            starterSlotMulliganed[slotIndex] = true;

            Debug.Log($"[DraftManager] Mulliganed slot {slotIndex}: {newCard.cardName}");

            // Notify UI to update
            OnStarterDraftOffered?.Invoke(starterDraftedCards.ToArray());

            return true;
        }

        // ==========================================
        // MID-GAME DRAFT (1 of 3 cards)
        // ==========================================

        private IEnumerator MidGameDraftCoroutine()
        {
            isDrafting = true;

            Debug.Log("[DraftManager] === MID-GAME DRAFT START ===");

            // Master Client generates rarity combination
            CardRarity[] rarityCombination = null;

            if (PhotonNetwork.IsMasterClient)
            {
                rarityCombination = GenerateRandomRarityCombination(midGameChoices);

                // Send to all players
                photonView.RPC("RPC_ReceiveRarityCombination", RpcTarget.AllBuffered, rarityCombination);
            }
            else
            {
                // Wait for Master Client to send combo (with timeout)
                float timeout = 5f;
                while (rarityCombination == null && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }

                if (rarityCombination == null)
                {
                    Debug.LogError("[DraftManager] Timeout waiting for rarity combination!");
                    isDrafting = false;
                    yield break;
                }
            }

            // Draw cards from deck based on combo
            currentDraftChoices = DrawCardsFromDeck(rarityCombination);

            if (currentDraftChoices == null || currentDraftChoices.Length == 0)
            {
                Debug.LogError("[DraftManager] Failed to draw mid-game cards!");
                isDrafting = false;
                yield break;
            }

            // Show UI
            OnMidGameDraftOffered?.Invoke(currentDraftChoices);

            // Wait for player to choose or timeout
            float timeRemaining = midGameDraftTime;
            CardData chosenCard = null;

            while (timeRemaining > 0f && chosenCard == null)
            {
                OnDraftTimerUpdate?.Invoke(timeRemaining);

                // TODO: Check if player clicked a card
                // chosenCard = CheckPlayerChoice();

                timeRemaining -= Time.deltaTime;
                yield return null;
            }

            // Timeout - auto-select random card
            if (chosenCard == null)
            {
                chosenCard = currentDraftChoices[Random.Range(0, currentDraftChoices.Length)];
                Debug.Log($"[DraftManager] TIMEOUT - auto-selected {chosenCard.cardName}");
                OnDraftTimeout?.Invoke();
            }

            // Activate chosen card
            ActivateCard(chosenCard);

            isDrafting = false;

            Debug.Log($"[DraftManager] Mid-game draft complete! Chose: {chosenCard.cardName}");
        }

        /// <summary>
        /// Player selects a card from mid-game draft
        /// </summary>
        public void SelectMidGameCard(int choiceIndex)
        {
            if (!isDrafting)
            {
                Debug.LogWarning("[DraftManager] Not currently drafting!");
                return;
            }

            if (currentDraftChoices == null || choiceIndex < 0 || choiceIndex >= currentDraftChoices.Length)
            {
                Debug.LogError($"[DraftManager] Invalid choice index: {choiceIndex}");
                return;
            }

            CardData chosenCard = currentDraftChoices[choiceIndex];

            if (chosenCard == null)
            {
                Debug.LogError("[DraftManager] Chosen card is null!");
                return;
            }

            // Activate immediately
            ActivateCard(chosenCard);

            // End draft
            isDrafting = false;
            currentDraftChoices = null;

            Debug.Log($"[DraftManager] Player selected: {chosenCard.cardName}");
        }

        // ==========================================
        // CARD DRAWING FROM DECK
        // ==========================================

        /// <summary>
        /// Draws random card of specific rarity from player's deck
        /// </summary>
        private CardData DrawRandomCardFromDeck(CardRarity targetRarity)
        {
            if (playerDeck == null || playerDeck.cards.Count == 0)
            {
                Debug.LogError("[DraftManager] Deck is empty!");
                return null;
            }

            // Get all cards of target rarity
            List<CardData> validCards = playerDeck.cards
                .Where(card => card != null && card.rarity == targetRarity)
                .ToList();

            if (validCards.Count == 0)
            {
                Debug.LogWarning($"[DraftManager] No {targetRarity} cards in deck!");
                return null;
            }

            // Random selection
            return validCards[Random.Range(0, validCards.Count)];
        }

        /// <summary>
        /// Draws multiple cards based on rarity array
        /// </summary>
        private CardData[] DrawCardsFromDeck(CardRarity[] rarities)
        {
            CardData[] cards = new CardData[rarities.Length];

            for (int i = 0; i < rarities.Length; i++)
            {
                cards[i] = DrawRandomCardFromDeck(rarities[i]);
            }

            return cards;
        }

        /// <summary>
        /// Generates random rarity combination for mid-game draft
        /// </summary>
        private CardRarity[] GenerateRandomRarityCombination(int count)
        {
            CardRarity[] combo = new CardRarity[count];

            for (int i = 0; i < count; i++)
            {
                combo[i] = GetRandomRarity();
            }

            return combo;
        }

        private CardRarity GetRandomRarity()
        {
            float rand = Random.value;

            if (rand < 0.05f) return CardRarity.Legendary; // 5%
            if (rand < 0.30f) return CardRarity.Rare;      // 25%
            return CardRarity.Common;                       // 70%
        }

        // ==========================================
        // CARD ACTIVATION
        // ==========================================

        /// <summary>
        /// Activates drafted card (adds to PlayerCardManager)
        /// </summary>
        private void ActivateCard(CardData card)
        {
            if (card == null) return;

            playerCardManager?.ActivateCard(card);
            OnCardDrafted?.Invoke(card);

            Debug.Log($"[DraftManager] Activated card: {card.cardName}");
        }

        // ==========================================
        // PHOTON RPC
        // ==========================================

        [PunRPC]
        private void RPC_ReceiveRarityCombination(CardRarity[] combination)
        {
            Debug.Log($"[DraftManager] Received rarity combo: [{string.Join(", ", combination)}]");
            // Combo is used in MidGameDraftCoroutine
        }

        // ==========================================
        // UTILITY
        // ==========================================

        /// <summary>
        /// Sets player's deck (called from lobby/menu)
        /// </summary>
        public void SetDeck(DeckData deck)
        {
            playerDeck = deck;
            Debug.Log($"[DraftManager] Deck set: {deck.deckName} ({deck.cards.Count} cards)");
        }

        public bool IsDrafting => isDrafting;
        public bool IsStarterDraftComplete => isStarterDraftComplete;

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Test Starter Draft")]
        private void TestStarterDraft()
        {
            StartStarterDraft();
        }

        [ContextMenu("Test Mid-Game Draft")]
        private void TestMidGameDraft()
        {
            StartMidGameDraft();
        }
    }
}