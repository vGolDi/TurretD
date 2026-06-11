using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace ElementumDefense.Turrets
{
public class TurretUiController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private Button[] upgradeButtons;
    [SerializeField] private TextMeshProUGUI[] buttonTexts;

    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI turretNameText;
    [SerializeField] private TextMeshProUGUI turretStatsText;

    [Header("Merge")]
    [Tooltip("Przycisk MERGE — ustaw w prefabie TurretUI. Będzie ukryty gdy merge niedostępny.")]
    [SerializeField] private Button mergeButton;
    [SerializeField] private TextMeshProUGUI mergeButtonText;

    private Turret turret;
    private Camera mainCamera;
    private MergeReadyIndicator mergeIndicator;

    // ==========================================
    // LIFECYCLE
    // ==========================================

    private void Start()
    {
        mainCamera = Camera.main;
        Hide();

        // Merge button: ukryj natychmiast (zanim cokolwiek zrobi Show)
        if (mergeButton != null)
        {
            mergeButton.gameObject.SetActive(false);
            mergeButton.onClick.RemoveAllListeners();
            mergeButton.onClick.AddListener(OnMergeButtonClicked);
        }
    }

    private void LateUpdate()
    {
        if (uiPanel != null && uiPanel.activeSelf && mainCamera != null)
        {
            uiPanel.transform.LookAt(
                uiPanel.transform.position + mainCamera.transform.rotation * Vector3.forward,
                mainCamera.transform.rotation * Vector3.up
            );
        }
    }

    private void OnDestroy()
    {
        if (turret != null)
            turret.OnUpgraded -= UpdateDisplay;
    }

    // ==========================================
    // LINK
    // ==========================================

    public void LinkTurret(Turret ownerTurret)
    {
        turret = ownerTurret;
        turret.OnUpgraded -= UpdateDisplay;
        turret.OnUpgraded += UpdateDisplay;

        // Cache MergeReadyIndicator — szuka na root, parent lub children
        // (obsługuje wzorzec Turret_Logic jako child)
        mergeIndicator = turret.GetComponentInParent<MergeReadyIndicator>();
        if (mergeIndicator == null)
            mergeIndicator = turret.GetComponent<MergeReadyIndicator>();
        if (mergeIndicator == null)
            mergeIndicator = turret.GetComponentInChildren<MergeReadyIndicator>();
    }

    // ==========================================
    // SHOW / HIDE
    // ==========================================

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

    // ==========================================
    // UPDATE DISPLAY
    // ==========================================

    private void UpdateDisplay()
    {
        if (turret == null) return;

        // --- Nazwa turretu ---
        if (turretNameText != null && turret.TurretData != null)
            turretNameText.text = turret.TurretData.turretName;

        // --- Statystyki ---
        if (turretStatsText != null)
            turretStatsText.text = BuildStatsText();

        // --- Przyciski upgrade ---
        UpdateUpgradeButtons();

        // --- Przycisk MERGE ---
        UpdateMergeButton();
    }

    // ==========================================
    // STATS TEXT
    // ==========================================

    private string BuildStatsText()
    {
        string stats = "";

        // Damage
        stats += $"⚔ DMG: {turret.CurrentDamage:F0}";
        float dmgDiff = turret.CurrentDamage - turret.BaseDamage;
        if (Mathf.Abs(dmgDiff) > 0.01f)
            stats += $" <color={(dmgDiff > 0 ? "green" : "red")}>({dmgDiff:+0.0;-0.0})</color>";
        stats += "\n";

        // Fire Rate
        stats += $"⚡ FR: {turret.CurrentFireRate:F2}/s";
        float frDiff = turret.CurrentFireRate - turret.BaseFireRate;
        if (Mathf.Abs(frDiff) > 0.01f)
            stats += $" <color={(frDiff > 0 ? "green" : "red")}>({frDiff:+0.00;-0.00})</color>";
        stats += "\n";

        // Range
        stats += $"◎ RNG: {turret.CurrentRange:F1}";
        float rngDiff = turret.CurrentRange - turret.BaseRange;
        if (Mathf.Abs(rngDiff) > 0.01f)
            stats += $" <color={(rngDiff > 0 ? "green" : "red")}>({rngDiff:+0.0;-0.0})</color>";

        return stats;
    }

    // ==========================================
    // UPGRADE BUTTONS
    // ==========================================

    private void UpdateUpgradeButtons()
    {
        TurretData[] availableUpgrades = turret.GetAvailableUpgrades();
        bool isMaxLevel = IsMaxLevel();

        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            if (upgradeButtons[i] == null) continue;

            // Ukryj upgrade buttony gdy turret jest max level (gotowy do merge)
            if (isMaxLevel)
            {
                upgradeButtons[i].gameObject.SetActive(false);
                continue;
            }

            bool hasUpgrade = availableUpgrades != null &&
                              i < availableUpgrades.Length &&
                              availableUpgrades[i] != null;

            upgradeButtons[i].gameObject.SetActive(hasUpgrade);

            if (!hasUpgrade) continue;

            TurretData upgrade = availableUpgrades[i];

            if (buttonTexts != null && i < buttonTexts.Length && buttonTexts[i] != null)
                buttonTexts[i].text = FormatUpgradeText(upgrade);

            int pathIndex = i;
            upgradeButtons[i].onClick.RemoveAllListeners();
            upgradeButtons[i].onClick.AddListener(() => OnUpgradeButtonClicked(pathIndex));
        }
    }

    private string FormatUpgradeText(TurretData upgrade)
    {
        string costText = $"{upgrade.upgradeCost}";

        // Sprawdź modyfikatory kart
        if (turret?.GetOwner() != null)
        {
            var cardMgr = turret.GetOwner()
                .GetComponent<ElementumDefense.Cards.PlayerCardManager>();
            if (cardMgr != null)
            {
                int modifiedCost = cardMgr.GetModifiedTurretCost(upgrade.upgradeCost);
                if (modifiedCost != upgrade.upgradeCost)
                    costText = $"<s>{upgrade.upgradeCost}</s> {modifiedCost}";
            }
        }

        return $"<b>{upgrade.turretName}</b>\n" +
               $"Cost: {costText} Gold\n" +
               $"DMG: {upgrade.damage} | RNG: {upgrade.range}";
    }

    private void OnUpgradeButtonClicked(int pathIndex)
    {
        turret?.Upgrade(pathIndex);
    }

    // ==========================================
    // MERGE BUTTON
    // ==========================================

    private void UpdateMergeButton()
    {
        if (mergeButton == null) return;

        bool isMaxLevel = IsMaxLevel();
        bool hasPartner = mergeIndicator != null && mergeIndicator.HasMergePartner;

        // Pokaż przycisk tylko gdy turret jest na max levelu
        mergeButton.gameObject.SetActive(isMaxLevel);

        if (!isMaxLevel) return;

        // Dostępność przycisku zależy od tego czy partner jest w zasięgu
        mergeButton.interactable = hasPartner;

        // Tekst przycisku
        if (mergeButtonText != null)
        {
            mergeButtonText.text = hasPartner
                ? "MERGE!"
                : "MERGE\n<size=70%>(brak partnera w zasięgu)</size>";
        }
    }

    private void OnMergeButtonClicked()
    {
        if (mergeIndicator == null || !mergeIndicator.HasMergePartner)
        {
            Debug.Log("[TurretUI] Merge attempted but no partner available.");
            return;
        }

        bool success = mergeIndicator.TryMerge();

        if (success)
        {
            // UI zostanie zniszczone razem z turretem po merge
            Debug.Log("[TurretUI] Merge successful!");
        }
        else
        {
            Debug.LogWarning("[TurretUI] Merge failed — check TurretMergeManager synergies list.");
        }
    }

    // ==========================================
    // HELPERS
    // ==========================================

    /// <summary>
    /// Turret jest na max levelu i MOŻE być mergowany gdy:
    /// - TurretData nie ma żadnych upgradePaths (koniec drzewka upgrade)
    /// - TurretData.canMerge == true (nie jest już zmergowanym turretem LV4)
    /// </summary>
    private bool IsMaxLevel()
    {
        if (turret?.TurretData == null) return false;
        bool noUpgrades = turret.TurretData.upgradePaths == null ||
                          turret.TurretData.upgradePaths.Length == 0;
        return noUpgrades && turret.TurretData.canMerge;
    }
}
}
