using System.Collections.Generic;
using UnityEngine;

namespace ElementumDefense.Projectiles
{
    /// <summary>
    /// Object pool for projectiles (performance optimization)
    /// Reuses projectile instances instead of Instantiate/Destroy
    /// </summary>
    public class ProjectilePool : MonoBehaviour
    {
        [Header("Pool Settings")]
        public GameObject projectilePrefab; 
        public int initialPoolSize = 20;
        public int maxPoolSize = 100;
        public Transform poolParent;

        private Queue<Projectile> availableProjectiles = new Queue<Projectile>();
        private List<Projectile> activeProjectiles = new List<Projectile>();


        /// <summary>
        /// Setup pool with prefab and sizes (called by ProjectileManager)
        /// </summary>
        public void Setup(GameObject prefab, int initialSize, int maxSize)
        {
            projectilePrefab = prefab;
            initialPoolSize = initialSize;
            maxPoolSize = maxSize;

            Initialize();
        }
        private void Start()
        {
            if (projectilePrefab != null && availableProjectiles.Count == 0)
            {
                Initialize();
            }
        }
        private void Initialize()
        {
            if (poolParent == null)
            {
                GameObject parent = new GameObject($"Pool_{projectilePrefab.name}");
                poolParent = parent.transform;
                poolParent.SetParent(transform);
            }

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewProjectile();
            }

            Debug.Log($"[ProjectilePool] Initialized pool '{projectilePrefab.name}' with {initialPoolSize} instances");
        }
        /// <summary>
        /// Gets projectile from pool (or creates new if pool empty)
        /// </summary>
        public Projectile GetProjectile(Vector3 position, Quaternion rotation)
        {
            Projectile projectile;

            if (availableProjectiles.Count > 0)
            {
                // Reuse from pool
                projectile = availableProjectiles.Dequeue();
            }
            else
            {
                // Create new if pool exhausted
                if (activeProjectiles.Count < maxPoolSize)
                {
                    projectile = CreateNewProjectile();
                    Debug.LogWarning($"[ProjectilePool] Pool exhausted, creating new projectile ({activeProjectiles.Count}/{maxPoolSize})");
                }
                else
                {
                    Debug.LogError($"[ProjectilePool] Max pool size reached ({maxPoolSize})! Reusing oldest.");
                    projectile = activeProjectiles[0];
                    activeProjectiles.RemoveAt(0);
                }
            }

            // Setup projectile
            projectile.transform.position = position;
            projectile.transform.rotation = rotation;
            projectile.gameObject.SetActive(true);

            activeProjectiles.Add(projectile);

            return projectile;
        }

        /// <summary>
        /// Returns projectile to pool
        /// </summary>
        public void ReturnProjectile(Projectile projectile)
        {
            if (projectile == null) return;

            projectile.gameObject.SetActive(false);
            projectile.transform.SetParent(poolParent);

            activeProjectiles.Remove(projectile);
            availableProjectiles.Enqueue(projectile);
        }

        /// <summary>
        /// Creates new projectile instance
        /// </summary>
        private Projectile CreateNewProjectile()
        {
            GameObject obj = Instantiate(projectilePrefab, poolParent);
            obj.SetActive(false);

            Projectile projectile = obj.GetComponent<Projectile>();
            if (projectile == null)
            {
                Debug.LogError($"[ProjectilePool] Prefab '{projectilePrefab.name}' has no Projectile component!");
            }

            availableProjectiles.Enqueue(projectile);

            return projectile;
        }

        /// <summary>
        /// Clears all active projectiles (e.g., wave end)
        /// </summary>
        public void ClearAllActive()
        {
            foreach (var projectile in activeProjectiles.ToArray())
            {
                ReturnProjectile(projectile);
            }

            Debug.Log($"[ProjectilePool] Cleared all active projectiles");
        }

        // Debug info
        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            GUILayout.Label($"Pool '{projectilePrefab.name}': Active={activeProjectiles.Count}, Available={availableProjectiles.Count}");
        }
    }
}