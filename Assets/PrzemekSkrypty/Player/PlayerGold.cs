using UnityEngine;
using UnityEngine.UIElements;
using Photon.Pun;


namespace ElementumDefense.Players
{
public class PlayerGold : MonoBehaviour
{
    [Header("Starting Settings")]
    [SerializeField]
    private int startingGold = 100;

    private int currentGold;
    private PhotonView photonView;

    public static PlayerGold LocalInstance
    { get; private set; }

    /// <summary>
    /// Reconnect restore: overwrite the in-match gold balance directly.
    /// Bypasses earn/spend so the snapshot value is reproduced exactly.
    /// </summary>
    public void RestoreGold(int amount)
    {
        currentGold = Mathf.Max(0, amount);
        UpdateUI();
    }

    // UI Toolkit � na TYM SAMYM PREFABIE
    private UIDocument uiDocument;
    private Label goldValueLabel;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();

        if (photonView != null && photonView.IsMine)
            LocalInstance = this;
    }

    private void Start()
    {
        currentGold = startingGold;

        if (photonView != null && !photonView.IsMine)
        {
            // Nie nasz gracz � ukryj gold UI
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument != null)
                uiDocument.enabled = false;
            return;
        }

        FindGoldLabel();
        UpdateUI();
    }

    private void FindGoldLabel()
    {
        // Szukaj UIDocument na W�ASNYM GameObject
        uiDocument = GetComponent<UIDocument>();

        if (uiDocument != null &&
            uiDocument.rootVisualElement != null)
        {
            goldValueLabel =
                uiDocument.rootVisualElement
                    .Q<Label>("gold-value");

            if (goldValueLabel != null)
            {
                Debug.Log(
                    "[PlayerGold] Gold label found " +
                    "on own prefab");
            }
        }

        // Fallback: szukaj w scenie
        if (goldValueLabel == null)
        {
            var docs = FindObjectsByType<UIDocument>(
                FindObjectsSortMode.None);

            foreach (var doc in docs)
            {
                if (doc == uiDocument) continue;
                if (doc.rootVisualElement == null)
                    continue;

                var label =
                    doc.rootVisualElement
                        .Q<Label>("gold-value");

                if (label != null)
                {
                    goldValueLabel = label;
                    Debug.Log(
                        "[PlayerGold] Gold label " +
                        "found in scene");
                    break;
                }
            }
        }
    }

    public int GetGold() => currentGold;

    public bool HasEnough(int amount) =>
        currentGold >= amount;

    public void AddGold(int amount)
    {
        currentGold += amount;
        UpdateUI();
        Debug.Log(
            $"[PlayerGold] +{amount}. " +
            $"Total: {currentGold}");
    }

    public bool SpendGold(int amount)
    {
        if (!HasEnough(amount))
        {
            Debug.Log(
                $"[PlayerGold] Not enough! " +
                $"Need {amount}, " +
                $"have {currentGold}");
            return false;
        }

        currentGold -= amount;
        UpdateUI();
        Debug.Log(
            $"[PlayerGold] -{amount}. " +
            $"Remaining: {currentGold}");
        return true;
    }

    private void UpdateUI()
    {
        if (photonView != null && !photonView.IsMine)
            return;

        if (goldValueLabel == null)
            FindGoldLabel();

        if (goldValueLabel != null)
            goldValueLabel.text =
                currentGold.ToString();
    }

    private void OnDestroy()
    {
        if (LocalInstance == this)
            LocalInstance = null;
    }
}
}
