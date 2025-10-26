using UnityEngine;
using TMPro;

namespace ElementumDefense.Projectiles
{
    public class ProjectileHitUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI shotsText;
        [SerializeField] private TextMeshProUGUI hitsText;
        [SerializeField] private TextMeshProUGUI accuracyText;
        [SerializeField] private TextMeshProUGUI killsText; // NEW!
        [SerializeField] private TextMeshProUGUI damageText; // NEW!

        [Header("Display Settings")]
        [SerializeField] private bool showAccuracy = true;
        [SerializeField] private string shotsFormat = "Shots: {0}";
        [SerializeField] private string hitsFormat = "Hits: {0}";
        [SerializeField] private string accuracyFormat = "Accuracy: {0:F1}%";
        [SerializeField] private string killsFormat = "Kills: {0}"; // NEW!
        [SerializeField] private string damageFormat = "Damage: {0}"; // NEW!

        private void Start()
        {
            if (ProjectileStatsManager.Instance == null)
            {
                Debug.LogError("[ProjectileHitUI] ProjectileStatsManager not found!");
                gameObject.SetActive(false);
                return;
            }

            UpdateDisplay(0, 0);

            ProjectileStatsManager.Instance.OnStatsUpdated += UpdateDisplay;
            ProjectileStatsManager.Instance.OnKillRegistered += UpdateKills; // NEW!
        }

        private void UpdateDisplay(int shots, int hits)
        {
            if (shotsText != null)
            {
                shotsText.text = string.Format(shotsFormat, shots);
            }

            if (hitsText != null)
            {
                hitsText.text = string.Format(hitsFormat, hits);
            }

            if (accuracyText != null && showAccuracy)
            {
                float accuracy = ProjectileStatsManager.Instance.GetAccuracy();
                accuracyText.text = string.Format(accuracyFormat, accuracy);

                // Color code accuracy
                if (accuracy >= 80f)
                    accuracyText.color = Color.green;
                else if (accuracy >= 50f)
                    accuracyText.color = Color.yellow;
                else
                    accuracyText.color = Color.red;
            }

            // Update damage
            if (damageText != null)
            {
                damageText.text = string.Format(damageFormat, ProjectileStatsManager.Instance.TotalDamageDealt);
            }
        }

        // ========== NEW ==========
        private void UpdateKills(int kills)
        {
            if (killsText != null)
            {
                killsText.text = string.Format(killsFormat, kills);
            }
        }
        // ========================

        private void OnDestroy()
        {
            if (ProjectileStatsManager.Instance != null)
            {
                ProjectileStatsManager.Instance.OnStatsUpdated -= UpdateDisplay;
                ProjectileStatsManager.Instance.OnKillRegistered -= UpdateKills;
            }
        }
    }
}