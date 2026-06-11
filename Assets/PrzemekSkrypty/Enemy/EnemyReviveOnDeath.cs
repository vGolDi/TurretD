using UnityEngine;
using ElementumDefense.Waves;

namespace ElementumDefense.Enemies
{
    /// <summary>
    /// Po śmierci wroga spawnuje POJEDYNCZĄ kopię (revive). Różnica względem
    /// EnemySplitOnDeath:
    /// - zawsze 1 dziecko (matryoshka / phoenix / boss-form-2)
    /// - dziecko ma % HP rodzica (lub własne maxHP z prefab-a, jeśli reviveHpPercent=0)
    /// - spawnowany prefab MOŻE BYĆ INNY (transformacja w innego potwora)
    /// - osobny licznik rewi (maxRevives), niezależny od split
    ///
    /// Przykłady użycia:
    /// 1) Phoenix: revivePrefab = ten sam prefab, reviveHpPercent = 0.5, maxRevives = 1
    /// 2) Matryoshka: revivePrefab = mniejsza wersja, reviveHpPercent = 1.0, maxRevives = 2
    /// 3) Boss Form 2: revivePrefab = inny boss, reviveHpPercent = 1.0, maxRevives = 1
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyReviveOnDeath : MonoBehaviour, IEnemyPoolable
    {
        [Header("Revive Prefab")]
        [Tooltip("Prefab spawnowany po śmierci. Może być ten sam (klasyczne revive) " +
                 "lub inny (transformacja, matryoshka, druga forma bossa).")]
        [SerializeField] private GameObject revivePrefab;

        [Header("Health")]
        [Tooltip("HP rewi'u jako % maxHP rodzica. 0 = użyj domyślnego maxHP z prefabu, " +
                 "0.5 = pół życia rodzica, 1.0 = pełne życie rodzica.")]
        [SerializeField, Range(0f, 1f)] private float reviveHpPercent = 0.5f;

        [Header("Revive Count")]
        [Tooltip("Maksymalna liczba rewi'ów. 1 = jeden raz wstaje, potem normalna śmierć. " +
                 "2 = wstaje dwa razy (matryoshka), itd.")]
        [SerializeField, Min(1)] private int maxRevives = 1;

        [Tooltip("Bieżący licznik rewi'ów. 0 = jeszcze nie wstawał. Inkrementowany na " +
                 "spawnowanym dziecku, nie na rodzicu.")]
        [SerializeField] private int reviveCount = 0;

        [Header("Visual / Position")]
        [Tooltip("Skala spawnowanego rewi'u (np. 0.7 dla matryoshki - zmniejszone)")]
        [SerializeField] private float reviveScale = 1f;

        [Tooltip("Offset pozycji spawnu (0,0,0 = dokładnie tam gdzie zginął rodzic)")]
        [SerializeField] private Vector3 spawnOffset = Vector3.zero;

        [Header("VFX (opcjonalne)")]
        [Tooltip("VFX odpalany w momencie rewi'u (np. dym, eksplozja, błysk)")]
        [SerializeField] private GameObject reviveVfx;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private EnemyHealth health;
        private bool didRevive = false;

        // Snapshots of prefab defaults — restored by OnSpawnedFromPool so
        // sabotage-applied revives don't bleed into the next wave.
        private GameObject prefabRevivePrefab;
        private float prefabReviveHpPercent;
        private int prefabMaxRevives;

        public int CurrentReviveCount => reviveCount;
        public int MaxRevives => maxRevives;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            if (health == null)
            {
                Debug.LogError($"[Revive:{name}] BRAK EnemyHealth! Komponent nie zadziała.");
                return;
            }
            // Snapshot prefab values for pool reset.
            prefabRevivePrefab = revivePrefab;
            prefabReviveHpPercent = reviveHpPercent;
            prefabMaxRevives = maxRevives;

            health.OnDeath += HandleDeath;
            if (debugLogs)
                Debug.Log($"[Revive:{name}] Subskrybowany na OnDeath. " +
                          $"reviveCount={reviveCount}/{maxRevives}, " +
                          $"prefab={(revivePrefab != null ? revivePrefab.name : "NULL")}");
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnDeath -= HandleDeath;
        }

        // ==========================================
        // POOLING
        // ==========================================

        public void OnSpawnedFromPool()
        {
            didRevive = false;
            // reviveCount is set BY the spawner (parent revive bumps it on the child)
            // so we don't reset it here — that would break matryoshka chains.

            // Restore prefab default for revive prefab (sabotage may have set it).
            revivePrefab = prefabRevivePrefab;
            reviveHpPercent = prefabReviveHpPercent;
            maxRevives = prefabMaxRevives;

            if (health != null)
                health.OnDeath += HandleDeath;
        }

        /// <summary>
        /// Sabotage entry point: makes this enemy revive once for the duration
        /// of the wave. Pool reset wipes it on next reuse.
        /// </summary>
        public void ApplyFromSabotage(GameObject prefab, float hpPercent)
        {
            // If sabotage didn't supply a prefab, default to "this same enemy".
            revivePrefab = prefab != null ? prefab : gameObject;
            reviveHpPercent = Mathf.Clamp01(hpPercent);
            // Make sure at least 1 revive is allowed even if prefab default was 0.
            if (maxRevives < 1) maxRevives = 1;
            didRevive = false;
        }

        public void OnReturnedToPool()
        {
            if (health != null)
                health.OnDeath -= HandleDeath;
        }

        private void HandleDeath(EnemyHealth source, int killerId)
        {
            if (debugLogs)
                Debug.Log($"[Revive:{name}] HandleDeath WYWOŁANE (killerId={killerId}, " +
                          $"reviveCount={reviveCount}/{maxRevives})");

            if (didRevive)
            {
                if (debugLogs) Debug.Log($"[Revive:{name}] Już rewi'wał (didRevive=true) - SKIP");
                return;
            }
            didRevive = true;

            if (revivePrefab == null)
            {
                Debug.LogWarning($"[Revive:{name}] Revive Prefab NIE PRZYPISANY w inspektorze - SKIP");
                return;
            }

            if (reviveCount >= maxRevives)
            {
                if (debugLogs)
                    Debug.Log($"[Revive:{name}] Wyczerpane rewi'e ({reviveCount}>={maxRevives}) - SKIP, normalna śmierć");
                return;
            }

            SpawnRevive();
        }

        private void SpawnRevive()
        {
            // Skopiuj kontekst rodzica
            var parentMovement = GetComponent<EnemyMovement>();
            Paths parentPath = parentMovement != null ? parentMovement.GetCurrentPath() : null;
            int parentWaypoint = parentMovement != null ? parentMovement.GetCurrentWaypointIndex() : 0;

            int parentMaxHP = health.GetMaxHP();
            Transform parentRoot = transform.parent != null ? transform.parent : null;

            WaveManager waveManager = parentRoot != null
                ? parentRoot.GetComponentInChildren<WaveManager>()
                : Object.FindAnyObjectByType<WaveManager>();

            // Spawn pozycja: tam gdzie zginął rodzic + offset
            Vector3 spawnPos = transform.position + spawnOffset;

            GameObject revive = ElementumDefense.Enemies.EnemyPoolManager.Instance != null
                ? ElementumDefense.Enemies.EnemyPoolManager.Instance.Spawn(revivePrefab, spawnPos, transform.rotation)
                : Instantiate(revivePrefab, spawnPos, transform.rotation);
            if (parentRoot != null) revive.transform.SetParent(parentRoot);

            revive.transform.localScale = Vector3.one * reviveScale;

            // HP - albo % rodzica, albo domyślne z prefabu (jeśli percent==0)
            EnemyHealth reviveHealth = revive.GetComponent<EnemyHealth>();
            if (reviveHealth != null && reviveHpPercent > 0f)
            {
                int reviveMaxHP = Mathf.Max(1, Mathf.RoundToInt(parentMaxHP * reviveHpPercent));
                reviveHealth.SetMaxHP(reviveMaxHP);
            }

            // Path - kontynuacja od waypointa rodzica
            EnemyMovement reviveMovement = revive.GetComponent<EnemyMovement>();
            if (reviveMovement != null && parentPath != null)
            {
                reviveMovement.SetPath(parentPath);
                reviveMovement.SetWaypointIndex(parentWaypoint);
            }
            else if (debugLogs && parentPath == null)
            {
                Debug.LogWarning($"[Revive:{name}] parentPath==NULL, rewi będzie stać w miejscu!");
            }

            // Inkrementuj licznik na spawnowanym dziecku - ŻYCIOWE bo bez tego
            // każda generacja zaczynałaby od 0 i mielibyśmy nieskończony loop
            EnemyReviveOnDeath childRevive = revive.GetComponent<EnemyReviveOnDeath>();
            if (childRevive != null)
            {
                childRevive.reviveCount = reviveCount + 1;
                childRevive.maxRevives = maxRevives; // dziedziczy max
            }

            // Zarejestruj w WaveManager - inaczej wave nie czeka na rewi
            if (waveManager != null)
            {
                waveManager.RegisterDynamicEnemy(revive);
            }

            // VFX
            if (reviveVfx != null)
            {
                Instantiate(reviveVfx, spawnPos, Quaternion.identity);
            }

            if (debugLogs)
                Debug.Log($"[Revive:{name}] Spawned revive '{revive.name}' at {spawnPos} " +
                          $"(HP={reviveHealth?.GetMaxHP() ?? -1}, count={reviveCount + 1}/{maxRevives})");
        }
    }
}
