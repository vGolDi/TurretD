// Assets/PrzemekSkrypty/Progression/QuestManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ElementumDefense.Cards;
using ElementumDefense.Lootbox;
using ElementumDefense.Auth;
using ElementumDefense.BattlePass;
using ElementumDefense.Multiplayer;

namespace ElementumDefense.Progression
{
    public enum QuestType
    {
        PlayGames,
        WinGames,
        KillEnemies,
        BuildTurrets,
        SpendGold,
        OpenLootboxes,
        UnlockCards
    }

    public enum QuestTier
    {
        Daily,
        Weekly,
        Special
    }

    [Serializable]
    public class Quest
    {
        public string questID;
        public QuestType type;
        public QuestTier tier;
        public string description;
        public int currentProgress;
        public int targetAmount;

        public int rewardGold;
        public int rewardCrystals;
        public int rewardXP;
        public int rewardBPXP;

        public string rewardLootboxName;
        [NonSerialized] public LootboxData rewardLootbox;

        public bool isCompleted;
        public bool isClaimed;

        public float GetProgress01() => Mathf.Clamp01((float)currentProgress / targetAmount);
        public bool HasLootboxReward => !string.IsNullOrEmpty(rewardLootboxName);
    }

    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        public event Action OnQuestListUpdated;
        public event Action<LootboxData> OnLootboxRewarded;

        /// <summary>
        /// Fires once per quest when its reward is claimed. Achievement /
        /// telemetry systems subscribe to this instead of polling
        /// OnQuestListUpdated (which fires on every progress tick).
        /// </summary>
        public event Action<Quest> OnQuestClaimed;

        [Header("Quest Pool Settings")]
        [SerializeField, Range(1, 10), Tooltip("How many daily quests to assign per day")]
        private int dailyQuestCount = 3;

        [SerializeField, Tooltip("Generate weekly quests?")]
        private bool generateWeeklyQuest = true;

        [SerializeField, Range(1, 5), Tooltip("How many weekly quests to assign per week")]
        private int weeklyQuestCount = 1;

        [SerializeField, Range(1, 5), Tooltip("How many special (BP) quests to assign per week")]
        private int specialQuestCount = 2;

        [Header("Quest Pool (auto-loaded from Resources/Quests/)")]
        [SerializeField, Tooltip("All available quest templates. Auto-loads if empty.")]
        private List<QuestData> questPool = new List<QuestData>();

        public List<Quest> activeQuests = new List<Quest>();

        private Dictionary<string, LootboxData> lootboxCache = new Dictionary<string, LootboxData>();
        private string lastQuestDate = "";
        private string lastWeeklyDate = "";

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

            LoadQuestPool();
            CacheLootboxes();
        }

        private void Start()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnCloudReady += OnUserLoggedIn;
            }
            else
            {
                Debug.LogWarning("[QuestManager] AuthManager not found - using default save");
                LoadQuests();
            }
        }

        private void OnDestroy()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnCloudReady -= OnUserLoggedIn;
            }
        }

        private void OnUserLoggedIn(string username)
        {
            Debug.Log($"[QuestManager] User {username} logged in - loading their quests");

            activeQuests.Clear();
            lastQuestDate = "";
            lastWeeklyDate = "";

            LoadQuests();

            OnQuestListUpdated?.Invoke();
        }

        /// <summary>
        /// Loads all QuestData SOs from Resources/Quests/
        /// </summary>
        private void LoadQuestPool()
        {
            if (questPool.Count > 0) return; // Already assigned in Inspector

            QuestData[] loaded = Resources.LoadAll<QuestData>("Quests");
            if (loaded.Length > 0)
            {
                questPool.Clear();
                questPool.AddRange(loaded);
                Debug.Log($"[QuestManager] Auto-loaded {loaded.Length} quest templates from Resources/Quests/");
            }
            else
            {
                Debug.LogWarning("[QuestManager] No QuestData found in Resources/Quests/. Create some!");
            }
        }

        private void CacheLootboxes()
        {
            LootboxData[] allLootboxes = Resources.LoadAll<LootboxData>("Lootboxes");

            foreach (var lb in allLootboxes)
            {
                if (lb != null && !lootboxCache.ContainsKey(lb.name))
                {
                    lootboxCache[lb.name] = lb;
                }
            }

            Debug.Log($"[QuestManager] Cached {lootboxCache.Count} lootbox types");
        }

        private LootboxData GetLootboxByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return lootboxCache.TryGetValue(name, out var lb) ? lb : null;
        }

        // ==========================================
        // DAILY/WEEKLY RESET CHECK
        // ==========================================

        private void CheckDailyReset()
        {
            string todayDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            string thisWeek = GetWeekIdentifier();

            Debug.Log($"[QuestManager] Last Daily: '{lastQuestDate}', Today: '{todayDate}'");
            Debug.Log($"[QuestManager] Last Weekly: '{lastWeeklyDate}', This Week: '{thisWeek}'");

            bool dailyReset = lastQuestDate != todayDate;
            bool weeklyReset = lastWeeklyDate != thisWeek;

            if (dailyReset)
            {
                Debug.Log("[QuestManager] New day - assigning daily quests from pool");
                RemoveQuestsByTier(QuestTier.Daily);
                AssignQuestsFromPool(QuestTier.Daily, dailyQuestCount);
                lastQuestDate = todayDate;
            }

            if (weeklyReset && generateWeeklyQuest)
            {
                Debug.Log("[QuestManager] New week - assigning weekly quests from pool");
                RemoveQuestsByTier(QuestTier.Weekly);
                AssignQuestsFromPool(QuestTier.Weekly, weeklyQuestCount);

                // Special quests (BP quests) also reset weekly
                Debug.Log("[QuestManager] New week - assigning special (BP) quests from pool");
                RemoveQuestsByTier(QuestTier.Special);
                AssignQuestsFromPool(QuestTier.Special, specialQuestCount);

                lastWeeklyDate = thisWeek;
            }

            if (!dailyReset && !weeklyReset)
            {
                RestoreLootboxReferences();
            }

            SaveQuests();
            OnQuestListUpdated?.Invoke();
        }

        private string GetWeekIdentifier()
        {
            DateTime now = DateTime.UtcNow;
            int weekNumber = System.Globalization.CultureInfo.CurrentCulture.Calendar
                .GetWeekOfYear(now, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            return $"{now.Year}-W{weekNumber:D2}";
        }

        private void RemoveQuestsByTier(QuestTier tier)
        {
            activeQuests.RemoveAll(q => q.tier == tier);
        }

        private void RestoreLootboxReferences()
        {
            foreach (var quest in activeQuests)
            {
                if (!string.IsNullOrEmpty(quest.rewardLootboxName))
                {
                    quest.rewardLootbox = GetLootboxByName(quest.rewardLootboxName);
                }
            }
        }

        // ==========================================
        // QUEST ASSIGNMENT FROM POOL
        // ==========================================

        /// <summary>
        /// Picks random quests from the SO pool for the given tier.
        /// Uses weighted random selection without repeats.
        /// </summary>
        private void AssignQuestsFromPool(QuestTier tier, int count)
        {
            // Filter pool by tier, not manual-only, and level requirement
            int playerLevel = PlayerCollection.Instance?.GetLevel() ?? 1;

            List<QuestData> eligible = questPool
                .Where(q => q != null
                    && q.questTier == tier
                    && !q.manualOnly
                    && q.requiredLevel <= playerLevel)
                .ToList();

            if (eligible.Count == 0)
            {
                Debug.LogWarning($"[QuestManager] No eligible quests in pool for tier {tier}!");
                return;
            }

            // Pick 'count' unique quests (weighted random, no duplicates)
            List<QuestData> selected = new List<QuestData>();
            List<QuestData> remaining = new List<QuestData>(eligible);

            for (int i = 0; i < count && remaining.Count > 0; i++)
            {
                QuestData picked = WeightedRandomPick(remaining);
                if (picked != null)
                {
                    selected.Add(picked);
                    remaining.Remove(picked);
                }
            }

            // If we need more quests than unique templates, allow repeats
            if (selected.Count < count)
            {
                for (int i = selected.Count; i < count; i++)
                {
                    QuestData picked = WeightedRandomPick(eligible);
                    if (picked != null)
                        selected.Add(picked);
                }
            }

            // Create runtime quests from selected templates
            foreach (var questData in selected)
            {
                Quest quest = questData.CreateRuntimeQuest();
                activeQuests.Add(quest);
            }

            Debug.Log($"[QuestManager] Assigned {selected.Count} {tier} quests from pool");
        }

        /// <summary>
        /// Weighted random selection from a list of QuestData.
        /// </summary>
        private QuestData WeightedRandomPick(List<QuestData> pool)
        {
            if (pool.Count == 0) return null;
            if (pool.Count == 1) return pool[0];

            int totalWeight = 0;
            foreach (var q in pool)
                totalWeight += q.selectionWeight;

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int cumulative = 0;

            foreach (var q in pool)
            {
                cumulative += q.selectionWeight;
                if (roll < cumulative)
                    return q;
            }

            return pool[pool.Count - 1];
        }

        // ==========================================
        // PUBLIC API - ADD QUESTS
        // ==========================================

        /// <summary>
        /// Adds a quest from a specific QuestData template.
        /// </summary>
        public Quest AddQuestFromData(QuestData questData)
        {
            if (questData == null) return null;

            Quest quest = questData.CreateRuntimeQuest();
            activeQuests.Add(quest);
            SaveQuests();
            OnQuestListUpdated?.Invoke();

            Debug.Log($"[QuestManager] Added quest from SO: {quest.description}");
            return quest;
        }

        /// <summary>
        /// Adds a custom quest (legacy API, still works for code-driven quests).
        /// </summary>
        public Quest AddQuest(QuestType type, QuestTier tier, int target, int gold, int xp, LootboxData lootbox = null)
        {
            string description = $"[{tier}] Complete objective ({target})";

            Quest quest = new Quest
            {
                questID = Guid.NewGuid().ToString(),
                type = type,
                tier = tier,
                description = description,
                targetAmount = target,
                rewardGold = gold,
                rewardXP = xp,
                rewardLootbox = lootbox,
                rewardLootboxName = lootbox != null ? lootbox.name : "",
                currentProgress = 0,
                isCompleted = false,
                isClaimed = false
            };

            activeQuests.Add(quest);
            SaveQuests();
            OnQuestListUpdated?.Invoke();

            Debug.Log($"[QuestManager] Added quest: {quest.description}");
            return quest;
        }

        /// <summary>
        /// Adds a special/event quest (legacy API for code-driven special quests).
        /// </summary>
        public Quest AddSpecialQuest(QuestType type, int target, int gold, int xp, string customDescription = null)
        {
            Quest quest = new Quest
            {
                questID = Guid.NewGuid().ToString(),
                type = type,
                tier = QuestTier.Special,
                description = customDescription ?? $"[Special] Complete objective ({target})",
                targetAmount = target,
                rewardGold = gold,
                rewardXP = xp,
                rewardLootbox = null,
                rewardLootboxName = "",
                currentProgress = 0,
                isCompleted = false,
                isClaimed = false
            };

            activeQuests.Add(quest);
            SaveQuests();
            OnQuestListUpdated?.Invoke();

            Debug.Log($"[QuestManager] Added special quest: {quest.description}");
            return quest;
        }

        /// <summary>
        /// Removes a specific quest.
        /// </summary>
        public void RemoveQuest(Quest quest)
        {
            if (activeQuests.Contains(quest))
            {
                activeQuests.Remove(quest);
                SaveQuests();
                OnQuestListUpdated?.Invoke();
                Debug.Log($"[QuestManager] Removed quest: {quest.description}");
            }
        }

        /// <summary>
        /// Gets quests by tier.
        /// </summary>
        public List<Quest> GetQuestsByTier(QuestTier tier)
        {
            return activeQuests.Where(q => q.tier == tier).ToList();
        }

        /// <summary>
        /// Gets the full quest pool (for editor/debug).
        /// </summary>
        public List<QuestData> GetQuestPool() => questPool;

        // ==========================================
        // PROGRESS & CLAIMING
        // ==========================================

        public void ReportProgress(QuestType type, int amount)
        {
            bool changed = false;

            foreach (var quest in activeQuests)
            {
                if (quest.type == type && !quest.isCompleted)
                {
                    quest.currentProgress += amount;

                    if (quest.currentProgress >= quest.targetAmount)
                    {
                        quest.currentProgress = quest.targetAmount;
                        quest.isCompleted = true;
                        Debug.Log($"[QuestManager] Quest Completed: {quest.description}");
                    }
                    changed = true;
                }
            }

            if (changed)
            {
                SaveQuests();
                OnQuestListUpdated?.Invoke();
            }
        }

        public void ClaimReward(Quest quest)
        {
            if (!quest.isCompleted || quest.isClaimed) return;

            if (PlayerCollection.Instance != null)
            {
                PlayerCollection.Instance.AddGold(quest.rewardGold);
                if (quest.rewardCrystals > 0)
                    PlayerCollection.Instance.AddCrystals(quest.rewardCrystals);
                PlayerCollection.Instance.AddXP(quest.rewardXP);
            }

            // Battle Pass XP (independent from player XP)
            if (quest.rewardBPXP > 0 && BattlePassManager.Instance != null)
            {
                BattlePassManager.Instance.AddXP(quest.rewardBPXP);
            }

            if (quest.HasLootboxReward)
            {
                if (quest.rewardLootbox == null)
                {
                    quest.rewardLootbox = GetLootboxByName(quest.rewardLootboxName);
                }

                if (quest.rewardLootbox != null && LootboxInventory.Instance != null)
                {
                    LootboxInventory.Instance.AddLootbox(quest.rewardLootbox, 1);
                    OnLootboxRewarded?.Invoke(quest.rewardLootbox);

                    Debug.Log($"[QuestManager] Lootbox rewarded: {quest.rewardLootbox.lootboxName}");
                }
            }

            quest.isClaimed = true;
            SaveQuests();
            OnQuestListUpdated?.Invoke();
            OnQuestClaimed?.Invoke(quest);

            Debug.Log($"[QuestManager] Reward claimed: {quest.rewardGold}g, {quest.rewardXP}xp" +
                      (quest.HasLootboxReward ? $" + {quest.rewardLootboxName}" : ""));
        }

        // ==========================================
        // SAVE/LOAD
        // ==========================================

        [Serializable]
        private class QuestSaveData
        {
            public string lastQuestDate;
            public string lastWeeklyDate;
            public List<Quest> quests = new List<Quest>();
        }

        private void SaveQuests()
        {
            QuestSaveData saveData = new QuestSaveData
            {
                lastQuestDate = this.lastQuestDate,
                lastWeeklyDate = this.lastWeeklyDate,
                quests = activeQuests
            };

            string json = JsonUtility.ToJson(saveData, true);

            if (CloudSaveManager.Instance != null)
            {
                CloudSaveManager.Instance.SaveData("QuestManagerData", json);
            }
            else
            {
                Debug.LogWarning("[QuestManager] CloudSaveManager is null - data NOT saved!");
            }
        }

        private void LoadQuests()
        {
            if (CloudSaveManager.Instance != null)
            {
                Debug.Log("[QuestManager] Loading quests from PlayFab cloud...");
                CloudSaveManager.Instance.LoadData("QuestManagerData",
                    json =>
                    {
                        Debug.Log("[QuestManager] Cloud data loaded OK.");
                        ProcessLoadedQuestJson(json);
                    },
                    () =>
                    {
                        Debug.Log("[QuestManager] No cloud data - generating new quests.");
                        activeQuests = new List<Quest>();
                        CheckDailyReset();
                    });
            }
            else
            {
                Debug.LogWarning("[QuestManager] CloudSaveManager is null!");
                activeQuests = new List<Quest>();
                CheckDailyReset();
            }
        }

        private void ProcessLoadedQuestJson(string json)
        {
            try
            {
                QuestSaveData saveData = JsonUtility.FromJson<QuestSaveData>(json);
                if (saveData != null)
                {
                    lastQuestDate = saveData.lastQuestDate ?? "";
                    lastWeeklyDate = saveData.lastWeeklyDate ?? "";
                    activeQuests = saveData.quests ?? new List<Quest>();
                }
                CheckDailyReset();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[QuestManager] Failed to load JSON: {e.Message}");
                activeQuests = new List<Quest>();
                CheckDailyReset();
            }
        }

        // ==========================================
        // DEBUG TOOLS
        // ==========================================

        [ContextMenu("DEBUG: Force Daily Reset")]
        public void DebugForceDailyReset()
        {
            lastQuestDate = "";
            RemoveQuestsByTier(QuestTier.Daily);
            AssignQuestsFromPool(QuestTier.Daily, dailyQuestCount);
            SaveQuests();
            OnQuestListUpdated?.Invoke();
            Debug.Log("<color=yellow>[DEBUG] Forced Daily Reset</color>");
        }

        [ContextMenu("DEBUG: Force Weekly Reset")]
        public void DebugForceWeeklyReset()
        {
            lastWeeklyDate = "";
            RemoveQuestsByTier(QuestTier.Weekly);
            AssignQuestsFromPool(QuestTier.Weekly, weeklyQuestCount);
            RemoveQuestsByTier(QuestTier.Special);
            AssignQuestsFromPool(QuestTier.Special, specialQuestCount);
            SaveQuests();
            OnQuestListUpdated?.Invoke();
            Debug.Log("<color=yellow>[DEBUG] Forced Weekly + Special Reset</color>");
        }

        [ContextMenu("DEBUG: Force Full Reset (Daily + Weekly)")]
        public void DebugForceFullReset()
        {
            lastQuestDate = "";
            lastWeeklyDate = "";
            activeQuests.Clear();
            CheckDailyReset();
            Debug.Log("<color=yellow>[DEBUG] Forced Full Reset</color>");
        }

        [ContextMenu("DEBUG: Complete All Quests")]
        public void DebugCompleteAll()
        {
            foreach (var q in activeQuests)
            {
                q.currentProgress = q.targetAmount;
                q.isCompleted = true;
            }
            SaveQuests();
            OnQuestListUpdated?.Invoke();
            Debug.Log("<color=green>[DEBUG] All quests completed</color>");
        }

        [ContextMenu("DEBUG: Complete Daily Quests Only")]
        public void DebugCompleteDailyOnly()
        {
            foreach (var q in activeQuests.Where(q => q.tier == QuestTier.Daily))
            {
                q.currentProgress = q.targetAmount;
                q.isCompleted = true;
            }
            SaveQuests();
            OnQuestListUpdated?.Invoke();
            Debug.Log("<color=green>[DEBUG] Daily quests completed</color>");
        }

        [ContextMenu("DEBUG: Print Quest Status")]
        public void DebugPrintStatus()
        {
            Debug.Log($"=== QUEST STATUS ===");
            Debug.Log($"Last Daily: {lastQuestDate}");
            Debug.Log($"Last Weekly: {lastWeeklyDate}");
            Debug.Log($"Total Quests: {activeQuests.Count}");
            Debug.Log($"Quest Pool: {questPool.Count} templates");
            Debug.Log($"");

            Debug.Log($"--- DAILY ({GetQuestsByTier(QuestTier.Daily).Count}) ---");
            foreach (var q in GetQuestsByTier(QuestTier.Daily))
            {
                string status = q.isClaimed ? "CLAIMED" : (q.isCompleted ? "DONE" : "IN PROGRESS");
                string lootbox = q.HasLootboxReward ? " [+BOX]" : "";
                Debug.Log($"  [{status}] {q.description} ({q.currentProgress}/{q.targetAmount}){lootbox}");
            }

            Debug.Log($"--- WEEKLY ({GetQuestsByTier(QuestTier.Weekly).Count}) ---");
            foreach (var q in GetQuestsByTier(QuestTier.Weekly))
            {
                string status = q.isClaimed ? "CLAIMED" : (q.isCompleted ? "DONE" : "IN PROGRESS");
                string lootbox = q.HasLootboxReward ? " [+BOX]" : "";
                Debug.Log($"  [{status}] {q.description} ({q.currentProgress}/{q.targetAmount}){lootbox}");
            }

            Debug.Log($"--- SPECIAL ({GetQuestsByTier(QuestTier.Special).Count}) ---");
            foreach (var q in GetQuestsByTier(QuestTier.Special))
            {
                string status = q.isClaimed ? "CLAIMED" : (q.isCompleted ? "DONE" : "IN PROGRESS");
                string lootbox = q.HasLootboxReward ? " [+BOX]" : "";
                Debug.Log($"  [{status}] {q.description} ({q.currentProgress}/{q.targetAmount}){lootbox}");
            }
        }

        [ContextMenu("DEBUG: Print Quest Pool")]
        public void DebugPrintPool()
        {
            Debug.Log($"=== QUEST POOL ({questPool.Count} templates) ===");
            foreach (var q in questPool)
            {
                if (q == null) continue;
                Debug.Log($"  [{q.questTier}] {q.questId}: {q.GetFormattedDescription()} " +
                          $"(target={q.targetAmount}, gold={q.rewardGold}, xp={q.rewardXP}, weight={q.selectionWeight})");
            }
        }
    }
}
