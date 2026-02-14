using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

namespace ElementumDefense.Cards
{
    public class DraftManager : MonoBehaviourPunCallbacks
    {
        public static DraftManager Instance { get; private set; }

        [Header("Deck Configuration")]
        [SerializeField] private DeckData playerDeck;

        [Header("Draft Timing")]
        [SerializeField] private float starterDraftTime = 60f;
        [SerializeField] private float midGameDraftTime = 30f;
        [SerializeField] private int wavesBetweenDrafts = 5;

        [Header("Starter Draft Configuration")]
        [SerializeField]
        private CardRarity[] starterRaritySlots = new CardRarity[]
        {
            CardRarity.Legendary,
            CardRarity.Rare,
            CardRarity.Rare,
            CardRarity.Common,
            CardRarity.Common
        };

        [Header("Mid-Game Draft Configuration")]
        [SerializeField] private int midGameChoices = 3;
        

        // References
        private PlayerCardManager playerCardManager;

        // Draft state
        private bool isDrafting = false;
        private bool isStarterDraftComplete = false;
        private int nextDraftWave;

        // Starter draft state
        private List<CardData> starterDraftedCards = new List<CardData>();
        private Dictionary<int, bool> starterSlotMulliganed = new Dictionary<int, bool>();

        // Mid-game draft state
        private CardData[] currentDraftChoices;
        private Dictionary<int, bool> midGameSlotMulliganed = new Dictionary<int, bool>();

        private bool midGameCardSelected = false; // ← NOWE: flaga czy gracz wybrał kartę

        // ========== NOWE: RPC rarity sharing ==========
        private CardRarity[] receivedRarityCombination = null;
        private bool rarityReceived = false;
        // ===============================================

        // Events
        public System.Action<CardData[]> OnStarterDraftOffered;
        public System.Action<CardData[]> OnMidGameDraftOffered;
        public System.Action<CardData> OnCardDrafted;
        public System.Action OnDraftTimeout;
        public System.Action<float> OnDraftTimerUpdate;     
        public System.Action<int, CardData> OnMidGameCardMulliganed;

        private bool waitingForConfirmation = false;
        public bool WaitingForConfirmation => waitingForConfirmation;
        private bool localDraftComplete = false;
        private const string IS_READY_KEY = "isReadyForWaves";
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
                Debug.LogError("[DraftManager] PhotonView not found!");
            }

            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;

            playerCardManager = GetComponent<PlayerCardManager>();

            for (int i = 0; i < starterRaritySlots.Length; i++)
            {
                starterSlotMulliganed[i] = false;
            }

            // ========== NOWE: Poprawna inicjalizacja nextDraftWave ==========
            nextDraftWave = wavesBetweenDrafts;
            Debug.Log($"[DraftManager] Next mid-game draft at wave {nextDraftWave}");
            // ================================================================
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        public void StartStarterDraft()
        {
            if (isStarterDraftComplete)
            {
                Debug.LogWarning("[DraftManager] Starter draft already completed!");
                return;
            }

            if (playerDeck == null)
            {
                Debug.LogWarning("[DraftManager] No deck assigned. Trying default...");
                TryLoadTestDeck();

                if (playerDeck == null)
                {
                    Debug.LogError("[DraftManager] FATAL: No deck available!");
                    isDrafting = false;
                    isStarterDraftComplete = true;
                    return;
                }
            }

            StartCoroutine(StarterDraftCoroutine());
        }

        public void TryLoadTestDeck()
        {
            if (playerDeck != null) return;

            DeckData[] decks = Resources.LoadAll<DeckData>("Decks");
            if (decks.Length > 0)
            {
                playerDeck = decks[0];
                Debug.Log($"[DraftManager] Loaded default deck: {playerDeck.deckName}");
            }
            else
            {
                Debug.LogError("[DraftManager] No decks in Resources/Decks/!");
            }
        }

        public DeckData GetPlayerDeck() => playerDeck;

        /// <summary>
        /// Called by WaveManager after each wave to check if draft should trigger
        /// </summary>
        public void CheckMidGameDraft(int currentWave)
        {
            Debug.Log($"[DraftManager] CheckMidGameDraft called. Wave={currentWave}, " +
                      $"nextDraftWave={nextDraftWave}, " +
                      $"isStarterComplete={isStarterDraftComplete}, " +
                      $"isDrafting={isDrafting}");

            if (!isStarterDraftComplete)
            {
                Debug.LogWarning("[DraftManager] Cannot mid-game draft before starter!");
                return;
            }

            if (isDrafting)
            {
                Debug.LogWarning("[DraftManager] Already drafting!");
                return;
            }

            if (currentWave >= nextDraftWave)
            {
                nextDraftWave = currentWave + wavesBetweenDrafts;
                Debug.Log($"[DraftManager] Triggering mid-game draft! Next at wave {nextDraftWave}");
                StartMidGameDraft();
            }
            else
            {
                Debug.Log($"[DraftManager] No draft yet. Next at wave {nextDraftWave}");
            }
        }

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
        // STARTER DRAFT
        // ==========================================

        private IEnumerator StarterDraftCoroutine()
        {
            isDrafting = true;
            waitingForConfirmation = true;
            localDraftComplete = false;

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

            if (waitingForConfirmation)
            {
                Debug.Log("[DraftManager] Starter draft TIMEOUT - auto-confirming");
                OnDraftTimeout?.Invoke();
                ConfirmStarterDraft();
            }

            Debug.Log("[DraftManager] Local card selection done. Waiting for others...");
        }

        public void ActivateStarterCards()
        {
            Debug.Log("[DraftManager] Activating starter cards...");

            foreach (CardData card in starterDraftedCards)
            {
                if (card != null)
                {
                    ActivateCard(card);
                }
            }

            isStarterDraftComplete = true;
            isDrafting = false; // ← Teraz mid-game draft może się uruchomić

            Debug.Log($"[DraftManager] Starter draft COMPLETE. " +
                      $"isStarterDraftComplete={isStarterDraftComplete}, " +
                      $"isDrafting={isDrafting}, " +
                      $"nextDraftWave={nextDraftWave}");
        }

        public void ConfirmStarterDraft()
        {
            if (!waitingForConfirmation)
            {
                Debug.LogWarning("[DraftManager] Not waiting for confirmation!");
                return;
            }

            waitingForConfirmation = false;
            localDraftComplete = true;

            Debug.Log("[DraftManager] Player confirmed starter draft!");

            var playerProps = new ExitGames.Client.Photon.Hashtable();
            playerProps[CARDS_CONFIRMED_KEY] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

            ShowWaitingMessage();
        }

        private CardData[] DrawStarterCards()
        {
            CardData[] cards = new CardData[5];
            starterDraftedCards.Clear(); // ← NOWE: Clear previous

            for (int i = 0; i < starterRaritySlots.Length; i++)
            {
                CardRarity targetRarity = starterRaritySlots[i];
                CardData card = DrawRandomCardFromDeck(targetRarity);

                if (card == null)
                {
                    Debug.LogWarning($"[DraftManager] Failed to draw {targetRarity} for slot {i}!");
                }

                cards[i] = card;
                starterDraftedCards.Add(card);
            }

            return cards;
        }

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

            CardRarity targetRarity = starterRaritySlots[slotIndex];
            CardData newCard = DrawRandomCardFromDeck(targetRarity);

            if (newCard == null)
            {
                Debug.LogError($"[DraftManager] Failed to mulligan slot {slotIndex}!");
                return false;
            }

            starterDraftedCards[slotIndex] = newCard;
            starterSlotMulliganed[slotIndex] = true;

            Debug.Log($"[DraftManager] Mulliganed slot {slotIndex}: {newCard.cardName}");

            OnStarterDraftOffered?.Invoke(starterDraftedCards.ToArray());

            return true;
        }

        // ==========================================
        // MID-GAME DRAFT (Z MULLIGAN)
        // ==========================================

        private IEnumerator MidGameDraftCoroutine()
        {
            isDrafting = true;
            midGameCardSelected = false;

            // ========== NOWE: Reset mulligan tracking ==========
            midGameSlotMulliganed.Clear();
            for (int i = 0; i < midGameChoices; i++)
            {
                midGameSlotMulliganed[i] = false;
            }
            // ===================================================

            Debug.Log("[DraftManager] === MID-GAME DRAFT START ===");

            // PHASE 1: Rarity generation
            CardRarity[] rarityCombination = null;

            if (PhotonNetwork.IsMasterClient)
            {
                rarityCombination = GenerateRandomRarityCombination(midGameChoices);

                int[] rarityInts = rarityCombination.Select(r => (int)r).ToArray();
                photonView.RPC("RPC_ReceiveRarityCombination", RpcTarget.AllBuffered, rarityInts);

                Debug.Log($"[DraftManager] Master generated rarities: " +
                          $"[{string.Join(", ", rarityCombination)}]");
            }
            else
            {
                rarityReceived = false;
                float timeout = 5f;

                while (!rarityReceived && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }

                if (!rarityReceived)
                {
                    Debug.LogError("[DraftManager] Timeout waiting for rarity combination!");
                    isDrafting = false;
                    yield break;
                }

                rarityCombination = receivedRarityCombination;
            }

            // PHASE 2: Draw cards (store rarities for mulligan)
            // ========== NOWE: Save rarity combo for mulligan rerolls ==========
            currentMidGameRarities = rarityCombination;
            // =================================================================

            currentDraftChoices = DrawCardsFromDeck(rarityCombination);

            if (currentDraftChoices == null || currentDraftChoices.Length == 0)
            {
                Debug.LogError("[DraftManager] Failed to draw mid-game cards!");
                isDrafting = false;
                yield break;
            }

            for (int i = 0; i < currentDraftChoices.Length; i++)
            {
                string name = currentDraftChoices[i] != null
                    ? currentDraftChoices[i].cardName
                    : "NULL";
                Debug.Log($"[DraftManager] Mid-game choice {i}: {name} ({rarityCombination[i]})");
            }

            // Show UI
            OnMidGameDraftOffered?.Invoke(currentDraftChoices);

            // PHASE 3: Wait for selection or timeout
            float timeRemaining = midGameDraftTime;

            while (timeRemaining > 0f && !midGameCardSelected)
            {
                OnDraftTimerUpdate?.Invoke(timeRemaining);
                timeRemaining -= Time.deltaTime;
                yield return null;
            }

            // Timeout - auto-select
            if (!midGameCardSelected)
            {
                CardData autoCard = currentDraftChoices.FirstOrDefault(c => c != null);
                if (autoCard == null) autoCard = currentDraftChoices[0];

                Debug.Log($"[DraftManager] TIMEOUT - auto-selected: " +
                          $"{(autoCard != null ? autoCard.cardName : "NULL")}");

                OnDraftTimeout?.Invoke();

                if (autoCard != null)
                {
                    ActivateCard(autoCard);
                }
            }

            // Cleanup
            currentDraftChoices = null;
            currentMidGameRarities = null;
            isDrafting = false;

            Debug.Log("[DraftManager] Mid-game draft COMPLETE.");
        }

        // ==========================================
        // NOWE: Mid-game rarity storage for mulligan
        // ==========================================

        private CardRarity[] currentMidGameRarities;

        // ==========================================
        // NOWE: Mid-game Mulligan (random rarity!)
        // ==========================================

        /// <summary>
        /// Mulligans a mid-game draft card.
        /// Unlike starter mulligan, the NEW card gets a RANDOM rarity
        /// (can go from Legendary → Common or Common → Legendary!)
        /// </summary>
        public bool MulliganMidGameCard(int slotIndex)
        {
            if (!isDrafting)
            {
                Debug.LogWarning("[DraftManager] Not currently drafting!");
                return false;
            }

            if (currentDraftChoices == null ||
                slotIndex < 0 ||
                slotIndex >= currentDraftChoices.Length)
            {
                Debug.LogError($"[DraftManager] Invalid mid-game slot index: {slotIndex}");
                return false;
            }

            if (midGameSlotMulliganed.ContainsKey(slotIndex) && midGameSlotMulliganed[slotIndex])
            {
                Debug.LogWarning($"[DraftManager] Mid-game slot {slotIndex} already mulliganed!");
                return false;
            }

            // ========== KLUCZOWE: Losowa nowa rzadkość! ==========
            CardRarity oldRarity = currentDraftChoices[slotIndex] != null
                ? currentDraftChoices[slotIndex].rarity
                : CardRarity.Common;

            CardRarity newRarity = GetRandomRarity(); // Totally random!
                                                      // =====================================================

            // Draw new card with random rarity
            CardData newCard = DrawRandomCardFromDeck(newRarity);

            if (newCard == null)
            {
                Debug.LogError($"[DraftManager] Failed to mulligan mid-game slot {slotIndex}!");
                return false;
            }

            // Get old card name for logging
            string oldCardName = currentDraftChoices[slotIndex]?.cardName ?? "NULL";

            // Replace card
            currentDraftChoices[slotIndex] = newCard;
            midGameSlotMulliganed[slotIndex] = true;

            // Update stored rarity (for display purposes)
            if (currentMidGameRarities != null && slotIndex < currentMidGameRarities.Length)
            {
                currentMidGameRarities[slotIndex] = newRarity;
            }

            Debug.Log($"[DraftManager] 🔄 Mid-game mulligan slot {slotIndex}: " +
                      $"{oldCardName} ({oldRarity}) → {newCard.cardName} ({newRarity})");

            // Notify UI to update single slot
            OnMidGameCardMulliganed?.Invoke(slotIndex, newCard);

            // Also fire full refresh for UI that listens to it
            OnMidGameDraftOffered?.Invoke(currentDraftChoices);

            return true;
        }

        /// <summary>
        /// Checks if mid-game slot can be mulliganed
        /// </summary>
        public bool CanMulliganMidGameSlot(int slotIndex)
        {
            if (!isDrafting) return false;
            if (currentDraftChoices == null) return false;
            if (slotIndex < 0 || slotIndex >= currentDraftChoices.Length) return false;

            return !midGameSlotMulliganed.ContainsKey(slotIndex) ||
                   !midGameSlotMulliganed[slotIndex];
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

            if (midGameCardSelected)
            {
                Debug.LogWarning("[DraftManager] Already selected a card!");
                return;
            }

            if (currentDraftChoices == null ||
                choiceIndex < 0 ||
                choiceIndex >= currentDraftChoices.Length)
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

            ActivateCard(chosenCard);
            midGameCardSelected = true;

            Debug.Log($"[DraftManager] ✅ Selected mid-game card: {chosenCard.cardName}");
        }

        // ==========================================
        // CARD DRAWING
        // ==========================================

        private CardData DrawRandomCardFromDeck(CardRarity targetRarity)
        {
            if (playerDeck == null || playerDeck.cards.Count == 0)
            {
                Debug.LogError("[DraftManager] Deck is empty!");
                return null;
            }

            List<CardData> validCards = playerDeck.cards
                .Where(card => card != null && card.rarity == targetRarity)
                .ToList();

            if (validCards.Count == 0)
            {
                Debug.LogWarning($"[DraftManager] No {targetRarity} cards in deck! " +
                                 $"Falling back to any card.");

                // ========== NOWE: Fallback - weź dowolną kartę ==========
                validCards = playerDeck.cards.Where(c => c != null).ToList();

                if (validCards.Count == 0)
                {
                    Debug.LogError("[DraftManager] Deck has no valid cards at all!");
                    return null;
                }
                // ========================================================
            }

            return validCards[Random.Range(0, validCards.Count)];
        }

        private CardData[] DrawCardsFromDeck(CardRarity[] rarities)
        {
            CardData[] cards = new CardData[rarities.Length];

            for (int i = 0; i < rarities.Length; i++)
            {
                cards[i] = DrawRandomCardFromDeck(rarities[i]);
            }

            return cards;
        }

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

            if (rand < 0.05f) return CardRarity.Legendary;
            if (rand < 0.30f) return CardRarity.Rare;
            return CardRarity.Common;
        }

        // ==========================================
        // CARD ACTIVATION
        // ==========================================

        private void ActivateCard(CardData card)
        {
            if (card == null) return;

            playerCardManager?.ActivateCard(card);
            OnCardDrafted?.Invoke(card);

            Debug.Log($"[DraftManager] Activated card: {card.cardName}");
        }

        // ==========================================
        // PHOTON RPC (NAPRAWIONE)
        // ==========================================

        [PunRPC]
        private void RPC_ReceiveRarityCombination(int[] rarityInts)
        {
            receivedRarityCombination = rarityInts
                .Select(i => (CardRarity)i)
                .ToArray();

            rarityReceived = true;

            Debug.Log($"[DraftManager] RPC received rarities: " +
                      $"[{string.Join(", ", receivedRarityCombination)}]");
        }

        // ==========================================
        // UPDATE & PHOTON CALLBACKS
        // ==========================================

        private void Update()
        {
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

            if (confirmedCount >= PhotonNetwork.CurrentRoom.PlayerCount)
            {
                Debug.Log("[DraftManager - Master] All confirmed! Sending signal.");

                var roomProps = new ExitGames.Client.Photon.Hashtable();
                roomProps[ALL_CARDS_READY_KEY] = true;
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

                // ← Prevent re-checking
                localDraftComplete = false;
            }
        }

        public override void OnRoomPropertiesUpdate(
            ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.ContainsKey(ALL_CARDS_READY_KEY) &&
                (bool)propertiesThatChanged[ALL_CARDS_READY_KEY])
            {
                Debug.Log("[DraftManager] All cards confirmed! Starting countdown.");
                StartFinalCountdown();
            }
        }

        private void StartFinalCountdown()
        {
            var playerProps = new ExitGames.Client.Photon.Hashtable();
            playerProps[CARDS_CONFIRMED_KEY] = false;
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

            GameStartCountdown countdown = Object.FindFirstObjectByType<GameStartCountdown>(); 
            if (countdown != null)
            {
                countdown.StartCountdown();
            }
            else
            {
                Debug.LogError("[DraftManager] GameStartCountdown not found!");
            }
        }

        private void ShowWaitingMessage()
        {
            GameStartCountdown countdown = Object.FindFirstObjectByType<GameStartCountdown>();
            if (countdown != null)
            {
                TextMeshProUGUI text = countdown.GetCountdownText();
                if (text != null)
                {
                    text.gameObject.SetActive(true);
                    text.text = "Waiting for other players...";
                }
            }   
        }

        // ==========================================
        // UTILITY
        // ==========================================

        public void SetDeck(DeckData deck)
        {
            playerDeck = deck;
            Debug.Log($"[DraftManager] Deck set: {deck.deckName} ({deck.cards.Count} cards)");
        }

        public bool IsDrafting => isDrafting;
        public bool IsStarterDraftComplete => isStarterDraftComplete;

        [ContextMenu("Test Starter Draft")]
        private void TestStarterDraft() => StartStarterDraft();

        [ContextMenu("Test Mid-Game Draft")]
        private void TestMidGameDraft() => StartMidGameDraft();

        [ContextMenu("Debug State")]
        private void DebugState()
        {
            Debug.Log($"[DraftManager DEBUG] isDrafting={isDrafting}, " +
                      $"isStarterDraftComplete={isStarterDraftComplete}, " +
                      $"nextDraftWave={nextDraftWave}, " +
                      $"waitingForConfirmation={waitingForConfirmation}, " +
                      $"localDraftComplete={localDraftComplete}");
        }
    }
}