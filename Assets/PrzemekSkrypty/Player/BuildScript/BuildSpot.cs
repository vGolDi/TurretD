using UnityEngine;

/// <summary>
/// Marks valid turret placement locations
/// TODO: Implement restricted build zones (only on BuildSpots)
/// TODO: Add snap-to-grid functionality
/// </summary>
public class BuildSpot : MonoBehaviour
{
    [Header("Build Spot Settings")]
    [SerializeField] private bool isOccupied = false;
    [SerializeField] private GameObject currentTurret;

    // Visual feedback
    [SerializeField] private Material availableMaterial;
    [SerializeField] private Material occupiedMaterial;
    private Renderer spotRenderer;

    private void Start()
    {
        spotRenderer = GetComponent<Renderer>();
        UpdateVisual();
    }

    public bool IsAvailable()
    {
        return !isOccupied;
    }

    public void Occupy(GameObject turret)
    {
        isOccupied = true;
        currentTurret = turret;
        UpdateVisual();
    }

    public void Free()
    {
        isOccupied = false;
        currentTurret = null;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (spotRenderer != null)
        {
            spotRenderer.material = isOccupied ? occupiedMaterial : availableMaterial;
        }
    }

    // Show placement grid in editor
    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
}