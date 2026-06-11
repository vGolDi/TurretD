using UnityEngine;
using UnityEngine.UIElements;
using Photon.Pun;
using Photon.Realtime;
using ElementumDefense.Cards;
using ElementumDefense.Multiplayer;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class LobbyUI : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioClip matchFoundSound;
        [SerializeField] private AudioClip tickSound;

        private AudioSource audioSource;
        private VisualElement root;

        // Elements
        private Label lobbyMode;
        private Label lobbyStatus;
        private Label lobbyTimer;
        private Label lobbyPlayerCount;
        private Label player1Name;
        private Label player1Rank;
        private Label player2Name;
        private Label player2Rank;
        private VisualElement player2Slot;
        private VisualElement player2ReadyDot;
        private VisualElement modeBadge;
        private VisualElement statusDot;
        private VisualElement statusPulse;
        private Button btnCancel;
        private Label bottomText;

        // Match found overlay
        private VisualElement matchFoundOverlay;
        private VisualElement matchFoundBarFill;
        private Label matchFoundText;

        // Timer
        private float searchStartTime;
        private bool isSearching;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource =
                    gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null) return;

            root = uiDoc.rootVisualElement;
            if (root == null) return;

            var bg = root.Q<VisualElement>("lobby-root");
            StarfieldInjector.Instance?.Register(bg);

            QueryElements();
            BindButtons();
            InitializeDisplay();
        }

        private void Update()
        {
            if (isSearching)
                UpdateTimer();
        }

        // ==========================================
        // QUERY & BIND
        // ==========================================

        private void QueryElements()
        {
            lobbyMode =
                root.Q<Label>("lobby-mode");
            lobbyStatus =
                root.Q<Label>("lobby-status");
            lobbyTimer =
                root.Q<Label>("lobby-timer");
            lobbyPlayerCount =
                root.Q<Label>("lobby-player-count");
            player1Name =
                root.Q<Label>("player1-name");
            player1Rank =
                root.Q<Label>("player1-rank");
            player2Name =
                root.Q<Label>("player2-name");
            player2Rank =
                root.Q<Label>("player2-rank");
            player2Slot =
                root.Q<VisualElement>("player2-slot");
            player2ReadyDot =
                root.Q<VisualElement>(
                    "player2-ready-dot");

            statusDot =
                root.Q<VisualElement>("status-dot");
            statusPulse =
                root.Q<VisualElement>("status-pulse");
            btnCancel =
                root.Q<Button>("btn-cancel");
            bottomText =
                root.Q<Label>("lobby-bottom-text");

            matchFoundOverlay =
                root.Q<VisualElement>(
                    "match-found-overlay");
            matchFoundBarFill =
                root.Q<VisualElement>(
                    "match-found-bar-fill");
            matchFoundText =
                root.Q<Label>("match-found-text");

            // Find mode badge container
            modeBadge = root.Q<VisualElement>(
                className: "mode-badge");
        }

        private void BindButtons()
        {
            btnCancel?
                .RegisterCallback<ClickEvent>(evt =>
                {
                    var nm = FindFirstObjectByType
                        <NetworkManager>();
                    nm?.CancelMatchmaking();
                    evt.StopPropagation();
                });
        }

        // ==========================================
        // INITIALIZE
        // ==========================================

        private void InitializeDisplay()
        {
            // Player 1 info
            string myName =
                PhotonNetwork.NickName ?? "TRAVELER";

            if (player1Name != null)
                player1Name.text =
                    myName.ToUpper();

            if (player1Rank != null)
            {
                var player = PlayerCollection.Instance;
                if (player != null)
                    player1Rank.text =
                        player.GetRankName();
            }

            // Game mode
            UpdateModeDisplay();

            // Start search timer
            searchStartTime = Time.time;
            isSearching = true;

            HideMatchFound();
        }

        private void UpdateModeDisplay()
        {
            string mode = "CASUAL";
            bool isRanked = false;

            var player = PlayerCollection.Instance;
            if (player != null)
            {
                mode = player.SelectedGameMode
                    .ToString().ToUpper();
                isRanked = player.SelectedGameMode ==
                    GameMode.Ranked;
            }

            if (lobbyMode != null)
                lobbyMode.text = mode;

            if (modeBadge != null)
            {
                modeBadge.RemoveFromClassList(
                    "mode-badge-ranked");
                if (isRanked)
                    modeBadge.AddToClassList(
                        "mode-badge-ranked");
            }
        }

        // ==========================================
        // PUBLIC API (called by NetworkManager)
        // ==========================================

        public void UpdateStatus(string message)
        {
            if (lobbyStatus != null)
                lobbyStatus.text = message;
        }

        public void SetStatusConnected()
        {
            statusDot?.AddToClassList(
                "status-dot-connected");
            statusDot?.RemoveFromClassList(
                "status-dot-error");
        }

        public void SetStatusError()
        {
            statusDot?.AddToClassList(
                "status-dot-error");
            statusDot?.RemoveFromClassList(
                "status-dot-connected");
        }

        public void SetStatusSearching()
        {
            statusDot?.RemoveFromClassList(
                "status-dot-connected");
            statusDot?.RemoveFromClassList(
                "status-dot-error");
        }

        public void UpdatePlayerCount(
            int current, int max)
        {
            if (lobbyPlayerCount != null)
            {
                lobbyPlayerCount.text =
                    $"{current} / {max}";

                lobbyPlayerCount.RemoveFromClassList(
                    "player-count-full");

                if (current >= max)
                    lobbyPlayerCount.AddToClassList(
                        "player-count-full");
            }
        }

        public void SetOpponentJoined(
            string opponentName, string rank)
        {
            if (player2Name != null)
            {
                player2Name.text =
                    opponentName.ToUpper();
                player2Name.RemoveFromClassList(
                    "player-name-waiting");
            }

            if (player2Rank != null)
                player2Rank.text = rank;

            // Update hex icon
            var hexIcon = player2Slot?.Q<Label>(
                className: "player-hex-icon");
            if (hexIcon != null)
            {
                hexIcon.text = "⚔";
                hexIcon.RemoveFromClassList(
                    "player-hex-icon-waiting");
            }

            // Update hex visuals
            var hexBg = player2Slot?.Q<VisualElement>(
                className: "player-hex-bg");
            hexBg?.AddToClassList(
                "player-hex-bg-gold");

            var hexBorder = player2Slot
                ?.Q<VisualElement>(
                    className: "player-hex-border");
            hexBorder?.AddToClassList(
                "player-hex-border-gold");

            // Ready dot
            player2ReadyDot?.AddToClassList(
                "player-ready-dot-active");

            isSearching = false;
        }

        public void SetOpponentLeft()
        {
            if (player2Name != null)
            {
                player2Name.text = "SEARCHING...";
                player2Name.AddToClassList(
                    "player-name-waiting");
            }

            if (player2Rank != null)
                player2Rank.text = "...";

            var hexIcon = player2Slot?.Q<Label>(
                className: "player-hex-icon");
            if (hexIcon != null)
            {
                hexIcon.text = "?";
                hexIcon.AddToClassList(
                    "player-hex-icon-waiting");
            }

            var hexBg = player2Slot?.Q<VisualElement>(
                className: "player-hex-bg");
            hexBg?.RemoveFromClassList(
                "player-hex-bg-gold");

            var hexBorder = player2Slot
                ?.Q<VisualElement>(
                    className: "player-hex-border");
            hexBorder?.RemoveFromClassList(
                "player-hex-border-gold");

            player2ReadyDot?.RemoveFromClassList(
                "player-ready-dot-active");

            searchStartTime = Time.time;
            isSearching = true;
        }

        public void ShowMatchFound()
        {
            matchFoundOverlay?
                .RemoveFromClassList("hidden");
            isSearching = false;

            if (matchFoundBarFill != null)
                matchFoundBarFill.style.width =
                    new StyleLength(
                        new Length(0,
                            LengthUnit.Percent));

            if (bottomText != null)
                bottomText.text =
                    "MATCH FOUND — PREPARING ARENA";

            PlaySound(matchFoundSound);
        }

        public void UpdateMatchFoundProgress(
            float progress, string text)
        {
            if (matchFoundBarFill != null)
                matchFoundBarFill.style.width =
                    new StyleLength(
                        new Length(
                            progress * 100f,
                            LengthUnit.Percent));

            if (matchFoundText != null)
                matchFoundText.text = text;
        }

        public void HideMatchFound()
        {
            matchFoundOverlay?
                .AddToClassList("hidden");
        }

        public void SetCancelEnabled(bool enabled)
        {
            btnCancel?.SetEnabled(enabled);

            if (!enabled)
            {
                btnCancel?.AddToClassList("hidden");
            }
            else
            {
                btnCancel?.RemoveFromClassList(
                    "hidden");
            }
        }

        // ==========================================
        // TIMER
        // ==========================================

        private void UpdateTimer()
        {
            float elapsed = Time.time - searchStartTime;
            int minutes = Mathf.FloorToInt(
                elapsed / 60f);
            int seconds = Mathf.FloorToInt(
                elapsed % 60f);

            if (lobbyTimer != null)
                lobbyTimer.text =
                    $"{minutes:00}:{seconds:00}";
        }

        // ==========================================
        // AUDIO
        // ==========================================

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip, 0.8f);
        }
    }
}