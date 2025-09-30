using UnityEngine;

[CreateAssetMenu(fileName = "New Wave", menuName = "Tower Defense/Wave")]
public class WaveData : ScriptableObject
{
    [Tooltip("Sekcje wewn¹trz jednej fali (np. ró¿ne typy przeciwników)")]
    public WavePart[] waveParts;

    [Tooltip("Czas oczekiwania przed kolejn¹ fal¹")]
    public float delayAfterWave = 5f;
}

[System.Serializable]
public class WavePart
{
    [Tooltip("Prefab przeciwnika do zespawnowania")]
    public GameObject enemyPrefab;

    [Tooltip("Liczba przeciwników w tej czêœci fali")]
    public int enemyCount;

    [Tooltip("Odstêp czasu miêdzy ka¿dym spawnem (sekundy)")]
    public float spawnInterval = 1f;

    [Tooltip("Która œcie¿ka z WaveManager.paths ma byæ u¿yta (indeks)")]
    public int pathIndex = 0;
}