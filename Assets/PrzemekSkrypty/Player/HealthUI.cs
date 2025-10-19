using UnityEngine;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

public class HealthUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI myHealthText;
    [SerializeField] private TextMeshProUGUI enemyHealthText;

    [Header("Optional: Health Bars")]
    [SerializeField] private UnityEngine.UI.Slider myHealthBar;
    [SerializeField] private UnityEngine.UI.Slider enemyHealthBar;

    private PlayerHealth myHealth;
    private PlayerHealth enemyHealth;

    private void Start()
    {
        // Wait a bit for players to spawn
        Invoke(nameof(FindPlayers), 1f);
    }

    private void FindPlayers()
    {
        // Find all PlayerHealth components
        PlayerHealth[] allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

        foreach (PlayerHealth player in allPlayers)
        {
            PhotonView pv = player.GetPhotonView();
            if (pv != null && pv.IsMine)
            {
                // This is my player
                myHealth = player;
                myHealth.OnHealthChanged += UpdateMyHealth;
                UpdateMyHealth(myHealth.CurrentHealth, myHealth.MaxHealth);
            }
            else if (pv != null && !pv.IsMine)
            {
                // This is enemy player
                enemyHealth = player;
                enemyHealth.OnHealthChanged += UpdateEnemyHealth;
                UpdateEnemyHealth(enemyHealth.CurrentHealth, enemyHealth.MaxHealth);
            }
        }

        if (myHealth == null)
        {
            Debug.LogWarning("[HealthUI] Could not find local player health!");
        }

        if (enemyHealth == null)
        {
            Debug.LogWarning("[HealthUI] Could not find enemy player health!");
        }
    }

    /// <summary>
    /// Updates display of my health
    /// </summary>
    private void UpdateMyHealth(int current, int max)
    {
        if (myHealthText != null)
        {
            myHealthText.text = $"HP: {current}/{max}";

            // Color coding (optional)
            if (current <= max * 0.25f)
            {
                myHealthText.color = Color.red; // Low HP
            }
            else if (current <= max * 0.5f)
            {
                myHealthText.color = Color.yellow; // Medium HP
            }
            else
            {
                myHealthText.color = Color.green; // High HP
            }
        }

        if (myHealthBar != null)
        {
            myHealthBar.maxValue = max;
            myHealthBar.value = current;
        }
    }

    /// <summary>
    /// Updates display of enemy health
    /// </summary>
    private void UpdateEnemyHealth(int current, int max)
    {
        if (enemyHealthText != null)
        {
            PhotonView pv = enemyHealth?.GetPhotonView();
            string enemyName = pv != null ? pv.Owner.NickName : "Enemy";

            enemyHealthText.text = $"{enemyName}: {current}/{max}";
        }

        if (enemyHealthBar != null)
        {
            enemyHealthBar.maxValue = max;
            enemyHealthBar.value = current;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (myHealth != null)
        {
            myHealth.OnHealthChanged -= UpdateMyHealth;
        }

        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged -= UpdateEnemyHealth;
        }
    }
}
