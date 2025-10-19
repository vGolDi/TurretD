using UnityEngine;
using Photon.Pun;

/// <summary>
/// Generates passive gold income when player stands inside zone
/// Example: Gold mines, resource points
/// </summary>
[RequireComponent(typeof(Collider))]
public class GoldZone : MonoBehaviour
{
    [Header("Income Settings")]
    [SerializeField, Tooltip("Gold granted per tick")]
    private int goldPerTick = 5;

    [SerializeField, Tooltip("Time between gold grants (seconds)")]
    private float interval = 2f;

    [Header("Visual Feedback")]
    [SerializeField] private GameObject visualEffect; // Optional particle effect
    [SerializeField] private Color gizmoColor = Color.yellow;

    private bool playerInZone = false;
    private float timer = 0f;
    private PhotonView currentPlayerView; // Track which player is in zone

    private void Start()
    {
        // Ensure trigger is enabled
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Update()
    {
        if (playerInZone && currentPlayerView != null)
        {
            timer += Time.deltaTime;

            if (timer >= interval)
            {
                GrantGold();
                timer = 0f;
            }
        }
    }

    private void GrantGold()
    {
        // Only grant gold to local player
        if (currentPlayerView != null && currentPlayerView.IsMine)
        {
            PlayerGold playerGold = currentPlayerView.GetComponent<PlayerGold>();
            if (playerGold != null)
            {
                playerGold.AddGold(goldPerTick);
                Debug.Log($"[GoldZone] Granted {goldPerTick} gold");

                // TODO: Play coin pickup sound/VFX
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if it's a player
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && other.CompareTag("Player"))
        {
            // Only activate for local player
            if (pv.IsMine)
            {
                playerInZone = true;
                currentPlayerView = pv;
                timer = 0f;

                if (visualEffect != null)
                {
                    visualEffect.SetActive(true);
                }

                Debug.Log("[GoldZone] Player entered gold zone");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && pv == currentPlayerView)
        {
            playerInZone = false;
            currentPlayerView = null;

            if (visualEffect != null)
            {
                visualEffect.SetActive(false);
            }

            Debug.Log("[GoldZone] Player left gold zone");
        }
    }

    // Visualize zone in editor
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }
    }
}