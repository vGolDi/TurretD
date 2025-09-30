using UnityEngine;
using Photon.Pun;

public class TurretInteract : MonoBehaviour
{
    [Header("Ustawienia")]
    [Tooltip("Czy UI ma si� pokazywa� automatycznie, gdy gracz podejdzie?")]
    [SerializeField] private bool showUiOnProximity = true;

    private Turret turret;
    private TurretUiController turretUI;
    private bool playerIsInRange = false;

    private void Awake()
    {
        turret = GetComponent<Turret>();
        
    }

    // Nowa metoda, kt�r� Turret wywo�uje, aby "pod��czy�" UI
    public void LinkUiController(TurretUiController newUiController)
    {
        turretUI = newUiController;
        turretUI.LinkTurret(turret); 

        if (playerIsInRange && showUiOnProximity)
        {
            Show();
        }
        else
        {
            Hide(); 
        }
    }

    public void OnClicked()
    {
        if (PlayerInputManager.LocalInstance != null && PlayerInputManager.LocalInstance.IsInBuildMode)
        {
            return;
        }

        // Prze��cz widoczno�� UI
        if (turretUI != null && turretUI.IsVisible())
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInRange = true;
            if (showUiOnProximity)
            {
                Show();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerIsInRange = false;
            Hide();
        }
    }

    private void Show()
    {
        turretUI?.Show();
    }

    private void Hide()
    {
        turretUI?.Hide();
    }
}