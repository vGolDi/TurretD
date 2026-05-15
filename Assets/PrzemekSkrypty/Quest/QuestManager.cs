// Assets/PrzemekSkrypty/Progression/QuestManager.cs
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ElementumDefense.Cards;
using ElementumDefense.Lootbox;
using ElementumDefense.Auth;

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
        public int rewardXP;

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

        [Header("Daily Quest Settings")]
        [SerializeField, Range(1, 10)]
        private int dailyQuestCount = 3;

        [SerializeField, Tooltip("First daily quest always gives lootbox")]
        private bool firstQuestAlwaysGivesLootbox = true;

        [SerializeField, Range(0f, 1f)]
        private float otherQuestsLootboxChance = 0.33f;

        [Header("Weekly Quest Settings")]
        [SerializeField]
        private bool generateWeeklyQuest = true;

        [SerializeField, Range(1, 5)]
        private int weeklyQuestCount = 1;

        [Header("Quest Difficulty Scaling")]
        [SerializeField] private int minPlayGames = 2;
        [SerializeField] private int maxPlayGames = 5;
        [SerializeField] private int minWinGames = 1;
        [SerializeField] private int maxWinGames = 3;
        [SerializeField] private int minKillEnemies = 20;
        [SerializeField] private int maxKillEnemies = 100;
        [SerializeField] private int minBuildTurrets = 5;
        [SerializeField] private int maxBuildTurrets = 20;

        [Header("Lootbox Rewards")]
        [SerializeField] private LootboxData dailyQuestLootbox;
        [SerializeField] private LootboxData weeklyQuestLootbox;
        [SerializeField] private LootboxData specialQuestLootbox;

        [Header("Reward Scaling")]
        [SerializeField] private int baseGoldReward = 100;
        [SerializeField] private int baseXPReward = 50;
        [SerializeField] private float weeklyRewardMultiplier = 3f;

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

            CacheLootboxes();
        }

        private void Start()
        {
            if (AuthManager.Instance != null)
            {
                AuthManager.Instance.OnCloudReady += OnUserLoggedIn;

                // OnCloudReady will fire after login verification
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
        // SAVE PATH
        // ==========================================


        // ==========================================
        // DAILY/WEEKLY RESET CHECK
        // ==========================================

        private void CheckDailyReset()
        {
            

            string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
            string thisWeek = GetWeekIdentifier();

            Debug.Log($"[QuestManager] Last Daily: '{lastQuestDate}', Today: '{todayDate}'");
            Debug.Log($"[QuestManager] Last Weekly: '{lastWeeklyDate}', This Week: '{thisWeek}'");

            bool dailyReset = lastQuestDate != todayDate;
            bool weeklyReset = lastWeeklyDate != thisWeek;

            if (dailyReset)
            {
                Debug.Log("[QuestManager] New day - generating daily quests");
                RemoveQuestsByTier(QuestTier.Daily);
                GenerateDailyQuests();
                lastQuestDate = todayDate;
            }

            if (weeklyReset && generateWeeklyQuest)
            {
                Debug.Log("[QuestManager] New week - generating weekly quests");
                RemoveQuestsByTier(QuestTier.Weekly);
                GenerateWeeklyQuests();
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
            // Format: "2024-W05" (rok-tydzie�)
            DateTime now = DateTime.Now;
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
        // QUEST GENERATION
        // ==========================================

        private void GenerateDailyQuests()
        {
            // Lista dost�pnych typ�w quest�w (bez powt�rek)
            List<QuestType> availableTypes = new List<QuestType>
            {
                QuestType.PlayGames,
                QuestType.WinGames,
                QuestType.KillEnemies,
                QuestType.BuildTurrets
            };

            // Shuffle dla losowo�ci
            ShuffleList(availableTypes);

            for (int i = 0; i < dailyQuestCount; i++)
            {
                QuestType questType;

                // Pierwszy quest = WinGames (z lootboxem je�li w��czone)
                if (i == 0)
                {
                    questType = QuestType.WinGames;
                }
                else
                {
                    // We� kolejny typ z listy (lub losuj je�li sko�czy�y si� unikalne)
                    if (i - 1 < availableTypes.Count)
                    {
                        questType = availableTypes[i - 1];
                        // Pomi� WinGames bo ju� jest
                        if (questType == QuestType.WinGames && i < availableTypes.Count)
                        {
                            questType = availableTypes[i];
                        }
                    }
                    else
                    {
                        questType = GetRandomQuestType();
                    }
                }

                // Okre�l czy da� lootbox
                LootboxData lootbox = null;
                if (i == 0 && firstQuestAlwaysGivesLootbox)
                {
                    lootbox = dailyQuestLootbox;
                }
                else if (UnityEngine.Random.value < otherQuestsLootboxChance)
                {
                    lootbox = dailyQuestLootbox;
                }

                Quest quest = GenerateQuestOfType(questType, QuestTier.Daily, lootbox);
                activeQuests.Add(quest);
            }

            Debug.Log($"[QuestManager] Generated {dailyQuestCount} daily quests");
        }

        private void GenerateWeeklyQuests()
        {
            for (int i = 0; i < weeklyQuestCount; i++)
            {
                QuestType questType = GetRandomQuestType();
                Quest quest = GenerateQuestOfType(questType, QuestTier.Weekly, weeklyQuestLootbox);
                activeQuests.Add(quest);
            }

            Debug.Log($"[QuestManager] Generated {weeklyQuestCount} weekly quests");
        }

        private Quest GenerateQuestOfType(QuestType type, QuestTier tier, LootboxData lootbox = null)
        {
            int target = GetTargetForType(type, tier);
            int gold = CalculateGoldReward(type, target, tier);
            int xp = CalculateXPReward(type, target, tier);
            string description = GetDescription(type, target, tier);

            return new Quest
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
        }

        private QuestType GetRandomQuestType()
        {
            QuestType[] types = {
                QuestType.PlayGames,
                QuestType.WinGames,
                QuestType.KillEnemies,
                QuestType.BuildTurrets
            };
            return types[UnityEngine.Random.Range(0, types.Length)];
        }

        private int GetTargetForType(QuestType type, QuestTier tier)
        {
            float multiplier = tier == QuestTier.Weekly ? 3f : 1f;

            return type switch
            {
                QuestType.PlayGames => Mathf.RoundToInt(UnityEngine.Random.Range(minPlayGames, maxPlayGames + 1) * multiplier),
                QuestType.WinGames => Mathf.RoundToInt(UnityEngine.Random.Range(minWinGames, maxWinGames + 1) * multiplier),
                QuestType.KillEnemies => Mathf.RoundToInt(UnityEngine.Random.Range(minKillEnemies, maxKillEnemies + 1) * multiplier),
                QuestType.BuildTurrets => Mathf.RoundToInt(UnityEngine.Random.Range(minBuildTurrets, maxBuildTurrets + 1) * multiplier),
                QuestType.SpendGold => Mathf.RoundToInt(UnityEngine.Random.Range(500, 2000) * multiplier),
                QuestType.OpenLootboxes => Mathf.RoundToInt(UnityEngine.Random.Range(1, 3) * multiplier),
                QuestType.UnlockCards => Mathf.RoundToInt(UnityEngine.Random.Range(1, 3) * multiplier),
                _ => 1
            };
        }

        private int CalculateGoldReward(QuestType type, int target, QuestTier tier)
        {
            float baseReward = type switch
            {
                QuestType.WinGames => baseGoldReward * 2f * target,
                QuestType.KillEnemies => baseGoldReward * 0.5f + target * 2,
                QuestType.BuildTurrets => baseGoldReward + target * 5,
                _ => baseGoldReward * target
            };

            if (tier == QuestTier.Weekly)
                baseReward *= weeklyRewardMultiplier;

            return Mathf.RoundToInt(baseReward);
        }

        private int CalculateXPReward(QuestType type, int target, QuestTier tier)
        {
            float baseReward = type switch
            {
                QuestType.WinGames => baseXPReward * 2f * target,
                QuestType.KillEnemies => baseXPReward * 0.3f + target,
                _ => baseXPReward * target
            };

            if (tier == QuestTier.Weekly)
                baseReward *= weeklyRewardMultiplier;

            return Mathf.RoundToInt(baseReward);
        }

        private string GetDescription(QuestType type, int target, QuestTier tier)
        {
            // Prefix dla KA�DEGO tieru
            string prefix = tier switch
            {
                QuestTier.Daily => "[Daily] ",
                QuestTier.Weekly => "[Weekly] ",
                QuestTier.Special => "[Special] ",
                _ => ""
            };

            return type switch
            {
                QuestType.PlayGames => $"{prefix}Play {target} matches",
                QuestType.WinGames => $"{prefix}Win {target} matches",
                QuestType.KillEnemies => $"{prefix}Eliminate {target} enemies",
                QuestType.BuildTurrets => $"{prefix}Build {target} turrets",
                QuestType.SpendGold => $"{prefix}Spend {target} gold",
                QuestType.OpenLootboxes => $"{prefix}Open {target} lootboxes",
                QuestType.UnlockCards => $"{prefix}Unlock {target} new cards",
                _ => $"{prefix}Complete objective"
            };
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        // ==========================================
        // PUBLIC API - ADD QUESTS
        // ==========================================

        /// <summary>
        /// Adds a custom quest
        /// </summary>
        public Quest AddQuest(QuestType type, QuestTier tier, int target, int gold, int xp, LootboxData lootbox = null)
        {
            Quest quest = new Quest
            {
                questID = Guid.NewGuid().ToString(),
                type = type,
                tier = tier,
                description = GetDescription(type, target, tier),
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
        /// Adds a random daily quest
        /// </summary>
        public Quest AddRandomDailyQuest(bool withLootbox = false)
        {
            QuestType type = GetRandomQuestType();
            LootboxData lootbox = withLootbox ? dailyQuestLootbox : null;
            Quest quest = GenerateQuestOfType(type, QuestTier.Daily, lootbox);

            activeQuests.Add(quest);
            SaveQuests();
            OnQuestListUpdated?.Invoke();

            Debug.Log($"[QuestManager] Added random daily quest: {quest.description}");
            return quest;
        }

        /// <summary>
        /// Adds a random weekly quest
        /// </summary>
        public Quest AddRandomWeeklyQuest()
        {
            QuestType type = GetRandomQuestType();
            Quest quest = GenerateQuestOfType(type, QuestTier.Weekly, weeklyQuestLootbox);

            activeQuests.Add(quest);
            SaveQuests();
            OnQuestListUpdated?.Invoke();

            Debug.Log($"[QuestManager] Added random weekly quest: {quest.description}");
            return quest;
        }

        /// <summary>
        /// Adds a special/event quest
        /// </summary>
        public Quest AddSpecialQuest(QuestType type, int target, int gold, int xp, string customDescription = null)
        {
            Quest quest = new Quest
            {
                questID = Guid.NewGuid().ToString(),
                type = type,
                tier = QuestTier.Special,
                description = customDescription ?? $"[Special] {GetDescription(type, target, QuestTier.Special)}",
                targetAmount = target,
                rewardGold = gold,
                rewardXP = xp,
                rewardLootbox = specialQuestLootbox,
                rewardLootboxName = specialQuestLootbox != null ? specialQuestLootbox.name : "",
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
        /// Removes a specific quest
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
        /// Gets quests by tier
        /// </summary>
        public List<Quest> GetQuestsByTier(QuestTier tier)
        {
            return activeQuests.Where(q => q.tier == tier).ToList();
        }

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
                PlayerCollection.Instance.AddXP(quest.rewardXP);
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
                CheckDailyReset(); // <-- ZMIANA TUTAJ
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[QuestManager] Failed to load JSON: {e.Message}");
                activeQuests = new List<Quest>();
                CheckDailyReset(); // <-- I TUTAJ
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
            GenerateDailyQuests();
            SaveQuests();
            OnQuestListUpdated?.Invoke();
            Debug.Log("<color=yellow>[DEBUG] Forced Daily Reset</color>");
        }

        [ContextMenu("DEBUG: Force Weekly Reset")]
        public void DebugForceWeeklyReset()
        {
            lastWeeklyDate = "";
            RemoveQuestsByTier(QuestTier.Weekly);
            GenerateWeeklyQuests();
            SaveQuests();
            OnQuestListUpdated?.Invoke();
            Debug.Log("<color=yellow>[DEBUG] Forced Weekly Reset</color>");
        }

        [ContextMenu("DEBUG: Force Full Reset (Daily + Weekly)")]
        public void DebugForceFullReset()
        {
            lastQuestDate = "";
            lastWeeklyDate = "";
            activeQuests.Clear();

            {
            }

            CheckDailyReset();
            Debug.Log("<color=yellow>[DEBUG] Forced Full Reset</color>");
        }

        [ContextMenu("DEBUG: Add Random Daily Quest")]
        public void DebugAddDailyQuest()
        {
            AddRandomDailyQuest(withLootbox: false);
        }

        [ContextMenu("DEBUG: Add Daily Quest with Lootbox")]
        public void DebugAddDailyQuestWithLootbox()
        {
            AddRandomDailyQuest(withLootbox: true);
        }

        [ContextMenu("DEBUG: Add Weekly Quest")]
        public void DebugAddWeeklyQuest()
        {
            AddRandomWeeklyQuest();
        }

        [ContextMenu("DEBUG: Add Special Quest")]
        public void DebugAddSpecialQuest()
        {
            AddSpecialQuest(QuestType.WinGames, 5, 1000, 500, "[EVENT] Win 5 matches for special reward!");
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

        [ContextMenu("DEBUG: Clear All Quest Data")]
        public void DebugClearAllData()
        {
            string[] files = Directory.GetFiles(Application.persistentDataPath, "Quests_*.json");
            foreach (string file in files)
            {
                File.Delete(file);
                Debug.Log($"[DEBUG] Deleted: {file}");
            }

            activeQuests.Clear();
            lastQuestDate = "";
            lastWeeklyDate = "";
            OnQuestListUpdated?.Invoke();

            Debug.Log("<color=red>[DEBUG] All quest data cleared!</color>");
        }

        [ContextMenu("DEBUG: Remove All Claimed Quests")]
        public void DebugRemoveClaimed()
        {
            int removed = activeQuests.RemoveAll(q => q.isClaimed);
            SaveQuests();
            OnQuestListUpdated?.Invoke();
            Debug.Log($"<color=yellow>[DEBUG] Removed {removed} claimed quests</color>");
        }
    }
}
