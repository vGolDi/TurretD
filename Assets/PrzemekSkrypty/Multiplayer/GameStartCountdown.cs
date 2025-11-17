//using UnityEngine;
//using TMPro;
//using System.Collections;
//using Photon.Pun;
//using ElementumDefense.Cards;
//using System.Collections.Generic;

//public class GameStartCountdown : MonoBehaviourPunCallbacks
//{
//    [Header("Countdown Settings")]
//    [SerializeField] private float countdownTime = 5f;
//    [SerializeField] private bool waitForAllPlayers = true;

//    [Header("UI References")]
//    [SerializeField] private TextMeshProUGUI countdownText;
//    [SerializeField] private TextMeshProUGUI waitingForPlayersText;

//    [Header("Game References")]
//    //public WaveManager waveManager;
//    private List<WaveManager> allWaveManagers = new List<WaveManager>();

//    private bool countdownStarted = false;
//    private bool hasCheckedPlayers = false;

//    private DraftManager draftManager;

//    [Header("Draft Synchronization")]
//    private bool localPlayerReady = false;
//    private int playersReady = 0;
//    private int totalPlayers = 0;
//    private void Start()
//    {
//        //Debug.Log("========== GAME START COUNTDOWN - START ==========");
//        //Debug.Log($"[Countdown] WaveManager assigned: {(waveManager != null ? "YES" : "NO")}");
//        //Debug.Log($"[Countdown] WaitForAllPlayers: {waitForAllPlayers}");

//        //if (countdownText != null)
//        //{
//        //    countdownText.gameObject.SetActive(false);
//        //}

//        //if (!waitForAllPlayers)
//        //{
//        //    Debug.Log("[Countdown] Not waiting - starting immediately");
//        //    StartGameCountdown();
//        //}
//        //else
//        //{
//        //    if (waitingForPlayersText != null)
//        //    {
//        //        waitingForPlayersText.gameObject.SetActive(true);
//        //    }
//        //}
//        //Debug.Log("========== GAME START COUNTDOWN - START ==========");

//        //if (countdownText != null)
//        //{
//        //    countdownText.gameObject.SetActive(false);
//        //}

//        //if (!waitForAllPlayers)
//        //{
//        //    Debug.Log("[Countdown] Not waiting - starting immediately");
//        //    StartGameCountdown();
//        //}
//        //else
//        //{
//        //    if (waitingForPlayersText != null)
//        //    {
//        //        waitingForPlayersText.gameObject.SetActive(true);
//        //    }
//        //}
//    }
//    public override void OnEnable()
//    {
//        base.OnEnable(); // Dobra praktyka, aby wywołać metodę bazową

//        Debug.Log("[GameStartCountdown] Zostałem włączony! Rozpoczynam sprawdzanie...");

//        countdownStarted = false;
//        hasCheckedPlayers = false;

//        if (countdownText != null)
//            countdownText.gameObject.SetActive(false);

//        if (waitingForPlayersText != null)
//            waitingForPlayersText.gameObject.SetActive(true);
//    }
//    public void RegisterWaveManager(WaveManager waveManager)
//    {
//        if (!allWaveManagers.Contains(waveManager))
//        {
//            allWaveManagers.Add(waveManager);
//            Debug.Log($"[Countdown] Registered WaveManager. Total: {allWaveManagers.Count}");
//        }
//    }
//    private void Update()
//    {
//        if (!countdownStarted && waitForAllPlayers && !hasCheckedPlayers)
//        {
//            CheckIfShouldStart();
//        }
//    }

//    private void CheckIfShouldStart()
//    {
//        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
//        {
//            Debug.Log("[Countdown] Not in multiplayer - starting now");
//            hasCheckedPlayers = true;
//            StartGameCountdown();
//            return;
//        }

//        if (PhotonNetwork.CurrentRoom == null)
//        {
//            Debug.LogWarning("[Countdown] Room is null - starting anyway");
//            hasCheckedPlayers = true;
//            StartGameCountdown();
//            return;
//        }

//        int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
//        int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;

//        Debug.Log($"[Countdown] Players: {currentPlayers}/{maxPlayers}");

//        if (waitingForPlayersText != null)
//        {
//            waitingForPlayersText.text = $"Waiting for players... ({currentPlayers}/{maxPlayers})";
//        }

//        if (currentPlayers >= maxPlayers)
//        {
//            Debug.Log("[Countdown] All players ready - starting countdown!");
//            hasCheckedPlayers = true;

//            if (waitingForPlayersText != null)
//            {
//                waitingForPlayersText.gameObject.SetActive(false);
//            }

//            StartGameCountdown();
//        }
//    }

//    private void StartGameCountdown()
//    {
//        if (countdownStarted)
//        {
//            Debug.Log("[Countdown] Already started - aborting");
//            return;
//        }

//        Debug.Log("========== STARTING COUNTDOWN ==========");
//        countdownStarted = true;

//        if (countdownText != null)
//        {
//            countdownText.gameObject.SetActive(true);
//        }

//        StartCoroutine(CountdownRoutine());
//    }

//    private IEnumerator CountdownRoutine()
//    {
//        float timer = countdownTime;

//        while (timer > 0)
//        {
//            if (countdownText != null)
//            {
//                int displayNumber = Mathf.CeilToInt(timer);
//                countdownText.text = displayNumber.ToString();

//                float scale = 1f + (0.2f * Mathf.Sin(Time.time * 10f));
//                countdownText.transform.localScale = Vector3.one * scale;
//            }

//            timer -= Time.deltaTime;
//            yield return null;
//        }

//        OnCountdownComplete();
//    }

//    private void OnCountdownComplete()
//    {
//        //Debug.Log("========== COUNTDOWN COMPLETE ==========");

//        //if (countdownText != null)
//        //{
//        //    countdownText.text = "GO!";
//        //    countdownText.transform.localScale = Vector3.one * 1.5f;
//        //}

//        //StartCoroutine(HideCountdownText());

//        //// ========== ZMIENIONE: Najpierw draft, potem gra ==========
//        //StartCoroutine(StartGameSequence());
//        //// ===========================================================
//        ///Debug.Log("========== COUNTDOWN COMPLETE ==========");

//        if (countdownText != null)
//        {
//            countdownText.text = "GO!";
//            countdownText.transform.localScale = Vector3.one * 1.5f;
//        }

//        StartCoroutine(HideCountdownText());
//        StartCoroutine(StartGameSequence()); // To jest poprawne
//    }

//    private IEnumerator HideCountdownText()
//    {
//        yield return new WaitForSeconds(1f);

//        if (countdownText != null)
//        {
//            countdownText.gameObject.SetActive(false);
//        }
//    }

//    // ========== NOWA METODA: Starter Draft → Waves ==========
//    private IEnumerator StartGameSequence()
//    {
//        //Debug.Log("[Countdown] ========== STARTING GAME SEQUENCE ==========");

//        //// 1. Find DraftManager
//        //yield return StartCoroutine(FindDraftManager());

//        //if (draftManager == null)
//        //{
//        //    Debug.LogError("[Countdown] No DraftManager found! Skipping draft, starting waves...");
//        //    StartGame();
//        //    yield break;
//        //}

//        //// 2. Start Starter Draft
//        //Debug.Log("[Countdown] Starting Starter Draft...");
//        //draftManager.StartStarterDraft();

//        //// 3. Wait for LOCAL player to complete draft
//        //while (draftManager.IsDrafting || !draftManager.IsStarterDraftComplete)
//        //{
//        //    yield return null;
//        //}

//        //Debug.Log("[Countdown] ✅ LOCAL draft complete!");

//        //// ========== NOWE: Synchronizuj z innymi graczami ==========
//        //localPlayerReady = true;

//        //if (PhotonNetwork.IsMasterClient)
//        //{
//        //    totalPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
//        //    playersReady = 1; // Master is ready

//        //    Debug.Log($"[Countdown] Master ready. Waiting for {totalPlayers - 1} more players...");
//        //}
//        //else
//        //{
//        //    // Non-master: notify master that I'm ready
//        //    photonView.RPC("RPC_PlayerDraftComplete", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer.ActorNumber);
//        //    Debug.Log("[Countdown] Notified Master that I'm ready. Waiting for start signal...");
//        //}

//        //if (PhotonNetwork.IsMasterClient)
//        //{
//        //    yield return new WaitUntil(() => AllPlayersReady());
//        //    Debug.Log("[Countdown] ✅ ALL PLAYERS READY! Starting waves...");
//        //    photonView.RPC("RPC_StartWaves", RpcTarget.All);
//        //}
//        Debug.Log("[Countdown] ========== STARTING GAME SEQUENCE ==========");

//        // DraftManager powinien już istnieć w tym momencie
//        draftManager = DraftManager.Instance;

//        if (draftManager == null)
//        {
//            Debug.LogError("[Countdown] No DraftManager found! Gra nie może kontynuować.");
//            yield break; // Zakończ, jeśli nie ma DraftManagera
//        }

//        Debug.Log("[Countdown] Uruchamianie Starter Draft...");
//        draftManager.StartStarterDraft();

//        // Czekaj na koniec synchronizacji draftu
//        yield return new WaitUntil(() => !draftManager.IsDrafting && draftManager.IsStarterDraftComplete);

//        Debug.Log("[Countdown] ✅ Draft zakończony. Uruchamianie fal...");

//        // Start fal
//        StartGame();
//    }

//    ///// <summary>
//    ///// Finds DraftManager (retry until found)
//    ///// </summary>
//    //private IEnumerator FindDraftManager()
//    //{
//    //    float timeout = 5f;
//    //    float elapsed = 0f;

//    //    while (draftManager == null && elapsed < timeout)
//    //    {
//    //        draftManager = DraftManager.Instance;

//    //        if (draftManager == null)
//    //        {
//    //            Debug.LogWarning("[Countdown] Waiting for DraftManager...");
//    //            yield return new WaitForSeconds(0.5f);
//    //            elapsed += 0.5f;
//    //        }
//    //    }

//    //    if (draftManager != null)
//    //    {
//    //        Debug.Log("[Countdown] ✅ Found DraftManager!");
//    //    }
//    //    else
//    //    {
//    //        Debug.LogError("[Countdown] ❌ DraftManager not found after 5s!");
//    //    }
//    //}
//    // =========================================================

//    private void StartGame()
//    {
//        Debug.Log("[Countdown] StartGame called");

//        // Znajdź wszystkie WaveManagery jeśli lista jest pusta
//        if (allWaveManagers.Count == 0)
//        {
//            WaveManager[] managers = FindObjectsByType<WaveManager>(FindObjectsSortMode.None);
//            allWaveManagers.AddRange(managers);
//            Debug.Log($"[Countdown] Found {allWaveManagers.Count} WaveManagers");
//        }

//        // Wystartuj wszystkie areny
//        foreach (WaveManager wm in allWaveManagers)
//        {
//            if (wm != null)
//            {
//                Debug.Log($"[Countdown] Starting waves on {wm.gameObject.name}");
//                wm.StartWaves();
//            }
//        }

//        if (allWaveManagers.Count == 0)
//        {
//            Debug.LogError("[Countdown] No WaveManagers found!");
//        }

//    //Debug.Log("[Countdown] StartGame called");

//    //    if (waveManager != null)
//    //    {
//    //        Debug.Log("[Countdown] Calling waveManager.StartWaves()...");
//    //        waveManager.StartWaves();
//    //        Debug.Log("[Countdown] ✅ Waves started!");
//    //    }
//    //    else
//    //    {
//    //        Debug.LogError("[Countdown] WaveManager is NULL! Cannot start waves!");
//    //    }
//    }

//    public void ManualStart()
//    {
//        if (!countdownStarted)
//        {
//            Debug.Log("[Countdown] Manual start triggered");
//            StartGameCountdown();
//        }
//    }
//    /// <summary>
//    /// RPC: Non-master player notifies master that they're ready
//    /// </summary>
//    [PunRPC]
//    private void RPC_PlayerDraftComplete(int actorNumber)
//    {
//        if (!PhotonNetwork.IsMasterClient) return;

//        playersReady++;
//        Debug.Log($"[Countdown] Player {actorNumber} ready! ({playersReady}/{totalPlayers})");
//    }

//    /// <summary>
//    /// RPC: Master tells everyone to start waves
//    /// </summary>
//    [PunRPC]
//    private void RPC_StartWaves()
//    {
//        Debug.Log("[Countdown] ✅ Received START signal from Master!");
//        StartGame();
//    }

//    /// <summary>
//    /// Checks if all players are ready
//    /// </summary>
//    private bool AllPlayersReady()
//    {
//        if (!localPlayerReady) return false;

//        if (PhotonNetwork.IsMasterClient)
//        {
//            // Master checks count
//            return playersReady >= totalPlayers;
//        }
//        else
//        {
//            // Non-master just waits for RPC
//            return false; // RPC will call StartGame() directly
//        }
//    }
//}
using UnityEngine;
using TMPro;
using System.Collections;
using Photon.Pun;
using ElementumDefense.Cards;

public class GameStartCountdown : MonoBehaviourPunCallbacks
{
    [Header("Countdown Settings")]
    [SerializeField] private float countdownTime = 5f;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("Game References")]
    public WaveManager waveManager;
    private DraftManager draftManager;

    private bool countdownStarted = false;

    // ✅ DODAJ AWAKE - UKRYJ TEKST NA STARCIE
    private void Awake()
    {
        // KRYTYCZNE - Ukryj tekst odliczania na starcie
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
            Debug.Log("[GameStartCountdown] CountdownText ukryty na starcie.");
        }
    }

    public new void OnEnable()
    {
        base.OnEnable();
    }

    /// <summary>
    /// PUBLICZNA metoda, która rozpoczyna cały proces.
    /// Wywoływana przez DraftManager PO wyborze kart przez wszystkich.
    /// </summary>
    public void StartCountdown()
    {
        if (countdownStarted)
        {
            Debug.LogWarning("[GameStartCountdown] Countdown już wystartował!");
            return;
        }

        Debug.Log("[GameStartCountdown] Otrzymano polecenie startu. Rozpoczynam odliczanie...");
        countdownStarted = true;

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.text = ""; // ✅ Wyczyść "Czekam na innych..."
            Debug.Log("[GameStartCountdown] CountdownText aktywowany.");
        }

        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        float timer = countdownTime;
        while (timer > 0)
        {
            if (countdownText != null)
                countdownText.text = Mathf.CeilToInt(timer).ToString();

            timer -= Time.deltaTime;
            yield return null;
        }
        OnCountdownComplete();
    }

    private void OnCountdownComplete()
    {
        Debug.Log("[GameStartCountdown] Odliczanie zakończone. Uruchamiam sekwencję gry.");
        if (countdownText != null)
            countdownText.text = "GO!";

        StartCoroutine(HideCountdownText());
        StartCoroutine(StartGameSequence());
    }

    private IEnumerator HideCountdownText()
    {
        yield return new WaitForSeconds(1f);
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
            Debug.Log("[GameStartCountdown] CountdownText ukryty po 'GO!'.");
        }
    }

    private IEnumerator StartGameSequence()
    {
        Debug.Log($"[{PhotonNetwork.LocalPlayer.NickName}] Countdown zakończony! Aktywuję karty...");

        draftManager = DraftManager.Instance;
        if (draftManager == null)
        {
            Debug.LogError("[GameStartCountdown] FATAL: DraftManager not found!");
            yield break;
        }

        // Aktywuj karty dopiero teraz
        draftManager.ActivateStarterCards();

        Debug.Log($"[{PhotonNetwork.LocalPlayer.NickName}] Uruchamianie fal...");

        if (waveManager == null)
        {
            waveManager = GetComponentInParent<WaveManager>(true);
            if (waveManager == null)
            {
                Debug.LogError("[GameStartCountdown] WaveManager nie został znaleziony!");
                yield break;
            }
        }

        waveManager.StartWaves();
    }
    /// <summary>
    /// Publiczny dostęp do tekstu odliczania (dla DraftManager)
    /// </summary>
    public TextMeshProUGUI GetCountdownText()
    {
        return countdownText;
    }
}