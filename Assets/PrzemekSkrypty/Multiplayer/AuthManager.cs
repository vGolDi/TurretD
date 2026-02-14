using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace ElementumDefense.Auth
{
    [System.Serializable]
    public class UserData
    {
        public string username;
        public string passwordHash; // Nie przechowujemy hase³ jawnym tekstem!
    }

    [System.Serializable]
    public class UserDatabase
    {
        public List<UserData> users = new List<UserData>();
    }

    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        private string databasePath;
        private UserDatabase userDatabase;

        // Aktualnie zalogowany gracz
        public string CurrentUsername { get; private set; }
        public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUsername);

        // Events
        public System.Action<string> OnLoginSuccess; // string = username
        public System.Action<string> OnLoginFailed;  // string = error message
        public System.Action<string> OnRegisterSuccess;
        public System.Action<string> OnRegisterFailed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            databasePath = Path.Combine(Application.persistentDataPath, "UserDB.json");
            LoadDatabase();
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        public void Register(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                OnRegisterFailed?.Invoke("Nazwa i has³o nie mog¹ byæ puste.");
                return;
            }

            if (userDatabase.users.Any(u => u.username == username))
            {
                OnRegisterFailed?.Invoke("U¿ytkownik o takiej nazwie ju¿ istnieje.");
                return;
            }

            // Tworzenie nowego u¿ytkownika
            UserData newUser = new UserData
            {
                username = username,
                passwordHash = HashPassword(password)
            };

            userDatabase.users.Add(newUser);
            SaveDatabase();

            Debug.Log($"[Auth] Zarejestrowano u¿ytkownika: {username}");
            OnRegisterSuccess?.Invoke(username);

            // Opcjonalnie: Automatyczne logowanie po rejestracji
            Login(username, password);
        }

        public void Login(string username, string password)
        {
            var user = userDatabase.users.FirstOrDefault(u => u.username == username);

            if (user == null)
            {
                OnLoginFailed?.Invoke("U¿ytkownik nie istnieje.");
                return;
            }

            string inputHash = HashPassword(password);
            if (user.passwordHash != inputHash)
            {
                OnLoginFailed?.Invoke("Nieprawid³owe has³o.");
                return;
            }

            // Sukces
            CurrentUsername = username;
            Debug.Log($"[Auth] Zalogowano jako: {username}");
            OnLoginSuccess?.Invoke(username);
        }

        public void Logout()
        {
            CurrentUsername = null;
            // Tutaj mo¿na prze³adowaæ scenê do Menu Logowania
            UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene"); // Zak³adam nazwê sceny
        }

        // ==========================================
        // DATABASE HANDLING
        // ==========================================

        private void LoadDatabase()
        {
            if (File.Exists(databasePath))
            {
                string json = File.ReadAllText(databasePath);
                userDatabase = JsonUtility.FromJson<UserDatabase>(json);
            }
            else
            {
                userDatabase = new UserDatabase();
            }
        }

        private void SaveDatabase()
        {
            string json = JsonUtility.ToJson(userDatabase, true);
            File.WriteAllText(databasePath, json);
        }

        // Proste hashowanie SHA256 (dla bezpieczeñstwa lokalnego)
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}