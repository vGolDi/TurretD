using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField, Tooltip("Punkty ¿ycia przeciwnika")] private int maxHP = 100;
    private int currentHP;

    private HealthBar healthBar;

    [SerializeField] private int goldReward = 10;
    private bool killRewardGiven = false;

    void Start()
    {
        currentHP = maxHP;
        healthBar = GetComponentInChildren<HealthBar>();
    }

    public void TakeDamage(int dmg, bool isPlayerOwned = false)
    {
        currentHP -= dmg;

        if (currentHP <= 0 && !killRewardGiven)
        {
            killRewardGiven = true;

            if (isPlayerOwned)
            {
                PlayerGold.Instance.AddGold(goldReward);
            }

            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
