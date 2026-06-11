using UnityEngine;
using Photon.Pun;
using ElementumDefense.UI;
using ElementumDefense.Cards;
using ElementumDefense.Turrets;


namespace ElementumDefense.Players
{
/// <summary>
/// Manages turret building via hotbar system.
/// Handles selection, build mode entry/exit, and input.
/// </summary>
public class BuildManager : MonoBehaviour
{
    [Header("Hotbar Configuration")]
    [Tooltip("Turrets available on hotbar (keys 1-5)")]
    [SerializeField] private TurretData[] turretHotbar;

    private TurretData selectedTurret;
    private PlayerBuilder playerBuilder;
    private SimpleInputManager inputManager;
    private PhotonView photonView;
    private HotbarUI hotbarUI;

    private void Awake()
    {
        playerBuilder = GetComponent<PlayerBuilder>();
        inputManager = GetComponent<SimpleInputManager>();
        photonView = GetComponent<PhotonView>();
        hotbarUI = FindFirstObjectByType<HotbarUI>();
    }

    private void Update()
    {
        if (photonView == null || !photonView.IsMine) return;

        HandleHotbarInput();

        // Exit build mode with RMB or ESC
        if (inputManager.IsInBuildMode)
        {
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                ExitBuildMode();
            }
        }
    }

    /// <summary>Checks for hotbar key presses (1-9)</summary>
    private void HandleHotbarInput()
    {
        for (int i = 0; i < turretHotbar.Length && i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectTurretToBuild(turretHotbar[i]);
                hotbarUI?.OnHotkeyPressed(i);
                break;
            }
        }
    }

    /// <summary>Attempts to enter build mode with selected turret</summary>
    // ==========================================
    // SABOTAGE: BUILD DISABLE
    // ==========================================

    private bool isBuildingDisabled = false;

    // Element block — set of element types that cannot be built right now.
    // Used by ElementBlockSabotage. Cleared at end of sabotage duration.
    private readonly System.Collections.Generic.HashSet<ElementumDefense.Elements.ElementType> blockedElements
        = new System.Collections.Generic.HashSet<ElementumDefense.Elements.ElementType>();

    /// <summary>Sabotage API: disables/enables building turrets</summary>
    public void SetBuildingDisabled(bool disabled)
    {
        isBuildingDisabled = disabled;
        if (disabled)
        {
            ExitBuildMode();
            Debug.Log("[BuildManager] Building DISABLED by sabotage");
        }
        else
        {
            Debug.Log("[BuildManager] Building RESTORED");
        }
    }

    public bool IsBuildingDisabled => isBuildingDisabled;

    /// <summary>Sabotage API: blocks/unblocks an element type from being built.</summary>
    public void SetElementBlocked(ElementumDefense.Elements.ElementType element, bool blocked)
    {
        if (blocked) blockedElements.Add(element);
        else blockedElements.Remove(element);
        Debug.Log($"[BuildManager] Element {element} blocked={blocked}");
    }

    public bool IsElementBlocked(ElementumDefense.Elements.ElementType element)
        => blockedElements.Contains(element);

    public void SelectTurretToBuild(TurretData turret)
    {
        if (turret == null) return;
        if (isBuildingDisabled) { Debug.Log("[BuildManager] Building disabled by sabotage!"); return; }
        if (blockedElements.Contains(turret.elementType))
        {
            Debug.Log($"[BuildManager] Element {turret.elementType} is currently blocked by sabotage!");
            return;
        }

        // Calculate cost with card modifiers
        int finalCost = turret.cost;
        PlayerCardManager cardManager = GetComponent<PlayerCardManager>();
        if (cardManager != null)
        {
            finalCost = cardManager.GetModifiedTurretCost(turret.cost);
        }

        if (PlayerGold.LocalInstance.HasEnough(finalCost))
        {
            selectedTurret = turret;
            inputManager.EnterBuildMode();
            playerBuilder.ActivateBuildMode(turret);
        }
        else
        {
            Debug.Log($"[BuildManager] Not enough gold (need {finalCost})");
        }
    }

    /// <summary>Exits build mode and cancels turret placement</summary>
    public void ExitBuildMode()
    {
        if (playerBuilder == null || inputManager == null) return;

        inputManager.ExitBuildMode();
        playerBuilder.DeactivateBuildMode();
        selectedTurret = null;
    }

    /// <summary>Called by PlayerBuilder after successful turret placement</summary>
    public void OnTurretBuilt()
    {
        ExitBuildMode();
    }

    public bool IsInBuildMode() => inputManager?.IsInBuildMode ?? false;
}
}
