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

    [Header("Art Deco Colors")]
    [SerializeField] private Image panelBackground;
    [SerializeField] private Image[] buttonBackgrounds;

    // Art Deco palette
    private static readonly Color PanelBg =
        new Color(0.04f, 0.06f, 0.1f, 0.9f);
    private static readonly Color BorderGold =
        new Color(0.96f, 0.62f, 0.04f, 0.3f);
    private static readonly Color TextCream =
        new Color(1f, 0.95f, 0.78f, 1f);
    private static readonly Color TextGold =
        new Color(0.96f, 0.62f, 0.04f, 0.6f);
    private static readonly Color BtnAfford =
        new Color(0.29f, 0.87f, 0.5f, 0.15f);
    private static readonly Color BtnCantAfford =
        new Color(0.97f, 0.44f, 0.44f, 0.1f);
    private static readonly Color StatUp =
        new Color(0.29f, 0.87f, 0.5f);
    private static readonly Color StatDown =
        new Color(0.97f, 0.44f, 0.44f);

    private Turret turret;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;

        // Apply Art Deco styling
        if (panelBackground != null)
            panelBackground.color = PanelBg;

        if (turretNameText != null)
            turretNameText.color = TextCream;

        if (turretStatsText != null)
            turretStatsText.color = TextCream;

        Hide();
    }

    private void LateUpdate()
    {
        if (uiPanel.activeSelf && mainCamera != null)
        {
            uiPanel.transform.LookAt(
                uiPanel.transform.position +
                mainCamera.transform.rotation *
                    Vector3.forward,
                mainCamera.transform.rotation *
                    Vector3.up);
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
            uiPanel.SetActive(false);
    }

    public bool IsVisible()
    {
        return uiPanel != null && uiPanel.activeSelf;
    }

    private void UpdateDisplay()
    {
        if (turret == null) return;

        // Name
        if (turretNameText != null &&
            turret.TurretData != null)
        {
            turretNameText.text =
                turret.TurretData.turretName
                    .ToUpper();
        }

        // Stats with Art Deco formatting
        if (turretStatsText != null)
        {
            string stats = "";

            // Damage
            stats += FormatStat(
                "DMG", turret.CurrentDamage,
                turret.BaseDamage, "F0");

            // Fire Rate
            stats += FormatStat(
                "RATE", turret.CurrentFireRate,
                turret.BaseFireRate, "F2");

            // Range
            stats += FormatStat(
                "RNG", turret.CurrentRange,
                turret.BaseRange, "F1");

            turretStatsText.text = stats;
        }

        // Upgrade buttons
        TurretData[] upgrades =
            turret.GetAvailableUpgrades();

        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            if (upgrades != null &&
                i < upgrades.Length &&
                upgrades[i] != null)
            {
                upgradeButtons[i].gameObject
                    .SetActive(true);

                TurretData upgrade = upgrades[i];

                buttonTexts[i].text =
                    FormatUpgradeText(upgrade);
                buttonTexts[i].color = TextCream;

                // Color button based on affordability
                bool canAfford =
                    PlayerGold.LocalInstance != null &&
                    PlayerGold.LocalInstance
                        .HasEnough(
                            upgrade.upgradeCost);

                if (buttonBackgrounds != null &&
                    i < buttonBackgrounds.Length &&
                    buttonBackgrounds[i] != null)
                {
                    buttonBackgrounds[i].color =
                        canAfford
                            ? BtnAfford
                            : BtnCantAfford;
                }

                upgradeButtons[i].interactable =
                    canAfford;

                int pathIndex = i;
                upgradeButtons[i].onClick
                    .RemoveAllListeners();
                upgradeButtons[i].onClick
                    .AddListener(() =>
                        OnUpgradeButtonClicked(
                            pathIndex));
            }
            else
            {
                upgradeButtons[i].gameObject
                    .SetActive(false);
            }
        }
    }

    private string FormatStat(
        string label, float current,
        float baseVal, string format)
    {
        string line = $"{label}: {current.ToString(format)}";

        float diff = current - baseVal;
        if (Mathf.Abs(diff) > 0.01f)
        {
            string hex = diff > 0
                ? ColorUtility.ToHtmlStringRGB(StatUp)
                : ColorUtility.ToHtmlStringRGB(
                    StatDown);

            line += $" <color=#{hex}>" +
                    $"({diff.ToString("+0.#;-0.#")})" +
                    $"</color>";
        }

        return line + "\n";
    }

    private string FormatUpgradeText(
        TurretData upgrade)
    {
        int cost = upgrade.upgradeCost;
        string costText = $"{cost}";

        if (turret?.GetOwner() != null)
        {
            var cardMgr = turret.GetOwner()
                .GetComponent<ElementumDefense
                    .Cards.PlayerCardManager>();

            if (cardMgr != null)
            {
                int modified =
                    cardMgr.GetModifiedTurretCost(
                        cost);
                if (modified != cost)
                    costText =
                        $"<s>{cost}</s> {modified}";
            }
        }

        return $"<b>{upgrade.turretName}</b>\n" +
               $"{costText} Gold  |  " +
               $"DMG {upgrade.damage}";
    }

    private void OnUpgradeButtonClicked(
        int pathIndex)
    {
        turret?.Upgrade(pathIndex);
    }

    private void OnDestroy()
    {
        if (turret != null)
            turret.OnUpgraded -= UpdateDisplay;
    }
}
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//public class TurretUiController : MonoBehaviour
//{
//    [Header("UI References")]
//    [SerializeField] private GameObject uiPanel;
//    [SerializeField] private Button[] upgradeButtons;
//    [SerializeField] private TextMeshProUGUI[] buttonTexts;

//    [Header("Stats Display")]
//    [SerializeField] private TextMeshProUGUI turretNameText;
//    [SerializeField] private TextMeshProUGUI turretStatsText;

//    private Turret turret;
//    private Camera mainCamera;

//    private void Start()
//    {
//        mainCamera = Camera.main;
//        Hide();
//    }

//    private void LateUpdate()
//    {
//        if (uiPanel.activeSelf && mainCamera != null)
//        {
//            uiPanel.transform.LookAt(
//                uiPanel.transform.position + mainCamera.transform.rotation * Vector3.forward,
//                mainCamera.transform.rotation * Vector3.up
//            );
//        }
//    }

//    public void LinkTurret(Turret ownerTurret)
//    {
//        turret = ownerTurret;

//        turret.OnUpgraded -= UpdateDisplay;
//        turret.OnUpgraded += UpdateDisplay;
//    }

//    public void Show()
//    {
//        if (turret == null) return;

//        UpdateDisplay();
//        uiPanel.SetActive(true);
//    }

//    public void Hide()
//    {
//        if (uiPanel != null)
//        {
//            uiPanel.SetActive(false);
//        }
//    }

//    public bool IsVisible()
//    {
//        return uiPanel != null && uiPanel.activeSelf;
//    }

//    private void UpdateDisplay()
//    {
//        if (turret == null) return;

//        // ========== NOWE: Show current stats with modifiers ==========
//        if (turretNameText != null && turret.TurretData != null)
//        {
//            turretNameText.text = turret.TurretData.turretName;
//        }

//        if (turretStatsText != null)
//        {
//            bool hasModifier = Mathf.Abs(turret.CurrentDamage - turret.BaseDamage) > 0.01f ||
//                               Mathf.Abs(turret.CurrentFireRate - turret.BaseFireRate) > 0.01f ||
//                               Mathf.Abs(turret.CurrentRange - turret.BaseRange) > 0.01f;

//            string stats = "";

//            // Damage
//            stats += $"⚔️ DMG: {turret.CurrentDamage:F0}";
//            if (Mathf.Abs(turret.CurrentDamage - turret.BaseDamage) > 0.01f)
//            {
//                float diff = turret.CurrentDamage - turret.BaseDamage;
//                string color = diff > 0 ? "green" : "red";
//                stats += $" <color={color}>({diff:+0.0;-0.0})</color>";
//            }
//            stats += "\n";

//            // Fire Rate
//            stats += $"⚡ FR: {turret.CurrentFireRate:F2}/s";
//            if (Mathf.Abs(turret.CurrentFireRate - turret.BaseFireRate) > 0.01f)
//            {
//                float diff = turret.CurrentFireRate - turret.BaseFireRate;
//                string color = diff > 0 ? "green" : "red";
//                stats += $" <color={color}>({diff:+0.00;-0.00})</color>";
//            }
//            stats += "\n";

//            // Range
//            stats += $"🎯 RNG: {turret.CurrentRange:F1}";
//            if (Mathf.Abs(turret.CurrentRange - turret.BaseRange) > 0.01f)
//            {
//                float diff = turret.CurrentRange - turret.BaseRange;
//                string color = diff > 0 ? "green" : "red";
//                stats += $" <color={color}>({diff:+0.0;-0.0})</color>";
//            }

//            turretStatsText.text = stats;
//        }
//        // ============================================================

//        // Update upgrade buttons
//        TurretData[] availableUpgrades = turret.GetAvailableUpgrades();

//        for (int i = 0; i < upgradeButtons.Length; i++)
//        {
//            if (availableUpgrades != null &&
//                i < availableUpgrades.Length &&
//                availableUpgrades[i] != null)
//            {
//                upgradeButtons[i].gameObject.SetActive(true);
//                TurretData upgrade = availableUpgrades[i];

//                buttonTexts[i].text = FormatUpgradeText(upgrade);

//                int pathIndex = i;
//                upgradeButtons[i].onClick.RemoveAllListeners();
//                upgradeButtons[i].onClick.AddListener(() => OnUpgradeButtonClicked(pathIndex));
//            }
//            else
//            {
//                upgradeButtons[i].gameObject.SetActive(false);
//            }
//        }
//    }

//    private string FormatUpgradeText(TurretData upgrade)
//    {
//        // ========== NOWE: Show cost with modifier ==========
//        string costText = $"{upgrade.upgradeCost}";

//        // Try to get card manager for cost modifier
//        if (turret != null && turret.GetOwner() != null)
//        {
//            var cardMgr = turret.GetOwner()
//                .GetComponent<ElementumDefense.Cards.PlayerCardManager>();

//            if (cardMgr != null)
//            {
//                int modifiedCost = cardMgr.GetModifiedTurretCost(upgrade.upgradeCost);
//                if (modifiedCost != upgrade.upgradeCost)
//                {
//                    costText = $"<s>{upgrade.upgradeCost}</s> {modifiedCost}";
//                }
//            }
//        }
//        // ===================================================

//        return $"<b>{upgrade.turretName}</b>\n" +
//               $"Cost: {costText} Gold\n" +
//               $"DMG: {upgrade.damage} | RNG: {upgrade.range}";
//    }

//    private void OnUpgradeButtonClicked(int pathIndex)
//    {
//        turret?.Upgrade(pathIndex);
//    }

//    private void OnDestroy()
//    {
//        if (turret != null)
//        {
//            turret.OnUpgraded -= UpdateDisplay;
//        }
//    }
//}