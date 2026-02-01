//using UnityEngine;
//using UnityEngine.SceneManagement;
//using TMPro;
//using Photon.Pun;
//using ElementumDefense.Cards; // Dla PlayerCollection
//using ElementumDefense.Progression;

//public class GameEndManager : MonoBehaviour
//{
//    [Header("End Game UI")]
//    [SerializeField] private GameObject endGamePanel;
//    [SerializeField] private TextMeshProUGUI resultText;
//    [SerializeField] private UnityEngine.UI.Button returnToMenuButton;

//    [Header("Settings")]
//    [SerializeField] private string menuSceneName = "MainMenu";

//    [Header("Rewards")]
//    [SerializeField] private int victoryXP = 500;
//    [SerializeField] private int defeatXP = 100;

//    [Header("Ranked Rewards")]
//    [SerializeField] private int winElo = 25;
//    [SerializeField] private int loseElo = -15;

//    private bool gameEnded = false;

//    private void Start()
//    {
//        // Hide end game panel initially
//        if (endGamePanel != null)
//        {
//            endGamePanel.SetActive(false);
//        }

//        // Setup button
//        if (returnToMenuButton != null)
//        {
//            returnToMenuButton.onClick.AddListener(ReturnToMenu);
//        }
//    }

//    /// <summary>
//    /// Shows victory screen
//    /// </summary>
//    public void ShowVictory()
//    {
//        if (gameEnded) return;
//        gameEnded = true;

//        PlayerCollection.Instance?.AddXP(victoryXP);
//        CheckRankedResult(true);
//        // 2. Zaktualizuj Questy
//        if (QuestManager.Instance != null)
//        {
//            QuestManager.Instance.ReportProgress(QuestType.WinGames, 1);
//            QuestManager.Instance.ReportProgress(QuestType.PlayGames, 1);
//        }
//        Debug.Log("[GameEndManager] VICTORY!");

//        if (endGamePanel != null)
//        {
//            endGamePanel.SetActive(true);
//        }

//        if (resultText != null)
//        {
//            resultText.text = "VICTORY!";
//            resultText.color = Color.green;
//        }

//        // Pause game (optional)
//        // Time.timeScale = 0f;

//        // Show cursor
//        Cursor.lockState = CursorLockMode.None;
//        Cursor.visible = true;
//    }

//    /// <summary>
//    /// Shows defeat screen
//    /// </summary>
//    public void ShowDefeat()
//    {
//        if (gameEnded) return;
//        gameEnded = true;

//        // 1. Dodaj XP
//        PlayerCollection.Instance?.AddXP(defeatXP);
//        CheckRankedResult(false);
//        // 2. Zaktualizuj Questy (tylko Play, nie Win)
//        if (QuestManager.Instance != null)
//        {
//            QuestManager.Instance.ReportProgress(QuestType.PlayGames, 1);
//        }

//        Debug.Log("[GameEndManager] DEFEAT!");

//        if (endGamePanel != null)
//        {
//            endGamePanel.SetActive(true);
//        }

//        if (resultText != null)
//        {
//            resultText.text = "DEFEAT";
//            resultText.color = Color.red;
//        }

//        // Pause game (optional)
//        // Time.timeScale = 0f;

//        // Show cursor
//        Cursor.lockState = CursorLockMode.None;
//        Cursor.visible = true;
//    }

//    /// <summary>
//    /// Returns to main menu
//    /// </summary>
//    public void ReturnToMenu()
//    {
//        Debug.Log("[GameEndManager] Returning to menu...");

//        // Unpause game
//        Time.timeScale = 1f;

//        // Disconnect from Photon
//        if (PhotonNetwork.IsConnected)
//        {
//            PhotonNetwork.Disconnect();
//        }

//        // Load menu scene
//        SceneManager.LoadScene(menuSceneName);
//    }

//    private void CheckRankedResult(bool isVictory)
//    {
//        var player = ElementumDefense.Cards.PlayerCollection.Instance;

//        // Sprawdzamy czy to by³ mecz rankingowy
//        if (player != null && player.SelectedGameMode == ElementumDefense.Cards.GameMode.Ranked)
//        {
//            int eloChange = isVictory ? winElo : loseElo;
//            player.AddElo(eloChange);

//            if (resultText != null)
//            {
//                string sign = eloChange > 0 ? "+" : "";
//                // Dopisz informacjê o ELO do ekranu koñcowego (opcjonalnie)
//                resultText.text += $"\n<size=60%>{sign}{eloChange} ELO</size>";
//            }
//        }
//    }
//    // =========================================================
//    // DEBUG TOOLS (Prawy klik na komponent w Inspektorze)
//    // =========================================================

//    [ContextMenu("DEBUG: Symuluj Wygran¹ (Win)")]
//    public void DebugSimulateWin()
//    {
//        // Resetujemy flagê, ¿eby mo¿na by³o klikaæ wielokrotnie podczas testów
//        gameEnded = false;
//        ShowVictory();
//        Debug.Log("<color=green>[DEBUG] Wymuszono zwyciêstwo!</color>");
//    }

//    [ContextMenu("DEBUG: Symuluj Przegran¹ (Defeat)")]
//    public void DebugSimulateDefeat()
//    {
//        gameEnded = false;
//        ShowDefeat();
//        Debug.Log("<color=red>[DEBUG] Wymuszono pora¿kê!</color>");
//    }
//}
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;
using ElementumDefense.Cards;       // Dla PlayerCollection
using ElementumDefense.Progression; // Dla QuestManager

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