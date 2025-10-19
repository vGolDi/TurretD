using UnityEngine;
using Photon.Pun;

/// <summary>
/// Disables entire camera rig (including Cinemachine Virtual Cameras) for non-local players
/// Attach to PlayerCameraRoot GameObject (parent of all cameras)
/// </summary>
public class DisableCameraForRemote : MonoBehaviour
{
    private void Start()
    {
        // Find PhotonView in parent (Player root)
        PhotonView pv = GetComponentInParent<PhotonView>();

        if (pv == null)
        {
            Debug.LogError("[DisableCameraForRemote] No PhotonView found in parent!");
            return;
        }

        // If this is NOT local player, disable entire camera rig
        if (!pv.IsMine)
        {
            Debug.Log($"[DisableCameraForRemote] Disabling camera rig for remote player (ViewID: {pv.ViewID})");

            // Disable entire GameObject (PlayerCameraRoot)
            // This disables MainCamera, PlayerFollowCamera, and all children
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log($"[DisableCameraForRemote] This is MY camera rig - keeping enabled (ViewID: {pv.ViewID})");
        }
    }
}