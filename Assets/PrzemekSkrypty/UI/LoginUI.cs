using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;

namespace ElementumDefense.Auth
{
    public class LoginUI : MonoBehaviour
    {
        [Header("Inputs")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput; // Ustaw Content Type na Password

        [Header("Buttons")]
        [SerializeField] private Button loginButton;
        [SerializeField] private Button registerButton;

        [Header("Feedback")]
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Navigation")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private void Start()
        {
            loginButton.onClick.AddListener(TryLogin);
            registerButton.onClick.AddListener(TryRegister);

            // Subskrypcja zdarzeñ AuthManagera
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnLoginSuccess += HandleLoginSuccess;
                AuthManager.Instance.OnLoginFailed += HandleError;
                AuthManager.Instance.OnRegisterSuccess += HandleRegisterSuccess;
                AuthManager.Instance.OnRegisterFailed += HandleError;
            }
        }

        private void OnDestroy()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnLoginSuccess -= HandleLoginSuccess;
                AuthManager.Instance.OnLoginFailed -= HandleError;
                AuthManager.Instance.OnRegisterSuccess -= HandleRegisterSuccess;
                AuthManager.Instance.OnRegisterFailed -= HandleError;
            }
        }

        private void TryLogin()
        {
            messageText.text = "Logowanie...";
            AuthManager.Instance.Login(usernameInput.text, passwordInput.text);
        }

        private void TryRegister()
        {
            messageText.text = "Rejestracja...";
            AuthManager.Instance.Register(usernameInput.text, passwordInput.text);
        }

        private void HandleLoginSuccess(string username)
        {
            messageText.color = Color.green;
            messageText.text = $"Witaj, {username}!";

            PhotonNetwork.NickName = username;
            Debug.Log($"[LoginUI] Ustawiono Photon NickName na: {username}");
            // PrzejdŸ do gry
            Invoke(nameof(LoadMenu), 1f);
        }

        private void HandleRegisterSuccess(string username)
        {
            messageText.color = Color.green;
            messageText.text = "Konto utworzone! Logowanie...";
        }

        private void HandleError(string error)
        {
            messageText.color = Color.red;
            messageText.text = error;
        }

        private void LoadMenu()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}