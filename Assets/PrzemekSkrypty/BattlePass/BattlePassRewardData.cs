// Assets/PrzemekSkrypty/BattlePass/BattlePassRewardData.cs
using UnityEngine;
using ElementumDefense.Lootbox;
using ElementumDefense.Skins;

namespace ElementumDefense.BattlePass
{
    /// <summary>
    /// Defines a single reward in a Battle Pass tier.
    /// </summary>
    [System.Serializable]
    public class BattlePassRewardData
    {
        [Header("Reward Type")]
        public BPRewardType rewardType = BPRewardType.Gold;

        [Header("Currency Rewards")]
        [Tooltip("Amount of gold/crystals to grant")]
        [Min(0)]
        public int currencyAmount = 100;

        [Header("Lootbox Reward")]
        public LootboxData lootbox;
        [Min(1)]
        public int lootboxQuantity = 1;

        [Header("Skin Reward")]
        public SkinData skin;

        [Header("Visual")]
        [Tooltip("Override icon for this reward (auto-picks from lootbox/skin if null)")]
        public Sprite overrideIcon;

        [Tooltip("Display name override (auto-generates if empty)")]
        public string overrideDisplayName;

        // ==========================================
        // HELPERS
        // ==========================================

        public string GetDisplayName()
        {
            if (!string.IsNullOrEmpty(overrideDisplayName))
                return overrideDisplayName;

            return rewardType switch
            {
                BPRewardType.Gold => $"{currencyAmount} Gold",
                BPRewardType.Crystals => $"{currencyAmount} Crystals",
                BPRewardType.Lootbox => lootbox != null ? lootbox.lootboxName : "Lootbox",
                BPRewardType.Skin => skin != null ? skin.skinName : "Skin",
                _ => "Reward"
            };
        }

        public Sprite GetIcon()
        {
            if (overrideIcon != null) return overrideIcon;

            return rewardType switch
            {
                BPRewardType.Lootbox => lootbox?.lootboxIcon,
                BPRewardType.Skin => skin?.previewIcon,
                _ => null
            };
        }
    }

    public enum BPRewardType
    {
        Gold,
        Crystals,
        Lootbox,
        Skin
    }
}
