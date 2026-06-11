using UnityEngine;
using System.Collections.Generic;
using ElementumDefense.Enemies;

namespace ElementumDefense.Projectiles
{
    /// <summary>
    /// A projectile that travels to its target and upon impact,
    /// spawns several smaller sub-projectiles (fragments) that seek nearby enemies.
    /// Good for: Cluster Bombs, Flak Cannons, Spawner attacks.
    /// </summary>
    public class ClusterProjectile : Projectile
    {
        [Header("Cluster Settings")]
        [Tooltip("Prefab of the smaller projectiles to spawn (e.g. HomingProjectile)")]
        [SerializeField] private GameObject fragmentPrefab;
        
        [Tooltip("How many fragments to spawn on impact")]
        [SerializeField] private int fragmentCount = 3;
        
        [Tooltip("Damage multiplier for fragments (0.5 = fragments deal 50% of main projectile damage)")]
        [Range(0.1f, 2f)]
        [SerializeField] private float fragmentDamageMultiplier = 0.5f;
        
        [Tooltip("Radius to search for enemies to target with fragments")]
        [SerializeField] private float searchRadius = 15f;

        protected override void UpdateMovement()
        {
            // Prosty lot prosto w cel. 
            // Jeśli Twoja gra korzysta z ArcProjectie dla moździerzy, 
            // możesz tu wkleić logikę paraboli z ArcProjectie.
            Vector3 direction = (GetTargetPosition() - transform.position).normalized;

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            transform.position += transform.forward * speed * Time.deltaTime;
        }

        protected override void OnHitTarget(EnemyHealth enemy)
        {
            // Najpierw spawnujemy odłamki
            SpawnClusters(enemy);

            // Następnie wywołujemy standardową logikę trafienia (zadanie obrażeń, usunięcie pocisku matki)
            base.OnHitTarget(enemy);
        }

        private void SpawnClusters(EnemyHealth primaryTarget)
        {
            if (fragmentPrefab == null)
            {
                Debug.LogWarning("[ClusterProjectile] Fragment Prefab is missing!");
                return;
            }

            // Znajdź wszystkich wrogów w promieniu (poza głównym celem, który dostanie z "matki")
            Collider[] hits = Physics.OverlapSphere(transform.position, searchRadius);
            List<EnemyHealth> validTargets = new List<EnemyHealth>();

            foreach (var hit in hits)
            {
                EnemyHealth e = hit.GetComponent<EnemyHealth>();
                if (e != null && e != primaryTarget)
                {
                    validTargets.Add(e);
                }
            }

            // Obliczamy obrażenia dla pojedynczego odłamka
            int fragDamage = Mathf.RoundToInt(damage * fragmentDamageMultiplier);

            // Spawnujemy zadeklarowaną liczbę odłamków
            for (int i = 0; i < fragmentCount; i++)
            {
                EnemyHealth subTarget = null;
                
                // Jeśli mamy wrogów w okolicy, wylosuj im cel
                if (validTargets.Count > 0)
                {
                    subTarget = validTargets[Random.Range(0, validTargets.Count)];
                }

                // Trochę podnosimy miejsce spawnu odłamków, żeby nie rodziły się pod ziemią
                Vector3 spawnPos = transform.position + Vector3.up * 1f;
                Quaternion spawnRot = Quaternion.identity;

                if (subTarget != null)
                {
                    spawnRot = Quaternion.LookRotation((subTarget.transform.position - spawnPos).normalized);
                }
                else
                {
                    // Jeśli nie ma wroga w pobliżu, wyślij je w losowych kierunkach (rozprysk na pustej ziemi)
                    spawnRot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
                }

                // Używamy ProjectileManager do zrespienia pocisków
                Projectile fragment = ProjectileManager.Instance.SpawnProjectile(fragmentPrefab, spawnPos, spawnRot);
                
                if (fragment != null)
                {
                    // Inicjalizujemy odłamek z nowymi statystykami
                    fragment.Initialize(
                        subTarget, 
                        fragDamage, 
                        elementType, 
                        statusEffect, 
                        statusChance, 
                        statusDuration, 
                        statusStrength, 
                        null // Odłamki wrócą do swojej własnej puli na podstawie ich prefabu
                    );
                }
            }

            Debug.Log($"[ClusterProjectile] Spawned {fragmentCount} fragments on impact.");
        }
    }
}
