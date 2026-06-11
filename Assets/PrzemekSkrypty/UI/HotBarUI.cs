using UnityEngine;
using UnityEngine.UIElements;
using ElementumDefense.Players;
using ElementumDefense.Turrets;

namespace ElementumDefense.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class HotbarUI : MonoBehaviour
    {
        [Header("Hotbar Configuration")]
        [SerializeField]
        private TurretData[] turretHotbar;

        private VisualElement root;
        private VisualElement hotbarSlots;

        private PlayerGold playerGold;
        private BuildManager buildManager;
        private int selectedSlotIndex = -1;

        private bool isInitialized;
        private float retryTimer;
        private const float RETRY_INTERVAL = 0.5f;

        // Cached slot elements
        private VisualElement[] slotElements;

        private void Start()
        {
            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null) return;

            root = uiDoc.rootVisualElement;
            hotbarSlots =
                root.Q<VisualElement>("hotbar-slots");
        }

        private void Update()
        {
            if (!isInitialized)
            {
                retryTimer += Time.deltaTime;
                if (retryTimer >= RETRY_INTERVAL)
                {
                    retryTimer = 0f;
                    TryInitialize();
                }
                return;
            }

            UpdateDisplay();
            UpdateSelectedSlot();
        }

        private void TryInitialize()
        {
            playerGold = PlayerGold.LocalInstance;
            buildManager =
                FindFirstObjectByType<BuildManager>();

            if (playerGold == null) return;
            if (buildManager == null) return;

            BuildSlots();
            isInitialized = true;
            Debug.Log("[HotbarUI] Initialized");
        }

        // ==========================================
        // BUILD SLOTS
        // ==========================================

        private void BuildSlots()
        {
            if (hotbarSlots == null) return;
            hotbarSlots.Clear();

            if (turretHotbar == null) return;

            slotElements =
                new VisualElement[turretHotbar.Length];

            for (int i = 0; i < turretHotbar.Length; i++)
            {
                if (turretHotbar[i] == null) continue;

                var slot = BuildSlot(
                    turretHotbar[i], i);
                hotbarSlots.Add(slot);
                slotElements[i] = slot;
            }
        }

        private VisualElement BuildSlot(
            TurretData turret, int index)
        {
            var slot = new VisualElement();
            slot.AddToClassList("hotbar-slot");
            slot.name = $"hotbar-slot-{index}";

            // Hotkey label
            var key = new Label($"{index + 1}");
            key.AddToClassList("hotbar-key");
            slot.Add(key);

            // Icon
            if (turret.turretIcon != null)
            {
                var icon = new VisualElement();
                icon.AddToClassList("hotbar-icon");
                icon.style.backgroundImage =
                    new StyleBackground(
                        turret.turretIcon);
                slot.Add(icon);
            }

            // Name
            var name = new Label(turret.turretName);
            name.AddToClassList("hotbar-name");
            slot.Add(name);

            // Cost
            var cost = new Label(
                $"{turret.cost}");
            cost.AddToClassList("hotbar-cost");
            cost.name = $"cost-{index}";
            slot.Add(cost);

            // Bottom accent
            var accent = new VisualElement();
            accent.AddToClassList(
                "hotbar-slot-bottom-accent");
            slot.Add(accent);

            // Click handler
            int idx = index;
            slot.RegisterCallback<ClickEvent>(evt =>
            {
                OnSlotClicked(idx);
                evt.StopPropagation();
            });

            return slot;
        }

        private void OnSlotClicked(int index)
        {
            if (buildManager == null) return;
            if (turretHotbar == null) return;
            if (index < 0 ||
                index >= turretHotbar.Length) return;

            selectedSlotIndex = index;
            buildManager.SelectTurretToBuild(
                turretHotbar[index]);
        }
            
        // ==========================================
        // UPDATE DISPLAY
        // ==========================================

        private void UpdateDisplay()
        {
            if (playerGold == null) return;
            if (slotElements == null) return;

            int gold = playerGold.GetGold();

            for (int i = 0;
                i < slotElements.Length; i++)
            {
                if (slotElements[i] == null) continue;
                if (turretHotbar[i] == null) continue;

                var slot = slotElements[i];
                int cost = turretHotbar[i].cost;

                // Remove old classes
                slot.RemoveFromClassList(
                    "hotbar-slot-affordable");
                slot.RemoveFromClassList(
                    "hotbar-slot-partial");
                slot.RemoveFromClassList(
                    "hotbar-slot-expensive");

                // Cost label color
                var costLabel =
                    slot.Q<Label>($"cost-{i}");

                if (gold >= cost)
                {
                    slot.AddToClassList(
                        "hotbar-slot-affordable");
                    costLabel?.RemoveFromClassList(
                        "hotbar-cost-expensive");
                }
                else if (gold >= cost * 0.5f)
                {
                    slot.AddToClassList(
                        "hotbar-slot-partial");
                    costLabel?.RemoveFromClassList(
                        "hotbar-cost-expensive");
                }
                else
                {
                    slot.AddToClassList(
                        "hotbar-slot-expensive");
                    costLabel?.AddToClassList(
                        "hotbar-cost-expensive");
                }
            }
        }

        private void UpdateSelectedSlot()
        {
            if (buildManager == null) return;
            if (slotElements == null) return;

            bool inBuild = buildManager.IsInBuildMode();

            for (int i = 0;
                i < slotElements.Length; i++)
            {
                if (slotElements[i] == null) continue;

                if (inBuild && i == selectedSlotIndex)
                {
                    slotElements[i].AddToClassList(
                        "hotbar-slot-selected");
                }
                else
                {
                    slotElements[i].RemoveFromClassList(
                        "hotbar-slot-selected");
                }
            }

            if (!inBuild)
                selectedSlotIndex = -1;
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        public void OnHotkeyPressed(int slotIndex)
        {
            selectedSlotIndex = slotIndex;
        }
    }
}
