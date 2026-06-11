using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using ElementumDefense.Cards;
using ElementumDefense.Multiplayer;
using Photon.Pun;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class PreGameUI : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioClip selectSound;
        [SerializeField] private AudioClip tickSound;

        private AudioSource audioSource;
        private VisualElement root;

        private Label arenaType;
        private Label timerValue;
        private VisualElement deckList;
        private VisualElement waitingSection;
        private Label waitingText;

        private bool isDeckSelected = false;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource =
                    gameObject.AddComponent<AudioSource>();
        }

        // ==========================================
        // SHOW / HIDE
        // ==========================================

        public void Show()
        {
            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null) return;

            uiDoc.enabled = true;
            gameObject.SetActive(true);
            root = uiDoc.rootVisualElement;
            if (root == null) return;

            root.style.display = DisplayStyle.Flex;
            isDeckSelected = false;

            QueryElements();
            PopulateDecks();
            ShowDeckSelection();

            var bg = root.Q<VisualElement>("pregame-root");
            StarfieldInjector.Instance?.Register(bg);
        }

        public void Hide()
        {
            if (root != null)
            {
                var bg = root.Q<VisualElement>("pregame-root");
                StarfieldInjector.Instance?.Unregister(bg);
            }

            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc != null &&
                uiDoc.rootVisualElement != null)
                uiDoc.rootVisualElement.style.display =
                    DisplayStyle.None;
        }

        private void QueryElements()
        {
            arenaType =
                root.Q<Label>("arena-type");
            timerValue =
                root.Q<Label>("pregame-timer");
            deckList =
                root.Q<VisualElement>(
                    "deck-selection-list");
            waitingSection =
                root.Q<VisualElement>(
                    "waiting-section");
            waitingText =
                root.Q<Label>("waiting-text");
        }

        // ==========================================
        // ARENA
        // ==========================================

        public void SetArenaType(string arena)
        {
            if (arenaType != null)
                arenaType.text =
                    $"{arena.ToUpper()} ARENA";
        }

        // ==========================================
        // TIMER
        // ==========================================

        public void UpdateTimer(float remaining)
        {
            if (isDeckSelected) return;

            if (timerValue != null)
            {
                timerValue.text =
                    Mathf.CeilToInt(remaining)
                        .ToString();

                if (remaining <= 5f)
                    timerValue.AddToClassList(
                        "timer-critical");
                else
                    timerValue.RemoveFromClassList(
                        "timer-critical");
            }
        }

        // ==========================================
        // DECK POPULATION
        // ==========================================

        private void PopulateDecks()
        {
            if (deckList == null) return;
            deckList.Clear();

            var player = PlayerCollection.Instance;
            if (player == null) return;

            List<DeckData> decks =
                player.GetPlayerDecks();

            if (decks.Count == 0)
            {
                var empty = new Label("NO DECKS FOUND");
                empty.style.color =
                    new StyleColor(
                        new Color(0.97f, 0.44f, 0.44f));
                empty.style.fontSize = 12;
                empty.style.letterSpacing = 4;
                empty.style.unityTextAlign =
                    TextAnchor.MiddleCenter;
                deckList.Add(empty);
                return;
            }

            foreach (var deck in decks)
            {
                var item = BuildDeckItem(deck);
                deckList.Add(item);
            }
        }

        private VisualElement BuildDeckItem(DeckData deck)
        {
            var item = new VisualElement();
            item.AddToClassList("deck-sel-item");

            var icon = new Label("✦");
            icon.AddToClassList("deck-sel-icon");
            item.Add(icon);

            var info = new VisualElement();
            info.AddToClassList("deck-sel-info");

            var name = new Label(deck.deckName);
            name.AddToClassList("deck-sel-name");
            info.Add(name);

            var count = new Label(
                $"{deck.cards.Count} cards");
            count.AddToClassList("deck-sel-count");
            info.Add(count);

            item.Add(info);

            var arrow = new Label("→");
            arrow.AddToClassList("deck-sel-arrow");
            item.Add(arrow);

            var deckRef = deck;
            item.RegisterCallback<ClickEvent>(evt =>
            {
                if (isDeckSelected) return;
                PlaySound(selectSound);
                OnDeckClicked(deckRef, item);
                evt.StopPropagation();
            });

            return item;
        }

        private void OnDeckClicked(
            DeckData deck, VisualElement selectedItem)
        {
            if (isDeckSelected) return;
            isDeckSelected = true;

            // Highlight selected
            selectedItem.AddToClassList(
                "deck-sel-item-selected");

            // Dim others
            deckList.Query<VisualElement>(
                className: "deck-sel-item")
                .ForEach(el =>
                {
                    if (el != selectedItem)
                        el.style.opacity = 0.3f;
                });

            // Notify PreGameManager
            var pgm = PreGameManager.Instance;
            if (pgm != null)
                pgm.OnDeckSelectedFromUI(deck);

            ShowWaiting();
        }

        // ==========================================
        // WAITING STATE
        // ==========================================

        private void ShowDeckSelection()
        {
            waitingSection?.AddToClassList("hidden");
        }

        public void ShowWaiting()
        {
            waitingSection?
                .RemoveFromClassList("hidden");

            if (timerValue != null)
                timerValue.text = "—";
        }

        public void SetWaitingText(string text)
        {
            if (waitingText != null)
                waitingText.text = text;
        }

        // ==========================================
        // AUTO SELECT
        // ==========================================

        public void AutoSelectFirstDeck()
        {
            if (isDeckSelected) return;

            var player = PlayerCollection.Instance;
            if (player == null) return;

            var decks = player.GetPlayerDecks();
            if (decks.Count > 0)
            {
                var firstItem = deckList?.Q<VisualElement>(
                    className: "deck-sel-item");
                OnDeckClicked(decks[0], firstItem);
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip, 0.7f);
        }
    }
}