using UnityEngine;
using TMPro;

public class PlayerGold : MonoBehaviour
{
    public static PlayerGold Instance { get; private set; }

    [Header("Startowe ustawienia")]
    [Tooltip("Iloœæ z³ota na start gry")]
    [SerializeField] private int startingGold = 100;

    [Header("UI")]
    [Tooltip("Referencja do tekstu UI pokazuj¹cego z³oto")]
    [SerializeField] private TextMeshProUGUI goldText;

    private int currentGold;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
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
    }

    public bool SpendGold(int amount)
    {
        if (!HasEnough(amount))
            return false;

        currentGold -= amount;
        UpdateUI();
        return true;
    }

    private void UpdateUI()
    {
        if (goldText != null)
            goldText.text = $"Gold: {currentGold}";
    }
}