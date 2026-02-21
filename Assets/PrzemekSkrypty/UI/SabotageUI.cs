using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using ElementumDefense.Cards;
using Photon.Pun;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SabotageUI : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioClip selectSound;
        [SerializeField] private AudioClip revealSound;

        private AudioSource audioSource;
        private VisualElement root;

        private VisualElement sabDraftPanel;
        private VisualElement sabCards;
        private Label sabTimer;
        private VisualElement sabWaiting;
        private Label sabWaitingText;

        private VisualElement sabRevealPanel;
        private VisualElement sabRevealCards;
        private Label sabRevealTimer;

        private SabotageDraftManager sabManager;
        private bool isInitialized;
        private bool hasSelected;

        private float retryTimer;
        private const float RETRY_INTERVAL = 0.5f;

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
            QueryElements();
            HideAll();
        }

        private void Update()
        {
            if (!isInitialized)
            {
                retryTimer += Time.deltaTime;
                if (retryTimer >= RETRY_INTERVAL)
                {
                    retryTimer = 0f;
                    TryInitialize();
                }
            }
        }

        private void TryInitialize()
        {
            sabManager =
                SabotageDraftManager.Instance;
            if (sabManager == null) return;

            sabManager.OnSabotageOffered +=
                ShowDraft;
            sabManager.OnDraftTimerUpdate +=
                UpdateTimer;
            sabManager.OnRevealPhaseStart +=
                ShowReveal;
            sabManager.OnRevealPhaseEnd +=
                HideReveal;
            sabManager.OnDraftTimeout +=
                OnTimeout;
            sabManager.OnSabotageDraftComplete +=
                OnDraftComplete;

            isInitialized = true;
            Debug.Log("[SabotageUI] Initialized");
        }

        private void OnDestroy()
        {
            if (sabManager != null)
            {
                sabManager.OnSabotageOffered -=
                    ShowDraft;
                sabManager.OnDraftTimerUpdate -=
                    UpdateTimer;
                sabManager.OnRevealPhaseStart -=
                    ShowReveal;
                sabManager.OnRevealPhaseEnd -=
                    HideReveal;
                sabManager.OnDraftTimeout -=
                    OnTimeout;
                sabManager.OnSabotageDraftComplete -=
                    OnDraftComplete;
            }
        }

        private void QueryElements()
        {
            sabDraftPanel =
                root.Q<VisualElement>(
                    "sab-draft-panel");
            sabCards =
                root.Q<VisualElement>("sab-cards");
            sabTimer =
                root.Q<Label>("sab-timer");
            sabWaiting =
                root.Q<VisualElement>("sab-waiting");
            sabWaitingText =
                root.Q<Label>("sab-waiting-text");

            sabRevealPanel =
                root.Q<VisualElement>(
                    "sab-reveal-panel");
            sabRevealCards =
                root.Q<VisualElement>(
                    "sab-reveal-cards");
            sabRevealTimer =
                root.Q<Label>("sab-reveal-timer");
        }

        // ==========================================
        // DRAFT
        // ==========================================

        private void ShowDraft(
            SabotageCardData[] cards)
        {
            hasSelected = false;
            ShowSabRoot();

            sabDraftPanel?
                .RemoveFromClassList("hidden");
            sabRevealPanel?
                .AddToClassList("hidden");
            sabWaiting?.AddToClassList("hidden");

            if (sabCards == null) return;
            sabCards.Clear();

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null) continue;
                var card = BuildSabCard(cards[i], i);
                sabCards.Add(card);
            }
        }

        private VisualElement BuildSabCard(
            SabotageCardData sab, int index)
        {
            string rk = sab.rarity switch
            {
                CardRarity.Legendary => "legendary",
                CardRarity.Rare => "rare",
                _ => "common"
            };

            var slot = new VisualElement();
            slot.AddToClassList("sab-card");
            slot.AddToClassList($"sab-card-{rk}");

            // Inner border
            var inner = new VisualElement();
            inner.AddToClassList("sab-card-inner");
            slot.Add(inner);

            // Corners
            foreach (var pos in new[] {
                "tl", "tr", "bl", "br" })
            {
                var c = new VisualElement();
                c.AddToClassList("sc-corner");
                c.AddToClassList($"sc-corner-{pos}");
                slot.Add(c);
            }

            // Icon
            if (sab.sabotageIcon != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList("sab-card-icon");
                icon.style.backgroundImage =
                    new StyleBackground(
                        sab.sabotageIcon);
                slot.Add(icon);
            }

            // Rarity line
            var line = new VisualElement();
            line.AddToClassList("sab-rarity-line");
            line.AddToClassList(
                $"sab-rarity-line-{rk}");
            slot.Add(line);

            // Name
            var name = new Label(sab.sabotageName);
            name.AddToClassList("sab-card-name");
            slot.Add(name);

            // Rarity
            var rarity = new Label(
                sab.rarity.ToString().ToUpper());
            rarity.AddToClassList(
                "sab-card-rarity-text");
            rarity.AddToClassList(
                $"sab-rarity-text-{rk}");
            slot.Add(rarity);

            // Description
            if (!string.IsNullOrEmpty(sab.description))
            {
                var desc = new Label(sab.description);
                desc.AddToClassList("sab-card-desc");
                slot.Add(desc);
            }

            // Duration
            var dur = new Label(sab.GetDurationText());
            dur.AddToClassList("sab-card-duration");
            slot.Add(dur);

            // Buttons container — pushed to bottom
            var btns = new VisualElement();
            btns.AddToClassList("sab-card-buttons");

            var btn = new Button();
            btn.text = "INFLICT";
            btn.AddToClassList("sab-select-btn");

            int idx = index;
            btn.RegisterCallback<ClickEvent>(evt =>
            {
                PlaySound(selectSound);
                OnSabotageSelected(idx);
                evt.StopPropagation();
            });
            btns.Add(btn);

            slot.Add(btns);

            // Hover accent
            var accent = new VisualElement();
            accent.AddToClassList(
                "sab-card-hover-accent");
            accent.AddToClassList(
                $"sab-hover-{rk}");
            slot.Add(accent);

            return slot;
        }

        private void OnSabotageSelected(int index)
        {
            if (hasSelected) return;
            hasSelected = true;

            sabManager?.SelectSabotage(index);

            var allCards = sabCards
                .Query<VisualElement>(
                    className: "sab-card").ToList();

            for (int i = 0; i < allCards.Count; i++)
            {
                if (i == index)
                    allCards[i].AddToClassList(
                        "sab-card-selected");
                else
                    allCards[i].AddToClassList(
                        "sab-card-dimmed");

                var btn = allCards[i].Q<Button>(
                    className: "sab-select-btn");
                btn?.SetEnabled(false);
            }

            sabWaiting?
                .RemoveFromClassList("hidden");
        }

        // ==========================================
        // TIMER
        // ==========================================

        private void UpdateTimer(float remaining)
        {
            if (hasSelected) return;
            if (sabTimer == null) return;

            sabTimer.text =
                Mathf.CeilToInt(remaining).ToString();

            if (remaining <= 5f)
                sabTimer.AddToClassList(
                    "sab-timer-critical");
            else
                sabTimer.RemoveFromClassList(
                    "sab-timer-critical");
        }

        // ==========================================
        // REVEAL
        // ==========================================

        private void ShowReveal(
            Dictionary<int, SabotageCardData>
                selections)
        {
            sabDraftPanel?.AddToClassList("hidden");
            sabRevealPanel?
                .RemoveFromClassList("hidden");

            PlaySound(revealSound);

            if (sabRevealCards == null) return;
            sabRevealCards.Clear();

            int localActor =
                PhotonNetwork.LocalPlayer.ActorNumber;

            foreach (var kvp in selections)
            {
                int actorNumber = kvp.Key;
                SabotageCardData sab = kvp.Value;
                bool isMe = actorNumber == localActor;

                var player = PhotonNetwork.CurrentRoom
                    .GetPlayer(actorNumber);
                string playerName =
                    player?.NickName ??
                    $"Player{actorNumber}";

                var card = BuildRevealCard(
                    playerName, sab, isMe);
                sabRevealCards.Add(card);
            }

            StartCoroutine(RevealCountdown());
        }

        private VisualElement BuildRevealCard(
            string playerName,
            SabotageCardData sab,
            bool isMe)
        {
            var card = new VisualElement();
            card.AddToClassList("sab-reveal-card");
            card.AddToClassList(
                isMe
                    ? "sab-reveal-card-self"
                    : "sab-reveal-card-enemy");

            var name = new Label(
                playerName.ToUpper());
            name.AddToClassList(
                "sab-reveal-player");
            name.AddToClassList(
                isMe
                    ? "sab-reveal-player-self"
                    : "sab-reveal-player-enemy");
            card.Add(name);

            if (sab?.sabotageIcon != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList(
                    "sab-reveal-icon");
                icon.style.backgroundImage =
                    new StyleBackground(
                        sab.sabotageIcon);
                card.Add(icon);
            }

            var sabName = new Label(
                sab?.sabotageName ?? "Unknown");
            sabName.AddToClassList(
                "sab-reveal-sab-name");
            card.Add(sabName);

            var target = new Label(
                isMe
                    ? "(Your pick)"
                    : "→ TARGETS YOU!");
            target.AddToClassList(
                "sab-reveal-target");
            target.AddToClassList(
                isMe
                    ? "sab-reveal-target-self"
                    : "sab-reveal-target-enemy");
            card.Add(target);

            return card;
        }

        private IEnumerator RevealCountdown()
        {
            float time = 5f;

            while (time > 0f)
            {
                if (sabRevealTimer != null)
                    sabRevealTimer.text =
                        $"Wave starting in: " +
                        $"{Mathf.CeilToInt(time)}s";

                time -= Time.deltaTime;
                yield return null;
            }
        }

        private void HideReveal()
        {
            sabRevealPanel?
                .AddToClassList("hidden");
        }

        private void OnTimeout()
        {
            Debug.Log("[SabotageUI] Timeout");
        }

        private void OnDraftComplete()
        {
            HideAll();
            hasSelected = false;
        }

        private void ShowSabRoot()
        {
            var sabRoot =
                root.Q<VisualElement>(
                    "sabotage-root");
            sabRoot?.RemoveFromClassList("hidden");

            var bg = root.Q<VisualElement>("sab-window");
            StarfieldInjector.Instance?.Register(bg);
        }

        private void HideAll()
        {
            if (root != null)
            {
                var bg = root.Q<VisualElement>("sab-window");
                StarfieldInjector.Instance?.Unregister(bg);
            }

            var sabRoot =
                root.Q<VisualElement>(
                    "sabotage-root");
            sabRoot?.AddToClassList("hidden");
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip, 0.8f);
        }
    }
}
