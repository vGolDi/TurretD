using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using ElementumDefense.Players;
using ElementumDefense.Skins;
using ElementumDefense.Waves;



namespace ElementumDefense.Multiplayer
{
[System.Serializable]
public struct ArenaPrefabEntry
{
    public string arenaType; // np. "Fire", "Ice"
    public string prefabName; // np. "Arena_Fire_Prefab"
}
public class GameManager_MP : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private string playerPrefabName = "Player_MP";
   // [SerializeField] private string arenaPrefabName = "Arena_Prefab";

    [Header("Spawn Configuration")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Vector3 arenaOffset = Vector3.zero;

    [Header("Arena Prefabs")]
    [SerializeField] private ArenaPrefabEntry[] arenaPrefabs;
    private const string ARENA_TYPE_KEY = "arenaType";
    private string arenaPrefabNameToLoad;
    private string currentArenaType = "";

    private Dictionary<int, GameObject> playerArenas = new Dictionary<int, GameObject>();
    private Dictionary<int, GameObject> playerObjects = new Dictionary<int, GameObject>();

    // Reconnect: snapshot to restore once the local player object exists.
    private ElementumDefense.Multiplayer.Reconnect.PlayerMatchSnapshot pendingRestore;

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

        // Failsafe: if a reconnect paused the message queue for scene loading,
        // make sure it's running now that the game scene is active. Buffered
        // instantiates (opponent's Player_MP) will now process into THIS scene.
        if (!PhotonNetwork.IsMessageQueueRunning)
        {
            Debug.Log("[GameManager_MP] Resuming paused Photon message queue.");
            PhotonNetwork.IsMessageQueueRunning = true;
        }

        // ===== Reconnect: detect a saved snapshot for THIS match. =====
        // Set RestorePending BEFORE arenas spawn so the normal bootstrap
        // (PreGame → countdown → WaveManager.StartWaves) suppresses its wave-0
        // start and lets the restore resume from the saved wave instead.
        TryDetectReconnectSnapshot();
        // ==============================================================

        // Logika wyboru areny
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(ARENA_TYPE_KEY, out object arenaTypeObj))
        {
            string arenaType = (string)arenaTypeObj;
            currentArenaType = arenaType;
            arenaPrefabNameToLoad = GetPrefabNameForArenaType(arenaType);

            Debug.Log($"[GameManager_MP] Odczytano typ areny z pokoju: {arenaType}. Ładuję prefab: {arenaPrefabNameToLoad}");

            if (string.IsNullOrEmpty(arenaPrefabNameToLoad))
            {
                Debug.LogError($"Nie znaleziono prefaba dla areny typu: {arenaType}! Ładuję domyślny.");
                arenaPrefabNameToLoad = "Arena_Prefab"; // Fallback
            }
        }
        else
        {
            Debug.LogError("Nie znaleziono właściwości areny w pokoju! Ładuję domyślny.");
            arenaPrefabNameToLoad = "Arena_Prefab"; // Fallback
        }

        ValidateSetup();
        StartCoroutine(DelayedSpawn());
    }

    private System.Collections.IEnumerator DelayedSpawn()
    {
        yield return new WaitForSeconds(0.5f); // Wait half a second
        SpawnPlayerAndArena();
    }

    /// <summary>
    /// Reconnect: load + verify the local match snapshot. If valid, mark a
    /// restore as pending and stash it for <see cref="SpawnPlayer"/> to apply.
    /// If a snapshot exists but fails the server-witnessed hash check (tampering),
    /// forfeit immediately.
    /// </summary>
    private void TryDetectReconnectSnapshot()
    {
        var svc = ElementumDefense.Multiplayer.Reconnect.MatchSnapshotService.Instance;
        if (svc == null) return;

        if (!svc.TryLoad(out var snap))
            return; // no snapshot (fresh match) or undecryptable — normal flow

        // Room guard: a snapshot only applies to the room it was saved in. A
        // leftover snapshot from a previous match (forfeit/abandon) must NOT
        // restore into a brand-new match. Clear it and continue normally.
        string currentRoom = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "";
        if (string.IsNullOrEmpty(snap.roomName) || snap.roomName != currentRoom)
        {
            Debug.Log($"[GameManager_MP] Snapshot room '{snap.roomName}' != current '{currentRoom}' — " +
                      "stale snapshot, clearing and starting fresh.");
            svc.Clear();
            return;
        }

        if (!svc.VerifyServerHash(snap))
        {
            Debug.LogWarning("[GameManager_MP] Snapshot failed integrity check — forfeiting (anti-tamper).");
            ForfeitForTampering();
            return;
        }

        pendingRestore = snap;
        ElementumDefense.Multiplayer.Reconnect.MatchRestoreService.RestorePending = true;
        Debug.Log($"[GameManager_MP] Reconnect snapshot accepted (wave={snap.currentWaveIndex}).");
    }

    private void ForfeitForTampering()
    {
        ElementumDefense.Multiplayer.MatchOpponentWatcher.RaiseForfeit();
        ElementumDefense.Multiplayer.Reconnect.MatchSnapshotService.Instance?.Clear();
        ElementumDefense.Multiplayer.PendingMatchState.Clear();

        var gem = FindAnyObjectByType<GameEndManager>();
        gem?.ShowDefeat();
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

        GameObject arenaPrefab = Resources.Load<GameObject>(arenaPrefabNameToLoad);
        Debug.Log($"[GameManager_MP] Arena prefab '{arenaPrefabNameToLoad}': {(arenaPrefab != null ? "FOUND" : "NOT FOUND")}");
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

        GameObject arenaPrefab = Resources.Load<GameObject>(arenaPrefabNameToLoad);
        if (arenaPrefab == null)
        {
            Debug.LogError($"[GameManager_MP] Arena prefab '{arenaPrefabNameToLoad}' not found in Resources folder!");
            return;
        }

        Vector3 arenaPosition = spawnPoint.position + arenaOffset;
        Debug.Log($"[GameManager_MP] Arena position: {arenaPosition}");

        GameObject arena = Instantiate(arenaPrefab, arenaPosition, spawnPoint.rotation);
        arena.name = $"Arena_Player{actorNumber}";

        // Set arena type for skin compatibility
        ArenaSkinApplier skinApplier = arena.GetComponent<ArenaSkinApplier>();
        if (skinApplier != null)
        {
            skinApplier.arenaType = currentArenaType;
        }

        // ... (logi walidacyjne, np. WaveManager, Paths) ...

        // ========== POPRAWIONE POWIĄZANIE z PreGameManager ==========
        // Znajdź komponenty w nowo stworzonej arenie
        GameStartCountdown foundCountdown = arena.GetComponentInChildren<GameStartCountdown>();

        // PreGameManager jest jeden na całą scenę
        PreGameManager preGameManager = FindObjectOfType<PreGameManager>();

        if (preGameManager != null && foundCountdown != null)
        {
            Debug.Log("[GameManager_MP] Uruchamianie fazy PreGame...");
            preGameManager.StartPreGamePhase(foundCountdown); // To jest OK
        }
        else
        {
            Debug.LogError("Nie znaleziono PreGameManager lub GameStartCountdown! Gra nie wystartuje poprawnie. Uruchamiam countdown awaryjnie.");
            foundCountdown?.StartCountdown(); // Awaryjnie wywołujemy nową metodę
        }
        // ==========================================================

        playerArenas[actorNumber] = arena;
        Debug.Log($"[GameManager_MP]  Arena spawned for Player {actorNumber}");
        //Debug.Log($"========== SPAWNING ARENA FOR PLAYER {actorNumber} ==========");

        //GameObject arenaPrefab = Resources.Load<GameObject>(arenaPrefabName);
        //if (arenaPrefab == null)
        //{
        //    Debug.LogError("[GameManager_MP] Arena prefab not found!");
        //    return;
        //}

        //Vector3 arenaPosition = spawnPoint.position + arenaOffset;
        //Debug.Log($"[GameManager_MP] Arena position: {arenaPosition}");

        //GameObject arena = Instantiate(arenaPrefab, arenaPosition, spawnPoint.rotation);
        //arena.name = $"Arena_Player{actorNumber}";

        //// Znajdź komponenty
        //WaveManager waveManager = arena.GetComponentInChildren<WaveManager>();

        //// ========== NOWE: Zarejestruj WaveManager w GameController ==========
        //GameStartCountdown countdown = FindFirstObjectByType<GameStartCountdown>();
        //if (countdown != null && waveManager != null)
        //{
        //    countdown.RegisterWaveManager(waveManager);
        //    Debug.Log($"[GameManager_MP] Registered WaveManager with GameController");
        //}
        //else
        //{
        //    Debug.LogError($"[GameManager_MP] Countdown: {countdown != null}, WaveManager: {waveManager != null}");
        //}
        //// ====================================================================

        //playerArenas[actorNumber] = arena;

        //Debug.Log($"[GameManager_MP] ✅ Arena spawned for Player {actorNumber}");
        ////Debug.Log($"========== SPAWNING ARENA FOR PLAYER {actorNumber} ==========");

        ////GameObject arenaPrefab = Resources.Load<GameObject>(arenaPrefabName);
        ////if (arenaPrefab == null)
        ////{
        ////    Debug.LogError("[GameManager_MP] Arena prefab not found!");
        ////    return;
        ////}

        ////Vector3 arenaPosition = spawnPoint.position + arenaOffset;
        ////Debug.Log($"[GameManager_MP] Arena position: {arenaPosition}");

        ////GameObject arena = Instantiate(arenaPrefab, arenaPosition, spawnPoint.rotation);
        ////arena.name = $"Arena_Player{actorNumber}";

        ////// USUŃ TO STĄD! 
        ////// ArenaOwner arenaOwner = arena.GetComponent<ArenaOwner>();
        ////// if (arenaOwner != null)
        ////// {
        //////     arenaOwner.SetOwner(photonView);
        ////// }

        ////// Validation logs
        ////WaveManager waveManager = arena.GetComponentInChildren<WaveManager>();
        ////GameStartCountdown countdown = arena.GetComponentInChildren<GameStartCountdown>();
        ////Paths[] paths = arena.GetComponentsInChildren<Paths>();

        ////Debug.Log($"[GameManager_MP] Arena components:");
        ////Debug.Log($"  - WaveManager: {(waveManager != null ? "FOUND" : "MISSING")}");
        ////Debug.Log($"  - GameStartCountdown: {(countdown != null ? "FOUND" : "MISSING")}");
        ////Debug.Log($"  - Paths count: {paths.Length}");

        ////playerArenas[actorNumber] = arena;

        ////Debug.Log($"[GameManager_MP]  Arena spawned for Player {actorNumber}");
    }
    private string GetPrefabNameForArenaType(string arenaType)
    {
        foreach (var entry in arenaPrefabs)
        {
            if (entry.arenaType.Equals(arenaType, System.StringComparison.OrdinalIgnoreCase))
            {
                return entry.prefabName;
            }
        }
        return null;
    }
    private void SpawnPlayer(Transform spawnPoint, int actorNumber)
    {
        Debug.Log($"========== SPAWNING PLAYER {actorNumber} ==========");
        Debug.Log($"[GameManager_MP] Player spawn position: {spawnPoint.position}");
        Debug.Log($"[GameManager_MP] PhotonNetwork.IsConnected: {PhotonNetwork.IsConnected}");
        Debug.Log($"[GameManager_MP] PhotonNetwork.InRoom: {PhotonNetwork.InRoom}");

        // ========== CHECK FOR EXISTING PLAYER ON RECONNECT ==========
        // Identify the player object by its PlayerHealth component + ownership,
        // NOT by name. (The prefab is "PlayerArmature", not "Player_MP" — the old
        // name check never matched, so reconnect always spawned a DUPLICATE.)
        GameObject playerObject = null;
        var duplicates = new System.Collections.Generic.List<GameObject>();
        PhotonView[] allViews = Object.FindObjectsByType<PhotonView>(FindObjectsSortMode.None);
        foreach (var pvExisting in allViews)
        {
            if (pvExisting.IsMine && pvExisting.GetComponentInChildren<PlayerHealth>() != null)
            {
                if (playerObject == null)
                {
                    playerObject = pvExisting.gameObject;
                    Debug.Log($"[GameManager_MP] Found existing player '{pvExisting.gameObject.name}' " +
                              $"(ViewID {pvExisting.ViewID}) — reusing, skipping instantiation.");
                }
                else
                {
                    // A second owned player object = leftover duplicate (buffered
                    // old instance + a spurious new one). Clean it up.
                    duplicates.Add(pvExisting.gameObject);
                }
            }
        }

        foreach (var dup in duplicates)
        {
            Debug.LogWarning($"[GameManager_MP] Destroying duplicate player '{dup.name}' " +
                             $"(ViewID {dup.GetComponent<PhotonView>()?.ViewID}).");
            PhotonNetwork.Destroy(dup);
        }

        if (playerObject == null)
        {
            // ========== DODAJ: Sprawdź czy prefab istnieje ==========
            GameObject prefabCheck = Resources.Load<GameObject>(playerPrefabName);
            Debug.Log($"[GameManager_MP] Prefab '{playerPrefabName}' exists in Resources: {prefabCheck != null}");
            if (prefabCheck != null)
            {
                PlayerHealth healthCheck = prefabCheck.GetComponentInChildren<PlayerHealth>();
                Debug.Log($"[GameManager_MP] Prefab has PlayerHealth: {healthCheck != null}");
            }
            // =========================================================

            playerObject = PhotonNetwork.Instantiate(
                playerPrefabName,
                spawnPoint.position,
                spawnPoint.rotation
            );

            Debug.Log($"[GameManager_MP] Player instantiated: {playerObject.name}");
        }
        
        Debug.Log($"[GameManager_MP] Player has {playerObject.transform.childCount} children");

        // ========== DODAJ: Sprawdź PlayerHealth na zaspawnowanym obiekcie ==========
        PlayerHealth health = playerObject.GetComponentInChildren<PlayerHealth>();
        Debug.Log($"[GameManager_MP] Spawned player has PlayerHealth: {health != null}");
        if (health != null)
        {
            Debug.Log($"[GameManager_MP] PlayerHealth.enabled: {health.enabled}");
            Debug.Log($"[GameManager_MP] PlayerHealth.gameObject.activeInHierarchy: {health.gameObject.activeInHierarchy}");
        }
        // ===========================================================================

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

                // ===== Reconnect: apply the restore once arena is linked. =====
                if (pendingRestore != null)
                {
                    Debug.Log("[GameManager_MP] Starting match-state restore...");
                    StartCoroutine(
                        ElementumDefense.Multiplayer.Reconnect.MatchRestoreService.Restore(pendingRestore));
                    pendingRestore = null;
                }
                // ==============================================================
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
}
