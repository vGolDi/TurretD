using UnityEngine;
using UnityEngine.AI;


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
    public void SetPath(Paths newPath)
    {
        if (newPath == null) return;

        agent = GetComponent<NavMeshAgent>();
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

        // Check if we've reached current waypoint
        if (!agent.pathPending && agent.remainingDistance < waypointReachDistance)
        {
            MoveToNextWaypoint();
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
            // Reached end of path
            OnPathCompleted();
        }
    }

    private void OnPathCompleted()
    {
        Debug.Log($"[Enemy] {gameObject.name} reached end of path at position {transform.position}");

        // Find arena owner
        ArenaOwner arena = GetComponentInParent<ArenaOwner>();
        if (arena == null)
        {
            Debug.LogWarning($"[Enemy] No ArenaOwner in parent, searching in scene...");
            // Try to find in scene (fallback)
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
}
