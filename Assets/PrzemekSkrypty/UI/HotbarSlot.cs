using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ElementumDefense.UI
{
    /// <summary>
    /// Single hotbar slot component
    /// Attach to each Slot GameObject
    /// </summary>
    public class HotbarSlot : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI hotkeyText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image selectionBorder;

        private TurretData turretData;
        private int hotkeyNumber;

        /// <summary>
        /// Sets up slot with turret data
        /// </summary>
        public void Initialize(TurretData data, int hotkey)
        {
            turretData = data;
            hotkeyNumber = hotkey;

            // Set icon color based on element
            if (iconImage != null && data.displayPrefab != null)
            {
                iconImage.color = ElementumDefense.Elements.ElementUtility.GetElementColor(data.elementType);
            }

            // Set name
            if (nameText != null)
            {
                nameText.text = data.turretName;
            }

            // Set cost
            if (costText != null)
            {
                costText.text = $"{data.cost}g";
            }

            // Set hotkey
            if (hotkeyText != null)
            {
                hotkeyText.text = hotkey.ToString();
            }

            // Hide selection border initially
            if (selectionBorder != null)
            {
                selectionBorder.gameObject.SetActive(false);
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Updates slot color based on affordability
        /// </summary>
        public void UpdateColor(Color color)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = color;
            }

            if (costText != null)
            {
                costText.color = color;
            }
        }

        /// <summary>
        /// Shows/hides selection indicator
        /// </summary>
        public void SetSelected(bool selected)
        {
            if (selectionBorder != null)
            {
                selectionBorder.gameObject.SetActive(selected);
            }
        }
    }
}