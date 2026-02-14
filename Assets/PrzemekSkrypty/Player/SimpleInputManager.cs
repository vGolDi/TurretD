using UnityEngine;
using Photon.Pun;

/// <summary>
/// Simplified input manager for Tower Defense.
/// Cursor is ALWAYS visible - no lock states.
/// Only tracks build mode for other systems.
/// </summary>
public class SimpleInputManager : MonoBehaviour
{
    public bool IsInBuildMode { get; private set; } = false;

    public static SimpleInputManager LocalInstance { get; private set; }

    private PhotonView photonView;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    private void Start()
    {
        if (photonView != null && photonView.IsMine)
        {
            LocalInstance = this;

            // Cursor ALWAYS visible in this game
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
    }

    public void EnterBuildMode()
    {
        if (photonView == null || !photonView.IsMine) return;
        IsInBuildMode = true;
        Debug.Log("[SimpleInputManager] Entered build mode");
    }

    public void ExitBuildMode()
    {
        if (photonView == null || !photonView.IsMine) return;
        IsInBuildMode = false;
        Debug.Log("[SimpleInputManager] Exited build mode");
    }

    private void OnDestroy()
    {
        if (LocalInstance == this)
        {
            LocalInstance = null;
        }
    }
}