using UnityEngine;

/// <summary>
/// ScriptableObject defining a single wave
/// Contains multiple wave parts (enemy groups)
/// </summary>
[CreateAssetMenu(fileName = "Wave_01", menuName = "Tower Defense/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Wave Configuration")]
    [SerializeField, Tooltip("Groups of enemies in this wave")]
    public WavePart[] waveParts;

    [SerializeField, Tooltip("Delay before next wave starts (seconds)")]
    public float delayAfterWave = 5f;

    [Header("Rewards")]
    [SerializeField, Tooltip("Bonus gold for completing this wave")]
    public int waveCompletionBonus = 0;

    // TODO: Add difficulty multiplier
    // public float healthMultiplier = 1f;
    // public float speedMultiplier = 1f;

    /// <summary>
    /// Returns total enemy count in this wave
    /// </summary>
    public int GetTotalEnemyCount()
    {
        int total = 0;
        if (waveParts != null)
        {
            foreach (var part in waveParts)
            {
                total += part.enemyCount;
            }
        }
        return total;
    }

    /// <summary>
    /// Returns estimated wave duration in seconds
    /// </summary>
    public float GetEstimatedDuration()
    {
        float duration = 0f;
        if (waveParts != null)
        {
            foreach (var part in waveParts)
            {
                duration += part.enemyCount * part.spawnInterval;
            }
        }
        return duration;
    }
}

/// <summary>
/// Defines a group of enemies within a wave
/// </summary>
[System.Serializable]
public class WavePart
{
    [Header("Enemy Configuration")]
    [Tooltip("Enemy prefab to spawn")]
    public GameObject enemyPrefab;

    [Tooltip("Number of enemies to spawn")]
    public int enemyCount = 10;

    [Tooltip("Time between each spawn (seconds)")]
    public float spawnInterval = 1f;

    [Header("Path Assignment")]
    [Tooltip("Which path index from WaveManager.paths to use")]
    public int pathIndex = 0;

    // TODO: Add spawn formation patterns
    // public SpawnPattern spawnPattern = SpawnPattern.Linear;
}