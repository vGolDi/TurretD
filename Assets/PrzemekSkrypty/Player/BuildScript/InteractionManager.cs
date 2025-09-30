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
        // Sprawdzaj klikniêcia tylko, jeœli NIE jesteœmy w trybie budowania
        if (buildManager != null && !buildManager.IsInBuildMode())
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f, turretLayer))
                {
                    // SprawdŸ, czy klikniêty obiekt ma komponent interakcji
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
// Metoda pomocnicza, któr¹ trzeba dodaæ do BuildManager.cs
// public bool IsInBuildMode() => isInBuildMode;

