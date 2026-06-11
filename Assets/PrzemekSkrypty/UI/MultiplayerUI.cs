using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using ElementumDefense.Cards;
using ElementumDefense.Ranked;
using ElementumDefense.Multiplayer;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MultiplayerUI : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField]
        private MainMenuController mainMenuController;

        [Header("Scene")]
        [SerializeField]
        private string lobbySceneName = "LobbyScene";

        [Header("Audio")]
        [SerializeField]
        private AudioClip buttonClickSound;
        [SerializeField]
        private AudioClip modeSelectSound;

        private AudioSource audioSource;
        private VisualElement root;

        // ==========================================
        // UI ELEMENTS — base
        // ==========================================

        private Button btnBack;
        private VisualElement modeCasual;
        private VisualElement modeRanked;
        private VisualElement modeCustom;
        private Label rankName;
        private Label eloValue;
        private Label rankIcon;
        private VisualElement rankHexBg;
        private VisualElement rankHexBorder;

        // Loading
        private VisualElement loadingOverlay;
        private VisualElement loadingBarFill;
        private Label loadingText;

        // ==========================================
        // UI ELEMENTS — rank decorations
        // ==========================================

        private VisualElement rankHexagon;
        private VisualElement rankGlow;
        private VisualElement rankOuterRing;
        private VisualElement rankThirdRing;
        private VisualElement[] rankTips =
            new VisualElement[4];
        private Label rankStarL;
        private Label rankStarR;
        private Label rankCrown;

        // ==========================================
        // LIFECYCLE
        // ==========================================

        private void Awake()
        {
            audioSource =
                GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject
                    .AddComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (mainMenuController == null)
                mainMenuController =
                    FindFirstObjectByType
                        <MainMenuController>();
        }

        // ==========================================
        // SHOW / HIDE
        // ==========================================

        public void Show()
        {
            var uiDoc =
                GetComponent<UIDocument>();
            if (uiDoc == null) return;

            uiDoc.enabled = true;
            gameObject.SetActive(true);

            root = uiDoc.rootVisualElement;
            if (root == null) return;

            root.style.display = DisplayStyle.Flex;

            QueryElements();
            BindButtons();
            EnsureBanLabels();
            RefreshDisplay();
            StartBanTicker();

            var bg = root.Q<VisualElement>(
                "multiplayer-root");
            StarfieldInjector.Instance
                ?.Register(bg);
        }

        public void Hide()
        {
            if (root != null)
            {
                var bg = root.Q<VisualElement>(
                    "multiplayer-root");
                StarfieldInjector.Instance
                    ?.Unregister(bg);
            }

            StopAllCoroutines();
            banTickerRoutine = null;

            var uiDoc =
                GetComponent<UIDocument>();
            if (uiDoc != null &&
                uiDoc.rootVisualElement != null)
            {
                uiDoc.rootVisualElement
                    .style.display =
                    DisplayStyle.None;
            }

            root = uiDoc?.rootVisualElement;
        }

        // ==========================================
        // QUERY
        // ==========================================

        private void QueryElements()
        {
            // Base elements
            btnBack = root.Q<Button>("btn-back");
            modeCasual =
                root.Q<VisualElement>("mode-casual");
            modeRanked =
                root.Q<VisualElement>("mode-ranked");
            modeCustom =
                root.Q<VisualElement>("mode-custom");

            rankName =
                root.Q<Label>("mp-rank-name");
            eloValue =
                root.Q<Label>("mp-elo-value");
            rankIcon =
                root.Q<Label>("mp-rank-icon");
            rankHexBg =
                root.Q<VisualElement>(
                    "mp-rank-hex-bg");
            rankHexBorder =
                root.Q<VisualElement>(
                    "mp-rank-hex-border");

            // Loading
            loadingOverlay =
                root.Q<VisualElement>(
                    "mp-loading-overlay");
            loadingBarFill =
                root.Q<VisualElement>(
                    "mp-loading-bar-fill");
            loadingText =
                root.Q<Label>("mp-loading-text");

            // ---- Rank decorations ----
            rankHexagon =
                root.Q<VisualElement>(
                    "mp-rank-hexagon");
            rankGlow =
                root.Q<VisualElement>(
                    "mp-rank-glow");
            rankOuterRing =
                root.Q<VisualElement>(
                    "mp-rank-outer-ring");
            rankThirdRing =
                root.Q<VisualElement>(
                    "mp-rank-third-ring");

            rankTips[0] =
                root.Q<VisualElement>(
                    "mp-rank-tip-top");
            rankTips[1] =
                root.Q<VisualElement>(
                    "mp-rank-tip-right");
            rankTips[2] =
                root.Q<VisualElement>(
                    "mp-rank-tip-bottom");
            rankTips[3] =
                root.Q<VisualElement>(
                    "mp-rank-tip-left");

            rankStarL =
                root.Q<Label>("mp-rank-star-l");
            rankStarR =
                root.Q<Label>("mp-rank-star-r");
            rankCrown =
                root.Q<Label>("mp-rank-crown");
        }

        // ==========================================
        // BIND
        // ==========================================

        private void BindButtons()
        {
            btnBack?.RegisterCallback
                <ClickEvent>(evt =>
                {
                    PlayClick();
                    mainMenuController?.BackToMainMenu();
                    evt.StopPropagation();
                });

            modeCasual?.RegisterCallback
                <ClickEvent>(evt =>
                {
                    PlayMode();
                    SelectMode(GameMode.Casual);
                    evt.StopPropagation();
                });

            modeRanked?.RegisterCallback
                <ClickEvent>(evt =>
                {
                    PlayMode();
                    SelectMode(GameMode.Ranked);
                    evt.StopPropagation();
                });
        }

        // ==========================================
        // REFRESH
        // ==========================================

        private void RefreshDisplay()
        {
            RefreshRank();
            HideLoading();
        }

        private void RefreshRank()
        {
            var player =
                PlayerCollection.Instance;
            if (player == null) return;

            int elo = player.GetElo();
            string rank = player.GetRankName();
            Color rankColor =
                player.GetRankColor();

            // ---- Tekst rangi ----
            if (rankName != null)
            {
                rankName.text = rank;
                rankName.style.color =
                    new StyleColor(rankColor);
            }

            if (eloValue != null)
                eloValue.text = $"{elo} ELO";

            if (rankIcon != null)
            {
                rankIcon.text =
                    GetRankNumeral(elo);
                rankIcon.style.color =
                    new StyleColor(rankColor);
            }

            // ---- Kolory głównego rombu ----
            if (rankHexBg != null)
            {
                Color bg = rankColor;
                bg.a = 0.1f;
                rankHexBg.style.backgroundColor =
                    new StyleColor(bg);
            }

            if (rankHexBorder != null)
            {
                SetBorderColor(
                    rankHexBorder, rankColor);
            }

            // ---- Ozdobniki zależne od rangi ----
            ApplyRankDecorations(elo, rankColor);
        }

        // ==========================================
        // RANK DECORATIONS
        // ==========================================

        /// <summary>
        /// Główna metoda — aktywuje/dezaktywuje
        /// ozdobniki w zależności od ELO:
        ///
        /// Bronze:   sam romb
        /// Silver+:  aura + czubki
        /// Gold+:    + pierścień zewnętrzny,
        ///           grubsza ramka
        /// Platinum+: + gwiazdki
        /// Diamond:  + korona + trzeci pierścień
        /// </summary>
        private void ApplyRankDecorations(
            int elo, Color rankColor)
        {
            bool silver = elo >= 1200;
            bool gold = elo >= 1500;
            bool platinum = elo >= 1800;
            bool diamond = elo >= 2200;

            // ---- Badge scale ----
            // Wyższe rangi = lekko większy romb
            if (rankHexagon != null)
            {
                float s = diamond ? 1.12f :
                          platinum ? 1.06f :
                          gold ? 1.03f : 1f;
                rankHexagon.style.scale =
                    new StyleScale(
                        new Scale(
                            new Vector2(s, s)));
            }

            // ---- GLOW / AURA (Silver+) ----
            if (rankGlow != null)
            {
                float glowAlpha =
                    diamond ? 0.18f :
                    platinum ? 0.13f :
                    gold ? 0.10f :
                    silver ? 0.06f : 0f;

                rankGlow.style.opacity =
                    glowAlpha > 0f ? 1f : 0f;

                Color gc = rankColor;
                gc.a = glowAlpha;
                rankGlow.style.backgroundColor =
                    new StyleColor(gc);
            }

            // ---- OUTER RING (Gold+) ----
            if (rankOuterRing != null)
            {
                rankOuterRing.style.opacity =
                    gold ? 1f : 0f;

                if (gold)
                {
                    Color rc = rankColor;
                    rc.a = diamond ? 0.5f :
                           platinum ? 0.4f : 0.3f;
                    SetBorderColor(
                        rankOuterRing, rc);
                }
            }

            // ---- THIRD RING (Diamond) ----
            if (rankThirdRing != null)
            {
                rankThirdRing.style.opacity =
                    diamond ? 1f : 0f;

                if (diamond)
                {
                    Color tc = rankColor;
                    tc.a = 0.22f;
                    SetBorderColor(
                        rankThirdRing, tc);
                }
            }

            // ---- TIP DIAMONDS (Silver+) ----
            if (rankTips != null)
            {
                foreach (var tip in rankTips)
                {
                    if (tip == null) continue;

                    tip.style.opacity =
                        silver ? 1f : 0f;

                    if (silver)
                    {
                        // Wypełnienie
                        Color tipFill = rankColor;
                        tipFill.a =
                            gold ? 0.6f : 0.35f;
                        tip.style.backgroundColor =
                            new StyleColor(tipFill);

                        // Ramka
                        Color tipBorder = rankColor;
                        tipBorder.a = 0.8f;
                        SetBorderColor(
                            tip, tipBorder);
                    }
                }
            }

            // ---- STARS (Platinum+) ----
            SetLabelVisible(
                rankStarL, platinum, rankColor);
            SetLabelVisible(
                rankStarR, platinum, rankColor);

            // ---- CROWN (Diamond) ----
            SetLabelVisible(
                rankCrown, diamond, rankColor);

            // ---- Main border thickness ----
            // Im wyższa ranga, tym grubsza ramka
            if (rankHexBorder != null)
            {
                int bw = diamond ? 3 :
                         (gold || platinum) ? 2 : 1;
                SetBorderWidth(rankHexBorder, bw);
            }

            // ---- Main bg intensity ----
            // Jaśniejsze wypełnienie rombu
            if (rankHexBg != null)
            {
                Color bg = rankColor;
                bg.a = diamond ? 0.20f :
                       platinum ? 0.16f :
                       gold ? 0.14f :
                       silver ? 0.10f : 0.08f;
                rankHexBg.style.backgroundColor =
                    new StyleColor(bg);
            }
        }

        // ==========================================
        // HELPER — border color
        // ==========================================

        private void SetBorderColor(
            VisualElement el, Color c)
        {
            if (el == null) return;
            var sc = new StyleColor(c);
            el.style.borderTopColor = sc;
            el.style.borderBottomColor = sc;
            el.style.borderLeftColor = sc;
            el.style.borderRightColor = sc;
        }

        // ==========================================
        // HELPER — border width
        // ==========================================

        private void SetBorderWidth(
            VisualElement el, int w)
        {
            if (el == null) return;
            el.style.borderTopWidth = w;
            el.style.borderBottomWidth = w;
            el.style.borderLeftWidth = w;
            el.style.borderRightWidth = w;
        }

        // ==========================================
        // HELPER — label visibility
        // ==========================================

        private void SetLabelVisible(
            Label label,
            bool visible,
            Color color)
        {
            if (label == null) return;
            label.style.opacity =
                visible ? 1f : 0f;
            if (visible)
                label.style.color =
                    new StyleColor(color);
        }

        // ==========================================
        // MODE SELECTION
        // ==========================================

        private void SelectMode(GameMode mode)
        {
            if (ElementumDefense.Multiplayer.PendingMatchState.HasPending)
            {
                Debug.LogWarning("[MultiplayerUI] Matchmaking blocked. A match is currently pending.");
                if (loadingText != null)
                {
                    loadingText.text = "You must Reconnect or Abandon your current match first.";
                }
                return;
            }

            // Gate matchmaking on local matchmaking ban (forfeit cooldown).
            if (MatchmakingBan.IsBannedForMode(mode))
            {
                Debug.LogWarning($"[MultiplayerUI] Matchmaking blocked. " +
                    $"Ban active for {MatchmakingBan.SecondsRemainingForMode(mode)}s ({MatchmakingBan.GetReasonForMode(mode)}).");
                FlashBanLabel(mode);
                return;
            }

            var player =
                PlayerCollection.Instance;
            if (player != null)
                player.SelectedGameMode = mode;

            Debug.Log(
                $"[MultiplayerUI] " +
                $"Selected: {mode}");
            StartCoroutine(
                LoadSceneSequence(lobbySceneName));
        }

        private void ShowBanMessage()
        {
            // Reuse the loading-text label as a transient warning channel.
            if (loadingText != null)
            {
                loadingText.text = $"Matchmaking locked. " +
                    $"Try again in {MatchmakingBan.FormatRemaining()}.";
            }
        }

        // ==========================================
        // BAN COUNTDOWN UI
        // ==========================================

        // Created lazily under each mode card. Holding our own refs avoids
        // re-querying the tree in the per-second update.
        private Label banLabelCasual;
        private Label banLabelRanked;
        private Coroutine banTickerRoutine;

        /// <summary>
        /// Adds a small "BAN: M:SS" label under each mode card if not yet
        /// present. We do this in C# rather than in the UXML so the menu
        /// continues to work even if the UXML hasn't been edited yet.
        /// </summary>
        private void EnsureBanLabels()
        {
            banLabelCasual = AttachBanLabel(modeCasual, "ban-label-casual", banLabelCasual);
            banLabelRanked = AttachBanLabel(modeRanked, "ban-label-ranked", banLabelRanked);
        }

        private Label AttachBanLabel(VisualElement card, string name, Label existing)
        {
            if (card == null) return existing;
            if (existing != null && existing.parent == card) return existing;

            // Maybe a previous Show() already added it.
            var found = card.Q<Label>(name);
            if (found != null) return found;

            var lbl = new Label("") { name = name };
            lbl.AddToClassList("mp-mode-ban");
            // Inline minimal styling — fits all themes without needing USS edits.
            lbl.style.position = Position.Absolute;
            lbl.style.bottom = 6;
            lbl.style.left = 0;
            lbl.style.right = 0;
            lbl.style.unityTextAlign = TextAnchor.MiddleCenter;
            lbl.style.color = new StyleColor(new Color(0.96f, 0.62f, 0.04f));
            lbl.style.fontSize = 11;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            lbl.style.letterSpacing = 2;
            lbl.style.display = DisplayStyle.None;
            card.Add(lbl);
            return lbl;
        }

        private void StartBanTicker()
        {
            if (banTickerRoutine != null) StopCoroutine(banTickerRoutine);
            banTickerRoutine = StartCoroutine(BanTickerRoutine());
        }

        private IEnumerator BanTickerRoutine()
        {
            // Refresh once on entry so the label is already correct, then once a second.
            while (true)
            {
                UpdateBanLabels();
                yield return new WaitForSeconds(1f);
            }
        }

        private void UpdateBanLabels()
        {
            bool casualBanned = MatchmakingBan.IsBannedForMode(GameMode.Casual);
            string casualRemaining = casualBanned ? MatchmakingBan.FormatRemainingForMode(GameMode.Casual) : "";
            ApplyBanLabel(banLabelCasual, modeCasual, casualBanned, casualRemaining);

            bool rankedBanned = MatchmakingBan.IsBannedForMode(GameMode.Ranked);
            string rankedRemaining = rankedBanned ? MatchmakingBan.FormatRemainingForMode(GameMode.Ranked) : "";
            ApplyBanLabel(banLabelRanked, modeRanked, rankedBanned, rankedRemaining);
        }

        private void ApplyBanLabel(Label lbl, VisualElement card, bool banned, string timeStr)
        {
            if (lbl == null) return;
            lbl.style.display = banned ? DisplayStyle.Flex : DisplayStyle.None;
            if (banned) lbl.text = $"BAN  {timeStr}";

            if (card != null)
            {
                if (banned)
                {
                    card.AddToClassList("mp-mode-disabled");
                }
                else
                {
                    if (card.name == "mode-casual" || card.name == "mode-ranked")
                    {
                        card.RemoveFromClassList("mp-mode-disabled");
                    }
                }
            }
        }

        private void FlashBanLabel(GameMode mode)
        {
            // Briefly highlight the label so the click feels acknowledged.
            Label target = mode == GameMode.Ranked ? banLabelRanked : banLabelCasual;
            if (target == null) return;
            target.style.color = new StyleColor(new Color(1f, 0.4f, 0.4f));
            StartCoroutine(ResetLabelColorAfter(target, 0.6f));
        }

        private IEnumerator ResetLabelColorAfter(Label lbl, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (lbl != null)
                lbl.style.color = new StyleColor(new Color(0.96f, 0.62f, 0.04f));
        }

        // ==========================================
        // LOADING
        // ==========================================

        private IEnumerator LoadSceneSequence(
            string sceneName)
        {
            ShowLoading();

            AsyncOperation asyncLoad =
                SceneManager.LoadSceneAsync(
                    sceneName);
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                float progress =
                    Mathf.Clamp01(
                        asyncLoad.progress / 0.9f);

                if (loadingBarFill != null)
                    loadingBarFill.style.width =
                        new StyleLength(
                            new Length(
                                progress * 100f,
                                LengthUnit.Percent));

                if (loadingText != null)
                    loadingText.text =
                        $"Loading... " +
                        $"{Mathf.RoundToInt(progress * 100)}%";

                if (asyncLoad.progress >= 0.9f)
                {
                    if (loadingBarFill != null)
                        loadingBarFill.style.width =
                            new StyleLength(
                                new Length(
                                    100f,
                                    LengthUnit
                                        .Percent));

                    if (loadingText != null)
                        loadingText.text =
                            "Entering arena...";

                    yield return
                        new WaitForSecondsRealtime(
                            0.5f);
                    asyncLoad
                        .allowSceneActivation = true;
                }

                yield return null;
            }
        }

        private void ShowLoading()
        {
            loadingOverlay?.RemoveFromClassList(
                "hidden");

            if (loadingBarFill != null)
                loadingBarFill.style.width =
                    new StyleLength(
                        new Length(
                            0, LengthUnit.Percent));

            if (loadingText != null)
                loadingText.text = "Loading... 0%";
        }

        private void HideLoading()
        {
            loadingOverlay?.AddToClassList(
                "hidden");
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private string GetRankNumeral(int elo)
        {
            return EloCalculator
                .GetRankNumeral(elo);
        }

        private void PlayClick()
        {
            if (buttonClickSound != null)
                audioSource?.PlayOneShot(
                    buttonClickSound, 0.7f);
        }

        private void PlayMode()
        {
            if (modeSelectSound != null)
                audioSource?.PlayOneShot(
                    modeSelectSound, 0.8f);
            else
                PlayClick();
        }
    }
}