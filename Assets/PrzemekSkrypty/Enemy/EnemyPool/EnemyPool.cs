using System.Collections.Generic;
using UnityEngine;

namespace ElementumDefense.Enemies
{
    /// <summary>
    /// Per-prefab object pool for enemies. One instance lives per unique
    /// enemy prefab (managed by <see cref="EnemyPoolManager"/>).
    /// 
    /// Important lifecycle detail for NavMeshAgent enemies: the FIRST instantiate
    /// happens AT the requested spawn position so the agent attaches to a
    /// NavMesh-valid location during Awake. If we instantiated at the pool root
    /// transform (often 0,0,0) the agent could fail to bind to the navmesh and
    /// the enemy would just stand there.
    /// </summary>
    public class EnemyPool
    {
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly Queue<PooledEnemy> available = new Queue<PooledEnemy>();
        private readonly HashSet<PooledEnemy> active = new HashSet<PooledEnemy>();

        public GameObject Prefab => prefab;
        public int AvailableCount => available.Count;
        public int ActiveCount => active.Count;

        public EnemyPool(GameObject sourcePrefab, Transform poolParent)
        {
            prefab = sourcePrefab;
            parent = poolParent;
        }

        /// <summary>
        /// Pre-instantiate <paramref name="count"/> instances at a known
        /// NavMesh-valid position. Park them inactive under the pool parent.
        /// </summary>
        public void Prewarm(int count, Vector3 navMeshValidPosition, Quaternion rotation)
        {
            for (int i = 0; i < count; i++)
            {
                PooledEnemy pe = CreateNewInstance(navMeshValidPosition, rotation);
                pe.gameObject.SetActive(false);
                available.Enqueue(pe);
            }
        }

        /// <summary>Get a fresh-state enemy at the requested transform.</summary>
        public PooledEnemy Get(Vector3 position, Quaternion rotation)
        {
            PooledEnemy pe;

            if (available.Count > 0)
            {
                pe = available.Dequeue();
                // Move to spawn position WHILE INACTIVE so OnEnable sees the
                // right transform — NavMeshAgent will then attach to navmesh
                // at this location instead of wherever it was last frame.
                pe.transform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                // First creation: instantiate directly at spawn position so
                // Awake/OnEnable see a NavMesh-valid location from the start.
                pe = CreateNewInstance(position, rotation);
            }

            // Activate FIRST. This re-attaches NavMeshAgent and runs every
            // OnEnable hook (e.g. EnemyArmor.AllArmored registration).
            pe.gameObject.SetActive(true);

            // Now run reset hooks — agent operations are valid here because
            // the GameObject is active and the agent is on the navmesh.
            pe.OnGetFromPool();

            active.Add(pe);
            return pe;
        }

        /// <summary>Return an enemy to the pool. Called by PooledEnemy.ReturnToPool.</summary>
        public void Return(PooledEnemy pe)
        {
            if (pe == null) return;
            if (!active.Remove(pe))
            {
                // Already returned (double-free) — silently ignore.
                return;
            }

            pe.gameObject.SetActive(false);
            pe.transform.SetParent(parent, worldPositionStays: false);

            pe.OnReturnedToPoolInternal();

            available.Enqueue(pe);
        }

        /// <summary>Returns every active instance to the pool. Used by ClearAllEnemies.</summary>
        public void ReturnAll()
        {
            // Copy because Return mutates the active set.
            var snapshot = new List<PooledEnemy>(active);
            for (int i = 0; i < snapshot.Count; i++)
            {
                Return(snapshot[i]);
            }
        }

        private PooledEnemy CreateNewInstance(Vector3 position, Quaternion rotation)
        {
            // Instantiate at spawn position so NavMeshAgent.Awake binds correctly.
            // Re-parent under the pool root afterwards (worldPositionStays: true
            // would re-evaluate world transform anyway, so we use the explicit
            // 4-arg overload which sets parent without re-warping).
            GameObject go = Object.Instantiate(prefab, position, rotation, parent);
            PooledEnemy pe = go.GetComponent<PooledEnemy>();
            if (pe == null) pe = go.AddComponent<PooledEnemy>();
            pe.Initialize(this, prefab);
            return pe;
        }
    }
}
