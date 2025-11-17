using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Manages sabotage card drafting system
    /// Triggers every X waves, shows 1 of 3 cards, 5s reveal before activation
    /// </summary>
    public class SabotageDraftManager : MonoBehaviourPunCallbacks
    {
        public static SabotageDraftManager Instance { get; private set; }

        [Header("Timing Configuration")]
        [SerializeField, Tooltip("Waves between sabotage drafts")]
        private int wavesBetweenSabotages = 5;

        [SerializeField, Tooltip("Time limit to choose sabotage (seconds)")]
        private float sabotageChoiceTime = 20f;

        [SerializeField, Tooltip("Reveal duration after selection (seconds)")]
        private float revealDuration = 5f;

        [Header("Draft Configuration")]
        [SerializeField, Tooltip("How many sabotage cards to choose from")]
        private int sabotageChoices = 3;

        [Header("References")]
        private PlayerCardManager playerCardManager;
        private SabotagePool sabotagePool;

        // State
        private bool isDrafting = false;
        private int nextSabotageWave = 0;
        private SabotageCardData[] currentOfferedCards;
        private SabotageCardData selectedSabotage;

        // Multiplayer sync
        private Dictionary<int, SabotageCardData> playerSelections = new Dictionary<int, SabotageCardData>(); // ActorNumber -> Selected card

        // Events
        public System.Action<SabotageCardData[]> OnSabotageOffered;
        public System.Action<float> OnDraftTimerUpdate;
        public System.Action OnDraftTimeout;
        public System.Action<Dictionary<int, SabotageCardData>> OnRevealPhaseStart; // Show what everyone picked
        public System.Action OnRevealPhaseEnd;
        public System.Action<SabotageCardData, PhotonView> OnSabotageApplied; 

        // ==========================================
        // INITIALIZATION
        // ==========================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this); //gameObject
                return;
            }

            Instance = this;

            playerCardManager = GetComponent<PlayerCardManager>();
            sabotagePool = SabotagePool.Instance;

            if (sabotagePool == null)
            {
                Debug.LogError("[SabotageDraftManager] SabotagePool not found!");
            }
        }

        private void Start()
        {
            // Set first sabotage wave
            nextSabotageWave = wavesBetweenSabotages;
        }

        // ==========================================
        // PUBLIC API - TRIGGER SABOTAGE DRAFT
        // ==========================================

        /// <summary>
        /// Checks if sabotage draft should trigger (called by WaveManager)
        /// </summary>
        /// <param name="currentWave">Current wave number</param>
        public void CheckSabotageDraft(int currentWave)
        {
            if (currentWave >= nextSabotageWave)
            {
                nextSabotageWave = currentWave + wavesBetweenSabotages;
                StartSabotageDraft();
            }
        }

        /// <summary>
        /// Manually start sabotage draft
        /// </summary>
        public void StartSabotageDraft()
        {
            if (isDrafting)
            {
                Debug.LogWarning("[SabotageDraftManager] Already drafting sabotage!");
                return;
            }

            if (sabotagePool == null)
            {
                Debug.LogError("[SabotageDraftManager] SabotagePool is null!");
                return;
            }

            StartCoroutine(SabotageDraftCoroutine());
        }

        // ==========================================
        // SABOTAGE DRAFT FLOW
        // ==========================================

        private IEnumerator SabotageDraftCoroutine()
        {
            isDrafting = true;
            playerSelections.Clear();
            selectedSabotage = null;

            Debug.Log("[SabotageDraftManager] === SABOTAGE DRAFT START ===");

            // ========== PHASE 1: Master Client generates rarity combo ==========
            CardRarity[] rarityCombination = null;

            if (PhotonNetwork.IsMasterClient)
            {
                rarityCombination = sabotagePool.GenerateRarityCombination();

                if (rarityCombination == null)
                {
                    Debug.LogError("[SabotageDraftManager] Failed to generate rarity combo!");
                    isDrafting = false;
                    yield break;
                }

                // Send to all players
                photonView.RPC("RPC_ReceiveSabotageRarities", RpcTarget.AllBuffered, (object)rarityCombination);
            }
            else
            {
                // Wait for Master Client (with timeout)
                float timeout = 5f;
                while (rarityCombination == null && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }

                if (rarityCombination == null)
                {
                    Debug.LogError("[SabotageDraftManager] Timeout waiting for rarity combo!");
                    isDrafting = false;
                    yield break;
                }
            }

            // ========== PHASE 2: Each player draws their own cards from pool ==========
            currentOfferedCards = sabotagePool.DrawSabotageCards(rarityCombination);

            if (currentOfferedCards == null || currentOfferedCards.Length == 0)
            {
                Debug.LogError("[SabotageDraftManager] Failed to draw sabotage cards!");
                isDrafting = false;
                yield break;
            }

            // Show UI
            OnSabotageOffered?.Invoke(currentOfferedCards);

            Debug.Log($"[SabotageDraftManager] Offered sabotages: {string.Join(", ", System.Array.ConvertAll(currentOfferedCards, c => c?.sabotageName ?? "NULL"))}");

            // ========== PHASE 3: Wait for player to choose (or timeout) ==========
            float timeRemaining = sabotageChoiceTime;

            while (timeRemaining > 0f && selectedSabotage == null)
            {
                OnDraftTimerUpdate?.Invoke(timeRemaining);

                // Player selection is handled by SelectSabotage() method
                timeRemaining -= Time.deltaTime;
                yield return null;
            }

            // Timeout - auto-select random
            if (selectedSabotage == null)
            {
                selectedSabotage = currentOfferedCards[Random.Range(0, currentOfferedCards.Length)];
                Debug.Log($"[SabotageDraftManager] TIMEOUT - auto-selected {selectedSabotage.sabotageName}");
                OnDraftTimeout?.Invoke();
            }

            // ========== PHASE 4: Send selection to all players ==========
            int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
            photonView.RPC("RPC_PlayerSelectedSabotage", RpcTarget.AllBuffered, myActorNumber, selectedSabotage.name);

            // Wait for all players to submit (or timeout)
            yield return StartCoroutine(WaitForAllPlayersToSelect());

            // ========== PHASE 5: REVEAL PHASE (5 seconds) ==========
            Debug.Log("[SabotageDraftManager] === REVEAL PHASE START ===");

            OnRevealPhaseStart?.Invoke(playerSelections);

            // Display what each player chose
            foreach (var kvp in playerSelections)
            {
                int actorNumber = kvp.Key;
                SabotageCardData card = kvp.Value;

                Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
                string playerName = player?.NickName ?? $"Player{actorNumber}";

                Debug.Log($"[SabotageDraftManager] {playerName} chose: {card.sabotageName}");
            }

            // Wait 5 seconds for reveal
            yield return new WaitForSeconds(revealDuration);

            OnRevealPhaseEnd?.Invoke();

            Debug.Log("[SabotageDraftManager] === REVEAL PHASE END ===");

            // ========== PHASE 6: Apply sabotages ==========
            ApplySabotages();

            isDrafting = false;
            currentOfferedCards = null;

            Debug.Log("[SabotageDraftManager] === SABOTAGE DRAFT COMPLETE ===");
        }

        /// <summary>
        /// Waits for all players to submit their selection (with timeout)
        /// </summary>
        private IEnumerator WaitForAllPlayersToSelect()
        {
            float timeout = 10f; // Max wait time
            int expectedPlayers = PhotonNetwork.CurrentRoom.PlayerCount;

            while (playerSelections.Count < expectedPlayers && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (playerSelections.Count < expectedPlayers)
            {
                Debug.LogWarning($"[SabotageDraftManager] Timeout waiting for all players ({playerSelections.Count}/{expectedPlayers})");
            }
            else
            {
                Debug.Log($"[SabotageDraftManager] All players selected ({playerSelections.Count}/{expectedPlayers})");
            }
        }

        // ==========================================
        // PLAYER SELECTION
        // ==========================================

        /// <summary>
        /// Player selects a sabotage card
        /// </summary>
        /// <param name="choiceIndex">Index in currentOfferedCards (0-2)</param>
        public void SelectSabotage(int choiceIndex)
        {
            if (!isDrafting)
            {
                Debug.LogWarning("[SabotageDraftManager] Not currently drafting!");
                return;
            }

            if (selectedSabotage != null)
            {
                Debug.LogWarning("[SabotageDraftManager] Already selected a sabotage!");
                return;
            }

            if (currentOfferedCards == null || choiceIndex < 0 || choiceIndex >= currentOfferedCards.Length)
            {
                Debug.LogError($"[SabotageDraftManager] Invalid choice index: {choiceIndex}");
                return;
            }

            selectedSabotage = currentOfferedCards[choiceIndex];

            Debug.Log($"[SabotageDraftManager] Selected sabotage: {selectedSabotage.sabotageName}");
        }

        // ==========================================
        // SABOTAGE APPLICATION
        // ==========================================

        /// <summary>
        /// Applies all player sabotages to their targets
        /// In 1v1: sabotage targets opponent
        /// In FFA: sabotage targets all other players
        /// </summary>
        private void ApplySabotages()
        {
            int myActorNumber = PhotonNetwork.LocalPlayer.ActorNumber;

            foreach (var kvp in playerSelections)
            {
                int casterActorNumber = kvp.Key;
                SabotageCardData sabotage = kvp.Value;

                // Skip if this is my own sabotage (I don't sabotage myself)
                if (casterActorNumber == myActorNumber)
                    continue;

                // Find caster's PhotonView
                PhotonView casterView = FindPlayerPhotonView(casterActorNumber);

                if (casterView == null)
                {
                    Debug.LogError($"[SabotageDraftManager] Could not find PhotonView for player {casterActorNumber}!");
                    continue;
                }

                // Apply sabotage to ME (I'm being sabotaged by caster)
                ApplySabotageToMe(sabotage, casterView);
            }

            Debug.Log("[SabotageDraftManager] All sabotages applied");
        }

        /// <summary>
        /// Applies sabotage card to local player
        /// </summary>
        private void ApplySabotageToMe(SabotageCardData sabotage, PhotonView casterView)
        {
            if (sabotage == null || playerCardManager == null)
            {
                Debug.LogError("[SabotageDraftManager] Cannot apply sabotage - null reference!");
                return;
            }

            // Apply via PlayerCardManager
            playerCardManager.ApplySabotage(sabotage, casterView);

            // Trigger event
            OnSabotageApplied?.Invoke(sabotage, casterView);

            string casterName = casterView.Owner?.NickName ?? "Unknown";
            Debug.Log($"[SabotageDraftManager] Applied sabotage '{sabotage.sabotageName}' from {casterName}");
        }

        /// <summary>
        /// Finds PhotonView for player with given ActorNumber
        /// </summary>
        private PhotonView FindPlayerPhotonView(int actorNumber)
        {
            // Find all PhotonViews in scene
            PhotonView[] allViews = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);

            foreach (PhotonView pv in allViews)
            {
                if (pv.Owner != null && pv.Owner.ActorNumber == actorNumber)
                {
                    // Check if this is the player's main PhotonView (has PlayerCardManager)
                    if (pv.GetComponent<PlayerCardManager>() != null)
                    {
                        return pv;
                    }
                }
            }

            return null;
        }

        // ==========================================
        // PHOTON RPC
        // ==========================================

        /// <summary>
        /// Receives rarity combination from Master Client
        /// </summary>
        [PunRPC]
        private void RPC_ReceiveSabotageRarities(CardRarity[] rarities)
        {
            Debug.Log($"[SabotageDraftManager] Received sabotage rarities: [{string.Join(", ", rarities)}]");
            // Rarities are used in SabotageDraftCoroutine
        }

        /// <summary>
        /// Receives player's sabotage selection
        /// </summary>
        /// <param name="actorNumber">Player who selected</param>
        /// <param name="sabotageName">Name of ScriptableObject (e.g., "DisableUpgrades")</param>
        [PunRPC]
        private void RPC_PlayerSelectedSabotage(int actorNumber, string sabotageName)
        {
            // Load sabotage from SabotagePool
            SabotageCardData sabotage = FindSabotageByName(sabotageName);

            if (sabotage == null)
            {
                Debug.LogError($"[SabotageDraftManager] Could not find sabotage '{sabotageName}'!");
                return;
            }

            playerSelections[actorNumber] = sabotage;

            Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            string playerName = player?.NickName ?? $"Player{actorNumber}";

            Debug.Log($"[SabotageDraftManager] {playerName} selected: {sabotage.sabotageName}");
        }

        /// <summary>
        /// Finds sabotage card by ScriptableObject name
        /// </summary>
        private SabotageCardData FindSabotageByName(string name)
        {
            if (sabotagePool == null) return null;

            // Try to load from Resources
            SabotageCardData sabotage = Resources.Load<SabotageCardData>($"Cards/Sabotages/{name}");

            if (sabotage == null)
            {
                Debug.LogWarning($"[SabotageDraftManager] Sabotage '{name}' not found in Resources/Cards/Sabotages/");
            }

            return sabotage;
        }

        // ==========================================
        // UTILITY
        // ==========================================

        public bool IsDrafting => isDrafting;

        public int GetNextSabotageWave() => nextSabotageWave;

        /// <summary>
        /// Gets current revealed selections (for UI)
        /// </summary>
        public Dictionary<int, SabotageCardData> GetPlayerSelections()
        {
            return new Dictionary<int, SabotageCardData>(playerSelections);
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Test Sabotage Draft")]
        private void TestSabotageDraft()
        {
            StartSabotageDraft();
        }
    }
}