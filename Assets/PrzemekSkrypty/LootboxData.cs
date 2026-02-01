using UnityEngine;

namespace ElementumDefense.Cards
{
    [CreateAssetMenu(fileName = "New Lootbox", menuName = "Tower Defense/Lootbox Data")]
    public class LootboxData : ScriptableObject
    {
        [Header("Settings")]
        public string boxName = "Standard Box";
        public int cardCount = 3; // 3 dla zwyk³ej, 5 dla legendarnej
        public int priceGold = 1000; // Cena w z³ocie (opcjonalnie)

        [Header("Drop Chances (Must sum to 100)")]
        [Range(0, 100)] public float legendaryChance = 5f;
        [Range(0, 100)] public float rareChance = 25f;
        [Range(0, 100)] public float commonChance = 70f;

        [Header("Duplicate Refunds")]
        public int refundCommonGold = 50;
        public int refundRareGold = 150;
        public int refundLegendaryCrystals = 10; // Legendarne daj¹ kryszta³y!
    }
}