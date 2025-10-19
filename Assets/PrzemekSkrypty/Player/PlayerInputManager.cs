using UnityEngine;
using Photon.Pun;

/// <summary>
/// Manages player input states (build mode, free mouse mode)
/// Controls cursor lock state
/// </summary>
public class PlayerInputManager : MonoBehaviour
{
    // Input states
    public bool IsInBuildMode { get; private set; } = false;
    public bool IsInFreeMouseMode { get; private set; } = false;

    // Static reference to local player's instance
    public static PlayerInputManager LocalInstance { get; private set; }

    private PhotonView photonView;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    private void Start()
    {
        // Set local instance reference
        if (photonView != null && photonView.IsMine)
        {
            LocalInstance = this;
            ExitBuildMode(); // Start with cursor locked
        }
    }

    private void Update()
    {
        if (!photonView.IsMine) return;

        // Toggle free mouse mode (hold Shift to unlock cursor)
        // Only works when NOT in build mode
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

    /// <summary>
    /// Enters build mode - unlocks cursor
    /// </summary>
    public void EnterBuildMode()
    {
        if (photonView == null || !photonView.IsMine) return;

        IsInBuildMode = true;
        UnlockCursor();

        Debug.Log("[InputManager] Entered build mode");
    }

    /// <summary>
    /// Exits build mode - locks cursor
    /// </summary>
    public void ExitBuildMode()
    {
        if (photonView == null || !photonView.IsMine) return;

        IsInBuildMode = false;
        LockCursor();
        ExitFreeMouseMode();

        Debug.Log("[InputManager] Exited build mode");
    }

    /// <summary>
    /// Enters free mouse mode (e.g., holding Shift)
    /// </summary>
    private void EnterFreeMouseMode()
    {
        IsInFreeMouseMode = true;
        UnlockCursor();
    }

    /// <summary>
    /// Exits free mouse mode
    /// </summary>
    private void ExitFreeMouseMode()
    {
        IsInFreeMouseMode = false;

        // Only lock cursor if not in build mode
        if (!IsInBuildMode)
        {
            LockCursor();
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        if (LocalInstance == this)
        {
            LocalInstance = null;
        }
    }
}