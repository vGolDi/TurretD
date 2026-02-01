
//using UnityEngine;
//using Photon.Pun;
//using Photon.Realtime;
//using TMPro;
//using UnityEngine.SceneManagement;

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
//    [SerializeField] private string gameSceneName = "SampleScene";

//    [Header("Arena Settings")]
//    [SerializeField] private string[] availableArenaTypes = { "Fire", "Ice", "Earth" };
//    private const string ARENA_TYPE_KEY = "arenaType";

//    private bool isLoadingGame = false;

//    private void Start()
//    {
//        //  ZMIANA: Wy³¹czamy automatyczn¹ synchronizacjê
//        PhotonNetwork.AutomaticallySyncScene = false;
//        PhotonNetwork.GameVersion = gameVersion;

//        UpdateStatus("Connecting to server...");
//        PhotonNetwork.ConnectUsingSettings();

//        if (cancelButton != null) cancelButton.SetActive(false);
//        if (waitingText != null) waitingText.SetActive(false);
//    }

//    #region Photon Callbacks

//    public override void OnConnectedToMaster()
//    {
//        UpdateStatus("Connected! Searching for match...");
//        PhotonNetwork.JoinLobby();
//        Debug.Log("[NetworkManager] Connected to Photon Cloud");
//    }

//    public override void OnJoinedLobby()
//    {
//        Debug.Log("[NetworkManager] Joined lobby");
//        UpdateStatus("Searching for match...");

//        // 1. Pobierz wybrany tryb z PlayerCollection
//        var mode = ElementumDefense.Cards.PlayerCollection.Instance.SelectedGameMode;
//        string modeString = mode.ToString(); // "Casual" lub "Ranked"

//        Debug.Log($"[NetworkManager] Looking for {modeString} game...");

//        // 2. Ustaw filtr: Szukamy pokoju, który ma w³aœciwoœæ "gm" (GameMode) == modeString
//        ExitGames.Client.Photon.Hashtable expectedCustomRoomProperties = new ExitGames.Client.Photon.Hashtable
//        {
//            { "gm", modeString }
//        };

//        PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, maxPlayersPerRoom);
//    }

//    public override void OnJoinedRoom()
//    {
//        UpdateStatus("Room joined!");
//        UpdatePlayerCount();

//        if (cancelButton != null) cancelButton.SetActive(true);

//        if (PhotonNetwork.CurrentRoom.PlayerCount < maxPlayersPerRoom)
//        {
//            if (waitingText != null) waitingText.SetActive(true);
//        }

//        CheckPlayerCount();
//        Debug.Log($"[NetworkManager] Joined room '{PhotonNetwork.CurrentRoom.Name}'");
//    }

//    public override void OnPlayerEnteredRoom(Player newPlayer)
//    {
//        UpdateStatus("Player joined!");
//        UpdatePlayerCount();
//        CheckPlayerCount();
//        Debug.Log($"[NetworkManager] Player {newPlayer.NickName} entered room");
//    }

//    public override void OnPlayerLeftRoom(Player otherPlayer)
//    {
//        UpdateStatus("Player left. Waiting for new opponent...");
//        UpdatePlayerCount();

//        if (waitingText != null) waitingText.SetActive(true);
//        Debug.Log($"[NetworkManager] Player {otherPlayer.NickName} left room");
//    }

//    public override void OnDisconnected(DisconnectCause cause)
//    {
//        UpdateStatus($"Disconnected: {cause}");
//        if (cancelButton != null) cancelButton.SetActive(false);
//        Debug.LogWarning($"[NetworkManager] Disconnected: {cause}");
//    }

//    public override void OnJoinRandomFailed(short returnCode, string message)
//    {
//        UpdateStatus("No available rooms. Creating new one...");
//        CreateRoom();
//    }

//    #endregion

//    #region Public Methods

//    public void CancelMatchmaking()
//    {
//        Debug.Log("[NetworkManager] Cancelling matchmaking...");

//        if (PhotonNetwork.InRoom)
//        {
//            PhotonNetwork.LeaveRoom();
//        }

//        SceneManager.LoadScene("MainMenu");
//    }

//    #endregion

//    #region Private Methods

//    private void CreateRoom()
//    {
//        var mode = ElementumDefense.Cards.PlayerCollection.Instance.SelectedGameMode;
//        string modeString = mode.ToString();

//        RoomOptions roomOptions = new RoomOptions
//        {
//            MaxPlayers = maxPlayersPerRoom,
//            IsVisible = true,
//            IsOpen = true,
//            // 3. WA¯NE: Ustawiamy Custom Properties dla nowo tworzonego pokoju
//            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "gm", modeString } },
//            CustomRoomPropertiesForLobby = new string[] { "gm" } // Te w³aœciwoœci s¹ widoczne w lobby
//        };

//        string roomName = $"{modeString}_{Random.Range(1000, 9999)}";
//        PhotonNetwork.CreateRoom(roomName, roomOptions);
//        Debug.Log($"[NetworkManager] Creating {modeString} room '{roomName}'");
//    }

//    private void CheckPlayerCount()
//    {
//        //if (isLoadingGame)
//        //{
//        //    Debug.Log("[NetworkManager] Already loading game - skipping");
//        //    return;
//        //}

//        //Debug.Log($"[NetworkManager] CheckPlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayersPerRoom}, IsMasterClient: {PhotonNetwork.IsMasterClient}");

//        //if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayersPerRoom)
//        //{
//        //    isLoadingGame = true;
//        //    UpdateStatus("Match found! Starting game...");

//        //    if (waitingText != null) waitingText.SetActive(false);

//        //    Debug.Log("[NetworkManager] Starting game in 1 second...");

//        //    //  Wszyscy gracze ³aduj¹ scenê
//        //    StartCoroutine(LoadGameSceneCoroutine());
//        //}
//        if (isLoadingGame)
//        {
//            Debug.Log("[NetworkManager] Already loading game - skipping");
//            return;
//        }

//        Debug.Log($"[NetworkManager] CheckPlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayersPerRoom}, IsMasterClient: {PhotonNetwork.IsMasterClient}");

//        if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayersPerRoom)
//        {
//            UpdateStatus("Match found! Preparing game...");
//            if (waitingText != null) waitingText.SetActive(false);

//            // Tylko Master Client losuje arenê i uruchamia grê dla wszystkich
//            if (PhotonNetwork.IsMasterClient)
//            {
//                // 1. Losuj arenê
//                string chosenArena = availableArenaTypes[Random.Range(0, availableArenaTypes.Length)];
//                Debug.Log($"[NetworkManager] Master Client wylosowa³ arenê: {chosenArena}");

//                // 2. Ustaw w³aœciwoœæ pokoju, aby wszyscy gracze j¹ znali
//                ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable
//                {
//                    { ARENA_TYPE_KEY, chosenArena }
//                };
//                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

//                // 3. Zamknij pokój, ¿eby nikt wiêcej nie do³¹czy³
//                PhotonNetwork.CurrentRoom.IsOpen = false;

//                // 4. Wywo³aj RPC, ¿eby wszyscy gracze za³adowali scenê
//                Debug.Log("[NetworkManager] Sending RPC to load game scene for all players...");
//                photonView.RPC("RPC_LoadGameScene", RpcTarget.All);
//            }
//        }
//    }
//    [PunRPC]
//    private void RPC_LoadGameScene()
//    {
//        if (isLoadingGame) return;
//        isLoadingGame = true;

//        Debug.Log($"[NetworkManager] Otrzymano sygna³ do za³adowania sceny gry: {gameSceneName}");
//        StartCoroutine(LoadGameSceneCoroutine());
//    }
//    // ===================================

//    private System.Collections.IEnumerator LoadGameSceneCoroutine()
//    {
//        yield return new WaitForSeconds(1f);
//        Debug.Log($"[NetworkManager] Loading scene: {gameSceneName} (Local load)");
//        SceneManager.LoadScene(gameSceneName);
//    }
//    //private System.Collections.IEnumerator LoadGameSceneCoroutine()
//    //{
//    //    yield return new WaitForSeconds(1f);

//    //    Debug.Log($"[NetworkManager] Loading scene: {gameSceneName} (Local load)");

//    //    //  KLUCZOWA ZMIANA: U¿yj normalnego SceneManager
//    //    SceneManager.LoadScene(gameSceneName);
//    //}

//    private void UpdateStatus(string message)
//    {
//        if (statusText != null)
//        {
//            statusText.text = message;
//        }
//        Debug.Log($"[NetworkManager] {message}");
//    }

//    private void UpdatePlayerCount()
//    {
//        if (playerCountText != null && PhotonNetwork.CurrentRoom != null)
//        {
//            int current = PhotonNetwork.CurrentRoom.PlayerCount;
//            int max = PhotonNetwork.CurrentRoom.MaxPlayers;

//            playerCountText.text = $"Players: {current}/{max}";

//            if (current >= max)
//            {
//                playerCountText.color = Color.green;
//            }
//            else
//            {
//                playerCountText.color = Color.cyan;
//            }
//        }
//    }

//    #endregion
//}
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine.SceneManagement;
using ElementumDefense.Cards; // Potrzebne do GameMode i PlayerCollection

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private GameObject waitingText;
    [SerializeField] private GameObject cancelButton;

    [Header("Matchmaking Settings")]
    [SerializeField] private byte maxPlayersPerRoom = 2;
    [SerializeField] private string gameVersion = "0.1";
    [SerializeField] private string gameSceneName = "GameScene"; // Upewnij siê, ¿e nazwa jest poprawna!

    [Header("Arena Settings")]
    [SerializeField] private string[] availableArenaTypes = { "Fire", "Ice", "Earth" };
    private const string ARENA_TYPE_KEY = "arenaType";

    private bool isLoadingGame = false;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.GameVersion = gameVersion;

        // Reset UI na starcie
        if (cancelButton != null) cancelButton.SetActive(false);
        if (waitingText != null) waitingText.SetActive(false);
        UpdatePlayerCountUI(0, 0); // Reset licznika

        // ====================================================
        // POPRAWKA: Sprawdzenie stanu po³¹czenia
        // ====================================================
        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.InRoom)
            {
                // B³¹d: Gracz wszed³ na scenê lobby, ale jest ju¿ w pokoju? Wychodzimy.
                Debug.LogWarning("[NetworkManager] Player already in room. Leaving...");
                PhotonNetwork.LeaveRoom();
            }
            else if (PhotonNetwork.InLobby)
            {
                // Jesteœmy w lobby i po³¹czeni - od razu szukamy meczu
                Debug.Log("[NetworkManager] Already in Lobby. Starting Matchmaking...");
                OnJoinedLobby();
            }
            else
            {
                // Po³¹czeni, ale nie w lobby - wchodzimy do lobby
                Debug.Log("[NetworkManager] Connected but not in Lobby. Joining Lobby...");
                PhotonNetwork.JoinLobby();
            }
        }
        else
        {
            // Niepo³¹czeni - standardowa procedura
            UpdateStatus("Connecting to server...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        Debug.Log("[NetworkManager] Connected to Photon Cloud");
        UpdateStatus("Connected! Joining Lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[NetworkManager] Joined lobby");
        StartMatchmaking();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[NetworkManager] Joined room '{PhotonNetwork.CurrentRoom.Name}'");
        UpdateStatus("Room joined! Waiting for opponent...");

        UpdatePlayerCount(); // Aktualizuj licznik 1/2

        // Poka¿ przycisk wyjœcia
        if (cancelButton != null) cancelButton.SetActive(true);

        // Jeœli czekamy na drugiego gracza
        if (PhotonNetwork.CurrentRoom.PlayerCount < maxPlayersPerRoom)
        {
            if (waitingText != null) waitingText.SetActive(true);
        }

        CheckPlayerCount();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[NetworkManager] Player {newPlayer.NickName} entered room");
        UpdateStatus("Player joined!");
        UpdatePlayerCount();
        CheckPlayerCount();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[NetworkManager] Player {otherPlayer.NickName} left room");
        UpdateStatus("Player left. Waiting for new opponent...");
        UpdatePlayerCount();

        if (waitingText != null) waitingText.SetActive(true);
        // Anuluj ³adowanie gry jeœli ktoœ wyjdzie w ostatniej chwili
        isLoadingGame = false;
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[NetworkManager] Disconnected: {cause}");
        UpdateStatus($"Disconnected: {cause}");
        if (cancelButton != null) cancelButton.SetActive(false);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("[NetworkManager] No match found. Creating new room...");
        UpdateStatus("Creating new room...");
        CreateRoom();
    }

    #endregion

    #region Matchmaking Logic

    private void StartMatchmaking()
    {
        UpdateStatus("Searching for match...");

        // 1. Pobierz tryb gry z PlayerCollection
        string modeString = "Casual"; // Default
        if (PlayerCollection.Instance != null)
        {
            modeString = PlayerCollection.Instance.SelectedGameMode.ToString();
        }

        Debug.Log($"[NetworkManager] Looking for {modeString} game...");

        // 2. Ustaw filtr: Szukamy pokoju z odpowiednim trybem
        ExitGames.Client.Photon.Hashtable expectedCustomRoomProperties = new ExitGames.Client.Photon.Hashtable
        {
            { "gm", modeString }
        };

        // Szukaj pokoju z tymi w³aœciwoœciami
        PhotonNetwork.JoinRandomRoom(expectedCustomRoomProperties, maxPlayersPerRoom);
    }

    private void CreateRoom()
    {
        string modeString = "Casual";
        if (PlayerCollection.Instance != null)
        {
            modeString = PlayerCollection.Instance.SelectedGameMode.ToString();
        }

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true,
            // WA¯NE: Ustawiamy w³aœciwoœci pokoju (gm = GameMode)
            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "gm", modeString } },
            CustomRoomPropertiesForLobby = new string[] { "gm" }
        };

        string roomName = $"{modeString}_{Random.Range(1000, 9999)}";
        PhotonNetwork.CreateRoom(roomName, roomOptions);
        Debug.Log($"[NetworkManager] Creating {modeString} room '{roomName}'");
    }

    #endregion

    #region Game Start Logic

    private void CheckPlayerCount()
    {
        if (isLoadingGame) return;

        Debug.Log($"[NetworkManager] CheckPlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayersPerRoom}");

        if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayersPerRoom)
        {
            UpdateStatus("Match found! Preparing game...");
            if (waitingText != null) waitingText.SetActive(false);
            if (cancelButton != null) cancelButton.SetActive(false); // Blokuj wyjœcie jak gra startuje

            // Tylko Master Client inicjuje start
            if (PhotonNetwork.IsMasterClient)
            {
                // 1. Losuj arenê
                string chosenArena = availableArenaTypes[Random.Range(0, availableArenaTypes.Length)];

                // 2. Zapisz w pokoju
                ExitGames.Client.Photon.Hashtable roomProps = new ExitGames.Client.Photon.Hashtable
                {
                    { ARENA_TYPE_KEY, chosenArena }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

                // 3. Zamknij pokój
                PhotonNetwork.CurrentRoom.IsOpen = false;
                PhotonNetwork.CurrentRoom.IsVisible = false;

                // 4. Wyœlij RPC startu
                Debug.Log("[NetworkManager] Sending RPC to load game scene...");
                photonView.RPC("RPC_LoadGameScene", RpcTarget.All);
            }
        }
    }

    [PunRPC]
    private void RPC_LoadGameScene()
    {
        if (isLoadingGame) return;
        isLoadingGame = true;

        Debug.Log($"[NetworkManager] Loading Game Scene: {gameSceneName}");
        StartCoroutine(LoadGameSceneCoroutine());
    }

    private System.Collections.IEnumerator LoadGameSceneCoroutine()
    {
        // Krótkie opóŸnienie dla efektu
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(gameSceneName);
    }

    #endregion

    #region UI & Helpers

    public void CancelMatchmaking()
    {
        Debug.Log("[NetworkManager] Cancelling matchmaking...");

        // Najwa¿niejsze: WyjdŸ z pokoju, ale zostañ po³¹czony z Photonem
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        // Wróæ do menu
        SceneManager.LoadScene("MainMenu");
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    private void UpdatePlayerCount()
    {
        if (PhotonNetwork.CurrentRoom != null)
        {
            UpdatePlayerCountUI(PhotonNetwork.CurrentRoom.PlayerCount, PhotonNetwork.CurrentRoom.MaxPlayers);
        }
    }

    private void UpdatePlayerCountUI(int current, int max)
    {
        if (playerCountText != null)
        {
            playerCountText.text = $"Players: {current}/{max}";
            playerCountText.color = current >= max ? Color.green : Color.cyan;
        }
    }

    #endregion
}