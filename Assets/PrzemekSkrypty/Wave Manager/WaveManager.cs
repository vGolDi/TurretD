using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ElementumDefense.Cards;
using ElementumDefense.UI;
using ElementumDefense.Elements;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

[System.Serializable]
public class WaveModifiers
{
    public float enemyHPMultiplier = 1f;
    public float enemySpeedMultiplier = 1f;
    public float enemyCountMultiplier = 1f;
    public float spawnRateMultiplier = 1f;
    public bool overrideElement = false;
    public ElementType newElement = ElementType.None;
    public List<GameObject> bonusEnemyPrefabs = new List<GameObject>();

    public void Reset()
    {
        enemyHPMultiplier = 1f;
        enemySpeedMultiplier = 1f;
        enemyCountMultiplier = 1f;
        spawnRateMultiplier = 1f;
        overrideElement = false;
        newElement = ElementType.None;
        bonusEnemyPrefabs.Clear();
    }
}

public class WaveManager : MonoBehaviourPunCallbacks
{
    [Header("Wave Configuration")]
    [SerializeField] private WaveData[] waves;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Paths")]
    [SerializeField] private Paths[] paths;

    [Header("UI")]
    [SerializeField]
    private float waveAnnounceDuration = 2f;

    [Header("Mayhem")]
    [SerializeField, Tooltip("Wave data for the Mayhem round (endless survival)")]
    private WaveData mayhemWave;

    [SerializeField, Tooltip("Gold bonus given to each player before Mayhem starts")]
    private int mayhemBonusGold = 200;

    // Runtime state
    private int currentWaveIndex = 0;
    private bool isSpawning = false;
    private int enemiesAlive = 0;
    private int enemiesSpawned = 0;
    private int totalEnemiesInCurrentWave = 0;
    private bool isMayhemActive = false;
    private bool normalWavesComplete = false;

    private WaveModifiers activeModifiers = new WaveModifiers();

    private DraftManager draftManager;
    private SabotageDraftManager sabotageDraftManager;
    private ArenaOwner arenaOwner;

    private const string WAVES_COMPLETE_KEY = "wavesComplete";

    private void Start()
    {
        Debug.Log(
            $"[WaveManager] Started on " +
            $"{gameObject.name}");

        arenaOwner =
            GetComponentInParent<ArenaOwner>();

        if (arenaOwner != null && arenaOwner.ownerPhotonView != null)
        {
            draftManager = arenaOwner.ownerPhotonView.GetComponent<DraftManager>();
            sabotageDraftManager = arenaOwner.ownerPhotonView.GetComponent<SabotageDraftManager>();
        }

        Debug.Log(
            $"[WaveManager] DraftManager: " +
            $"{(draftManager != null)}");
        Debug.Log(
            $"[WaveManager] SabotageDraftManager: " +
            $"{(sabotageDraftManager != null)}");

        if (waves == null || waves.Length == 0)
        {
            Debug.LogError(
                "[WaveManager] No waves assigned!");
            return;
        }

        if (spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            Debug.LogError(
                "[WaveManager] No spawn points!");
            return;
        }

        if (paths == null || paths.Length == 0)
        {
            Debug.LogError(
                "[WaveManager] No paths!");
            return;
        }

        // Initialize wave badge with total
        UpdateWaveBadge();
    }

    public void StartWaves()
    {
        ArenaOwner ao =
            GetComponentInParent<ArenaOwner>();
        if (ao != null &&
            ao.ownerPhotonView != null &&
            !ao.ownerPhotonView.IsMine)
        {
            Debug.Log(
                "[WaveManager] Not my arena!");
            return;
        }

        if (isSpawning)
        {
            Debug.LogWarning(
                "[WaveManager] Already running!");
            return;
        }

        if (draftManager == null && ao != null && ao.ownerPhotonView != null)
        {
            draftManager = ao.ownerPhotonView.GetComponent<DraftManager>();
        }

        StartCoroutine(RunGameWaves());
    }

    // ==========================================
    // MAIN WAVE LOOP
    // ==========================================

    private IEnumerator RunGameWaves()
    {
        isSpawning = true;

        for (int i = 0; i < waves.Length; i++)
        {
            currentWaveIndex = i;
            WaveData currentWave = waves[i];

            // Count total enemies in wave
            totalEnemiesInCurrentWave = 0;
            foreach (var part in
                currentWave.waveParts)
            {
                totalEnemiesInCurrentWave +=
                    part.enemyCount;
            }

            enemiesSpawned = 0;

            // ===== MID-GAME DRAFT =====
            if (currentWaveIndex > 0)
            {
                yield return HandleDrafts();
            }

            // Update wave badge
            UpdateWaveBadge();

            // Show wave announcement
            var hud = WaveHUD.Instance;
            if (hud != null)
            {
                yield return
                    hud.ShowWaveAnnouncement(
                        currentWaveIndex + 1,
                        waves.Length,
                        waveAnnounceDuration);
            }

            // Update spawn progress (0/total)
            UpdateSpawnProgress();

            // Spawn wave
            yield return StartCoroutine(
                SpawnWave(currentWave));

            // Wait for all enemies to die
            yield return new WaitUntil(
                () => enemiesAlive <= 0);

            // Hide spawn progress between waves
            hud?.HideSpawnProgress();

            // Notify card manager
            ArenaOwner ao =
                GetComponentInParent<ArenaOwner>();
            if (ao?.ownerPhotonView != null)
            {
                PlayerCardManager cardManager =
                    ao.ownerPhotonView
                        .GetComponent<
                            PlayerCardManager>();
                cardManager?.OnWaveCompleted();
            }

            // ===== WAVE COMPLETION GOLD =====
            PayWaveCompletionBonus(currentWave);

            // Reset modifiers for next wave
            activeModifiers.Reset();

            // Synchronize players before the next wave
            SetSingleWaveCompleteProperty(currentWaveIndex, true);

            if (currentWaveIndex < waves.Length - 1)
            {
                WaveHUD.Instance?.ShowWaitingMessage("WAITING FOR OTHER PLAYER...");
                yield return new WaitUntil(() => AllPlayersSingleWaveComplete(currentWaveIndex));
                WaveHUD.Instance?.HideWaitingMessage();
            }

            yield return new WaitForSeconds(
                currentWave.delayAfterWave);
        }

        // ===== NORMAL WAVES FINISHED =====
        normalWavesComplete = true;
        isSpawning = false;
        Debug.Log(
            "[WaveManager] All normal waves completed!");

        // Signal to other players that we finished
        SetWavesCompleteProperty(true);

        // Show completion banner while waiting
        WaveHUD.Instance?.ShowAllWavesComplete();

        // ===== CHECK FOR MAYHEM =====
        yield return StartCoroutine(
            CheckAndStartMayhem());
    }

    // ==========================================
    // DRAFT HANDLING
    // ==========================================

    private IEnumerator HandleDrafts()
    {
        if (draftManager == null && arenaOwner != null && arenaOwner.ownerPhotonView != null)
            draftManager = arenaOwner.ownerPhotonView.GetComponent<DraftManager>();

        if (draftManager != null)
        {
            Debug.Log(
                $"[WaveManager] Wave " +
                $"{currentWaveIndex}: " +
                $"Checking mid-game draft.");

            draftManager.CheckMidGameDraft(
                currentWaveIndex);

            float draftTimeout = 120f;
            while (draftManager.IsDrafting &&
                   draftTimeout > 0f)
            {
                draftTimeout -= Time.deltaTime;
                yield return null;
            }

            if (draftTimeout <= 0f)
            {
                Debug.LogError(
                    "[WaveManager] Draft timeout!");
            }
        }

        // Sabotage draft
        if (sabotageDraftManager == null && arenaOwner != null && arenaOwner.ownerPhotonView != null)
        {
            sabotageDraftManager = arenaOwner.ownerPhotonView.GetComponent<SabotageDraftManager>();
        }

        if (sabotageDraftManager != null)
        {
            sabotageDraftManager
                .CheckSabotageDraft(
                    currentWaveIndex);

            float sabotageTimeout = 120f;
            while (sabotageDraftManager.IsDrafting &&
                   sabotageTimeout > 0f)
            {
                sabotageTimeout -= Time.deltaTime;
                yield return null;
            }
        }
    }

    // ==========================================
    // SPAWNING
    // ==========================================

    private IEnumerator SpawnWave(WaveData wave)
    {
        enemiesAlive = 0;

        foreach (WavePart part in wave.waveParts)
        {
            if (part.pathIndex < 0 ||
                part.pathIndex >= paths.Length)
            {
                Debug.LogError(
                    $"[WaveManager] Invalid path " +
                    $"index {part.pathIndex}!");
                continue;
            }

            if (part.pathIndex >=
                spawnPoints.Length)
            {
                Debug.LogError(
                    $"[WaveManager] No spawn for " +
                    $"path {part.pathIndex}!");
                continue;
            }

            int finalEnemyCount = Mathf.RoundToInt(part.enemyCount * activeModifiers.enemyCountMultiplier);
            float finalSpawnInterval = part.spawnInterval * activeModifiers.spawnRateMultiplier;

            for (int j = 0;
                 j < finalEnemyCount; j++)
            {
                SpawnEnemy(
                    part.enemyPrefab,
                    spawnPoints[part.pathIndex],
                    paths[part.pathIndex]);

                yield return new WaitForSeconds(
                    finalSpawnInterval);
            }
        }

        // Spawn bonus enemies (bosses from sabotage)
        if (activeModifiers.bonusEnemyPrefabs.Count > 0)
        {
            Debug.Log($"[WaveManager] Spawning {activeModifiers.bonusEnemyPrefabs.Count} bonus enemies!");
            int defaultPathIndex = 0; // Default to first path for bonus enemies
            if (paths.Length > 0 && spawnPoints.Length > 0)
            {
                foreach (GameObject bonusPrefab in activeModifiers.bonusEnemyPrefabs)
                {
                    if (bonusPrefab != null)
                    {
                        SpawnEnemy(
                            bonusPrefab,
                            spawnPoints[defaultPathIndex],
                            paths[defaultPathIndex]);
                        
                        yield return new WaitForSeconds(1f); // small delay between boss spawns
                    }
                }
            }
        }
    }

    private void SpawnEnemy(
        GameObject enemyPrefab,
        Transform spawnPoint,
        Paths path)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError(
                "[WaveManager] Enemy prefab null!");
            return;
        }

        GameObject enemyObj = Instantiate(
            enemyPrefab,
            spawnPoint.position,
            Quaternion.identity);
        enemyObj.transform.SetParent(
            transform.root);

        EnemyMovement movement =
            enemyObj.GetComponent<EnemyMovement>();
        if (movement != null && path != null)
        {
            movement.SetPath(path);
            if (activeModifiers.enemySpeedMultiplier != 1f)
            {
                movement.SetBaseSpeed(movement.GetBaseSpeed() * activeModifiers.enemySpeedMultiplier);
            }
        }

        enemiesAlive++;
        enemiesSpawned++;
        UpdateSpawnProgress();

        EnemyHealth health =
            enemyObj.GetComponent<EnemyHealth>();
        if (health != null)
        {
            if (activeModifiers.enemyHPMultiplier != 1f)
            {
                health.SetMaxHP(Mathf.RoundToInt(health.GetMaxHP() * activeModifiers.enemyHPMultiplier));
            }
            if (activeModifiers.overrideElement)
            {
                health.SetElementType(activeModifiers.newElement);
            }

            StartCoroutine(
                TrackEnemyLifetime(enemyObj));
        }
    }

    private IEnumerator TrackEnemyLifetime(
        GameObject enemy)
    {
        while (enemy != null)
        {
            yield return null;
        }

        enemiesAlive--;
    }

    // ==========================================
    // WAVE COMPLETION GOLD
    // ==========================================

    private void PayWaveCompletionBonus(
        WaveData wave)
    {
        if (wave.waveCompletionBonus <= 0) return;

        ArenaOwner ao =
            GetComponentInParent<ArenaOwner>();
        if (ao?.ownerPhotonView == null) return;

        PlayerGold playerGold =
            ao.ownerPhotonView
                .GetComponent<PlayerGold>();

        if (playerGold != null)
        {
            playerGold.AddGold(
                wave.waveCompletionBonus);
            Debug.Log(
                $"[WaveManager] Wave completion " +
                $"bonus: +{wave.waveCompletionBonus} " +
                $"gold");
        }
    }

    // ==========================================
    // MAYHEM SYSTEM
    // ==========================================

    private IEnumerator CheckAndStartMayhem()
    {
        if (mayhemWave == null)
        {
            Debug.Log(
                "[WaveManager] No Mayhem wave " +
                "assigned. Game ends normally.");
            yield break;
        }

        // Check if local player is alive
        PlayerHealth localHealth =
            PlayerHealth.LocalInstance;
        if (localHealth == null || localHealth.IsDead)
        {
            Debug.Log(
                "[WaveManager] Local player is dead." +
                " No Mayhem.");
            yield break;
        }

        Debug.Log(
            "[WaveManager] Waiting for all players" +
            " to finish waves...");

        // Wait for all players to finish waves
        WaveHUD.Instance?.ShowWaitingMessage(
            "WAITING FOR OTHER PLAYERS...");

        yield return new WaitUntil(
            () => AllPlayersWavesComplete());

        WaveHUD.Instance?.HideWaitingMessage();

        // Check if both players are still alive
        if (!BothPlayersAlive())
        {
            Debug.Log(
                "[WaveManager] A player died " +
                "during waves. No Mayhem.");
            yield break;
        }

        Debug.Log(
            "[WaveManager] Both players alive! " +
            "Starting MAYHEM!");

        // Hide the "all waves complete" banner
        WaveHUD.Instance?.HideAllWavesComplete();

        // Give bonus gold
        PayMayhemBonusGold();

        // Draft before Mayhem
        yield return HandleDrafts();

        // Show Mayhem announcement
        var hud = WaveHUD.Instance;
        if (hud != null)
        {
            yield return hud.ShowMayhemAnnouncement(
                waveAnnounceDuration);
        }

        // Update badge to MAYHEM
        hud?.SetMayhemBadge();

        // Start Mayhem wave
        isMayhemActive = true;
        isSpawning = true;

        // Count enemies
        totalEnemiesInCurrentWave = 0;
        foreach (var part in mayhemWave.waveParts)
        {
            totalEnemiesInCurrentWave +=
                part.enemyCount;
        }
        enemiesSpawned = 0;
        UpdateSpawnProgress();

        // Spawn Mayhem wave (no waiting for
        // completion — game ends when someone dies)
        yield return StartCoroutine(
            SpawnWave(mayhemWave));

        // Wait for all enemies to die
        // (if somehow all are killed)
        yield return new WaitUntil(
            () => enemiesAlive <= 0);

        isSpawning = false;
        isMayhemActive = false;
        hud?.HideSpawnProgress();

        Debug.Log(
            "[WaveManager] Mayhem wave finished! " +
            "Both players survived all enemies.");
    }

    private void PayMayhemBonusGold()
    {
        if (mayhemBonusGold <= 0) return;

        ArenaOwner ao =
            GetComponentInParent<ArenaOwner>();
        if (ao?.ownerPhotonView == null) return;

        PlayerGold playerGold =
            ao.ownerPhotonView
                .GetComponent<PlayerGold>();

        if (playerGold != null)
        {
            playerGold.AddGold(mayhemBonusGold);
            Debug.Log(
                $"[WaveManager] Mayhem bonus: " +
                $"+{mayhemBonusGold} gold");
        }
    }

    // ==========================================
    // MULTIPLAYER SYNC
    // ==========================================

    private void SetSingleWaveCompleteProperty(int waveIndex, bool complete)
    {
        if (PhotonNetwork.LocalPlayer == null) return;

        var props = new ExitGames.Client.Photon.Hashtable();
        props[$"wave_{waveIndex}_complete"] = complete;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.Log($"[WaveManager] Set wave_{waveIndex}_complete={complete}");
    }

    private bool AllPlayersSingleWaveComplete(int waveIndex)
    {
        if (!PhotonNetwork.InRoom) return true;

        string key = $"wave_{waveIndex}_complete";
        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.TryGetValue(key, out object val))
            {
                return false;
            }
            if (!(bool)val) return false;
        }

        return true;
    }

    private void SetWavesCompleteProperty(
        bool complete)
    {
        if (PhotonNetwork.LocalPlayer == null)
            return;

        var props = new ExitGames.Client.Photon
            .Hashtable();
        props[WAVES_COMPLETE_KEY] = complete;
        PhotonNetwork.LocalPlayer
            .SetCustomProperties(props);

        Debug.Log(
            $"[WaveManager] Set wavesComplete=" +
            $"{complete}");
    }

    private bool AllPlayersWavesComplete()
    {
        if (!PhotonNetwork.InRoom) return true;

        foreach (var player in
            PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties
                .TryGetValue(
                    WAVES_COMPLETE_KEY,
                    out object val))
            {
                return false;
            }

            if (!(bool)val) return false;
        }

        return true;
    }

    private bool BothPlayersAlive()
    {
        // Check all PlayerHealth instances
        PlayerHealth[] allPlayers =
            FindObjectsByType<PlayerHealth>(
                FindObjectsSortMode.None);

        foreach (var ph in allPlayers)
        {
            if (ph.IsDead) return false;
        }

        return allPlayers.Length > 0;
    }

    // ==========================================
    // UI UPDATES
    // ==========================================

    private void UpdateWaveBadge()
    {
        var hud = WaveHUD.Instance;
        if (hud == null) return;

        hud.SetWave(
            currentWaveIndex + 1,
            waves != null ? waves.Length : 0);
    }

    private void UpdateSpawnProgress()
    {
        var hud = WaveHUD.Instance;
        if (hud == null) return;

        hud.SetSpawnProgress(
            enemiesSpawned,
            totalEnemiesInCurrentWave);
    }

    // ==========================================
    // PUBLIC API
    // ==========================================

    public WaveModifiers GetActiveModifiers() => activeModifiers;

    public void ApplyWaveModifiers(System.Action<WaveModifiers> modifierAction)
    {
        if (modifierAction != null)
        {
            modifierAction.Invoke(activeModifiers);
            Debug.Log($"[WaveManager] Applied wave modifiers. Current state: HP={activeModifiers.enemyHPMultiplier}x, Speed={activeModifiers.enemySpeedMultiplier}x, Count={activeModifiers.enemyCountMultiplier}x, ElementOverride={activeModifiers.overrideElement}");
        }
    }

    [ContextMenu("Clear All Enemies")]
    public void ClearAllEnemies()
    {
        GameObject[] enemies =
            GameObject.FindGameObjectsWithTag(
                "Enemy");
        foreach (GameObject enemy in enemies)
            Destroy(enemy);

        enemiesAlive = 0;
        enemiesSpawned = 0;
        UpdateSpawnProgress();
    }

    public int GetCurrentWaveIndex()
        => currentWaveIndex;

    public int GetTotalWaves()
        => waves != null ? waves.Length : 0;

    public bool IsSpawning => isSpawning;

    public bool IsMayhemActive => isMayhemActive;
}
