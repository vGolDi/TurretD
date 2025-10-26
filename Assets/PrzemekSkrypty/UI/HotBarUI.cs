using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace ElementumDefense.UI
{
    /// <summary>
    /// Displays turret hotbar (keys 1-5)
    /// Shows: Icon, name, cost, affordability
    /// </summary>
    public class HotbarUI : MonoBehaviour
    {
        [Header("Hotbar Configuration")]
        [SerializeField] private TurretData[] turretHotbar; // 5 turrets (assign in Inspector)

        [Header("UI Slots (assign from Hierarchy)")]
        [SerializeField] private HotbarSlot[] slots; // Drag 5 slot GameObjects here

        [Header("Color Coding")]
        [SerializeField] private Color affordableColor = Color.green;
        [SerializeField] private Color partiallyAffordableColor = Color.yellow; // 50-99% gold
        [SerializeField] private Color cannotAffordColor = Color.red;
        [SerializeField] private Color selectedColor = Color.cyan; // When in build mode

        private PlayerGold playerGold;
        private BuildManager buildManager;
        private int selectedSlotIndex = -1;

        private bool isInitialized = false;
        private float initializationRetryTimer = 0f;
        private const float RETRY_INTERVAL = 0.5f;
        private void Start()
        {
            Debug.Log("[HotbarUI] Waiting for player to spawn...");
        }

        private void Update()
        {
            if (!isInitialized)
            {
                initializationRetryTimer += Time.deltaTime;

                if (initializationRetryTimer >= RETRY_INTERVAL)
                {
                    initializationRetryTimer = 0f;
                    TryInitialize();
                }

                return; // Don't update until initialized
            }
            // Update every frame (gold changes frequently)
            UpdateDisplay();

            // Highlight selected slot
            UpdateSelectedSlot();
        }
        /// <summary>
        /// Attempts to initialize (called repeatedly until successful)
        /// </summary>
        private void TryInitialize()
        {
            // Find player components
            playerGold = PlayerGold.LocalInstance;
            buildManager = FindObjectOfType<BuildManager>();

            if (playerGold == null)
            {
                Debug.LogWarning("[HotbarUI] Still waiting for PlayerGold...");
                return;
            }

            if (buildManager == null)
            {
                Debug.LogWarning("[HotbarUI] BuildManager not found!");
                return;
            }

            // Success! Initialize
            InitializeSlots();
            UpdateDisplay();
            isInitialized = true;

            Debug.Log("[HotbarUI]  Successfully initialized!");
        }
        /// <summary>
        /// Sets up each slot with turret data
        /// </summary>
        private void InitializeSlots()
        {
            if (slots == null || slots.Length == 0)
            {
                Debug.LogError("[HotbarUI] No slots assigned!");
                return;
            }

            for (int i = 0; i < slots.Length && i < turretHotbar.Length; i++)
            {
                if (turretHotbar[i] != null)
                {
                    slots[i].Initialize(turretHotbar[i], i + 1); // Hotkey = index + 1
                }
                else
                {
                    slots[i].gameObject.SetActive(false); // Hide empty slots
                }
            }

            Debug.Log($"[HotbarUI] Initialized {slots.Length} hotbar slots");
        }

        /// <summary>
        /// Updates all slots (color coding based on gold)
        /// </summary>
        private void UpdateDisplay()
        {
            if (playerGold == null) return;

            int currentGold = playerGold.GetGold();

            for (int i = 0; i < slots.Length && i < turretHotbar.Length; i++)
            {
                if (turretHotbar[i] == null) continue;

                int cost = turretHotbar[i].cost;
                Color slotColor = GetAffordabilityColor(currentGold, cost);

                slots[i].UpdateColor(slotColor);
            }
        }

        /// <summary>
        /// Highlights currently selected turret
        /// </summary>
        private void UpdateSelectedSlot()
        {
            if (buildManager == null) return;

            // Check if in build mode
            bool inBuildMode = buildManager.IsInBuildMode();

            if (inBuildMode)
            {
                // Find which turret is selected (TODO: BuildManager needs to expose this)
                // For now, just highlight based on recent keypress
                for (int i = 0; i < slots.Length; i++)
                {
                    if (i == selectedSlotIndex)
                    {
                        slots[i].SetSelected(true);
                    }
                    else
                    {
                        slots[i].SetSelected(false);
                    }
                }
            }
            else
            {
                // Clear all selections
                foreach (var slot in slots)
                {
                    slot.SetSelected(false);
                }
                selectedSlotIndex = -1;
            }
        }

        /// <summary>
        /// Returns color based on affordability
        /// </summary>
        private Color GetAffordabilityColor(int gold, int cost)
        {
            if (gold >= cost)
            {
                return affordableColor; // Can afford
            }
            else if (gold >= cost * 0.5f)
            {
                return partiallyAffordableColor; // 50-99%
            }
            else
            {
                return cannotAffordColor; // Cannot afford
            }
        }

        /// <summary>
        /// Called when hotkey is pressed (from BuildManager)
        /// </summary>
        public void OnHotkeyPressed(int slotIndex)
        {
            selectedSlotIndex = slotIndex;
        }
    }
}