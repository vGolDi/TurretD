using System.Collections.Generic;
using UnityEngine;

namespace ElementumDefense.Projectiles
{
    /// <summary>
    /// Global manager for all projectile pools
    /// Singleton - accessible from anywhere
    /// </summary>
    public class ProjectileManager : MonoBehaviour
    {
        public static ProjectileManager Instance { get; private set; }

        [Header("Pool Settings")]
        [SerializeField] private int defaultPoolSize = 20;
        [SerializeField] private int maxPoolSize = 100;

        // Dictionary of pools (one pool per prefab)
        private Dictionary<GameObject, ProjectilePool> pools = new Dictionary<GameObject, ProjectilePool>();

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional - persist between scenes
        }

        /// <summary>
        /// Spawns projectile from pool (or creates new pool if needed)
        /// </summary>
        public Projectile SpawnProjectile(
            GameObject prefab,
            Vector3 position,
            Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError("[ProjectileManager] Tried to spawn null prefab!");
                return null;
            }

            // Get or create pool for this prefab
            if (!pools.ContainsKey(prefab))
            {
                CreatePoolForPrefab(prefab);
            }

            ProjectilePool pool = pools[prefab];
            Projectile projectile = pool.GetProjectile(position, rotation);

            return projectile;
        }

        /// <summary>
        /// Creates new pool for prefab
        /// </summary>
        private void CreatePoolForPrefab(GameObject prefab)
        {
            // Create pool GameObject
            GameObject poolObj = new GameObject($"Pool_{prefab.name}");
            poolObj.transform.SetParent(transform);

            // Add and configure ProjectilePool component
            ProjectilePool pool = poolObj.AddComponent<ProjectilePool>();

            // Use reflection to set private fields (or make them public in ProjectilePool)
            // For now, we'll modify ProjectilePool to have a Setup method

            pool.Setup(prefab, defaultPoolSize, maxPoolSize);

            pools.Add(prefab, pool);

            Debug.Log($"[ProjectileManager] Created pool for '{prefab.name}'");
        }

        /// <summary>
        /// Clears all projectiles (useful for wave end, game over, etc.)
        /// </summary>
        public void ClearAllProjectiles()
        {
            foreach (var pool in pools.Values)
            {
                pool.ClearAllActive();
            }

            Debug.Log("[ProjectileManager] Cleared all projectiles");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}