using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Photon.Pun;
using System.Collections.Generic;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MultiplayerPauseMenu : MonoBehaviour
    {
        [Header("Scene")]
        [SerializeField]
        private string mainMenuSceneName = "MainMenu";

        [Header("Audio")]
        [SerializeField] private AudioClip buttonClickSound;

        private AudioSource audioSource;

        // Root
        private VisualElement root;
        private VisualElement pauseRoot;

        // Views
        private VisualElement menuView;
        private VisualElement settingsView;
        private VisualElement quitView;

        // Buttons
        private Button btnResume;
        private Button btnSettings;
        private Button btnQuit;
        private Button btnBackSettings;
        private Button btnCancelQuit;
        private Button btnConfirmQuit;

        // Settings controls
        private Slider volumeSlider;
        private Label volumeValue;
        private DropdownField qualityDropdown;
        private Toggle fullscreenToggle;

        private bool isMenuOpen;

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
            BindCallbacks();
            LoadSettings();
            InitQualityDropdown();
            HideMenu();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isMenuOpen)
                    HideMenu();
                else
                    ShowMenu();
            }
        }

        // ==========================================
        // QUERY
        // ==========================================

        private void QueryElements()
        {
            pauseRoot =
                root.Q<VisualElement>("pause-root");

            menuView =
                root.Q<VisualElement>(
                    "pause-menu-view");
            settingsView =
                root.Q<VisualElement>(
                    "pause-settings-view");
            quitView =
                root.Q<VisualElement>(
                    "pause-quit-view");

            btnResume =
                root.Q<Button>("btn-resume");
            btnSettings =
                root.Q<Button>("btn-settings");
            btnQuit =
                root.Q<Button>("btn-quit");
            btnBackSettings =
                root.Q<Button>("btn-back-settings");
            btnCancelQuit =
                root.Q<Button>("btn-cancel-quit");
            btnConfirmQuit =
                root.Q<Button>("btn-confirm-quit");

            volumeSlider =
                root.Q<Slider>("volume-slider");
            volumeValue =
                root.Q<Label>("volume-value");
            qualityDropdown =
                root.Q<DropdownField>(
                    "quality-dropdown");
            fullscreenToggle =
                root.Q<Toggle>("fullscreen-toggle");
        }

        // ==========================================
        // CALLBACKS
        // ==========================================

        private void BindCallbacks()
        {
            btnResume?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    PlayClick();
                    HideMenu();
                    evt.StopPropagation();
                });

            btnSettings?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    PlayClick();
                    ShowView(settingsView);
                    evt.StopPropagation();
                });

            btnQuit?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    PlayClick();
                    ShowView(quitView);
                    evt.StopPropagation();
                });

            btnBackSettings?
                .RegisterCallback<ClickEvent>(evt =>
                {
                    PlayClick();
                    SaveSettings();
                    ShowView(menuView);
                    evt.StopPropagation();
                });

            btnCancelQuit?
                .RegisterCallback<ClickEvent>(evt =>
                {
                    PlayClick();
                    ShowView(menuView);
                    evt.StopPropagation();
                });

            btnConfirmQuit?
                .RegisterCallback<ClickEvent>(evt =>
                {
                    PlayClick();
                    ConfirmQuit();
                    evt.StopPropagation();
                });

            // Volume slider
            volumeSlider?
                .RegisterValueChangedCallback(evt =>
                {
                    float val = evt.newValue;
                    AudioListener.volume = val;

                    if (volumeValue != null)
                        volumeValue.text =
                            Mathf.RoundToInt(
                                val * 100f) + "%";
                });

            // Quality dropdown
            qualityDropdown?
                .RegisterValueChangedCallback(evt =>
                {
                    int idx =
                        qualityDropdown.index;
                    QualitySettings
                        .SetQualityLevel(idx);
                });

            // Fullscreen toggle
            fullscreenToggle?
                .RegisterValueChangedCallback(evt =>
                {
                    Screen.fullScreen = evt.newValue;
                });
        }

        // ==========================================
        // QUALITY DROPDOWN INIT
        // ==========================================

        private void InitQualityDropdown()
        {
            if (qualityDropdown == null) return;

            var names =
                QualitySettings.names;
            var choices = new List<string>(names);

            qualityDropdown.choices = choices;
            qualityDropdown.index =
                QualitySettings.GetQualityLevel();
        }

        // ==========================================
        // VIEW MANAGEMENT
        // ==========================================

        private void ShowView(VisualElement view)
        {
            menuView?.AddToClassList("hidden");
            settingsView?.AddToClassList("hidden");
            quitView?.AddToClassList("hidden");

            view?.RemoveFromClassList("hidden");
        }

        // ==========================================
        // SHOW / HIDE MENU
        // ==========================================

        public void ShowMenu()
        {
            isMenuOpen = true;
            pauseRoot?.RemoveFromClassList("hidden");
            ShowView(menuView);

            //Cursor.lockState = CursorLockMode.None;
            //Cursor.visible = true;

            Debug.Log(
                "[PauseMenu] Opened (game continues)");
        }

        public void HideMenu()
        {
            isMenuOpen = false;
            SaveSettings();
            pauseRoot?.AddToClassList("hidden");

            Debug.Log("[PauseMenu] Closed");
        }

        public bool IsOpen() => isMenuOpen;

        // ==========================================
        // SETTINGS LOAD / SAVE
        // ==========================================

        private void LoadSettings()
        {
            // Volume
            float vol = PlayerPrefs.GetFloat(
                "Volume", 1f);
            AudioListener.volume = vol;

            if (volumeSlider != null)
                volumeSlider.value = vol;
            if (volumeValue != null)
                volumeValue.text =
                    Mathf.RoundToInt(vol * 100f) + "%";

            // Quality
            int quality = PlayerPrefs.GetInt(
                "Quality",
                QualitySettings.GetQualityLevel());
            QualitySettings.SetQualityLevel(quality);

            // Fullscreen
            bool fs = PlayerPrefs.GetInt(
                "Fullscreen",
                Screen.fullScreen ? 1 : 0) == 1;
            Screen.fullScreen = fs;

            if (fullscreenToggle != null)
                fullscreenToggle.value = fs;
        }

        private void SaveSettings()
        {
            if (volumeSlider != null)
                PlayerPrefs.SetFloat(
                    "Volume", volumeSlider.value);

            PlayerPrefs.SetInt(
                "Quality",
                QualitySettings.GetQualityLevel());

            if (fullscreenToggle != null)
                PlayerPrefs.SetInt(
                    "Fullscreen",
                    fullscreenToggle.value ? 1 : 0);

            PlayerPrefs.Save();
            Debug.Log("[PauseMenu] Settings saved");
        }

        // ==========================================
        // QUIT
        // ==========================================

        private void ConfirmQuit()
        {
            Debug.Log("[PauseMenu] Leaving match (becoming inactive for reconnect)...");

            // Start the reconnect window NOW (from the leave moment) so a long
            // match doesn't arrive at the menu with an already-expired window.
            ElementumDefense.Multiplayer.PendingMatchState.RefreshWindow();

            // Leave the room as INACTIVE (slot reserved by PlayerTtl). This is
            // crucial: staying in the room means buffered instantiates are NOT
            // re-delivered on reconnect, so the player would miss the opponent's
            // object and orphan their own (causing duplicate players). Becoming
            // inactive + RejoinRoom later gives a clean buffer re-delivery.
            if (PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom(true);

            SceneManager.LoadScene(mainMenuSceneName);
        }

        // ==========================================
        // AUDIO
        // ==========================================

        private void PlayClick()
        {
            if (buttonClickSound != null &&
                audioSource != null)
                audioSource.PlayOneShot(
                    buttonClickSound, 0.7f);
        }
    }
}//using UnityEngine;
 //using UnityEngine.SceneManagement;
 //using UnityEngine.UI;
 //using TMPro;
 //using Photon.Pun;

//namespace ElementumDefense.UI
//{
//    /// <summary>
//    /// Pause menu for multiplayer (no Time.timeScale pause!)
//    /// Shows settings and quit options
//    /// </summary>
//    public class MultiplayerPauseMenu : MonoBehaviour
//    {
//        [Header("UI References")]
//        [SerializeField] private GameObject pauseMenuPanel;
//        [SerializeField] private GameObject settingsPanel;
//        [SerializeField] private GameObject quitConfirmationPanel;

//        [Header("Settings UI")]
//        [SerializeField] private Slider volumeSlider;
//        [SerializeField] private TMP_Dropdown qualityDropdown;
//        [SerializeField] private Toggle fullscreenToggle;

//        [Header("Scene")]
//        [SerializeField] private string mainMenuSceneName = "MainMenu";

//        private bool isMenuOpen = false;

//        private void Start()
//        {
//            // Hide all panels initially
//            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
//            if (settingsPanel != null) settingsPanel.SetActive(false);
//            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);

//            // Load settings
//            LoadSettings();
//        }

//        private void Update()
//        {
//            // Toggle menu with ESC
//            if (Input.GetKeyDown(KeyCode.Escape))
//            {
//                if (isMenuOpen)
//                {
//                    CloseMenu();
//                }
//                else
//                {
//                    OpenMenu();
//                }
//            }
//        }

//        #region Menu Control

//        /// <summary>
//        /// Opens pause menu (NO game pause!)
//        /// </summary>
//        public void OpenMenu()
//        {
//            isMenuOpen = true;

//            // Show pause panel
//            if (pauseMenuPanel != null)
//            {
//                pauseMenuPanel.SetActive(true);
//            }

//            // Hide other panels
//            if (settingsPanel != null) settingsPanel.SetActive(false);
//            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);

//            // Show cursor
//            Cursor.lockState = CursorLockMode.None;
//            Cursor.visible = true;

//            Debug.Log("[MultiplayerPauseMenu] Menu opened (game continues)");
//        }

//        /// <summary>
//        /// Closes menu and returns to game
//        /// </summary>
//        public void CloseMenu()
//        {
//            isMenuOpen = false;

//            // Hide all panels
//            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
//            if (settingsPanel != null) settingsPanel.SetActive(false);
//            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);

//            // Hide cursor (optional - depends on your game)
//            // Cursor.lockState = CursorLockMode.Locked;
//            // Cursor.visible = false;

//            Debug.Log("[MultiplayerPauseMenu] Menu closed");
//        }

//        #endregion

//        #region Settings

//        /// <summary>
//        /// Opens settings panel
//        /// </summary>
//        public void OpenSettings()
//        {
//            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
//            if (settingsPanel != null) settingsPanel.SetActive(true);

//            Debug.Log("[MultiplayerPauseMenu] Settings opened");
//        }

//        /// <summary>
//        /// Returns from settings to main pause menu
//        /// </summary>
//        public void BackFromSettings()
//        {
//            SaveSettings();

//            if (settingsPanel != null) settingsPanel.SetActive(false);
//            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
//        }

//        /// <summary>
//        /// Volume slider changed
//        /// </summary>
//        public void OnVolumeChanged(float value)
//        {
//            AudioListener.volume = value;
//        }

//        /// <summary>
//        /// Quality dropdown changed
//        /// </summary>
//        public void OnQualityChanged(int index)
//        {
//            QualitySettings.SetQualityLevel(index);
//        }

//        /// <summary>
//        /// Fullscreen toggle changed
//        /// </summary>
//        public void OnFullscreenChanged(bool isFullscreen)
//        {
//            Screen.fullScreen = isFullscreen;
//        }

//        /// <summary>
//        /// Loads settings from PlayerPrefs
//        /// </summary>
//        private void LoadSettings()
//        {
//            if (volumeSlider != null)
//            {
//                float volume = PlayerPrefs.GetFloat("Volume", 1f);
//                volumeSlider.value = volume;
//                AudioListener.volume = volume;
//            }

//            if (qualityDropdown != null)
//            {
//                int quality = PlayerPrefs.GetInt("Quality", QualitySettings.GetQualityLevel());
//                qualityDropdown.value = quality;
//                QualitySettings.SetQualityLevel(quality);
//            }

//            if (fullscreenToggle != null)
//            {
//                bool fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
//                fullscreenToggle.isOn = fullscreen;
//                Screen.fullScreen = fullscreen;
//            }
//        }

//        /// <summary>
//        /// Saves settings to PlayerPrefs
//        /// </summary>
//        private void SaveSettings()
//        {
//            if (volumeSlider != null)
//                PlayerPrefs.SetFloat("Volume", volumeSlider.value);

//            if (qualityDropdown != null)
//                PlayerPrefs.SetInt("Quality", qualityDropdown.value);

//            if (fullscreenToggle != null)
//                PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);

//            PlayerPrefs.Save();
//            Debug.Log("[MultiplayerPauseMenu] Settings saved");
//        }

//        #endregion

//        #region Quit

//        /// <summary>
//        /// Opens quit confirmation dialog
//        /// </summary>
//        public void OpenQuitConfirmation()
//        {
//            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
//            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(true);

//            Debug.Log("[MultiplayerPauseMenu] Quit confirmation opened");
//        }

//        /// <summary>
//        /// Cancels quit and returns to pause menu
//        /// </summary>
//        public void CancelQuit()
//        {
//            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);
//            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
//        }

//        /// <summary>
//        /// Confirms quit - disconnects and returns to menu
//        /// </summary>
//        public void ConfirmQuit()
//        {
//            Debug.Log("[MultiplayerPauseMenu] Quitting game...");

//            // Disconnect from Photon
//            if (PhotonNetwork.IsConnected)
//            {
//                PhotonNetwork.Disconnect();
//            }

//            // Load main menu
//            SceneManager.LoadScene(mainMenuSceneName);
//        }

//        #endregion
//    }
//}