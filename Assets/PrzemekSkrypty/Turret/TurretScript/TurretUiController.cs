using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class TurretUiController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private Button[] upgradeButtons;
    [SerializeField] private TextMeshProUGUI[] buttonTexts;

    [Header("Stats Display (Optional)")]
    [SerializeField] private TextMeshProUGUI turretNameText;
    [SerializeField] private TextMeshProUGUI turretStatsText;

    private Turret turret; // Owner turret logic
    private Camera mainCamera;
    private void Start()
    {
        mainCamera = Camera.main;
        Hide(); // Start hidden
    }

    private void LateUpdate()
    {
        // Make UI face camera (billboard effect)
        if (uiPanel.activeSelf && mainCamera != null)
        {
            uiPanel.transform.LookAt(
                uiPanel.transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up
            );
        }
    }

    /// <summary>
    /// Links this UI to its parent turret logic
    /// Called by TurretInteract
    /// </summary>
    public void LinkTurret(Turret ownerTurret)
    {
        turret = ownerTurret;

        // Subscribe to upgrade event
        turret.OnUpgraded -= UpdateDisplay;
        turret.OnUpgraded += UpdateDisplay;
    }

    public void Show()
    {
        if (turret == null) return;

        UpdateDisplay();
        uiPanel.SetActive(true);
    }

    public void Hide()
    {
        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }
    }

    public bool IsVisible()
    {
        return uiPanel != null && uiPanel.activeSelf;
    }

    /// <summary>
    /// Refreshes UI with current turret data
    /// </summary>
    private void UpdateDisplay()
    {
        if (turret == null) return;

        TurretData[] availableUpgrades = turret.GetAvailableUpgrades();

        // Update upgrade buttons
        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            if (availableUpgrades != null &&
                i < availableUpgrades.Length &&
                availableUpgrades[i] != null)
            {
                // Show button with upgrade info
                upgradeButtons[i].gameObject.SetActive(true);
                TurretData upgrade = availableUpgrades[i];

                buttonTexts[i].text = FormatUpgradeText(upgrade);

                // Setup button click listener
                int pathIndex = i; // Capture index for lambda
                upgradeButtons[i].onClick.RemoveAllListeners();
                upgradeButtons[i].onClick.AddListener(() => OnUpgradeButtonClicked(pathIndex));
            }
            else
            {
                // Hide unused buttons
                upgradeButtons[i].gameObject.SetActive(false);
            }
        }

        // Update current stats display (if available)
        // UpdateCurrentStats();
    }

    /// <summary>
    /// Formats upgrade button text with name and cost
    /// TODO: Show stat differences (e.g., +5 damage, +2 range)
    /// </summary>
    private string FormatUpgradeText(TurretData upgrade)
    {
        return $"<b>{upgrade.turretName}</b>\n" +
               $"Cost: {upgrade.upgradeCost} Gold\n" +
               $"DMG: {upgrade.damage} | RNG: {upgrade.range}";
    }

    /// <summary>
    /// Called when player clicks an upgrade button
    /// </summary>
    private void OnUpgradeButtonClicked(int pathIndex)
    {
        turret?.Upgrade(pathIndex);
        // UI will auto-refresh via OnUpgraded event
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (turret != null)
        {
            turret.OnUpgraded -= UpdateDisplay;
        }
    }
}
