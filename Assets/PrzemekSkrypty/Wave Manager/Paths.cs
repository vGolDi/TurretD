using UnityEngine;

/// <summary>
/// Przechowuje sekwencj� punkt�w (waypoint�w) definiuj�cych �cie�k�.
/// Odpowiada r�wnie� za wizualizacj� tej �cie�ki w edytorze Unity.
/// </summary>
public class Paths : MonoBehaviour
{
    [Tooltip("Punkty, kt�re tworz� �cie�k�. Ich kolejno�� okre�la tras� przeciwnik�w.")]
    [SerializeField] private Transform[] waypoints;

    [Tooltip("Kolor �cie�ki w edytorze dla debugowania")]
    [SerializeField] private Color pathColor = Color.cyan;

    /// <summary>
    /// Zwraca waypoint o podanym indeksie.
    /// Zabezpiecza przed wyj�ciem poza zakres tablicy.
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
    /// Metoda OnDrawGizmos jest wywo�ywana przez Unity tylko w edytorze.
    /// S�u�y do rysowania pomocniczych wizualizacji na scenie.
    /// </summary>
    private void OnDrawGizmos()
    {
        // Sprawdzamy, czy mamy co rysowa�
        if (waypoints == null || waypoints.Length < 2)
        {
            return;
        }

        Gizmos.color = pathColor;

        // Rysujemy linie pomi�dzy kolejnymi waypointami
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            // Upewniamy si�, �e obiekty Transform nie zosta�y usuni�te
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }
        }
    }
}