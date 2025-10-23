using UnityEngine;
using TMPro;
using System.Collections;
using Photon.Pun;

/// <summary>
/// Manages game start countdown (LOCAL - no RPC needed)
/// Each player runs their own countdown independently
/// </summary>
public class GameStartCountdown : MonoBehaviour
{
    [Header("Countdown Settings")]
    [SerializeField] private float countdownTime = 5f;
    [SerializeField] private bool waitForAllPlayers = true;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private TextMeshProUGUI waitingForPlayersText;

    [Header("Game References")]
    public WaveManager waveManager;

    private bool countdownStarted = false;
    private bool hasCheckedPlayers = false;

    private void Start()
    {
        Debug.Log("========== GAME START COUNTDOWN - START ==========");
        Debug.Log($"[Countdown] WaveManager assigned: {(waveManager != null ? "YES" : "NO")}");
        Debug.Log($"[Countdown] WaitForAllPlayers: {waitForAllPlayers}");

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }

        // If not waiting for all players, start immediately
        if (!waitForAllPlayers)
        {
            Debug.Log("[Countdown] Not waiting - starting immediately");
            StartGameCountdown();
        }
        else
        {
            // Show waiting message
            if (waitingForPlayersText != null)
            {
                waitingForPlayersText.gameObject.SetActive(true);
            }
        }
    }

    private void Update()
    {
        // Check if we should start countdown
        if (!countdownStarted && waitForAllPlayers && !hasCheckedPlayers)
        {
            CheckIfShouldStart();
        }
    }

    /// <summary>
    /// Checks if all players are in room and starts countdown
    /// NO RPC - purely local check
    /// </summary>
    private void CheckIfShouldStart()
    {
        // Check if we're in multiplayer
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.Log("[Countdown] Not in multiplayer - starting now");
            hasCheckedPlayers = true;
            StartGameCountdown();
            return;
        }

        // Check if room is null (shouldn't happen, but safety)
        if (PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogWarning("[Countdown] Room is null - starting anyway");
            hasCheckedPlayers = true;
            StartGameCountdown();
            return;
        }

        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
        int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;

        Debug.Log($"[Countdown] Players: {currentPlayers}/{maxPlayers}");

        // Update waiting text
        if (waitingForPlayersText != null)
        {
            waitingForPlayersText.text = $"Waiting for players... ({currentPlayers}/{maxPlayers})";
        }

        // Start if room is full
        if (currentPlayers >= maxPlayers)
        {
            Debug.Log("[Countdown] All players ready - starting countdown!");
            hasCheckedPlayers = true;

            if (waitingForPlayersText != null)
            {
                waitingForPlayersText.gameObject.SetActive(false);
            }

            StartGameCountdown();
        }
    }

    /// <summary>
    /// Starts countdown (LOCAL - no networking)
    /// </summary>
    private void StartGameCountdown()
    {
        if (countdownStarted)
        {
            Debug.Log("[Countdown] Already started - aborting");
            return;
        }

        Debug.Log("========== STARTING COUNTDOWN ==========");
        countdownStarted = true;

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
        }

        StartCoroutine(CountdownRoutine());
    }

    /// <summary>
    /// Countdown coroutine
    /// </summary>
    private IEnumerator CountdownRoutine()
    {
        float timer = countdownTime;

        while (timer > 0)
        {
            if (countdownText != null)
            {
                int displayNumber = Mathf.CeilToInt(timer);
                countdownText.text = displayNumber.ToString();

                // Optional: scale animation
                float scale = 1f + (0.2f * Mathf.Sin(Time.time * 10f));
                countdownText.transform.localScale = Vector3.one * scale;
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        OnCountdownComplete();
    }

    /// <summary>
    /// Called when countdown finishes
    /// </summary>
    private void OnCountdownComplete()
    {
        Debug.Log("========== COUNTDOWN COMPLETE - STARTING GAME ==========");

        if (countdownText != null)
        {
            countdownText.text = "GO!";
            countdownText.transform.localScale = Vector3.one * 1.5f;
        }

        StartCoroutine(HideCountdownText());
        StartGame();
    }

    /// <summary>
    /// Hides countdown text after delay
    /// </summary>
    private IEnumerator HideCountdownText()
    {
        yield return new WaitForSeconds(1f);

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Starts the actual game
    /// </summary>
    private void StartGame()
    {
        Debug.Log("[Countdown] StartGame called");

        if (waveManager != null)
        {
            Debug.Log("[Countdown] Calling waveManager.StartWaves()...");
            waveManager.StartWaves(); // Teraz tylko lokalny gracz uruchamia swoje fale
            Debug.Log("[Countdown]  Waves started!");
        }
        else
        {
            Debug.LogError("[Countdown] WaveManager is NULL! Cannot start waves!");
        }
    }

    /// <summary>
    /// Public method to manually start (e.g., from UI button)
    /// </summary>
    public void ManualStart()
    {
        if (!countdownStarted)
        {
            Debug.Log("[Countdown] Manual start triggered");
            StartGameCountdown();
        }
    }
}