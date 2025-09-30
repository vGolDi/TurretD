using System.IO;
using UnityEngine;
using System.Collections;
using TMPro;


/// <summary>
/// G��wny menad�er gry, kt�ry kontroluje przebieg fal przeciwnik�w.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [Header("Konfiguracja fal")]
    [Tooltip("Lista fal do wykonania w tej rozgrywce")]
    [SerializeField] private WaveData[] waves;

    [Header("Pozycje startowe przeciwnik�w")]
    [Tooltip("Punkty, z kt�rych spawnuj� przeciwnicy (jeden na �cie�k�)")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("�cie�ki poruszania si� przeciwnik�w")]
    [Tooltip("Lista tras (waypoint�w), kt�rymi poruszaj� si� przeciwnicy")]
    [SerializeField] private Paths[] paths;

    [Header("UI")]
    [SerializeField] private TMP_Text waveInfoText;
    [SerializeField] private float waveInfoDisplayTime = 2f;


    private int currentWaveIndex = 0;

    void Start()
    {
        // TODO: Doda� przycisk UI do startu fali 
    }

    public void StartWaves()
    {
        StartCoroutine(RunGameWaves());
    }

    private IEnumerator ShowWaveInfo(string message)
    {
        if (waveInfoText != null)
        {
            waveInfoText.text = message;
            waveInfoText.enabled = true;
        }

        yield return new WaitForSeconds(waveInfoDisplayTime);

        if (waveInfoText != null)
        {
            waveInfoText.enabled = false;
        }
    }

    private IEnumerator RunGameWaves()
    {
        for (int i = 0; i < waves.Length; i++)
        {
            currentWaveIndex = i + 1;

            // Pokaz info
            yield return StartCoroutine(ShowWaveInfo($"Rozpoczyna si� fala {currentWaveIndex}"));

            yield return StartCoroutine(SpawnWave(waves[i]));
            yield return new WaitForSeconds(waves[i].delayAfterWave);
            ClearAllEnemies();
        }

        Debug.Log("All waves completed!");
    }

    private void ClearAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }
    }
    private IEnumerator SpawnWave(WaveData wave)
    {
        foreach (WavePart part in wave.waveParts)
        {
            for (int i = 0; i < part.enemyCount; i++)
            {
                GameObject enemyObj = Instantiate(
                    part.enemyPrefab,
                    spawnPoints[part.pathIndex].position,
                    Quaternion.identity
                );

                EnemyMovement enemyMovement = enemyObj.GetComponent<EnemyMovement>();
                if (enemyMovement != null && paths.Length > part.pathIndex)
                {
                    enemyMovement.SetPath(paths[part.pathIndex]);
                }

                yield return new WaitForSeconds(part.spawnInterval);
            }
        }
    }
}
