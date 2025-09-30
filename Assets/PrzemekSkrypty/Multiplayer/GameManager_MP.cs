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
        // Sprawd�, czy jeste�my po��czeni. Je�li nie (np. testujemy scen� offline), wyjd�.
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("Nie po��czono z Photonem! Uruchom gr� ze sceny MainMenu.");
            return;
        }

        SpawnPlayerAndArena();
    }

    private void SpawnPlayerAndArena()
    {
        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        if (actorNumber > spawnPoints.Length)
        {
            Debug.LogError("Za ma�o spawn point�w dla tego gracza!");
            return;
        }

        Transform spawnPoint = spawnPoints[actorNumber - 1];

        // Stw�rz aren� (lokalnie)
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

        // 1. Stw�rz gracza przez sie�, ale zapami�taj go w zmiennej
        GameObject playerObject = PhotonNetwork.Instantiate(playerPrefabName, spawnPoint.position, spawnPoint.rotation);

        // 2. Wykonaj to tylko dla LOKALNEGO gracza
        if (playerObject.GetComponent<PhotonView>().IsMine)
        {
            // Wy��cz Character Controller, aby m�c bezpiecznie ustawi� pozycj�
            CharacterController cc = playerObject.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            // 3. Ustaw pozycj� jeszcze raz, na wszelki wypadek
            playerObject.transform.position = spawnPoint.position;

            // 4. W��cz Character Controller z powrotem
            if (cc != null)
            {
                cc.enabled = true;
            }

            Debug.Log($"Poprawnie ustawiono pozycj� lokalnego gracza na {spawnPoint.position}");
        }
    }
}