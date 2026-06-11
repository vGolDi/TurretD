// Assets/PrzemekSkrypty/Skins/SkinInventory.cs
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ElementumDefense.Auth;
using ElementumDefense.Multiplayer;
using ElementumDefense.Players;
using ElementumDefense.Turrets;

namespace ElementumDefense.Skins
{
    /// <summary>
    /// Manages player's owned skins and equipped skins.
    /// Cloud-synced via PlayFab. No local file storage.
    /// Purchasing is handled by ShopManager — this class only tracks ownership.
    /// </summary>
    public class SkinInventory : MonoBehaviour
    {
        public static SkinInventory Instance { get; private set; }

        [Header("All Skins (auto-loaded from Resources/Skins/)")]
        [SerializeField]
        private List<SkinData> allSkins = new List<SkinData>();

        [Header("Skin Model Settings")]
        [Tooltip("Tag on the child object that holds the visual model (for prefab swap)")]
        [SerializeField] private string modelChildTag = "PlayerModel";

        [Tooltip("If no tag found, swap the first child with this name")]
        [SerializeField] private string modelChildName = "Model";

        // Runtime state
        private List<string> ownedSkinIds = new List<string>();
        private Dictionary<string, string> equippedSkins = new Dictionary<string, string>();
        // Key = targetId (e.g., "PlayerCharacter"), Value = skinId

        // ==========================================
        // EVENTS
        // ==========================================

        /// <summary>Fired when a skin is purchased/unlocked</summary>
        public event Action<SkinData> OnSkinUnlocked;

        /// <summary>Fired when a skin is equipped/unequipped</summary>
        public event Action<SkinData, string> OnSkinEquipped; // skin, targetId

        /// <summary>Fired when inventory data is loaded</summary>
        public event Action OnInventoryLoaded;

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

            AutoLoadAllSkins();
        }

        private void Start()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnCloudReady += OnUserLoggedIn;
            }
        }

        private void OnDestroy()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnCloudReady -= OnUserLoggedIn;
            }
            if (Instance == this) Instance = null;
        }

        private void OnUserLoggedIn(string username)
        {
            Debug.Log($"[SkinInventory] User {username} logged in - loading skins");
            ownedSkinIds.Clear();
            equippedSkins.Clear();
            LoadFromCloud();
        }

        private void AutoLoadAllSkins()
        {
            SkinData[] loaded = Resources.LoadAll<SkinData>("Skins");
            if (loaded.Length > 0)
            {
                allSkins.Clear();
                allSkins.AddRange(loaded);
                Debug.Log($"[SkinInventory] Auto-loaded {loaded.Length} skins from Resources/Skins/");
            }
            else
            {
                Debug.LogWarning("[SkinInventory] No SkinData found in Resources/Skins/");
            }
        }

        // ==========================================
        // PUBLIC API - QUERIES
        // ==========================================

        /// <summary>Get all skins available in the game</summary>
        public List<SkinData> GetAllSkins() => new List<SkinData>(allSkins);

        /// <summary>Get all skins for a specific target (e.g., all Fire Turret skins)</summary>
        public List<SkinData> GetSkinsForTarget(string targetId)
        {
            return allSkins.Where(s => s.targetId == targetId).ToList();
        }

        /// <summary>Get all skins by category</summary>
        public List<SkinData> GetSkinsByCategory(SkinCategory category)
        {
            return allSkins.Where(s => s.category == category).ToList();
        }

        /// <summary>Get all owned skins</summary>
        public List<SkinData> GetOwnedSkins()
        {
            return allSkins.Where(s => ownedSkinIds.Contains(s.skinId) || s.isDefault).ToList();
        }

        /// <summary>Check if player owns a specific skin</summary>
        public bool OwnsSkin(string skinId)
        {
            SkinData skin = allSkins.FirstOrDefault(s => s.skinId == skinId);
            if (skin != null && skin.isDefault) return true;
            return ownedSkinIds.Contains(skinId);
        }

        /// <summary>Check if player owns a specific skin</summary>
        public bool OwnsSkin(SkinData skin)
        {
            if (skin == null) return false;
            if (skin.isDefault) return true;
            return ownedSkinIds.Contains(skin.skinId);
        }

        /// <summary>Get the currently equipped skin for a target entity</summary>
        public SkinData GetEquippedSkin(string targetId)
        {
            if (equippedSkins.TryGetValue(targetId, out string skinId))
            {
                return allSkins.FirstOrDefault(s => s.skinId == skinId);
            }
            // Return default skin for this target if exists
            return allSkins.FirstOrDefault(s => s.targetId == targetId && s.isDefault);
        }

        /// <summary>Check if a specific skin is currently equipped</summary>
        public bool IsSkinEquipped(string skinId)
        {
            return equippedSkins.ContainsValue(skinId);
        }

        // ==========================================
        // PUBLIC API - UNLOCK (called by ShopManager)
        // ==========================================

        /// <summary>Unlock a skin (called by ShopManager after purchase)</summary>
        public void UnlockSkin(SkinData skin)
        {
            if (skin == null || OwnsSkin(skin)) return;

            ownedSkinIds.Add(skin.skinId);
            OnSkinUnlocked?.Invoke(skin);
            SaveToCloud();

            Debug.Log($"[SkinInventory] Unlocked: {skin.skinName}");
        }

        /// <summary>Unlock a skin by ID</summary>
        public void UnlockSkin(string skinId)
        {
            SkinData skin = allSkins.FirstOrDefault(s => s.skinId == skinId);
            if (skin != null) UnlockSkin(skin);
        }

        // ==========================================
        // PUBLIC API - EQUIP
        // ==========================================

        /// <summary>Equip a skin for its target entity</summary>
        public bool EquipSkin(SkinData skin)
        {
            if (skin == null) return false;
            if (!OwnsSkin(skin))
            {
                Debug.LogWarning($"[SkinInventory] Cannot equip '{skin.skinName}' - not owned!");
                return false;
            }

            equippedSkins[skin.targetId] = skin.skinId;
            OnSkinEquipped?.Invoke(skin, skin.targetId);
            SaveToCloud();

            Debug.Log($"[SkinInventory] Equipped '{skin.skinName}' on '{skin.targetId}'");
            return true;
        }

        /// <summary>Unequip skin from a target (revert to default)</summary>
        public void UnequipSkin(string targetId)
        {
            if (equippedSkins.ContainsKey(targetId))
            {
                equippedSkins.Remove(targetId);
                SaveToCloud();

                // Find and fire event with default skin
                SkinData defaultSkin = allSkins.FirstOrDefault(
                    s => s.targetId == targetId && s.isDefault);
                OnSkinEquipped?.Invoke(defaultSkin, targetId);

                Debug.Log($"[SkinInventory] Unequipped skin from '{targetId}'");
            }
        }

        // ==========================================
        // SAVE/LOAD - CLOUD ONLY
        // ==========================================

        [Serializable]
        private class SkinSaveData
        {
            public List<string> ownedIds = new List<string>();
            public List<EquippedEntry> equipped = new List<EquippedEntry>();
        }

        [Serializable]
        private class EquippedEntry
        {
            public string targetId;
            public string skinId;
        }

        private void SaveToCloud()
        {
            SkinSaveData saveData = new SkinSaveData
            {
                ownedIds = new List<string>(ownedSkinIds)
            };

            foreach (var kvp in equippedSkins)
            {
                saveData.equipped.Add(new EquippedEntry
                {
                    targetId = kvp.Key,
                    skinId = kvp.Value
                });
            }

            string json = JsonUtility.ToJson(saveData, true);

            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.SaveData("SkinInventoryData", json);
            }
            else
            {
                Debug.LogWarning("[SkinInventory] CloudSaveManager is null - data NOT saved!");
            }
        }

        private void LoadFromCloud()
        {
            if (CloudSaveManager.Instance != null)
            {
                Debug.Log("[SkinInventory] Loading skins from PlayFab cloud...");
                CloudSaveManager.Instance.LoadData("SkinInventoryData",
                    json =>
                    {
                        Debug.Log("[SkinInventory] Cloud data loaded OK.");
                        ProcessLoadedJson(json);
                    },
                    () =>
                    {
                        Debug.Log("[SkinInventory] No cloud data - fresh skin inventory.");
                        UnlockDefaultSkins();
                        OnInventoryLoaded?.Invoke();
                    });
            }
            else
            {
                Debug.LogWarning("[SkinInventory] CloudSaveManager is null!");
                UnlockDefaultSkins();
                OnInventoryLoaded?.Invoke();
            }
        }

        private void ProcessLoadedJson(string json)
        {
            try
            {
                SkinSaveData saveData = JsonUtility.FromJson<SkinSaveData>(json);

                ownedSkinIds = saveData.ownedIds ?? new List<string>();

                equippedSkins.Clear();
                if (saveData.equipped != null)
                {
                    foreach (var entry in saveData.equipped)
                    {
                        equippedSkins[entry.targetId] = entry.skinId;
                    }
                }

                Debug.Log($"[SkinInventory] Loaded: {ownedSkinIds.Count} owned skins, " +
                          $"{equippedSkins.Count} equipped");

                OnInventoryLoaded?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SkinInventory] Failed to parse JSON: {e.Message}");
                UnlockDefaultSkins();
                OnInventoryLoaded?.Invoke();
            }
        }

        private void UnlockDefaultSkins()
        {
            foreach (var skin in allSkins.Where(s => s.isDefault))
            {
                if (!ownedSkinIds.Contains(skin.skinId))
                    ownedSkinIds.Add(skin.skinId);
            }
        }

        // ==========================================
        // SKIN APPLICATION (used by game systems)
        // ==========================================

        /// <summary>
        /// Apply the equipped skin to a target GameObject.
        /// Handles: prefab swap (child model), material swap, tint.
        /// Call this when spawning a turret/character.
        /// Returns the SkinData that was applied (or null if no skin).
        /// </summary>
        public SkinData ApplySkin(string targetId, GameObject target)
        {
            SkinData skin = GetEquippedSkin(targetId);
            if (skin == null || skin.isDefault) return null;

            Debug.Log($"[SkinInventory] Applying skin '{skin.skinName}' to '{target.name}'");

            // === 1. PREFAB SWAP: Replace child model ===
            if (skin.skinPrefab != null)
            {
                SwapModel(target, skin.skinPrefab);
            }

            // === 2. MATERIAL SWAP ===
            if (skin.skinMaterial != null)
            {
                ApplyMaterial(target, skin.skinMaterial);
            }

            // === 3. TINT ===
            if (skin.skinTint != Color.white)
            {
                ApplyTint(target, skin.skinTint);
            }

            return skin;
        }

        /// <summary>
        /// Swaps the visual model child of a target object.
        /// Looks for child by tag (modelChildTag) or name (modelChildName).
        /// If neither found, replaces the first child that has a Renderer.
        /// </summary>
        private void SwapModel(GameObject target, GameObject newModelPrefab)
        {
            Transform oldModel = null;

            // Strategy 1: Find by tag
            if (!string.IsNullOrEmpty(modelChildTag))
            {
                foreach (Transform child in target.transform)
                {
                    if (child.CompareTag(modelChildTag))
                    {
                        oldModel = child;
                        break;
                    }
                }
            }

            // Strategy 2: Find by name
            if (oldModel == null && !string.IsNullOrEmpty(modelChildName))
            {
                oldModel = target.transform.Find(modelChildName);
            }

            // Strategy 3: Find first child with a Renderer
            if (oldModel == null)
            {
                foreach (Transform child in target.transform)
                {
                    if (child.GetComponentInChildren<Renderer>() != null)
                    {
                        oldModel = child;
                        break;
                    }
                }
            }

            if (oldModel != null)
            {
                // Remember transform and original name
                string originalName = oldModel.name;
                Vector3 localPos = oldModel.localPosition;
                Quaternion localRot = oldModel.localRotation;
                Vector3 localScale = oldModel.localScale;

                // === CAPTURE REFERENCES before Destroy ===
                // Other components (e.g., TDCameraController) may have Transform
                // references pointing to bones INSIDE the old model.
                // We collect them so we can re-link after spawning the new model.
                var refsToRelink = new List<(Component comp, System.Reflection.FieldInfo field, string boneName)>();

                // Find all sibling components that have a Transform field pointing
                // into the old model
                MonoBehaviour[] siblings = target.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var comp in siblings)
                {
                    if (comp == null || comp.gameObject == oldModel.gameObject) continue;

                    var fields = comp.GetType().GetFields(
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic);

                    foreach (var field in fields)
                    {
                        if (field.FieldType != typeof(Transform)) continue;

                        Transform val = field.GetValue(comp) as Transform;
                        if (val != null && val.IsChildOf(oldModel))
                        {
                            refsToRelink.Add((comp, field, val.name));
                            Debug.Log($"[SkinInventory] Will re-link: {comp.GetType().Name}.{field.Name} → bone '{val.name}'");
                        }
                    }
                }

                // DESTROY old model
                Destroy(oldModel.gameObject);

                // Spawn new model as child of root
                GameObject newModel = Instantiate(newModelPrefab, target.transform);
                newModel.transform.localPosition = localPos;
                newModel.transform.localRotation = localRot;
                newModel.transform.localScale = localScale;

                // CRITICAL: Name must be IDENTICAL to the old model.
                // Animator resolves bones by path: "Root/Model/mixamorig:Hips/..."
                newModel.name = originalName;

                // === RE-LINK REFERENCES ===
                foreach (var (comp, field, boneName) in refsToRelink)
                {
                    Transform newBone = FindChildRecursive(newModel.transform, boneName);
                    if (newBone != null)
                    {
                        field.SetValue(comp, newBone);
                        Debug.Log($"[SkinInventory] Re-linked: {comp.GetType().Name}.{field.Name} → new '{boneName}'");
                    }
                    else
                    {
                        // Fallback: point to the root of new model
                        field.SetValue(comp, newModel.transform);
                        Debug.LogWarning($"[SkinInventory] Bone '{boneName}' not found in new model, falling back to root");
                    }
                }

                // === ANIMATOR FIX ===
                Animator rootAnimator = target.GetComponent<Animator>();
                Animator newModelAnimator = newModel.GetComponent<Animator>();

                if (rootAnimator != null && newModelAnimator != null)
                {
                    rootAnimator.avatar = newModelAnimator.avatar;
                    newModelAnimator.enabled = false;
                }

                // Rebind after old model is destroyed (next frame)
                StartCoroutine(RebindAnimatorNextFrame(target));

                Debug.Log($"[SkinInventory] Swapped model: '{originalName}' → skin (kept name '{originalName}')");
            }
            else
            {
                Debug.LogWarning($"[SkinInventory] No model child found on '{target.name}' to swap!");
            }
        }

        /// <summary>Recursively find a child transform by name</summary>
        private Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                Transform found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private System.Collections.IEnumerator RebindAnimatorNextFrame(GameObject target)
        {
            // Wait for Destroy to complete (happens at end of frame)
            yield return null;

            Animator rootAnimator = target.GetComponent<Animator>();
            if (rootAnimator != null)
            {
                rootAnimator.Rebind();
                Debug.Log($"[SkinInventory] Animator rebound (next frame): avatar={rootAnimator.avatar?.name}, controller={rootAnimator.runtimeAnimatorController?.name}");
            }
        }

        /// <summary>Apply a material to all renderers on the target</summary>
        private void ApplyMaterial(GameObject target, Material material)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                Material[] mats = renderer.materials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = material;
                renderer.materials = mats;
            }
            Debug.Log($"[SkinInventory] Applied material to {renderers.Length} renderers");
        }

        /// <summary>Apply a color tint to all renderers on the target</summary>
        private void ApplyTint(GameObject target, Color tint)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat.HasProperty("_Color"))
                        mat.color = tint;
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", tint);
                }
            }
            Debug.Log($"[SkinInventory] Applied tint to {renderers.Length} renderers");
        }

        /// <summary>
        /// Get the skin prefab override for a target.
        /// Returns null if no skin equipped or skin has no prefab override.
        /// </summary>
        public GameObject GetSkinPrefab(string targetId)
        {
            SkinData skin = GetEquippedSkin(targetId);
            return skin?.skinPrefab;
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Print Skin Inventory")]
        private void DebugPrintInventory()
        {
            Debug.Log("=== SKIN INVENTORY ===");
            Debug.Log($"Owned: {ownedSkinIds.Count} skins");
            foreach (string id in ownedSkinIds)
            {
                SkinData skin = allSkins.FirstOrDefault(s => s.skinId == id);
                string equipped = equippedSkins.ContainsValue(id) ? " [EQUIPPED]" : "";
                Debug.Log($"  - {skin?.skinName ?? id} ({skin?.rarity}){equipped}");
            }

            Debug.Log($"Equipped mappings: {equippedSkins.Count}");
            foreach (var kvp in equippedSkins)
            {
                Debug.Log($"  {kvp.Key} -> {kvp.Value}");
            }
        }

        [ContextMenu("Unlock All Skins (DEBUG)")]
        private void DebugUnlockAll()
        {
            foreach (var skin in allSkins)
            {
                if (!ownedSkinIds.Contains(skin.skinId))
                    ownedSkinIds.Add(skin.skinId);
            }
            SaveToCloud();
            Debug.Log($"[DEBUG] Unlocked all {allSkins.Count} skins");
        }

        [ContextMenu("RESET: Clear All Skin Data")]
        private void DebugClearAllSkinData()
        {
            ownedSkinIds.Clear();
            equippedSkins.Clear();
            SaveToCloud();
            Debug.Log("[DEBUG] Cleared ALL skin data (owned + equipped) and saved to cloud!");
        }

        [ContextMenu("RESET: Unequip All Skins")]
        private void DebugUnequipAll()
        {
            equippedSkins.Clear();
            SaveToCloud();
            Debug.Log("[DEBUG] Unequipped all skins and saved to cloud!");
        }

        [ContextMenu("RESET: Wipe Cloud Skin Data")]
        private void DebugWipeCloudData()
        {
            ownedSkinIds.Clear();
            equippedSkins.Clear();

            // Save empty data to cloud
            string emptyJson = JsonUtility.ToJson(new SkinSaveData(), true);
            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.SaveData("SkinInventoryData", emptyJson);
                Debug.Log("[DEBUG] Wiped SkinInventoryData from PlayFab cloud!");
            }
            else
            {
                Debug.LogWarning("[DEBUG] CloudSaveManager is null - cannot wipe cloud data!");
            }
        }
    }
}
