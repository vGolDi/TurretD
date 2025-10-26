//using UnityEngine;
//using Photon.Pun;
//using Photon.Realtime;
//using TMPro;

//public class NetworkManager : MonoBehaviourPunCallbacks
//{
//    [Header("UI References")]
//    [SerializeField] private TMP_Text statusText;
//    [SerializeField] private TMP_Text playerCountText; 
//    [SerializeField] private GameObject waitingText; 
//    [SerializeField] private GameObject loadingSpinner; 
//    [SerializeField] private GameObject cancelButton;

//    [Header("Matchmaking Settings")]
//    [SerializeField] private byte maxPlayersPerRoom = 2;
//    [SerializeField] private string gameVersion = "0.1";

//    private bool isLoadingGame = false;

//    private void Start()
//    {
//        PhotonNetwork.AutomaticallySyncScene = true;
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
//        PhotonNetwork.JoinRandomRoom();
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

//    public override void OnCreateRoomFailed(short returnCode, string message)
//    {
//        UpdateStatus($"Failed to create room: {message}");
//        Debug.LogError($"[NetworkManager] Create room failed: {message}");
//    }

//    public override void OnJoinRoomFailed(short returnCode, string message)
//    {
//        UpdateStatus($"Failed to join room: {message}");
//        Debug.LogError($"[NetworkManager] Join room failed: {message}");
//    }

//    public override void OnJoinRandomFailed(short returnCode, string message)
//    {
//        UpdateStatus("No available rooms. Creating new one...");
//        CreateRoom();
//    }

//    #endregion

//    #region Public Methods

//    /// <summary>
//    /// Button callback - leaves current room
//    /// </summary>
//    public void CancelMatchmaking()
//    {
//        Debug.Log("[NetworkManager] Cancelling matchmaking...");

//        if (PhotonNetwork.InRoom)
//        {
//            PhotonNetwork.LeaveRoom();
//        }

//        // Return to main menu
//        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
//    }

//    #endregion

//    #region Private Methods

//    private void CreateRoom()
//    {
//        RoomOptions roomOptions = new RoomOptions
//        {
//            MaxPlayers = maxPlayersPerRoom,
//            IsVisible = true,
//            IsOpen = true
//        };

//        string roomName = $"Room_{Random.Range(1000, 9999)}";
//        PhotonNetwork.CreateRoom(roomName, roomOptions);

//        Debug.Log($"[NetworkManager] Creating room '{roomName}'");
//    }

//    private void CheckPlayerCount()
//    {

//        if (isLoadingGame)
//        {
//            Debug.Log("[NetworkManager] Already loading game - skipping");
//            return;
//        }
//        Debug.Log($"[NetworkManager] CheckPlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayersPerRoom}, IsMasterClient: {PhotonNetwork.IsMasterClient}");

//        if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayersPerRoom && PhotonNetwork.IsMasterClient)
//        {
//            isLoadingGame = true;

//            UpdateStatus("Match found! Starting game...");

//            if (waitingText != null) waitingText.SetActive(false);

//            Debug.Log("[NetworkManager] Invoking LoadGameScene in 2 seconds...");
//            Invoke(nameof(LoadGameScene), 2f);
//        }
//        else
//        {
//            Debug.Log($"[NetworkManager] Not loading: PlayerCount={PhotonNetwork.CurrentRoom.PlayerCount}, IsMaster={PhotonNetwork.IsMasterClient}");
//        }
//    }

//    private void LoadGameScene()
//    {
//        //if (!isLoadingGame)
//        //{
//        //    Debug.LogWarning("[NetworkManager] LoadGameScene called but flag is false!");
//        //    return;
//        //}
//        //Debug.Log("[NetworkManager] LoadGameScene called!");

//        //if (PhotonNetwork.IsMasterClient)
//        //{
//        //    Debug.Log("[NetworkManager] Loading SampleScene...");
//        //    PhotonNetwork.LoadLevel("SampleScene");
//        //}
//        //else
//        //{
//        //    Debug.LogWarning("[NetworkManager] Not master client - cannot load scene!");
//        //}
//        if (!isLoadingGame)
//        {
//            Debug.LogWarning("[NetworkManager] LoadGameScene called but flag is false!");
//            return;
//        }

//        Debug.Log("[NetworkManager] LoadGameScene called!");

//        if (PhotonNetwork.IsMasterClient)
//        {
//            // SprawdŸ czy scena istnieje w Build Settings
//            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
//            Debug.Log($"[NetworkManager] Scenes in build: {sceneCount}");

//            if (UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath("SampleScene") == -1)
//            {
//                Debug.LogError("[NetworkManager] SampleScene is NOT in Build Settings!");
//                return;
//            }

//            Debug.Log("[NetworkManager] Loading SampleScene...");
//            PhotonNetwork.LoadLevel("SampleScene");
//        }
//        else
//        {
//            Debug.LogWarning("[NetworkManager] Not master client - waiting for scene sync...");
//        }
//    }

//    private void UpdateStatus(string message)
//    {
//        if (statusText != null)
//        {
//            statusText.text = message;
//        }
//        Debug.Log($"[NetworkManager] {message}");
//    }

//    /// <summary>
//    /// Updates player count display
//    /// </summary>
//    private void UpdatePlayerCount()
//    {
//        if (playerCountText != null && PhotonNetwork.CurrentRoom != null)
//        {
//            int current = PhotonNetwork.CurrentRoom.PlayerCount;
//            int max = PhotonNetwork.CurrentRoom.MaxPlayers;

//            playerCountText.text = $"Players: {current}/{max}";

//            // Color coding
//            if (current >= max)
//            {
//                playerCountText.color = Color.green; // Ready!
//            }
//            else
//            {
//                playerCountText.color = Color.cyan; // Waiting
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
    [SerializeField] private string gameSceneName = "SampleScene";

    private bool isLoadingGame = false;

    private void Start()
    {
        //  ZMIANA: Wy³¹czamy automatyczn¹ synchronizacjê
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.GameVersion = gameVersion;

        UpdateStatus("Connecting to server...");
        PhotonNetwork.ConnectUsingSettings();

        if (cancelButton != null) cancelButton.SetActive(false);
        if (waitingText != null) waitingText.SetActive(false);
    }

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        UpdateStatus("Connected! Searching for match...");
        PhotonNetwork.JoinLobby();
        Debug.Log("[NetworkManager] Connected to Photon Cloud");
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[NetworkManager] Joined lobby");
        UpdateStatus("Searching for match...");
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinedRoom()
    {
        UpdateStatus("Room joined!");
        UpdatePlayerCount();

        if (cancelButton != null) cancelButton.SetActive(true);

        if (PhotonNetwork.CurrentRoom.PlayerCount < maxPlayersPerRoom)
        {
            if (waitingText != null) waitingText.SetActive(true);
        }

        CheckPlayerCount();
        Debug.Log($"[NetworkManager] Joined room '{PhotonNetwork.CurrentRoom.Name}'");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateStatus("Player joined!");
        UpdatePlayerCount();
        CheckPlayerCount();
        Debug.Log($"[NetworkManager] Player {newPlayer.NickName} entered room");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateStatus("Player left. Waiting for new opponent...");
        UpdatePlayerCount();

        if (waitingText != null) waitingText.SetActive(true);
        Debug.Log($"[NetworkManager] Player {otherPlayer.NickName} left room");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        UpdateStatus($"Disconnected: {cause}");
        if (cancelButton != null) cancelButton.SetActive(false);
        Debug.LogWarning($"[NetworkManager] Disconnected: {cause}");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        UpdateStatus("No available rooms. Creating new one...");
        CreateRoom();
    }

    #endregion

    #region Public Methods

    public void CancelMatchmaking()
    {
        Debug.Log("[NetworkManager] Cancelling matchmaking...");

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }

        SceneManager.LoadScene("MainMenu");
    }

    #endregion

    #region Private Methods

    private void CreateRoom()
    {
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            IsOpen = true
        };

        string roomName = $"Room_{Random.Range(1000, 9999)}";
        PhotonNetwork.CreateRoom(roomName, roomOptions);
        Debug.Log($"[NetworkManager] Creating room '{roomName}'");
    }

    private void CheckPlayerCount()
    {
        if (isLoadingGame)
        {
            Debug.Log("[NetworkManager] Already loading game - skipping");
            return;
        }

        Debug.Log($"[NetworkManager] CheckPlayerCount: {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayersPerRoom}, IsMasterClient: {PhotonNetwork.IsMasterClient}");

        if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayersPerRoom)
        {
            isLoadingGame = true;
            UpdateStatus("Match found! Starting game...");

            if (waitingText != null) waitingText.SetActive(false);

            Debug.Log("[NetworkManager] Starting game in 1 second...");

            //  Wszyscy gracze ³aduj¹ scenê
            StartCoroutine(LoadGameSceneCoroutine());
        }
    }

    private System.Collections.IEnumerator LoadGameSceneCoroutine()
    {
        yield return new WaitForSeconds(1f);

        Debug.Log($"[NetworkManager] Loading scene: {gameSceneName} (Local load)");

        //  KLUCZOWA ZMIANA: U¿yj normalnego SceneManager
        SceneManager.LoadScene(gameSceneName);
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[NetworkManager] {message}");
    }

    private void UpdatePlayerCount()
    {
        if (playerCountText != null && PhotonNetwork.CurrentRoom != null)
        {
            int current = PhotonNetwork.CurrentRoom.PlayerCount;
            int max = PhotonNetwork.CurrentRoom.MaxPlayers;

            playerCountText.text = $"Players: {current}/{max}";

            if (current >= max)
            {
                playerCountText.color = Color.green;
            }
            else
            {
                playerCountText.color = Color.cyan;
            }
        }
    }

    #endregion
}