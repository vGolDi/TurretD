using System.Collections;
using UnityEngine;
using TMPro;
using Photon.Pun;

/// <summary>
/// Controls wave spawning, progression, and game flow
/// In multiplayer: each player has their own WaveManager instance
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("Wave Configuration")]
    [SerializeField, Tooltip("List of waves to spawn")]
    private WaveData[] waves;

    [Header("Spawn Points")]
    [SerializeField, Tooltip("Starting positions for enemies (one per path)")]
    private Transform[] spawnPoints;

    [Header("Paths")]
    [SerializeField, Tooltip("Enemy movement paths")]
    private Paths[] paths;

    [Header("UI")]
    [SerializeField] private TMP_Text waveInfoText;
    [SerializeField] private TMP_Text waveProgressText;
    [SerializeField] private float waveInfoDisplayTime = 2f;

    // Runtime state
    private int currentWaveIndex = 0;
    private bool isSpawning = false;
    private int enemiesAlive = 0;

    private PhotonView photonView;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    private void Start()
    {
        ValidateSetup();
    }

    /// <summary>
    /// Validates wave manager setup (paths, spawn points, etc.)
    /// </summary>
    private void ValidateSetup()
    {
        if (waves == null || waves.Length == 0)
        {
            Debug.LogError("[WaveManager] No waves assigned!");
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[WaveManager] No spawn points assigned!");
        }

        if (paths == null || paths.Length == 0)
        {
            Debug.LogError("[WaveManager] No paths assigned!");
        }

        // Validate each path
        if (paths != null)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                if (paths[i] != null && !paths[i].IsValid())
                {
                    Debug.LogError($"[WaveManager] Path {i} is invalid!");
                }
            }
        }
    }

    /// <summary>
    /// Starts wave spawning sequence
    /// Called by GameStartCountdown or UI button
    /// </summary>
    public void StartWaves()
    {
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

            // Show wave start notification
            StartCoroutine(ShowWaveInfo($"Wave {currentWaveIndex + 1}/{waves.Length}"));

            // Spawn wave
            yield return StartCoroutine(SpawnWave(currentWave));

            // Wait for all enemies to be killed
            yield return new WaitUntil(() => enemiesAlive <= 0);

            // Grant wave completion bonus
            if (currentWave.waveCompletionBonus > 0 && photonView != null && photonView.IsMine)
            {
                PlayerGold.LocalInstance?.AddGold(currentWave.waveCompletionBonus);
                Debug.Log($"[WaveManager] Wave {currentWaveIndex + 1} completed! Bonus: {currentWave.waveCompletionBonus} gold");
            }

            // Delay before next wave
            yield return new WaitForSeconds(currentWave.delayAfterWave);
        }

        isSpawning = false;
        OnAllWavesCompleted();
    }

    /// <summary>
    /// Spawns all parts of a single wave
    /// </summary>
    private IEnumerator SpawnWave(WaveData wave)
    {
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
            for (int i = 0; i < part.enemyCount; i++)
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

        // CRITICAL FIX: Set arena as parent so enemy can find ArenaOwner!
        enemyObj.transform.SetParent(transform.root); // Arena_Prefab is root

        Debug.Log($"[WaveManager] Spawned enemy at {spawnPoint.position}, parent: {enemyObj.transform.parent.name}");

        // Set path
        EnemyMovement movement = enemyObj.GetComponent<EnemyMovement>();
        if (movement != null && path != null)
        {
            movement.SetPath(path);
        }

        // Track alive count
        enemiesAlive++;

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
        yield return new WaitUntil(() => enemy == null);
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
            waveInfoText.gameObject.SetActive(true); // W³¹cz GameObject
            Debug.Log($"[WaveManager] Showing wave info: {message}");
        }
        else
        {
            Debug.LogWarning("[WaveManager] waveInfoText is NULL!");
        }

        yield return new WaitForSeconds(waveInfoDisplayTime);

        if (waveInfoText != null)
        {
            waveInfoText.gameObject.SetActive(false); // Wy³¹cz GameObject
            Debug.Log($"[WaveManager] Hiding wave info");
        }
    }

    /// <summary>
    /// Updates wave progress UI
    /// </summary>
    private void UpdateWaveProgressUI()
    {
        if (waveProgressText != null)
        {
            waveProgressText.text = $"Wave {currentWaveIndex + 1}/{waves.Length}\nEnemies: {enemiesAlive}";
        }
    }

    /// <summary>
    /// Called when all waves are completed
    /// </summary>
    private void OnAllWavesCompleted()
    {
        Debug.Log("[WaveManager] All waves completed!");

        // TODO: Show victory screen
        // TODO: Calculate final score
        // TODO: Award achievements

        StartCoroutine(ShowWaveInfo("VICTORY!"));
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
        Debug.Log("[WaveManager] Cleared all enemies");
    }

    // Editor helper
    private void OnValidate()
    {
        // Auto-find paths if not assigned
        if (paths == null || paths.Length == 0)
        {
            paths = FindObjectsOfType<Paths>();
        }
    }
}