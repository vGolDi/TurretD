using UnityEngine;
using Photon.Pun;

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

    private void Awake()
    {
        playerBuilder = GetComponent<PlayerBuilder>();
        playerInputManager = GetComponent<PlayerInputManager>();
        photonView = GetComponent<PhotonView>();
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
                break;
            }
        }
    }

    /// <summary>
    /// Attempts to enter build mode with selected turret
    /// </summary>
    public void SelectTurretToBuild(TurretData turret)
    {
        if (playerBuilder == null || playerInputManager == null || turret == null)
        {
            Debug.LogWarning("[BuildManager] Missing component or null turret");
            return;
        }

        // Check if player can afford it
        if (PlayerGold.LocalInstance.HasEnough(turret.cost))
        {
            selectedTurret = turret;
            playerInputManager.EnterBuildMode();
            playerBuilder.ActivateBuildMode(turret);
        }
        else
        {
            Debug.Log($"[BuildManager] Not enough gold for {turret.turretName} (need {turret.cost})");
            // TODO: Play error sound/show UI feedback
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