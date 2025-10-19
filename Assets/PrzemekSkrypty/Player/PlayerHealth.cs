using UnityEngine;
using System;
using Photon.Pun;
using TMPro;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    //events
    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDied;

    public static PlayerHealth LocalInstance {  get; private set; }

    private PhotonView photonView;
    private bool isDead = false;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();

        if(photonView != null && photonView.IsMine)
        {
            LocalInstance = this;
        }
    }
    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"[PlayerHealth] {photonView.Owner.NickName} took {damage} damage. HP: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (photonView.IsMine)
        {
            photonView.RPC("RPC_SyncHealth", RpcTarget.OthersBuffered, currentHealth);
        }

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }
    
    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);

        Debug.Log($"[PlayerHealth] Healed {amount}. HP: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (photonView.IsMine)
        {
            photonView.RPC("RPC_SyncHealth", RpcTarget.OthersBuffered, currentHealth);
        }
    }

    [PunRPC]
    private void RPC_SyncHealth(int newHealth)
    {
        currentHealth = newHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"[PlayerHealth] {photonView.Owner.NickName} DIED!");

        OnPlayerDied?.Invoke();

        // Notify all players about death
        if (photonView.IsMine)
        {
            photonView.RPC("RPC_PlayerDied", RpcTarget.AllBuffered);
        }

        // Trigger game end
        GameEndManager gameEndManager = FindAnyObjectByType<GameEndManager>();
        if (gameEndManager != null)
        {
            if (photonView.IsMine)
            {
                // I died = I lost
                gameEndManager.ShowDefeat();
            }
            else
            {
                // Enemy died = I won
                gameEndManager.ShowVictory();
            }
        }
    }

    [PunRPC]
    private void RPC_PlayerDied()
    {
        isDead = true;
        OnPlayerDied?.Invoke();
    }

    public PhotonView GetPhotonView()
    {
        return photonView;
    }

    private void OnDestroy()
    {
        if (LocalInstance == this)
        {
            LocalInstance = null;
        }
    }
}
