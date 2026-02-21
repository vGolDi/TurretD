using UnityEngine;
using UnityEngine.UIElements;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class CreditsUI : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField]
        private MainMenuController mainMenuController;

        [Header("Version")]
        [SerializeField]
        private string gameVersion = "0.1.0 Alpha";

        [Header("Audio")]
        [SerializeField] private AudioClip buttonClickSound;

        private AudioSource audioSource;
        private VisualElement root;

        private Button btnBack;
        private Label versionLabel;

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
            UpdateVersion();

            var bg = root.Q<VisualElement>("credits-root");
            StarfieldInjector.Instance?.Register(bg);
        }

        public void Hide()
        {
            if (root != null)
            {
                var bg = root.Q<VisualElement>("credits-root");
                StarfieldInjector.Instance?.Unregister(bg);
            }

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
        // QUERY & BIND
        // ==========================================

        private void QueryElements()
        {
            btnBack = root.Q<Button>("btn-back");
            versionLabel =
                root.Q<Label>("credits-version");
        }

        private void BindButtons()
        {
            btnBack?.RegisterCallback<ClickEvent>(evt =>
            {
                PlayClick();
                mainMenuController?.BackToMainMenu();
                evt.StopPropagation();
            });
        }

        private void UpdateVersion()
        {
            if (versionLabel != null)
                versionLabel.text = $"v{gameVersion}";
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private void PlayClick()
        {
            if (buttonClickSound != null &&
                audioSource != null)
                audioSource.PlayOneShot(
                    buttonClickSound, 0.7f);
        }
    }
}