using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using ElementumDefense.Auth;
using ElementumDefense.Multiplayer;

namespace ElementumDefense.Emotes
{
    /// <summary>
    /// Manages owned emotes, equipped wheel loadout, and cloud save.
    /// Singleton — lives on persistent manager object.
    /// 
    /// Wheel has 8 slots. Player equips emotes to slots from their collection.
    /// </summary>
    public class EmoteInventory : MonoBehaviour
    {
        public static EmoteInventory Instance { get; private set; }

        public const int WHEEL_SLOTS = 8;

        [Header("All Emotes")]
        [Tooltip("Master list of all emotes in the game")]
        [SerializeField] private List<EmoteData> allEmotes = new List<EmoteData>();

        [Header("Settings")]
        [SerializeField] private bool autoLoadFromResources = true;

        // Runtime state
        private HashSet<string> ownedEmoteIds = new HashSet<string>();
        private EmoteData[] wheelSlots = new EmoteData[WHEEL_SLOTS];

        // Events
        public System.Action<EmoteData> OnEmoteUnlocked;
        public System.Action OnWheelChanged;

        // ==========================================
        // LIFECYCLE
        // ==========================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (autoLoadFromResources)
            {
                var loaded = Resources.LoadAll<EmoteData>("Emotes");
                foreach (var e in loaded)
                {
                    if (!allEmotes.Contains(e))
                        allEmotes.Add(e);
                }
            }

            // Auto-own defaults
            foreach (var emote in allEmotes)
            {
                if (emote.isDefault)
                    ownedEmoteIds.Add(emote.emoteId);
            }
        }

        private void Start()
        {
            LoadFromCloud();
        }

        // ==========================================
        // QUERIES
        // ==========================================

        public List<EmoteData> GetAllEmotes() => new List<EmoteData>(allEmotes);

        public List<EmoteData> GetOwnedEmotes()
        {
            return allEmotes.Where(e => ownedEmoteIds.Contains(e.emoteId)).ToList();
        }

        public bool OwnsEmote(string emoteId) => ownedEmoteIds.Contains(emoteId);
        public bool OwnsEmote(EmoteData emote) => emote != null && ownedEmoteIds.Contains(emote.emoteId);

        /// <summary>Get the emote in a wheel slot (0-7). Null if empty.</summary>
        public EmoteData GetWheelSlot(int slot)
        {
            if (slot < 0 || slot >= WHEEL_SLOTS) return null;
            return wheelSlots[slot];
        }

        /// <summary>Get all wheel slots as array (for UI).</summary>
        public EmoteData[] GetWheelLoadout()
        {
            return (EmoteData[])wheelSlots.Clone();
        }

        // ==========================================
        // ACTIONS
        // ==========================================

        public void UnlockEmote(EmoteData emote)
        {
            if (emote == null) return;
            if (ownedEmoteIds.Add(emote.emoteId))
            {
                OnEmoteUnlocked?.Invoke(emote);
                SaveToCloud();
                Debug.Log($"[EmoteInventory] Unlocked: {emote.emoteName}");
            }
        }

        public void UnlockEmote(string emoteId)
        {
            var emote = allEmotes.FirstOrDefault(e => e.emoteId == emoteId);
            if (emote != null) UnlockEmote(emote);
        }

        /// <summary>Equip an emote to a wheel slot (0-7).</summary>
        public void EquipToSlot(EmoteData emote, int slot)
        {
            if (slot < 0 || slot >= WHEEL_SLOTS) return;
            if (emote != null && !OwnsEmote(emote))
            {
                Debug.LogWarning($"[EmoteInventory] Can't equip {emote.emoteName} — not owned!");
                return;
            }

            // Remove from other slot if already equipped
            if (emote != null)
            {
                for (int i = 0; i < WHEEL_SLOTS; i++)
                {
                    if (wheelSlots[i] != null && wheelSlots[i].emoteId == emote.emoteId)
                        wheelSlots[i] = null;
                }
            }

            wheelSlots[slot] = emote;
            OnWheelChanged?.Invoke();
            SaveToCloud();

            Debug.Log($"[EmoteInventory] Slot {slot} = {emote?.emoteName ?? "EMPTY"}");
        }

        /// <summary>Clear a wheel slot.</summary>
        public void ClearSlot(int slot)
        {
            EquipToSlot(null, slot);
        }

        // ==========================================
        // CLOUD SAVE
        // ==========================================

        [System.Serializable]
        private class EmoteSaveData
        {
            public List<string> ownedIds = new List<string>();
            public List<string> wheelSlotIds = new List<string>(); // 8 entries, "" = empty
        }

        private void SaveToCloud()
        {
            var saveData = new EmoteSaveData();
            saveData.ownedIds = ownedEmoteIds.ToList();

            saveData.wheelSlotIds = new List<string>();
            for (int i = 0; i < WHEEL_SLOTS; i++)
            {
                saveData.wheelSlotIds.Add(wheelSlots[i]?.emoteId ?? "");
            }

            string json = JsonUtility.ToJson(saveData, true);

            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.SaveData("EmoteInventoryData", json);
            }
            else
            {
                Debug.LogWarning("[EmoteInventory] CloudSaveManager null — data NOT saved!");
            }
        }

        private void LoadFromCloud()
        {
            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.LoadData("EmoteInventoryData",
                    (json) =>
                    {
                        if (!string.IsNullOrEmpty(json))
                        {
                            ApplyLoadedData(json);
                        }
                        else
                        {
                            SetupDefaultWheel();
                        }
                    },
                    () =>
                    {
                        SetupDefaultWheel();
                    });
            }
            else
            {
                SetupDefaultWheel();
            }
        }

        private void ApplyLoadedData(string json)
        {
            try
            {
                var saveData = JsonUtility.FromJson<EmoteSaveData>(json);

                ownedEmoteIds.Clear();
                foreach (string id in saveData.ownedIds)
                    ownedEmoteIds.Add(id);

                // Re-add defaults
                foreach (var emote in allEmotes)
                {
                    if (emote.isDefault)
                        ownedEmoteIds.Add(emote.emoteId);
                }

                // Load wheel
                for (int i = 0; i < WHEEL_SLOTS; i++)
                {
                    if (i < saveData.wheelSlotIds.Count && !string.IsNullOrEmpty(saveData.wheelSlotIds[i]))
                    {
                        wheelSlots[i] = allEmotes.FirstOrDefault(
                            e => e.emoteId == saveData.wheelSlotIds[i]);
                    }
                    else
                    {
                        wheelSlots[i] = null;
                    }
                }

                Debug.Log($"[EmoteInventory] Loaded: {ownedEmoteIds.Count} emotes, " +
                          $"{wheelSlots.Count(s => s != null)} wheel slots");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[EmoteInventory] Load failed: {ex.Message}");
                SetupDefaultWheel();
            }
        }

        private void SetupDefaultWheel()
        {
            var defaults = allEmotes.Where(e => e.isDefault).ToList();
            for (int i = 0; i < WHEEL_SLOTS && i < defaults.Count; i++)
            {
                wheelSlots[i] = defaults[i];
            }
            Debug.Log($"[EmoteInventory] Default wheel: {defaults.Count} emotes loaded");
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Unlock All Emotes")]
        private void DebugUnlockAll()
        {
            foreach (var e in allEmotes)
                ownedEmoteIds.Add(e.emoteId);
            SaveToCloud();
            Debug.Log($"[EmoteInventory] Unlocked all {allEmotes.Count} emotes");
        }

        [ContextMenu("Print Wheel")]
        private void DebugPrintWheel()
        {
            for (int i = 0; i < WHEEL_SLOTS; i++)
            {
                Debug.Log($"  Slot {i}: {wheelSlots[i]?.emoteName ?? "(empty)"}");
            }
        }
    }
}
