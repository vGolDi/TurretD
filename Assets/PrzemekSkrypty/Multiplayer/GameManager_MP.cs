using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

public class GameManager_MP : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private string playerPrefabName = "Player_MP";
    [SerializeField] private string arenaPrefabName = "Arena_Prefab";

    [Header("Spawn Configuration")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Vector3 arenaOffset = Vector3.zero;

    private Dictionary<int, GameObject> playerArenas = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> playerObjects = new Dictionary<int, GameObject>();

    private PhotonView photonView;
    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
        Debug.Log($"[GameManager_MP] AWAKE called! IsConnected: {PhotonNetwork.IsConnected}, ActorNumber: {(PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1)}");
    }

    private void Start()
    {
        Debug.Log("========== GAME MANAGER START ==========");
        Debug.Log($"[GameManager_MP] PhotonNetwork.IsConnected: {PhotonNetwork.IsConnected}");
        Debug.Log($"[GameManager_MP] PhotonNetwork.InRoom: {PhotonNetwork.InRoom}");

        if (PhotonNetwork.CurrentRoom != null)
        {
            Debug.Log($"[GameManager_MP] Room: {PhotonNetwork.CurrentRoom.Name}, Players: {PhotonNetwork.CurrentRoom.PlayerCount}");
        }

        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogError("[GameManager_MP] Not connected to Photon!");
            return;
        }

        ValidateSetup();

        // CRITICAL: Wait one frame before spawning to ensure Photon is ready
        StartCoroutine(DelayedSpawn());
    }

    private System.Collections.IEnumerator DelayedSpawn()
    {
        yield return new WaitForSeconds(0.5f); // Wait half a second
        SpawnPlayerAndArena();
    }

    private void ValidateSetup()
    {
        Debug.Log("========== VALIDATING SETUP ==========");

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[GameManager_MP] No spawn points assigned!");
            return;
        }

        Debug.Log($"[GameManager_MP] Spawn points count: {spawnPoints.Length}");
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                Debug.Log($"  SpawnPoint[{i}]: {spawnPoints[i].position}");
            }
            else
            {
                Debug.LogError($"  SpawnPoint[{i}]: NULL!");
            }
        }

        int playerCount = PhotonNetwork.CurrentRoom?.PlayerCount ?? 0;
        if (playerCount > spawnPoints.Length)
        {
            Debug.LogError($"[GameManager_MP] Not enough spawn points! Need {playerCount}, have {spawnPoints.Length}");
        }

        GameObject playerPrefab = Resources.Load<GameObject>(playerPrefabName);
        Debug.Log($"[GameManager_MP] Player prefab '{playerPrefabName}': {(playerPrefab != null ? "FOUND" : "NOT FOUND")}");

        GameObject arenaPrefab = Resources.Load<GameObject>(arenaPrefabName);
        Debug.Log($"[GameManager_MP] Arena prefab '{arenaPrefabName}': {(arenaPrefab != null ? "FOUND" : "NOT FOUND")}");
    }

    private void SpawnPlayerAndArena()
    {
        Debug.Log("========== SPAWNING PLAYER AND ARENA ==========");

        int actorNumber = PhotonNetwork.LocalPlayer.ActorNumber;
        Debug.Log($"[GameManager_MP] Local player ActorNumber: {actorNumber}");

        if (actorNumber > spawnPoints.Length)
        {
            Debug.LogError($"[GameManager_MP] Actor number {actorNumber} exceeds spawn points!");
            return;
        }

        Transform spawnPoint = spawnPoints[actorNumber - 1];
        Debug.Log($"[GameManager_MP] Using spawn point: {spawnPoint.position}");

        // 1. FIRST: Spawn Arena
        SpawnArena(spawnPoint, actorNumber);

        // 2. THEN: Spawn Player (after small delay)
        StartCoroutine(DelayedPlayerSpawn(spawnPoint, actorNumber));
    }

    private System.Collections.IEnumerator DelayedPlayerSpawn(Transform spawnPoint, int actorNumber)
    {
        yield return new WaitForSeconds(0.2f); // Wait for arena to fully instantiate
        SpawnPlayer(spawnPoint, actorNumber);
    }

    private void SpawnArena(Transform spawnPoint, int actorNumber)
    {
        Debug.Log($"========== SPAWNING ARENA FOR PLAYER {actorNumber} ==========");

        GameObject arenaPrefab = Resources.Load<GameObject>(arenaPrefabName);
        if (arenaPrefab == null)
        {
            Debug.LogError("[GameManager_MP] Arena prefab not found!");
            return;
        }

        Vector3 arenaPosition = spawnPoint.position + arenaOffset;
        Debug.Log($"[GameManager_MP] Arena position: {arenaPosition}");

        GameObject arena = Instantiate(arenaPrefab, arenaPosition, spawnPoint.rotation);
        arena.name = $"Arena_Player{actorNumber}";

        // USUÑ TO ST¥D! 
        // ArenaOwner arenaOwner = arena.GetComponent<ArenaOwner>();
        // if (arenaOwner != null)
        // {
        //     arenaOwner.SetOwner(photonView);
        // }

        // Validation logs
        WaveManager waveManager = arena.GetComponentInChildren<WaveManager>();
        GameStartCountdown countdown = arena.GetComponentInChildren<GameStartCountdown>();
        Paths[] paths = arena.GetComponentsInChildren<Paths>();

        Debug.Log($"[GameManager_MP] Arena components:");
        Debug.Log($"  - WaveManager: {(waveManager != null ? "FOUND" : "MISSING")}");
        Debug.Log($"  - GameStartCountdown: {(countdown != null ? "FOUND" : "MISSING")}");
        Debug.Log($"  - Paths count: {paths.Length}");

        playerArenas[actorNumber] = arena;

        Debug.Log($"[GameManager_MP]  Arena spawned for Player {actorNumber}");
    }

    private void SpawnPlayer(Transform spawnPoint, int actorNumber)
    {
        Debug.Log($"========== SPAWNING PLAYER {actorNumber} ==========");
        Debug.Log($"[GameManager_MP] Player spawn position: {spawnPoint.position}");

        GameObject playerObject = PhotonNetwork.Instantiate(
            playerPrefabName,
            spawnPoint.position,
            spawnPoint.rotation
        );

        Debug.Log($"[GameManager_MP] Player instantiated: {playerObject.name}");

        PhotonView pv = playerObject.GetComponent<PhotonView>();
        if (pv != null)
        {
            Debug.Log($"[GameManager_MP] PhotonView - IsMine: {pv.IsMine}, ViewID: {pv.ViewID}");

            if (pv.IsMine)
            {
                Debug.Log("[GameManager_MP] This is MY player - configuring...");
                ConfigureLocalPlayer(playerObject, spawnPoint);

                //  DODAJ TO TUTAJ - PO SPAWNIE GRACZA!
                // Link player's arena to this player
                if (playerArenas.ContainsKey(actorNumber))
                {
                    GameObject arena = playerArenas[actorNumber];
                    ArenaOwner arenaOwner = arena.GetComponent<ArenaOwner>();

                    if (arenaOwner != null)
                    {
                        arenaOwner.SetOwner(pv); // Teraz pv to PhotonView GRACZA!
                        Debug.Log($"[GameManager_MP] Linked arena to player {pv.Owner.NickName}");
                    }
                    else
                    {
                        Debug.LogError("[GameManager_MP] Arena has no ArenaOwner component!");
                    }
                }
            }
            else
            {
                Debug.Log("[GameManager_MP] This is REMOTE player - ignoring");
            }
        }

        playerObjects[actorNumber] = playerObject;

        Debug.Log($"[GameManager_MP]  Player {actorNumber} spawned");
    }

    private void ConfigureLocalPlayer(GameObject player, Transform spawnPoint)
    {
        Debug.Log($"[GameManager_MP] Configuring local player at {spawnPoint.position}");

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            Debug.Log("[GameManager_MP] Disabling CharacterController...");
            cc.enabled = false;
        }

        Debug.Log($"[GameManager_MP] Setting position to: {spawnPoint.position}");
        player.transform.position = spawnPoint.position;
        player.transform.rotation = spawnPoint.rotation;

        if (cc != null)
        {
            Debug.Log("[GameManager_MP] Re-enabling CharacterController...");
            cc.enabled = true;
        }

        string nickname = PhotonNetwork.LocalPlayer.NickName;
        if (string.IsNullOrEmpty(nickname))
        {
            nickname = $"Player_{PhotonNetwork.LocalPlayer.ActorNumber}";
            PhotonNetwork.LocalPlayer.NickName = nickname;
        }

        Debug.Log($"[GameManager_MP]  Local player configured: {nickname}");

        // NEW DEBUG:
        Camera[] cameras = player.GetComponentsInChildren<Camera>(true);
        Debug.Log($"[GameManager_MP] Cameras in player: {cameras.Length}");
        foreach (Camera cam in cameras)
        {
            Debug.Log($"  - Camera: {cam.name}, Enabled: {cam.enabled}, Tag: {cam.tag}");
        }

        AudioListener[] listeners = player.GetComponentsInChildren<AudioListener>(true);
        Debug.Log($"[GameManager_MP] AudioListeners in player: {listeners.Length}");
    }

    public void CleanupPlayer(int actorNumber)
    {
        Debug.Log($"[GameManager_MP] Cleaning up Player {actorNumber}");

        if (playerArenas.ContainsKey(actorNumber))
        {
            Destroy(playerArenas[actorNumber]);
            playerArenas.Remove(actorNumber);
        }

        playerObjects.Remove(actorNumber);
    }

    private void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                // Draw spawn point
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(spawnPoints[i].position, 1f);

                // Draw arena position
                Gizmos.color = Color.yellow;
                Vector3 arenaPos = spawnPoints[i].position + arenaOffset;
                Gizmos.DrawWireCube(arenaPos, Vector3.one * 5f);

#if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    spawnPoints[i].position + Vector3.up * 2f,
                    $"Spawn {i + 1}\nPlayer: {spawnPoints[i].position}\nArena: {arenaPos}"
                );
#endif
            }
        }
    }
}