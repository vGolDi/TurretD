using UnityEngine;
using System;
using System.Collections.Generic;
using ElementumDefense.UI; // Zak³adam, ¿e tu bêd¹ elementy UI
using ElementumDefense.Cards;

namespace ElementumDefense.Progression
{
    public enum QuestType
    {
        PlayGames,      // Zagraj X gier
        WinGames,       // Wygraj X gier
        KillEnemies,    // Zabij X wrogów
        BuildTurrets    // Zbuduj X wie¿
    }

    [Serializable]
    public class Quest
    {
        public string questID; // Unikalne ID do zapisu
        public QuestType type;
        public string description;
        public int currentProgress;
        public int targetAmount;
        public int rewardGold;
        public int rewardXP;
        public bool isCompleted;
        public bool isClaimed;

        // Helper do UI
        public float GetProgress01() => Mathf.Clamp01((float)currentProgress / targetAmount);
    }

    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        public event Action OnQuestListUpdated;


        [Header("Settings")]
        [SerializeField] private int dailyQuestCount = 3;

        public List<Quest> activeQuests = new List<Quest>();

        // Klucze zapisu
        private const string QUESTS_SAVE_KEY = "ActiveQuests_V1";
        private const string LAST_LOGIN_DATE_KEY = "LastQuestDate";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            CheckDailyReset();
        }

        /// <summary>
        /// Sprawdza, czy jest nowy dzieñ. Jeœli tak -> generuje nowe questy.
        /// </summary>
        private void CheckDailyReset()
        {
            string savedDate = PlayerPrefs.GetString(LAST_LOGIN_DATE_KEY, "");
            string todayDate = DateTime.Now.ToString("yyyy-MM-dd"); // Format: 2023-10-27

            Debug.Log($"[QuestManager] Saved Date: '{savedDate}', Today: '{todayDate}'");

            if (savedDate != todayDate)
            {
                Debug.Log("[QuestManager] New day detected! Generating fresh quests.");
                GenerateNewDailyQuests();

                // Zapisz dzisiejsz¹ datê jako ostatni¹
                PlayerPrefs.SetString(LAST_LOGIN_DATE_KEY, todayDate);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.Log("[QuestManager] Same day. Loading existing quests.");
                LoadQuests();
            }
        }

        private void GenerateNewDailyQuests()
        {
            activeQuests.Clear();

            // Tutaj prosta logika losowania (mo¿na rozbudowaæ)
            activeQuests.Add(CreateQuest(QuestType.PlayGames, 3, 100, 50)); // Zagraj 3
            activeQuests.Add(CreateQuest(QuestType.KillEnemies, 20, 150, 100)); // Zabij 20
            activeQuests.Add(CreateQuest(QuestType.WinGames, 1, 300, 200)); // Wygraj 1

            SaveQuests();
            OnQuestListUpdated?.Invoke();
        }

        private Quest CreateQuest(QuestType type, int target, int gold, int xp)
        {
            return new Quest
            {
                questID = Guid.NewGuid().ToString(),
                type = type,
                description = GetDescription(type, target),
                targetAmount = target,
                rewardGold = gold,
                rewardXP = xp,
                currentProgress = 0,
                isCompleted = false,
                isClaimed = false
            };
        }

        private string GetDescription(QuestType type, int target)
        {
            return type switch
            {
                QuestType.PlayGames => $"Play {target} matches",
                QuestType.WinGames => $"Win {target} matches",
                QuestType.KillEnemies => $"Eliminate {target} enemies",
                QuestType.BuildTurrets => $"Build {target} turrets",
                _ => "Do something"
            };
        }

        // ==========================================
        // LOGIKA POSTÊPU I ODBIERANIA
        // ==========================================

        public void ReportProgress(QuestType type, int amount)
        {
            bool changed = false;
            foreach (var quest in activeQuests)
            {
                // Tylko jeœli typ siê zgadza I nie jest jeszcze skoñczony
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

            if (changed) SaveQuests();
        }

        public void ClaimReward(Quest quest)
        {
            if (quest.isCompleted && !quest.isClaimed)
            {
                // Przyznaj nagrody
                if (PlayerCollection.Instance != null)
                {
                    PlayerCollection.Instance.AddGold(quest.rewardGold);
                    PlayerCollection.Instance.AddXP(quest.rewardXP);
                }

                quest.isClaimed = true;
                SaveQuests(); // Zapisz stan (¿e odebrano)

                Debug.Log($"[QuestManager] Reward claimed: {quest.rewardGold}g, {quest.rewardXP}xp");

                OnQuestListUpdated?.Invoke();
            }
        }

        // ==========================================
        // ZAPIS I ODCZYT (JSON Wrapper)
        // ==========================================

        [Serializable]
        private class QuestListWrapper { public List<Quest> quests; }

        private void SaveQuests()
        {
            string json = JsonUtility.ToJson(new QuestListWrapper { quests = activeQuests });
            PlayerPrefs.SetString(QUESTS_SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadQuests()
        {
            if (PlayerPrefs.HasKey(QUESTS_SAVE_KEY))
            {
                string json = PlayerPrefs.GetString(QUESTS_SAVE_KEY);
                try
                {
                    QuestListWrapper wrapper = JsonUtility.FromJson<QuestListWrapper>(json);
                    activeQuests = wrapper.quests ?? new List<Quest>();
                }
                catch
                {
                    GenerateNewDailyQuests(); // Fallback w razie b³êdu JSON
                }
            }
            else
            {
                GenerateNewDailyQuests();
            }
        }

        // ==========================================
        // DEBUG TOOLS (Context Menu)
        // ==========================================

        [ContextMenu("DEBUG: Force Midnight Reset")]
        public void DebugForceReset()
        {
            PlayerPrefs.DeleteKey(LAST_LOGIN_DATE_KEY);
            CheckDailyReset(); // To wywo³a Generate -> Invoke Event -> UI Refresh
            Debug.Log("<color=yellow>[DEBUG] Forced Daily Reset executed.</color>");
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
            Debug.Log("<color=green>[DEBUG] All quests completed.</color>");
        }
    }
}
