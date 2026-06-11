// Assets/PrzemekSkrypty/Quest/QuestData.cs
using UnityEngine;
using ElementumDefense.Lootbox;

namespace ElementumDefense.Progression
{
    /// <summary>
    /// ScriptableObject defining a single quest template.
    /// Create via: Right Click → Create → Tower Defense → Quests → Quest Data
    /// 
    /// Place in Resources/Quests/ for auto-loading.
    /// The QuestManager picks random quests from this pool each day/week.
    /// </summary>
    [CreateAssetMenu(fileName = "New Quest", menuName = "Tower Defense/Quests/Quest Data")]
    public class QuestData : ScriptableObject
    {
        [Header("=== IDENTITY ===")]
        [Tooltip("Unique quest ID. Auto-fills from asset name if empty.")]
        public string questId;

        [TextArea(1, 3)]
        [Tooltip("Description shown to the player. Use {target} as placeholder for targetAmount.")]
        public string description = "Complete the objective";

        [Header("=== QUEST TYPE ===")]
        public QuestType questType = QuestType.PlayGames;
        public QuestTier questTier = QuestTier.Daily;

        [Header("=== OBJECTIVE ===")]
        [Tooltip("How much the player needs to achieve (e.g. 3 wins, 50 kills)")]
        [Min(1)]
        public int targetAmount = 3;

        [Header("=== REWARDS ===")]
        [Min(0)]
        public int rewardGold = 100;

        [Min(0), Tooltip("Crystals (premium currency) reward")]
        public int rewardCrystals = 0;

        [Min(0), Tooltip("XP awarded to player level progression")]
        public int rewardXP = 50;

        [Min(0), Tooltip("XP awarded to Battle Pass progression (independent from player XP)")]
        public int rewardBPXP = 0;

        [Tooltip("Optional lootbox reward. Leave empty for no lootbox.")]
        public LootboxData rewardLootbox;

        [Header("=== SETTINGS ===")]
        [Tooltip("Weight for random selection. Higher = more likely to be picked.")]
        [Min(1)]
        public int selectionWeight = 10;

        [Tooltip("Minimum player level to receive this quest. 0 = no requirement.")]
        [Min(0)]
        public int requiredLevel = 0;

        [Tooltip("If true, this quest won't be randomly assigned (only manually via code).")]
        public bool manualOnly = false;

        // ==========================================
        // HELPERS
        // ==========================================

        /// <summary>
        /// Returns the description with {target} replaced by actual targetAmount.
        /// </summary>
        public string GetFormattedDescription()
        {
            string prefix = questTier switch
            {
                QuestTier.Daily => "[Daily] ",
                QuestTier.Weekly => "[Weekly] ",
                QuestTier.Special => "[Special] ",
                _ => ""
            };

            return prefix + description.Replace("{target}", targetAmount.ToString());
        }

        /// <summary>
        /// Creates a runtime Quest instance from this template.
        /// </summary>
        public Quest CreateRuntimeQuest()
        {
            return new Quest
            {
                questID = System.Guid.NewGuid().ToString(),
                type = questType,
                tier = questTier,
                description = GetFormattedDescription(),
                targetAmount = targetAmount,
                rewardGold = rewardGold,
                rewardCrystals = rewardCrystals,
                rewardXP = rewardXP,
                rewardBPXP = rewardBPXP,
                rewardLootbox = rewardLootbox,
                rewardLootboxName = rewardLootbox != null ? rewardLootbox.name : "",
                currentProgress = 0,
                isCompleted = false,
                isClaimed = false
            };
        }

        // ==========================================
        // EDITOR
        // ==========================================

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(questId))
                questId = name;
        }
    }
}
