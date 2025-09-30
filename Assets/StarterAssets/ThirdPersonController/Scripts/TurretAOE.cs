using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretAOE : MonoBehaviour
{
    public float damagePerTick = 5f;
    public float tickRate = 1f;
    public float radius = 5f; // zasiêg ataku
    private List<EnemyHealth> enemiesInRange = new List<EnemyHealth>();
    private LineRenderer lr;

    void Start()
    {
        GetComponent<SphereCollider>().radius = radius;

        lr = GetComponent<LineRenderer>();
        DrawRangeCircle();

        StartCoroutine(DamageEnemies());
    }

    void DrawRangeCircle()
    {
        int segments = 50;
        lr.positionCount = segments;
        float angle = 0f;

        for (int i = 0; i < segments; i++)
        {
            float x = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            float z = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            lr.SetPosition(i, new Vector3(x, 0.01f, z));
            angle += 360f / segments;
        }
    }

    IEnumerator DamageEnemies()
    {
        while (true)
        {
            foreach (EnemyHealth enemy in enemiesInRange.ToArray())
            {
                if (enemy != null)
                    enemy.TakeDamage((int)damagePerTick);
                else
                    enemiesInRange.Remove(enemy);
            }

            yield return new WaitForSeconds(tickRate);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy != null && !enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Add(enemy);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy != null && enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Remove(enemy);
        }
    }
}
