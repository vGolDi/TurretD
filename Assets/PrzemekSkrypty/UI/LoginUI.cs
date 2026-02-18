using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.Collections;

namespace ElementumDefense.Auth
{
    [RequireComponent(typeof(UIDocument))]
    public class LoginUI : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField]
        private string mainMenuSceneName = "MainMenu";

        [Header("Version")]
        [SerializeField]
        private string gameVersion = "0.1.0 Alpha";

        [Header("Audio")]
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip successSound;
        [SerializeField] private AudioClip errorSound;

        private AudioSource audioSource;
        private VisualElement root;

        // Elements
        private VisualElement tabLogin;
        private VisualElement tabRegister;
        private VisualElement confirmPasswordGroup;
        private TextField inputUsername;
        private TextField inputPassword;
        private TextField inputConfirmPassword;
        private Label messageLabel;
        private Button btnSubmit;
        private Label versionLabel;

        // State
        private bool isRegisterMode = false;
        private bool isProcessing = false;
        private bool eventsSubscribed = false;

        // Track focused field for Tab navigation
        private enum FocusedField
        {
            Username,
            Password,
            ConfirmPassword
        }

        private FocusedField currentFocus =
            FocusedField.Username;

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

        private void Start()
        {
            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null) return;

            root = uiDoc.rootVisualElement;
            if (root == null) return;

            QueryElements();
            BindControls();
            SetMode(false);
            UpdateVersion();
            SubscribeAuthEvents();

            StartCoroutine(FocusUsernameDelayed());
        }

        private void OnDestroy()
        {
            UnsubscribeAuthEvents();
        }

        private void SubscribeAuthEvents()
        {
            if (eventsSubscribed) return;

            // Poczekaj na AuthManager jeœli jeszcze
            // nie istnieje
            if (AuthManager.Instance == null)
            {
                StartCoroutine(
                    WaitForAuthManagerAndSubscribe());
                return;
            }

            DoSubscribe();
        }

        private IEnumerator WaitForAuthManagerAndSubscribe()
        {
            float timeout = 5f;
            float elapsed = 0f;

            while (AuthManager.Instance == null &&
                   elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (AuthManager.Instance != null)
            {
                DoSubscribe();
            }
            else
            {
                Debug.LogError(
                    "[LoginUI] AuthManager not found " +
                    "after timeout!");
            }
        }

        private void DoSubscribe()
        {
            if (eventsSubscribed) return;

            var auth = AuthManager.Instance;
            auth.OnLoginSuccess += HandleLoginSuccess;
            auth.OnLoginFailed += HandleError;
            auth.OnRegisterSuccess +=
                HandleRegisterSuccess;
            auth.OnRegisterFailed += HandleError;

            eventsSubscribed = true;
            Debug.Log(
                "[LoginUI] Auth events subscribed");
        }

        private void UnsubscribeAuthEvents()
        {
            if (!eventsSubscribed) return;
            if (AuthManager.Instance == null) return;

            var auth = AuthManager.Instance;
            auth.OnLoginSuccess -= HandleLoginSuccess;
            auth.OnLoginFailed -= HandleError;
            auth.OnRegisterSuccess -=
                HandleRegisterSuccess;
            auth.OnRegisterFailed -= HandleError;

            eventsSubscribed = false;
        }

        private IEnumerator FocusUsernameDelayed()
        {
            yield return null;
            yield return null;
            inputUsername?.Focus();
            currentFocus = FocusedField.Username;
        }

        // ==========================================
        // QUERY & BIND
        // ==========================================

        private void QueryElements()
        {
            tabLogin =
                root.Q<VisualElement>("tab-login");
            tabRegister =
                root.Q<VisualElement>("tab-register");
            confirmPasswordGroup =
                root.Q<VisualElement>(
                    "confirm-password-group");

            inputUsername =
                root.Q<TextField>("input-username");
            inputPassword =
                root.Q<TextField>("input-password");
            inputConfirmPassword =
                root.Q<TextField>(
                    "input-confirm-password");

            messageLabel =
                root.Q<Label>("login-message");
            btnSubmit =
                root.Q<Button>("btn-submit");
            versionLabel =
                root.Q<Label>("login-version");
        }

        private void BindControls()
        {
            // Tab switching
            tabLogin?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    PlayClick();
                    SetMode(false);
                    evt.StopPropagation();
                });

            tabRegister?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    PlayClick();
                    SetMode(true);
                    evt.StopPropagation();
                });

            // Submit button
            btnSubmit?.RegisterCallback<ClickEvent>(
                evt =>
                {
                    PlayClick();
                    Submit();
                    evt.StopPropagation();
                });

            // ==========================================
            // KEYBOARD NAVIGATION
            // Tab = next field, Enter = submit or next
            // ==========================================

            inputUsername?
                .RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Tab ||
                        evt.keyCode == KeyCode.Return ||
                        evt.keyCode ==
                            KeyCode.KeypadEnter)
                    {
                        evt.PreventDefault();
                        evt.StopPropagation();
                        inputPassword?.Focus();
                        currentFocus =
                            FocusedField.Password;
                    }
                }, TrickleDown.TrickleDown);

            inputPassword?
                .RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Tab)
                    {
                        evt.PreventDefault();
                        evt.StopPropagation();

                        if (isRegisterMode)
                        {
                            inputConfirmPassword
                                ?.Focus();
                            currentFocus =
                                FocusedField
                                    .ConfirmPassword;
                        }
                        else
                        {
                            Submit();
                        }
                    }
                    else if (
                        evt.keyCode ==
                            KeyCode.Return ||
                        evt.keyCode ==
                            KeyCode.KeypadEnter)
                    {
                        evt.PreventDefault();
                        evt.StopPropagation();

                        if (isRegisterMode)
                        {
                            inputConfirmPassword
                                ?.Focus();
                            currentFocus =
                                FocusedField
                                    .ConfirmPassword;
                        }
                        else
                        {
                            Submit();
                        }
                    }
                }, TrickleDown.TrickleDown);

            inputConfirmPassword?
                .RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Tab ||
                        evt.keyCode ==
                            KeyCode.Return ||
                        evt.keyCode ==
                            KeyCode.KeypadEnter)
                    {
                        evt.PreventDefault();
                        evt.StopPropagation();
                        Submit();
                    }
                }, TrickleDown.TrickleDown);

            // Track focus
            inputUsername?
                .RegisterCallback<FocusInEvent>(
                    evt => currentFocus =
                        FocusedField.Username);
            inputPassword?
                .RegisterCallback<FocusInEvent>(
                    evt => currentFocus =
                        FocusedField.Password);
            inputConfirmPassword?
                .RegisterCallback<FocusInEvent>(
                    evt => currentFocus =
                        FocusedField.ConfirmPassword);
        }

        // ==========================================
        // MODE SWITCHING
        // ==========================================

        private void SetMode(bool register)
        {
            isRegisterMode = register;

            if (tabLogin != null)
            {
                if (register)
                    tabLogin.RemoveFromClassList(
                        "login-tab-active");
                else
                    tabLogin.AddToClassList(
                        "login-tab-active");
            }

            if (tabRegister != null)
            {
                if (register)
                    tabRegister.AddToClassList(
                        "login-tab-active");
                else
                    tabRegister.RemoveFromClassList(
                        "login-tab-active");
            }

            if (confirmPasswordGroup != null)
            {
                if (register)
                    confirmPasswordGroup
                        .RemoveFromClassList("hidden");
                else
                    confirmPasswordGroup
                        .AddToClassList("hidden");
            }

            if (btnSubmit != null)
                btnSubmit.text = register
                    ? "CREATE ACCOUNT"
                    : "ENTER";

            ClearMessage();
            inputUsername?.Focus();
            currentFocus = FocusedField.Username;
        }

        // ==========================================
        // SUBMIT
        // ==========================================

        private void Submit()
        {
            if (isProcessing) return;

            string username =
                inputUsername?.value?.Trim();
            string password = inputPassword?.value;

            if (string.IsNullOrEmpty(username))
            {
                ShowMessage(
                    "Username is required",
                    MessageType.Error);
                inputUsername?.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowMessage(
                    "Password is required",
                    MessageType.Error);
                inputPassword?.Focus();
                return;
            }

            if (password.Length < 3)
            {
                ShowMessage(
                    "Password: at least 3 characters",
                    MessageType.Error);
                inputPassword?.Focus();
                return;
            }

            if (AuthManager.Instance == null)
            {
                ShowMessage(
                    "Auth service unavailable",
                    MessageType.Error);
                Debug.LogError(
                    "[LoginUI] AuthManager is null!");
                return;
            }

            if (isRegisterMode)
            {
                string confirm =
                    inputConfirmPassword?.value;

                if (string.IsNullOrEmpty(confirm))
                {
                    ShowMessage(
                        "Please confirm your password",
                        MessageType.Error);
                    inputConfirmPassword?.Focus();
                    return;
                }

                if (password != confirm)
                {
                    ShowMessage(
                        "Passwords do not match",
                        MessageType.Error);
                    inputConfirmPassword?.Focus();
                    return;
                }

                ShowMessage(
                    "Creating account...",
                    MessageType.Loading);
                SetProcessing(true);
                AuthManager.Instance.Register(
                    username, password);
            }
            else
            {
                ShowMessage(
                    "Signing in...",
                    MessageType.Loading);
                SetProcessing(true);
                AuthManager.Instance.Login(
                    username, password);
            }
        }

        // ==========================================
        // AUTH CALLBACKS
        // ==========================================

        private void HandleLoginSuccess(string username)
        {
            Debug.Log(
                $"[LoginUI] HandleLoginSuccess: " +
                $"{username}");

            SetProcessing(false);
            ShowMessage(
                $"Welcome, {username}",
                MessageType.Success);
            PlaySound(successSound);

            PhotonNetwork.NickName = username;
            Debug.Log(
                $"[LoginUI] Photon NickName: " +
                $"{username}");

            StartCoroutine(LoadMenuDelayed());
        }

        private void HandleRegisterSuccess(
            string username)
        {
            Debug.Log(
                $"[LoginUI] HandleRegisterSuccess: " +
                $"{username}");

            SetProcessing(false);
            ShowMessage(
                "Account created! Signing in...",
                MessageType.Success);
            PlaySound(successSound);
        }

        private void HandleError(string error)
        {
            Debug.Log(
                $"[LoginUI] HandleError: {error}");

            SetProcessing(false);
            ShowMessage(error, MessageType.Error);
            PlaySound(errorSound);
        }

        private IEnumerator LoadMenuDelayed()
        {
            yield return new WaitForSeconds(1.2f);
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // ==========================================
        // MESSAGES
        // ==========================================

        private enum MessageType
        {
            None,
            Success,
            Error,
            Loading
        }

        private void ShowMessage(
            string text, MessageType type)
        {
            if (messageLabel == null) return;

            messageLabel.text = text;

            messageLabel.RemoveFromClassList(
                "login-message-success");
            messageLabel.RemoveFromClassList(
                "login-message-error");
            messageLabel.RemoveFromClassList(
                "login-message-loading");

            switch (type)
            {
                case MessageType.Success:
                    messageLabel.AddToClassList(
                        "login-message-success");
                    break;
                case MessageType.Error:
                    messageLabel.AddToClassList(
                        "login-message-error");
                    break;
                case MessageType.Loading:
                    messageLabel.AddToClassList(
                        "login-message-loading");
                    break;
            }
        }

        private void ClearMessage()
        {
            if (messageLabel == null) return;

            messageLabel.text = "";
            messageLabel.RemoveFromClassList(
                "login-message-success");
            messageLabel.RemoveFromClassList(
                "login-message-error");
            messageLabel.RemoveFromClassList(
                "login-message-loading");
        }

        // ==========================================
        // STATE
        // ==========================================

        private void SetProcessing(bool processing)
        {
            isProcessing = processing;

            if (btnSubmit != null)
                btnSubmit.SetEnabled(!processing);
            if (inputUsername != null)
                inputUsername.SetEnabled(!processing);
            if (inputPassword != null)
                inputPassword.SetEnabled(!processing);
            if (inputConfirmPassword != null)
                inputConfirmPassword
                    .SetEnabled(!processing);
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private void UpdateVersion()
        {
            if (versionLabel != null)
                versionLabel.text = $"v{gameVersion}";
        }

        private void PlayClick()
        {
            PlaySound(buttonClickSound);
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && audioSource != null)
                audioSource.PlayOneShot(clip, 0.7f);
        }
    }
}//using UnityEngine;
 //using TMPro;
 //using UnityEngine.UI;
 //using UnityEngine.SceneManagement;
 //using Photon.Pun;

//namespace ElementumDefense.Auth
//{
//    public class LoginUI : MonoBehaviour
//    {
//        [Header("Inputs")]
//        [SerializeField] private TMP_InputField usernameInput;
//        [SerializeField] private TMP_InputField passwordInput; // Ustaw Content Type na Password

//        [Header("Buttons")]
//        [SerializeField] private Button loginButton;
//        [SerializeField] private Button registerButton;

//        [Header("Feedback")]
//        [SerializeField] private TextMeshProUGUI messageText;

//        [Header("Navigation")]
//        [SerializeField] private string mainMenuSceneName = "MainMenu";

//        private void Start()
//        {
//            loginButton.onClick.AddListener(TryLogin);
//            registerButton.onClick.AddListener(TryRegister);

//            // Subskrypcja zdarzeñ AuthManagera
//            if (AuthManager.Instance != null)
//            {
//                AuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
//                AuthManager.Instance.OnLoginFailed += HandleError;
//                AuthManager.Instance.OnRegisterSuccess += HandleRegisterSuccess;
//                AuthManager.Instance.OnRegisterFailed += HandleError;
//            }
//        }

//        private void OnDestroy()
//        {
//            if (AuthManager.Instance != null)
//            {
//                AuthManager.Instance.OnLoginSuccess -= HandleLoginSuccess;
//                AuthManager.Instance.OnLoginFailed -= HandleError;
//                AuthManager.Instance.OnRegisterSuccess -= HandleRegisterSuccess;
//                AuthManager.Instance.OnRegisterFailed -= HandleError;
//            }
//        }

//        private void TryLogin()
//        {
//            messageText.text = "Logowanie...";
//            AuthManager.Instance.Login(usernameInput.text, passwordInput.text);
//        }

//        private void TryRegister()
//        {
//            messageText.text = "Rejestracja...";
//            AuthManager.Instance.Register(usernameInput.text, passwordInput.text);
//        }

//        private void HandleLoginSuccess(string username)
//        {
//            messageText.color = Color.green;
//            messageText.text = $"Witaj, {username}!";

//            PhotonNetwork.NickName = username;
//            Debug.Log($"[LoginUI] Ustawiono Photon NickName na: {username}");
//            // PrzejdŸ do gry
//            Invoke(nameof(LoadMenu), 1f);
//        }

//        private void HandleRegisterSuccess(string username)
//        {
//            messageText.color = Color.green;
//            messageText.text = "Konto utworzone! Logowanie...";
//        }

//        private void HandleError(string error)
//        {
//            messageText.color = Color.red;
//            messageText.text = error;
//        }

//        private void LoadMenu()
//        {
//            SceneManager.LoadScene(mainMenuSceneName);
//        }
//    }
//}