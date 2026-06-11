using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Photon.Pun;
using ElementumDefense.Cards;
using ElementumDefense.Ranked;
using ElementumDefense.Auth;

namespace ElementumDefense.Multiplayer
{
    /// <summary>
    /// Auth-gated reconnect prompt. Lifecycle:
    ///  1. Auto-bootstrapped via [RuntimeInitializeOnLoadMethod] on every scene
    ///     load that isn't the in-game scene. Keeps an idle DontDestroyOnLoad
    ///     instance so we don't depend on Designer placement.
    ///  2. Subscribes to <see cref="AuthManager.OnCloudReady"/> — this fires
    ///     AFTER PlayFab login + cloud verification succeeded, so we know we
    ///     have a valid PlayFabId and can safely scope per-account state.
    ///  3. Binds the active account on <see cref="PendingMatchState"/> and
    ///     <see cref="MatchmakingBan"/>, then checks for a pending match.
    ///  4. Pending found inside reconnect window → opens an Art Deco
    ///     UIDocument popup with Reconnect / Forfeit buttons.
    ///  5. Pending found but window expired → silent auto-forfeit (loss + ban).
    /// 
    /// On Logout we clear bound account so a different player on the same PC
    /// doesn't inherit the previous user's pending state.
    /// </summary>
    public class ReconnectPromptController : MonoBehaviourPunCallbacks
    {
        // ==========================================
        // BOOTSTRAP
        // ==========================================

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // We don't need this in the actual match scene — the popup belongs
            // to the menu/login flow. Cheap name check is enough.
            string scene = SceneManager.GetActiveScene().name;
            if (scene != null && scene.IndexOf("Game", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return;

            if (FindAnyObjectByType<ReconnectPromptController>() != null) return;

            var go = new GameObject("[ReconnectPromptController]");
            DontDestroyOnLoad(go);
            go.AddComponent<ReconnectPromptController>();
        }

        // ==========================================
        // CONFIG
        // ==========================================

        [SerializeField, Tooltip("UXML for the popup. Auto-loaded from Resources if null.")]
        private VisualTreeAsset uxml;

        [SerializeField, Tooltip("USS file. Auto-loaded if null.")]
        private StyleSheet uss;

        [SerializeField, Tooltip("PanelSettings used to spawn the overlay. Auto-loaded if null.")]
        private PanelSettings panelSettings;

        // ==========================================
        // STATE
        // ==========================================

        /// <summary>Must match NetworkManager.gameSceneName in the inspector.</summary>
        [SerializeField, Tooltip("Game scene name. Must match NetworkManager.")]
        private string gameSceneName = "SampleScene";

        private enum UIState { Hidden, Confirm, Reconnecting, Failed }
        private UIState state = UIState.Hidden;

        private string roomName;
        private GameMode mode;

        // UI refs
        private UIDocument doc;
        private VisualElement card;
        private Label modeLbl, roomLbl, windowLbl, statusLbl, titleLbl, subtitleLbl, forfeitNoteLbl;
        private Button rejoinBtn, forfeitBtn;

        // ==========================================
        // LIFECYCLE
        // ==========================================

        public override void OnEnable()
        {
            base.OnEnable();
            // Hook auth — fire only after cloud is ready (PlayFabId guaranteed).
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnCloudReady += HandleCloudReady;
                
                // If AuthManager is already logged in, the event already fired.
                if (AuthManager.Instance.IsLoggedIn && !string.IsNullOrEmpty(AuthManager.Instance.PlayFabId))
                {
                    HandleCloudReady(AuthManager.Instance.CurrentUsername);
                }
            }
            else
            {
                // AuthManager spawns later — poll briefly until it exists.
                StartCoroutine(WaitForAuthManager());
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        public override void OnDisable()
        {
            base.OnDisable();
            if (AuthManager.Instance != null)
                AuthManager.Instance.OnCloudReady -= HandleCloudReady;

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private System.Collections.IEnumerator WaitForAuthManager()
        {
            float timeout = 10f;
            while (timeout > 0f && AuthManager.Instance == null)
            {
                yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
            }
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnCloudReady += HandleCloudReady;
                // If the user logged in before this controller subscribed,
                // the event might have already fired. Detect by IsLoggedIn.
                if (AuthManager.Instance.IsLoggedIn)
                    HandleCloudReady(AuthManager.Instance.CurrentUsername);
            }
        }

        // ==========================================
        // ACCOUNT BINDING + PENDING CHECK
        // ==========================================

        private void HandleCloudReady(string username)
        {
            string playFabId = AuthManager.Instance != null
                ? AuthManager.Instance.PlayFabId
                : null;

            // Bind per-account namespace BEFORE reading any state.
            PendingMatchState.UseAccount(playFabId);
            MatchmakingBan.UseAccount(playFabId);

            Debug.Log($"[ReconnectPrompt] Auth ready. Account={playFabId}, " +
                      $"hasPending={PendingMatchState.HasPending}");

            if (!PendingMatchState.HasPending) return;

            if (!PendingMatchState.IsWithinReconnectWindow)
            {
                Debug.Log("[ReconnectPrompt] Window expired — auto-forfeit.");
                ApplyForfeit();
                return;
            }

            roomName = PendingMatchState.RoomName;
            mode = PendingMatchState.Mode;
            ShowPopup();
        }

        // ==========================================
        // POPUP CONSTRUCTION
        // ==========================================

        private void EnsureUIAssets()
        {
            if (uxml == null)
                uxml = Resources.Load<VisualTreeAsset>("UI/ReconnectPrompt")
                    ?? FindAssetByPath<VisualTreeAsset>(
                        "Assets/PrzemekSkrypty/UI/ReconnectPrompt.uxml");

            if (uss == null)
                uss = Resources.Load<StyleSheet>("UI/ReconnectPromptStyles")
                    ?? FindAssetByPath<StyleSheet>(
                        "Assets/PrzemekSkrypty/UI/ReconnectPromptStyles.uss");

            if (panelSettings == null)
                panelSettings = Resources.Load<PanelSettings>("MainMenuUISettings")
                    ?? FindAssetByPath<PanelSettings>(
                        "Assets/PrzemekSkrypty/UI/MainMenuUISettings.asset");
        }

        // Editor fallback so the bootstrap also works when assets aren't in Resources.
        private static T FindAssetByPath<T>(string path) where T : Object
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
            return null;
#endif
        }

        private void ShowPopup()
        {
            EnsureUIAssets();

            if (uxml == null)
            {
                Debug.LogError("[ReconnectPrompt] Missing UXML — cannot show popup.");
                ApplyForfeit();
                return;
            }

            if (doc == null)
            {
                doc = gameObject.AddComponent<UIDocument>();
                doc.panelSettings = panelSettings;
                doc.visualTreeAsset = uxml;
                if (uss != null)
                    doc.rootVisualElement.styleSheets.Add(uss);
                // Float above other UIDocuments in the scene (lobby etc.)
                doc.sortingOrder = 1000;
                BindUI(doc.rootVisualElement);
            }

            doc.rootVisualElement.visible = true;
            doc.rootVisualElement.style.display = DisplayStyle.Flex;
            state = UIState.Confirm;
            RefreshConfirmDisplay();
            StartCoroutine(WindowCountdownRoutine());
        }

        private void HidePopup()
        {
            if (doc != null && doc.rootVisualElement != null)
            {
                doc.rootVisualElement.style.display = DisplayStyle.None;
            }
            state = UIState.Hidden;
        }

        private void BindUI(VisualElement root)
        {
            card = root.Q<VisualElement>("reconnect-card");
            titleLbl = root.Q<Label>("reconnect-title");
            subtitleLbl = root.Q<Label>("reconnect-subtitle");
            modeLbl = root.Q<Label>("reconnect-mode");
            roomLbl = root.Q<Label>("reconnect-room");
            windowLbl = root.Q<Label>("reconnect-window");
            statusLbl = root.Q<Label>("reconnect-status");
            forfeitNoteLbl = root.Q<Label>("reconnect-forfeit-note");
            rejoinBtn = root.Q<Button>("reconnect-btn-rejoin");
            forfeitBtn = root.Q<Button>("reconnect-btn-forfeit");

            if (rejoinBtn != null) rejoinBtn.clicked += TryReconnect;
            if (forfeitBtn != null) forfeitBtn.clicked += () => ApplyForfeit();
        }

        // ==========================================
        // DISPLAY HELPERS
        // ==========================================

        private void RefreshConfirmDisplay()
        {
            if (modeLbl != null) modeLbl.text = mode.ToString();
            if (roomLbl != null) roomLbl.text = string.IsNullOrEmpty(roomName) ? "—" : roomName;

            int seconds = Mathf.Max(0, PendingMatchState.SecondsRemaining);
            if (windowLbl != null) windowLbl.text = $"{seconds / 60}:{seconds % 60:00}";

            int banSeconds = mode == GameMode.Ranked
                ? MatchmakingBan.RANKED_BAN_SECONDS
                : MatchmakingBan.CASUAL_BAN_SECONDS;

            if (forfeitNoteLbl != null)
                forfeitNoteLbl.text = $"Forfeit will count as a loss and lock matchmaking " +
                                      $"for {banSeconds / 60}:{banSeconds % 60:00}.";

            SetStatus("");
            SetBusy(false);
            if (rejoinBtn != null) rejoinBtn.SetEnabled(true);
            if (forfeitBtn != null) forfeitBtn.SetEnabled(true);
        }

        private void SetStatus(string text)
        {
            if (statusLbl != null) statusLbl.text = text ?? "";
        }

        private void SetBusy(bool busy)
        {
            if (card == null) return;
            if (busy) card.AddToClassList("is-busy");
            else card.RemoveFromClassList("is-busy");
        }

        private System.Collections.IEnumerator WindowCountdownRoutine()
        {
            while (state == UIState.Confirm)
            {
                int seconds = Mathf.Max(0, PendingMatchState.SecondsRemaining);
                if (windowLbl != null) windowLbl.text = $"{seconds / 60}:{seconds % 60:00}";

                if (seconds <= 0)
                {
                    Debug.Log("[ReconnectPrompt] Window hit zero — auto-forfeit.");
                    ApplyForfeit();
                    yield break;
                }
                yield return new WaitForSeconds(1f);
            }
        }

        // ==========================================
        // ACTIONS
        // ==========================================

        private void TryReconnect()
        {
            state = UIState.Reconnecting;
            SetBusy(true);
            if (rejoinBtn != null) rejoinBtn.SetEnabled(false);
            if (forfeitBtn != null) forfeitBtn.SetEnabled(false);
            SetStatus("Connecting to server...");

            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.Name == roomName)
            {
                Debug.Log($"[ReconnectPrompt] Already in room '{roomName}'. Reloading game scene.");
                SetStatus("Rejoining... loading game...");
                HidePopup();
                // Pause buffered-event processing until the game scene is active
                // (resumed in OnSceneLoaded). Same reason as the rejoin path.
                PhotonNetwork.IsMessageQueueRunning = false;
                SceneManager.LoadScene(gameSceneName);
                return;
            }

            // Always go through the full chain — connect → master → lobby → rejoin.
            // RejoinRoom requires Server == MasterServer; if we're still on
            // NameServer, ConnectUsingSettings + the OnConnectedToMaster
            // callback handle the transition for us.
            if (!PhotonNetwork.IsConnected)
            {
                string playFabId = AuthManager.Instance != null
                    ? AuthManager.Instance.PlayFabId
                    : null;
                if (!string.IsNullOrEmpty(playFabId))
                {
                    if (PhotonNetwork.AuthValues == null)
                        PhotonNetwork.AuthValues = new Photon.Realtime.AuthenticationValues();
                    PhotonNetwork.AuthValues.UserId = playFabId;
                }

                PhotonNetwork.ConnectUsingSettings();
            }
            StartCoroutine(WaitForMasterAndRejoin());
        }

        private System.Collections.IEnumerator WaitForMasterAndRejoin()
        {
            // Wait until Photon's server type is MasterServer — that's the
            // only state where RejoinRoom is valid.
            float timeout = 15f;
            while (timeout > 0f)
            {
                if (PhotonNetwork.IsConnectedAndReady &&
                    PhotonNetwork.Server == Photon.Realtime.ServerConnection.MasterServer)
                {
                    break;
                }
                yield return new WaitForSeconds(0.25f);
                timeout -= 0.25f;
                SetStatus($"Connecting... ({Mathf.CeilToInt(timeout)}s)");
            }

            if (!(PhotonNetwork.IsConnectedAndReady &&
                  PhotonNetwork.Server == Photon.Realtime.ServerConnection.MasterServer))
            {
                FailWithMessage("Failed to reach master server.");
                yield break;
            }

            DoRejoin();
        }

        private void DoRejoin()
        {
            SetStatus($"Rejoining \"{roomName}\"...");
            bool ok = PhotonNetwork.RejoinRoom(roomName);
            if (!ok)
            {
                FailWithMessage("Rejoin call rejected. Forfeiting.");
                // Auto-forfeit after a short delay so the user reads the message.
                StartCoroutine(DelayedForfeit(1.5f));
            }
            // Otherwise OnSceneLoaded handles closing the popup once GameScene loads.
        }

        private void FailWithMessage(string msg)
        {
            state = UIState.Failed;
            SetStatus(msg);
            // Re-enable the buttons so the player can try again or forfeit.
            if (rejoinBtn != null) rejoinBtn.SetEnabled(true);
            if (forfeitBtn != null) forfeitBtn.SetEnabled(true);
            SetBusy(false);
        }

        private System.Collections.IEnumerator DelayedForfeit(float seconds)
        {
            yield return new WaitForSeconds(seconds);
            ApplyForfeit();
        }

        private void ApplyForfeit()
        {
            if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom && !string.IsNullOrEmpty(roomName))
            {
                StartCoroutine(ForfeitInRoomRoutine());
            }
            else
            {
                if (PhotonNetwork.InRoom)
                {
                    try
                    {
                        bool raised = MatchOpponentWatcher.RaiseForfeit();
                        Debug.Log($"[ReconnectPrompt] RaiseForfeit → {raised}");
                        PhotonNetwork.SendAllOutgoingCommands(); // Force send before leaving
                        PhotonNetwork.LeaveRoom();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[ReconnectPrompt] RaiseForfeit failed: {e.Message}");
                    }
                }
                ApplyLocalForfeit();
            }
        }

        private System.Collections.IEnumerator ForfeitInRoomRoutine()
        {
            SetStatus("Forfeiting match in background...");
            SetBusy(true);

            Debug.Log($"[ReconnectPrompt] Rejoining room '{roomName}' in background to forfeit...");
            PhotonNetwork.RejoinRoom(roomName);

            float timeout = 4.0f;
            while (timeout > 0f && !PhotonNetwork.InRoom)
            {
                yield return new WaitForSeconds(0.1f);
                timeout -= 0.1f;
            }

            if (PhotonNetwork.InRoom)
            {
                bool raiseFailed = false;
                try
                {
                    bool raised = MatchOpponentWatcher.RaiseForfeit();
                    Debug.Log($"[ReconnectPrompt] Background RaiseForfeit → {raised}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[ReconnectPrompt] Background RaiseForfeit failed: {e.Message}");
                    raiseFailed = true;
                }

                if (!raiseFailed)
                {
                    yield return new WaitForSeconds(0.5f); // Wait to ensure network packet is sent
                }
                
                PhotonNetwork.LeaveRoom();
                yield return new WaitForSeconds(0.2f);
            }
            else
            {
                Debug.LogWarning("[ReconnectPrompt] Background rejoin for forfeit timed out or failed. Applying local forfeit anyway.");
            }

            ApplyLocalForfeit();
        }

        private void ApplyLocalForfeit()
        {
            // Local: ELO loss accounting
            var collection = PlayerCollection.Instance;
            if (collection != null && mode == GameMode.Ranked)
            {
                int myElo = collection.GetElo();
                int loss = EloCalculator.CalculateEloChange(myElo, myElo, false);
                collection.AddElo(loss);
                Debug.Log($"[ReconnectPrompt] Ranked forfeit applied: {loss} ELO.");
            }
            else
            {
                Debug.Log("[ReconnectPrompt] Casual forfeit applied.");
            }

            MatchmakingBan.ApplyForMode(mode);
            PendingMatchState.Clear();
            // Drop the in-match state snapshot too — the match is abandoned, so a
            // leftover snapshot must not leak into the next match.
            ElementumDefense.Multiplayer.Reconnect.MatchSnapshotService.Instance?.Clear();
            HidePopup();
        }

        // ==========================================
        // PHOTON CALLBACKS
        // ==========================================

        public override void OnJoinedRoom()
        {
            if (state == UIState.Reconnecting)
            {
                Debug.Log($"[ReconnectPrompt] Rejoined room '{PhotonNetwork.CurrentRoom.Name}' successfully! Loading game scene '{gameSceneName}'...");
                SetStatus("Rejoined! Loading game...");

                HidePopup();

                // CRITICAL for reconnect: pause Photon's incoming message queue
                // BEFORE loading the game scene. Buffered PhotonNetwork.Instantiate
                // events (e.g. the opponent's Player_MP) are delivered right after
                // rejoin — if processed while still in the menu scene they spawn
                // there and die on scene load (so the reconnecting player never sees
                // the opponent, and RPCs to those views fail). Pausing holds them
                // until the game scene is active; we resume in OnSceneLoaded.
                PhotonNetwork.IsMessageQueueRunning = false;

                SceneManager.LoadScene(gameSceneName);
            }
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            if (state == UIState.Reconnecting)
            {
                Debug.LogError($"[ReconnectPrompt] Rejoin room failed: {message} (code {returnCode})");
                FailWithMessage($"Rejoin failed: {message}. Forfeiting...");
                StartCoroutine(DelayedForfeit(2.0f));
            }
        }

        // ==========================================
        // SCENE TRANSITION
        // ==========================================

        private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
        {
            if (scene.name == null) return;

            // If the rejoin succeeded the GameScene loads — close popup and let
            // the in-game flow take over.
            if (state == UIState.Reconnecting && scene.name == gameSceneName)
            {
                Debug.Log("[ReconnectPrompt] Game scene loaded after rejoin — resuming message queue, closing prompt.");
                // Resume processing of buffered events now that the game scene is
                // active — the opponent's Player_MP and other networked objects
                // will now instantiate INTO the game scene.
                PhotonNetwork.IsMessageQueueRunning = true;
                HidePopup();
                return;
            }

            // Menu scene loaded while we're idle — re-check for pending match.
            // This handles the case when the player exited the game via
            // Pause Menu → Main Menu (HandleCloudReady already fired during
            // login, so the event won't fire again).
            if (state == UIState.Hidden && scene.name != gameSceneName)
            {
                if (AuthManager.Instance != null && AuthManager.Instance.IsLoggedIn
                    && PendingMatchState.HasAccount && PendingMatchState.HasPending)
                {
                    Debug.Log($"[ReconnectPrompt] Menu scene '{scene.name}' loaded with pending match. Checking window...");

                    // If we're STILL in the room (left via Pause Menu without
                    // disconnecting), reconnect is instant and no TTL has been
                    // consumed — always offer the popup regardless of the window.
                    // The window only matters when fully disconnected (app closed).
                    bool stillInRoom = PhotonNetwork.InRoom &&
                                       PhotonNetwork.CurrentRoom != null &&
                                       PhotonNetwork.CurrentRoom.Name == PendingMatchState.RoomName;

                    if (!stillInRoom && !PendingMatchState.IsWithinReconnectWindow)
                    {
                        Debug.Log("[ReconnectPrompt] Window expired (and not in room) — auto-forfeit.");
                        ApplyForfeit();
                        return;
                    }

                    roomName = PendingMatchState.RoomName;
                    mode = PendingMatchState.Mode;
                    ShowPopup();
                }
            }
        }
    }
}
