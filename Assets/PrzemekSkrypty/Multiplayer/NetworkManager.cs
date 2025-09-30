using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class NetworkManager : MonoBehaviourPunCallbacks // Wa¿ne: dziedziczymy po MonoBehaviourPunCallbacks
{
    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject findMatchButton;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        statusText.text = "£¹czenie z serwerem...";
        PhotonNetwork.ConnectUsingSettings(); // £¹czymy siê z serwerami Photon
        findMatchButton.SetActive(false);
    }

    // Ten callback jest wywo³ywany, gdy po³¹czymy siê z g³ównym serwerem Photon
    public override void OnConnectedToMaster()
    {
        statusText.text = "Po³¹czono! Kliknij, aby znaleŸæ grê.";
        findMatchButton.SetActive(true);
        PhotonNetwork.JoinLobby(); // Do³¹czamy do domyœlnego lobby
    }

    // Ta metoda bêdzie podpiêta pod przycisk
    public void FindMatch()
    {
        statusText.text = "Szukanie pokoju...";
        findMatchButton.SetActive(false);
        PhotonNetwork.JoinRandomOrCreateRoom(); // Próbujemy do³¹czyæ do losowego pokoju, lub tworzymy nowy
    }

    // Wywo³ywane, gdy do³¹czymy do pokoju
    public override void OnJoinedRoom()
    {
        statusText.text = $"Do³¹czono do pokoju! Graczy: {PhotonNetwork.CurrentRoom.PlayerCount}";
        CheckPlayerCount();
    }

    // Wywo³ywane, gdy inny gracz do³¹czy do naszego pokoju
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        statusText.text = $"Gracz do³¹czy³! Graczy: {PhotonNetwork.CurrentRoom.PlayerCount}";
        CheckPlayerCount();
    }

    private void CheckPlayerCount()
    {
        // Na razie gramy 1v1, wiêc potrzebujemy 2 graczy
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
        {
            statusText.text = "Znaleziono przeciwnika! Startowanie gry...";
            PhotonNetwork.LoadLevel("SampleScene"); // Zmieñ "GameScene" na nazwê swojej sceny z gr¹
        }
    }

    // Wywo³ywane, gdy po³¹czenie z Photonem siê zerwie
    public override void OnDisconnected(DisconnectCause cause)
    {
        statusText.text = $"Roz³¹czono: {cause}";
        findMatchButton.SetActive(false);
    }
}