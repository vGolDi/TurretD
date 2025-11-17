using UnityEngine;
using TMPro;
using Photon.Pun;

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

    private bool isInitialized = false;
    private float retryTimer = 0f;
    private const float RETRY_INTERVAL = 1f;

    private void Start()
    {
        Debug.Log("[HealthUI] Starting...");
    }

    private void Update()
    {
        if (!isInitialized)
        {
            retryTimer += Time.deltaTime;

            if (retryTimer >= RETRY_INTERVAL)
            {
                retryTimer = 0f;
                TryFindPlayers();
            }
        }
    }

    private void TryFindPlayers()
    {
        // ========== KLUCZOWA ZMIANA: FindObjectsByType zamiast czekania na event ==========
        PlayerHealth[] allPlayers = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);

        Debug.Log($"[HealthUI] ========== SEARCHING FOR PLAYERS ==========");
        Debug.Log($"[HealthUI] Total PlayerHealth found: {allPlayers.Length}");
        Debug.Log($"[HealthUI] PhotonNetwork.InRoom: {PhotonNetwork.InRoom}");
        Debug.Log($"[HealthUI] Room PlayerCount: {(PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.PlayerCount : 0)}");

        int foundMy = 0;
        int foundEnemy = 0;

        // Debug: Pokaż WSZYSTKIE znalezione PlayerHealth
        for (int i = 0; i < allPlayers.Length; i++)
        {
            PlayerHealth p = allPlayers[i];
            PhotonView pv = p.GetPhotonView();

            Debug.Log($"[HealthUI] Player[{i}]: {p.gameObject.name}");
            Debug.Log($"  - Scene: {p.gameObject.scene.name}");
            Debug.Log($"  - Scene path: {p.gameObject.scene.path}");
            Debug.Log($"  - PhotonView: {(pv != null ? "YES" : "NO")}");

            if (pv != null)
            {
                Debug.Log($"  - ViewID: {pv.ViewID}");
                Debug.Log($"  - IsMine: {pv.IsMine}");
                Debug.Log($"  - Owner: {(pv.Owner != null ? pv.Owner.NickName : "NULL")}");
                Debug.Log($"  - OwnerActorNr: {pv.OwnerActorNr}");
            }
        }

        // Przypisz graczy
        foreach (PlayerHealth player in allPlayers)
        {
            PhotonView pv = player.GetPhotonView();

            if (pv == null)
            {
                Debug.LogWarning($"[HealthUI] {player.gameObject.name} has no PhotonView - skipping");
                continue;
            }

            if (pv.ViewID == 0)
            {
                Debug.LogWarning($"[HealthUI] {player.gameObject.name} has ViewID=0 - skipping");
                continue;
            }

            if (pv.IsMine)
            {
                if (myHealth != player)
                {
                    // Unsubscribe from old
                    if (myHealth != null)
                    {
                        myHealth.OnHealthChanged -= UpdateMyHealth;
                    }

                    myHealth = player;
                    myHealth.OnHealthChanged += UpdateMyHealth;
                    UpdateMyHealth(myHealth.CurrentHealth, myHealth.MaxHealth);
                    foundMy++;
                    Debug.Log($"[HealthUI] ✅ MY player: {player.gameObject.name} (ViewID: {pv.ViewID})");
                }
            }
            else
            {
                if (enemyHealth != player)
                {
                    // Unsubscribe from old
                    if (enemyHealth != null)
                    {
                        enemyHealth.OnHealthChanged -= UpdateEnemyHealth;
                    }

                    enemyHealth = player;
                    enemyHealth.OnHealthChanged += UpdateEnemyHealth;
                    UpdateEnemyHealth(enemyHealth.CurrentHealth, enemyHealth.MaxHealth);
                    foundEnemy++;
                    Debug.Log($"[HealthUI] ✅ ENEMY player: {player.gameObject.name} (ViewID: {pv.ViewID})");
                }
            }
        }

        // Sprawdź czy zakończono inicjalizację
        if (myHealth != null && enemyHealth != null)
        {
            isInitialized = true;
            Debug.Log("[HealthUI] ✅✅✅ Both players found and initialized!");
        }
        else if (myHealth != null)
        {
            int totalPlayers = PhotonNetwork.CurrentRoom != null
                ? PhotonNetwork.CurrentRoom.PlayerCount
                : 1;

            if (totalPlayers == 1)
            {
                // Single player
                isInitialized = true;
                Debug.Log("[HealthUI] ✅ Single player mode");

                if (enemyHealthText != null) enemyHealthText.gameObject.SetActive(false);
                if (enemyHealthBar != null) enemyHealthBar.gameObject.SetActive(false);
            }
            else if (allPlayers.Length >= totalPlayers)
            {
                // Wszyscy gracze są, ale enemy nie znaleziony
                Debug.LogError("[HealthUI] ❌ All players in room but enemy not found!");
                Debug.LogError($"[HealthUI] My player PhotonView.IsMine might be broken!");
            }
            else
            {
                Debug.LogWarning($"[HealthUI] Waiting... Found {allPlayers.Length}/{totalPlayers} players");
            }
        }
        else
        {
            Debug.LogWarning($"[HealthUI] Still searching... (My: {myHealth != null}, Enemy: {enemyHealth != null})");
        }
    }

    private void UpdateMyHealth(int current, int max)
    {
        if (myHealthText != null)
        {
            myHealthText.text = $"HP: {current}/{max}";

            if (current <= max * 0.25f)
                myHealthText.color = Color.red;
            else if (current <= max * 0.5f)
                myHealthText.color = Color.yellow;
            else
                myHealthText.color = Color.green;
        }

        if (myHealthBar != null)
        {
            myHealthBar.maxValue = max;
            myHealthBar.value = current;
        }
    }

    private void UpdateEnemyHealth(int current, int max)
    {
        if (enemyHealthText != null)
        {
            PhotonView pv = enemyHealth?.GetPhotonView();
            string enemyName = pv != null && pv.Owner != null ? pv.Owner.NickName : "Enemy";

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