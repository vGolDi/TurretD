using UnityEngine;
using System.Collections;
using TMPro;
using ElementumDefense.Cards;

public class WaveManager : MonoBehaviour
{
    [Header("Wave Configuration")]
    [SerializeField] private WaveData[] waves;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Paths")]
    [SerializeField] private Paths[] paths;

    [Header("UI")]
    [SerializeField] private TMP_Text waveInfoText;
    [SerializeField] private float waveInfoDisplayTime = 2f;
    [SerializeField] private TMP_Text waveProgressText;

    // Runtime state
    private int currentWaveIndex = 0;
    private bool isSpawning = false;
    private int enemiesAlive = 0;
    private int totalEnemiesInCurrentWave = 0;

    private DraftManager draftManager;
    private SabotageDraftManager sabotageDraftManager;
    private ArenaOwner arenaOwner;

    private void Start()
    {
        Debug.Log($"[WaveManager] Started on {gameObject.name}");

        arenaOwner = GetComponentInParent<ArenaOwner>();

        // ========== NAPRAWIONE: Znajdź DraftManager przez Singleton ==========
        draftManager = DraftManager.Instance;

        if (draftManager != null)
        {
            Debug.Log($"[WaveManager] ✅ Found DraftManager via Singleton");
        }
        else
        {
            Debug.LogWarning("[WaveManager] ⚠️ DraftManager.Instance is null at Start. Will retry.");
        }

        // SabotageDraftManager - też przez singleton lub FindObjectOfType
        sabotageDraftManager = SabotageDraftManager.Instance; // jeśli ma singleton
                                                              // LUB:
                                                              // sabotageDraftManager = FindObjectOfType<SabotageDraftManager>();

        Debug.Log($"[WaveManager] Found DraftManager: {(draftManager != null ? "YES" : "NO")}");
        Debug.Log($"[WaveManager] Found SabotageDraftManager: {(sabotageDraftManager != null ? "YES" : "NO")}");
        // =====================================================================

        if (waves == null || waves.Length == 0)
        {
            Debug.LogError("[WaveManager] No waves assigned!");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[WaveManager] No spawn points assigned!");
            return;
        }

        if (paths == null || paths.Length == 0)
        {
            Debug.LogError("[WaveManager] No paths assigned!");
            return;
        }
    }

    public void StartWaves()
    {
        ArenaOwner arenaOwner = GetComponentInParent<ArenaOwner>();
        if (arenaOwner != null && arenaOwner.ownerPhotonView != null && !arenaOwner.ownerPhotonView.IsMine)
        {
            Debug.Log("[WaveManager] Not starting waves - not my arena!");
            return;
        }

        if (isSpawning)
        {
            Debug.LogWarning("[WaveManager] Waves already running!");
            return;
        }

        // ========== NOWE: Retry finding DraftManager before starting ==========
        if (draftManager == null)
        {
            draftManager = DraftManager.Instance;
            Debug.Log($"[WaveManager] Retry find DraftManager: {(draftManager != null ? "YES" : "NO")}");
        }
        // ======================================================================

        StartCoroutine(RunGameWaves());
    }

    private IEnumerator RunGameWaves()
    {
        isSpawning = true;

        for (int i = 0; i < waves.Length; i++)
        {
            currentWaveIndex = i;
            WaveData currentWave = waves[i];

            totalEnemiesInCurrentWave = 0;
            foreach (var part in currentWave.waveParts)
            {
                totalEnemiesInCurrentWave += part.enemyCount;
            }

            // ========== MID-GAME DRAFT CHECK ==========
            if (currentWaveIndex > 0)
            {
                // ========== NOWE: Lazy retry ==========
                if (draftManager == null)
                {
                    draftManager = DraftManager.Instance;
                }
                // ======================================

                if (draftManager != null)
                {
                    Debug.Log($"[WaveManager] Wave {currentWaveIndex}: Checking mid-game draft. " +
                              $"isDrafting={draftManager.IsDrafting}, " +
                              $"isStarterComplete={draftManager.IsStarterDraftComplete}");

                    draftManager.CheckMidGameDraft(currentWaveIndex);

                    // Wait for draft to finish (with safety timeout)
                    float draftTimeout = 120f;
                    while (draftManager.IsDrafting && draftTimeout > 0f)
                    {
                        draftTimeout -= Time.deltaTime;
                        yield return null;
                    }

                    if (draftTimeout <= 0f)
                    {
                        Debug.LogError("[WaveManager] Draft timeout! Forcing continue.");
                    }
                    else if (draftManager.IsDrafting == false)
                    {
                        Debug.Log("[WaveManager] Draft finished. Continuing waves.");
                    }
                }
                else
                {
                    Debug.LogError("[WaveManager] ❌ DraftManager STILL null! Cannot draft.");
                }

                // Sabotage draft
                if (sabotageDraftManager == null)
                {
                    sabotageDraftManager = FindObjectOfType<SabotageDraftManager>();
                }

                if (sabotageDraftManager != null)
                {
                    sabotageDraftManager.CheckSabotageDraft(currentWaveIndex);

                    float sabotageTimeout = 120f;
                    while (sabotageDraftManager.IsDrafting && sabotageTimeout > 0f)
                    {
                        sabotageTimeout -= Time.deltaTime;
                        yield return null;
                    }
                }
            }
            // ==========================================

            StartCoroutine(ShowWaveInfo($"Wave {currentWaveIndex + 1}/{waves.Length}"));

            yield return StartCoroutine(SpawnWave(currentWave));

            yield return new WaitUntil(() => enemiesAlive <= 0);

            ArenaOwner ao = GetComponentInParent<ArenaOwner>();
            if (ao?.ownerPhotonView != null)
            {
                PlayerCardManager cardManager = ao.ownerPhotonView.GetComponent<PlayerCardManager>();
                cardManager?.OnWaveCompleted();
            }

            yield return new WaitForSeconds(currentWave.delayAfterWave);
        }

        isSpawning = false;
        Debug.Log("[WaveManager] All waves completed!");
        StartCoroutine(ShowWaveInfo("ALL WAVES COMPLETED!"));
    }

    private IEnumerator SpawnWave(WaveData wave)
    {
        enemiesAlive = 0;
        UpdateWaveProgressUI();

        foreach (WavePart part in wave.waveParts)
        {
            if (part.pathIndex < 0 || part.pathIndex >= paths.Length)
            {
                Debug.LogError($"[WaveManager] Invalid path index {part.pathIndex}!");
                continue;
            }

            if (part.pathIndex >= spawnPoints.Length)
            {
                Debug.LogError($"[WaveManager] No spawn point for path {part.pathIndex}!");
                continue;
            }

            for (int j = 0; j < part.enemyCount; j++)
            {
                SpawnEnemy(part.enemyPrefab, spawnPoints[part.pathIndex], paths[part.pathIndex]);
                yield return new WaitForSeconds(part.spawnInterval);
            }
        }
    }

    private void SpawnEnemy(GameObject enemyPrefab, Transform spawnPoint, Paths path)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[WaveManager] Enemy prefab is null!");
            return;
        }

        GameObject enemyObj = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
        enemyObj.transform.SetParent(transform.root);

        EnemyMovement movement = enemyObj.GetComponent<EnemyMovement>();
        if (movement != null && path != null)
        {
            movement.SetPath(path);
        }

        enemiesAlive++;
        UpdateWaveProgressUI();

        EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
        if (health != null)
        {
            StartCoroutine(TrackEnemyLifetime(enemyObj));
        }
    }

    private IEnumerator TrackEnemyLifetime(GameObject enemy)
    {
        while (enemy != null)
        {
            yield return null;
        }

        enemiesAlive--;
        UpdateWaveProgressUI();
    }

    private IEnumerator ShowWaveInfo(string message)
    {
        if (waveInfoText != null)
        {
            waveInfoText.text = message;
            waveInfoText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(waveInfoDisplayTime);

        if (waveInfoText != null)
        {
            waveInfoText.gameObject.SetActive(false);
        }
    }

    private void UpdateWaveProgressUI()
    {
        if (waveProgressText != null)
        {
            if (currentWaveIndex < waves.Length)
            {
                waveProgressText.text = $"Wave {currentWaveIndex + 1}/{waves.Length}" +
                                        $"\nEnemies: {enemiesAlive}/{totalEnemiesInCurrentWave}";
            }
            else
            {
                waveProgressText.text = "All waves completed!";
            }
        }
    }

    [ContextMenu("Clear All Enemies")]
    public void ClearAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }
        enemiesAlive = 0;
        UpdateWaveProgressUI();
    }
}