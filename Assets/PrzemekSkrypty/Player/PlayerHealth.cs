using UnityEngine;
using System;
using Photon.Pun;
using TMPro;


namespace ElementumDefense.Players
{
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDied;

    /// <summary>
    /// Fired the moment current HP would hit 0, BEFORE death is committed
    /// and BEFORE the death RPC is broadcast. Subscribers (e.g. PhoenixHeartGuard)
    /// may heal the player here to cancel the death.
    /// </summary>
    public event Action OnLethalDamage;

    public static PlayerHealth LocalInstance { get; private set; }

    private PhotonView photonView;
    private bool isDead = false;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    //private void Awake()
    //{
    //    photonView = GetComponent<PhotonView>();
    //    Debug.Log($"========== PLAYERHEALTH AWAKE ==========");
    //    Debug.Log($"[PlayerHealth] GameObject: {gameObject.name}");
    //    Debug.Log($"[PlayerHealth] GameObject.activeInHierarchy: {gameObject.activeInHierarchy}");
    //    Debug.Log($"[PlayerHealth] Component enabled: {enabled}");
    //    // ========== ZMIENIONE: Dodaj walidację ==========
    //    if (photonView == null)
    //    {
    //        Debug.LogError($"[PlayerHealth] No PhotonView on {gameObject.name}!");
    //        return;
    //    }

    //    Debug.Log($"[PlayerHealth] Awake - ViewID: {photonView.ViewID}, IsMine: {photonView.IsMine}, Owner: {photonView.Owner?.NickName}");

    //    if (photonView.IsMine)
    //    {
    //        LocalInstance = this;
    //        Debug.Log($"[PlayerHealth] Set as LocalInstance");
    //    }
    //    // ================================================
    //}
    private void Awake()
    {
        Debug.Log($"========== PLAYERHEALTH AWAKE ==========");
        Debug.Log($"[PlayerHealth] GameObject: {gameObject.name}");
        Debug.Log($"[PlayerHealth] GameObject path: {GetFullPath(transform)}");

        photonView = GetComponent<PhotonView>();

        if (photonView == null)
        {
            Debug.LogError($"[PlayerHealth] No PhotonView on {gameObject.name}!");
            return;
        }

        Debug.Log($"[PlayerHealth] ViewID: {photonView.ViewID}, IsMine: {photonView.IsMine}, Owner: {photonView.Owner?.NickName}");

        if (photonView.IsMine)
        {
            LocalInstance = this;
            Debug.Log($"[PlayerHealth] Set as LocalInstance");
        }
    }

    // Helper method
    private string GetFullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // ========== NOWE: Notify HealthUI that player spawned ==========
        //HealthUI healthUI = FindAnyObjectByType<HealthUI>();
        //if (healthUI != null)
        //{
        //    healthUI.OnPlayerSpawned(this);
        //    Debug.Log($"[PlayerHealth] Notified HealthUI about spawn");
        //}
        //// ===============================================================
    }

    public void TakeDamage(int damage)
    {
        // ========== DODAJ: Sprawdź czy PhotonView jest OK ==========
        if (photonView == null || photonView.ViewID == 0)
        {
            Debug.LogError("[PlayerHealth] Invalid PhotonView - cannot sync damage!");
            return;
        }
        // ===========================================================

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"[PlayerHealth] {photonView.Owner.NickName} took {damage} damage. HP: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        // ===== PhoenixHeart hook =====
        // If a listener (PhoenixHeartGuard) heals us back above 0 inside this
        // event, the Die() check below will skip and the OthersBuffered RPC
        // below will sync the post-revive HP to the remote. No desync.
        if (currentHealth <= 0 && !isDead)
        {
            OnLethalDamage?.Invoke();
        }

        if (photonView.IsMine)
        {
            Debug.Log($"[PlayerHealth] Syncing health to others via RPC (ViewID: {photonView.ViewID})");

            // ========== ZMIENIONE: Sprawdź ViewID przed RPC ==========
            if (photonView.ViewID != 0)
            {
                photonView.RPC("RPC_SyncHealth", RpcTarget.OthersBuffered, currentHealth);
            }
            else
            {
                Debug.LogError("[PlayerHealth] ViewID is 0 - cannot send RPC!");
            }
            // =========================================================
        }

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        // ========== DODAJ: Walidacja PhotonView ==========
        if (photonView == null || photonView.ViewID == 0)
        {
            Debug.LogError("[PlayerHealth] Invalid PhotonView - cannot sync heal!");
            return;
        }
        // =================================================

        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);

        Debug.Log($"[PlayerHealth] Healed {amount}. HP: {currentHealth}/{maxHealth}");

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (photonView.IsMine)
        {
            if (photonView.ViewID != 0)
            {
                photonView.RPC("RPC_SyncHealth", RpcTarget.OthersBuffered, currentHealth);
            }
        }
    }

    [PunRPC]
    private void RPC_SyncHealth(int newHealth)
    {
        Debug.Log($"[PlayerHealth] RPC_SyncHealth received: {newHealth}");

        currentHealth = newHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    /// <summary>
    /// Reconnect restore: set HP to the snapshot value and re-broadcast it so
    /// the remote peer's tracked value matches. Does not trigger death unless
    /// the restored value is lethal.
    /// </summary>
    public void RestoreHealth(int hp)
    {
        currentHealth = Mathf.Clamp(hp, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (photonView != null && photonView.IsMine && photonView.ViewID != 0)
        {
            photonView.RPC("RPC_SyncHealth", RpcTarget.OthersBuffered, currentHealth);
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"[PlayerHealth] {photonView.Owner.NickName} DIED!");

        OnPlayerDied?.Invoke();

        if (photonView.IsMine && photonView.ViewID != 0)
        {
            photonView.RPC("RPC_PlayerDied", RpcTarget.AllBuffered);

            // Reconnect-robust win signal: publish the dead actor as a room
            // property. The opponent's MatchOpponentWatcher reads it and wins,
            // even if our PhotonView changed/duplicated after a reconnect (which
            // can make the RPC above miss its target).
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.LocalPlayer != null)
            {
                var props = new ExitGames.Client.Photon.Hashtable
                {
                    { ElementumDefense.Multiplayer.MatchOpponentWatcher.DEAD_ACTOR_KEY,
                      PhotonNetwork.LocalPlayer.ActorNumber }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
            }
        }

        GameEndManager gameEndManager = FindAnyObjectByType<GameEndManager>();
        if (gameEndManager != null)
        {
            if (photonView.IsMine)
            {
                gameEndManager.ShowDefeat();
            }
            else
            {
                gameEndManager.ShowVictory();
            }
        }
    }

    [PunRPC]
    private void RPC_PlayerDied()
    {
        Debug.Log($"[PlayerHealth] RPC_PlayerDied received");
        isDead = true;
        OnPlayerDied?.Invoke();
    }

    public PhotonView GetPhotonView()
    {
        return photonView;
    }

    private void OnDisable()
    {
        Debug.Log($"========== PLAYERHEALTH ONDISABLE ==========");
        Debug.Log($"[PlayerHealth] GameObject: {gameObject.name}");
        Debug.Log($"[PlayerHealth] ViewID: {photonView?.ViewID}");
        Debug.Log($"[PlayerHealth] IsMine: {photonView?.IsMine}");
        Debug.Log($"[PlayerHealth] ⚠️ DISABLED BY:");

        System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
        Debug.Log(stackTrace.ToString());
    }

    private void OnDestroy()
    {
        Debug.Log($"========== PLAYERHEALTH ONDESTROY ==========");
        Debug.Log($"[PlayerHealth] ViewID: {photonView?.ViewID}");
        Debug.Log($"[PlayerHealth] IsMine: {photonView?.IsMine}");
        Debug.Log($"[PlayerHealth] 💀 DESTROYED BY:");

        System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(true);
        Debug.Log(stackTrace.ToString());

        if (LocalInstance == this)
        {
            LocalInstance = null;
        }
    }
}
}
