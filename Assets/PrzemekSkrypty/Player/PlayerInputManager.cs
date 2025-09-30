using UnityEngine;
using Photon.Pun;

public class PlayerInputManager : MonoBehaviour
{
    public bool IsInBuildMode { get; private set; } = false;
    public bool IsInFreeMouseMode { get; private set; } = false;
    public static PlayerInputManager LocalInstance { get; private set; }
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
            ExitBuildMode();
        }
    }
    private void Update()
    {
        if (!photonView.IsMine) return;

        // Prze³¹czaj tryb wolnej myszy, ale tylko gdy NIE jesteœ w trybie budowy
        if (!IsInBuildMode)
        {
            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                EnterFreeMouseMode();
            }
            if (Input.GetKeyUp(KeyCode.LeftShift))
            {
                ExitFreeMouseMode();
            }
        }
    }
    public void EnterBuildMode()
    {
        if (photonView == null || !photonView.IsMine) return;
        IsInBuildMode = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitBuildMode()
    {
        if (photonView == null || !photonView.IsMine) return;
        IsInBuildMode = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        ExitFreeMouseMode();
    }
    private void EnterFreeMouseMode()
    {
        IsInFreeMouseMode = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ExitFreeMouseMode()
    {
        IsInFreeMouseMode = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}