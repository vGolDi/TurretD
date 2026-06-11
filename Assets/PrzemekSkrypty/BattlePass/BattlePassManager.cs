// Assets/PrzemekSkrypty/BattlePass/BattlePassManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ElementumDefense.Auth;
using ElementumDefense.Cards;
using ElementumDefense.Lootbox;
using ElementumDefense.Skins;
using ElementumDefense.Multiplayer;
using ElementumDefense.Players;

namespace ElementumDefense.BattlePass
{
    /// <summary>
    /// Core Battle Pass manager. Handles XP tracking, tier progression,
    /// reward claiming, premium purchase, and cloud save/load.
    /// 
    /// Singleton — lives on its own GameObject (auto-created by AuthManager pattern).
    /// </summary>
    public class BattlePassManager : MonoBehaviour
    {
        public static BattlePassManager Instance { get; private set; }

        [Header("Season Config")]
        [SerializeField, Tooltip("Current active season SO. Swap this to change seasons.")]
        private BattlePassSeasonData currentSeason;

        [Header("Auto-Load")]
        [SerializeField, Tooltip("Auto-load season from Resources/BattlePass/ if not assigned")]
        private bool autoLoadFromResources = true;

        // ==========================================
        // EVENTS
        // ==========================================

        /// <summary>Fired when BP XP changes. Passes (currentXP, currentTier).</summary>
        public event Action<int, int> OnXPChanged;

        /// <summary>Fired when player reaches a new tier. Passes new tier number.</summary>
        public event Action<int> OnTierReached;

        /// <summary>Fired when a reward is claimed. Passes (tierNumber, isPremium).</summary>
        public event Action<int, bool> OnRewardClaimed;

        /// <summary>Fired when premium is purchased.</summary>
        public event Action OnPremiumPurchased;

        /// <summary>Fired when BP data is loaded/refreshed.</summary>
        public event Action OnBattlePassLoaded;

        // ==========================================
        // RUNTIME STATE
        // ==========================================

        private BattlePassSaveData saveData;
        private const string SAVE_KEY = "BattlePassData";

        // ==========================================
        // PROPERTIES
        // ==========================================

        public BattlePassSeasonData CurrentSeason => currentSeason;
        public int CurrentXP => saveData?.currentXP ?? 0;
        public int CurrentTier => currentSeason != null ? currentSeason.GetTierForXP(CurrentXP) : 0;
        public bool HasPremium => saveData?.hasPremium ?? false;
        public string ActiveSeasonId => saveData?.seasonId ?? "";

        /// <summary>
        /// XP progress within the current tier (for progress bar).
        /// Returns (currentInTier, totalForTier).
        /// </summary>
        public (int current, int total) GetTierProgress()
        {
            if (currentSeason == null) return (0, 1);

            int tier = CurrentTier;
            int nextTier = tier + 1;

            if (nextTier > currentSeason.TotalTiers)
                return (0, 0); // Max tier reached

            int xpForCurrentTier = tier > 0 ? currentSeason.GetXPForTier(tier) : 0;
            int xpForNextTier = currentSeason.GetXPForTier(nextTier);

            int progressInTier = CurrentXP - xpForCurrentTier;
            int totalForTier = xpForNextTier - xpForCurrentTier;

            return (Mathf.Max(0, progressInTier), Mathf.Max(1, totalForTier));
        }

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

            if (autoLoadFromResources && currentSeason == null)
                AutoLoadSeason();

            saveData = new BattlePassSaveData();
        }

        private void Start()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnCloudReady += OnUserLoggedIn;
            }
            else
            {
                // Offline/testing fallback
                LoadData();
            }
        }

        private void OnDestroy()
        {
            if (AuthManager.Instance != null)
                AuthManager.Instance.OnCloudReady -= OnUserLoggedIn;

            if (Instance == this) Instance = null;
        }

        private void OnUserLoggedIn(string username)
        {
            Debug.Log($"[BattlePass] User '{username}' logged in — loading BP data");
            LoadData();
        }

        private void AutoLoadSeason()
        {
            var seasons = Resources.LoadAll<BattlePassSeasonData>("BattlePass");
            if (seasons.Length > 0)
            {
                // Pick the active season, or the first one
                currentSeason = seasons.FirstOrDefault(s => s.IsActive()) ?? seasons[0];
                Debug.Log($"[BattlePass] Auto-loaded season: {currentSeason.seasonName}");
            }
            else
            {
                Debug.LogWarning("[BattlePass] No BattlePassSeasonData found in Resources/BattlePass/");
            }
        }

        // ==========================================
        // XP API
        // ==========================================

        /// <summary>
        /// Adds XP to the Battle Pass. Called from GameEndManager and QuestManager.
        /// </summary>
        public void AddXP(int amount)
        {
            if (amount <= 0) return;
            if (currentSeason == null) return;
            if (!currentSeason.IsActive()) return;

            int previousTier = CurrentTier;
            saveData.currentXP += amount;

            int newTier = CurrentTier;

            // Fire events
            OnXPChanged?.Invoke(saveData.currentXP, newTier);

            if (newTier > previousTier)
            {
                for (int t = previousTier + 1; t <= newTier; t++)
                {
                    OnTierReached?.Invoke(t);
                    Debug.Log($"[BattlePass] Reached tier {t}!");
                }
            }

            SaveData();
            Debug.Log($"[BattlePass] +{amount} XP → Total: {saveData.currentXP}, Tier: {newTier}");
        }

        /// <summary>
        /// Awards BP XP for completing a match.
        /// Called from GameEndManager.
        /// </summary>
        public void AwardMatchXP(bool isVictory)
        {
            if (currentSeason == null) return;

            int xp = isVictory ? currentSeason.xpPerMatchWin : currentSeason.xpPerMatchLoss;
            if (xp > 0)
                AddXP(xp);
        }

        // ==========================================
        // REWARD CLAIMING
        // ==========================================

        /// <summary>
        /// Claims the free reward for a specific tier.
        /// </summary>
        public bool ClaimFreeReward(int tierNumber)
        {
            if (!CanClaimFreeReward(tierNumber)) return false;

            var tier = currentSeason.GetTier(tierNumber);
            if (tier?.freeReward == null) return false;

            GrantReward(tier.freeReward);
            saveData.claimedFreeTiers.Add(tierNumber);

            OnRewardClaimed?.Invoke(tierNumber, false);
            SaveData();

            Debug.Log($"[BattlePass] Claimed FREE reward for tier {tierNumber}: {tier.freeReward.GetDisplayName()}");
            return true;
        }

        /// <summary>
        /// Claims the premium reward for a specific tier.
        /// </summary>
        public bool ClaimPremiumReward(int tierNumber)
        {
            if (!CanClaimPremiumReward(tierNumber)) return false;

            var tier = currentSeason.GetTier(tierNumber);
            if (tier?.premiumReward == null) return false;

            GrantReward(tier.premiumReward);
            saveData.claimedPremiumTiers.Add(tierNumber);

            OnRewardClaimed?.Invoke(tierNumber, true);
            SaveData();

            Debug.Log($"[BattlePass] Claimed PREMIUM reward for tier {tierNumber}: {tier.premiumReward.GetDisplayName()}");
            return true;
        }

        /// <summary>
        /// Claims ALL available unclaimed rewards up to current tier.
        /// </summary>
        public void ClaimAllAvailable()
        {
            int tier = CurrentTier;
            for (int i = 1; i <= tier; i++)
            {
                ClaimFreeReward(i);
                if (HasPremium)
                    ClaimPremiumReward(i);
            }
        }

        // ==========================================
        // CLAIM CHECKS
        // ==========================================

        public bool CanClaimFreeReward(int tierNumber)
        {
            if (currentSeason == null) return false;
            if (tierNumber <= 0 || tierNumber > currentSeason.TotalTiers) return false;
            if (CurrentTier < tierNumber) return false; // Not reached yet
            if (saveData.claimedFreeTiers.Contains(tierNumber)) return false; // Already claimed
            var tier = currentSeason.GetTier(tierNumber);
            return tier?.freeReward != null;
        }

        public bool CanClaimPremiumReward(int tierNumber)
        {
            if (currentSeason == null) return false;
            if (!HasPremium) return false;
            if (tierNumber <= 0 || tierNumber > currentSeason.TotalTiers) return false;
            if (CurrentTier < tierNumber) return false;
            if (saveData.claimedPremiumTiers.Contains(tierNumber)) return false;
            var tier = currentSeason.GetTier(tierNumber);
            return tier?.premiumReward != null;
        }

        public bool IsFreeClaimed(int tierNumber) => saveData.claimedFreeTiers.Contains(tierNumber);
        public bool IsPremiumClaimed(int tierNumber) => saveData.claimedPremiumTiers.Contains(tierNumber);

        // ==========================================
        // PREMIUM PURCHASE
        // ==========================================

        /// <summary>
        /// Purchases the premium Battle Pass using Crystals.
        /// Returns true if successful.
        /// </summary>
        public bool PurchasePremium()
        {
            if (HasPremium)
            {
                Debug.Log("[BattlePass] Already owns premium!");
                return false;
            }

            if (currentSeason == null) return false;

            var player = PlayerCollection.Instance;
            if (player == null) return false;

            int price = currentSeason.premiumPriceCrystals;
            if (!player.CanAffordCrystals(price))
            {
                Debug.Log($"[BattlePass] Can't afford premium! Need {price} crystals.");
                return false;
            }

            player.AddCrystals(-price);
            saveData.hasPremium = true;

            OnPremiumPurchased?.Invoke();
            SaveData();

            Debug.Log($"[BattlePass] Premium purchased for {price} crystals!");
            return true;
        }

        /// <summary>
        /// Grants premium without payment (e.g., from shop bundle, promo code).
        /// </summary>
        public void GrantPremium()
        {
            if (HasPremium) return;
            saveData.hasPremium = true;
            OnPremiumPurchased?.Invoke();
            SaveData();
            Debug.Log("[BattlePass] Premium granted (free)!");
        }

        // ==========================================
        // REWARD GRANTING
        // ==========================================

        private void GrantReward(BattlePassRewardData reward)
        {
            if (reward == null) return;

            var player = PlayerCollection.Instance;
            if (player == null) return;

            switch (reward.rewardType)
            {
                case BPRewardType.Gold:
                    player.AddGold(reward.currencyAmount);
                    break;

                case BPRewardType.Crystals:
                    player.AddCrystals(reward.currencyAmount);
                    break;

                case BPRewardType.Lootbox:
                    if (reward.lootbox != null && LootboxInventory.Instance != null)
                        LootboxInventory.Instance.AddLootbox(reward.lootbox, reward.lootboxQuantity);
                    break;

                case BPRewardType.Skin:
                    if (reward.skin != null && SkinInventory.Instance != null)
                        SkinInventory.Instance.UnlockSkin(reward.skin.skinId);
                    break;
            }
        }

        // ==========================================
        // SEASON RESET
        // ==========================================

        /// <summary>
        /// Checks if the current save data matches the active season.
        /// If not, resets progress for the new season.
        /// </summary>
        private void CheckSeasonReset()
        {
            if (currentSeason == null) return;

            if (saveData.seasonId != currentSeason.seasonId)
            {
                Debug.Log($"[BattlePass] New season detected! '{saveData.seasonId}' → '{currentSeason.seasonId}'. Resetting progress.");
                saveData = new BattlePassSaveData
                {
                    seasonId = currentSeason.seasonId
                };
                SaveData();
            }
        }

        // ==========================================
        // SAVE / LOAD
        // ==========================================

        private void SaveData()
        {
            if (saveData == null) return;

            string json = JsonUtility.ToJson(saveData);

            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.SaveData(SAVE_KEY, json);
            }
            else
            {
                // Fallback: local save
                PlayerPrefs.SetString(SAVE_KEY, json);
                PlayerPrefs.Save();
            }
        }

        private void LoadData()
        {
            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.LoadData(SAVE_KEY,
                    json =>
                    {
                        saveData = JsonUtility.FromJson<BattlePassSaveData>(json) ?? new BattlePassSaveData();
                        CheckSeasonReset();
                        OnBattlePassLoaded?.Invoke();
                        Debug.Log($"[BattlePass] Loaded from cloud: XP={saveData.currentXP}, Premium={saveData.hasPremium}, Season={saveData.seasonId}");
                    },
                    () =>
                    {
                        // Not found — fresh start
                        saveData = new BattlePassSaveData
                        {
                            seasonId = currentSeason?.seasonId ?? ""
                        };
                        SaveData();
                        OnBattlePassLoaded?.Invoke();
                        Debug.Log("[BattlePass] No cloud data found — fresh start.");
                    });
            }
            else
            {
                // Fallback: local
                string json = PlayerPrefs.GetString(SAVE_KEY, "");
                if (!string.IsNullOrEmpty(json))
                    saveData = JsonUtility.FromJson<BattlePassSaveData>(json) ?? new BattlePassSaveData();
                else
                    saveData = new BattlePassSaveData { seasonId = currentSeason?.seasonId ?? "" };

                CheckSeasonReset();
                OnBattlePassLoaded?.Invoke();
            }
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("DEBUG: Add 500 BP XP")]
        public void DebugAdd500XP() => AddXP(500);

        [ContextMenu("DEBUG: Add 5000 BP XP")]
        public void DebugAdd5000XP() => AddXP(5000);

        [ContextMenu("DEBUG: Grant Premium")]
        public void DebugGrantPremium() => GrantPremium();

        [ContextMenu("DEBUG: Reset BP Data")]
        public void DebugReset()
        {
            saveData = new BattlePassSaveData { seasonId = currentSeason?.seasonId ?? "" };
            SaveData();
            OnBattlePassLoaded?.Invoke();
            Debug.Log("[BattlePass] DEBUG: Reset all progress.");
        }
    }

    // ==========================================
    // SAVE DATA
    // ==========================================

    [Serializable]
    public class BattlePassSaveData
    {
        public string seasonId = "";
        public int currentXP = 0;
        public bool hasPremium = false;
        public List<int> claimedFreeTiers = new List<int>();
        public List<int> claimedPremiumTiers = new List<int>();
    }
}
