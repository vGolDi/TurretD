using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class SettingsUI : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField]
        private MainMenuController mainMenuController;

        [Header("Audio")]
        [SerializeField] private AudioClip buttonClickSound;

        private AudioSource audioSource;
        private VisualElement root;

        // Audio
        private Slider sliderMasterVolume;
        private Slider sliderMusicVolume;
        private Slider sliderSfxVolume;
        private Label labelMasterVolume;
        private Label labelMusicVolume;
        private Label labelSfxVolume;

        // Graphics
        private DropdownField dropdownQuality;
        private DropdownField dropdownResolution;
        private Toggle toggleFullscreen;
        private Toggle toggleVsync;

        // Gameplay
        private Toggle toggleCameraShake;
        private Toggle toggleDamageNumbers;
        private Toggle toggleCardAnimations;

        // Buttons
        private Button btnBack;
        private Button btnApply;
        private Button btnReset;

        // Resolution cache
        private Resolution[] availableResolutions;

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
            BindControls();
            LoadSettings();
        }

        public void Hide()
        {
            SaveSettings();

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
            // Audio
            sliderMasterVolume =
                root.Q<Slider>("slider-master-volume");
            sliderMusicVolume =
                root.Q<Slider>("slider-music-volume");
            sliderSfxVolume =
                root.Q<Slider>("slider-sfx-volume");
            labelMasterVolume =
                root.Q<Label>("label-master-volume");
            labelMusicVolume =
                root.Q<Label>("label-music-volume");
            labelSfxVolume =
                root.Q<Label>("label-sfx-volume");

            // Graphics
            dropdownQuality =
                root.Q<DropdownField>("dropdown-quality");
            dropdownResolution =
                root.Q<DropdownField>("dropdown-resolution");
            toggleFullscreen =
                root.Q<Toggle>("toggle-fullscreen");
            toggleVsync =
                root.Q<Toggle>("toggle-vsync");

            // Gameplay
            toggleCameraShake =
                root.Q<Toggle>("toggle-camera-shake");
            toggleDamageNumbers =
                root.Q<Toggle>("toggle-damage-numbers");
            toggleCardAnimations =
                root.Q<Toggle>("toggle-card-animations");

            // Buttons
            btnBack = root.Q<Button>("btn-back");
            btnApply = root.Q<Button>("btn-apply");
            btnReset = root.Q<Button>("btn-reset");
        }

        // ==========================================
        // BIND
        // ==========================================

        private void BindControls()
        {
            // Back
            btnBack?.RegisterCallback<ClickEvent>(evt =>
            {
                PlayClick();
                SaveSettings();
                mainMenuController?.BackToMainMenu();
                evt.StopPropagation();
            });

            // Apply
            btnApply?.RegisterCallback<ClickEvent>(evt =>
            {
                PlayClick();
                ApplySettings();
                evt.StopPropagation();
            });

            // Reset
            btnReset?.RegisterCallback<ClickEvent>(evt =>
            {
                PlayClick();
                ResetDefaults();
                evt.StopPropagation();
            });

            // Audio sliders
            sliderMasterVolume?
                .RegisterValueChangedCallback(evt =>
                {
                    AudioListener.volume = evt.newValue;
                    UpdateSliderLabel(
                        labelMasterVolume, evt.newValue);
                });

            sliderMusicVolume?
                .RegisterValueChangedCallback(evt =>
                {
                    UpdateSliderLabel(
                        labelMusicVolume, evt.newValue);
                });

            sliderSfxVolume?
                .RegisterValueChangedCallback(evt =>
                {
                    UpdateSliderLabel(
                        labelSfxVolume, evt.newValue);
                });

            // Quality dropdown
            SetupQualityDropdown();

            // Resolution dropdown
            SetupResolutionDropdown();

            // Fullscreen
            toggleFullscreen?
                .RegisterValueChangedCallback(evt =>
                {
                    Screen.fullScreen = evt.newValue;
                });

            // VSync
            toggleVsync?
                .RegisterValueChangedCallback(evt =>
                {
                    QualitySettings.vSyncCount =
                        evt.newValue ? 1 : 0;
                });
        }

        // ==========================================
        // DROPDOWNS SETUP
        // ==========================================

        private void SetupQualityDropdown()
        {
            if (dropdownQuality == null) return;

            var names = QualitySettings.names.ToList();
            dropdownQuality.choices = names;
            dropdownQuality.index =
                QualitySettings.GetQualityLevel();

            dropdownQuality
                .RegisterValueChangedCallback(evt =>
                {
                    int idx = dropdownQuality.index;
                    QualitySettings.SetQualityLevel(idx);
                });
        }

        private void SetupResolutionDropdown()
        {
            if (dropdownResolution == null) return;

            availableResolutions = Screen.resolutions
                .GroupBy(r => new { r.width, r.height })
                .Select(g => g.Last())
                .ToArray();

            var choices = new List<string>();
            int currentIdx = 0;

            for (int i = 0; i < availableResolutions.Length; i++)
            {
                var r = availableResolutions[i];
                choices.Add($"{r.width} x {r.height}");

                if (r.width == Screen.currentResolution.width &&
                    r.height == Screen.currentResolution.height)
                {
                    currentIdx = i;
                }
            }

            dropdownResolution.choices = choices;
            dropdownResolution.index = currentIdx;

            dropdownResolution
                .RegisterValueChangedCallback(evt =>
                {
                    int idx = dropdownResolution.index;
                    if (idx >= 0 &&
                        idx < availableResolutions.Length)
                    {
                        var res = availableResolutions[idx];
                        Screen.SetResolution(
                            res.width, res.height,
                            Screen.fullScreen);
                    }
                });
        }

        // ==========================================
        // SETTINGS PERSISTENCE
        // ==========================================

        private void LoadSettings()
        {
            // Audio
            float master =
                PlayerPrefs.GetFloat("MasterVolume", 1f);
            float music =
                PlayerPrefs.GetFloat("MusicVolume", 0.5f);
            float sfx =
                PlayerPrefs.GetFloat("SfxVolume", 0.7f);

            if (sliderMasterVolume != null)
            {
                sliderMasterVolume.value = master;
                AudioListener.volume = master;
                UpdateSliderLabel(labelMasterVolume, master);
            }

            if (sliderMusicVolume != null)
            {
                sliderMusicVolume.value = music;
                UpdateSliderLabel(labelMusicVolume, music);
            }

            if (sliderSfxVolume != null)
            {
                sliderSfxVolume.value = sfx;
                UpdateSliderLabel(labelSfxVolume, sfx);
            }

            // Graphics
            if (dropdownQuality != null)
            {
                int quality = PlayerPrefs.GetInt(
                    "Quality",
                    QualitySettings.GetQualityLevel());
                dropdownQuality.index = quality;
                QualitySettings.SetQualityLevel(quality);
            }

            if (toggleFullscreen != null)
            {
                bool fs = PlayerPrefs.GetInt(
                    "Fullscreen",
                    Screen.fullScreen ? 1 : 0) == 1;
                toggleFullscreen.value = fs;
                Screen.fullScreen = fs;
            }

            if (toggleVsync != null)
            {
                int vsync = PlayerPrefs.GetInt(
                    "VSync",
                    QualitySettings.vSyncCount);
                toggleVsync.value = vsync > 0;
                QualitySettings.vSyncCount = vsync;
            }

            // Gameplay
            if (toggleCameraShake != null)
                toggleCameraShake.value =
                    PlayerPrefs.GetInt(
                        "CameraShake", 1) == 1;

            if (toggleDamageNumbers != null)
                toggleDamageNumbers.value =
                    PlayerPrefs.GetInt(
                        "DamageNumbers", 1) == 1;

            if (toggleCardAnimations != null)
                toggleCardAnimations.value =
                    PlayerPrefs.GetInt(
                        "CardAnimations", 1) == 1;

            Debug.Log("[SettingsUI] Settings loaded");
        }

        public void SaveSettings()
        {
            // Audio
            if (sliderMasterVolume != null)
                PlayerPrefs.SetFloat(
                    "MasterVolume",
                    sliderMasterVolume.value);

            if (sliderMusicVolume != null)
                PlayerPrefs.SetFloat(
                    "MusicVolume",
                    sliderMusicVolume.value);

            if (sliderSfxVolume != null)
                PlayerPrefs.SetFloat(
                    "SfxVolume",
                    sliderSfxVolume.value);

            // Graphics
            if (dropdownQuality != null)
                PlayerPrefs.SetInt(
                    "Quality",
                    dropdownQuality.index);

            if (toggleFullscreen != null)
                PlayerPrefs.SetInt(
                    "Fullscreen",
                    toggleFullscreen.value ? 1 : 0);

            if (toggleVsync != null)
                PlayerPrefs.SetInt(
                    "VSync",
                    toggleVsync.value ? 1 : 0);

            // Gameplay
            if (toggleCameraShake != null)
                PlayerPrefs.SetInt(
                    "CameraShake",
                    toggleCameraShake.value ? 1 : 0);

            if (toggleDamageNumbers != null)
                PlayerPrefs.SetInt(
                    "DamageNumbers",
                    toggleDamageNumbers.value ? 1 : 0);

            if (toggleCardAnimations != null)
                PlayerPrefs.SetInt(
                    "CardAnimations",
                    toggleCardAnimations.value ? 1 : 0);

            PlayerPrefs.Save();
            Debug.Log("[SettingsUI] Settings saved");
        }

        private void ApplySettings()
        {
            SaveSettings();
            Debug.Log("[SettingsUI] Settings applied");
        }

        private void ResetDefaults()
        {
            if (sliderMasterVolume != null)
                sliderMasterVolume.value = 1f;
            if (sliderMusicVolume != null)
                sliderMusicVolume.value = 0.5f;
            if (sliderSfxVolume != null)
                sliderSfxVolume.value = 0.7f;

            if (dropdownQuality != null)
            {
                int mid = QualitySettings.names.Length / 2;
                dropdownQuality.index = mid;
                QualitySettings.SetQualityLevel(mid);
            }

            if (toggleFullscreen != null)
            {
                toggleFullscreen.value = true;
                Screen.fullScreen = true;
            }

            if (toggleVsync != null)
            {
                toggleVsync.value = true;
                QualitySettings.vSyncCount = 1;
            }

            if (toggleCameraShake != null)
                toggleCameraShake.value = true;
            if (toggleDamageNumbers != null)
                toggleDamageNumbers.value = true;
            if (toggleCardAnimations != null)
                toggleCardAnimations.value = true;

            SaveSettings();
            Debug.Log("[SettingsUI] Reset to defaults");
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private void UpdateSliderLabel(
            Label label, float value)
        {
            if (label != null)
                label.text =
                    $"{Mathf.RoundToInt(value * 100)}%";
        }

        private void PlayClick()
        {
            if (buttonClickSound != null &&
                audioSource != null)
                audioSource.PlayOneShot(
                    buttonClickSound, 0.7f);
        }

        // ==========================================
        // STATIC ACCESS (for gameplay systems)
        // ==========================================

        public static bool CameraShakeEnabled =>
            PlayerPrefs.GetInt("CameraShake", 1) == 1;

        public static bool DamageNumbersEnabled =>
            PlayerPrefs.GetInt("DamageNumbers", 1) == 1;

        public static bool CardAnimationsEnabled =>
            PlayerPrefs.GetInt("CardAnimations", 1) == 1;

        public static float MusicVolume =>
            PlayerPrefs.GetFloat("MusicVolume", 0.5f);

        public static float SfxVolume =>
            PlayerPrefs.GetFloat("SfxVolume", 0.7f);
    }
}