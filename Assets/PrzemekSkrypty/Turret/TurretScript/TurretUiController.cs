using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class TurretUiController : MonoBehaviour
{
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private Button[] upgradeButtons;
    [SerializeField] private TextMeshProUGUI[] buttonTexts;

    private Turret turret; // Referencja do "w�a�ciciela" tego UI

   
    // Metoda wywo�ywana przez TurretInteract, aby po��czy� UI z logik�
    public void LinkTurret(Turret ownerTurret)
    {
        turret = ownerTurret;
        // Zapisz si� na nas�uchiwanie zdarzenia ulepszenia
        turret.OnUpgraded -= UpdateDisplay; // Najpierw odpinamy na wszelki wypadek
        turret.OnUpgraded += UpdateDisplay;
    }

    public void Show()
    {
        if (turret == null) return;
        UpdateDisplay();
        uiPanel.SetActive(true);
    }

    private void UpdateDisplay()
    {
        if (turret == null) return;
        TurretData[] availableUpgrades = turret.GetAvailableUpgrades();

        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            if (i < availableUpgrades.Length && availableUpgrades[i] != null)
            {
                upgradeButtons[i].gameObject.SetActive(true);
                TurretData upgrade = availableUpgrades[i];
                buttonTexts[i].text = $"{upgrade.turretName}\nKoszt: {upgrade.upgradeCost}G";

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

    private void OnUpgradeButtonClicked(int pathIndex)
    {
        turret?.Upgrade(pathIndex);
        // Po klikni�ciu niech zdecyduje TurretInteract, co dalej (np. ukry� UI)
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

    private void OnDestroy()
    {
        // Dobra praktyka: wypisz si� ze zdarzenia, gdy UI jest niszczone
        if (turret != null)
        {
            turret.OnUpgraded -= UpdateDisplay;
        }
    }
}
