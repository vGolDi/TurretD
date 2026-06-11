using UnityEngine;
using ElementumDefense.Turrets;

namespace ElementumDefense.StatusEffects
{
    /// <summary>
    /// Light element armor reduction effect.
    /// Applied by Light Turret aura to nearby enemies.
    /// Reduces effective armor — increases all damage received.
    /// Does NOT stack, only refreshes.
    /// </summary>
    public class ExposeEffect : StatusEffect
    {
        public override StatusEffectType EffectType => StatusEffectType.Expose;
        public override string DisplayName => "Expose";
        public override int MaxStacks => 1;
        public override bool IsStackable => false;
        public override bool RefreshOnReapply => true;

        /// <summary>Armor reduction fraction (0.3 = -30% armor)</summary>
        public float ArmorReduction { get; private set; }

        public ExposeEffect(float armorReduction = 0.30f)
        {
            ArmorReduction = armorReduction;
        }

        protected override void OnApplied()
        {
            Debug.Log($"[ExposeEffect] Applied to {target?.name} — -{(ArmorReduction * 100f):F0}% armor");
        }
    }
}
