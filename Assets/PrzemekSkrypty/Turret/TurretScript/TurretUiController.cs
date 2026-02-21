
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TurretUiController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private Button[] upgradeButtons;
    [SerializeField] private TextMeshProUGUI[] buttonTexts;

    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI turretNameText;
    [SerializeField] private TextMeshProUGUI turretStatsText;

    private Turret turret;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        Hide();
    }

    private void LateUpdate()
    {
        if (uiPanel.activeSelf && mainCamera != null)
        {
            uiPanel.transform.LookAt(
                uiPanel.transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up
            );
        }
    }

    public void LinkTurret(Turret ownerTurret)
    {
        turret = ownerTurret;

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

    private void UpdateDisplay()
    {
        if (turret == null) return;

        // ========== NOWE: Show current stats with modifiers ==========
        if (turretNameText != null && turret.TurretData != null)
        {
            turretNameText.text = turret.TurretData.turretName;
        }

        if (turretStatsText != null)
        {
            bool hasModifier = Mathf.Abs(turret.CurrentDamage - turret.BaseDamage) > 0.01f ||
                               Mathf.Abs(turret.CurrentFireRate - turret.BaseFireRate) > 0.01f ||
                               Mathf.Abs(turret.CurrentRange - turret.BaseRange) > 0.01f;

            string stats = "";

            // Damage
            stats += $"⚔️ DMG: {turret.CurrentDamage:F0}";
            if (Mathf.Abs(turret.CurrentDamage - turret.BaseDamage) > 0.01f)
            {
                float diff = turret.CurrentDamage - turret.BaseDamage;
                string color = diff > 0 ? "green" : "red";
                stats += $" <color={color}>({diff:+0.0;-0.0})</color>";
            }
            stats += "\n";

            // Fire Rate
            stats += $"⚡ FR: {turret.CurrentFireRate:F2}/s";
            if (Mathf.Abs(turret.CurrentFireRate - turret.BaseFireRate) > 0.01f)
            {
                float diff = turret.CurrentFireRate - turret.BaseFireRate;
                string color = diff > 0 ? "green" : "red";
                stats += $" <color={color}>({diff:+0.00;-0.00})</color>";
            }
            stats += "\n";

            // Range
            stats += $"🎯 RNG: {turret.CurrentRange:F1}";
            if (Mathf.Abs(turret.CurrentRange - turret.BaseRange) > 0.01f)
            {
                float diff = turret.CurrentRange - turret.BaseRange;
                string color = diff > 0 ? "green" : "red";
                stats += $" <color={color}>({diff:+0.0;-0.0})</color>";
            }

            turretStatsText.text = stats;
        }
        // ============================================================

        // Update upgrade buttons
        TurretData[] availableUpgrades = turret.GetAvailableUpgrades();

        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            if (availableUpgrades != null &&
                i < availableUpgrades.Length &&
                availableUpgrades[i] != null)
            {
                upgradeButtons[i].gameObject.SetActive(true);
                TurretData upgrade = availableUpgrades[i];

                buttonTexts[i].text = FormatUpgradeText(upgrade);

                int pathIndex = i;
                upgradeButtons[i].onClick.RemoveAllListeners();
                upgradeButtons[i].onClick.AddListener(() => OnUpgradeButtonClicked(pathIndex));
            }
            else
            {
                upgradeButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private string FormatUpgradeText(TurretData upgrade)
    {
        // ========== NOWE: Show cost with modifier ==========
        string costText = $"{upgrade.upgradeCost}";

        // Try to get card manager for cost modifier
        if (turret != null && turret.GetOwner() != null)
        {
            var cardMgr = turret.GetOwner()
                .GetComponent<ElementumDefense.Cards.PlayerCardManager>();

            if (cardMgr != null)
            {
                int modifiedCost = cardMgr.GetModifiedTurretCost(upgrade.upgradeCost);
                if (modifiedCost != upgrade.upgradeCost)
                {
                    costText = $"<s>{upgrade.upgradeCost}</s> {modifiedCost}";
                }
            }
        }
        // ===================================================

        return $"<b>{upgrade.turretName}</b>\n" +
               $"Cost: {costText} Gold\n" +
               $"DMG: {upgrade.damage} | RNG: {upgrade.range}";
    }

    private void OnUpgradeButtonClicked(int pathIndex)
    {
        turret?.Upgrade(pathIndex);
    }

    private void OnDestroy()
    {
        if (turret != null)
        {
            turret.OnUpgraded -= UpdateDisplay;
        }
    }
}