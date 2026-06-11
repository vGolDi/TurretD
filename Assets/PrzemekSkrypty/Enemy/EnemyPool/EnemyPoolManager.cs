using System.Collections.Generic;
using UnityEngine;

namespace ElementumDefense.Enemies
{
    /// <summary>
    /// Global router that maps enemy prefabs to their <see cref="EnemyPool"/>.
    /// Auto-creates the singleton on first access — you don't need to drop one
    /// in the scene.
    /// 
    /// Public API:
    ///  - <see cref="Spawn(GameObject, Vector3, Quaternion)"/> — replacement for Instantiate.
    ///  - <see cref="Despawn(GameObject)"/> — replacement for Destroy.
    ///  - <see cref="Prewarm(GameObject, int)"/> — pre-create N instances.
    ///  - <see cref="ClearAll"/> — return every active enemy.
    /// </summary>
    public class EnemyPoolManager : MonoBehaviour
    {
        private static EnemyPoolManager instance;

        public static EnemyPoolManager Instance
        {
            get
            {
                if (instance != null) return instance;
                if (!Application.isPlaying) return null;

                var go = new GameObject("[EnemyPoolManager]");
                instance = go.AddComponent<EnemyPoolManager>();
                DontDestroyOnLoad(go);
                return instance;
            }
        }

        // Keyed by prefab reference (NOT name) — same prefab variant always hits
        // the same pool, different prefabs always get different pools.
        private readonly Dictionary<GameObject, EnemyPool> pools = new Dictionary<GameObject, EnemyPool>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        /// <summary>
        /// Spawn an enemy from the pool tied to <paramref name="prefab"/>.
        /// First call for a given prefab creates the pool lazily.
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogError("[EnemyPoolManager] Spawn called with null prefab!");
                return null;
            }

            EnemyPool pool = GetOrCreatePool(prefab);
            PooledEnemy pe = pool.Get(position, rotation);
            return pe.gameObject;
        }

        /// <summary>
        /// Return a pooled enemy. Safe to call on any GameObject — falls back
        /// to Destroy if the object isn't pooled.
        /// </summary>
        public void Despawn(GameObject enemy)
        {
            if (enemy == null) return;
            PooledEnemy pe = enemy.GetComponent<PooledEnemy>();
            if (pe != null)
            {
                pe.ReturnToPool();
            }
            else
            {
                Destroy(enemy);
            }
        }

        /// <summary>Pre-instantiate <paramref name="count"/> instances of <paramref name="prefab"/> at a NavMesh-valid position.</summary>
        public void Prewarm(GameObject prefab, int count, Vector3 navMeshValidPosition, Quaternion rotation = default)
        {
            if (prefab == null || count <= 0) return;
            GetOrCreatePool(prefab).Prewarm(count, navMeshValidPosition, rotation);
        }

        /// <summary>Returns all active enemies across all pools. Used by ClearAllEnemies.</summary>
        public void ClearAll()
        {
            foreach (var pool in pools.Values)
            {
                pool.ReturnAll();
            }
        }

        // ==========================================
        // INTERNAL
        // ==========================================

        private EnemyPool GetOrCreatePool(GameObject prefab)
        {
            if (pools.TryGetValue(prefab, out EnemyPool existing))
                return existing;

            var poolParentGO = new GameObject($"EnemyPool_{prefab.name}");
            poolParentGO.transform.SetParent(transform);
            var pool = new EnemyPool(prefab, poolParentGO.transform);
            pools.Add(prefab, pool);

            Debug.Log($"[EnemyPoolManager] Created pool for '{prefab.name}'");
            return pool;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
