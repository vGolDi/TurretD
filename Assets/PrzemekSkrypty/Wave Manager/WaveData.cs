using UnityEngine;

[CreateAssetMenu(fileName = "New Wave", menuName = "Tower Defense/Wave")]
public class WaveData : ScriptableObject
{
    [Tooltip("Sekcje wewn�trz jednej fali (np. r�ne typy przeciwnik�w)")]
    public WavePart[] waveParts;

    [Tooltip("Czas oczekiwania przed kolejn� fal�")]
    public float delayAfterWave = 5f;
}

[System.Serializable]
public class WavePart
{
    [Tooltip("Prefab przeciwnika do zespawnowania")]
    public GameObject enemyPrefab;

    [Tooltip("Liczba przeciwnik�w w tej cz�ci fali")]
    public int enemyCount;

    [Tooltip("Odst�p czasu mi�dzy ka�dym spawnem (sekundy)")]
    public float spawnInterval = 1f;

    [Tooltip("Kt�ra �cie�ka z WaveManager.paths ma by� u�yta (indeks)")]
    public int pathIndex = 0;
}