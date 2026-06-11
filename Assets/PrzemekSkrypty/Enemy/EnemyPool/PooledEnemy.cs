using UnityEngine;

namespace ElementumDefense.Enemies
{
    /// <summary>
    /// Marker / lifecycle bridge component placed on every pooled enemy root.
    /// Auto-added by <see cref="EnemyPool"/> if missing — no manual setup needed.
    /// 
    /// Responsibilities:
    ///  - Remember which pool owns this instance and the source prefab key.
    ///  - Cache the prefab's original scale so Split/Revive scaling can be reverted.
    ///  - Walk all <see cref="IEnemyPoolable"/> components on spawn/return.
    /// </summary>
    public class PooledEnemy : MonoBehaviour
    {
        private EnemyPool pool;
        private GameObject sourcePrefab;
        private Vector3 originalScale;
        private IEnemyPoolable[] poolables;
        private bool initialized;

        public GameObject SourcePrefab => sourcePrefab;
        public bool IsInitialized => initialized;

        /// <summary>Called once by EnemyPool right after Instantiate.</summary>
        public void Initialize(EnemyPool ownerPool, GameObject prefab)
        {
            pool = ownerPool;
            sourcePrefab = prefab;
            originalScale = transform.localScale;
            poolables = GetComponentsInChildren<IEnemyPoolable>(includeInactive: true);
            initialized = true;
        }

        /// <summary>
        /// Called by EnemyPool right AFTER SetActive(true). Agent operations
        /// are valid here because the GameObject is active and the
        /// NavMeshAgent has had a chance to bind to the navmesh.
        /// </summary>
        public void OnGetFromPool()
        {
            transform.localScale = originalScale;

            if (poolables == null) return;
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i]?.OnSpawnedFromPool();
            }
        }

        /// <summary>
        /// Public entry used by EnemyHealth.Die / EnemyMovement.OnPathCompleted.
        /// If the pool is gone (e.g., scene unloading) we fall back to Destroy.
        /// </summary>
        public void ReturnToPool()
        {
            if (pool == null)
            {
                Destroy(gameObject);
                return;
            }
            pool.Return(this);
        }

        /// <summary>Called by EnemyPool right after SetActive(false).</summary>
        public void OnReturnedToPoolInternal()
        {
            if (poolables == null) return;
            for (int i = 0; i < poolables.Length; i++)
            {
                poolables[i]?.OnReturnedToPool();
            }
        }
    }
}
