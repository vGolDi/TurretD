using UnityEngine;
using UnityEngine.AI;
using ElementumDefense.StatusEffects;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    private Paths currentPath;
    private int currentWaypointIndex = 0;
    private NavMeshAgent agent;

    [Header("Movement Settings")]
    [SerializeField, Tooltip("Distance threshold to consider waypoint reached")]
    private float waypointReachDistance = 0.2f;

    private int damageToPlayer = 10;

    [Header("Speed Modifiers")]
    [SerializeField] private float baseSpeed = 3.5f;
    private float currentSpeedModifier = 1f;
    private StatusEffectManager statusEffectManager;

    [Header("NavMesh Avoidance")]
    [SerializeField, Tooltip("Avoidance priority (lower = higher priority)")]
    private int avoidancePriority = 50;

    [SerializeField, Tooltip("Avoidance radius")]
    private float avoidanceRadius = 0.5f;

    [SerializeField, Tooltip("How much to avoid other agents (0-1)")]
    [Range(0f, 1f)]
    private float avoidanceWeight = 0.5f;

    // ========== NOWE: Path Refresh System ==========
    [Header("Path Refresh (Bug Fix)")]
    [SerializeField, Tooltip("Auto-refresh path interval (prevents stuck after slow/freeze)")]
    private float pathRefreshInterval = 0.3f;

    [SerializeField, Tooltip("Min distance to move before considering 'stuck'")]
    private float stuckThreshold = 0.05f;

    private float pathRefreshTimer = 0f;
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private const float STUCK_CHECK_INTERVAL = 1f;
    // ================================================

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        statusEffectManager = GetComponent<StatusEffectManager>();

        if (agent != null)
        {
            ConfigureNavMeshAgent();
        }

        // ========== NOWE: Subscribe to events ==========
        if (statusEffectManager != null)
        {
            statusEffectManager.OnSlowEffectEnded += OnMovementEffectEnded;
            statusEffectManager.OnFreezeEffectEnded += OnMovementEffectEnded;
        }
        // ================================================

        lastPosition = transform.position;
    }

    // ========== NOWA FUNKCJA ==========
    /// <summary>
    /// Called when Slow/Freeze effect ends
    /// </summary>
    private void OnMovementEffectEnded()
    {
        Debug.Log($"[EnemyMovement] Movement effect ended - refreshing path");
        RefreshCurrentDestination();
    }
    // ==================================

    private void OnDestroy()
    {
        // ========== NOWE: Unsubscribe ==========
        if (statusEffectManager != null)
        {
            statusEffectManager.OnSlowEffectEnded -= OnMovementEffectEnded;
            statusEffectManager.OnFreezeEffectEnded -= OnMovementEffectEnded;
        }
        // =======================================
    }

    private void ConfigureNavMeshAgent()
    {
        agent.speed = baseSpeed;
        agent.acceleration = 12f;
        agent.angularSpeed = 180f;

        // Ultra-aggressive settings
        agent.radius = 0.25f;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        agent.avoidancePriority = 50;
        agent.stoppingDistance = 0f;
        agent.autoBraking = false;
        // ========== ZMIEÑ TÊ LINIJKÊ: ==========
        agent.baseOffset = 1f; // By³o 0f - teraz wy¿ej!
                               // LUB dostosuj do wysokoœci swojego modelu:
                               // agent.baseOffset = GetComponent<CapsuleCollider>()?.height / 2f ?? 1f;
                               // =======================================

        Debug.Log($"[EnemyMovement] NavMeshAgent configured: radius={agent.radius:F2}, offset={agent.baseOffset:F2}");
    }

    public void SetPath(Paths newPath)
    {
        if (newPath == null) return;
        if (agent == null) return;

        currentPath = newPath;
        currentWaypointIndex = 0;

        Transform firstWaypoint = currentPath.GetWaypoint(currentWaypointIndex);
        if (firstWaypoint != null)
        {
            agent.SetDestination(firstWaypoint.position);
        }
    }

    void Update()
    {
        if (currentPath == null || agent == null) return;

        ApplyStatusModifiers();
        CheckAndRefreshPath();
        CheckIfStuck();

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

    // ========== NOWA FUNKCJA: Periodic Path Refresh ==========
    /// <summary>
    /// Periodically refreshes NavMeshAgent destination
    /// Prevents "stuck" bug after status effects end
    /// </summary>
    private void CheckAndRefreshPath()
    {
        pathRefreshTimer += Time.deltaTime;

        if (pathRefreshTimer >= pathRefreshInterval)
        {
            pathRefreshTimer = 0f;

            // Only refresh if agent should be moving
            if (!agent.isStopped && currentPath != null)
            {
                // Check if agent has valid path
                if (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
                {
                    Debug.LogWarning($"[EnemyMovement] {gameObject.name} has invalid path! Refreshing...");
                    RefreshCurrentDestination();
                }
            }
        }
    }

    // ========== NOWA FUNKCJA: Stuck Detection ==========
    /// <summary>
    /// Detects if enemy is stuck (not moving when it should)
    /// Forces path refresh if stuck
    /// </summary>
    private void CheckIfStuck()
    {
        stuckTimer += Time.deltaTime;

        if (stuckTimer >= STUCK_CHECK_INTERVAL)
        {
            stuckTimer = 0f;

            // Check if enemy moved enough
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);

            // If barely moved AND should be moving AND has speed
            if (distanceMoved < stuckThreshold &&
                !agent.isStopped &&
                currentSpeedModifier > 0.1f &&
                !statusEffectManager.IsFrozen)
            {
                Debug.LogWarning($"[EnemyMovement] {gameObject.name} appears STUCK! (moved {distanceMoved:F3}m) - Force refreshing path");
                RefreshCurrentDestination();
            }

            lastPosition = transform.position;
        }
    }

    // ========== NOWA FUNKCJA: Refresh Destination ==========
    /// <summary>
    /// Forces NavMeshAgent to recalculate path to current waypoint
    /// </summary>
    private void RefreshCurrentDestination()
    {
        if (currentPath == null || agent == null) return;

        Transform currentWaypoint = currentPath.GetWaypoint(currentWaypointIndex);

        if (currentWaypoint != null)
        {
            // Force recalculate path
            agent.SetDestination(currentWaypoint.position);
            Debug.Log($"[EnemyMovement] Path refreshed to waypoint {currentWaypointIndex}");
        }
    }
    // ==========================================================

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
                agent.speed = 0f; // Frozen = zero speed
                agent.velocity = Vector3.zero; // Force stop any momentum

                Debug.Log($"[EnemyMovement] {gameObject.name} FROZEN (speed=0)");
            }
            else
            {
                // Normal speed with modifier
                agent.speed = baseSpeed * currentSpeedModifier;

                Debug.Log($"[EnemyMovement] {gameObject.name} speed set to {agent.speed:F2} (modifier: {currentSpeedModifier:F2})");
            }
            

            int dynamicPriority = Mathf.RoundToInt(50 + (1f - currentSpeedModifier) * 30);
            agent.avoidancePriority = Mathf.Clamp(dynamicPriority, 0, 99);
        }
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
            Debug.Log($"[Enemy] Found arena owner");
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

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (agent == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, avoidanceRadius);

        // ========== NOWE: Draw path in editor ==========
        if (agent.hasPath)
        {
            Gizmos.color = Color.green;
            Vector3[] corners = agent.path.corners;

            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
        // ================================================
    }
}