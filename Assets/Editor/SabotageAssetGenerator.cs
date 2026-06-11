#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ElementumDefense.Cards;
using ElementumDefense.Elements;

namespace ElementumDefense.EditorTools
{
    /// <summary>
    /// One-stop generator for sabotage SO families.
    /// 
    /// Run via: Tools → Sabotages → Generate All
    /// 
    /// Idempotent — running twice will skip files that already exist.
    /// Use "Force Regenerate" to overwrite.
    /// 
    /// Output:
    ///   Resources/SabotageEffects/Generated/{Family}/{Variant}_Effect.asset
    ///   Resources/Cards/Sabotages/Generated/{Family}/{Variant}_Card.asset
    /// </summary>
    public static class SabotageAssetGenerator
    {
        private const string EFFECTS_ROOT = "Assets/Resources/SabotageEffects/Generated";
        private const string CARDS_ROOT = "Assets/Resources/Cards/Sabotages/Generated";

        // ==========================================
        // CONFIG: defaults that apply to every generated card
        // ==========================================

        private static readonly ElementType[] AllElementsExceptNone = new[]
        {
            ElementType.Fire, ElementType.Ice, ElementType.Lightning,
            ElementType.Nature, ElementType.Dark, ElementType.Light
        };

        // Natural counter map — used by ElementBlock to pick which element enemies should become
        // when one of the player's elements is blocked. Idea: blocking Fire forces Ice enemies
        // (the Fire counter), so the player feels the loss.
        // Pairings reflect the 6-element matchup chart in ElementUtility.
        private static readonly Dictionary<ElementType, ElementType> NaturalCounter = new Dictionary<ElementType, ElementType>
        {
            { ElementType.Fire, ElementType.Ice },
            { ElementType.Ice, ElementType.Fire },
            { ElementType.Lightning, ElementType.Nature },
            { ElementType.Nature, ElementType.Lightning },
            { ElementType.Dark, ElementType.Light },
            { ElementType.Light, ElementType.Dark },
        };

        // ==========================================
        // ENTRY POINTS
        // ==========================================

        [MenuItem("Tools/Sabotages/Generate All", false, 1)]
        public static void GenerateAll()
        {
            EnsureFolder(EFFECTS_ROOT);
            EnsureFolder(CARDS_ROOT);

            int created = 0;

            // Element-based families (8 element variants each)
            created += GenerateChangeElementVariants();
            created += GenerateElementBlockVariants();
            created += GenerateElementResistVariants();

            // Rarity-based opponent sabotages (Common / Rare / Legendary)
            created += GenerateWaveHPVariants();
            created += GenerateWaveSpeedVariants();
            created += GenerateWaveCountVariants();
            created += GenerateTaxVariants();
            created += GenerateSkimVariants();
            created += GenerateTowerTaxVariants();
            created += GenerateInflationVariants();
            created += GenerateCooldownDragVariants();
            created += GenerateBankRunVariants();
            created += GenerateRegenVariants();
            created += GenerateShieldPhaseVariants();
            created += GenerateNecropolisVariants();
            created += GenerateLootDroughtVariants();
            created += GenerateApocalypseVariants();
            created += GenerateStealGoldVariants();
            created += GenerateForcePickVariants();

            // Rarity-based self-sabotages
            created += GenerateFrugalVariants();
            created += GenerateGlassCannonVariants();
            created += GenerateHalvedRangeVariants();
            created += GenerateInvertedEconomyVariants();
            created += GeneratePacifistVariants();
            created += GenerateSpeedDemonVariants();
            created += GenerateNoMulliganVariants();
            created += GenerateElementLockVariants();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[SabotageGenerator] Done. Created {created} new assets " +
                      $"(skipped existing). Check {EFFECTS_ROOT} and {CARDS_ROOT}.");
        }

        [MenuItem("Tools/Sabotages/Force Regenerate (deletes existing)", false, 100)]
        public static void ForceRegenerate()
        {
            if (!EditorUtility.DisplayDialog("Force Regenerate",
                "This will DELETE everything under:\n" +
                $"  {EFFECTS_ROOT}\n  {CARDS_ROOT}\n\nProceed?", "Yes, delete and regenerate", "Cancel"))
                return;

            if (AssetDatabase.IsValidFolder(EFFECTS_ROOT))
                AssetDatabase.DeleteAsset(EFFECTS_ROOT);
            if (AssetDatabase.IsValidFolder(CARDS_ROOT))
                AssetDatabase.DeleteAsset(CARDS_ROOT);

            AssetDatabase.Refresh();
            GenerateAll();
        }

        // ==========================================
        // ELEMENT-BASED FAMILIES
        // ==========================================

        private static int GenerateChangeElementVariants()
        {
            int n = 0;
            foreach (var element in AllElementsExceptNone)
            {
                var effect = GetOrCreateEffect<ChangeEnemyElementSabotage>(
                    "ChangeEnemyElement", element.ToString(),
                    e => e.newElement = element,
                    ref n);

                CreateCardForEffect("ChangeEnemyElement", element.ToString(), effect,
                    name: $"Element Swap: {element}",
                    desc: $"Next wave's enemies become {element} type.",
                    rarity: CardRarity.Common,
                    tag: SabotageTag.Enemies,
                    durationType: SabotageDurationType.Temporary,
                    durationRounds: 1,
                    dropWeight: 30f,
                    ref n);
            }
            return n;
        }

        private static int GenerateElementBlockVariants()
        {
            int n = 0;
            foreach (var element in AllElementsExceptNone)
            {
                var counter = NaturalCounter.TryGetValue(element, out var c) ? c : ElementType.None;

                var effect = GetOrCreateEffect<ElementBlockSabotage>(
                    "ElementBlock", element.ToString(),
                    e => { e.blockedElement = element; e.overrideEnemyElement = counter != ElementType.None; e.enemyElementOverride = counter; },
                    ref n);

                CreateCardForEffect("ElementBlock", element.ToString(), effect,
                    name: $"Block {element}",
                    desc: $"Cannot build {element} turrets next wave; enemies arrive {counter}.",
                    rarity: CardRarity.Rare,
                    tag: SabotageTag.Turrets,
                    durationType: SabotageDurationType.Temporary,
                    durationRounds: 1,
                    dropWeight: 18f,
                    ref n);
            }
            return n;
        }

        private static int GenerateElementResistVariants()
        {
            int n = 0;
            foreach (var element in AllElementsExceptNone)
            {
                var effect = GetOrCreateEffect<ElementResistSabotage>(
                    "ElementResist", element.ToString(),
                    e => { e.resistedElement = element; e.damageMultiplier = 0.5f; },
                    ref n);

                CreateCardForEffect("ElementResist", element.ToString(), effect,
                    name: $"Resist: {element}",
                    desc: $"Enemies take 50% damage from {element} for one wave.",
                    rarity: CardRarity.Common,
                    tag: SabotageTag.Enemies,
                    durationType: SabotageDurationType.Temporary,
                    durationRounds: 1,
                    dropWeight: 35f,
                    ref n);
            }
            return n;
        }

        // ==========================================
        // RARITY-BASED OPPONENT SABOTAGES
        // ==========================================

        private static int GenerateWaveHPVariants() => MakeRarityFamily<WaveHPSabotage>(
            "WaveHP",
            common:    e => e.hpMultiplier = 1.15f,
            rare:      e => e.hpMultiplier = 1.30f,
            legendary: e => e.hpMultiplier = 1.60f,
            cardName: r => $"Tough Wave ({r})",
            cardDesc: r => "Enemies in next wave have boosted HP.",
            tag: SabotageTag.Enemies,
            durationRounds: 1);

        private static int GenerateWaveSpeedVariants() => MakeRarityFamily<WaveSpeedSabotage>(
            "WaveSpeed",
            common:    e => e.speedMultiplier = 1.10f,
            rare:      e => e.speedMultiplier = 1.25f,
            legendary: e => e.speedMultiplier = 1.40f,
            cardName: r => $"Hurry Wave ({r})",
            cardDesc: r => "Enemies in next wave move faster.",
            tag: SabotageTag.Enemies,
            durationRounds: 1);

        private static int GenerateWaveCountVariants() => MakeRarityFamily<WaveCountSabotage>(
            "WaveCount",
            common:    e => e.countMultiplier = 1.15f,
            rare:      e => e.countMultiplier = 1.35f,
            legendary: e => e.countMultiplier = 1.60f,
            cardName: r => $"Swarm ({r})",
            cardDesc: r => "More enemies in next wave.",
            tag: SabotageTag.Enemies,
            durationRounds: 1);

        private static int GenerateTaxVariants() => MakeRarityFamily<TaxSabotage>(
            "Tax",
            common:    e => { e.goldPerTick = 3;  e.tickInterval = 1f; },
            rare:      e => { e.goldPerTick = 8;  e.tickInterval = 1f; },
            legendary: e => { e.goldPerTick = 15; e.tickInterval = 1f; },
            cardName: r => $"Tax ({r})",
            cardDesc: r => "Drains gold from your opponent over time.",
            tag: SabotageTag.Economy,
            durationType: SabotageDurationType.Temporary,
            durationSeconds: 30f);

        private static int GenerateSkimVariants() => MakeRarityFamily<SkimSabotage>(
            "Skim",
            common:    e => { e.skimPercent = 0.03f; e.tickInterval = 5f; },
            rare:      e => { e.skimPercent = 0.06f; e.tickInterval = 5f; },
            legendary: e => { e.skimPercent = 0.10f; e.tickInterval = 5f; },
            cardName: r => $"Skim ({r})",
            cardDesc: r => "Drains a percentage of opponent's gold every tick.",
            tag: SabotageTag.Economy,
            durationType: SabotageDurationType.Temporary,
            durationSeconds: 30f);

        private static int GenerateTowerTaxVariants() => MakeRarityFamily<TowerTaxSabotage>(
            "TowerTax",
            common:    e => e.costMultiplier = 1.15f,
            rare:      e => e.costMultiplier = 1.30f,
            legendary: e => e.costMultiplier = 1.50f,
            cardName: r => $"Tower Tax ({r})",
            cardDesc: r => "Increases turret build/upgrade cost.",
            tag: SabotageTag.Economy,
            durationRounds: 1);

        private static int GenerateInflationVariants() => MakeRarityFamily<InflationSabotage>(
            "Inflation",
            common:    e => e.costMultiplier = 1.30f,
            rare:      e => e.costMultiplier = 1.50f,
            legendary: e => e.costMultiplier = 1.80f,
            cardName: r => $"Inflation ({r})",
            cardDesc: r => "Heavy cost increase for opponent's turrets.",
            tag: SabotageTag.Economy,
            durationRounds: 1);

        private static int GenerateCooldownDragVariants() => MakeRarityFamily<CooldownDragSabotage>(
            "CooldownDrag",
            common:    e => e.fireRateMultiplier = 0.95f,
            rare:      e => e.fireRateMultiplier = 0.85f,
            legendary: e => e.fireRateMultiplier = 0.70f,
            cardName: r => $"Cooldown Drag ({r})",
            cardDesc: r => "Slows opponent's turret fire rate.",
            tag: SabotageTag.Turrets,
            durationRounds: 1);

        private static int GenerateBankRunVariants() => MakeRarityFamily<BankRunSabotage>(
            "BankRun",
            common:    e => e.drainPercent = 0.25f,
            rare:      e => e.drainPercent = 0.40f,
            legendary: e => e.drainPercent = 0.55f,
            cardName: r => $"Bank Run ({r})",
            cardDesc: r => "Instantly drains a portion of opponent's gold.",
            tag: SabotageTag.Economy,
            durationType: SabotageDurationType.Instant);

        private static int GenerateRegenVariants() => MakeRarityFamily<RegenSabotage>(
            "Regen",
            common:    e => e.percentPerSecond = 0.015f,
            rare:      e => e.percentPerSecond = 0.030f,
            legendary: e => e.percentPerSecond = 0.050f,
            cardName: r => $"Regenerating Wave ({r})",
            cardDesc: r => "Enemies regenerate HP each second.",
            tag: SabotageTag.Enemies,
            durationRounds: 1);

        private static int GenerateShieldPhaseVariants() => MakeRarityFamily<ShieldPhaseSabotage>(
            "ShieldPhase",
            common:    e => e.armorStacks = 1,
            rare:      e => e.armorStacks = 2,
            legendary: e => e.armorStacks = 3,
            cardName: r => $"Shield Phase ({r})",
            cardDesc: r => "Every enemy in next wave has armor (clicks to break).",
            tag: SabotageTag.Enemies,
            durationRounds: 1);

        private static int GenerateNecropolisVariants() => MakeRarityFamily<NecropolisSabotage>(
            "Necropolis",
            common:    e => e.reviveHpPercent = 0.30f,
            rare:      e => e.reviveHpPercent = 0.50f,
            legendary: e => e.reviveHpPercent = 0.75f,
            cardName: r => $"Necropolis ({r})",
            cardDesc: r => "Every enemy revives once with partial HP.",
            tag: SabotageTag.Enemies,
            durationRounds: 1);

        private static int GenerateLootDroughtVariants() => MakeRarityFamily<LootDroughtSabotage>(
            "LootDrought",
            common:    e => e.goldRewardMultiplier = 0.85f,
            rare:      e => e.goldRewardMultiplier = 0.65f,
            legendary: e => e.goldRewardMultiplier = 0.40f,
            cardName: r => $"Loot Drought ({r})",
            cardDesc: r => "Reduces gold per kill in next wave.",
            tag: SabotageTag.Economy,
            durationRounds: 1);

        private static int GenerateApocalypseVariants()
        {
            int n = 0;
            // Apocalypse uses a list of bosses, varying COUNT per rarity.
            // We try to find a default boss prefab to seed the list — designer can edit afterwards.
            GameObject defaultBoss = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/PrzemekSkrypty/Prefabs/Enemy Boss.prefab");

            int[] bossCounts = { 1, 2, 4 };
            CardRarity[] rarities = { CardRarity.Common, CardRarity.Rare, CardRarity.Legendary };

            for (int i = 0; i < rarities.Length; i++)
            {
                var rarity = rarities[i];
                int count = bossCounts[i];
                var effect = GetOrCreateEffect<ApocalypseSabotage>(
                    "Apocalypse", rarity.ToString(),
                    e =>
                    {
                        e.bossPrefabs = new List<GameObject>();
                        if (defaultBoss != null)
                            for (int j = 0; j < count; j++) e.bossPrefabs.Add(defaultBoss);
                    },
                    ref n);

                CreateCardForEffect("Apocalypse", rarity.ToString(), effect,
                    name: $"Apocalypse ({rarity})",
                    desc: $"Spawns {count} boss(es) at the end of next wave.",
                    rarity: rarity,
                    tag: SabotageTag.Enemies,
                    durationType: SabotageDurationType.Instant,
                    durationRounds: 0,
                    dropWeight: DropWeightForRarity(rarity),
                    ref n);
            }
            return n;
        }

        private static int GenerateStealGoldVariants() => MakeRarityFamily<StealGoldSabotage>(
            "StealGold",
            common:    e => { e.stealPercent = 15f; e.minimumSteal = 25;  e.maximumSteal = 200; },
            rare:      e => { e.stealPercent = 25f; e.minimumSteal = 50;  e.maximumSteal = 400; },
            legendary: e => { e.stealPercent = 40f; e.minimumSteal = 100; e.maximumSteal = 750; },
            cardName: r => $"Steal Gold ({r})",
            cardDesc: r => "Steals a percentage of opponent's gold (capped).",
            tag: SabotageTag.Economy,
            durationType: SabotageDurationType.Instant);

        private static int GenerateForcePickVariants() => MakeRarityFamily<ForcePickSabotage>(
            "ForcePick",
            common:    e => e.reducedChoices = 2,
            rare:      e => e.reducedChoices = 1,
            legendary: e => e.reducedChoices = 1,  // legendary same as rare; flag-driven
            cardName: r => $"Force Pick ({r})",
            cardDesc: r => "Reduces opponent's next mid-game draft choice count.",
            tag: SabotageTag.Player,
            durationType: SabotageDurationType.Permanent);

        // ==========================================
        // RARITY-BASED SELF SABOTAGES
        // ==========================================

        private static int GenerateFrugalVariants() => MakeRarityFamily<FrugalSelfSabotage>(
            "Frugal",
            common:    e => { e.passiveGoldMultiplier = 0.7f;  e.killGoldMultiplier = 1.3f; },
            rare:      e => { e.passiveGoldMultiplier = 0.5f;  e.killGoldMultiplier = 1.5f; },
            legendary: e => { e.passiveGoldMultiplier = 0.25f; e.killGoldMultiplier = 1.85f; },
            cardName: r => $"Frugal ({r})",
            cardDesc: r => "Lower passive gold but higher gold per kill.",
            tag: SabotageTag.SelfSabotage,
            durationRounds: 1,
            isSelf: true);

        private static int GenerateGlassCannonVariants() => MakeRarityFamily<GlassCannonSelfSabotage>(
            "GlassCannon",
            common:    e => e.damageMultiplier = 1.5f,
            rare:      e => e.damageMultiplier = 2f,
            legendary: e => e.damageMultiplier = 2.75f,
            cardName: r => $"Glass Cannon ({r})",
            cardDesc: r => "Drops your HP to 1, but turrets deal massively more damage.",
            tag: SabotageTag.SelfSabotage,
            durationRounds: 1,
            isSelf: true,
            rewardGold: r => RewardForRarity(r, 50, 150, 350));

        private static int GenerateHalvedRangeVariants() => MakeRarityFamily<HalvedRangeSelfSabotage>(
            "HalvedRange",
            common:    e => { e.rangeMultiplier = 0.85f; e.fireRateMultiplier = 1.30f; },
            rare:      e => { e.rangeMultiplier = 0.70f; e.fireRateMultiplier = 1.60f; },
            legendary: e => { e.rangeMultiplier = 0.50f; e.fireRateMultiplier = 2.00f; },
            cardName: r => $"Halved Range ({r})",
            cardDesc: r => "Less range, much higher fire rate.",
            tag: SabotageTag.SelfSabotage,
            durationRounds: 1,
            isSelf: true,
            rewardGold: r => RewardForRarity(r, 30, 80, 200));

        private static int GenerateInvertedEconomyVariants() => MakeRarityFamily<InvertedEconomySelfSabotage>(
            "InvertedEconomy",
            common:    e => e.waveEndBonus = 250,
            rare:      e => e.waveEndBonus = 500,
            legendary: e => e.waveEndBonus = 850,
            cardName: r => $"Inverted Economy ({r})",
            cardDesc: r => "0 gold per kill, lump-sum bonus at wave end.",
            tag: SabotageTag.SelfSabotage,
            durationRounds: 1,
            isSelf: true);

        private static int GeneratePacifistVariants() => MakeRarityFamily<PacifistSelfSabotage>(
            "Pacifist",
            common:    e => e.bonusGoldAtWaveEnd = 75,
            rare:      e => e.bonusGoldAtWaveEnd = 175,
            legendary: e => e.bonusGoldAtWaveEnd = 350,
            cardName: r => $"Pacifist ({r})",
            cardDesc: r => "Cannot upgrade turrets this wave; bonus gold at end.",
            tag: SabotageTag.SelfSabotage,
            durationRounds: 1,
            isSelf: true,
            rewardGold: r => RewardForRarity(r, 75, 175, 350));

        private static int GenerateSpeedDemonVariants() => MakeRarityFamily<SpeedDemonSelfSabotage>(
            "SpeedDemon",
            common:    e => { e.enemySpeedMultiplier = 1.20f; e.killGoldMultiplier = 1.5f; },
            rare:      e => { e.enemySpeedMultiplier = 1.35f; e.killGoldMultiplier = 1.8f; },
            legendary: e => { e.enemySpeedMultiplier = 1.55f; e.killGoldMultiplier = 2.2f; },
            cardName: r => $"Speed Demon ({r})",
            cardDesc: r => "Enemies move much faster, but kills give more gold.",
            tag: SabotageTag.SelfSabotage,
            durationRounds: 1,
            isSelf: true);

        private static int GenerateNoMulliganVariants() => MakeRarityFamily<NoMulliganSelfSabotage>(
            "NoMulligan",
            common:    e => e.extraChoices = 1,
            rare:      e => e.extraChoices = 2,
            legendary: e => e.extraChoices = 3,
            cardName: r => $"No Mulligan ({r})",
            cardDesc: r => "No mulligan in next draft; extra cards offered as compensation.",
            tag: SabotageTag.SelfSabotage,
            durationType: SabotageDurationType.Permanent,
            isSelf: true);

        private static int GenerateElementLockVariants() => MakeRarityFamily<ElementLockSelfSabotage>(
            "ElementLock",
            common:    e => e.bonusGoldAtWaveEnd = 100,
            rare:      e => e.bonusGoldAtWaveEnd = 225,
            legendary: e => e.bonusGoldAtWaveEnd = 400,
            cardName: r => $"Element Lock ({r})",
            cardDesc: r => "Cannot build any turret this wave; bonus gold at end.",
            tag: SabotageTag.SelfSabotage,
            durationRounds: 1,
            isSelf: true,
            rewardGold: r => RewardForRarity(r, 100, 225, 400));

        // ==========================================
        // GENERIC HELPERS
        // ==========================================

        // Creates a 3-rarity family (Common/Rare/Legendary) of effect+card pairs.
        private static int MakeRarityFamily<TEffect>(
            string family,
            System.Action<TEffect> common,
            System.Action<TEffect> rare,
            System.Action<TEffect> legendary,
            System.Func<CardRarity, string> cardName,
            System.Func<CardRarity, string> cardDesc,
            SabotageTag tag,
            int durationRounds = 0,
            SabotageDurationType durationType = SabotageDurationType.Temporary,
            float durationSeconds = 0f,
            bool isSelf = false,
            System.Func<CardRarity, int> rewardGold = null)
            where TEffect : SabotageEffectBase
        {
            int n = 0;
            var pairs = new (CardRarity rarity, System.Action<TEffect> apply)[]
            {
                (CardRarity.Common, common),
                (CardRarity.Rare, rare),
                (CardRarity.Legendary, legendary),
            };

            foreach (var (rarity, apply) in pairs)
            {
                var effect = GetOrCreateEffect<TEffect>(family, rarity.ToString(), apply, ref n);
                CreateCardForEffect(
                    family: family,
                    variant: rarity.ToString(),
                    effect: effect,
                    name: cardName(rarity),
                    desc: cardDesc(rarity),
                    rarity: rarity,
                    tag: tag,
                    durationType: durationType,
                    durationRounds: durationRounds,
                    durationSeconds: durationSeconds,
                    dropWeight: DropWeightForRarity(rarity),
                    isSelf: isSelf,
                    rewardGold: rewardGold != null ? rewardGold(rarity) : 0,
                    counter: ref n);
            }
            return n;
        }

        private static TEffect GetOrCreateEffect<TEffect>(
            string family, string variant, System.Action<TEffect> configure, ref int counter)
            where TEffect : SabotageEffectBase
        {
            string folder = $"{EFFECTS_ROOT}/{family}";
            EnsureFolder(folder);
            string path = $"{folder}/{family}_{variant}_Effect.asset";

            var existing = AssetDatabase.LoadAssetAtPath<TEffect>(path);
            if (existing != null) return existing;

            var so = ScriptableObject.CreateInstance<TEffect>();
            configure(so);
            AssetDatabase.CreateAsset(so, path);
            counter++;
            return so;
        }

        private static void CreateCardForEffect(
            string family, string variant, SabotageEffectBase effect,
            string name, string desc,
            CardRarity rarity, SabotageTag tag,
            SabotageDurationType durationType,
            int durationRounds,
            float durationSeconds,
            float dropWeight,
            bool isSelf,
            int rewardGold,
            ref int counter)
        {
            string folder = $"{CARDS_ROOT}/{family}";
            EnsureFolder(folder);
            string path = $"{folder}/{family}_{variant}_Card.asset";

            var existing = AssetDatabase.LoadAssetAtPath<SabotageCardData>(path);
            if (existing != null) return;

            var card = ScriptableObject.CreateInstance<SabotageCardData>();
            card.sabotageName = name;
            card.description = desc;
            card.rarity = rarity;
            card.sabotageTag = tag;
            card.durationType = durationType;
            card.durationRounds = durationRounds;
            card.duration = durationSeconds;
            card.dropWeight = dropWeight;
            card.sabotageEffect = effect;
            card.targetType = isSelf ? SabotageTarget.Self : SabotageTarget.Opponent;
            card.rewardGold = rewardGold;

            AssetDatabase.CreateAsset(card, path);
            counter++;
        }

        // Convenience overload — older call sites that don't pass rewardGold/isSelf.
        private static void CreateCardForEffect(
            string family, string variant, SabotageEffectBase effect,
            string name, string desc,
            CardRarity rarity, SabotageTag tag,
            SabotageDurationType durationType,
            int durationRounds,
            float dropWeight,
            ref int counter)
            => CreateCardForEffect(family, variant, effect, name, desc, rarity, tag,
                                   durationType, durationRounds, 0f, dropWeight, false, 0, ref counter);

        private static float DropWeightForRarity(CardRarity rarity) => rarity switch
        {
            CardRarity.Common => 50f,
            CardRarity.Rare => 25f,
            CardRarity.Legendary => 8f,
            _ => 30f
        };

        private static int RewardForRarity(CardRarity r, int common, int rare, int legendary) => r switch
        {
            CardRarity.Common => common,
            CardRarity.Rare => rare,
            CardRarity.Legendary => legendary,
            _ => common
        };

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
