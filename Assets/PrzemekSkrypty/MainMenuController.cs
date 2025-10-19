//using UnityEngine;
//using UnityEngine.SceneManagement;
//using UnityEngine.UI;
//using TMPro;

///// <summary>
///// Controls main menu UI and scene transitions
///// Entry point of the game
///// </summary>
//public class MainMenuController : MonoBehaviour
//{
//    [Header("UI Panels")]
//    [SerializeField] private GameObject mainMenuPanel;
//    [SerializeField] private GameObject settingsPanel;
//    [SerializeField] private GameObject creditsPanel;

//    [Header("Buttons")]
//    [SerializeField] private Button singlePlayerButton;
//    [SerializeField] private Button multiPlayerButton;
//    [SerializeField] private Button settingsButton;
//    [SerializeField] private Button creditsButton;
//    [SerializeField] private Button quitButton;

//    [Header("Scene Names")]
//    [SerializeField, Tooltip("Single player game scene name")]
//    private string singlePlayerScene = "GameScene_SP";

//    [SerializeField, Tooltip("Multiplayer lobby scene name")]
//    private string multiPlayerScene = "LobbyScene";

//    [Header("Settings")]
//    [SerializeField] private Slider volumeSlider;
//    [SerializeField] private TMP_Dropdown qualityDropdown;
//    [SerializeField] private Toggle fullscreenToggle;

//    [Header("Version Display")]
//    [SerializeField] private TMP_Text versionText;
//    [SerializeField] private string gameVersion = "0.1.0 Alpha";

//    private void Start()
//    {
//        InitializeMenu();
//        LoadSettings();
//    }

//    /// <summary>
//    /// Sets up menu UI and button listeners
//    /// </summary>
//    private void InitializeMenu()
//    {
//        // Show main menu panel
//        ShowPanel(mainMenuPanel);

//        // Setup button listeners
//        if (singlePlayerButton != null)
//            singlePlayerButton.onClick.AddListener(StartSinglePlayer);

//        if (multiPlayerButton != null)
//            multiPlayerButton.onClick.AddListener(StartMultiPlayer);

//        if (settingsButton != null)
//            settingsButton.onClick.AddListener(OpenSettings);

//        if (creditsButton != null)
//            creditsButton.onClick.AddListener(OpenCredits);

//        if (quitButton != null)
//            quitButton.onClick.AddListener(QuitGame);

//        // Setup settings listeners
//        if (volumeSlider != null)
//            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

//        if (qualityDropdown != null)
//            qualityDropdown.onValueChanged.AddListener(OnQualityChanged);

//        if (fullscreenToggle != null)
//            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

//        // Display version
//        if (versionText != null)
//            versionText.text = $"v{gameVersion}";

//        Debug.Log($"[MainMenu] Initialized - Version {gameVersion}");
//    }

//    #region Scene Loading

//    /// <summary>
//    /// Starts single player game
//    /// </summary>
//    public void StartSinglePlayer()
//    {
//        Debug.Log("[MainMenu] Starting single player...");
//        LoadScene(singlePlayerScene);
//    }

//    /// <summary>
//    /// Opens multiplayer lobby
//    /// </summary>
//    public void StartMultiPlayer()
//    {
//        Debug.Log("[MainMenu] Starting multiplayer...");
//        LoadScene(multiPlayerScene);
//    }

//    /// <summary>
//    /// Loads specified scene with loading screen (optional)
//    /// </summary>
//    private void LoadScene(string sceneName)
//    {
//        // TODO: Show loading screen
//        SceneManager.LoadScene(sceneName);
//    }

//    #endregion

//    #region Panel Management

//    /// <summary>
//    /// Opens settings panel
//    /// </summary>
//    public void OpenSettings()
//    {
//        ShowPanel(settingsPanel);
//        Debug.Log("[MainMenu] Opened settings");
//    }

//    /// <summary>
//    /// Opens credits panel
//    /// </summary>
//    public void OpenCredits()
//    {
//        ShowPanel(creditsPanel);
//        Debug.Log("[MainMenu] Opened credits");
//    }

//    /// <summary>
//    /// Returns to main menu panel
//    /// </summary>
//    public void BackToMainMenu()
//    {
//        ShowPanel(mainMenuPanel);
//        SaveSettings(); // Save settings when returning to main menu
//    }

//    /// <summary>
//    /// Shows specified panel and hides others
//    /// </summary>
//    private void ShowPanel(GameObject panel)
//    {
//        mainMenuPanel?.SetActive(panel == mainMenuPanel);
//        settingsPanel?.SetActive(panel == settingsPanel);
//        creditsPanel?.SetActive(panel == creditsPanel);
//    }

//    #endregion

//    #region Settings

//    /// <summary>
//    /// Called when volume slider changes
//    /// </summary>
//    private void OnVolumeChanged(float value)
//    {
//        AudioListener.volume = value;
//        Debug.Log($"[MainMenu] Volume set to {value}");
//    }

//    /// <summary>
//    /// Called when quality dropdown changes
//    /// </summary>
//    private void OnQualityChanged(int index)
//    {
//        QualitySettings.SetQualityLevel(index);
//        Debug.Log($"[MainMenu] Quality set to {QualitySettings.names[index]}");
//    }

//    /// <summary>
//    /// Called when fullscreen toggle changes
//    /// </summary>
//    private void OnFullscreenChanged(bool isFullscreen)
//    {
//        Screen.fullScreen = isFullscreen;
//        Debug.Log($"[MainMenu] Fullscreen: {isFullscreen}");
//    }

//    /// <summary>
//    /// Loads saved settings from PlayerPrefs
//    /// </summary>
//    private void LoadSettings()
//    {
//        // Volume
//        if (volumeSlider != null)
//        {
//            float volume = PlayerPrefs.GetFloat("Volume", 1f);
//            volumeSlider.value = volume;
//            AudioListener.volume = volume;
//        }

//        // Quality
//        if (qualityDropdown != null)
//        {
//            int quality = PlayerPrefs.GetInt("Quality", QualitySettings.GetQualityLevel());
//            qualityDropdown.value = quality;
//            QualitySettings.SetQualityLevel(quality);
//        }

//        // Fullscreen
//        if (fullscreenToggle != null)
//        {
//            bool fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
//            fullscreenToggle.isOn = fullscreen;
//            Screen.fullScreen = fullscreen;
//        }

//        Debug.Log("[MainMenu] Settings loaded");
//    }

//    /// <summary>
//    /// Saves current settings to PlayerPrefs
//    /// </summary>
//    private void SaveSettings()
//    {
//        if (volumeSlider != null)
//            PlayerPrefs.SetFloat("Volume", volumeSlider.value);

//        if (qualityDropdown != null)
//            PlayerPrefs.SetInt("Quality", qualityDropdown.value);

//        if (fullscreenToggle != null)
//            PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);

//        PlayerPrefs.Save();
//        Debug.Log("[MainMenu] Settings saved");
//    }

//    #endregion

//    #region Application Control

//    /// <summary>
//    /// Quits the game
//    /// </summary>
//    public void QuitGame()
//    {
//        Debug.Log("[MainMenu] Quitting game...");

//#if UNITY_EDITOR
//        UnityEditor.EditorApplication.isPlaying = false;
//#else
//        Application.Quit();
//#endif
//    }

//    #endregion

//    private void OnApplicationQuit()
//    {
//        SaveSettings();
//    }
//}