using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections;
using System.Collections.Generic;
using System;

namespace ElementumDefense.Auth
{
    /// <summary>
    /// Cloud save manager with built-in rate limiting.
    /// PlayFab allows ~5 UpdateUserData calls per 10 seconds.
    /// This manager batches save requests and throttles them.
    /// </summary>
    public class CloudSaveManager : MonoBehaviour
    {
        public static CloudSaveManager Instance { get; private set; }

        [Header("Rate Limiting")]
        [Tooltip("Minimum seconds between save requests to PlayFab")]
        [SerializeField] private float minSaveInterval = 3f;

        [Tooltip("How long to wait after last change before flushing (debounce)")]
        [SerializeField] private float debounceDelay = 2f;

        // Pending saves — latest data per key (overwrites previous)
        private Dictionary<string, string> pendingData = new Dictionary<string, string>();

        // Timing
        private float lastSaveTime = -999f;
        private Coroutine debounceCoroutine;
        private bool isSaving = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationPause(bool pause)
        {
            // Flush on app pause (mobile) or alt-tab
            if (pause) FlushNow();
        }

        private void OnApplicationQuit()
        {
            // Flush on quit
            FlushNow();
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        /// <summary>
        /// Queue data to be saved to PlayFab. 
        /// Multiple calls with the same key will overwrite previous pending data.
        /// Data is sent after debounce delay, respecting rate limits.
        /// </summary>
        public void SaveData(string key, string json)
        {
            pendingData[key] = json;

            // Restart debounce timer
            if (debounceCoroutine != null)
                StopCoroutine(debounceCoroutine);

            debounceCoroutine = StartCoroutine(DebounceSave());
        }

        /// <summary>
        /// Load data from PlayFab (no rate limiting needed for reads).
        /// </summary>
        public void LoadData(string key, Action<string> onSuccess, Action onNotFoundOrError)
        {
            Debug.Log($"[CloudSave] LoadData '{key}'. LoggedIn={PlayFabClientAPI.IsClientLoggedIn()}");

            var request = new GetUserDataRequest
            {
                Keys = new List<string> { key }
            };

            PlayFabClientAPI.GetUserData(request,
                res =>
                {
                    if (res.Data != null && res.Data.ContainsKey(key))
                    {
                        string value = res.Data[key].Value;
                        Debug.Log($"[CloudSave] LOADED '{key}' OK ({value.Length} chars)");
                        onSuccess?.Invoke(value);
                    }
                    else
                    {
                        Debug.Log($"[CloudSave] Key '{key}' NOT FOUND.");
                        onNotFoundOrError?.Invoke();
                    }
                },
                err =>
                {
                    Debug.LogError($"[CloudSave] LOAD FAILED '{key}': {err.ErrorMessage}");
                    onNotFoundOrError?.Invoke();
                });
        }

        /// <summary>Force-flush all pending saves immediately (used on quit/pause)</summary>
        public void FlushNow()
        {
            if (pendingData.Count > 0)
            {
                SendBatch();
            }
        }

        // ==========================================
        // INTERNAL — DEBOUNCE & THROTTLE
        // ==========================================

        private IEnumerator DebounceSave()
        {
            // Wait for debounce delay (more changes might come)
            yield return new WaitForSeconds(debounceDelay);

            // Check throttle — don't send if too soon
            float timeSinceLast = Time.realtimeSinceStartup - lastSaveTime;
            if (timeSinceLast < minSaveInterval)
            {
                float waitTime = minSaveInterval - timeSinceLast;
                yield return new WaitForSeconds(waitTime);
            }

            // Wait if a save is already in flight
            while (isSaving)
            {
                yield return new WaitForSeconds(0.5f);
            }

            SendBatch();
            debounceCoroutine = null;
        }

        private void SendBatch()
        {
            if (pendingData.Count == 0) return;
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                Debug.LogWarning("[CloudSave] Not logged in — skipping save.");
                return;
            }

            // Take snapshot of pending data and clear
            var dataToSend = new Dictionary<string, string>(pendingData);
            pendingData.Clear();

            isSaving = true;
            lastSaveTime = Time.realtimeSinceStartup;

            int totalChars = 0;
            foreach (var kvp in dataToSend) totalChars += kvp.Value.Length;

            Debug.Log($"[CloudSave] BATCH SAVE: {dataToSend.Count} keys, {totalChars} total chars");

            var request = new UpdateUserDataRequest
            {
                Data = dataToSend
            };

            PlayFabClientAPI.UpdateUserData(request,
                res =>
                {
                    isSaving = false;
                    string keys = string.Join(", ", dataToSend.Keys);
                    Debug.Log($"[CloudSave] SAVED OK: [{keys}]");
                },
                err =>
                {
                    isSaving = false;
                    string keys = string.Join(", ", dataToSend.Keys);

                    if (err.ErrorMessage.Contains("DataUpdateRateExceeded"))
                    {
                        // Re-queue failed data and retry later
                        Debug.LogWarning($"[CloudSave] Rate limited [{keys}] — retrying in {minSaveInterval * 2}s");
                        foreach (var kvp in dataToSend)
                        {
                            if (!pendingData.ContainsKey(kvp.Key))
                                pendingData[kvp.Key] = kvp.Value;
                        }

                        // Schedule retry
                        StartCoroutine(RetryAfterDelay(minSaveInterval * 2f));
                    }
                    else
                    {
                        Debug.LogError($"[CloudSave] SAVE FAILED [{keys}]: {err.ErrorMessage}");
                    }
                });
        }

        private IEnumerator RetryAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (pendingData.Count > 0)
            {
                Debug.Log("[CloudSave] Retrying previously failed save...");
                SendBatch();
            }
        }
    }
}