using UnityEngine;
using Photon.Pun;
using ElementumDefense.UI;
using ElementumDefense.Cards;

/// <summary>
/// Manages turret building via hotbar system
/// Handles selection, build mode entry/exit
/// </summary>
public class BuildManager : MonoBehaviour
{
    [Header("Hotbar Configuration")]
    [Tooltip("Turrets available on hotbar (keys 1-5)")]
    [SerializeField] private TurretData[] turretHotbar;

    private TurretData selectedTurret;
    private PlayerBuilder playerBuilder;
    private PlayerInputManager playerInputManager;
    private PhotonView photonView;

    private HotbarUI hotbarUI;

    private void Awake()
    {
        playerBuilder = GetComponent<PlayerBuilder>();
        playerInputManager = GetComponent<PlayerInputManager>();
        photonView = GetComponent<PhotonView>();

        hotbarUI = FindFirstObjectByType<HotbarUI>();
    }

    private void Update()
    {
        // Only process input for local player
        if (photonView == null || !photonView.IsMine) return;

        HandleHotbarInput();

        // Exit build mode with RMB or ESC
        if (playerInputManager.IsInBuildMode)
        {
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                ExitBuildMode();
            }
        }
    }

    /// <summary>
    /// Checks for hotbar key presses (1-5)
    /// </summary>
    private void HandleHotbarInput()
    {
        for (int i = 0; i < turretHotbar.Length && i < 9; i++) // Support up to keys 1-9
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectTurretToBuild(turretHotbar[i]);
                // ========== NOWE: Notify hotbar ==========
                if (hotbarUI != null)
                {
                    hotbarUI.OnHotkeyPressed(i);
                }
                // =========================================
                break;
            }
        }
    }

    /// <summary>
    /// Attempts to enter build mode with selected turret
    /// </summary>
    public void SelectTurretToBuild(TurretData turret)
    {
        if (turret == null) return;

        // ========== NOWE: Apply cost modifier ==========
        int finalCost = turret.cost;

        PlayerCardManager cardManager = GetComponent<PlayerCardManager>();
        if (cardManager != null)
        {
            finalCost = cardManager.GetModifiedTurretCost(turret.cost);
        }
        // ================================================

        if (PlayerGold.LocalInstance.HasEnough(finalCost))
        {
            selectedTurret = turret;
            playerInputManager.EnterBuildMode();
            playerBuilder.ActivateBuildMode(turret);
        }
        else
        {
            Debug.Log($"[BuildManager] Not enough gold (need {finalCost})");
        }
    }

    /// <summary>
    /// Exits build mode and cancels turret placement
    /// </summary>
    public void ExitBuildMode()
    {
        if (playerBuilder == null || playerInputManager == null) return;

        playerInputManager.ExitBuildMode();
        playerBuilder.DeactivateBuildMode();
        selectedTurret = null;
    }

    /// <summary>
    /// Called by PlayerBuilder after successful turret placement
    /// </summary>
    public void OnTurretBuilt()
    {
        ExitBuildMode();
    }

    public bool IsInBuildMode() => playerInputManager?.IsInBuildMode ?? false;
}