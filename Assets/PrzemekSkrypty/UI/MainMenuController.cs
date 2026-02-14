using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using ElementumDefense.UI;

/// <summary>
/// Controls main menu UI and scene transitions
/// Enhanced version with animations and polish
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject deckbuilderPanel;
    [SerializeField] private GameObject lootboxPanel;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button multiPlayerButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button deckbuilderButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button lootboxButton;

    [Header("Play Selection UI")]
    [SerializeField] private GameObject playSelectionPanel; // Przypisz nowy panel w Inspektorze
    [SerializeField] private Button playCasualButton;
    [SerializeField] private Button playRankedButton;
    [SerializeField] private Button playCustomButton;
    [SerializeField] private Button playBackButton; // Powrót do Main Menu

    [Header("Rank Display")]
    [SerializeField] private TMP_Text rankText; // Tekst pod przyciskiem Ranked
    [SerializeField] private TMP_Text eloText;

    [Header("Scene Names")]

    [SerializeField, Tooltip("Multiplayer lobby scene name")]
    private string multiPlayerScene = "LobbyScene";

    [Header("Settings UI")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button settingsBackButton; // NEW!

    [Header("Credits UI")]
    [SerializeField] private Button creditsBackButton; // NEW!

    [Header("Deck UI")]
    [SerializeField] private Button deckBackButton;

    [Header("Lootbox UI")]
    [SerializeField] private Button lootboxBackButton;

    [Header("Version Display")]
    [SerializeField] private TMP_Text versionText;
    [SerializeField] private string gameVersion = "0.1.0 Alpha";

    [Header("Loading Screen")]
    [SerializeField] private TMP_Text loadingText; // NEW!
    [SerializeField] private Slider loadingProgressBar; // NEW!

    [Header("Audio (optional)")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip menuMusic;

    private AudioSource audioSource;

    private void Start()
    {
        InitializeMenu();
        LoadSettings();
        PlayMenuMusic();
    }

    /// <summary>
    /// Sets up menu UI and button listeners
    /// </summary>
    private void InitializeMenu()
    {
        // Show main menu panel
        ShowPanel(mainMenuPanel);

        // Setup main menu button listeners
        if (multiPlayerButton != null)
            multiPlayerButton.onClick.AddListener(() => { PlayButtonSound(); StartMultiPlayer(); });

        if (deckbuilderButton != null)
            deckbuilderButton.onClick.AddListener(() => { PlayButtonSound(); OpenDeckbuilder(); });

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => { PlayButtonSound(); OpenSettings(); });

        if (creditsButton != null)
            creditsButton.onClick.AddListener(() => { PlayButtonSound(); OpenCredits(); });

        if (quitButton != null)
            quitButton.onClick.AddListener(() => { PlayButtonSound(); QuitGame(); });

        if (lootboxButton != null)
            lootboxButton.onClick.AddListener(() => { PlayButtonSound(); OpenLootboxMenu(); });

        // Setup settings listeners
        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

        // Setup back buttons
        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(() => { PlayButtonSound(); BackToMainMenu(); });

        if (creditsBackButton != null)
            creditsBackButton.onClick.AddListener(() => { PlayButtonSound(); BackToMainMenu(); });
        
        if (playBackButton != null) 
            playBackButton.onClick.AddListener(BackToMainMenu);

        if (deckBackButton != null)
            deckBackButton.onClick.AddListener(() => { PlayButtonSound(); BackToMainMenu(); });

        if (lootboxBackButton != null)
            lootboxBackButton.onClick.AddListener(() => { PlayButtonSound(); BackToMainMenu(); });

        // Display version
        if (versionText != null)
            versionText.text = $"v{gameVersion}";

        if (playCasualButton != null) playCasualButton.onClick.AddListener(() => SetModeAndPlay(ElementumDefense.Cards.GameMode.Casual));
        if (playRankedButton != null) playRankedButton.onClick.AddListener(() => SetModeAndPlay(ElementumDefense.Cards.GameMode.Ranked));
        // Custom na razie wy³¹czony lub placeholder
        if (playCustomButton != null) playCustomButton.interactable = false;
        
        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();

        Debug.Log($"[MainMenu] Initialized - Version {gameVersion}");
    }

    #region Scene Loading

    /// <summary>
    /// Opens multiplayer lobby
    /// </summary>
    public void StartMultiPlayer()
    {
        Debug.Log("[MainMenu] Opening Play Selection...");
        ShowPanel(playSelectionPanel);
        UpdateRankDisplay();
    }
    private void SetModeAndPlay(ElementumDefense.Cards.GameMode mode)
    {
        // Ustawiamy tryb w singletonie PlayerCollection
        var player = ElementumDefense.Cards.PlayerCollection.Instance;
        if (player != null)
        {
            player.SelectedGameMode = mode;
        }

        Debug.Log($"[MainMenu] Selected mode: {mode}. Loading Lobby...");
        // Teraz ³adujemy scenê lobby (tak jak wczeœniej robi³o to StartMultiPlayer)
        LoadSceneAsync(multiPlayerScene);
    }

    private void UpdateRankDisplay()
    {
        var player = ElementumDefense.Cards.PlayerCollection.Instance;
        if (player != null && rankText != null)
        {
            rankText.text = player.GetRankName();
            rankText.color = player.GetRankColor();

            if (eloText != null) eloText.text = $"{player.GetElo()} ELO";
        }
    }
    /// <summary>
    /// Loads scene with loading screen
    /// </summary>
    private void LoadSceneAsync(string sceneName)
    {
        ShowPanel(loadingPanel);
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Coroutine for async scene loading
    /// </summary>
    private System.Collections.IEnumerator LoadSceneCoroutine(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Update loading bar
        while (!asyncLoad.isDone)
        {
            // Progress goes from 0 to 0.9 during loading
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            // ========== UPDATE UI ==========
            if (loadingProgressBar != null)
            {
                loadingProgressBar.value = progress;
            }

            if (loadingText != null)
            {
                loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
            }
            // ===============================

            // ========== POPRAWKA: Check if almost done ==========
            if (asyncLoad.progress >= 0.9f)
            {
                // Force progress to 100%
                if (loadingProgressBar != null)
                {
                    loadingProgressBar.value = 1f;
                }

                if (loadingText != null)
                {
                    loadingText.text = "Press any key to continue...";
                }

                // ========== NOWE: Auto-continue after delay ==========
                yield return new WaitForSecondsRealtime(0.5f); // Small delay
                asyncLoad.allowSceneActivation = true;
                // =====================================================

                /* STARE: Wait for input (usuñ jeœli chcesz auto-load)
                // Wait for player input
                while (!Input.anyKeyDown)
                {
                    yield return null;
                }

                asyncLoad.allowSceneActivation = true;
                */
            }
            // ====================================================

            yield return null;
        }
    }

    #endregion

    #region Panel Management

    /// <summary>
    /// Opens deckbuilder panel
    /// </summary>
    public void OpenDeckbuilder()
    {
        ShowPanel(deckbuilderPanel);
        Debug.Log("[MainMenu] Opened deckbuilder");
    }
    /// <summary>
    /// Opens settings panel
    /// </summary>
    public void OpenSettings()
    {
        ShowPanel(settingsPanel);
        Debug.Log("[MainMenu] Opened settings");
    }

    /// <summary>
    /// Opens credits panel
    /// </summary>
    public void OpenCredits()
    {
        ShowPanel(creditsPanel);
        Debug.Log("[MainMenu] Opened credits");
    }
    public void OpenLootboxMenu()
    {
        ShowPanel(lootboxPanel);

        LootboxUI lootboxUI = lootboxPanel.GetComponent<LootboxUI>();
        if (lootboxUI != null)
        {
            lootboxUI.OpenLootboxMenu();
        }

        Debug.Log("[MainMenu] Opened Lootbox Menu");
    }

    /// <summary>
    /// Returns to main menu panel
    /// </summary>
    public void BackToMainMenu()
    {
        ShowPanel(mainMenuPanel);
        SaveSettings();
    }

    /// <summary>
    /// Shows specified panel and hides others
    /// </summary>
    private void ShowPanel(GameObject panel)
    {
        mainMenuPanel?.SetActive(panel == mainMenuPanel);
        settingsPanel?.SetActive(panel == settingsPanel);
        creditsPanel?.SetActive(panel == creditsPanel);
        loadingPanel?.SetActive(panel == loadingPanel);
        deckbuilderPanel?.SetActive(panel == deckbuilderPanel);
        playSelectionPanel?.SetActive(panel == playSelectionPanel);
        lootboxPanel?.SetActive(panel == lootboxPanel);
    }

    #endregion

    #region Settings

    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
    }

    public void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index);
    }

    public void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }

    public void LoadSettings()
    {
        if (volumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("Volume", 1f);
            volumeSlider.value = volume;
            AudioListener.volume = volume;
        }

        if (qualityDropdown != null)
        {
            int quality = PlayerPrefs.GetInt("Quality", QualitySettings.GetQualityLevel());
            qualityDropdown.value = quality;
            QualitySettings.SetQualityLevel(quality);
        }

        if (fullscreenToggle != null)
        {
            bool fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            fullscreenToggle.isOn = fullscreen;
            Screen.fullScreen = fullscreen;
        }

        Debug.Log("[MainMenu] Settings loaded");
    }

    private void SaveSettings()
    {
        if (volumeSlider != null)
            PlayerPrefs.SetFloat("Volume", volumeSlider.value);

        if (qualityDropdown != null)
            PlayerPrefs.SetInt("Quality", qualityDropdown.value);

        if (fullscreenToggle != null)
            PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);

        PlayerPrefs.Save();
        Debug.Log("[MainMenu] Settings saved");
    }

    #endregion

    #region Audio
        
    private void PlayMenuMusic()
    {
        if (menuMusic != null && audioSource != null)
        {
            audioSource.clip = menuMusic;
            audioSource.loop = true;
            audioSource.volume = 0.5f;
            audioSource.Play();
        }
    }

    private void PlayButtonSound()
    {
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound, 0.7f);
        }
    }

    #endregion

    #region Application Control

    public void QuitGame()
    {
        Debug.Log("[MainMenu] Quitting game...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    #endregion
    }
    private void OnApplicationQuit()
    {
        SaveSettings();
    }
}