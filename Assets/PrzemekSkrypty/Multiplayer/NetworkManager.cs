using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class NetworkManager : MonoBehaviourPunCallbacks // Wa�ne: dziedziczymy po MonoBehaviourPunCallbacks
{
    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject findMatchButton;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        statusText.text = "��czenie z serwerem...";
        PhotonNetwork.ConnectUsingSettings(); // ��czymy si� z serwerami Photon
        findMatchButton.SetActive(false);
    }

    // Ten callback jest wywo�ywany, gdy po��czymy si� z g��wnym serwerem Photon
    public override void OnConnectedToMaster()
    {
        statusText.text = "Po��czono! Kliknij, aby znale�� gr�.";
        findMatchButton.SetActive(true);
        PhotonNetwork.JoinLobby(); // Do��czamy do domy�lnego lobby
    }

    // Ta metoda b�dzie podpi�ta pod przycisk
    public void FindMatch()
    {
        statusText.text = "Szukanie pokoju...";
        findMatchButton.SetActive(false);
        PhotonNetwork.JoinRandomOrCreateRoom(); // Pr�bujemy do��czy� do losowego pokoju, lub tworzymy nowy
    }

    // Wywo�ywane, gdy do��czymy do pokoju
    public override void OnJoinedRoom()
    {
        statusText.text = $"Do��czono do pokoju! Graczy: {PhotonNetwork.CurrentRoom.PlayerCount}";
        CheckPlayerCount();
    }

    // Wywo�ywane, gdy inny gracz do��czy do naszego pokoju
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        statusText.text = $"Gracz do��czy�! Graczy: {PhotonNetwork.CurrentRoom.PlayerCount}";
        CheckPlayerCount();
    }

    private void CheckPlayerCount()
    {
        // Na razie gramy 1v1, wi�c potrzebujemy 2 graczy
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && PhotonNetwork.IsMasterClient)
        {
            statusText.text = "Znaleziono przeciwnika! Startowanie gry...";
            PhotonNetwork.LoadLevel("SampleScene"); // Zmie� "GameScene" na nazw� swojej sceny z gr�
        }
    }

    // Wywo�ywane, gdy po��czenie z Photonem si� zerwie
    public override void OnDisconnected(DisconnectCause cause)
    {
        statusText.text = $"Roz��czono: {cause}";
        findMatchButton.SetActive(false);
    }
}