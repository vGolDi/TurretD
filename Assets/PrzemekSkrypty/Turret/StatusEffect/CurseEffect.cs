using UnityEngine;

namespace ElementumDefense.StatusEffects
{
    /// <summary>
    /// Dark element status effect.
    /// Increases ALL damage the target takes by a flat percentage.
    /// Does NOT stack — only refreshes duration.
    /// </summary>
    public class CurseEffect : StatusEffect
    {
        public override StatusEffectType EffectType => StatusEffectType.Curse;
        public override string DisplayName => "Curse";
        public override int MaxStacks => 1;
        public override bool IsStackable => false;
        public override bool RefreshOnReapply => true;

        /// <summary>Damage multiplier applied to all incoming damage (1.35 = +35%)</summary>
        public float DamageMultiplier { get; private set; }

        private float multiplier;

        public CurseEffect(float damageBonus = 0.35f)
        {
            multiplier = damageBonus;
            DamageMultiplier = 1f + damageBonus;
        }

        protected override void OnApplied()
        {
            Debug.Log($"[CurseEffect] Applied to {target?.name} — +{(multiplier * 100f):F0}% dmg taken");
        }
    }
}
