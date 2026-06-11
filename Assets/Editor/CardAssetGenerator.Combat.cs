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
        // COMBAT — Common (turret crit + bonus gold/kill)
        // ==========================================

        private static int Combat_Commons()
        {
            int n = 0;

            var lucky = GetOrCreateEffect<CombatModifierEffect>("Combat", "LuckyStrike",
                e => { e.critChanceAdd = 0.05f; e.critMultiplierOverride = 2f; }, ref n);
            CreateCard("Combat", "LuckyStrike", lucky,
                "Lucky Strike", "Turrets gain +5% crit chance (×2 damage on crit).",
                CardRarity.Common, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            var bounty = GetOrCreateEffect<CombatModifierEffect>("Combat", "BountyHunter",
                e => { e.bonusGoldPerKill = 1; e.globalDamagePenaltyPercent = 5f; }, ref n);
            CreateCard("Combat", "BountyHunter", bounty,
                "Bounty Hunter", "+1 gold per kill. -5% damage globally.",
                CardRarity.Common, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            // Salvage — clean common variant of BountyHunter, no downside.
            // Different from BountyHunter because it sacrifices the +1g/kill
            // amount internally (still 1) but pays no damage tax. Designed for
            // newer players who want pure benefit.
            var salvage = GetOrCreateEffect<CombatModifierEffect>("Combat", "Salvage",
                e => { e.bonusGoldPerKill = 1; }, ref n);
            CreateCard("Combat", "Salvage", salvage,
                "Salvage", "+1 gold per kill.",
                CardRarity.Common, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            // Hot Streak — small clean crit chance for newer players.
            var hotStreak = GetOrCreateEffect<CombatModifierEffect>("Combat", "HotStreak",
                e => { e.critChanceAdd = 0.03f; e.critMultiplierOverride = 2f; }, ref n);
            CreateCard("Combat", "HotStreak", hotStreak,
                "Hot Streak", "Turrets gain +3% crit chance.",
                CardRarity.Common, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            return n;
        }

        // ==========================================
        // COMBAT — Rare
        // ==========================================

        private static int Combat_Rares()
        {
            int n = 0;

            var sharp = GetOrCreateEffect<CombatModifierEffect>("Combat", "Sharpshooter",
                e =>
                {
                    e.critChanceAdd = 0.15f;
                    e.critMultiplierOverride = 2f;
                    e.globalFireRatePenaltyPercent = 10f;
                }, ref n);
            CreateCard("Combat", "Sharpshooter", sharp,
                "Sharpshooter", "Turrets gain +15% crit chance. -10% fire rate globally.",
                CardRarity.Rare, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));

            var head = GetOrCreateEffect<CombatModifierEffect>("Combat", "Headhunter",
                e => { e.bonusGoldPerKill = 5; e.globalDamagePenaltyPercent = 10f; }, ref n);
            CreateCard("Combat", "Headhunter", head,
                "Headhunter", "+5 gold per kill. -10% damage globally.",
                CardRarity.Rare, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));

            return n;
        }

        // ==========================================
        // COMBAT — Legendary
        // ==========================================

        private static int Combat_Legendaries()
        {
            int n = 0;

            var crit = GetOrCreateEffect<CombatModifierEffect>("Combat", "CriticalMastery",
                e =>
                {
                    e.critChanceAdd = 0.25f;
                    e.critMultiplierOverride = 3f;
                    e.globalDamagePenaltyPercent = 20f;
                }, ref n);
            CreateCard("Combat", "CriticalMastery", crit,
                "Critical Mastery", "Turrets: +25% crit chance, ×3 crit damage. -20% damage globally.",
                CardRarity.Legendary, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, requiredLevel: 6, unlockCost: UnlockCostForRarity(CardRarity.Legendary));

            var tax = GetOrCreateEffect<CombatModifierEffect>("Combat", "TaxCollector",
                e =>
                {
                    e.bonusGoldPerKill = 3;
                    e.bossKillGoldMultiplier = 1.5f;
                    e.globalDamagePenaltyPercent = 15f;
                }, ref n);
            CreateCard("Combat", "TaxCollector", tax,
                "Tax Collector", "+3 gold per kill. +50% gold from bosses. -15% damage globally.",
                CardRarity.Legendary, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, requiredLevel: 6, unlockCost: UnlockCostForRarity(CardRarity.Legendary));

            return n;
        }

        // ==========================================
        // CONDITIONAL — Common (smaller versions of Rare cards)
        // ==========================================

        private static int Conditional_Commons()
        {
            int n = 0;

            // Wake Up Call — smaller WaveRush, only the opening burst.
            var wakeUp = GetOrCreateEffect<ConditionalEffect>("Conditional", "WakeUpCall",
                e =>
                {
                    e.context = PlayerModifierStack.ConditionalContext.WaveOpening;
                    e.thresholdValue = 5f;
                    e.bonusDamagePercent = 15f;
                }, ref n);
            CreateCard("Conditional", "WakeUpCall", wakeUp,
                "Wake Up Call", "First 5s of every wave: +15% turret damage.",
                CardRarity.Common, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            // Comeback Kid — smaller LastStand with higher HP threshold.
            var comeback = GetOrCreateEffect<ConditionalEffect>("Conditional", "ComebackKid",
                e =>
                {
                    e.context = PlayerModifierStack.ConditionalContext.LowPlayerHp;
                    e.thresholdValue = 0.50f;
                    e.bonusDamagePercent = 15f;
                }, ref n);
            CreateCard("Conditional", "ComebackKid", comeback,
                "Comeback Kid", "While below 50% HP: +15% turret damage.",
                CardRarity.Common, CardType.Defensive, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            return n;
        }

        // ==========================================
        // CONDITIONAL — Rare
        // ==========================================

        private static int Conditional_Rares()
        {
            int n = 0;

            var lastStand = GetOrCreateEffect<ConditionalEffect>("Conditional", "LastStand",
                e =>
                {
                    e.context = PlayerModifierStack.ConditionalContext.LowPlayerHp;
                    e.thresholdValue = 0.30f;
                    e.bonusDamagePercent = 30f;
                }, ref n);
            CreateCard("Conditional", "LastStand", lastStand,
                "Last Stand", "While below 30% HP: +30% turret damage.",
                CardRarity.Rare, CardType.Defensive, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));

            var rush = GetOrCreateEffect<ConditionalEffect>("Conditional", "WaveRush",
                e =>
                {
                    e.context = PlayerModifierStack.ConditionalContext.WaveOpening;
                    e.thresholdValue = 10f;
                    e.bonusDamagePercent = 50f;
                }, ref n);
            CreateCard("Conditional", "WaveRush", rush,
                "Wave Rush", "First 10s of every wave: +50% turret damage.",
                CardRarity.Rare, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));

            return n;
        }

        // ==========================================
        // CONDITIONAL — Legendary
        // ==========================================

        private static int Conditional_Legendaries()
        {
            int n = 0;

            var slayer = GetOrCreateEffect<ConditionalEffect>("Conditional", "BossSlayer",
                e =>
                {
                    e.context = PlayerModifierStack.ConditionalContext.VsBoss;
                    e.bonusDamagePercent = 60f;
                    e.normalEnemyPenaltyPercent = 10f;
                }, ref n);
            CreateCard("Conditional", "BossSlayer", slayer,
                "Boss Slayer", "+60% damage vs bosses. -10% damage vs normal enemies.",
                CardRarity.Legendary, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, requiredLevel: 5, unlockCost: UnlockCostForRarity(CardRarity.Legendary));

            var underdog = GetOrCreateEffect<ConditionalEffect>("Conditional", "Underdog",
                e =>
                {
                    e.context = PlayerModifierStack.ConditionalContext.UnderdogGold;
                    e.bonusDamagePercent = 30f;
                }, ref n);
            CreateCard("Conditional", "Underdog", underdog,
                "Underdog", "While behind in gold vs opponent: +30% turret damage.",
                CardRarity.Legendary, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, requiredLevel: 5, unlockCost: UnlockCostForRarity(CardRarity.Legendary));

            return n;
        }

        // ==========================================
        // WAVE TRIGGER — Common / Rare / Legendary
        // ==========================================

        private static int WaveTrigger_Commons()
        {
            int n = 0;
            var refresh = GetOrCreateEffect<WaveTriggerEffect>("WaveTrigger", "Refresh",
                e => { e.everyNWaves = 5; e.goldReward = 200; }, ref n);
            CreateCard("WaveTrigger", "Refresh", refresh,
                "Refresh", "Every 5 waves: +200 gold.",
                CardRarity.Common, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            // Coffee Break — smaller cycle. Lighter version players take when
            // they want a steady drip without committing 5 waves to a payout.
            var coffee = GetOrCreateEffect<WaveTriggerEffect>("WaveTrigger", "CoffeeBreak",
                e => { e.everyNWaves = 4; e.goldReward = 120; }, ref n);
            CreateCard("WaveTrigger", "CoffeeBreak", coffee,
                "Coffee Break", "Every 4 waves: +120 gold.",
                CardRarity.Common, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            return n;
        }

        private static int WaveTrigger_Rares()
        {
            int n = 0;
            var mastery = GetOrCreateEffect<WaveTriggerEffect>("WaveTrigger", "WaveMastery",
                e => { e.everyNWaves = 3; e.goldReward = 150; }, ref n);
            CreateCard("WaveTrigger", "WaveMastery", mastery,
                "Wave Mastery", "Every 3 waves: +150 gold.",
                CardRarity.Rare, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));
            return n;
        }

        private static int WaveTrigger_Legendaries()
        {
            int n = 0;
            var apex = GetOrCreateEffect<WaveTriggerEffect>("WaveTrigger", "ApexCycle",
                e => { e.everyNWaves = 2; e.goldReward = 100; }, ref n);
            CreateCard("WaveTrigger", "ApexCycle", apex,
                "Apex Cycle", "Every 2 waves: +100 gold.",
                CardRarity.Legendary, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, requiredLevel: 6, unlockCost: UnlockCostForRarity(CardRarity.Legendary));
            return n;
        }

        // ==========================================
        // SYNERGY — Rare / Legendary
        // ==========================================

        private static int Synergy_Rares()
        {
            int n = 0;

            // Element bond ×6 — one Rare per element
            foreach (var element in AllElements)
            {
                var bond = GetOrCreateEffect<SynergyEffect>(
                    "Synergy", $"ElementalBond_{element}",
                    e =>
                    {
                        e.scaleBy = SynergyEffect.ScaleSource.ElementCardsInDeck;
                        e.targetElement = element;
                        e.maxStacks = 6;
                        e.bonusDamagePercentPerStack = 5f;
                    }, ref n);
                CreateCard("Synergy", $"ElementalBond_{element}", bond,
                    $"Elemental Bond: {element}",
                    $"+5% turret damage per {element} card in your deck (max 6 stacks).",
                    CardRarity.Rare, CardType.Turret, CardActivationType.Continuous,
                    element, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));
            }

            var wealthy = GetOrCreateEffect<SynergyEffect>("Synergy", "Wealthy",
                e =>
                {
                    e.scaleBy = SynergyEffect.ScaleSource.EconomyCardsInDeck;
                    e.maxStacks = 5;
                    e.bonusGoldPerSecondPerStack = 2;
                }, ref n);
            CreateCard("Synergy", "Wealthy", wealthy,
                "Wealthy", "+2 gold/s per Economy card in your deck (max 5 stacks).",
                CardRarity.Rare, CardType.Economy, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));

            return n;
        }

        private static int Synergy_Legendaries()
        {
            int n = 0;
            var poly = GetOrCreateEffect<SynergyEffect>("Synergy", "Polymath",
                e =>
                {
                    e.scaleBy = SynergyEffect.ScaleSource.UniqueElementsInDeck;
                    e.maxStacks = 6;
                    e.bonusDamagePercentPerStack = 5f;
                }, ref n);
            CreateCard("Synergy", "Polymath", poly,
                "Polymath", "+5% turret damage per unique element in your deck (max 6 stacks).",
                CardRarity.Legendary, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, requiredLevel: 6, unlockCost: UnlockCostForRarity(CardRarity.Legendary));
            return n;
        }
    }
}
#endif
