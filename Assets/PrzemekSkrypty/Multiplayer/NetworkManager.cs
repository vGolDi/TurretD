using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;
using ElementumDefense.Cards;
using ElementumDefense.UI;

public class NetworkManager : MonoBehaviourPunCallbacks
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

    private const string ARENA_TYPE_KEY = "arenaType";

    private LobbyUI lobbyUI;
    private bool isLoadingGame = false;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.GameVersion = gameVersion;

        lobbyUI = FindFirstObjectByType<LobbyUI>();

        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.InRoom)
            {
                Debug.LogWarning(
                    "[NetworkManager] Already in room. " +
                    "Leaving...");
                PhotonNetwork.LeaveRoom();
            }
            else if (PhotonNetwork.InLobby)
            {
                Debug.Log(
                    "[NetworkManager] Already in Lobby.");
                OnJoinedLobby();
            }
            else
            {
                Debug.Log(
                    "[NetworkManager] Connected. " +
                    "Joining Lobby...");
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

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        Debug.Log(
            "[NetworkManager] Connected to Photon");
        UpdateUI("Connected! Joining lobby...");
        lobbyUI?.SetStatusConnected();
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[NetworkManager] Joined lobby");
        lobbyUI?.SetStatusSearching();
        StartMatchmaking();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log(
            $"[NetworkManager] Joined room " +
            $"'{PhotonNetwork.CurrentRoom.Name}'");

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

        UpdateUI("Opponent found!");

        string rank = "UNKNOWN";
        // Could fetch rank from custom properties
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
            $"[NetworkManager] Disconnected: {cause}");
        UpdateUI($"Disconnected: {cause}");
        lobbyUI?.SetStatusError();
        lobbyUI?.SetCancelEnabled(true);
    }

    public override void OnJoinRandomFailed(
        short returnCode, string message)
    {
        Debug.Log(
            "[NetworkManager] No match. Creating...");
        UpdateUI("Creating new room...");
        CreateRoom();
    }

    #endregion

    #region Matchmaking

    private void StartMatchmaking()
    {
        string modeString = "Casual";
        if (PlayerCollection.Instance != null)
            modeString = PlayerCollection.Instance
                .SelectedGameMode.ToString();

        UpdateUI($"Searching for {modeString} match...");

        Debug.Log(
            $"[NetworkManager] Searching " +
            $"{modeString}...");

        var props =
            new ExitGames.Client.Photon.Hashtable
            {
                { "gm", modeString }
            };

        PhotonNetwork.JoinRandomRoom(
            props, maxPlayersPerRoom);
    }

    private void CreateRoom()
    {
        string modeString = "Casual";
        if (PlayerCollection.Instance != null)
            modeString = PlayerCollection.Instance
                .SelectedGameMode.ToString();

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties =
                new ExitGames.Client.Photon.Hashtable
                {
                    { "gm", modeString }
                },
            CustomRoomPropertiesForLobby =
                new string[] { "gm" }
        };

        string roomName =
            $"{modeString}_{Random.Range(1000, 9999)}";
        PhotonNetwork.CreateRoom(roomName, options);

        Debug.Log(
            $"[NetworkManager] Creating " +
            $"'{roomName}'");
    }

    private void CheckForExistingOpponent()
    {
        if (PhotonNetwork.CurrentRoom == null) return;

        foreach (var kvp in
            PhotonNetwork.CurrentRoom.Players)
        {
            Player p = kvp.Value;
            if (!p.IsLocal)
            {
                lobbyUI?.SetOpponentJoined(
                    p.NickName, "UNKNOWN");
                break;
            }
        }
    }

    #endregion

    #region Game Start

    private void CheckPlayerCount()
    {
        if (isLoadingGame) return;
        if (PhotonNetwork.CurrentRoom == null) return;

        int count =
            PhotonNetwork.CurrentRoom.PlayerCount;
        int max =
            PhotonNetwork.CurrentRoom.MaxPlayers;

        Debug.Log(
            $"[NetworkManager] Players: {count}/{max}");

        if (count >= max)
        {
            UpdateUI("Match found! Preparing...");
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
                    "[NetworkManager] Starting game. " +
                    $"Arena: {arena}");
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
            SceneManager.LoadSceneAsync(gameSceneName);
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
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    #endregion

    #region UI Helpers

    private void UpdateUI(string message)
    {
        lobbyUI?.UpdateStatus(message);
    }

    public void CancelMatchmaking()
    {
        Debug.Log(
            "[NetworkManager] Cancelling...");

        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();

        SceneManager.LoadScene("MainMenu");
    }

    #endregion
}
//using UnityEngine;
//using Photon.Pun;
//using Photon.Realtime;
//using TMPro;
//using UnityEngine.SceneManagement;
//using ElementumDefense.Cards; // Potrzebne do GameMode i PlayerCollection

//public class NetworkManager : MonoBehaviourPunCallbacks
//{
//    [Header("UI References")]
//    [SerializeField] private TMP_Text statusText;
//    [SerializeField] private TMP_Text playerCountText;
//    [SerializeField] private GameObject waitingText;
//    [SerializeField] private GameObject cancelButton;

//    [Header("Matchmaking Settings")]
//    [SerializeField] private byte maxPlayersPerRoom = 2;
//    [SerializeField] private string gameVersion = "0.1";
//    [SerializeField] private string gameSceneName = "GameScene"; // Upewnij siê, ¿e nazwa jest poprawna!

//    [Header("Arena Settings")]
//    [SerializeField] private string[] availableArenaTypes = { "Fire", "Ice", "Earth" };
//    private const string ARENA_TYPE_KEY = "arenaType";

//    private bool isLoadingGame = false;

//    private void Start()
//    {
//        PhotonNetwork.AutomaticallySyncScene = false;
//        PhotonNetwork.GameVersion = gameVersion;

//        // Reset UI na starcie
//        if (cancelButton != null) cancelButton.SetActive(false);
//        if (waitingText != null) waitingText.SetActive(false);
//        UpdatePlayerCountUI(0, 0); // Reset licznika

//        // ====================================================
//        // POPRAWKA: Sprawdzenie stanu po³¹czenia
//        // ====================================================
//        if (PhotonNetwork.IsConnected)
//        {
//            if (PhotonNetwork.InRoom)
//            {
//                // B³¹d: Gracz wszed³ na scenê lobby, ale jest ju¿ w pokoju? Wychodzimy.
//                Debug.LogWarning("[NetworkManager] Player already in room. Leaving...");
//                PhotonNetwork.LeaveRoom();
//            }
//            else if (PhotonNetwork.InLobby)
//            {
//                // Jesteœmy w lobby i po³¹czeni - od razu szukamy meczu
//                Debug.Log("[NetworkManager] Already in Lobby. Starting Matchmaking...");
//                OnJoinedLobby();
//            }
//            else
//            {
//                // Po³¹czeni, ale nie w lobby - wchodzimy do lobby
//                Debug.Log("[NetworkManager] Connected but not in Lobby. Joining Lobby...");
//                PhotonNetwork.JoinLobby();
//            }
//        }
//        else
//        {
//            // Niepo³¹czeni - standardowa procedura
//            UpdateStatus("Connecting to server...");
//            PhotonNetwork.ConnectUsingSettings();
//        }
//    }

//    #region Photon Callbacks

//    public override void OnConnectedToMaster()
//    {
//        Debug.Log("[NetworkManager] Connected to Photon Cloud");
//        UpdateStatus("Connected! Joining Lobby...");
//        PhotonNetwork.JoinLobby();
//    }

//    public override void OnJoinedLobby()
//    {
//        Debug.Log("[NetworkManager] Joined lobby");
//        StartMatchmaking();
//    }

//    public override void OnJoinedRoom()
//    {
//        Debug.Log($"[NetworkManager] Joined room '{PhotonNetwork.CurrentRoom.Name}'");
//        UpdateStatus("Room joined! Waiting for opponent...");

//        UpdatePlayerCount(); // Aktualizuj licznik 1/2

//        // Poka¿ przycisk wyjœcia
//        if (cancelButton != null) cancelButton.SetActive(true);

//        // Jeœli czekamy na drugiego gracza
//        if (PhotonNetwork.CurrentRoom.PlayerCount < maxPlayersPerRoom)
//        {
//            if (waitingText != null) waitingText.SetActive(true);
//        }

//        CheckPlayerCount();
//    }

//    public override void OnPlayerEnteredRoom(Player newPlayer)
//    {
//        Debug.Log($"[NetworkManager] Player {newPlayer.NickName} entered room");
//        UpdateStatus("Player joined!");
//        UpdatePlayerCount();
//        CheckPlayerCount();
//    }

//    public override void OnPlayerLeftRoom(Player otherPlayer)
//    {
//        Debug.Log($"[NetworkManager] Player {otherPlayer.NickName} left room");
//        UpdateStatus("Player left. Waiting for new opponent...");
//        UpdatePlayerCount();

//        if (waitingText != null) waitingText.SetActive(true);
//        // Anuluj ³adowanie gry jeœli ktoœ wyjdzie w ostatniej chwili
//        isLoadingGame = false;
//    }

//    public override void OnDisconnected(DisconnectCause cause)
//    {
//        Debug.LogWarning($"[NetworkManager] Disconnected: {cause}");
//        UpdateStatus($"Disconnected: {cause}");
//        if (cancelButton != null) cancelButton.SetActive(false);
//    }

//    public override void OnJoinRandomFailed(short returnCode, string message)
//    {
//        Debug.Log("[NetworkManager] No match found. Creating new room...");
//        UpdateStatus("Creating new room...");
//        CreateRoom();
//    }

//    #endregion

//    #region Matchmaking Logic

//    private void StartMatchmaking()
//    {
//        UpdateStatus("Searching for match...");

//        // 1. Pobierz tryb gry z PlayerCollection
//        string modeString = "Casual"; // Default
//        if (PlayerCollection.Instance != null)
//        {
//            modeString = PlayerCollection.Instance.SelectedGameMode.ToString();
//        }

//        Debug.Log($"[NetworkManager] Looking for {modeString} game...");

//        // 2. Ustaw filtr: Szukamy pokoju z odpowiednim trybem
//        ExitGames.Client.Photon.Hashtable expectedCustomRoomProperties = new ExitGames.Client.Photon.Hashtable
//        {
//            { "gm", modeString }
//        };

//        // Szukaj pokoju z tymi w³aœciwoœciami
//        PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, maxPlayersPerRoom);
//    }

//    private void CreateRoom()
//    {
//        string modeString = "Casual";
//        if (PlayerCollection.Instance != null)
//        {
//            modeString = PlayerCollection.Instance.SelectedGameMode.ToString();
//        }

//        RoomOptions roomOptions = new RoomOptions
//        {
//            MaxPlayers = maxPlayersPerRoom,
//            IsVisible = true,
//            IsOpen = true,
//            // WA¯NE: Ustawiamy w³aœciwoœci pokoju (gm = GameMode)
//            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "gm", modeString } },
//            CustomRoomPropertiesForLobby = new string[] { "gm" }
//        };

//        string roomName = $"{modeString}_{Random.Range(1000, 9999)}";
//        PhotonNetwork.CreateRoom(roomName, roomOptions);
//        Debug.Log($"[NetworkManager] Creating {modeString} room '{roomName}'");
//    }

//    #endregion

//    #region Game Start Logic

//    private void CheckPlayerCount()
//    {
//        if (isLoadingGame) return;

//        Debug.Log($"[NetworkManager] CheckPlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayersPerRoom}");

//        if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayersPerRoom)
//        {
//            UpdateStatus("Match found! Preparing game...");
//            if (waitingText != null) waitingText.SetActive(false);
//            if (cancelButton != null) cancelButton.SetActive(false); // Blokuj wyjœcie jak gra startuje

//            // Tylko Master Client inicjuje start
//            if (PhotonNetwork.IsMasterClient)
//            {
//                // 1. Losuj arenê
//                string chosenArena = availableArenaTypes[Random.Range(0, availableArenaTypes.Length)];

//                // 2. Zapisz w pokoju
//                ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable
//                {
//                    { ARENA_TYPE_KEY, chosenArena }
//                };
//                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

//                // 3. Zamknij pokój
//                PhotonNetwork.CurrentRoom.IsOpen = false;
//                PhotonNetwork.CurrentRoom.IsVisible = false;

//                // 4. Wyœlij RPC startu
//                Debug.Log("[NetworkManager] Sending RPC to load game scene...");
//                photonView.RPC("RPC_LoadGameScene", RpcTarget.All);
//            }
//        }
//    }

//    [PunRPC]
//    private void RPC_LoadGameScene()
//    {
//        if (isLoadingGame) return;
//        isLoadingGame = true;

//        Debug.Log($"[NetworkManager] Loading Game Scene: {gameSceneName}");
//        StartCoroutine(LoadGameSceneCoroutine());
//    }

//    private System.Collections.IEnumerator LoadGameSceneCoroutine()
//    {
//        // Krótkie opóŸnienie dla efektu
//        yield return new WaitForSeconds(1f);
//        SceneManager.LoadScene(gameSceneName);
//    }

//    #endregion

//    #region UI & Helpers

//    public void CancelMatchmaking()
//    {
//        Debug.Log("[NetworkManager] Cancelling matchmaking...");

//        // Najwa¿niejsze: WyjdŸ z pokoju, ale zostañ po³¹czony z Photonem
//        if (PhotonNetwork.InRoom)
//        {
//            PhotonNetwork.LeaveRoom();
//        }

//        // Wróæ do menu
//        SceneManager.LoadScene("MainMenu");
//    }

//    private void UpdateStatus(string message)
//    {
//        if (statusText != null) statusText.text = message;
//    }

//    private void UpdatePlayerCount()
//    {
//        if (PhotonNetwork.CurrentRoom != null)
//        {
//            UpdatePlayerCountUI(PhotonNetwork.CurrentRoom.PlayerCount, PhotonNetwork.CurrentRoom.MaxPlayers);
//        }
//    }

//    private void UpdatePlayerCountUI(int current, int max)
//    {
//        if (playerCountText != null)
//        {
//            playerCountText.text = $"Players: {current}/{max}";
//            playerCountText.color = current >= max ? Color.green : Color.cyan;
//        }
//    }

//    #endregion
//}