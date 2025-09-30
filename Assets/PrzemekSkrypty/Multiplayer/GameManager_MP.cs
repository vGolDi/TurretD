using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class GameManager_MP : MonoBehaviour
{
    [Header("Prefaby Sieciowe")]
    [SerializeField] private string playerPrefabName = "Player_MP"; // Nazwa prefaba gracza w folderze Resources
    [SerializeField] private string arenaPrefabName = "Arena_Prefab"; // Nazwa prefaba areny w folderze Resources

    [Header("Punkty Startowe")]
    [SerializeField] private Transform[] spawnPoints;

    private void Start()
    {
        // SprawdŸ, czy jesteœmy po³¹czeni. Jeœli nie (np. testujemy scenê offline), wyjdŸ.
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("Nie po³¹czono z Photonem! Uruchom grê ze sceny MainMenu.");
            return;
        }

        SpawnPlayerAndArena();
    }

    private void SpawnPlayerAndArena()
    {
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        if (actorNumber > spawnPoints.Length)
        {
            Debug.LogError("Za ma³o spawn pointów dla tego gracza!");
            return;
        }

        Transform spawnPoint = spawnPoints[actorNumber - 1];

        // Stwórz arenê (lokalnie)
        GameObject arenaPrefab = Resources.Load<GameObject>(arenaPrefabName);
        if (arenaPrefab != null)
        {
            Instantiate(arenaPrefab, spawnPoint.position, spawnPoint.rotation);
        }
        else
        {
            Debug.LogError($"Nie znaleziono prefaba areny: {arenaPrefabName}");
        }

        // --- POPRAWIONE SPAWNOWANIE GRACZA ---

        // 1. Stwórz gracza przez sieæ, ale zapamiêtaj go w zmiennej
        GameObject playerObject = PhotonNetwork.Instantiate(playerPrefabName, spawnPoint.position, spawnPoint.rotation);

        // 2. Wykonaj to tylko dla LOKALNEGO gracza
        if (playerObject.GetComponent<PhotonView>().IsMine)
        {
            // Wy³¹cz Character Controller, aby móc bezpiecznie ustawiæ pozycjê
            CharacterController cc = playerObject.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            // 3. Ustaw pozycjê jeszcze raz, na wszelki wypadek
            playerObject.transform.position = spawnPoint.position;

            // 4. W³¹cz Character Controller z powrotem
            if (cc != null)
            {
                cc.enabled = true;
            }

            Debug.Log($"Poprawnie ustawiono pozycjê lokalnego gracza na {spawnPoint.position}");
        }
    }
}