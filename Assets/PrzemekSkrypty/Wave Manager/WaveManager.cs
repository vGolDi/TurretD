using UnityEngine;
using System.Collections;
using ElementumDefense.Cards;
using ElementumDefense.UI;

public class WaveManager : MonoBehaviour
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

    // Runtime state
    private int currentWaveIndex = 0;
    private bool isSpawning = false;
    private int enemiesAlive = 0;
    private int enemiesSpawned = 0;
    private int totalEnemiesInCurrentWave = 0;

    private DraftManager draftManager;
    private SabotageDraftManager sabotageDraftManager;
    private ArenaOwner arenaOwner;

    private void Start()
    {
        Debug.Log(
            $"[WaveManager] Started on " +
            $"{gameObject.name}");

        arenaOwner =
            GetComponentInParent<ArenaOwner>();

        draftManager = DraftManager.Instance;
        sabotageDraftManager =
            SabotageDraftManager.Instance;

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

        if (draftManager == null)
        {
            draftManager = DraftManager.Instance;
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

            yield return new WaitForSeconds(
                currentWave.delayAfterWave);
        }

        isSpawning = false;
        Debug.Log(
            "[WaveManager] All waves completed!");

        // Show completion banner
        WaveHUD.Instance?.ShowAllWavesComplete();
    }

    // ==========================================
    // DRAFT HANDLING
    // ==========================================

    private IEnumerator HandleDrafts()
    {
        if (draftManager == null)
            draftManager = DraftManager.Instance;

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
        if (sabotageDraftManager == null)
        {
            sabotageDraftManager =
                FindFirstObjectByType<
                    SabotageDraftManager>();
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

            for (int j = 0;
                 j < part.enemyCount; j++)
            {
                SpawnEnemy(
                    part.enemyPrefab,
                    spawnPoints[part.pathIndex],
                    paths[part.pathIndex]);

                yield return new WaitForSeconds(
                    part.spawnInterval);
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
            movement.SetPath(path);

        enemiesAlive++;
        enemiesSpawned++;
        UpdateSpawnProgress();

        EnemyHealth health =
            enemyObj.GetComponent<EnemyHealth>();
        if (health != null)
        {
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
}
