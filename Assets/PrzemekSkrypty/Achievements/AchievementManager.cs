// Assets/PrzemekSkrypty/Achievements/AchievementManager.cs
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ElementumDefense.Auth;
using ElementumDefense.Cards;

namespace ElementumDefense.Achievements
{
    /// <summary>
    /// Manages achievement progress and unlocks.
    /// Cloud-synced via PlayFab. Auto-tracks stats from game systems.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [Header("All Achievements (auto-loaded from Resources/Achievements/)")]
        [SerializeField]
        private List<AchievementData> allAchievements = new List<AchievementData>();

        // Runtime state
        private Dictionary<string, AchievementProgress> progressMap = new Dictionary<string, AchievementProgress>();

        // Cumulative stats (not directly available from PlayerCollection)
        private int totalGoldEarned = 0;
        private int totalGoldSpent = 0;
        private int totalCrystalsEarned = 0;
        private int totalLootboxesOpened = 0;
        private int totalQuestsCompleted = 0;

        // ==========================================
        // EVENTS
        // ==========================================

        /// <summary>Fired when an achievement is ready to claim</summary>
        public event Action<AchievementData, int> OnAchievementClaimable; // data, tier

        /// <summary>Fired when an achievement is claimed (reward given)</summary>
        public event Action<AchievementData, int> OnAchievementClaimed; // data, tier

        /// <summary>Fired when progress updates</summary>
        public event Action<AchievementData, int, int> OnProgressUpdated; // data, current, target

        /// <summary>Fired when data is loaded from cloud</summary>
        public event Action OnAchievementsLoaded;

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

            AutoLoadAllAchievements();
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
            Debug.Log($"[AchievementManager] User {username} logged in - loading achievements");
            progressMap.Clear();
            LoadFromCloud();

            // Re-check after delay — other managers (PlayerCollection) load async,
            // so stats may not be ready when our first check runs.
            StartCoroutine(DelayedAchievementCheck());
        }

        private IEnumerator DelayedAchievementCheck()
        {
            // Wait for other managers to finish loading from cloud
            yield return new WaitForSeconds(5f);
            Debug.Log("[AchievementManager] Delayed re-check of all achievements...");
            CheckAllAchievements();
        }

        private void AutoLoadAllAchievements()
        {
            AchievementData[] loaded = Resources.LoadAll<AchievementData>("Achievements");
            if (loaded.Length > 0)
            {
                allAchievements.Clear();
                allAchievements.AddRange(loaded);
                Debug.Log($"[AchievementManager] Auto-loaded {loaded.Length} achievements");
            }
            else
            {
                Debug.LogWarning("[AchievementManager] No AchievementData found in Resources/Achievements/");
            }
        }

        // ==========================================
        // PUBLIC API - QUERIES
        // ==========================================

        /// <summary>Get all achievement definitions</summary>
        public List<AchievementData> GetAllAchievements()
        {
            return allAchievements
                .Where(a => !a.isHidden || IsCompleted(a.achievementId) || IsClaimable(a.achievementId))
                .OrderBy(a => a.sortOrder)
                .ThenBy(a => a.rarity)
                .ToList();
        }

        /// <summary>Check if an achievement has been claimed (reward collected)</summary>
        public bool IsCompleted(string achievementId)
        {
            if (progressMap.TryGetValue(achievementId, out var p))
                return p.completed;
            return false;
        }

        /// <summary>Check if an achievement is ready to claim (target reached, reward not yet collected)</summary>
        public bool IsClaimable(string achievementId)
        {
            if (progressMap.TryGetValue(achievementId, out var p))
                return p.claimable && !p.completed;
            return false;
        }

        /// <summary>Get the current tier of a tiered achievement (0-indexed)</summary>
        public int GetCurrentTier(string achievementId)
        {
            if (progressMap.TryGetValue(achievementId, out var p))
                return p.currentTier;
            return 0;
        }

        /// <summary>Get stored progress value for an achievement</summary>
        public int GetProgress(string achievementId)
        {
            if (progressMap.TryGetValue(achievementId, out var p))
                return p.currentValue;
            return 0;
        }

        /// <summary>Get the computed live progress for an achievement (reads from game systems)</summary>
        public int GetLiveProgress(AchievementData achievement)
        {
            return ReadStatValue(achievement.trackType);
        }

        /// <summary>Get all completed achievements</summary>
        public List<AchievementData> GetCompletedAchievements()
        {
            return allAchievements.Where(a => IsCompleted(a.achievementId)).ToList();
        }

        /// <summary>Get completion percentage (0-1)</summary>
        public float GetCompletionPercentage()
        {
            if (allAchievements.Count == 0) return 0f;
            int completed = allAchievements.Count(a => IsCompleted(a.achievementId));
            return (float)completed / allAchievements.Count;
        }

        /// <summary>Get count of achievements ready to claim</summary>
        public int GetClaimableCount()
        {
            return progressMap.Values.Count(p => p.claimable && !p.completed);
        }

        // ==========================================
        // PUBLIC API - TRACKING
        // ==========================================

        /// <summary>
        /// Manually unlock an achievement (for Manual trackType).
        /// </summary>
        public void Unlock(string achievementId)
        {
            AchievementData data = allAchievements.FirstOrDefault(a => a.achievementId == achievementId);
            if (data == null)
            {
                Debug.LogWarning($"[AchievementManager] Achievement '{achievementId}' not found!");
                return;
            }

            SetProgress(achievementId, data.targetValue);
        }

        /// <summary>
        /// Increment a cumulative stat and check achievements.
        /// Call this from game systems when relevant events happen.
        /// </summary>
        public void AddGoldEarned(int amount)
        {
            totalGoldEarned += amount;
            CheckAchievementsForType(AchievementTrackType.GoldEarned);
        }

        public void AddGoldSpent(int amount)
        {
            totalGoldSpent += amount;
            CheckAchievementsForType(AchievementTrackType.GoldSpent);
        }

        public void AddCrystalsEarned(int amount)
        {
            totalCrystalsEarned += amount;
            CheckAchievementsForType(AchievementTrackType.CrystalsEarned);
        }

        public void AddLootboxOpened()
        {
            totalLootboxesOpened++;
            CheckAchievementsForType(AchievementTrackType.LootboxesOpened);
        }

        public void AddQuestCompleted()
        {
            totalQuestsCompleted++;
            CheckAchievementsForType(AchievementTrackType.QuestsCompleted);
        }

        /// <summary>
        /// Check all auto-tracked achievements against current game state.
        /// Call after major state changes (login, match end, etc.)
        /// </summary>
        public void CheckAllAchievements()
        {
            bool changed = false;

            foreach (var ach in allAchievements)
            {
                if (ach.trackType == AchievementTrackType.Manual) continue;
                if (IsCompleted(ach.achievementId) && !ach.hasTiers) continue;

                int currentValue = ReadStatValue(ach.trackType);
                changed |= EvaluateProgress(ach, currentValue);
            }

            if (changed) SaveToCloud();
        }

        // ==========================================
        // INTERNAL - PROGRESS EVALUATION
        // ==========================================

        private void CheckAchievementsForType(AchievementTrackType type)
        {
            bool changed = false;

            foreach (var ach in allAchievements.Where(a => a.trackType == type))
            {
                if (IsCompleted(ach.achievementId) && !ach.hasTiers) continue;

                int currentValue = ReadStatValue(type);
                changed |= EvaluateProgress(ach, currentValue);
            }

            if (changed) SaveToCloud();
        }

        private bool EvaluateProgress(AchievementData ach, int currentValue)
        {
            if (!progressMap.TryGetValue(ach.achievementId, out var progress))
            {
                progress = new AchievementProgress { id = ach.achievementId };
                progressMap[ach.achievementId] = progress;
            }

            bool changed = false;

            if (currentValue != progress.currentValue)
            {
                progress.currentValue = currentValue;
                changed = true;
                OnProgressUpdated?.Invoke(ach, currentValue, ach.GetTargetForTier(progress.currentTier));
            }

            // Check if target reached → mark as CLAIMABLE (not completed!)
            // Rewards are given only when the player clicks CLAIM.
            if (ach.hasTiers)
            {
                int nextTier = progress.currentTier;
                if (nextTier < ach.TierCount &&
                    currentValue >= ach.GetTargetForTier(nextTier) &&
                    !progress.claimable && !progress.completed)
                {
                    progress.claimable = true;
                    changed = true;
                    Debug.Log($"[Achievement] '{ach.achievementName}' tier {nextTier + 1} READY TO CLAIM!");
                    OnAchievementClaimable?.Invoke(ach, nextTier + 1);
                }
            }
            else
            {
                // Single tier
                if (!progress.completed && !progress.claimable &&
                    currentValue >= ach.targetValue)
                {
                    progress.claimable = true;
                    changed = true;
                    Debug.Log($"[Achievement] '{ach.achievementName}' READY TO CLAIM!");
                    OnAchievementClaimable?.Invoke(ach, 1);
                }
            }

            return changed;
        }

        /// <summary>
        /// Claim a completed achievement — grants reward and marks as completed.
        /// Returns true if claimed successfully.
        /// </summary>
        public bool ClaimAchievement(string achievementId)
        {
            AchievementData ach = allAchievements.FirstOrDefault(a => a.achievementId == achievementId);
            if (ach == null) return false;

            if (!progressMap.TryGetValue(achievementId, out var progress)) return false;
            if (!progress.claimable || progress.completed) return false;

            // Grant rewards
            GrantRewards(ach);

            // Mark as claimed
            if (ach.hasTiers)
            {
                progress.currentTier++;
                progress.claimable = false; // Reset claimable for next tier

                Debug.Log($"[Achievement] CLAIMED '{ach.achievementName}' tier {progress.currentTier}!");

                // Check if fully completed (all tiers done)
                if (progress.currentTier >= ach.TierCount)
                {
                    progress.completed = true;
                }
                else
                {
                    // Check if next tier is also already reached
                    int nextTarget = ach.GetTargetForTier(progress.currentTier);
                    if (progress.currentValue >= nextTarget)
                    {
                        progress.claimable = true;
                    }
                }
            }
            else
            {
                progress.completed = true;
                progress.claimable = false;
                progress.currentTier = 1;
                Debug.Log($"[Achievement] CLAIMED '{ach.achievementName}'!");
            }

            OnAchievementClaimed?.Invoke(ach, progress.currentTier);
            SaveToCloud();
            return true;
        }

        private void SetProgress(string achievementId, int value)
        {
            AchievementData ach = allAchievements.FirstOrDefault(a => a.achievementId == achievementId);
            if (ach == null) return;

            bool changed = EvaluateProgress(ach, value);
            if (changed) SaveToCloud();
        }

        private void GrantRewards(AchievementData ach)
        {
            var pc = PlayerCollection.Instance;
            if (pc == null) return;

            if (ach.rewardGold > 0)
            {
                pc.AddGold(ach.rewardGold);
                Debug.Log($"[Achievement] Reward: +{ach.rewardGold} Gold");
            }
            if (ach.rewardCrystals > 0)
            {
                pc.AddCrystals(ach.rewardCrystals);
                Debug.Log($"[Achievement] Reward: +{ach.rewardCrystals} Crystals");
            }
            if (ach.rewardXP > 0)
            {
                pc.AddXP(ach.rewardXP);
                Debug.Log($"[Achievement] Reward: +{ach.rewardXP} XP");
            }
        }

        // ==========================================
        // STAT READING
        // ==========================================

        private int ReadStatValue(AchievementTrackType type)
        {
            var pc = PlayerCollection.Instance;

            return type switch
            {
                AchievementTrackType.Wins => pc?.GetWins() ?? 0,
                AchievementTrackType.Losses => pc?.GetLosses() ?? 0,
                AchievementTrackType.MatchesPlayed => (pc?.GetWins() ?? 0) + (pc?.GetLosses() ?? 0),
                AchievementTrackType.PlayerLevel => pc?.GetLevel() ?? 0,
                AchievementTrackType.CardsUnlocked => pc?.GetUnlockedCards()?.Count ?? 0,
                AchievementTrackType.LegendaryCardsUnlocked => pc?.GetAllCardsByRarity(CardRarity.Legendary)
                    ?.Count(c => pc.IsUnlocked(c)) ?? 0,
                AchievementTrackType.DecksCreated => pc?.GetPlayerDecks()?.Count ?? 0,
                AchievementTrackType.EloReached => pc?.GetElo() ?? 0,
                AchievementTrackType.SkinsOwned => Skins.SkinInventory.Instance?.GetOwnedSkins()?.Count ?? 0,
                AchievementTrackType.GoldEarned => totalGoldEarned,
                AchievementTrackType.GoldSpent => totalGoldSpent,
                AchievementTrackType.CrystalsEarned => totalCrystalsEarned,
                AchievementTrackType.LootboxesOpened => totalLootboxesOpened,
                AchievementTrackType.QuestsCompleted => totalQuestsCompleted,
                _ => 0
            };
        }

        // ==========================================
        // SAVE / LOAD — CLOUD ONLY
        // ==========================================

        [Serializable]
        private class AchievementProgress
        {
            public string id;
            public int currentValue;
            public int currentTier;
            public bool completed;
            public bool claimable;
        }

        [Serializable]
        private class AchievementSaveData
        {
            public List<AchievementProgress> progress = new List<AchievementProgress>();
            public int goldEarned;
            public int goldSpent;
            public int crystalsEarned;
            public int lootboxesOpened;
            public int questsCompleted;
        }

        private void SaveToCloud()
        {
            var saveData = new AchievementSaveData
            {
                progress = progressMap.Values.ToList(),
                goldEarned = totalGoldEarned,
                goldSpent = totalGoldSpent,
                crystalsEarned = totalCrystalsEarned,
                lootboxesOpened = totalLootboxesOpened,
                questsCompleted = totalQuestsCompleted
            };

            string json = JsonUtility.ToJson(saveData, true);

            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.SaveData("AchievementData", json);
            }
            else
            {
                Debug.LogWarning("[AchievementManager] CloudSaveManager is null - data NOT saved!");
            }
        }

        private void LoadFromCloud()
        {
            if (CloudSaveManager.Instance != null)
            {
                Debug.Log("[AchievementManager] Loading achievements from PlayFab...");
                CloudSaveManager.Instance.LoadData("AchievementData",
                    json =>
                    {
                        ProcessLoadedJson(json);
                        // After loading, check all auto-tracked achievements
                        CheckAllAchievements();
                    },
                    () =>
                    {
                        Debug.Log("[AchievementManager] No cloud data - fresh achievement state.");
                        CheckAllAchievements();
                        OnAchievementsLoaded?.Invoke();
                    });
            }
            else
            {
                Debug.LogWarning("[AchievementManager] CloudSaveManager is null!");
                OnAchievementsLoaded?.Invoke();
            }
        }

        private void ProcessLoadedJson(string json)
        {
            try
            {
                AchievementSaveData saveData = JsonUtility.FromJson<AchievementSaveData>(json);

                progressMap.Clear();
                if (saveData.progress != null)
                {
                    foreach (var p in saveData.progress)
                    {
                        progressMap[p.id] = p;
                    }
                }

                totalGoldEarned = saveData.goldEarned;
                totalGoldSpent = saveData.goldSpent;
                totalCrystalsEarned = saveData.crystalsEarned;
                totalLootboxesOpened = saveData.lootboxesOpened;
                totalQuestsCompleted = saveData.questsCompleted;

                Debug.Log($"[AchievementManager] Loaded: {progressMap.Count} tracked, " +
                          $"{progressMap.Values.Count(p => p.completed)} completed");

                OnAchievementsLoaded?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AchievementManager] Failed to parse JSON: {e.Message}");
                OnAchievementsLoaded?.Invoke();
            }
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Check All Achievements Now")]
        private void DebugCheckAll() => CheckAllAchievements();

        [ContextMenu("Print Achievement Status")]
        private void DebugPrintStatus()
        {
            Debug.Log("=== ACHIEVEMENT STATUS ===");
            foreach (var ach in allAchievements.OrderBy(a => a.sortOrder))
            {
                bool done = IsCompleted(ach.achievementId);
                int progress = GetProgress(ach.achievementId);
                int live = GetLiveProgress(ach);
                Debug.Log($"  {(done ? "✓" : "○")} {ach.achievementName} — " +
                          $"stored:{progress} live:{live}/{ach.targetValue} " +
                          $"tier:{GetCurrentTier(ach.achievementId)}/{ach.TierCount}");
            }
        }

        [ContextMenu("RESET: Wipe All Achievement Data")]
        private void DebugWipeAll()
        {
            progressMap.Clear();
            totalGoldEarned = 0;
            totalGoldSpent = 0;
            totalCrystalsEarned = 0;
            totalLootboxesOpened = 0;
            totalQuestsCompleted = 0;
            SaveToCloud();
            Debug.Log("[DEBUG] Wiped all achievement data!");
        }
    }
}
