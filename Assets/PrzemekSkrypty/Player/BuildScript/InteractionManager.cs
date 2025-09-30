using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    [SerializeField] private LayerMask turretLayer;
    private Camera cam;
    private BuildManager buildManager;
    private void Awake()
    {
        buildManager = GetComponent<BuildManager>();
    }

    private void Start()
    {
        cam = Camera.main;
    }

    private void Update()
    {
        // Sprawdzaj klikni�cia tylko, je�li NIE jeste�my w trybie budowania
        if (buildManager != null && !buildManager.IsInBuildMode())
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, turretLayer))
                {
                    // Sprawd�, czy klikni�ty obiekt ma komponent interakcji
                    var interactable = hit.collider.GetComponentInParent<TurretInteract>();
                    if (interactable != null)
                    {
                        interactable.OnClicked();
                    }
                }
            }
        }
    }
}
// Metoda pomocnicza, kt�r� trzeba doda� do BuildManager.cs
// public bool IsInBuildMode() => isInBuildMode;

