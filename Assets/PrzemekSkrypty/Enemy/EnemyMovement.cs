using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    private Paths currentPath;
    private int currentWaypointIndex = 0;
    private NavMeshAgent agent;

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

        if (!agent.pathPending && agent.remainingDistance < 0.2f)
        {
            currentWaypointIndex++;
            Transform nextWaypoint = currentPath.GetWaypoint(currentWaypointIndex);

            if (nextWaypoint != null)
            {
                agent.SetDestination(nextWaypoint.position);
            }
            else
            {
                Destroy(gameObject); // dotar³ do koñca œcie¿ki
            }
        }
    }
}
