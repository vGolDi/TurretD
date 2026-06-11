// Assets/PrzemekSkrypty/Achievements/AchievementManager.cs
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ElementumDefense.Auth;
using ElementumDefense.Cards;
using ElementumDefense.Lootbox;
using ElementumDefense.Progression;
using ElementumDefense.Multiplayer;

namespace ElementumDefense.Achievements
{
    /// <summary>
    /// Manages achievement progress and unlocks.
    /// Cloud-synced via PlayFab. Auto-tracks stats from game systems.
    /// 
    /// Cumulative counters (gold earned/spent, lootboxes opened, quests claimed)
    /// are populated by SUBSCRIBING to events emitted by other managers
    /// (PlayerCollection, LootboxManager, QuestManager). No public Add* API —
    /// that would invite double-counting and was never wired up correctly anyway.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [Header("All Achievements (auto-loaded from Resources/Achievements/)")]
        [SerializeField]
        private List<AchievementData> allAchievements = new List<AchievementData>();

        // Runtime state
        private readonly Dictionary<string, AchievementProgress> progressMap
            = new Dictionary<string, AchievementProgress>();

        // Cumulative stats. Persisted on cloud and updated via event subscriptions.
        private int totalGoldEarned = 0;
        private int totalGoldSpent = 0;
        private int totalCrystalsEarned = 0;
        private int totalLootboxesOpened = 0;
        private int totalQuestsCompleted = 0;

        // Event subscription bookkeeping. Last seen balances are needed to
        // compute deltas from PlayerCollection's "current value" events.
        private int lastSeenGold = 0;
        private int lastSeenCrystals = 0;
        private bool subscribedToCollection = false;
        private bool subscribedToLootbox = false;
        private bool subscribedToQuests = false;

        // ==========================================
        // EVENTS
        // ==========================================

        /// <summary>Fired when an achievement is ready to claim</summary>
        public event Action<AchievementData, int> OnAchievementClaimable;

        /// <summary>Fired when an achievement is claimed (reward given)</summary>
        public event Action<AchievementData, int> OnAchievementClaimed;

        /// <summary>Fired when progress updates</summary>
        public event Action<AchievementData, int, int> OnProgressUpdated;

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

            // Connect to source-of-truth events. Wrapped in TrySubscribe so we
            // gracefully handle scene-load order — managers may not exist yet.
            TrySubscribeToManagers();
        }

        private void OnDestroy()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnCloudReady -= OnUserLoggedIn;
            }

            UnsubscribeFromManagers();
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
            yield return new WaitForSeconds(5f);

            // Some managers may have come online after Start() — try again.
            TrySubscribeToManagers();

            // Initialize lastSeen* baselines from current PlayerCollection state
            // so the FIRST OnGoldChanged delta is computed against the loaded
            // balance, not zero.
            var pc = PlayerCollection.Instance;
            if (pc != null)
            {
                lastSeenGold = pc.GetGold();
                lastSeenCrystals = pc.GetCrystals();
            }

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
        // EVENT SUBSCRIPTIONS (single source of truth for cumulative stats)
        // ==========================================

        private void TrySubscribeToManagers()
        {
            if (!subscribedToCollection && PlayerCollection.Instance != null)
            {
                PlayerCollection.Instance.OnGoldChanged += HandleGoldChanged;
                PlayerCollection.Instance.OnCrystalsChanged += HandleCrystalsChanged;
                subscribedToCollection = true;
            }

            if (!subscribedToLootbox && LootboxManager.Instance != null)
            {
                LootboxManager.Instance.OnLootboxOpened += HandleLootboxOpened;
                subscribedToLootbox = true;
            }

            if (!subscribedToQuests && QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestClaimed += HandleQuestClaimed;
                subscribedToQuests = true;
            }
        }

        private void UnsubscribeFromManagers()
        {
            if (subscribedToCollection && PlayerCollection.Instance != null)
            {
                PlayerCollection.Instance.OnGoldChanged -= HandleGoldChanged;
                PlayerCollection.Instance.OnCrystalsChanged -= HandleCrystalsChanged;
            }
            if (subscribedToLootbox && LootboxManager.Instance != null)
            {
                LootboxManager.Instance.OnLootboxOpened -= HandleLootboxOpened;
            }
            if (subscribedToQuests && QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestClaimed -= HandleQuestClaimed;
            }
        }

        // PlayerCollection.OnGoldChanged passes the CURRENT balance, not a
        // delta. We diff against lastSeenGold to know if the change was income
        // (bumps GoldEarned) or expense (bumps GoldSpent).
        private void HandleGoldChanged(int currentGold)
        {
            int delta = currentGold - lastSeenGold;
            lastSeenGold = currentGold;

            if (delta > 0)
            {
                totalGoldEarned += delta;
                CheckAchievementsForType(AchievementTrackType.GoldEarned);
            }
            else if (delta < 0)
            {
                totalGoldSpent += -delta;
                CheckAchievementsForType(AchievementTrackType.GoldSpent);
            }
        }

        private void HandleCrystalsChanged(int currentCrystals)
        {
            int delta = currentCrystals - lastSeenCrystals;
            lastSeenCrystals = currentCrystals;

            if (delta > 0)
            {
                totalCrystalsEarned += delta;
                CheckAchievementsForType(AchievementTrackType.CrystalsEarned);
            }
            // We don't track crystal spending — no matching achievement type.
        }

        private void HandleLootboxOpened(LootboxResult _)
        {
            totalLootboxesOpened++;
            CheckAchievementsForType(AchievementTrackType.LootboxesOpened);
        }

        private void HandleQuestClaimed(Quest _)
        {
            totalQuestsCompleted++;
            CheckAchievementsForType(AchievementTrackType.QuestsCompleted);
        }

        // ==========================================
        // PUBLIC API - QUERIES
        // ==========================================

        public List<AchievementData> GetAllAchievements()
        {
            return allAchievements
                .Where(a => !a.isHidden || IsCompleted(a.achievementId) || IsClaimable(a.achievementId))
                .OrderBy(a => a.sortOrder)
                .ThenBy(a => a.rarity)
                .ToList();
        }

        public bool IsCompleted(string achievementId)
            => progressMap.TryGetValue(achievementId, out var p) && p.completed;

        public bool IsClaimable(string achievementId)
            => progressMap.TryGetValue(achievementId, out var p) && p.claimable && !p.completed;

        public int GetCurrentTier(string achievementId)
            => progressMap.TryGetValue(achievementId, out var p) ? p.currentTier : 0;

        public int GetProgress(string achievementId)
            => progressMap.TryGetValue(achievementId, out var p) ? p.currentValue : 0;

        public int GetLiveProgress(AchievementData achievement) => ReadStatValue(achievement.trackType);

        public List<AchievementData> GetCompletedAchievements()
            => allAchievements.Where(a => IsCompleted(a.achievementId)).ToList();

        public float GetCompletionPercentage()
        {
            if (allAchievements.Count == 0) return 0f;
            int completed = allAchievements.Count(a => IsCompleted(a.achievementId));
            return (float)completed / allAchievements.Count;
        }

        public int GetClaimableCount()
            => progressMap.Values.Count(p => p.claimable && !p.completed);

        // ==========================================
        // PUBLIC API - TRACKING
        // ==========================================

        /// <summary>Manually unlock an achievement (for Manual trackType).</summary>
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
                if (!progress.completed && !progress.claimable && currentValue >= ach.targetValue)
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

            GrantRewards(ach);

            if (ach.hasTiers)
            {
                progress.currentTier++;
                progress.claimable = false;

                Debug.Log($"[Achievement] CLAIMED '{ach.achievementName}' tier {progress.currentTier}!");

                if (progress.currentTier >= ach.TierCount)
                {
                    progress.completed = true;
                }
                else
                {
                    int nextTarget = ach.GetTargetForTier(progress.currentTier);
                    if (progress.currentValue >= nextTarget)
                        progress.claimable = true;
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
                CloudSaveManager.Instance.SaveData("AchievementData", json);
            else
                Debug.LogWarning("[AchievementManager] CloudSaveManager is null - data NOT saved!");
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
                    foreach (var p in saveData.progress)
                        progressMap[p.id] = p;

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
                Debug.Log($"  {(done ? "OK" : "..")} {ach.achievementName} - " +
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
