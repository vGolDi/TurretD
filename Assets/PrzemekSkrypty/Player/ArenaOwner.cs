using UnityEngine;
using Photon.Pun;

public class ArenaOwner : MonoBehaviour
{
    public PhotonView ownerPhotonView;
    public PlayerHealth ownerHealth;

    public void SetOwner(PhotonView owner)
    {
        ownerPhotonView = owner;
        ownerHealth = owner?.GetComponent<PlayerHealth>();

        Debug.Log($"[ArenaOwner] SetOwner called. PhotonView: {(owner != null ? "OK" : "NULL")}, PlayerHealth: {(ownerHealth != null ? "OK" : "NULL")}");

        if (ownerHealth == null && owner != null)
        {
            Debug.LogError($"[ArenaOwner] Owner PhotonView exists but has NO PlayerHealth component!");
        }
    }

    public PlayerHealth GetOwnerHealth()
    {
        if (ownerHealth == null)
        {
            Debug.LogWarning("[ArenaOwner] GetOwnerHealth() - ownerHealth is NULL!");
        }
        return ownerHealth;
    }
}
