using UnityEngine;
using ElementumDefense.Waves;

namespace ElementumDefense.Enemies
{
    /// <summary>
    /// Po śmierci wroga spawnuje N kopii (mniejszych / słabszych) na pozycji śmierci.
    /// Każde dziecko kontynuuje ścieżkę rodzica od bieżącego waypointa.
    /// Dzieci mogą same mieć ten komponent (rekurencyjny split) - kontroluj
    /// poziom rekurencji przez `splitGeneration` w childPrefab.
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemySplitOnDeath : MonoBehaviour, IEnemyPoolable
    {
        [Header("Split Configuration")]
        [Tooltip("Prefab spawnowanego dziecka. Zwykle słabsza wersja tego samego wroga. " +
                 "Może być TEN SAM prefab - wtedy ustaw maxGenerations żeby uniknąć nieskończonego splittu.")]
        [SerializeField] private GameObject childPrefab;

        [Tooltip("Ile dzieci spawnować przy śmierci")]
        [SerializeField, Min(1)] private int childCount = 2;

        [Tooltip("Promień w którym rozsiewani są minionowie wokół miejsca śmierci")]
        [SerializeField] private float spawnRadius = 1.0f;

        [Tooltip("Mnożnik HP dziecka względem maxHP rodzica (0.5 = pół życia rodzica). " +
                 "Działa TYLKO jeśli childPrefab to ten sam typ wroga.")]
        [SerializeField, Range(0.1f, 1f)] private float childHpMultiplier = 0.5f;

        [Tooltip("Skala wizualna dziecka")]
        [SerializeField] private float childScale = 0.7f;

        [Header("Recursion Control")]
        [Tooltip("Bieżąca generacja splittu. 0 = oryginał. Inkrementowane przy każdym splicie. " +
                 "Splittery z generation >= maxGenerations NIE splittują dzieci.")]
        [SerializeField] private int splitGeneration = 0;

        [Tooltip("Maksymalna liczba pokoleń (np. 2 = oryginał -> dzieci -> wnuki, koniec)")]
        [SerializeField, Min(0)] private int maxGenerations = 1;

        [Header("Debug")]
        [Tooltip("Włącz logi do konsoli")]
        [SerializeField] private bool debugLogs = true;

        private EnemyHealth health;
        private bool didDie = false;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            if (health == null)
            {
                Debug.LogError($"[Split:{name}] BRAK EnemyHealth! Komponent nie zadziała.");
                return;
            }
            health.OnDeath += HandleDeath;
            if (debugLogs)
                Debug.Log($"[Split:{name}] Subskrybowany na OnDeath. Generation={splitGeneration}/{maxGenerations}, childCount={childCount}, childPrefab={(childPrefab != null ? childPrefab.name : "NULL")}");
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
            didDie = false;

            // EnemyHealth.OnReturnedToPool clears the OnDeath subscriber list,
            // so we re-subscribe on every spawn.
            if (health != null)
                health.OnDeath += HandleDeath;
        }

        public void OnReturnedToPool()
        {
            if (health != null)
                health.OnDeath -= HandleDeath;
        }

        private void HandleDeath(EnemyHealth source, int killerId)
        {
            if (debugLogs)
                Debug.Log($"[Split:{name}] HandleDeath WYWOŁANE (killerId={killerId})");

            if (didDie)
            {
                if (debugLogs) Debug.Log($"[Split:{name}] Już splittowano (didDie=true) - SKIP");
                return;
            }
            didDie = true;

            if (childPrefab == null)
            {
                Debug.LogWarning($"[Split:{name}] Child Prefab NIE PRZYPISANY w inspektorze - SKIP");
                return;
            }

            if (splitGeneration >= maxGenerations)
            {
                if (debugLogs)
                    Debug.Log($"[Split:{name}] Osiągnięto maxGenerations ({splitGeneration}>={maxGenerations}) - SKIP");
                return;
            }

            SpawnChildren();
        }

        private void SpawnChildren()
        {
            // Skopiuj kontekst rodzica
            var parentMovement = GetComponent<EnemyMovement>();
            Paths parentPath = parentMovement != null ? parentMovement.GetCurrentPath() : null;
            int parentWaypoint = parentMovement != null ? parentMovement.GetCurrentWaypointIndex() : 0;

            int parentMaxHP = health.GetMaxHP();
            int childMaxHP = Mathf.Max(1, Mathf.RoundToInt(parentMaxHP * childHpMultiplier));

            Transform parentRoot = transform.parent != null ? transform.parent : null;

            // Znajdź WaveManager w hierarchii (potrzebny do liczenia enemiesAlive)
            WaveManager waveManager = parentRoot != null
                ? parentRoot.GetComponentInChildren<WaveManager>()
                : Object.FindAnyObjectByType<WaveManager>();

            if (debugLogs)
            {
                Debug.Log($"[Split:{name}] SpawnChildren start. parentPath={(parentPath != null ? parentPath.name : "NULL")}, " +
                          $"parentWaypoint={parentWaypoint}, childMaxHP={childMaxHP}, " +
                          $"waveManager={(waveManager != null ? waveManager.name : "NULL")}, " +
                          $"parentRoot={(parentRoot != null ? parentRoot.name : "NULL")}");
            }

            for (int i = 0; i < childCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

                // Spawn through the pool so split children get the same lifecycle treatment.
                GameObject child = ElementumDefense.Enemies.EnemyPoolManager.Instance != null
                    ? ElementumDefense.Enemies.EnemyPoolManager.Instance.Spawn(childPrefab, spawnPos, Quaternion.identity)
                    : Instantiate(childPrefab, spawnPos, Quaternion.identity);
                if (parentRoot != null) child.transform.SetParent(parentRoot);

                child.transform.localScale = Vector3.one * childScale;

                // HP
                EnemyHealth childHealth = child.GetComponent<EnemyHealth>();
                if (childHealth != null)
                {
                    childHealth.SetMaxHP(childMaxHP);
                }
                else if (debugLogs)
                {
                    Debug.LogWarning($"[Split:{name}] Child #{i} ({child.name}) NIE MA EnemyHealth!");
                }

                // Path (kontynuacja od waypointa rodzica)
                EnemyMovement childMovement = child.GetComponent<EnemyMovement>();
                if (childMovement != null && parentPath != null)
                {
                    childMovement.SetPath(parentPath);
                    // FIX: SetPath resetuje waypoint do 0, więc po nim wymuszamy
                    // kontynuację od waypointa na którym był rodzic - inaczej dzieci
                    // cofają się do początku ścieżki
                    childMovement.SetWaypointIndex(parentWaypoint);
                }
                else if (debugLogs && parentPath == null)
                {
                    Debug.LogWarning($"[Split:{name}] Child #{i}: parentPath==NULL, dziecko będzie stało w miejscu!");
                }

                // Inkrementuj generację u dziecka, żeby kontrolować rekurencję
                EnemySplitOnDeath childSplit = child.GetComponent<EnemySplitOnDeath>();
                if (childSplit != null)
                {
                    childSplit.splitGeneration = splitGeneration + 1;
                }

                // Zarejestruj w WaveManager żeby wave czekała na ich śmierć
                if (waveManager != null)
                {
                    waveManager.RegisterDynamicEnemy(child);
                }

                if (debugLogs)
                    Debug.Log($"[Split:{name}] Spawned child #{i} '{child.name}' at {spawnPos} (HP={childMaxHP}, gen={splitGeneration + 1})");
            }
        }
    }
}
