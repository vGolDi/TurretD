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
        // ELEMENT FOCUS — Common (×6, +15% dmg per element)
        // ==========================================

        private static int ElementFocus_Commons()
        {
            int n = 0;
            foreach (var element in AllElements)
            {
                var fx = GetOrCreateEffect<TurretCardEffect>(
                    "ElementFocus", element.ToString(),
                    e => { e.targetElement = element; e.affectsAllTurrets = false; e.damageMultiplier = 1.15f; },
                    ref n);

                CreateCard("ElementFocus", element.ToString(), fx,
                    name: $"{element} Focus",
                    desc: $"{element} turrets gain +15% damage.",
                    rarity: CardRarity.Common,
                    type: CardType.Turret,
                    activation: CardActivationType.Continuous,
                    associatedElement: element,
                    ref n,
                    unlockCost: UnlockCostForRarity(CardRarity.Common));
            }
            return n;
        }

        // ==========================================
        // ELEMENT MASTERY — Rare (×6, +20% dmg + extra range)
        // ==========================================

        private static int ElementMastery_Rares()
        {
            int n = 0;
            foreach (var element in AllElements)
            {
                var fx = GetOrCreateEffect<TurretCardEffect>(
                    "ElementMastery", element.ToString(),
                    e =>
                    {
                        e.targetElement = element;
                        e.affectsAllTurrets = false;
                        e.damageMultiplier = 1.20f;
                        e.rangeMultiplier = 1.10f;
                    },
                    ref n);

                CreateCard("ElementMastery", element.ToString(), fx,
                    name: $"{element} Mastery",
                    desc: $"{element} turrets: +20% damage, +10% range.",
                    rarity: CardRarity.Rare,
                    type: CardType.Turret,
                    activation: CardActivationType.Continuous,
                    associatedElement: element,
                    ref n,
                    unlockCost: UnlockCostForRarity(CardRarity.Rare));
            }
            return n;
        }

        // ==========================================
        // ELEMENT AVATAR — Legendary (×6, +50% to chosen element, -25% to others)
        // ==========================================

        private static int ElementAvatar_Legendaries()
        {
            int n = 0;
            foreach (var element in AllElements)
            {
                var fx = GetOrCreateEffect<TurretCardEffect>(
                    "ElementAvatar", element.ToString(),
                    e =>
                    {
                        e.targetElement = element;
                        e.affectsAllTurrets = false;
                        e.damageMultiplier = 1.50f;
                        e.fireRateMultiplier = 1.50f;
                        e.rangeMultiplier = 1.50f;
                        e.otherElementsPenaltyPercent = 25f;
                    },
                    ref n);

                CreateCard("ElementAvatar", element.ToString(), fx,
                    name: $"Avatar of {element}",
                    desc: $"{element} turrets: +50% all stats. ALL OTHER elements: -25% damage.",
                    rarity: CardRarity.Legendary,
                    type: CardType.Turret,
                    activation: CardActivationType.Continuous,
                    associatedElement: element,
                    ref n,
                    requiredLevel: 7,
                    unlockCost: UnlockCostForRarity(CardRarity.Legendary));
            }
            return n;
        }

        // ==========================================
        // ELEMENT SIGNATURE — Legendary (×6, unique mechanic per element)
        // ==========================================

        private static int ElementSignature_Legendaries()
        {
            int n = 0;

            n += MakeSignature("Pyromancer", ElementType.Fire,
                "Pyromancer's Pact", "Fire turrets: +30% damage, +1.5m AOE.",
                e => { e.targetElement = ElementType.Fire; e.damageMultiplier = 1.30f; e.addAOERadius = 1.5f; });

            n += MakeSignature("Frostbite", ElementType.Ice,
                "Frostbite", "Ice turrets: +40% fire rate, +10% range.",
                e => { e.targetElement = ElementType.Ice; e.fireRateMultiplier = 1.40f; e.rangeMultiplier = 1.10f; });

            n += MakeSignature("StormCaller", ElementType.Lightning,
                "Storm Caller", "Lightning turrets: +25% damage, +2 chain targets.",
                e => { e.targetElement = ElementType.Lightning; e.damageMultiplier = 1.25f; e.addChainTargets = 2; });

            n += MakeSignature("WildGrowth", ElementType.Nature,
                "Wild Growth", "Nature turrets: +30% damage, +2 pierce.",
                e => { e.targetElement = ElementType.Nature; e.damageMultiplier = 1.30f; e.addPierceCount = 2; });

            n += MakeSignature("ShadowMastery", ElementType.Dark,
                "Shadow Mastery", "Dark turrets: +30% damage, +50% range.",
                e => { e.targetElement = ElementType.Dark; e.damageMultiplier = 1.30f; e.rangeMultiplier = 1.50f; });

            n += MakeSignature("RadiantBlessing", ElementType.Light,
                "Radiant Blessing", "Light turrets: +25% damage.",
                e => { e.targetElement = ElementType.Light; e.damageMultiplier = 1.25f; });

            return n;
        }

        private static int MakeSignature(string variant, ElementType element,
            string cardName, string desc, System.Action<TurretCardEffect> configure)
        {
            int n = 0;
            var fx = GetOrCreateEffect<TurretCardEffect>("ElementSignature", variant, configure, ref n);
            CreateCard("ElementSignature", variant, fx,
                cardName, desc,
                CardRarity.Legendary, CardType.Turret, CardActivationType.Continuous,
                element, ref n,
                requiredLevel: 6,
                unlockCost: UnlockCostForRarity(CardRarity.Legendary));
            return n;
        }
    }
}
#endif
