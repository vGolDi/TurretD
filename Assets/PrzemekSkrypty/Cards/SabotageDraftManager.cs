using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;

namespace ElementumDefense.Cards
{
    public class SabotageDraftManager : MonoBehaviourPunCallbacks
    {
        public static SabotageDraftManager Instance { get; private set; }

        [Header("Timing Configuration")]
        [SerializeField] private int wavesBetweenSabotages = 5;
        [SerializeField] private float sabotageChoiceTime = 20f;
        [SerializeField] private float revealDuration = 5f;

        [Header("Draft Configuration")]
        [SerializeField] private int sabotageChoices = 3;

        // References
        private PlayerCardManager playerCardManager; // ← Będzie szukany dynamicznie
        private SabotagePool sabotagePool;
        private PhotonView photonView;

        // State
        private bool isDrafting = false;
        private int nextSabotageWave;
        private SabotageCardData[] currentOfferedCards;
        private SabotageCardData selectedSabotage;
        private bool sabotageSelected = false;

        // RPC rarity sharing
        private CardRarity[] receivedSabotageRarities = null;
        private bool sabotageRaritiesReceived = false;

        // Multiplayer sync
        private Dictionary<int, SabotageCardData> playerSelections =
            new Dictionary<int, SabotageCardData>();

        // Events
        public System.Action<SabotageCardData[]> OnSabotageOffered;
        public System.Action<float> OnDraftTimerUpdate;
        public System.Action OnDraftTimeout;
        public System.Action<Dictionary<int, SabotageCardData>> OnRevealPhaseStart;
        public System.Action OnRevealPhaseEnd;
        public System.Action<SabotageCardData, PhotonView> OnSabotageApplied;
        public System.Action OnSabotageDraftComplete;

        private void Awake()
        {
            photonView = GetComponent<PhotonView>();

            if (photonView == null)
            {
                Debug.LogError("[SabotageDraftManager] PhotonView not found!");
            }

            // Only claim Singleton for the LOCAL player's manager.
            // Remote players' managers must NOT be destroyed — 
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

            // ========== NAPRAWIONE: NIE szukamy PlayerCardManager tutaj ==========
            // Będzie znaleziony dynamicznie gdy potrzebny
            // ====================================================================
        }

        public override void OnDisable()
        {
            base.OnDisable();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            sabotagePool = SabotagePool.Instance;

            if (sabotagePool == null)
            {
                sabotagePool = FindObjectOfType<SabotagePool>();
            }

            if (sabotagePool == null)
            {
                Debug.LogError("[SabotageDraftManager] SabotagePool not found!");
            }

            nextSabotageWave = wavesBetweenSabotages;
            Debug.Log($"[SabotageDraftManager] Next sabotage at wave {nextSabotageWave}");
        }

        // ==========================================
        // NOWE: Find local PlayerCardManager
        // ==========================================

        /// <summary>
        /// Finds the LOCAL player's PlayerCardManager.
        /// Called lazily because player object may not exist at Awake time.
        /// </summary>
        private PlayerCardManager FindLocalPlayerCardManager()
        {
            if (playerCardManager != null) return playerCardManager;

            // Find all PlayerCardManagers and return the local one
            PlayerCardManager[] managers =
                FindObjectsByType<PlayerCardManager>(FindObjectsSortMode.None);

            foreach (var mgr in managers)
            {
                PhotonView pv = mgr.GetComponent<PhotonView>();
                if (pv != null && pv.IsMine)
                {
                    playerCardManager = mgr;
                    Debug.Log("[SabotageDraftManager] ✅ Found local PlayerCardManager");
                    return playerCardManager;
                }
            }

            Debug.LogWarning("[SabotageDraftManager] Local PlayerCardManager not found!");
            return null;
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        public void CheckSabotageDraft(int currentWave)
        {
            Debug.Log($"[SabotageDraftManager] CheckSabotageDraft: " +
                      $"Wave={currentWave}, next={nextSabotageWave}, " +
                      $"isDrafting={isDrafting}");

            if (isDrafting)
            {
                Debug.LogWarning("[SabotageDraftManager] Already drafting!");
                return;
            }

            if (currentWave >= nextSabotageWave)
            {
                nextSabotageWave = currentWave + wavesBetweenSabotages;
                Debug.Log($"[SabotageDraftManager] Triggering! Next at wave {nextSabotageWave}");
                StartSabotageDraft();
            }
        }

        public void StartSabotageDraft()
        {
            if (isDrafting) return;

            if (sabotagePool == null)
            {
                sabotagePool = SabotagePool.Instance;
                if (sabotagePool == null)
                    sabotagePool = FindObjectOfType<SabotagePool>();
            }

            if (sabotagePool == null)
            {
                Debug.LogError("[SabotageDraftManager] SabotagePool null! Skipping.");
                return;
            }

            StartCoroutine(SabotageDraftCoroutine());
        }

        // ==========================================
        // DRAFT FLOW
        // ==========================================

        private IEnumerator SabotageDraftCoroutine()
        {
            isDrafting = true;
            playerSelections.Clear();
            selectedSabotage = null;
            sabotageSelected = false;

            // ========== Ensure we have PlayerCardManager ==========
            FindLocalPlayerCardManager();
            // =====================================================

            Debug.Log("[SabotageDraftManager] === SABOTAGE DRAFT START ===");

            // PHASE 1: Rarity generation
            CardRarity[] rarityCombination = null;

            if (PhotonNetwork.IsMasterClient)
            {
                rarityCombination = sabotagePool.GenerateRarityCombination();

                if (rarityCombination == null)
                {
                    Debug.LogError("[SabotageDraftManager] Failed to generate rarities!");
                    isDrafting = false;
                    yield break;
                }

                int[] rarityInts = rarityCombination.Select(r => (int)r).ToArray();
                photonView.RPC("RPC_ReceiveSabotageRarities",
                    RpcTarget.AllBuffered, rarityInts);
            }
            else
            {
                sabotageRaritiesReceived = false;
                float timeout = 5f;

                while (!sabotageRaritiesReceived && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }

                if (!sabotageRaritiesReceived)
                {
                    // Catch-up case (reconnect): the master already passed this
                    // wave's draft and won't broadcast rarities. Generate locally
                    // and continue — the opponent isn't viewing our draft anyway.
                    Debug.LogWarning("[SabotageDraftManager] No rarities from master " +
                                     "(catch-up after reconnect?) — generating locally.");
                    rarityCombination = sabotagePool.GenerateRarityCombinationLocal();
                    if (rarityCombination == null)
                    {
                        Debug.LogError("[SabotageDraftManager] Local rarity generation failed!");
                        isDrafting = false;
                        yield break;
                    }
                }
                else
                {
                    rarityCombination = receivedSabotageRarities;
                }
            }

            // PHASE 2: Draw cards
            currentOfferedCards = sabotagePool.DrawSabotageCards(rarityCombination);

            if (currentOfferedCards == null || currentOfferedCards.Length == 0)
            {
                Debug.LogError("[SabotageDraftManager] Failed to draw cards!");
                isDrafting = false;
                yield break;
            }

            for (int i = 0; i < currentOfferedCards.Length; i++)
            {
                string name = currentOfferedCards[i]?.sabotageName ?? "NULL";
                string effect = currentOfferedCards[i]?.sabotageEffect != null
                    ? "✅" : "❌ NO EFFECT";
                Debug.Log($"[SabotageDraftManager] Choice {i}: {name} {effect}");
            }

            OnSabotageOffered?.Invoke(currentOfferedCards);

            // PHASE 3: Wait for selection
            float timeRemaining = sabotageChoiceTime;

            while (timeRemaining > 0f && !sabotageSelected)
            {
                OnDraftTimerUpdate?.Invoke(timeRemaining);
                timeRemaining -= Time.deltaTime;
                yield return null;
            }

            if (!sabotageSelected)
            {
                SabotageCardData autoCard =
                    currentOfferedCards.FirstOrDefault(c => c != null);
                if (autoCard == null) autoCard = currentOfferedCards[0];

                selectedSabotage = autoCard;
                sabotageSelected = true;

                Debug.Log($"[SabotageDraftManager] TIMEOUT → {selectedSabotage?.sabotageName}");
                OnDraftTimeout?.Invoke();
            }

            // PHASE 4: Send selection
            if (selectedSabotage != null)
            {
                int myActor = PhotonNetwork.LocalPlayer.ActorNumber;
                photonView.RPC("RPC_PlayerSelectedSabotage",
                    RpcTarget.AllBuffered,
                    myActor,
                    selectedSabotage.name);
            }

            yield return StartCoroutine(WaitForAllPlayersToSelect());

            // PHASE 5: Reveal
            Debug.Log("[SabotageDraftManager] === REVEAL PHASE ===");
            OnRevealPhaseStart?.Invoke(playerSelections);

            foreach (var kvp in playerSelections)
            {
                Player player = PhotonNetwork.CurrentRoom.GetPlayer(kvp.Key);
                string pName = player?.NickName ?? $"Player{kvp.Key}";
                string cName = kvp.Value?.sabotageName ?? "NULL";
                Debug.Log($"[SabotageDraftManager] {pName} → {cName}");
            }

            yield return new WaitForSeconds(revealDuration);

            OnRevealPhaseEnd?.Invoke();

            // PHASE 6: Apply
            ApplySabotages();

            // Cleanup
            isDrafting = false;
            currentOfferedCards = null;
            selectedSabotage = null;
            sabotageSelected = false;

            OnSabotageDraftComplete?.Invoke();

            Debug.Log("[SabotageDraftManager] === SABOTAGE DRAFT COMPLETE ===");
        }

        private IEnumerator WaitForAllPlayersToSelect()
        {
            float timeout = 10f;
            int expected = PhotonNetwork.CurrentRoom.PlayerCount;

            while (playerSelections.Count < expected && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Debug.Log($"[SabotageDraftManager] Selections: " +
                      $"{playerSelections.Count}/{expected}" +
                      (timeout <= 0 ? " (TIMEOUT)" : ""));
        }

        // ==========================================
        // SELECTION
        // ==========================================

        public void SelectSabotage(int choiceIndex)
        {
            if (!isDrafting || sabotageSelected) return;

            if (currentOfferedCards == null ||
                choiceIndex < 0 ||
                choiceIndex >= currentOfferedCards.Length)
            {
                Debug.LogError($"[SabotageDraftManager] Invalid choice: {choiceIndex}");
                return;
            }

            selectedSabotage = currentOfferedCards[choiceIndex];
            sabotageSelected = true;

            // Reconnect save point (b): a network-visible sabotage choice was committed.
            ElementumDefense.Multiplayer.Reconnect.MatchSnapshotService.Instance?.CaptureAndSave($"sabotage-selected: {selectedSabotage.sabotageName}");

            Debug.Log($"[SabotageDraftManager] ✅ Selected: " +
                      $"{selectedSabotage.sabotageName}");
        }

        // ==========================================
        // APPLICATION (NAPRAWIONE)
        // ==========================================

        private void ApplySabotages()
        {
            // ========== Ensure we have local PlayerCardManager ==========
            PlayerCardManager localCardManager = FindLocalPlayerCardManager();

            if (localCardManager == null)
            {
                Debug.LogError("[SabotageDraftManager] Cannot apply sabotages - " +
                               "no local PlayerCardManager!");
                return;
            }
            // ============================================================

            int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

            // ==========================================
            // SELF-SABOTAGE: Apply my own selection to myself (if Self type)
            // ==========================================
            if (playerSelections.TryGetValue(myActorNumber, out SabotageCardData mySabotage))
            {
                if (mySabotage != null && mySabotage.IsSelfSabotage)
                {
                    SelfSabotageTracker tracker = localCardManager.GetComponent<SelfSabotageTracker>();
                    if (tracker == null)
                        tracker = SelfSabotageTracker.Instance;

                    if (tracker != null)
                    {
                        tracker.StartChallenge(mySabotage);
                        OnSabotageApplied?.Invoke(mySabotage, photonView);
                        Debug.Log($"[SabotageDraftManager] SELF-SABOTAGE: " +
                                  $"'{mySabotage.sabotageName}' applied to SELF!");
                    }
                    else
                    {
                        Debug.LogError("[SabotageDraftManager] SelfSabotageTracker not found! " +
                                       "Add it to the player object.");
                    }
                }
            }

            // ==========================================
            // OPPONENT SABOTAGES: Apply everyone else's selection to me
            // ==========================================
            foreach (var kvp in playerSelections)
            {
                int casterActorNumber = kvp.Key;
                SabotageCardData sabotage = kvp.Value;

                // Skip my own sabotage
                if (casterActorNumber == myActorNumber)
                    continue;

                if (sabotage == null)
                {
                    Debug.LogWarning($"[SabotageDraftManager] Null sabotage " +
                                     $"from player {casterActorNumber}");
                    continue;
                }

                // Skip if the opponent chose a self-sabotage (it only affects them, not me)
                if (sabotage.IsSelfSabotage)
                {
                    Debug.Log($"[SabotageDraftManager] Player {casterActorNumber} " +
                              $"chose self-sabotage '{sabotage.sabotageName}' — skipping (doesn't affect us).");
                    continue;
                }

                if (sabotage.sabotageEffect == null)
                {
                    Debug.LogError($"[SabotageDraftManager] Sabotage " +
                                   $"'{sabotage.sabotageName}' has NO EFFECT assigned!");
                    continue;
                }

                // Find caster's PhotonView
                PhotonView casterView = FindPlayerPhotonView(casterActorNumber);

                if (casterView == null)
                {
                    Debug.LogError($"[SabotageDraftManager] PhotonView not found " +
                                   $"for player {casterActorNumber}!");
                    continue;
                }

                localCardManager.ApplySabotage(sabotage, casterView);
                OnSabotageApplied?.Invoke(sabotage, casterView);

                string casterName = casterView.Owner?.NickName ?? "Unknown";
                Debug.Log($"[SabotageDraftManager] Applied " +
                          $"'{sabotage.sabotageName}' from {casterName}");
            }

            Debug.Log($"[SabotageDraftManager] All sabotages applied. " +
                      $"Active count: {localCardManager.GetActiveSabotages().Count}");
        }

        private PhotonView FindPlayerPhotonView(int actorNumber)
        {
            PhotonView[] allViews =
                FindObjectsByType<PhotonView>(FindObjectsSortMode.None);

            foreach (PhotonView pv in allViews)
            {
                if (pv.Owner != null &&
                    pv.Owner.ActorNumber == actorNumber &&
                    pv.GetComponent<PlayerCardManager>() != null)
                {
                    return pv;
                }
            }

            return null;
        }

        // ==========================================
        // RPC
        // ==========================================

        [PunRPC]
        private void RPC_ReceiveSabotageRarities(int[] rarityInts)
        {
            CardRarity[] rarities = rarityInts
                .Select(i => (CardRarity)i).ToArray();

            // Forward to the LOCAL Instance
            SabotageDraftManager target = Instance ?? this;
            target.receivedSabotageRarities = rarities;
            target.sabotageRaritiesReceived = true;

            Debug.Log($"[SabotageDraftManager] RPC received rarities " +
                      $"(forwarded to {(target == this ? "self" : "Instance")})");
        }

        [PunRPC]
        private void RPC_PlayerSelectedSabotage(int actorNumber, string sabotageName)
        {
            SabotageCardData sabotage = FindSabotageByName(sabotageName);

            if (sabotage == null)
            {
                Debug.LogError($"[SabotageDraftManager] '{sabotageName}' not found!");
                return;
            }

            // Forward to the LOCAL Instance
            SabotageDraftManager target = Instance ?? this;
            target.playerSelections[actorNumber] = sabotage;

            Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            string pName = player?.NickName ?? $"Player{actorNumber}";
            Debug.Log($"[SabotageDraftManager] RPC: {pName} → {sabotage.sabotageName}" +
                      $" (forwarded to {(target == this ? "self" : "Instance")})");
        }

        private SabotageCardData FindSabotageByName(string name)
        {
            if (sabotagePool != null)
            {
                SabotageCardData fromPool = sabotagePool.FindByName(name);
                if (fromPool != null) return fromPool;
            }

            // Direct path first (fast), then recursive search (sabotages live in
            // subfolders like Cards/Sabotages/Generated/ForcePick/...).
            SabotageCardData sabotage =
                Resources.Load<SabotageCardData>($"Cards/Sabotages/{name}");

            if (sabotage == null)
            {
                foreach (var s in Resources.LoadAll<SabotageCardData>("Cards"))
                {
                    if (s != null && s.name == name) { sabotage = s; break; }
                }
            }

            if (sabotage == null)
                Debug.LogWarning($"[SabotageDraftManager] '{name}' not found!");

            return sabotage;
        }

        // ==========================================
        // UTILITY
        // ==========================================

        public bool IsDrafting => isDrafting;
        public int GetNextSabotageWave() => nextSabotageWave;

        // ==========================================
        // RECONNECT — CAPTURE / RESTORE
        // ==========================================

        /// <summary>Reconnect: snapshot sabotage-draft state into the draft snapshot.</summary>
        public void CaptureInto(ElementumDefense.Multiplayer.Reconnect.DraftStateSnapshot snap)
        {
            if (snap == null) return;
            snap.sabotageSelected = sabotageSelected;
            snap.selectedSabotageName = selectedSabotage != null ? selectedSabotage.name : "";
            snap.nextSabotageWave = nextSabotageWave;
        }

        /// <summary>
        /// Reconnect: restore sabotage-draft flags so a re-delivered AllBuffered
        /// selection RPC does not cause a second pick to be offered/sent.
        /// </summary>
        public void RestoreFrom(ElementumDefense.Multiplayer.Reconnect.DraftStateSnapshot snap)
        {
            if (snap == null) return;
            isDrafting = false;
            sabotageSelected = snap.sabotageSelected;
            selectedSabotage = string.IsNullOrEmpty(snap.selectedSabotageName)
                ? null
                : FindSabotageByName(snap.selectedSabotageName);
            // Restore the sabotage cadence (NOT the card draft's nextDraftWave).
            if (snap.nextSabotageWave > 0) nextSabotageWave = snap.nextSabotageWave;

            Debug.Log($"[SabotageDraftManager] Restored: sabotageSelected={sabotageSelected}, " +
                      $"next={nextSabotageWave}");
        }

        public Dictionary<int, SabotageCardData> GetPlayerSelections()
        {
            return new Dictionary<int, SabotageCardData>(playerSelections);
        }

        [ContextMenu("Debug State")]
        private void DebugState()
        {
            Debug.Log($"[SabotageDraftManager] isDrafting={isDrafting}, " +
                      $"next={nextSabotageWave}, selected={sabotageSelected}, " +
                      $"pool={sabotagePool != null}, " +
                      $"cardMgr={playerCardManager != null}");
        }

        [ContextMenu("Test Sabotage Draft")]
        private void TestSabotageDraft() => StartSabotageDraft();
    }
}