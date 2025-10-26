using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private GameObject loadingPanel; // NEW!

    [Header("Main Menu Buttons")]
    [SerializeField] private Button multiPlayerButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;

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

        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => { PlayButtonSound(); OpenSettings(); });

        if (creditsButton != null)
            creditsButton.onClick.AddListener(() => { PlayButtonSound(); OpenCredits(); });

        if (quitButton != null)
            quitButton.onClick.AddListener(() => { PlayButtonSound(); QuitGame(); });

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

        // Display version
        if (versionText != null)
            versionText.text = $"v{gameVersion}";

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
        Debug.Log("[MainMenu] Starting multiplayer...");
        LoadSceneAsync(multiPlayerScene);
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