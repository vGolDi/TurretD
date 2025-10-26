using UnityEngine;
using Photon.Pun;

public class SceneLoadedDebug : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("=== GAME SCENE LOADED ===");
        Debug.Log($"Is Connected: {PhotonNetwork.IsConnected}");
        Debug.Log($"Is in Room: {PhotonNetwork.InRoom}");
        Debug.Log($"Players in Room: {PhotonNetwork.CurrentRoom?.PlayerCount ?? 0}");
        Debug.Log($"Is Master: {PhotonNetwork.IsMasterClient}");
    }

    private void Start()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("NOT CONNECTED! Returning to main menu...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            return;
        }

        if (!PhotonNetwork.InRoom)
        {
            Debug.LogError("NOT IN ROOM! Returning to main menu...");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            return;
        }

        Debug.Log(" Game scene ready!");
    }
}