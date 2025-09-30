using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;


public class BuildManager : MonoBehaviour
{
    [Header("Konfiguracja Paska Skrótów")]
    [Tooltip("Lista wie¿yczek dostêpnych do budowy na pasku skrótów (np. 5 slotów).")]
    [SerializeField] private TurretData[] turretHotbar;

    private bool isInBuildMode = false;
    private TurretData selectedTurret;

    private PlayerBuilder _playerBuilder;
    private PlayerBuilder playerBuilder
    {
        get
        {
            if (_playerBuilder == null) _playerBuilder = GetComponent<PlayerBuilder>();
            return _playerBuilder;
        }
    }

    private PlayerInputManager _playerInputManager;
    private PlayerInputManager playerInputManager
    {
        get
        {
            if (_playerInputManager == null) _playerInputManager = GetComponent<PlayerInputManager>();
            return _playerInputManager;
        }
    }

    private PhotonView photonView;

    public bool IsInBuildMode() => isInBuildMode;

    private void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }

    private void Update()
    {
        if (photonView == null || !photonView.IsMine)
        {
            return;
        }

        HandleHotbarInput();

        // U¿ywamy w³aœciwoœci, która jest bezpieczna
        if (playerInputManager.IsInBuildMode && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            ExitBuildMode();
        }
    }

    private void HandleHotbarInput()
    {
        for (int i = 0; i < turretHotbar.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectTurretToBuild(turretHotbar[i]);
                break;
            }
        }
    }

    public void SelectTurretToBuild(TurretData turret)
    {
        if (playerBuilder == null || playerInputManager == null) return;

        if (PlayerGold.Instance.HasEnough(turret.cost))
        {
            playerInputManager.EnterBuildMode();
            playerBuilder.ActivateBuildMode(turret);
        }
        else
        {
            Debug.Log($"Za ma³o z³ota, by wybraæ {turret.turretName}");
        }
    }

    public void ExitBuildMode()
    {
        if (playerBuilder == null || playerInputManager == null) return;
        playerInputManager.ExitBuildMode();
        playerBuilder.DeactivateBuildMode();
    }

    // Metoda, któr¹ PlayerBuilder wywo³a po pomyœlnym zbudowaniu
    public void OnTurretBuilt()
    {
        ExitBuildMode();
    }
}
