using UnityEngine;
using UnityEngine.AI;
using ElementumDefense.Enemies;
using ElementumDefense.StatusEffects;
using ElementumDefense.Players;
using ElementumDefense.Waves;


namespace ElementumDefense.Enemies
{
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour, IEnemyPoolable
{
    [Header("Data Source (optional)")]
    [SerializeField, Tooltip("Optional EnemyData SO. If assigned, overrides speed / waypointReachDistance / damage / agent settings in Awake. Leave null to use the inspector fields below.")]
    private ElementumDefense.Enemies.EnemyData enemyData;

    private Paths currentPath;
    private int currentWaypointIndex = 0;
    private NavMeshAgent agent;
    private EnemyHealth health;
    private float originalBaseSpeed;

    [Header("Movement Settings")]
    [SerializeField, Tooltip("Distance threshold to consider waypoint reached")]
    private float waypointReachDistance = 0.2f;

    private int damageToPlayer = 10;

    [Header("Speed Modifiers")]
    [SerializeField] private float baseSpeed = 3.5f;
    private float currentSpeedModifier = 1f;
    private StatusEffectManager statusEffectManager;

    [SerializeField, Tooltip("Avoidance radius")]
    private float avoidanceRadius = 0.5f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        statusEffectManager = GetComponent<StatusEffectManager>();
        health = GetComponent<EnemyHealth>();

        // Apply EnemyData overrides BEFORE we snapshot baseSpeed and configure
        // the agent — the SO values become the source of truth for this run.
        if (enemyData != null)
        {
            baseSpeed = enemyData.baseSpeed;
            waypointReachDistance = enemyData.waypointReachDistance;
            damageToPlayer = enemyData.damageToPlayer;
        }

        originalBaseSpeed = baseSpeed;

        if (agent != null)
        {
            ConfigureNavMeshAgent();
        }

        // The only legitimate "path refresh" trigger is when a movement-impairing
        // status effect (Slow / Freeze) ends. NavMeshAgent occasionally drops the
        // path while speed=0; we re-issue SetDestination once when speed returns.
        // No co-frame stuck detection — that masked real navmesh bugs and burnt
        // ~100 SetDestination calls/sec at 100 enemies on screen.
        if (statusEffectManager != null)
        {
            statusEffectManager.OnSlowEffectEnded += OnMovementEffectEnded;
            statusEffectManager.OnFreezeEffectEnded += OnMovementEffectEnded;
        }
    }

    /// <summary>
    /// Called when Slow/Freeze effect ends — re-issues SetDestination so the
    /// agent picks up where it left off. This is the ONLY place where we
    /// proactively refresh the destination outside normal waypoint advancement.
    /// </summary>
    private void OnMovementEffectEnded()
    {
        RefreshCurrentDestination();
    }

    private void OnDestroy()
    {
        if (statusEffectManager != null)
        {
            statusEffectManager.OnSlowEffectEnded -= OnMovementEffectEnded;
            statusEffectManager.OnFreezeEffectEnded -= OnMovementEffectEnded;
        }
    }

    // ==========================================
    // POOLING
    // ==========================================

    /// <summary>Reset all movement state before pool reuse.</summary>
    public void OnSpawnedFromPool()
    {
        currentPath = null;
        currentWaypointIndex = 0;
        currentSpeedModifier = 1f;

        // Restore base speed (sabotage/aura may have mutated it last life).
        baseSpeed = originalBaseSpeed;

        // NavMeshAgent may have been disabled while the GO was inactive.
        // Only touch agent.isStopped / .speed / .ResetPath if it's actually
        // attached to a NavMesh — otherwise we get spam errors.
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.Warp(transform.position);
            agent.isStopped = false;
            agent.velocity = Vector3.zero;
            agent.speed = originalBaseSpeed;
        }
    }

    public void OnReturnedToPool()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    private void ConfigureNavMeshAgent()
    {
        agent.speed = baseSpeed;
        agent.stoppingDistance = 0f;
        agent.autoBraking = false;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        agent.avoidancePriority = 50;

        // Tunable per-archetype if EnemyData is assigned — fallback to old hardcoded values.
        if (enemyData != null)
        {
            agent.acceleration = enemyData.agentAcceleration;
            agent.angularSpeed = enemyData.agentAngularSpeed;
            agent.radius = enemyData.agentRadius;
            agent.baseOffset = enemyData.agentBaseOffset;
        }
        else
        {
            agent.acceleration = 12f;
            agent.angularSpeed = 180f;
            agent.radius = 0.25f;
            agent.baseOffset = 1f;
        }

        Debug.Log($"[EnemyMovement] NavMeshAgent configured: radius={agent.radius:F2}, offset={agent.baseOffset:F2}");
    }

    public void SetPath(Paths newPath)
    {
        if (newPath == null) return;
        if (agent == null) return;

        currentPath = newPath;
        currentWaypointIndex = 0;

        Transform firstWaypoint = currentPath.GetWaypoint(currentWaypointIndex);
        if (firstWaypoint != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.SetDestination(firstWaypoint.position);
        }
    }

    /// <summary>
    /// Wymusza kontynuację ścieżki od konkretnego waypointa - używane przez SplitOnDeath,
    /// żeby dziecko nie cofało się do początku.
    /// </summary>
    public void SetWaypointIndex(int waypointIndex)
    {
        if (currentPath == null || agent == null) return;

        currentWaypointIndex = Mathf.Max(0, waypointIndex);
        Transform wp = currentPath.GetWaypoint(currentWaypointIndex);
        if (wp != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.SetDestination(wp.position);
        }
    }

    /// <summary>Bieżąca ścieżka (do skopiowania na splitted enemies).</summary>
    public Paths GetCurrentPath() => currentPath;

    /// <summary>Bieżący indeks waypointa - splitted enemies kontynuują z tego samego punktu.</summary>
    public int GetCurrentWaypointIndex() => currentWaypointIndex;

    void Update()
    {
        if (currentPath == null || agent == null) return;

        // Pooled enemies briefly exist with NavMeshAgent disabled (between
        // SetActive(false) and re-attaching). Skip the whole movement frame
        // until the agent is back on the navmesh.
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        ApplyStatusModifiers();

        if (statusEffectManager != null &&
            (statusEffectManager.IsFrozen || currentSpeedModifier < 0.1f))
        {
            return;
        }

        if (!agent.pathPending && agent.remainingDistance < waypointReachDistance)
        {
            MoveToNextWaypoint();
        }
    }

    /// <summary>
    /// Forces NavMeshAgent to recalculate path to current waypoint.
    /// Called only by event-driven hooks (e.g. status effect ending).
    /// </summary>
    private void RefreshCurrentDestination()
    {
        if (currentPath == null || agent == null) return;
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        Transform currentWaypoint = currentPath.GetWaypoint(currentWaypointIndex);
        if (currentWaypoint != null)
        {
            agent.SetDestination(currentWaypoint.position);
        }
    }

    private void ApplyStatusModifiers()
    {
        if (statusEffectManager == null) return;

        float modifier = statusEffectManager.SpeedModifier;

        if (Mathf.Abs(currentSpeedModifier - modifier) > 0.01f)
        {
            currentSpeedModifier = modifier;

            agent.speed = baseSpeed * currentSpeedModifier;

            if (statusEffectManager.IsFrozen)
            {
                agent.speed = 0f;            // Frozen = zero speed
                agent.velocity = Vector3.zero; // Force-stop any momentum
            }
            else
            {
                agent.speed = baseSpeed * currentSpeedModifier;
            }

            // Scale avoidance priority with speed: slower agents yield to faster ones.
            int dynamicPriority = Mathf.RoundToInt(50 + (1f - currentSpeedModifier) * 30);
            agent.avoidancePriority = Mathf.Clamp(dynamicPriority, 0, 99);
        }
    }

    public float GetBaseSpeed() => baseSpeed;

    public void SetBaseSpeed(float newSpeed)
    {
        baseSpeed = newSpeed;
        if (agent != null)
            agent.speed = baseSpeed * currentSpeedModifier;
    }

    public void SetSpeedModifier(float modifier)
    {
        currentSpeedModifier = Mathf.Clamp01(modifier);
        if (agent != null)
        {
            agent.speed = baseSpeed * currentSpeedModifier;
        }
    }

    private void MoveToNextWaypoint()
    {
        currentWaypointIndex++;
        Transform nextWaypoint = currentPath.GetWaypoint(currentWaypointIndex);

        if (nextWaypoint != null)
        {
            agent.SetDestination(nextWaypoint.position);
        }
        else
        {
            OnPathCompleted();
        }
    }

    private void OnPathCompleted()
    {
        Debug.Log($"[Enemy] {gameObject.name} reached end of path at position {transform.position}");

        ArenaOwner arena = GetComponentInParent<ArenaOwner>();
        if (arena == null)
        {
            Debug.LogWarning($"[Enemy] No ArenaOwner in parent, searching in scene...");
            arena = FindAnyObjectByType<ArenaOwner>();
        }

        if (arena != null)
        {
            PlayerHealth ownerHealth = arena.GetOwnerHealth();
            if (ownerHealth != null)
            {
                ownerHealth.TakeDamage(damageToPlayer);
                Debug.Log($"[Enemy] Reached end! Dealt {damageToPlayer} damage to player");
            }
            else
            {
                Debug.LogError($"[Enemy] ArenaOwner.GetOwnerHealth() returned NULL!");
            }
        }
        else
        {
            Debug.LogError("[EnemyMovement] Could not find ArenaOwner to damage!");
        }

        // Return to pool instead of destroying — same code path as natural death.
        if (health != null)
            health.ReleaseToPoolOrDestroy();
        else
            Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (agent == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, avoidanceRadius);

        if (agent.hasPath)
        {
            Gizmos.color = Color.green;
            Vector3[] corners = agent.path.corners;

            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }
}
}
