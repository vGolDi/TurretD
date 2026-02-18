using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using ElementumDefense.Cards;
using ElementumDefense.Progression;
using ElementumDefense.UI;

public class GameEndManager : MonoBehaviour
{
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
        // Subscribe to UI return button
        if (GameEndPanelUI.Instance != null)
        {
            GameEndPanelUI.Instance.OnReturnToMenu +=
                ReturnToMenu;
        }
    }

    private void OnEnable()
    {
        // Retry subscription if panel
        // spawns after us
        StartCoroutine(LateSubscribe());
    }

    private System.Collections.IEnumerator
        LateSubscribe()
    {
        yield return null;
        yield return null;

        if (GameEndPanelUI.Instance != null)
        {
            GameEndPanelUI.Instance.OnReturnToMenu -=
                ReturnToMenu;
            GameEndPanelUI.Instance.OnReturnToMenu +=
                ReturnToMenu;
        }
    }

    private void OnDestroy()
    {
        if (GameEndPanelUI.Instance != null)
        {
            GameEndPanelUI.Instance.OnReturnToMenu -=
                ReturnToMenu;
        }
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

        FreezeGameScene();

        ProcessRewards(
            isVictory,
            out int xpGained,
            out int eloChange);

        ShowUI(isVictory, xpGained, eloChange);
    }

    private void FreezeGameScene()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("[GameEndManager] Game Frozen.");
    }

    private void ProcessRewards(
        bool isVictory,
        out int xpGained,
        out int eloChange)
    {
        xpGained = isVictory ? victoryXP : defeatXP;
        eloChange = 0;

        var player = PlayerCollection.Instance;
        var questManager = QuestManager.Instance;

        // XP
        if (player != null)
            player.AddXP(xpGained);

        // Quests
        if (questManager != null)
        {
            questManager.ReportProgress(
                QuestType.PlayGames, 1);
            if (isVictory)
                questManager.ReportProgress(
                    QuestType.WinGames, 1);
        }

        // Ranked ELO
        if (player != null &&
            player.SelectedGameMode ==
                GameMode.Ranked)
        {
            eloChange = isVictory
                ? winElo : loseElo;
            player.AddElo(eloChange);
        }
    }

    // =========================================================
    // UI DISPLAY
    // =========================================================

    private void ShowUI(
        bool isVictory,
        int xpGained,
        int eloChange)
    {
        var panel = GameEndPanelUI.Instance;
        if (panel == null)
        {
            Debug.LogError(
                "[GameEndManager] " +
                "GameEndPanelUI not found!");
            return;
        }

        var player = PlayerCollection.Instance;
        bool isRanked = player != null &&
            player.SelectedGameMode ==
                GameMode.Ranked;

        // Gather data
        int level = player != null
            ? player.GetLevel() : 1;
        float xpCurrent = player != null
            ? player.GetCurrentXP() : 0;
        float xpMax = player != null
            ? player.GetXPForNextLevel() : 1000;

        if (isRanked)
        {
            int currentElo = player != null
                ? player.GetElo() : 1000;
            string rankNameStr = player != null
                ? player.GetRankName() : "UNRANKED";
            Color rankColor = player != null
                ? player.GetRankColor()
                : Color.gray;

            (int rankMin, int rankMax) =
                GetRankRange(currentElo);

            if (isVictory)
            {
                panel.ShowVictoryRanked(
                    xpGained, level,
                    xpCurrent, xpMax,
                    eloChange, currentElo,
                    rankNameStr, rankColor,
                    rankMin, rankMax);
            }
            else
            {
                panel.ShowDefeatRanked(
                    xpGained, level,
                    xpCurrent, xpMax,
                    eloChange, currentElo,
                    rankNameStr, rankColor,
                    rankMin, rankMax);
            }
        }
        else
        {
            if (isVictory)
                panel.ShowVictory(
                    xpGained, level,
                    xpCurrent, xpMax);
            else
                panel.ShowDefeat(
                    xpGained, level,
                    xpCurrent, xpMax);
        }
    }

    // =========================================================
    // RETURN TO MENU
    // =========================================================

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;

        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Disconnect();

        SceneManager.LoadScene(menuSceneName);
    }

    // =========================================================
    // RANK THRESHOLDS
    // =========================================================

    private (int, int) GetRankRange(int elo)
    {
        if (elo < 1200) return (0, 1200);
        if (elo < 1500) return (1200, 1500);
        if (elo < 1800) return (1500, 1800);
        if (elo < 2200) return (1800, 2200);
        return (2200, 3000);
    }

    // =========================================================
    // DEBUG
    // =========================================================

    [ContextMenu("DEBUG: Win")]
    public void DebugWin() => ShowVictory();

    [ContextMenu("DEBUG: Defeat")]
    public void DebugDefeat() => ShowDefeat();
}
