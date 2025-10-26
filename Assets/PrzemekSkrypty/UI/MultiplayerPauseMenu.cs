using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

namespace ElementumDefense.UI
{
    /// <summary>
    /// Pause menu for multiplayer (no Time.timeScale pause!)
    /// Shows settings and quit options
    /// </summary>
    public class MultiplayerPauseMenu : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject quitConfirmationPanel;

        [Header("Settings UI")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Scene")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private bool isMenuOpen = false;

        private void Start()
        {
            // Hide all panels initially
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);

            // Load settings
            LoadSettings();
        }

        private void Update()
        {
            // Toggle menu with ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isMenuOpen)
                {
                    CloseMenu();
                }
                else
                {
                    OpenMenu();
                }
            }
        }

        #region Menu Control

        /// <summary>
        /// Opens pause menu (NO game pause!)
        /// </summary>
        public void OpenMenu()
        {
            isMenuOpen = true;

            // Show pause panel
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
            }

            // Hide other panels
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);

            // Show cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("[MultiplayerPauseMenu] Menu opened (game continues)");
        }

        /// <summary>
        /// Closes menu and returns to game
        /// </summary>
        public void CloseMenu()
        {
            isMenuOpen = false;

            // Hide all panels
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);

            // Hide cursor (optional - depends on your game)
            // Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;

            Debug.Log("[MultiplayerPauseMenu] Menu closed");
        }

        #endregion

        #region Settings

        /// <summary>
        /// Opens settings panel
        /// </summary>
        public void OpenSettings()
        {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);

            Debug.Log("[MultiplayerPauseMenu] Settings opened");
        }

        /// <summary>
        /// Returns from settings to main pause menu
        /// </summary>
        public void BackFromSettings()
        {
            SaveSettings();

            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        }

        /// <summary>
        /// Volume slider changed
        /// </summary>
        public void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
        }

        /// <summary>
        /// Quality dropdown changed
        /// </summary>
        public void OnQualityChanged(int index)
        {
            QualitySettings.SetQualityLevel(index);
        }

        /// <summary>
        /// Fullscreen toggle changed
        /// </summary>
        public void OnFullscreenChanged(bool isFullscreen)
        {
            Screen.fullScreen = isFullscreen;
        }

        /// <summary>
        /// Loads settings from PlayerPrefs
        /// </summary>
        private void LoadSettings()
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
        }

        /// <summary>
        /// Saves settings to PlayerPrefs
        /// </summary>
        private void SaveSettings()
        {
            if (volumeSlider != null)
                PlayerPrefs.SetFloat("Volume", volumeSlider.value);

            if (qualityDropdown != null)
                PlayerPrefs.SetInt("Quality", qualityDropdown.value);

            if (fullscreenToggle != null)
                PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);

            PlayerPrefs.Save();
            Debug.Log("[MultiplayerPauseMenu] Settings saved");
        }

        #endregion

        #region Quit

        /// <summary>
        /// Opens quit confirmation dialog
        /// </summary>
        public void OpenQuitConfirmation()
        {
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(true);

            Debug.Log("[MultiplayerPauseMenu] Quit confirmation opened");
        }

        /// <summary>
        /// Cancels quit and returns to pause menu
        /// </summary>
        public void CancelQuit()
        {
            if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
        }

        /// <summary>
        /// Confirms quit - disconnects and returns to menu
        /// </summary>
        public void ConfirmQuit()
        {
            Debug.Log("[MultiplayerPauseMenu] Quitting game...");

            // Disconnect from Photon
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }

            // Load main menu
            SceneManager.LoadScene(mainMenuSceneName);
        }

        #endregion
    }
}