using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;
using ElementumDefense.Cards;

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
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip modeSelectSound;

        private AudioSource audioSource;
        private VisualElement root;

        // Elements
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
        // LIFECYCLE
        // ==========================================

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource =
                    gameObject.AddComponent<AudioSource>();
        }

        private void OnEnable()
        {
            if (mainMenuController == null)
                mainMenuController =
                    FindFirstObjectByType<MainMenuController>();
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

            QueryElements();
            BindButtons();
            RefreshDisplay();
        }

        public void Hide()
        {
            StopAllCoroutines();

            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc != null &&
                uiDoc.rootVisualElement != null)
            {
                uiDoc.rootVisualElement.style.display =
                    DisplayStyle.None;
            }

            root = uiDoc?.rootVisualElement;
        }

        // ==========================================
        // QUERY
        // ==========================================

        private void QueryElements()
        {
            btnBack = root.Q<Button>("btn-back");

            modeCasual = root.Q<VisualElement>("mode-casual");
            modeRanked = root.Q<VisualElement>("mode-ranked");
            modeCustom = root.Q<VisualElement>("mode-custom");

            rankName = root.Q<Label>("mp-rank-name");
            eloValue = root.Q<Label>("mp-elo-value");
            rankIcon = root.Q<Label>("mp-rank-icon");
            rankHexBg = root.Q<VisualElement>("mp-rank-hex-bg");
            rankHexBorder = root.Q<VisualElement>("mp-rank-hex-border");

            loadingOverlay = root.Q<VisualElement>("mp-loading-overlay");
            loadingBarFill = root.Q<VisualElement>("mp-loading-bar-fill");
            loadingText = root.Q<Label>("mp-loading-text");
        }

        // ==========================================
        // BIND
        // ==========================================

        private void BindButtons()
        {
            btnBack?.RegisterCallback<ClickEvent>(evt =>
            {
                PlayClick();
                mainMenuController?.BackToMainMenu();
                evt.StopPropagation();
            });

            modeCasual?.RegisterCallback<ClickEvent>(evt =>
            {
                PlayMode();
                SelectMode(GameMode.Casual);
                evt.StopPropagation();
            });

            modeRanked?.RegisterCallback<ClickEvent>(evt =>
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
            var player = PlayerCollection.Instance;
            if (player == null) return;

            string rank = player.GetRankName();
            Color rankColor = player.GetRankColor();
            int elo = player.GetElo();

            if (rankName != null)
            {
                rankName.text = rank;
                rankName.style.color = new StyleColor(rankColor);
            }

            if (eloValue != null)
                eloValue.text = $"{elo} ELO";

            if (rankIcon != null)
            {
                rankIcon.text = GetRankNumeral(elo);
                rankIcon.style.color = new StyleColor(rankColor);
            }

            if (rankHexBg != null)
            {
                Color bg = rankColor;
                bg.a = 0.1f;
                rankHexBg.style.backgroundColor = new StyleColor(bg);
            }

            if (rankHexBorder != null)
            {
                rankHexBorder.style.borderTopColor = new StyleColor(rankColor);
                rankHexBorder.style.borderBottomColor = new StyleColor(rankColor);
                rankHexBorder.style.borderLeftColor = new StyleColor(rankColor);
                rankHexBorder.style.borderRightColor = new StyleColor(rankColor);
            }
        }

        // ==========================================
        // MODE SELECTION
        // ==========================================

        private void SelectMode(GameMode mode)
        {
            var player = PlayerCollection.Instance;
            if (player != null)
                player.SelectedGameMode = mode;

            Debug.Log($"[MultiplayerUI] Selected: {mode}");
            StartCoroutine(LoadSceneSequence(lobbySceneName));
        }

        // ==========================================
        // LOADING
        // ==========================================

        private IEnumerator LoadSceneSequence(string sceneName)
        {
            ShowLoading();

            AsyncOperation asyncLoad =
                SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            while (!asyncLoad.isDone)
            {
                float progress =
                    Mathf.Clamp01(asyncLoad.progress / 0.9f);

                if (loadingBarFill != null)
                    loadingBarFill.style.width =
                        new StyleLength(
                            new Length(progress * 100f, LengthUnit.Percent));

                if (loadingText != null)
                    loadingText.text =
                        $"Loading... {Mathf.RoundToInt(progress * 100)}%";

                if (asyncLoad.progress >= 0.9f)
                {
                    if (loadingBarFill != null)
                        loadingBarFill.style.width =
                            new StyleLength(
                                new Length(100f, LengthUnit.Percent));

                    if (loadingText != null)
                        loadingText.text = "Entering arena...";

                    yield return new WaitForSecondsRealtime(0.5f);
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }
        }

        private void ShowLoading()
        {
            loadingOverlay?.RemoveFromClassList("hidden");

            if (loadingBarFill != null)
                loadingBarFill.style.width =
                    new StyleLength(new Length(0, LengthUnit.Percent));

            if (loadingText != null)
                loadingText.text = "Loading... 0%";
        }

        private void HideLoading()
        {
            loadingOverlay?.AddToClassList("hidden");
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private string GetRankNumeral(int elo)
        {
            if (elo >= 2200) return "V";
            if (elo >= 1800) return "IV";
            if (elo >= 1500) return "III";
            if (elo >= 1200) return "II";
            return "I";
        }

        private void PlayClick()
        {
            if (buttonClickSound != null)
                audioSource?.PlayOneShot(buttonClickSound, 0.7f);
        }

        private void PlayMode()
        {
            if (modeSelectSound != null)
                audioSource?.PlayOneShot(modeSelectSound, 0.8f);
            else
                PlayClick();
        }
    }
}