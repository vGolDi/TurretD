using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using ElementumDefense.Cards;
using ElementumDefense.Progression;
using ElementumDefense.Ranked;
using ElementumDefense.UI;
using ElementumDefense.Projectiles;
using ElementumDefense.BattlePass;

public class GameEndManager : MonoBehaviour
{
    [Header("Rewards Config")]
    [SerializeField] private int victoryXP = 500;
    [SerializeField] private int defeatXP = 100;

    // ELO jest teraz dynamiczne � nie ma
    // sta�ych winElo / loseElo!

    [Header("Settings")]
    [SerializeField]
    private string menuSceneName = "MainMenu";

    private bool gameEnded = false;

    private void Start()
    {
        if (GameEndPanelUI.Instance != null)
        {
            GameEndPanelUI.Instance.OnReturnToMenu +=
                ReturnToMenu;
        }
    }

    private void OnEnable()
    {
        StartCoroutine(LateSubscribe());
    }

    private System.Collections.IEnumerator
        LateSubscribe()
    {
        yield return null;
        yield return null;

        if (GameEndPanelUI.Instance != null)
        {
            GameEndPanelUI.Instance
                .OnReturnToMenu -= ReturnToMenu;
            GameEndPanelUI.Instance
                .OnReturnToMenu += ReturnToMenu;
        }
    }

    private void OnDestroy()
    {
        if (GameEndPanelUI.Instance != null)
        {
            GameEndPanelUI.Instance
                .OnReturnToMenu -= ReturnToMenu;
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

        // Questy
        if (questManager != null)
        {
            questManager.ReportProgress(
                QuestType.PlayGames, 1);
            if (isVictory)
                questManager.ReportProgress(
                    QuestType.WinGames, 1);
        }

        // Battle Pass XP
        if (BattlePassManager.Instance != null)
        {
            BattlePassManager.Instance.AwardMatchXP(isVictory);
        }

        // ========================================
        // RANKED ELO � dynamiczna kalkulacja
        // ========================================
        if (player != null &&
            player.SelectedGameMode ==
                GameMode.Ranked)
        {
            int myElo = player.GetElo();
            int opponentElo = GetOpponentElo();

            eloChange =
                EloCalculator.CalculateEloChange(
                    myElo, opponentElo, isVictory);

            player.AddElo(eloChange);

            // Statystyki W/L
            if (isVictory)
                player.AddWin();
            else
                player.AddLoss();

            Debug.Log(
                $"[Ranked] My ELO: {myElo}, " +
                $"Opponent ELO: {opponentElo}, " +
                $"Won: {isVictory}, " +
                $"Change: {(eloChange > 0 ? "+" : "")}" +
                $"{eloChange}");
        }
    }

    // =========================================================
    // ODCZYT ELO PRZECIWNIKA Z PHOTON
    // =========================================================

    /// <summary>
    /// Pobiera ELO przeciwnika z Photon Custom
    /// Properties. Fallback: w�asne ELO gracza.
    /// </summary>
    private int GetOpponentElo()
    {
        if (!PhotonNetwork.InRoom ||
            PhotonNetwork.CurrentRoom == null)
        {
            return GetFallbackElo();
        }

        foreach (var kvp in
            PhotonNetwork.CurrentRoom.Players)
        {
            Player p = kvp.Value;
            if (!p.IsLocal)
            {
                if (p.CustomProperties.TryGetValue(
                    "elo", out object eloObj))
                {
                    int oppElo = (int)eloObj;
                    Debug.Log(
                        $"[Ranked] Opponent ELO " +
                        $"from Photon: {oppElo}");
                    return oppElo;
                }

                Debug.LogWarning(
                    "[Ranked] Opponent has no " +
                    "ELO property � using fallback");
            }
        }

        return GetFallbackElo();
    }

    /// <summary>
    /// Fallback ELO je�li nie znamy przeciwnika
    /// (np. roz��czy� si�). Zak�adamy
    /// podobny poziom.
    /// </summary>
    private int GetFallbackElo()
    {
        return PlayerCollection.Instance?.GetElo()
            ?? EloCalculator.DEFAULT_ELO;
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
                ? player.GetRankName()
                : "UNRANKED";
            Color rankColor = player != null
                ? player.GetRankColor()
                : Color.gray;

            (int rankMin, int rankMax) =
                EloCalculator.GetRankRange(
                    currentElo);

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
    // DEBUG
    // =========================================================

    [ContextMenu("DEBUG: Win")]
    public void DebugWin() => ShowVictory();

    [ContextMenu("DEBUG: Defeat")]
    public void DebugDefeat() => ShowDefeat();
}