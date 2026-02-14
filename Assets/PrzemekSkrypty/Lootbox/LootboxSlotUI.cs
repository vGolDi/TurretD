// Assets/PrzemekSkrypty/UI/LootboxSlotUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ElementumDefense.Lootbox;

namespace ElementumDefense.UI
{
    /// <summary>
    /// UI component for single lootbox slot in inventory
    /// </summary>
    public class LootboxSlotUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image glowImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button openButton;

        [Header("Hover Effect")]
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float hoverDuration = 0.1f;

        private LootboxData lootboxData;
        private int count;
        private LootboxUI parentUI;

        /// <summary>
        /// Setup slot with lootbox data
        /// </summary>
        public void Setup(LootboxData lootbox, int amount, LootboxUI parent)
        {
            lootboxData = lootbox;
            count = amount;
            parentUI = parent;

            // Set icon
            if (iconImage != null && lootbox.lootboxIcon != null)
            {
                iconImage.sprite = lootbox.lootboxIcon;
            }

            // Set glow color
            if (glowImage != null)
            {
                glowImage.color = lootbox.GetRarityColor();
            }

            // Set background
            if (backgroundImage != null)
            {
                Color bgColor = lootbox.GetRarityColor();
                bgColor.a = 0.3f;
                backgroundImage.color = bgColor;
            }

            // Set name
            if (nameText != null)
            {
                nameText.text = lootbox.lootboxName;
            }

            // Set count
            if (countText != null)
            {
                countText.text = $"x{amount}";
            }

            // Set description
            if (descriptionText != null)
            {
                descriptionText.text = $"{lootbox.cardCount} Cards";
            }

            // Setup button
            if (openButton != null)
            {
                openButton.onClick.RemoveAllListeners();
                openButton.onClick.AddListener(OnOpenClicked);
            }
        }

        private void OnOpenClicked()
        {
            if (parentUI != null && lootboxData != null)
            {
                parentUI.TryOpenLootbox(lootboxData);
            }
        }

        // Hover effects (optional)
        public void OnPointerEnter()
        {
            transform.localScale = Vector3.one * hoverScale;
        }

        public void OnPointerExit()
        {
            transform.localScale = Vector3.one;
        }
    }
}