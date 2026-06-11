#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ElementumDefense.Cards;
using ElementumDefense.Elements;

namespace ElementumDefense.EditorTools
{
    /// <summary>
    /// One-stop generator for player cards (Common / Rare / Legendary).
    /// 
    /// Run via: Tools → Cards → Generate All
    /// 
    /// Idempotent — running twice will skip files that already exist.
    /// Use "Force Regenerate" to overwrite (deletes the Generated/ folders).
    /// 
    /// Output:
    ///   Resources/CardEffects/Generated/{Family}/{Variant}_Effect.asset
    ///   Resources/Cards/Generated/{Family}/{Variant}_Card.asset
    /// 
    /// Card design constraints:
    ///  - No max HP / heal cards (PvP balance — locked HP)
    ///  - Cards above Common have a tradeoff (downside)
    ///  - Phoenix Heart is the only "HP-touching" legendary
    /// </summary>
    public static partial class CardAssetGenerator
    {
        private const string EFFECTS_ROOT = "Assets/Resources/CardEffects/Generated";
        private const string CARDS_ROOT = "Assets/Resources/Cards/Generated";

        private static readonly ElementType[] AllElements = new[]
        {
            ElementType.Fire, ElementType.Ice, ElementType.Lightning,
            ElementType.Nature, ElementType.Dark, ElementType.Light
        };

        // ==========================================
        // ENTRY POINTS
        // ==========================================

        [MenuItem("Tools/Cards/Generate All", false, 1)]
        public static void GenerateAll()
        {
            EnsureFolder(EFFECTS_ROOT);
            EnsureFolder(CARDS_ROOT);

            int created = 0;

            // Turret — global
            created += GlobalTurret_Commons();
            created += GlobalTurret_Rares();
            created += GlobalTurret_Legendaries();

            // Turret — per-element
            created += ElementFocus_Commons();
            created += ElementMastery_Rares();
            created += ElementAvatar_Legendaries();
            created += ElementSignature_Legendaries();

            // Economy
            created += Economy_Commons();
            created += Economy_Rares();
            created += Economy_Legendaries();

            // Utility (speed-only + Phoenix legendary)
            created += Utility_Commons();
            created += Utility_Rares();
            created += Utility_Legendaries();

            // Combat (crit + gold/kill)
            created += Combat_Commons();
            created += Combat_Rares();
            created += Combat_Legendaries();

            // Conditional
            created += Conditional_Commons();
            created += Conditional_Rares();
            created += Conditional_Legendaries();

            // Wave trigger
            created += WaveTrigger_Commons();
            created += WaveTrigger_Rares();
            created += WaveTrigger_Legendaries();

            // Synergy
            created += Synergy_Rares();
            created += Synergy_Legendaries();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CardGenerator] Done. Created {created} new assets " +
                      $"(skipped existing). Check {EFFECTS_ROOT} and {CARDS_ROOT}.");
        }

        [MenuItem("Tools/Cards/Force Regenerate (deletes existing)", false, 100)]
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
        // SHARED HELPERS
        // ==========================================

        private static TEffect GetOrCreateEffect<TEffect>(
            string family, string variant, Action<TEffect> configure, ref int counter)
            where TEffect : CardEffectBase
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

        private static void CreateCard(
            string family, string variant, CardEffectBase effect,
            string name, string desc,
            CardRarity rarity, CardType type,
            CardActivationType activation,
            ElementType associatedElement,
            ref int counter,
            int requiredLevel = 1, int unlockCost = 100, bool starter = false)
        {
            string folder = $"{CARDS_ROOT}/{family}";
            EnsureFolder(folder);
            string path = $"{folder}/{family}_{variant}_Card.asset";

            if (AssetDatabase.LoadAssetAtPath<CardData>(path) != null) return;

            var card = ScriptableObject.CreateInstance<CardData>();
            card.cardName = name;
            card.description = desc;
            card.rarity = rarity;
            card.cardType = type;
            card.activationType = activation;
            card.associatedElement = associatedElement;
            card.cardEffect = effect;
            card.cardColor = card.GetRarityColor();
            card.requiredLevel = requiredLevel;
            card.unlockCost = unlockCost;
            card.isStarterCard = starter;
            card.canDropFromLootbox = true;

            AssetDatabase.CreateAsset(card, path);
            counter++;
        }

        private static int UnlockCostForRarity(CardRarity r) => r switch
        {
            CardRarity.Common => 100,
            CardRarity.Rare => 400,
            CardRarity.Legendary => 1500,
            _ => 100
        };

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        // ==========================================
        // TURRET — GLOBAL (no element)
        // ==========================================

        private static int GlobalTurret_Commons()
        {
            int n = 0;
            var sharp = GetOrCreateEffect<TurretCardEffect>("GlobalTurret", "SharpenedBolts",
                e => { e.affectsAllTurrets = true; e.damageMultiplier = 1.10f; }, ref n);
            CreateCard("GlobalTurret", "SharpenedBolts", sharp,
                "Sharpened Bolts", "All turrets gain +10% damage.",
                CardRarity.Common, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            var draw = GetOrCreateEffect<TurretCardEffect>("GlobalTurret", "QuickDraw",
                e => { e.affectsAllTurrets = true; e.fireRateMultiplier = 1.10f; }, ref n);
            CreateCard("GlobalTurret", "QuickDraw", draw,
                "Quick Draw", "All turrets fire 10% faster.",
                CardRarity.Common, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            var scope = GetOrCreateEffect<TurretCardEffect>("GlobalTurret", "Scope",
                e => { e.affectsAllTurrets = true; e.rangeMultiplier = 1.10f; }, ref n);
            CreateCard("GlobalTurret", "Scope", scope,
                "Scope", "All turrets gain +10% range.",
                CardRarity.Common, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            // Combo-stat commons — two small buffs for one slot. Reward players
            // who want light hybrid setups instead of three single-stat picks.
            var steady = GetOrCreateEffect<TurretCardEffect>("GlobalTurret", "SteadyAim",
                e => { e.affectsAllTurrets = true; e.damageMultiplier = 1.05f; e.rangeMultiplier = 1.05f; }, ref n);
            CreateCard("GlobalTurret", "SteadyAim", steady,
                "Steady Aim", "All turrets: +5% damage, +5% range.",
                CardRarity.Common, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            var hairTrigger = GetOrCreateEffect<TurretCardEffect>("GlobalTurret", "HairTrigger",
                e => { e.affectsAllTurrets = true; e.damageMultiplier = 1.05f; e.fireRateMultiplier = 1.05f; }, ref n);
            CreateCard("GlobalTurret", "HairTrigger", hairTrigger,
                "Hair Trigger", "All turrets: +5% damage, +5% fire rate.",
                CardRarity.Common, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            var opticMount = GetOrCreateEffect<TurretCardEffect>("GlobalTurret", "OpticMount",
                e => { e.affectsAllTurrets = true; e.fireRateMultiplier = 1.05f; e.rangeMultiplier = 1.05f; }, ref n);
            CreateCard("GlobalTurret", "OpticMount", opticMount,
                "Optic Mount", "All turrets: +5% fire rate, +5% range.",
                CardRarity.Common, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Common));

            return n;
        }

        private static int GlobalTurret_Rares()
        {
            int n = 0;
            // Heavy Rounds — +25% dmg, -10% fire rate
            var heavy = GetOrCreateEffect<TurretCardEffect>("GlobalTurret", "HeavyRounds",
                e => { e.affectsAllTurrets = true; e.damageMultiplier = 1.25f; e.fireRateMultiplier = 0.90f; }, ref n);
            CreateCard("GlobalTurret", "HeavyRounds", heavy,
                "Heavy Rounds", "All turrets: +25% damage, -10% fire rate.",
                CardRarity.Rare, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));

            var rapid = GetOrCreateEffect<TurretCardEffect>("GlobalTurret", "RapidFire",
                e => { e.affectsAllTurrets = true; e.fireRateMultiplier = 1.30f; e.damageMultiplier = 0.90f; }, ref n);
            CreateCard("GlobalTurret", "RapidFire", rapid,
                "Rapid Fire", "All turrets: +30% fire rate, -10% damage.",
                CardRarity.Rare, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));

            var longBarrel = GetOrCreateEffect<TurretCardEffect>("GlobalTurret", "LongBarrel",
                e => { e.affectsAllTurrets = true; e.rangeMultiplier = 1.25f; e.fireRateMultiplier = 0.90f; }, ref n);
            CreateCard("GlobalTurret", "LongBarrel", longBarrel,
                "Long Barrel", "All turrets: +25% range, -10% fire rate.",
                CardRarity.Rare, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));

            var splash = GetOrCreateEffect<TurretCardEffect>("GlobalTurret", "SplashRounds",
                e => { e.affectsAllTurrets = true; e.addAOERadius = 1.5f; e.damageMultiplier = 0.90f; }, ref n);
            CreateCard("GlobalTurret", "SplashRounds", splash,
                "Splash Rounds", "All turrets: +1.5m AOE, -10% damage.",
                CardRarity.Rare, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, unlockCost: UnlockCostForRarity(CardRarity.Rare));

            return n;
        }

        private static int GlobalTurret_Legendaries()
        {
            int n = 0;
            // Overdrive — +40% dmg, +40% fire rate, -25% range
            var over = GetOrCreateEffect<TurretCardEffect>("GlobalTurret", "Overdrive",
                e => { e.affectsAllTurrets = true; e.damageMultiplier = 1.40f; e.fireRateMultiplier = 1.40f; e.rangeMultiplier = 0.75f; }, ref n);
            CreateCard("GlobalTurret", "Overdrive", over,
                "Overdrive", "All turrets: +40% damage, +40% fire rate, -25% range.",
                CardRarity.Legendary, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, requiredLevel: 5, unlockCost: UnlockCostForRarity(CardRarity.Legendary));

            // Artillery Doctrine — +3m AOE, +20% dmg, -20% fire rate
            var arty = GetOrCreateEffect<TurretCardEffect>("GlobalTurret", "ArtilleryDoctrine",
                e => { e.affectsAllTurrets = true; e.addAOERadius = 3f; e.damageMultiplier = 1.20f; e.fireRateMultiplier = 0.80f; }, ref n);
            CreateCard("GlobalTurret", "ArtilleryDoctrine", arty,
                "Artillery Doctrine", "All turrets: +3m AOE, +20% damage, -20% fire rate.",
                CardRarity.Legendary, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, requiredLevel: 5, unlockCost: UnlockCostForRarity(CardRarity.Legendary));

            // Chain Reaction — +2 chain, -15% dmg
            var chain = GetOrCreateEffect<TurretCardEffect>("GlobalTurret", "ChainReaction",
                e => { e.affectsAllTurrets = true; e.addChainTargets = 2; e.damageMultiplier = 0.85f; }, ref n);
            CreateCard("GlobalTurret", "ChainReaction", chain,
                "Chain Reaction", "All turrets: +2 chain targets, -15% damage.",
                CardRarity.Legendary, CardType.Turret, CardActivationType.Continuous,
                ElementType.None, ref n, requiredLevel: 5, unlockCost: UnlockCostForRarity(CardRarity.Legendary));

            return n;
        }
    }
}
#endif

