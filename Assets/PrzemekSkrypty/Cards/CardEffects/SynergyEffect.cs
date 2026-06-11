using UnityEngine;
using Photon.Pun;
using ElementumDefense.Elements;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Card whose strength scales with the rest of the deck/active cards.
    /// 
    /// Counted in <see cref="PlayerModifierStack.RecalculateFromCards"/> and
    /// applied as additional damageMultiplier / passiveGoldPerSecond per stack.
    /// </summary>
    [CreateAssetMenu(fileName = "SynergyCard_Effect", menuName = "Tower Defense/Cards/Effects/Synergy (Deck-scaling)")]
    public class SynergyEffect : CardEffectBase
    {
        public enum ScaleSource
        {
            ElementCardsInDeck,    // count of activeCards with associatedElement == targetElement
            EconomyCardsInDeck,    // count of activeCards with cardType == Economy
            UniqueElementsInDeck   // count of unique associatedElement values present
        }

        [Header("Scaling")]
        public ScaleSource scaleBy = ScaleSource.ElementCardsInDeck;

        [Tooltip("Used only for ElementCardsInDeck.")]
        public ElementType targetElement = ElementType.Fire;

        [Tooltip("Cap on stack count (prevents runaway).")]
        public int maxStacks = 6;

        [Header("Bonus per stack")]
        [Tooltip("Bonus damage % per stack. 5 = +5% per matching card. e.g. 6 stacks = +30%.")]
        [Range(0f, 50f)]
        public float bonusDamagePercentPerStack = 5f;

        [Tooltip("Flat passive gold per second per stack (for Wealthy etc.).")]
        public int bonusGoldPerSecondPerStack = 0;

        public override void Activate(PhotonView ownerPhotonView)
        {
            LogActivation(ownerPhotonView,
                $"{scaleBy} +{bonusDamagePercentPerStack}%/stack (cap {maxStacks})");
        }

        public override string GetEffectDescription()
        {
            string source = scaleBy switch
            {
                ScaleSource.ElementCardsInDeck => $"per {targetElement} card in deck",
                ScaleSource.EconomyCardsInDeck => "per Economy card in deck",
                ScaleSource.UniqueElementsInDeck => "per unique element in deck",
                _ => ""
            };

            string desc = "";
            if (bonusDamagePercentPerStack > 0f)
                desc += $"⚔️ +{bonusDamagePercentPerStack:0}% damage {source}\n";
            if (bonusGoldPerSecondPerStack > 0)
                desc += $"💰 +{bonusGoldPerSecondPerStack} gold/s {source}\n";
            desc += $"<i>(max {maxStacks} stacks)</i>";
            return desc;
        }
    }
}
