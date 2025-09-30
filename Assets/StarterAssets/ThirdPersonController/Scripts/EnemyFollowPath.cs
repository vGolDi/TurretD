using UnityEngine;
using UnityEngine.AI;

public class EnemyFollowPath : MonoBehaviour
{
    public Transform[] waypoints;
    private int currentIndex = 0;
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (waypoints.Length > 0)
        {
            agent.SetDestination(waypoints[0].position);
        }
    }

    void Update()
    {
        if (!agent.pathPending && agent.remainingDistance < 0.2f)
        {
            currentIndex++;
            if (currentIndex < waypoints.Length)
            {
                agent.SetDestination(waypoints[currentIndex].position);
            }
            else
            {
                // Wróg dotar³ do koñca — np. znika
                Destroy(gameObject);
            }
        }
    }
}
