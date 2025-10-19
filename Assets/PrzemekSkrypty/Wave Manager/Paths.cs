using UnityEngine;

/// <summary>
/// Stores sequence of waypoints defining enemy path
/// Visualizes path in editor with colored lines
/// </summary>
public class Paths : MonoBehaviour
{
    [Header("Waypoint Configuration")]
    [SerializeField, Tooltip("Ordered list of waypoints (enemies follow in sequence)")]
    private Transform[] waypoints;

    [Header("Editor Visualization")]
    [SerializeField] private Color pathColor = Color.cyan;
    [SerializeField] private bool showWaypointNumbers = true;
    [SerializeField] private float waypointGizmoSize = 0.3f;

    /// <summary>
    /// Returns waypoint at specified index
    /// </summary>
    /// <param name="index">Waypoint index (0-based)</param>
    /// <returns>Transform of waypoint, or null if invalid index</returns>
    public Transform GetWaypoint(int index)
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("[Paths] No waypoints assigned!");
            return null;
        }

        if (index < 0 || index >= waypoints.Length)
        {
            return null; // End of path
        }

        return waypoints[index];
    }

    /// <summary>
    /// Returns total number of waypoints in path
    /// </summary>
    public int GetWaypointCount()
    {
        return waypoints?.Length ?? 0;
    }

    /// <summary>
    /// Validates path (checks for null waypoints)
    /// </summary>
    public bool IsValid()
    {
        if (waypoints == null || waypoints.Length < 2)
        {
            return false;
        }

        foreach (var wp in waypoints)
        {
            if (wp == null) return false;
        }

        return true;
    }

    // Editor visualization
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;

        Gizmos.color = pathColor;

        // Draw lines between waypoints
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);

                // Draw arrow direction
                Vector3 direction = (waypoints[i + 1].position - waypoints[i].position).normalized;
                Vector3 midpoint = (waypoints[i].position + waypoints[i + 1].position) / 2f;
                DrawArrow(midpoint, direction);
            }
        }

        // Draw waypoint spheres
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] != null)
            {
                Gizmos.color = i == 0 ? Color.green : (i == waypoints.Length - 1 ? Color.red : pathColor);
                Gizmos.DrawWireSphere(waypoints[i].position, waypointGizmoSize);

#if UNITY_EDITOR
                if (showWaypointNumbers)
                {
                    UnityEditor.Handles.Label(waypoints[i].position, $"WP {i}");
                }
#endif
            }
        }
    }

    private void DrawArrow(Vector3 position, Vector3 direction)
    {
        float arrowSize = 0.2f;
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
        Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;

        Gizmos.DrawRay(position, right * arrowSize);
        Gizmos.DrawRay(position, left * arrowSize);
    }
}