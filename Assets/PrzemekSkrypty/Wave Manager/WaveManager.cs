using System.IO;
using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// Manages wave spawning and UI display
/// Only runs waves for local player's arena
/// </summary>
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
    [SerializeField] private TMP_Text waveProgressText; // Nowy tekst dla progressu fali

    // Runtime state
    private int currentWaveIndex = 0;
    private bool isSpawning = false;
    private int enemiesAlive = 0;
    private int totalEnemiesInCurrentWave = 0;

    private void Start()
    {
        Debug.Log($"[WaveManager] Started on {gameObject.name}");

        // Validate setup
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

    /// <summary>
    /// Starts wave spawning sequence (only for local player's arena)
    /// </summary>
    public void StartWaves()
    {
        // CRITICAL FIX: Only start waves for LOCAL player's arena!
        ArenaOwner arenaOwner = GetComponentInParent<ArenaOwner>();
        if (arenaOwner != null && arenaOwner.ownerPhotonView != null && !arenaOwner.ownerPhotonView.IsMine)
        {
            Debug.Log("[WaveManager] Not starting waves - this is not my arena!");
            return;
        }

        if (isSpawning)
        {
            Debug.LogWarning("[WaveManager] Waves already running!");
            return;
        }

        StartCoroutine(RunGameWaves());
    }

    /// <summary>
    /// Main wave loop
    /// </summary>
    private IEnumerator RunGameWaves()
    {
        isSpawning = true;

        for (int i = 0; i < waves.Length; i++)
        {
            currentWaveIndex = i;
            WaveData currentWave = waves[i];

            // Calculate total enemies in this wave
            totalEnemiesInCurrentWave = 0;
            foreach (var part in currentWave.waveParts)
            {
                totalEnemiesInCurrentWave += part.enemyCount;
            }

            // Show wave start notification
            StartCoroutine(ShowWaveInfo($"Wave {currentWaveIndex + 1}/{waves.Length}"));

            // Spawn wave
            yield return StartCoroutine(SpawnWave(currentWave));

            // Wait for all enemies to be killed
            yield return new WaitUntil(() => enemiesAlive <= 0);

            // Delay before next wave
            yield return new WaitForSeconds(currentWave.delayAfterWave);
        }

        isSpawning = false;
        Debug.Log("[WaveManager] All waves completed!");

        // Optional: Show victory message
        StartCoroutine(ShowWaveInfo("ALL WAVES COMPLETED!"));
    }

    /// <summary>
    /// Spawns all parts of a single wave
    /// </summary>
    private IEnumerator SpawnWave(WaveData wave)
    {
        enemiesAlive = 0; // Reset counter for this wave
        UpdateWaveProgressUI();

        foreach (WavePart part in wave.waveParts)
        {
            // Validate path index
            if (part.pathIndex < 0 || part.pathIndex >= paths.Length)
            {
                Debug.LogError($"[WaveManager] Invalid path index {part.pathIndex} in wave!");
                continue;
            }

            // Validate spawn point
            if (part.pathIndex >= spawnPoints.Length)
            {
                Debug.LogError($"[WaveManager] No spawn point for path {part.pathIndex}!");
                continue;
            }

            // Spawn enemies
            for (int j = 0; j < part.enemyCount; j++)
            {
                SpawnEnemy(part.enemyPrefab, spawnPoints[part.pathIndex], paths[part.pathIndex]);
                yield return new WaitForSeconds(part.spawnInterval);
            }
        }
    }

    /// <summary>
    /// Spawns a single enemy
    /// </summary>
    private void SpawnEnemy(GameObject enemyPrefab, Transform spawnPoint, Paths path)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("[WaveManager] Enemy prefab is null!");
            return;
        }

        GameObject enemyObj = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        // Set parent to arena (for proper cleanup)
        enemyObj.transform.SetParent(transform.root);

        // Set path
        EnemyMovement movement = enemyObj.GetComponent<EnemyMovement>();
        if (movement != null && path != null)
        {
            movement.SetPath(path);
        }

        // Track alive count
        enemiesAlive++;
        UpdateWaveProgressUI();

        // Subscribe to death event
        EnemyHealth health = enemyObj.GetComponent<EnemyHealth>();
        if (health != null)
        {
            StartCoroutine(TrackEnemyLifetime(enemyObj));
        }
    }

    /// <summary>
    /// Tracks when an enemy is destroyed
    /// </summary>
    private IEnumerator TrackEnemyLifetime(GameObject enemy)
    {
        while (enemy != null)
        {
            yield return null;
        }

        enemiesAlive--;
        UpdateWaveProgressUI();
    }

    /// <summary>
    /// Shows wave info text
    /// </summary>
    private IEnumerator ShowWaveInfo(string message)
    {
        if (waveInfoText != null)
        {
            waveInfoText.text = message;
            waveInfoText.gameObject.SetActive(true);
            Debug.Log($"[WaveManager] Showing wave info: {message}");
        }

        yield return new WaitForSeconds(waveInfoDisplayTime);

        if (waveInfoText != null)
        {
            waveInfoText.gameObject.SetActive(false);
            Debug.Log($"[WaveManager] Hiding wave info");
        }
    }

    /// <summary>
    /// Updates wave progress UI (enemies left)
    /// </summary>
    private void UpdateWaveProgressUI()
    {
        if (waveProgressText != null)
        {
            if (currentWaveIndex < waves.Length)
            {
                waveProgressText.text = $"Wave {currentWaveIndex + 1}/{waves.Length}\nEnemies: {enemiesAlive}/{totalEnemiesInCurrentWave}";
            }
            else
            {
                waveProgressText.text = "All waves completed!";
            }
        }
    }

    /// <summary>
    /// Debug method to clear all enemies
    /// </summary>
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
        Debug.Log("[WaveManager] Cleared all enemies");
    }
}