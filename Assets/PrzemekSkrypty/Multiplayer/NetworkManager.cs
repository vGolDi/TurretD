using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using ElementumDefense.Cards;
using ElementumDefense.Ranked;
using ElementumDefense.UI;

public class NetworkManager :
    MonoBehaviourPunCallbacks
{
    [Header("Matchmaking Settings")]
    [SerializeField]
    private byte maxPlayersPerRoom = 2;
    [SerializeField]
    private string gameVersion = "0.1";
    [SerializeField]
    private string gameSceneName = "GameScene";

    [Header("Arena Settings")]
    [SerializeField]
    private string[] availableArenaTypes =
        { "Fire", "Ice", "Earth" };

    // ==========================================
    // PROPERTY KEYS
    // ==========================================

    private const string ARENA_TYPE_KEY =
        "arenaType";
    private const string ELO_PROP_KEY = "elo";
    private const string BUCKET_PROP_KEY =
        "eloBucket";
    private const string GAMEMODE_PROP_KEY = "gm";
    private const string HOST_ELO_KEY = "hostElo";

    // ==========================================
    // STATE
    // ==========================================

    private LobbyUI lobbyUI;
    private bool isLoadingGame = false;

    // Ranked matchmaking state
    private int myBucket;
    private int searchAttempt = 0;
    private bool isRankedSearch = false;

    // ==========================================
    // START
    // ==========================================

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.GameVersion = gameVersion;

        lobbyUI = FindFirstObjectByType<LobbyUI>();

        // Ustaw ELO jako custom property gracza
        SyncPlayerEloProperty();

        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.InRoom)
            {
                Debug.LogWarning(
                    "[NetworkManager] " +
                    "Already in room. Leaving...");
                PhotonNetwork.LeaveRoom();
            }
            else if (PhotonNetwork.InLobby)
            {
                Debug.Log(
                    "[NetworkManager] " +
                    "Already in Lobby.");
                OnJoinedLobby();
            }
            else
            {
                UpdateUI("Joining lobby...");
                PhotonNetwork.JoinLobby();
            }
        }
        else
        {
            UpdateUI("Connecting to server...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    // ==========================================
    // PHOTON PLAYER PROPERTIES
    // ==========================================

    /// <summary>
    /// Wysyła ELO gracza jako Photon Custom
    /// Property, żeby przeciwnik mógł je
    /// odczytać w trakcie meczu.
    /// </summary>
    private void SyncPlayerEloProperty()
    {
        int elo =
            PlayerCollection.Instance?.GetElo()
            ?? EloCalculator.DEFAULT_ELO;

        var props =
            new ExitGames.Client.Photon.Hashtable
            {
                { ELO_PROP_KEY, elo }
            };
        PhotonNetwork.LocalPlayer
            .SetCustomProperties(props);

        Debug.Log(
            $"[NetworkManager] Synced ELO " +
            $"property: {elo}");
    }

    // ==========================================
    // PHOTON CALLBACKS
    // ==========================================

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        Debug.Log(
            "[NetworkManager] Connected to Photon");
        UpdateUI("Connected! Joining lobby...");
        lobbyUI?.SetStatusConnected();

        // Odśwież ELO property po reconnect
        SyncPlayerEloProperty();

        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log(
            "[NetworkManager] Joined lobby");
        lobbyUI?.SetStatusSearching();
        StartMatchmaking();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log(
            $"[NetworkManager] Joined room " +
            $"'{PhotonNetwork.CurrentRoom.Name}'");

        // Weryfikacja ELO (safety check)
        if (isRankedSearch)
        {
            LogEloCompatibility();
        }

        UpdateUI("Waiting for opponent...");
        lobbyUI?.SetStatusConnected();
        lobbyUI?.UpdatePlayerCount(
            PhotonNetwork.CurrentRoom.PlayerCount,
            PhotonNetwork.CurrentRoom.MaxPlayers);
        lobbyUI?.SetCancelEnabled(true);

        CheckForExistingOpponent();
        CheckPlayerCount();
    }

    public override void OnPlayerEnteredRoom(
        Player newPlayer)
    {
        Debug.Log(
            $"[NetworkManager] Player joined: " +
            $"{newPlayer.NickName}");

        // Odczytaj rangę przeciwnika
        string rank = GetPlayerRankDisplay(
            newPlayer);

        UpdateUI("Opponent found!");
        lobbyUI?.SetOpponentJoined(
            newPlayer.NickName, rank);
        lobbyUI?.UpdatePlayerCount(
            PhotonNetwork.CurrentRoom.PlayerCount,
            PhotonNetwork.CurrentRoom.MaxPlayers);

        CheckPlayerCount();
    }

    public override void OnPlayerLeftRoom(
        Player otherPlayer)
    {
        Debug.Log(
            $"[NetworkManager] Player left: " +
            $"{otherPlayer.NickName}");

        UpdateUI("Opponent left. Searching...");
        lobbyUI?.SetOpponentLeft();
        lobbyUI?.UpdatePlayerCount(
            PhotonNetwork.CurrentRoom.PlayerCount,
            PhotonNetwork.CurrentRoom.MaxPlayers);
        lobbyUI?.SetCancelEnabled(true);
        isLoadingGame = false;
    }

    public override void OnDisconnected(
        DisconnectCause cause)
    {
        Debug.LogWarning(
            $"[NetworkManager] " +
            $"Disconnected: {cause}");
        UpdateUI($"Disconnected: {cause}");
        lobbyUI?.SetStatusError();
        lobbyUI?.SetCancelEnabled(true);
    }

    public override void OnJoinRandomFailed(
        short returnCode, string message)
    {
        if (isRankedSearch)
        {
            HandleRankedSearchFailed();
        }
        else
        {
            Debug.Log(
                "[NetworkManager] No match. " +
                "Creating...");
            UpdateUI("Creating new room...");
            CreateCasualRoom();
        }
    }

    #endregion

    // ==========================================
    // MATCHMAKING — DISPATCHER
    // ==========================================

    #region Matchmaking

    private void StartMatchmaking()
    {
        GameMode mode = GameMode.Casual;
        if (PlayerCollection.Instance != null)
            mode = PlayerCollection.Instance
                .SelectedGameMode;

        isRankedSearch =
            (mode == GameMode.Ranked);

        if (isRankedSearch)
            StartRankedMatchmaking();
        else
            StartCasualMatchmaking();
    }

    // ==========================================
    // CASUAL MATCHMAKING
    // ==========================================

    private void StartCasualMatchmaking()
    {
        UpdateUI(
            "Searching for Casual match...");
        Debug.Log(
            "[NetworkManager] Searching Casual...");

        var props =
            new ExitGames.Client.Photon.Hashtable
            {
                { GAMEMODE_PROP_KEY, "Casual" }
            };

        PhotonNetwork.JoinRandomRoom(
            props, maxPlayersPerRoom);
    }

    private void CreateCasualRoom()
    {
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties =
                new ExitGames.Client.Photon
                    .Hashtable
                {
                    { GAMEMODE_PROP_KEY, "Casual" }
                },
            CustomRoomPropertiesForLobby =
                new string[] { GAMEMODE_PROP_KEY }
        };

        string roomName =
            $"Casual_{Random.Range(1000, 9999)}";
        PhotonNetwork.CreateRoom(
            roomName, options);

        Debug.Log(
            $"[NetworkManager] Creating " +
            $"'{roomName}'");
    }

    // ==========================================
    // RANKED MATCHMAKING
    // ==========================================

    /// <summary>
    /// Rozpoczyna szukanie rankingowe:
    /// 1) Oblicza bucket gracza
    /// 2) Szuka pokoju w tym buckecie
    /// 3) Jeśli brak → ±1 bucket
    /// 4) Jeśli brak → tworzy pokój
    /// </summary>
    private void StartRankedMatchmaking()
    {
        int elo =
            PlayerCollection.Instance?.GetElo()
            ?? EloCalculator.DEFAULT_ELO;

        myBucket = EloCalculator.GetBucket(elo);
        searchAttempt = 0;

        (int searchMin, int searchMax) =
            EloCalculator.GetSearchRange(elo);

        string rankName =
            EloCalculator.GetRankName(elo);

        Debug.Log(
            $"[Ranked] Starting search. " +
            $"ELO: {elo}, Bucket: {myBucket}, " +
            $"Rank: {rankName}");

        UpdateUI(
            $"Searching Ranked... " +
            $"{rankName} ({elo} ELO)\n" +
            $"Range: {searchMin}–{searchMax}");

        TryJoinRankedRoom(myBucket);
    }

    /// <summary>
    /// Próbuje dołączyć do pokoju w danym buckecie.
    /// </summary>
    private void TryJoinRankedRoom(int bucket)
    {
        Debug.Log(
            $"[Ranked] Trying bucket {bucket} " +
            $"(attempt {searchAttempt})");

        var props =
            new ExitGames.Client.Photon.Hashtable
            {
                { GAMEMODE_PROP_KEY, "Ranked" },
                { BUCKET_PROP_KEY, bucket }
            };

        PhotonNetwork.JoinRandomRoom(
            props, maxPlayersPerRoom);
    }

    /// <summary>
    /// Obsługuje brak pokoju w szukanym buckecie.
    /// Rozszerza szukanie o sąsiednie buckety,
    /// potem tworzy pokój.
    /// </summary>
    private void HandleRankedSearchFailed()
    {
        searchAttempt++;

        // Krok 1: niższy bucket
        if (searchAttempt == 1 && myBucket > 0)
        {
            UpdateUI("Expanding search range...");
            TryJoinRankedRoom(myBucket - 1);
            return;
        }

        // Krok 2: wyższy bucket
        // (albo jeśli bucket był 0)
        if (searchAttempt <= 2)
        {
            UpdateUI("Expanding search range...");
            TryJoinRankedRoom(myBucket + 1);
            return;
        }

        // Krok 3: stwórz pokój
        UpdateUI("Creating ranked room...");
        CreateRankedRoom();
    }

    /// <summary>
    /// Tworzy pokój rankingowy z bucketem
    /// i ELO hosta.
    /// </summary>
    private void CreateRankedRoom()
    {
        int elo =
            PlayerCollection.Instance?.GetElo()
            ?? EloCalculator.DEFAULT_ELO;

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties =
                new ExitGames.Client.Photon
                    .Hashtable
                {
                    { GAMEMODE_PROP_KEY, "Ranked" },
                    { BUCKET_PROP_KEY, myBucket },
                    { HOST_ELO_KEY, elo }
                },
            CustomRoomPropertiesForLobby =
                new string[]
                {
                    GAMEMODE_PROP_KEY,
                    BUCKET_PROP_KEY
                }
        };

        string roomName =
            $"Ranked_B{myBucket}_" +
            $"{Random.Range(1000, 9999)}";
        PhotonNetwork.CreateRoom(
            roomName, options);

        Debug.Log(
            $"[Ranked] Created room " +
            $"'{roomName}' " +
            $"(Bucket: {myBucket}, ELO: {elo})");
    }

    // ==========================================
    // ELO VERIFICATION
    // ==========================================

    /// <summary>
    /// Loguje kompatybilność ELO w pokoju.
    /// Nie rozłącza — bucket system powinien
    /// zapobiec dużym rozbieżnościom.
    /// </summary>
    private void LogEloCompatibility()
    {
        int myElo =
            PlayerCollection.Instance?.GetElo()
            ?? EloCalculator.DEFAULT_ELO;

        // Sprawdź ELO hosta
        var roomProps =
            PhotonNetwork.CurrentRoom
                .CustomProperties;

        if (roomProps.TryGetValue(
            HOST_ELO_KEY, out object hostEloObj))
        {
            int hostElo = (int)hostEloObj;
            bool compatible =
                EloCalculator.CanMatch(
                    myElo, hostElo);

            Debug.Log(
                $"[Ranked] ELO check — " +
                $"Me: {myElo}, Host: {hostElo}, " +
                $"Compatible: {compatible}");

            if (!compatible)
            {
                Debug.LogWarning(
                    $"[Ranked] Wide ELO gap! " +
                    $"{myElo} vs {hostElo}");
            }
        }

        // Sprawdź innych graczy
        foreach (var kvp in
            PhotonNetwork.CurrentRoom.Players)
        {
            Player p = kvp.Value;
            if (!p.IsLocal &&
                p.CustomProperties.TryGetValue(
                    ELO_PROP_KEY,
                    out object eloObj))
            {
                int otherElo = (int)eloObj;
                bool ok = EloCalculator.CanMatch(
                    myElo, otherElo);

                Debug.Log(
                    $"[Ranked] vs {p.NickName}: " +
                    $"{otherElo} ELO, " +
                    $"OK: {ok}");
            }
        }
    }

    /// <summary>
    /// Zwraca czytelny string rangi gracza
    /// (do wyświetlenia w lobby).
    /// </summary>
    private string GetPlayerRankDisplay(
        Player player)
    {
        if (player.CustomProperties.TryGetValue(
            ELO_PROP_KEY, out object eloObj))
        {
            int elo = (int)eloObj;
            return $"{EloCalculator.GetRankName(elo)}" +
                   $" ({elo})";
        }

        return "UNRANKED";
    }

    private void CheckForExistingOpponent()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return;

        foreach (var kvp in
            PhotonNetwork.CurrentRoom.Players)
        {
            Player p = kvp.Value;
            if (!p.IsLocal)
            {
                string rank =
                    GetPlayerRankDisplay(p);
                lobbyUI?.SetOpponentJoined(
                    p.NickName, rank);
                break;
            }
        }
    }

    #endregion

    // ==========================================
    // GAME START
    // ==========================================

    #region Game Start

    private void CheckPlayerCount()
    {
        if (isLoadingGame) return;
        if (PhotonNetwork.CurrentRoom == null)
            return;

        int count =
            PhotonNetwork.CurrentRoom.PlayerCount;
        int max =
            PhotonNetwork.CurrentRoom.MaxPlayers;

        Debug.Log(
            $"[NetworkManager] " +
            $"Players: {count}/{max}");

        if (count >= max)
        {
            UpdateUI(
                "Match found! Preparing...");
            lobbyUI?.SetCancelEnabled(false);

            if (PhotonNetwork.IsMasterClient)
            {
                string arena =
                    availableArenaTypes[
                        Random.Range(0,
                            availableArenaTypes
                                .Length)];

                var roomProps =
                    new ExitGames.Client.Photon
                        .Hashtable
                    {
                        { ARENA_TYPE_KEY, arena }
                    };
                PhotonNetwork.CurrentRoom
                    .SetCustomProperties(roomProps);

                PhotonNetwork.CurrentRoom.IsOpen =
                    false;
                PhotonNetwork.CurrentRoom.IsVisible =
                    false;

                Debug.Log(
                    "[NetworkManager] " +
                    $"Starting game. Arena: {arena}");
                photonView.RPC(
                    "RPC_LoadGameScene",
                    RpcTarget.All);
            }
        }
    }

    [PunRPC]
    private void RPC_LoadGameScene()
    {
        if (isLoadingGame) return;
        isLoadingGame = true;

        Debug.Log(
            $"[NetworkManager] Loading: " +
            $"{gameSceneName}");

        lobbyUI?.ShowMatchFound();
        StartCoroutine(LoadGameSceneCoroutine());
    }

    private System.Collections.IEnumerator
        LoadGameSceneCoroutine()
    {
        lobbyUI?.UpdateMatchFoundProgress(
            0.2f, "Preparing arena...");
        yield return new WaitForSeconds(0.5f);

        lobbyUI?.UpdateMatchFoundProgress(
            0.5f, "Loading assets...");

        AsyncOperation asyncLoad =
            SceneManager.LoadSceneAsync(
                gameSceneName);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            float progress =
                Mathf.Clamp01(
                    asyncLoad.progress / 0.9f);

            float displayProgress =
                0.5f + progress * 0.5f;

            lobbyUI?.UpdateMatchFoundProgress(
                displayProgress,
                $"Loading... " +
                $"{Mathf.RoundToInt(displayProgress * 100)}%");

            if (asyncLoad.progress >= 0.9f)
            {
                lobbyUI?.UpdateMatchFoundProgress(
                    1f, "Entering arena...");
                yield return
                    new WaitForSeconds(0.5f);
                asyncLoad.allowSceneActivation =
                    true;
            }

            yield return null;
        }
    }

    #endregion

    // ==========================================
    // UI HELPERS
    // ==========================================

    #region UI Helpers

    private void UpdateUI(string message)
    {
        lobbyUI?.UpdateStatus(message);
    }

    public void CancelMatchmaking()
    {
        Debug.Log(
            "[NetworkManager] Cancelling...");

        isRankedSearch = false;
        searchAttempt = 0;

        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();

        SceneManager.LoadScene("MainMenu");
    }

    #endregion
}