using UnityEngine;
using Photon.Pun;

public class TurretInteract : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Tooltip("Show UI automatically when player is nearby?")]
    private bool showUiOnProximity = true;

    [SerializeField, Tooltip("Detection radius for proximity trigger")]
    private float proximityRadius = 3f;

    private Turret turret;
    private TurretUiController turretUI;
    private bool playerIsInRange = false;


    private void Awake()
    {
        turret = GetComponent<Turret>();
        
    }

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
        PhotonView ownerView = turret?.GetOwner();
        if (ownerView != null && !ownerView.IsMine)
        {
            Debug.Log("[TurretInteract] This turret belongs to another player!");
            return;
        }

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
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine && other.CompareTag("Player"))
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
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine && other.CompareTag("Player"))
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
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, proximityRadius);
    }
}