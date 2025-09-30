using System.Collections;
using UnityEngine;

public class SpawnerScript : MonoBehaviour
{
    public GameObject enemyPrefab;
    public Transform[] pathWaypoints;
    public float spawnDelay = 1.5f;
    public int enemyCount = 10;

    private void Start()
    {
        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            GameObject enemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
            var pathScript = enemy.GetComponent<EnemyFollowPath>();
            if (pathScript != null)
            {
                pathScript.waypoints = pathWaypoints;
            }

            yield return new WaitForSeconds(spawnDelay);
        }
    }
}
