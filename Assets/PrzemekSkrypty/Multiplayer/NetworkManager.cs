using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

/// <summary>
/// Handles Photon connection, lobby, matchmaking
/// Entry point for multiplayer functionality
/// </summary>
public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject findMatchButton;
    [SerializeField] private GameObject cancelButton;

    [Header("Matchmaking Settings")]
    [SerializeField] private byte maxPlayersPerRoom = 2; // 1v1 by default
    [SerializeField] private string gameVersion = "0.1";

    private void Start()
    {
        // Enable automatic scene synchronization
        PhotonNetwork.AutomaticallySyncScene = true;

        // Set game version
        PhotonNetwork.GameVersion = gameVersion;

        // Connect to Photon Cloud
        UpdateStatus("Connecting to server...");
        PhotonNetwork.ConnectUsingSettings();

        findMatchButton?.SetActive(false);
        cancelButton?.SetActive(false);
    }

    #region Photon Callbacks

    public override void OnConnectedToMaster()
    {
        UpdateStatus("Connected! Ready to find match.");
        findMatchButton?.SetActive(true);
        PhotonNetwork.JoinLobby();

        Debug.Log("[NetworkManager] Connected to Photon Cloud");
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[NetworkManager] Joined lobby");
    }

    public override void OnJoinedRoom()
    {
        UpdateStatus($"Joined room! Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayersPerRoom}");

        findMatchButton?.SetActive(false);
        cancelButton?.SetActive(true);

        CheckPlayerCount();

        Debug.Log($"[NetworkManager] Joined room '{PhotonNetwork.CurrentRoom.Name}'");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        UpdateStatus($"Player joined! Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayersPerRoom}");
        CheckPlayerCount();

        Debug.Log($"[NetworkManager] Player {newPlayer.NickName} entered room");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        UpdateStatus($"Player left. Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayersPerRoom}");

        Debug.Log($"[NetworkManager] Player {otherPlayer.NickName} left room");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        UpdateStatus($"Disconnected: {cause}");
        findMatchButton?.SetActive(false);
        cancelButton?.SetActive(false);

        Debug.LogWarning($"[NetworkManager] Disconnected: {cause}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        UpdateStatus($"Failed to create room: {message}");
        findMatchButton?.SetActive(true);

        Debug.LogError($"[NetworkManager] Create room failed: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        UpdateStatus($"Failed to join room: {message}");
        findMatchButton?.SetActive(true);

        Debug.LogError($"[NetworkManager] Join room failed: {message}");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        UpdateStatus("No available rooms. Creating new one...");
        CreateRoom();
    }

    #endregion

    #region Public Methods (UI Buttons)

    /// <summary>
    /// Button callback - attempts to find/create a match
    /// </summary>
    public void FindMatch()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            UpdateStatus("Not connected to server!");
            return;
        }

        UpdateStatus("Searching for match...");
        findMatchButton?.SetActive(false);

        // Try to join random room
        PhotonNetwork.JoinRandomRoom();
    }

    /// <summary>
    /// Button callback - leaves current room
    /// </summary>
    public void CancelMatchmaking()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            UpdateStatus("Left room.");
        }

        findMatchButton?.SetActive(true);
        cancelButton?.SetActive(false);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates a new room
    /// </summary>
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

    /// <summary>
    /// Checks if room is full and starts game
    /// </summary>
    private void CheckPlayerCount()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount >= maxPlayersPerRoom && PhotonNetwork.IsMasterClient)
        {
            UpdateStatus("Match found! Starting game...");

            // Small delay before loading scene
            Invoke(nameof(LoadGameScene), 2f);
        }
    }

    /// <summary>
    /// Loads main game scene (only Master Client)
    /// </summary>
    private void LoadGameScene()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("SampleScene"); // Change to your game scene name
        }
    }

    /// <summary>
    /// Updates status text UI
    /// </summary>
    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"[NetworkManager] {message}");
    }

    #endregion
}