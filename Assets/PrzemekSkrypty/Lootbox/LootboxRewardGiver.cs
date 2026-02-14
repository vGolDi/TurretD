// Assets/PrzemekSkrypty/Lootbox/LootboxRewardGiver.cs
using UnityEngine;
using System.Collections.Generic;
using ElementumDefense.Progression;
using ElementumDefense.Cards;

namespace ElementumDefense.Lootbox
{
    public class LootboxRewardGiver : MonoBehaviour
    {
        public static LootboxRewardGiver Instance { get; private set; }

        [Header("Level Completion Rewards")]
        [SerializeField] private LootboxData levelCompletionReward;
        [SerializeField] private int rareBoxEveryXLevels = 5;
        [SerializeField] private LootboxData rareLevelReward;
        [SerializeField] private int legendaryBoxEveryXLevels = 10;
        [SerializeField] private LootboxData legendaryLevelReward;

        [Header("Win Streak Rewards")]
        [SerializeField] private int streakForBonus = 3;
        [SerializeField] private LootboxData streakBonusReward;

        [Header("First Win of the Day")]
        [SerializeField] private LootboxData firstWinReward;
        [SerializeField] private bool enableFirstWinBonus = true;

        // Events - CENTRALNY HUB dla popup
        public System.Action<LootboxData, string> OnLootboxRewarded;

        private int currentWinStreak = 0;
        private string lastFirstWinDate = "";

        private const string FIRST_WIN_DATE_KEY = "FirstWinDate";
        private const string WIN_STREAK_KEY = "WinStreak";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadStreakData();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        // ==========================================
        // EVENT SUBSCRIPTIONS - CENTRALNY HUB
        // ==========================================

        private void SubscribeToEvents()
        {
            // QuestManager - nagrody z questów
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnLootboxRewarded -= OnQuestLootboxRewarded;
                QuestManager.Instance.OnLootboxRewarded += OnQuestLootboxRewarded;
                Debug.Log("[LootboxRewardGiver] Subscribed to QuestManager");
            }

            // PlayerCollection - nagrody za level-up
            if (PlayerCollection.Instance != null)
            {
                PlayerCollection.Instance.OnLootboxRewarded -= OnPlayerLevelUpLootbox;
                PlayerCollection.Instance.OnLootboxRewarded += OnPlayerLevelUpLootbox;
                Debug.Log("[LootboxRewardGiver] Subscribed to PlayerCollection");
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnLootboxRewarded -= OnQuestLootboxRewarded;
            }

            if (PlayerCollection.Instance != null)
            {
                PlayerCollection.Instance.OnLootboxRewarded -= OnPlayerLevelUpLootbox;
            }
        }

        // ==========================================
        // EVENT HANDLERS
        // ==========================================

        private void OnQuestLootboxRewarded(LootboxData lootbox)
        {
            if (lootbox == null) return;

            // Przeka¿ do popup przez centralny event
            OnLootboxRewarded?.Invoke(lootbox, "Quest Completed!");
            Debug.Log($"[LootboxRewardGiver] Quest rewarded lootbox: {lootbox.lootboxName}");
        }

        private void OnPlayerLevelUpLootbox(LootboxData lootbox)
        {
            if (lootbox == null) return;

            int level = PlayerCollection.Instance?.GetLevel() ?? 0;
            string reason = $"Reached Level {level}!";

            // Przeka¿ do popup przez centralny event
            OnLootboxRewarded?.Invoke(lootbox, reason);
            Debug.Log($"[LootboxRewardGiver] Level-up rewarded lootbox: {lootbox.lootboxName}");
        }

        // ==========================================
        // GAME RESULT REWARDS
        // ==========================================

        public void RewardForGameResult(int waveNumber, bool wasVictory)
        {
            if (!wasVictory)
            {
                currentWinStreak = 0;
                SaveStreakData();
                return;
            }

            currentWinStreak++;

            // Check First Win of the Day
            if (enableFirstWinBonus && CheckFirstWinOfDay())
            {
                GiveLootbox(firstWinReward, "First Win of the Day!");
            }

            // Normal level reward
            LootboxData reward = null;
            string reason = "";

            if (legendaryLevelReward != null && waveNumber % legendaryBoxEveryXLevels == 0)
            {
                reward = legendaryLevelReward;
                reason = $"Wave {waveNumber} Complete (Milestone!)";
            }
            else if (rareLevelReward != null && waveNumber % rareBoxEveryXLevels == 0)
            {
                reward = rareLevelReward;
                reason = $"Wave {waveNumber} Complete (Bonus!)";
            }
            else if (levelCompletionReward != null)
            {
                reward = levelCompletionReward;
                reason = $"Wave {waveNumber} Complete";
            }

            if (reward != null)
            {
                GiveLootbox(reward, reason);
            }

            // Win streak bonus
            if (streakBonusReward != null && currentWinStreak >= streakForBonus)
            {
                GiveLootbox(streakBonusReward, $"{streakForBonus} Win Streak!");
                currentWinStreak = 0;
            }

            SaveStreakData();
        }

        private bool CheckFirstWinOfDay()
        {
            string today = System.DateTime.Now.ToString("yyyy-MM-dd");

            if (lastFirstWinDate != today)
            {
                lastFirstWinDate = today;
                PlayerPrefs.SetString(FIRST_WIN_DATE_KEY, today);
                PlayerPrefs.Save();
                return true;
            }

            return false;
        }

        // ==========================================
        // DIRECT REWARD METHODS
        // ==========================================

        /// <summary>
        /// Gives lootbox to player AND triggers popup
        /// </summary>
        public void GiveLootbox(LootboxData lootboxType, string reason = "Reward")
        {
            if (lootboxType == null) return;

            LootboxInventory inventory = LootboxInventory.Instance;

            if (inventory != null)
            {
                inventory.AddLootbox(lootboxType, 1);

                // Trigger popup
                OnLootboxRewarded?.Invoke(lootboxType, reason);

                Debug.Log($"[LootboxRewardGiver] Gave {lootboxType.lootboxName}: {reason}");
            }
            else
            {
                Debug.LogError("[LootboxRewardGiver] LootboxInventory not found!");
            }
        }

        /// <summary>
        /// Gives multiple lootboxes
        /// </summary>
        public void GiveLootboxes(LootboxData lootboxType, int count, string reason = "Reward")
        {
            if (lootboxType == null || count <= 0) return;

            LootboxInventory inventory = LootboxInventory.Instance;

            if (inventory != null)
            {
                inventory.AddLootbox(lootboxType, count);

                // Trigger popup
                OnLootboxRewarded?.Invoke(lootboxType, $"{reason} (x{count})");

                Debug.Log($"[LootboxRewardGiver] Gave {count}x {lootboxType.lootboxName}: {reason}");
            }
        }

        // ==========================================
        // SAVE/LOAD
        // ==========================================

        private void SaveStreakData()
        {
            PlayerPrefs.SetInt(WIN_STREAK_KEY, currentWinStreak);
            PlayerPrefs.Save();
        }

        private void LoadStreakData()
        {
            currentWinStreak = PlayerPrefs.GetInt(WIN_STREAK_KEY, 0);
            lastFirstWinDate = PlayerPrefs.GetString(FIRST_WIN_DATE_KEY, "");
        }

        // ==========================================
        // DEBUG
        // ==========================================

        [ContextMenu("Test Give Common Lootbox")]
        private void TestGiveCommon()
        {
            LootboxData[] lootboxes = Resources.LoadAll<LootboxData>("Lootboxes");
            var common = System.Array.Find(lootboxes, l => l.rarity == LootboxRarity.Common);
            if (common != null)
            {
                GiveLootbox(common, "Debug Test Reward");
            }
        }

        [ContextMenu("Test Give Legendary Lootbox")]
        private void TestGiveLegendary()
        {
            LootboxData[] lootboxes = Resources.LoadAll<LootboxData>("Lootboxes");
            var legendary = System.Array.Find(lootboxes, l => l.rarity == LootboxRarity.Legendary);
            if (legendary != null)
            {
                GiveLootbox(legendary, "Debug Legendary!");
            }
        }
    }
}