using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
using ElementumDefense.Cards;      
using ElementumDefense.Progression; 

public class GameEndManager : MonoBehaviour
{
    [Header("End Game Panels")]
    [SerializeField] private GameObject endGamePanel;
    [SerializeField] private TextMeshProUGUI titleText; // VICTORY / DEFEAT
    [SerializeField] private Button returnToMenuButton;

    [Header("Progression UI (Casual & Ranked)")]
    [SerializeField] private GameObject progressionContainer; // Rodzic pasków
    [SerializeField] private TextMeshProUGUI levelText;       // "Lvl 5"
    [SerializeField] private Slider xpSlider;                 // Pasek XP
    [SerializeField] private TextMeshProUGUI xpGainText;      // "+500 XP"

    [Header("Ranked Specific UI")]
    [SerializeField] private GameObject rankedContainer;      // Poka¿ tylko w Ranked
    [SerializeField] private TextMeshProUGUI rankNameText;    // "SILVER"
    [SerializeField] private Slider eloSlider;                // Pasek postêpu w randze
    [SerializeField] private TextMeshProUGUI eloChangeText;   // "+25 ELO"

    [Header("Quests UI")]
    [SerializeField] private Transform questListContainer;
    [SerializeField] private GameObject questSlotPrefab;      // Prefab z QuestResultSlot

    [Header("Rewards Config")]
    [SerializeField] private int victoryXP = 500;
    [SerializeField] private int defeatXP = 100;
    [SerializeField] private int winElo = 25;
    [SerializeField] private int loseElo = -15;

    [Header("Settings")]
    [SerializeField] private string menuSceneName = "MainMenu";

    private bool gameEnded = false;

    private void Start()
    {
        if (endGamePanel != null) endGamePanel.SetActive(false);
        if (returnToMenuButton != null) returnToMenuButton.onClick.AddListener(ReturnToMenu);
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    public void ShowVictory()
    {
        if (gameEnded) return;
        EndGameSequence(true);
    }

    public void ShowDefeat()
    {
        if (gameEnded) return;
        EndGameSequence(false);
    }

    // =========================================================
    // CORE LOGIC
    // =========================================================

    private void EndGameSequence(bool isVictory)
    {
        gameEnded = true;

        // 1. ZamroŸ grê (Zapobiega utracie HP po koñcu)
        FreezeGameScene();

        // 2. Przyznaj nagrody (XP, ELO, Questy)
        ProcessRewards(isVictory, out int xpGained, out int eloChange);

        // 3. Wyœwietl UI
        ShowUI(isVictory, xpGained, eloChange);
    }

    private void FreezeGameScene()
    {
        // Pauzuje fizykê, animacje i Update w wiêkszoœci skryptów
        Time.timeScale = 0f;

        // Odblokuj kursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[GameEndManager] Game Frozen.");
    }

    private void ProcessRewards(bool isVictory, out int xpGained, out int eloChange)
    {
        xpGained = isVictory ? victoryXP : defeatXP;
        eloChange = 0;

        var player = PlayerCollection.Instance;
        var questManager = QuestManager.Instance;

        // -- XP --
        if (player != null)
        {
            player.AddXP(xpGained);
        }

        // -- QUESTS --
        if (questManager != null)
        {
            questManager.ReportProgress(QuestType.PlayGames, 1);
            if (isVictory)
            {
                questManager.ReportProgress(QuestType.WinGames, 1);
            }
        }

        // -- RANKED ELO --
        if (player != null && player.SelectedGameMode == GameMode.Ranked)
        {
            eloChange = isVictory ? winElo : loseElo;
            player.AddElo(eloChange);
        }
    }

    // =========================================================
    // UI DISPLAY
    // =========================================================

    private void ShowUI(bool isVictory, int xpGained, int eloChange)
    {
        if (endGamePanel != null) endGamePanel.SetActive(true);

        // 1. Tytu³
        if (titleText != null)
        {
            titleText.text = isVictory ? "VICTORY!" : "DEFEAT";
            titleText.color = isVictory ? Color.green : Color.red;
        }

        // 2. XP & Level (Zawsze widoczne)
        UpdateXPDisplay(xpGained);

        // 3. ELO (Tylko Ranked)
        var player = PlayerCollection.Instance;
        bool isRanked = player != null && player.SelectedGameMode == GameMode.Ranked;

        if (rankedContainer != null)
        {
            rankedContainer.SetActive(isRanked);
            if (isRanked)
            {
                UpdateEloDisplay(eloChange);
            }
        }

        // 4. Questy
        UpdateQuestDisplay();
    }

    private void UpdateXPDisplay(int xpGained)
    {
        var player = PlayerCollection.Instance;
        if (player == null) return;

        if (levelText != null)
            levelText.text = $"Lvl {player.GetLevel()}";

        if (xpGainText != null)
            xpGainText.text = $"+{xpGained} XP";

        if (xpSlider != null)
        {
            float current = player.GetCurrentXP();
            float max = player.GetXPForNextLevel();
            xpSlider.maxValue = max;
            xpSlider.value = current;
        }
    }

    private void UpdateEloDisplay(int eloChange)
    {
        var player = PlayerCollection.Instance;
        if (player == null) return;

        int currentElo = player.GetElo();

        if (rankNameText != null)
        {
            rankNameText.text = player.GetRankName();
            rankNameText.color = player.GetRankColor();
        }

        if (eloChangeText != null)
        {
            string sign = eloChange > 0 ? "+" : "";
            eloChangeText.text = $"{sign}{eloChange} ELO";
            eloChangeText.color = eloChange > 0 ? Color.green : Color.red;
        }

        // Obliczanie paska ELO w ramach rangi
        if (eloSlider != null)
        {
            // Pobieramy progi dla obecnego ELO
            (int min, int max) = GetRankRange(currentElo);

            eloSlider.minValue = min;
            eloSlider.maxValue = max;
            eloSlider.value = currentElo;
        }
    }

    private void UpdateQuestDisplay()
    {
        if (QuestManager.Instance == null || questListContainer == null || questSlotPrefab == null) return;

        // Wyczyœæ stare
        foreach (Transform child in questListContainer) Destroy(child.gameObject);

        // SprawdŸ tylko aktywne questy
        foreach (var quest in QuestManager.Instance.activeQuests)
        {
            // Poka¿ quest, jeœli jest nieskoñczony LUB w³aœnie zosta³ ukoñczony
            // (Dla uproszczenia pokazujemy wszystkie aktywne)
            GameObject slot = Instantiate(questSlotPrefab, questListContainer);
            slot.GetComponent<QuestResultSlot>().Setup(quest);
        }
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f; // Odkorkuj czas przed wyjœciem!
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
        SceneManager.LoadScene(menuSceneName);
    }

    // =========================================================
    // HELPER: RANK THRESHOLDS
    // =========================================================

    // Zwraca (minElo, maxElo) dla danej rangi, ¿eby pasek mia³ sens
    private (int, int) GetRankRange(int elo)
    {
        if (elo < 1200) return (0, 1200);       // Bronze
        if (elo < 1500) return (1200, 1500);    // Silver
        if (elo < 1800) return (1500, 1800);    // Gold
        if (elo < 2200) return (1800, 2200);    // Platinum
        return (2200, 3000);                    // Diamond (limit umowny)
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("DEBUG: Win")]
    public void DebugWin() => ShowVictory();

    [ContextMenu("DEBUG: Defeat")]
    public void DebugDefeat() => ShowDefeat();
}