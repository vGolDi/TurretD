using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using ElementumDefense.Skins;
using ElementumDefense.Achievements;

namespace ElementumDefense.Auth
{
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        public string CurrentUsername { get; private set; }
        public string PlayFabId { get; private set; }
        public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUsername);

        public Action<string> OnLoginSuccess;
        public Action<string> OnLoginFailed;
        public Action<string> OnRegisterSuccess;
        public Action<string> OnRegisterFailed;

        /// <summary>
        /// Fires AFTER cloud connectivity has been verified.
        /// Managers should subscribe to this instead of OnLoginSuccess
        /// to ensure PlayFab is fully ready for data operations.
        /// </summary>
        public Action<string> OnCloudReady;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Auto-create managers if missing from scene
            EnsureManagers();
        }

        private void EnsureManagers()
        {
            if (CloudSaveManager.Instance == null)
            {
                Debug.Log("[Auth] CloudSaveManager not found in scene - creating automatically...");
                var go = new GameObject("CloudSaveManager");
                go.AddComponent<CloudSaveManager>();
                Debug.Log("[Auth] CloudSaveManager created OK: " + (CloudSaveManager.Instance != null));
            }

            if (SkinInventory.Instance == null)
            {
                var skinGo = new GameObject("SkinInventory");
                skinGo.AddComponent<SkinInventory>();
                Debug.Log("[Auth] SkinInventory created automatically.");
            }

            if (AchievementManager.Instance == null)
            {
                var achGo = new GameObject("AchievementManager");
                achGo.AddComponent<AchievementManager>();
                Debug.Log("[Auth] AchievementManager created automatically.");
            }
        }

        public void Register(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                OnRegisterFailed?.Invoke("Username and password cannot be empty.");
                return;
            }

            if (password.Length < 6)
            {
                OnRegisterFailed?.Invoke("Password must be at least 6 characters.");
                return;
            }

            var request = new RegisterPlayFabUserRequest
            {
                Username = username,
                Email = username + "@elementum.dummy",
                Password = password,
                RequireBothUsernameAndEmail = false
            };

            PlayFabClientAPI.RegisterPlayFabUser(request,
                result =>
                {
                    Debug.Log($"[Auth] Registered successfully: {username}");
                    OnRegisterSuccess?.Invoke(username);
                    Login(username, password);
                },
                error =>
                {
                    Debug.LogError($"[Auth] Register error: {error.ErrorMessage}");
                    OnRegisterFailed?.Invoke("Error: " + error.ErrorMessage);
                });
        }

        public void Login(string username, string password)
        {
            var request = new LoginWithPlayFabRequest
            {
                Username = username,
                Password = password
            };

            PlayFabClientAPI.LoginWithPlayFab(request,
                result =>
                {
                    CurrentUsername = username;
                    PlayFabId = result.PlayFabId;

                    bool clientLoggedIn = PlayFabClientAPI.IsClientLoggedIn();
                    Debug.Log($"[Auth] === LOGIN SUCCESS ===");
                    Debug.Log($"[Auth] Username: {username}");
                    Debug.Log($"[Auth] PlayFabId: {PlayFabId}");
                    Debug.Log($"[Auth] IsClientLoggedIn: {clientLoggedIn}");
                    Debug.Log($"[Auth] CloudSaveManager.Instance: {(CloudSaveManager.Instance != null ? "OK" : "NULL!")}");

                    // Fire OnLoginSuccess first (for UI updates etc.)
                    OnLoginSuccess?.Invoke(username);

                    // Verify cloud connectivity, then fire OnCloudReady
                    VerifyCloudAndNotify(username);
                },
                error =>
                {
                    Debug.LogError($"[Auth] Login error: {error.ErrorMessage}");
                    OnLoginFailed?.Invoke("Login failed. Check username and password.");
                });
        }

        /// <summary>
        /// Writes a test value to PlayFab UserData, reads it back,
        /// then fires OnCloudReady. This proves the connection works.
        /// </summary>
        private void VerifyCloudAndNotify(string username)
        {
            string testValue = "test_" + System.DateTime.UtcNow.Ticks;

            Debug.Log($"[Auth] Verifying cloud: writing test value...");

            var writeRequest = new UpdateUserDataRequest
            {
                Data = new Dictionary<string, string>
                {
                    { "_CloudTest", testValue }
                }
            };

            PlayFabClientAPI.UpdateUserData(writeRequest,
                writeResult =>
                {
                    Debug.Log("[Auth] Cloud WRITE OK. Now reading back...");

                    PlayFabClientAPI.GetUserData(
                        new GetUserDataRequest(),
                        readResult =>
                        {
                            if (readResult.Data != null)
                            {
                                Debug.Log($"[Auth] Cloud READ OK. Keys found in PlayFab UserData:");
                                foreach (var kvp in readResult.Data)
                                {
                                    Debug.Log($"[Auth]   KEY: '{kvp.Key}' = {kvp.Value.Value.Length} chars");
                                }
                            }
                            else
                            {
                                Debug.LogWarning("[Auth] Cloud READ OK but Data is null (empty account).");
                            }

                            Debug.Log("[Auth] === CLOUD VERIFIED - firing OnCloudReady ===");
                            OnCloudReady?.Invoke(username);
                        },
                        readError =>
                        {
                            Debug.LogError($"[Auth] Cloud READ ERROR: {readError.ErrorMessage}");
                            OnCloudReady?.Invoke(username);
                        });
                },
                writeError =>
                {
                    Debug.LogError($"[Auth] Cloud WRITE ERROR: {writeError.ErrorMessage}");
                    Debug.LogError("[Auth] === CLOUD IS BROKEN! Check PlayFab dashboard. ===");
                    OnCloudReady?.Invoke(username);
                });
        }

        public void Logout()
        {
            CurrentUsername = null;
            PlayFabId = null;
            PlayFabClientAPI.ForgetAllCredentials();
            UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
        }
    }
}
