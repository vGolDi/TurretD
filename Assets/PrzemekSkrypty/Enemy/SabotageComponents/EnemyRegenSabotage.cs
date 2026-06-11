using UnityEngine;

namespace ElementumDefense.Enemies
{
    /// <summary>
    /// Wave-scoped regeneration: the enemy heals X% of its maxHP every second.
    /// Added at spawn-time by <see cref="WaveManager"/> when a Regen sabotage
    /// is active. Removed via <see cref="OnReturnedToPool"/> so it doesn't
    /// persist across waves.
    /// </summary>
    public class EnemyRegenSabotage : MonoBehaviour, IEnemyPoolable
    {
        private float percentPerSecond = 0f;
        private float tickAccumulator = 0f;
        private const float TICK_INTERVAL = 0.5f; // heal twice per second

        private EnemyHealth health;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
        }

        public void SetRegenRate(float pct) => percentPerSecond = pct;

        private void Update()
        {
            if (percentPerSecond <= 0f || health == null) return;

            tickAccumulator += Time.deltaTime;
            if (tickAccumulator < TICK_INTERVAL) return;

            tickAccumulator = 0f;
            int healAmount = Mathf.Max(1, Mathf.RoundToInt(health.GetMaxHP() * percentPerSecond * TICK_INTERVAL));
            health.Heal(healAmount);
        }

        public void OnSpawnedFromPool() { /* WaveManager will SetRegenRate after spawn if needed */ }
        public void OnReturnedToPool()
        {
            percentPerSecond = 0f;
            tickAccumulator = 0f;
        }
    }
}
