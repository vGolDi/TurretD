namespace ElementumDefense.Enemies
{
    /// <summary>
    /// Implement on any enemy MonoBehaviour that needs to reset its runtime
    /// state when an enemy is respawned from the pool.
    /// 
    /// Lifecycle:
    ///  1. Pool calls OnSpawnedFromPool() BEFORE SetActive(true).
    ///     -> reset HP, flags, timers, status effects, etc.
    ///  2. Pool calls OnReturnedToPool() AFTER SetActive(false).
    ///     -> drop transient references, stop coroutines.
    /// </summary>
    public interface IEnemyPoolable
    {
        void OnSpawnedFromPool();
        void OnReturnedToPool();
    }
}
