using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Handles the logic of opening lootboxes and processing rewards/refunds.
    /// </summary>
    public class LootboxManager : MonoBehaviour
    {
        public static LootboxManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Structure to hold the result of a single card drop
        /// </summary>
        public struct LootResult
        {
            public CardData Card;
            public bool IsNew; // True if unlocked, False if duplicate
            public int RefundAmount; // 0 if new
            public bool RefundIsCrystals; // True if refunded in crystals
        }

        public List<LootResult> OpenLootbox(LootboxData boxData)
        {
            List<LootResult> results = new List<LootResult>();
            PlayerCollection collection = PlayerCollection.Instance;

            if (collection == null)
            {
                Debug.LogError("[LootboxManager] PlayerCollection missing!");
                return results;
            }

            for (int i = 0; i < boxData.cardCount; i++)
            {
                // 1. Determine Rarity
                CardRarity rolledRarity = RollRarity(boxData);

                // 2. Pick Random Card of that Rarity
                List<CardData> pool = collection.GetAllCardsByRarity(rolledRarity);

                if (pool.Count == 0) continue; // Should not happen if configured correctly

                CardData drawnCard = pool[Random.Range(0, pool.Count)];
                LootResult result = new LootResult { Card = drawnCard };

                // 3. Check if owned
                if (collection.IsUnlocked(drawnCard))
                {
                    // DUPLICATE -> REFUND
                    result.IsNew = false;

                    if (drawnCard.rarity == CardRarity.Legendary)
                    {
                        result.RefundAmount = boxData.refundLegendaryCrystals;
                        result.RefundIsCrystals = true;
                        collection.AddCrystals(result.RefundAmount);
                    }
                    else
                    {
                        result.RefundAmount = drawnCard.rarity == CardRarity.Rare ? boxData.refundRareGold : boxData.refundCommonGold;
                        result.RefundIsCrystals = false;
                        collection.AddGold(result.RefundAmount);
                    }
                    Debug.Log($"[Lootbox] Duplicate {drawnCard.cardName}. Refunded {result.RefundAmount} currency.");
                }
                else
                {
                    // NEW CARD -> UNLOCK
                    result.IsNew = true;
                    result.RefundAmount = 0;
                    collection.UnlockCard(drawnCard); // Save is triggered inside UnlockCard
                }

                results.Add(result);
            }

            return results;
        }

        private CardRarity RollRarity(LootboxData data)
        {
            float roll = Random.Range(0f, 100f);

            // Check Legendary first (e.g. 0-5)
            if (roll < data.legendaryChance)
                return CardRarity.Legendary;

            // Check Rare (e.g. 5-30)
            if (roll < data.legendaryChance + data.rareChance)
                return CardRarity.Rare;

            // Default Common
            return CardRarity.Common;
        }
    }
}