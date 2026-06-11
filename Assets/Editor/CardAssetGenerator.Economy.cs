#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ElementumDefense.Cards;
using ElementumDefense.Elements;

namespace ElementumDefense.EditorTools
{
    public static partial class CardAssetGenerator
    {
        // ==========================================
        // ECONOMY — Common
        // ==========================================

        private static int Economy_Commons()
        {
            int n = 0;

            var pocket = GetOrCreateEffect<EconomyCardEffect>("Economy", "PocketChange",
                e => { e.goldPerSecond = 2; }, ref n);
            CreateCard("Economy", "PocketChange", pocket,
                "Pocket Change", "+2 gold/s passive income.",
                CardRarity.Common, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common), starter: true);

            var stash = GetOrCreateEffect<EconomyCardEffect>("Economy", "CoinStash",
                e => { e.instantGoldBonus = 150; }, ref n);
            CreateCard("Economy", "CoinStash", stash,
                "Coin Stash", "+150 gold instantly when drafted.",
                CardRarity.Common, CardType.Economy, CardActivationType.OnDraft,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            var bargain = GetOrCreateEffect<EconomyCardEffect>("Economy", "BargainHunter",
                e => { e.turretCostDiscount = 5f; }, ref n);
            CreateCard("Economy", "BargainHunter", bargain,
                "Bargain Hunter", "Turrets cost 5% less.",
                CardRarity.Common, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            // Penny Wise — light combo: discount + small passive income.
            var pennyWise = GetOrCreateEffect<EconomyCardEffect>("Economy", "PennyWise",
                e => { e.goldPerSecond = 1; e.turretCostDiscount = 5f; }, ref n);
            CreateCard("Economy", "PennyWise", pennyWise,
                "Penny Wise", "+1 gold/s and -5% turret cost.",
                CardRarity.Common, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            // Loose Change — combines OnDraft kicker with a passive trickle.
            var looseChange = GetOrCreateEffect<EconomyCardEffect>("Economy", "LooseChange",
                e => { e.instantGoldBonus = 50; e.goldPerSecond = 1; }, ref n);
            CreateCard("Economy", "LooseChange", looseChange,
                "Loose Change", "+50 gold instantly + 1 gold/s.",
                CardRarity.Common, CardType.Economy, CardActivationType.OnDraft,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            return n;
        }

        // ==========================================
        // ECONOMY — Rare
        // ==========================================

        private static int Economy_Rares()
        {
            int n = 0;

            var mine = GetOrCreateEffect<EconomyCardEffect>("Economy", "GoldMine",
                e => { e.goldPerSecond = 5; e.globalDamagePenaltyPercent = 5f; }, ref n);
            CreateCard("Economy", "GoldMine", mine,
                "Gold Mine", "+5 gold/s. -5% damage globally.",
                CardRarity.Rare, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));

            var raid = GetOrCreateEffect<EconomyCardEffect>("Economy", "TreasuryRaid",
                e => { e.instantGoldBonus = 400; }, ref n);
            CreateCard("Economy", "TreasuryRaid", raid,
                "Treasury Raid", "+400 gold instantly when drafted.",
                CardRarity.Rare, CardType.Economy, CardActivationType.OnDraft,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));

            var discount = GetOrCreateEffect<EconomyCardEffect>("Economy", "DiscountDay",
                e => { e.turretCostDiscount = 15f; e.globalRangePenaltyPercent = 10f; }, ref n);
            CreateCard("Economy", "DiscountDay", discount,
                "Discount Day", "Turrets cost 15% less. -10% range globally.",
                CardRarity.Rare, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));

            var investor = GetOrCreateEffect<EconomyCardEffect>("Economy", "InvestorsEdge",
                e =>
                {
                    e.goldPerSecond = 3;
                    e.turretCostDiscount = 5f;
                    e.globalFireRatePenaltyPercent = 10f;
                }, ref n);
            CreateCard("Economy", "InvestorsEdge", investor,
                "Investor's Edge", "+3 gold/s, -5% turret cost. -10% fire rate globally.",
                CardRarity.Rare, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));

            return n;
        }

        // ==========================================
        // ECONOMY — Legendary
        // ==========================================

        private static int Economy_Legendaries()
        {
            int n = 0;

            var goose = GetOrCreateEffect<EconomyCardEffect>("Economy", "GoldenGoose",
                e => { e.goldPerSecond = 12; e.globalDamagePenaltyPercent = 15f; }, ref n);
            CreateCard("Economy", "GoldenGoose", goose,
                "Golden Goose", "+12 gold/s. -15% damage globally.",
                CardRarity.Legendary, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, requiredLevel: 5, unlockCost: UnlockCostForRarity(CardRarity.Legendary));

            var royal = GetOrCreateEffect<EconomyCardEffect>("Economy", "RoyalTreasury",
                e => { e.instantGoldBonus = 1000; }, ref n);
            CreateCard("Economy", "RoyalTreasury", royal,
                "Royal Treasury", "+1000 gold instantly when drafted.",
                CardRarity.Legendary, CardType.Economy, CardActivationType.OnDraft,
                ElementType.None, ref n, requiredLevel: 5, unlockCost: UnlockCostForRarity(CardRarity.Legendary));

            var market = GetOrCreateEffect<EconomyCardEffect>("Economy", "FreeMarket",
                e => { e.turretCostDiscount = 30f; e.globalDamagePenaltyPercent = 20f; }, ref n);
            CreateCard("Economy", "FreeMarket", market,
                "Free Market", "Turrets cost 30% less. -20% damage globally.",
                CardRarity.Legendary, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, requiredLevel: 5, unlockCost: UnlockCostForRarity(CardRarity.Legendary));

            var midas = GetOrCreateEffect<EconomyCardEffect>("Economy", "MidasTouch",
                e => { e.goldPerSecond = 8; e.turretCostDiscount = 15f; e.globalRangePenaltyPercent = 15f; }, ref n);
            CreateCard("Economy", "MidasTouch", midas,
                "Midas Touch", "+8 gold/s, -15% turret cost. -15% range globally.",
                CardRarity.Legendary, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, requiredLevel: 6, unlockCost: UnlockCostForRarity(CardRarity.Legendary));

            return n;
        }

        // ==========================================
        // UTILITY — speed-only commons / rares + Phoenix legendary
        // ==========================================

        private static int Utility_Commons()
        {
            int n = 0;
            var step = GetOrCreateEffect<UtilityCardEffect>("Utility", "QuickStep",
                e => { e.moveSpeedPercent = 10f; }, ref n);
            CreateCard("Utility", "QuickStep", step,
                "Quick Step", "+10% move speed.",
                CardRarity.Common, CardType.Utility, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));
            return n;
        }

        private static int Utility_Rares()
        {
            int n = 0;
            var sprinter = GetOrCreateEffect<UtilityCardEffect>("Utility", "Sprinter",
                e => { e.moveSpeedPercent = 25f; }, ref n);
            CreateCard("Utility", "Sprinter", sprinter,
                "Sprinter", "+25% move speed.",
                CardRarity.Rare, CardType.Utility, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));
            return n;
        }

        private static int Utility_Legendaries()
        {
            int n = 0;
            var reflexes = GetOrCreateEffect<UtilityCardEffect>("Utility", "LightningReflexes",
                e => { e.moveSpeedPercent = 40f; }, ref n);
            CreateCard("Utility", "LightningReflexes", reflexes,
                "Lightning Reflexes", "+40% move speed.",
                CardRarity.Legendary, CardType.Utility, CardActivationType.Continuous,
                ElementType.None, ref n, requiredLevel: 4, unlockCost: UnlockCostForRarity(CardRarity.Legendary));

            // Phoenix Heart — the only HP-touching card. Revive once at 10 HP.
            var phoenix = GetOrCreateEffect<PhoenixHeartEffect>("Utility", "PhoenixHeart",
                e => { e.reviveHp = 10; e.maxRevives = 1; }, ref n);
            CreateCard("Utility", "PhoenixHeart", phoenix,
                "Phoenix Heart", "Once per match: when lethal damage would kill you, revive at 10 HP.",
                CardRarity.Legendary, CardType.Defensive, CardActivationType.Continuous,
                ElementType.None, ref n, requiredLevel: 7, unlockCost: UnlockCostForRarity(CardRarity.Legendary));

            return n;
        }
    }
}
#endif
