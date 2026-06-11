using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ElementumDefense.Multiplayer;
using ElementumDefense.Waves;

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

        // Sabotage override — when set to a positive value, the next mid-game
        // draft uses this count instead of midGameChoices, then resets to 0.
        // ForcePickSabotage uses this to reduce the target's choices.
        private int nextDraftChoiceOverride = 0;
        public void SetNextDraftChoiceOverride(int count) => nextDraftChoiceOverride = count;

        // Active runtime state of mulligan disable for the CURRENT draft.
        // Set from nextDraftMulliganDisabled at draft start, cleared on draft end.
        private bool currentDraftMulliganDisabled = false;

        // Disables mulligan for ONE upcoming mid-game draft. Set by
        // NoMulliganSelfSabotage; consumed (cleared) when the next mid-game
        // draft starts. UI can read CanMulliganMidGameSlot which respects this.
        private bool nextDraftMulliganDisabled = false;
        public void SetNextDraftMulliganDisabled(bool disabled) => nextDraftMulliganDisabled = disabled;
        public bool IsNextDraftMulliganDisabled => nextDraftMulliganDisabled;
        

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
        private int currentDraftWaveIndex = 0;      // ← Tracks which wave triggered the draft (for sync keys)

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

            // Only claim Singleton for the LOCAL player's DraftManager.
            // Remote players' DraftManagers must NOT be destroyed — 
            // PUN delivers RPCs to them by PhotonView ID.
            if (photonView != null && photonView.IsMine)
            {
                if (Instance != null && Instance != this)
                {
                    Destroy(this);
                    return;
                }
                Instance = this;
            }

            playerCardManager = GetComponent<PlayerCardManager>();

            for (int i = 0; i < starterRaritySlots.Length; i++)
            {
                starterSlotMulliganed[i] = false;
            }

            // ========== NOWE: Poprawna inicjalizacja nextDraftWave ==========
            nextDraftWave = wavesBetweenDrafts;
            Debug.Log($"[DraftManager] Next mid-game draft at wave {nextDraftWave} (IsMine={photonView?.IsMine})");
            // ================================================================

            if (photonView != null && photonView.IsMine)
            {
                // Check if we are reconnecting to a game already in progress
                if (PhotonNetwork.CurrentRoom != null && 
                    PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(PreGameManager.ALL_DECKS_READY_KEY) &&
                    (bool)PhotonNetwork.CurrentRoom.CustomProperties[PreGameManager.ALL_DECKS_READY_KEY])
                {
                    // Auto-assign deck if null — REQUIRED even on restore, because
                    // mid-game drafts draw from playerDeck. (Bug fix: previously the
                    // restore guard returned before this, leaving the deck null and
                    // crashing the next mid-game draft with "Deck is empty!".)
                    if (playerDeck == null)
                    {
                        var decks = PlayerCollection.Instance?.GetPlayerDecks();
                        if (decks != null && decks.Count > 0)
                            SetDeck(decks[0]);
                        else
                        {
                            var resDecks = Resources.LoadAll<DeckData>("Decks");
                            if (resDecks.Length > 0) SetDeck(resDecks[0]);
                        }
                    }

                    // Reconnect WITH a state snapshot: MatchRestoreService re-activates
                    // the player's cards directly, so skip the starter-draft auto-start
                    // to avoid double-activating starter cards. The deck above is still
                    // assigned so future mid-game drafts work.
                    if (ElementumDefense.Multiplayer.Reconnect.MatchRestoreService.RestorePending)
                    {
                        Debug.Log("[DraftManager] Restore pending — deck assigned, skipping starter-draft auto-start.");
                        return;
                    }

                    Debug.Log("[DraftManager] Game is already in progress! Starting Starter Draft.");

                    // Start the starter draft
                    StartStarterDraft();
                }
            }
        }

        public override void OnDisable()
        {
            base.OnDisable();
            if (Instance == this)
            {
                Instance = null;
            }
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

        // ==========================================
        // RECONNECT — DRAFT STATE CAPTURE / RESTORE
        // ==========================================

        /// <summary>Reconnect: snapshot the draft-phase state into the given struct.</summary>
        public void CaptureDraftState(ElementumDefense.Multiplayer.Reconnect.DraftStateSnapshot snap)
        {
            if (snap == null) return;
            snap.isStarterDraftComplete = isStarterDraftComplete;
            snap.nextDraftWave = nextDraftWave;
            snap.currentDraftWaveIndex = currentDraftWaveIndex;
            snap.midGameCardSelected = midGameCardSelected;
            snap.nextDraftChoiceOverride = nextDraftChoiceOverride;
            snap.nextDraftMulliganDisabled = nextDraftMulliganDisabled;
            snap.currentDraftMulliganDisabled = currentDraftMulliganDisabled;
            snap.selectedDeckName = playerDeck != null ? playerDeck.name : "";

            snap.starterDraftedCardNames.Clear();
            foreach (var c in starterDraftedCards)
                snap.starterDraftedCardNames.Add(c != null ? c.name : "");
        }

        /// <summary>
        /// Reconnect: restore draft-phase flags so the draft system does not
        /// re-trigger or re-offer choices the player already resolved.
        /// Active cards themselves are restored separately by re-activation.
        /// </summary>
        public void RestoreDraftState(ElementumDefense.Multiplayer.Reconnect.DraftStateSnapshot snap)
        {
            if (snap == null) return;
            isStarterDraftComplete = snap.isStarterDraftComplete;
            isDrafting = false;
            waitingForConfirmation = false;
            nextDraftWave = snap.nextDraftWave;
            currentDraftWaveIndex = snap.currentDraftWaveIndex;
            midGameCardSelected = snap.midGameCardSelected;
            nextDraftChoiceOverride = snap.nextDraftChoiceOverride;
            nextDraftMulliganDisabled = snap.nextDraftMulliganDisabled;
            currentDraftMulliganDisabled = snap.currentDraftMulliganDisabled;

            // Restore the actual deck used this match (overrides the default that
            // Awake auto-assigned), so mid-game drafts draw from the correct pool.
            if (!string.IsNullOrEmpty(snap.selectedDeckName))
            {
                DeckData deck = ResolveDeckByName(snap.selectedDeckName);
                if (deck != null) SetDeck(deck);
                else Debug.LogWarning($"[DraftManager] Restore: deck '{snap.selectedDeckName}' not found — keeping current.");
            }

            starterDraftedCards.Clear();
            foreach (var name in snap.starterDraftedCardNames)
            {
                if (string.IsNullOrEmpty(name)) { starterDraftedCards.Add(null); continue; }
                CardData card = ResolveCardByName(name);
                starterDraftedCards.Add(card);
            }

            Debug.Log($"[DraftManager] Restored draft state: starterComplete={isStarterDraftComplete}, " +
                      $"nextDraftWave={nextDraftWave}, midGameSelected={midGameCardSelected}");
        }

        private CardData ResolveCardByName(string name)
        {
            if (playerDeck != null && playerDeck.cards != null)
            {
                foreach (var c in playerDeck.cards)
                    if (c != null && c.name == name) return c;
            }
            // Recursive search across Resources/Cards (cards live in subfolders).
            foreach (var c in Resources.LoadAll<CardData>("Cards"))
                if (c != null && c.name == name) return c;

            Debug.LogWarning($"[DraftManager] ResolveCardByName: '{name}' not found in deck or Resources/Cards.");
            return null;
        }

        private DeckData ResolveDeckByName(string name)
        {
            // Prefer the player's own decks (matches by asset name or display name).
            var decks = PlayerCollection.Instance?.GetPlayerDecks();
            if (decks != null)
            {
                foreach (var d in decks)
                    if (d != null && (d.name == name || d.deckName == name)) return d;
            }
            // Fallback to Resources/Decks.
            foreach (var d in Resources.LoadAll<DeckData>("Decks"))
                if (d != null && (d.name == name || d.deckName == name)) return d;
            return null;
        }


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
                currentDraftWaveIndex = currentWave;
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

            // Check if game is already running (Reconnecting scenario)
            if (PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey(ALL_CARDS_READY_KEY) &&
                (bool)PhotonNetwork.CurrentRoom.CustomProperties[ALL_CARDS_READY_KEY])
            {
                Debug.Log("[DraftManager] Room already started. Activating cards immediately for Reconnected player.");
                var hud = ElementumDefense.UI.WaveHUD.Instance;
                hud?.HideWaitingMessage();
                ActivateStarterCards();
                return;
            }

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

            // ========== Per-player choice count (ForcePick / NoMulligan) ==========
            // IMPORTANT: the choice COUNT is per-player. The shared rarity combo
            // (broadcast by master) is generated at the DEFAULT length; each player
            // then truncates it to their own effectiveChoices below. This stops a
            // ForcePick on one player from shrinking the OTHER player's draft.
            int effectiveChoices = midGameChoices;
            if (nextDraftChoiceOverride > 0)
            {
                effectiveChoices = nextDraftChoiceOverride;
                nextDraftChoiceOverride = 0;
                Debug.Log($"[DraftManager] ForcePick: THIS player gets {effectiveChoices} choices");
            }

            // Apply NoMulligan sabotage override if any (one-shot, then clear).
            currentDraftMulliganDisabled = nextDraftMulliganDisabled;
            if (currentDraftMulliganDisabled)
            {
                nextDraftMulliganDisabled = false;
                Debug.Log("[DraftManager] NoMulligan: this draft has no mulligan");
            }

            Debug.Log("[DraftManager] === MID-GAME DRAFT START ===");

            // PHASE 1: Rarity generation — master makes a FULL-length combo and
            // broadcasts it (so both players see the SAME rarities). Per-player
            // count is applied by truncation afterwards.
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
                    // Catch-up case (reconnect): master already passed this draft
                    // and won't broadcast rarities. Generate locally and continue —
                    // the opponent isn't viewing our draft anyway.
                    Debug.LogWarning("[DraftManager] No rarities from master " +
                                     "(catch-up after reconnect?) — generating locally.");
                    rarityCombination = GenerateRandomRarityCombination(midGameChoices);
                }
                else
                {
                    rarityCombination = receivedRarityCombination;
                }
            }

            // Per-player truncation: this player sees only `effectiveChoices` of the
            // shared combo. A ForcePick'd player gets the first N rarities; the other
            // player keeps the full set.
            if (rarityCombination != null && effectiveChoices < rarityCombination.Length)
            {
                rarityCombination = rarityCombination.Take(effectiveChoices).ToArray();
                Debug.Log($"[DraftManager] Truncated to {effectiveChoices} choices for this player.");
            }

            // Mulligan tracking based on the FINAL (possibly reduced) count.
            midGameSlotMulliganed.Clear();
            int finalCount = rarityCombination != null ? rarityCombination.Length : 0;
            for (int i = 0; i < finalCount; i++)
            {
                midGameSlotMulliganed[i] = false;
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

            // PHASE 4: Sync — wait for all players to finish drafting
            // This prevents one player from starting the next wave
            // while the other is still choosing a card.
            string draftDoneKey = $"mid_draft_{currentDraftWaveIndex}_done";

            var doneProps = new ExitGames.Client.Photon.Hashtable();
            doneProps[draftDoneKey] = true;
            PhotonNetwork.LocalPlayer.SetCustomProperties(doneProps);

            Debug.Log($"[DraftManager] Set {draftDoneKey}=true, waiting for others...");

            // Show waiting message
            var hud = ElementumDefense.UI.WaveHUD.Instance;
            hud?.ShowWaitingMessage("WAITING FOR OTHER PLAYER...");

            // Wait for ALL players to finish
            float syncTimeout = 60f;
            while (syncTimeout > 0f)
            {
                bool allDone = true;
                foreach (var player in PhotonNetwork.PlayerList)
                {
                    if (!player.CustomProperties.TryGetValue(draftDoneKey, out object val) ||
                        !(bool)val)
                    {
                        allDone = false;
                        break;
                    }
                }

                if (allDone) break;

                syncTimeout -= Time.deltaTime;
                yield return null;
            }

            hud?.HideWaitingMessage();

            if (syncTimeout <= 0f)
            {
                Debug.LogWarning("[DraftManager] Sync timeout — proceeding anyway.");
            }

            // Cleanup
            currentDraftChoices = null;
            currentMidGameRarities = null;
            currentDraftMulliganDisabled = false;
            isDrafting = false;

            Debug.Log("[DraftManager] Mid-game draft COMPLETE (synced).");
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

            if (currentDraftMulliganDisabled)
            {
                Debug.LogWarning("[DraftManager] Mulligan disabled this draft (NoMulligan sabotage)");
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

            // NoMulligan sabotage gate — also blocks mulligan during this draft.
            if (currentDraftMulliganDisabled) return false;

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

            // Reconnect save point (a): a network-visible card choice was committed.
            ElementumDefense.Multiplayer.Reconnect.MatchSnapshotService.Instance?.CaptureAndSave($"card-selected: {chosenCard.cardName}");

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
            CardRarity[] rarities = rarityInts
                .Select(i => (CardRarity)i)
                .ToArray();

            // RPC arrives on the SENDER's PhotonView copy.
            // Forward to the LOCAL Instance so the waiting
            // coroutine picks it up.
            DraftManager target = Instance ?? this;
            target.receivedRarityCombination = rarities;
            target.rarityReceived = true;

            Debug.Log($"[DraftManager] RPC received rarities: " +
                      $"[{string.Join(", ", rarities)}]" +
                      $" (forwarded to {(target == this ? "self" : "Instance")})" );
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
            // Show waiting message via WaveHUD
            var hud = ElementumDefense.UI.WaveHUD.Instance;
            if (hud != null)
            {
                // Use countdown overlay to show waiting
                hud.ShowWaitingMessage(
                    "WAITING FOR OTHER PLAYERS");
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