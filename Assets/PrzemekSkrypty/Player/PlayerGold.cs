using UnityEngine;
using TMPro;
using Photon.Pun;

public class PlayerGold : MonoBehaviour
{
    [Header("Starting Settings")]
    [SerializeField, Tooltip("Gold amount at game start")]
    private int startingGold = 100;

    [Header("UI")]
    [SerializeField, Tooltip("UI text displaying current gold")]
    private TextMeshProUGUI goldText;

    private int currentGold;
    private PhotonView photonView;

    // Reference to LOCAL player's gold (for easy access)
    public static PlayerGold LocalInstance { get; private set; }


    private void Awake()
    {
        photonView = GetComponent<PhotonView>();

        // Only set LocalInstance if this is OUR player
        if (photonView != null && photonView.IsMine)
        {
            LocalInstance = this;
        }
    }

    private void Start()
    {
        currentGold = startingGold;
        UpdateUI();
    }

    public int GetGold() => currentGold;
    public bool HasEnough(int amount) => currentGold >= amount;

    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateUI();
        Debug.Log($"[PlayerGold] +{amount} gold. Total: {currentGold}");
    }

    public bool SpendGold(int amount)
    {
        if (!HasEnough(amount))
        {
            Debug.Log($"[PlayerGold] Not enough gold! Need {amount}, have {currentGold}");
            return false;
        }

        currentGold -= amount;
        UpdateUI();
        Debug.Log($"[PlayerGold] -{amount} gold. Remaining: {currentGold}");
        return true;
    }

    private void UpdateUI()
    {
        // Only update UI for local player
        if (photonView != null && photonView.IsMine && goldText != null)
        {
            goldText.text = $"Gold: {currentGold}";
        }
    }

    private void OnDestroy()
    {
        if (LocalInstance == this)
        {
            LocalInstance = null;
        }
    }
}