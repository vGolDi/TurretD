using Photon.Pun;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField, Tooltip("Enemy health points")] private int maxHP = 100;
    private int currentHP;

    private HealthBar healthBar;

    [SerializeField] private int goldReward = 10;
    private bool killRewardGiven = false;

    void Start()
    {
        currentHP = maxHP;
        healthBar = GetComponentInChildren<HealthBar>();

        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHP);
        }
    }

    public void TakeDamage(int dmg, int attackerPhotonViewID = -1)
    {
        currentHP -= dmg;

        // Update healthbar
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHP);
        }

        if (currentHP <= 0 && !killRewardGiven)
        {
            killRewardGiven = true;
            Die(attackerPhotonViewID);
        }
    }

    private void Die(int killerPhotonViewID)
    {
        // Award gold to the player who killed this enemy
        if (killerPhotonViewID != -1)
        {
            PhotonView killerView = PhotonView.Find(killerPhotonViewID);
            if (killerView != null && killerView.IsMine)
            {
                PlayerGold playerGold = killerView.GetComponent<PlayerGold>();
                if (playerGold != null)
                {
                    playerGold.AddGold(goldReward);
                }
            }
        }

        // TODO: Death VFX/SFX
        Destroy(gameObject);
    }
}
