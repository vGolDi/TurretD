using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using StarterAssets;

/// <summary>
/// Disables PlayerInput component for non-local players
/// Attach to Player root (same GameObject as PlayerInput)
/// </summary>
public class DisableInputForRemote : MonoBehaviour
{
    private void Start()
    {
        PhotonView pv = GetComponent<PhotonView>();

        if (pv != null && !pv.IsMine)
        {
            // Disable PlayerInput component for remote players
            PlayerInput playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                playerInput.enabled = false;
                Debug.Log($"[DisableInputForRemote] Disabled PlayerInput for remote player (ViewID: {pv.ViewID})");
            }

            // Also disable ThirdPersonController
            var controller = GetComponent<ThirdPersonController>();
            if (controller != null)
            {
                controller.enabled = false;
                Debug.Log($"[DisableInputForRemote] Disabled ThirdPersonController for remote player");
            }
        }
        else
        {
            Debug.Log($"[DisableInputForRemote] This is MY player - keeping input enabled");
        }
    }
}