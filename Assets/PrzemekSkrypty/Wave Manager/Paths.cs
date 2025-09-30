using UnityEngine;

/// <summary>
/// Przechowuje sekwencjê punktów (waypointów) definiuj¹cych œcie¿kê.
/// Odpowiada równie¿ za wizualizacjê tej œcie¿ki w edytorze Unity.
/// </summary>
public class Paths : MonoBehaviour
{
    [Tooltip("Punkty, które tworz¹ œcie¿kê. Ich kolejnoœæ okreœla trasê przeciwników.")]
    [SerializeField] private Transform[] waypoints;

    [Tooltip("Kolor œcie¿ki w edytorze dla debugowania")]
    [SerializeField] private Color pathColor = Color.cyan;

    /// <summary>
    /// Zwraca waypoint o podanym indeksie.
    /// Zabezpiecza przed wyjœciem poza zakres tablicy.
    /// </summary>

    public Transform GetWaypoint(int index)
    {
        if (waypoints == null || waypoints.Length == 0)
            return null;

        if (index < 0 || index >= waypoints.Length)
            return null;

        return waypoints[index];
    }


    /// <summary>
    /// Metoda OnDrawGizmos jest wywo³ywana przez Unity tylko w edytorze.
    /// S³u¿y do rysowania pomocniczych wizualizacji na scenie.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Sprawdzamy, czy mamy co rysowaæ
        if (waypoints == null || waypoints.Length < 2)
        {
            return;
        }

        Gizmos.color = pathColor;

        // Rysujemy linie pomiêdzy kolejnymi waypointami
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            // Upewniamy siê, ¿e obiekty Transform nie zosta³y usuniête
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
    }
}