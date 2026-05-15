// Assets/PrzemekSkrypty/Lootbox/LootboxInventory.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ElementumDefense.Auth; // DODANE - dla AuthManager

namespace ElementumDefense.Lootbox
{
    public class LootboxInventory : MonoBehaviour
    {
        public static LootboxInventory Instance { get; private set; }

        [Header("Available Lootbox Types")]
        [SerializeField]
        private List<LootboxData> allLootboxTypes = new List<LootboxData>();

        [Header("Player Inventory (Runtime)")]
        [SerializeField]
        private List<LootboxInventoryEntry> inventory = new List<LootboxInventoryEntry>();

        [Header("Save Settings")]
        [SerializeField] private bool autoSave = true;

        // Events
        public System.Action<LootboxData, int> OnLootboxAdded;
        public System.Action<LootboxData, int> OnLootboxRemoved;
        public System.Action OnInventoryChanged;

        // ==========================================
        // INITIALIZATION
        // ==========================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            AutoLoadLootboxTypes();
        }

        private void Start()
        {
            // KLUCZOWE: Subskrybuj si� na event logowania
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnCloudReady += OnUserLoggedIn;

                // Je�li u�ytkownik ju� jest zalogowany (np. po przej�ciu mi�dzy scenami)
                // OnCloudReady will fire after login verification
            }
            else
            {
                // Fallback dla test�w bez systemu logowania
                Debug.LogWarning("[LootboxInventory] AuthManager not found - using default save");
                LoadInventory();
            }
        }

        private void OnDestroy()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnCloudReady -= OnUserLoggedIn;
            }
        }

        /// <summary>
        /// Called when user logs in - loads their lootbox inventory
        /// </summary>
        private void OnUserLoggedIn(string username)
        {
            Debug.Log($"[LootboxInventory] User {username} logged in - loading their lootboxes");

            // Wyczy�� stary inventory
            inventory.Clear();

            // Za�aduj inventory tego u�ytkownika
            LoadInventory();

            OnInventoryChanged?.Invoke();
        }

        private void AutoLoadLootboxTypes()
        {
            if (allLootboxTypes.Count > 0) return;

            LootboxData[] loaded = Resources.LoadAll<LootboxData>("Lootboxes");

            if (loaded.Length > 0)
            {
                allLootboxTypes.AddRange(loaded);
                Debug.Log($"[LootboxInventory] Loaded {loaded.Length} lootbox types from Resources");
            }
            else
            {
                Debug.LogWarning("[LootboxInventory] No lootbox types found in Resources/Lootboxes/");
            }
        }

        // ==========================================
        // INVENTORY MANAGEMENT
        // ==========================================

        public void AddLootbox(LootboxData lootboxType, int count = 1)
        {
            if (lootboxType == null || count <= 0)
            {
                Debug.LogError("[LootboxInventory] Invalid lootbox or count!");
                return;
            }

            LootboxInventoryEntry entry = inventory.Find(e => e.lootboxType == lootboxType);

            if (entry == null)
            {
                entry = new LootboxInventoryEntry
                {
                    lootboxType = lootboxType,
                    count = 0
                };
                inventory.Add(entry);
            }

            entry.count += count;

            OnLootboxAdded?.Invoke(lootboxType, entry.count);
            OnInventoryChanged?.Invoke();

            Debug.Log($"[LootboxInventory] Added {count}x {lootboxType.lootboxName}. Total: {entry.count}");

            if (autoSave) SaveInventory();
        }

        public bool RemoveLootbox(LootboxData lootboxType, int count = 1)
        {
            if (lootboxType == null) return false;

            LootboxInventoryEntry entry = inventory.Find(e => e.lootboxType == lootboxType);

            if (entry == null || entry.count < count)
            {
                Debug.LogWarning($"[LootboxInventory] Not enough {lootboxType.lootboxName} to remove!");
                return false;
            }

            entry.count -= count;

            if (entry.count <= 0)
            {
                inventory.Remove(entry);
            }

            OnLootboxRemoved?.Invoke(lootboxType, entry?.count ?? 0);
            OnInventoryChanged?.Invoke();

            Debug.Log($"[LootboxInventory] Removed {count}x {lootboxType.lootboxName}. Remaining: {entry?.count ?? 0}");

            if (autoSave) SaveInventory();

            return true;
        }

        public int GetLootboxCount(LootboxData lootboxType)
        {
            if (lootboxType == null) return 0;
            LootboxInventoryEntry entry = inventory.Find(e => e.lootboxType == lootboxType);
            return entry?.count ?? 0;
        }

        public bool HasLootbox(LootboxData lootboxType)
        {
            return GetLootboxCount(lootboxType) > 0;
        }

        public int GetTotalLootboxCount()
        {
            return inventory.Sum(e => e.count);
        }

        public List<LootboxInventoryEntry> GetOwnedLootboxes()
        {
            return inventory.Where(e => e.count > 0).ToList();
        }

        public List<LootboxData> GetAllLootboxTypes()
        {
            return new List<LootboxData>(allLootboxTypes);
        }

        // ==========================================
        // SAVE/LOAD - PER USER!
        // ==========================================

        /// <summary>
        /// Gets save path for current user
        /// </summary>

        public void SaveInventory()
        {
            LootboxSaveData saveData = new LootboxSaveData();
            foreach (var entry in inventory)
            {
                if (entry.lootboxType == null) continue;
                saveData.entries.Add(new LootboxSaveEntry
                {
                    lootboxName = entry.lootboxType.name,
                    count = entry.count
                });
            }

            string json = JsonUtility.ToJson(saveData, true);

            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.SaveData("LootboxInventoryData", json);
            }
            else
            {
                Debug.LogWarning("[LootboxInventory] CloudSaveManager is null - data NOT saved!");
            }
        }

        public void LoadInventory()
        {
            inventory.Clear();
            if (CloudSaveManager.Instance != null)
            {
                Debug.Log("[LootboxInventory] Loading from PlayFab cloud...");
                CloudSaveManager.Instance.LoadData("LootboxInventoryData",
                    json =>
                    {
                        Debug.Log("[LootboxInventory] Cloud data loaded OK.");
                        ProcessLoadedLootboxJson(json);
                    },
                    () =>
                    {
                        Debug.Log("[LootboxInventory] No cloud data - empty inventory.");
                        OnInventoryChanged?.Invoke();
                    });
            }
            else
            {
                Debug.LogWarning("[LootboxInventory] CloudSaveManager is null!");
                OnInventoryChanged?.Invoke();
            }
        }

        private void ProcessLoadedLootboxJson(string json)
        {
            try
            {
                LootboxSaveData saveData = JsonUtility.FromJson<LootboxSaveData>(json);
                // Odtwarzamy ekwipunek skrzynek
                foreach (var entry in saveData.entries)
                {
                    LootboxData lootboxType = allLootboxTypes.Find(l => l.name == entry.lootboxName);

                    if (lootboxType != null)
                    {
                        inventory.Add(new LootboxInventoryEntry
                        {
                            lootboxType = lootboxType,
                            count = entry.count
                        });
                    }
                }

                OnInventoryChanged?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LootboxInventory] B��d wczytywania JSON: {e.Message}");
            }
        }
        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Add Test Common Lootbox")]
        private void AddTestCommon()
        {
            var common = allLootboxTypes.FirstOrDefault(l => l.rarity == LootboxRarity.Common);
            if (common != null) AddLootbox(common, 1);
        }

        [ContextMenu("Add Test Legendary Lootbox")]
        private void AddTestLegendary()
        {
            var legendary = allLootboxTypes.FirstOrDefault(l => l.rarity == LootboxRarity.Legendary);
            if (legendary != null) AddLootbox(legendary, 1);
        }

        [ContextMenu("Print Inventory")]
        private void PrintInventory()
        {
            Debug.Log($"=== LOOTBOX INVENTORY ({GetTotalLootboxCount()} total) ===");
            foreach (var entry in inventory)
            {
                Debug.Log($"  {entry.lootboxType.lootboxName}: {entry.count}x");
            }
        }

        [ContextMenu("Clear Inventory (This User)")]
        private void ClearInventory()
        {
            inventory.Clear();
            SaveInventory();
            OnInventoryChanged?.Invoke();
            Debug.Log("[LootboxInventory] Inventory cleared for current user");
        }
    }

    // ==========================================
    // HELPER CLASSES
    // ==========================================

    [System.Serializable]
    public class LootboxInventoryEntry
    {
        public LootboxData lootboxType;
        public int count;
    }

    [System.Serializable]
    public class LootboxSaveData
    {
        public List<LootboxSaveEntry> entries = new List<LootboxSaveEntry>();
    }

    [System.Serializable]
    public class LootboxSaveEntry
    {
        public string lootboxName;
        public int count;
    }
}
