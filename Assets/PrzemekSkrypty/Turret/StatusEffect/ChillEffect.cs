using UnityEngine;

namespace ElementumDefense.StatusEffects
{
    /// <summary>
    /// Ice element status effect.
    /// Stacks up to 2 — at 2 stacks automatically triggers Freeze.
    /// </summary>
    public class ChillEffect : StatusEffect
    {
        public override StatusEffectType EffectType => StatusEffectType.Chill;
        public override string DisplayName => "Chill";
        public override int MaxStacks => 2;
        public override bool IsStackable => true;

        private const float SLOW_PER_STACK = 0.20f; // 20% slow per stack (40% at max)

        /// <summary>Speed multiplier to apply (0.8 = 20% slow per stack)</summary>
        public float SlowMultiplier => 1f - (SLOW_PER_STACK * StackCount);

        /// <summary>True when stacked to max — caller should convert to Freeze</summary>
        public bool ShouldFreeze => StackCount >= MaxStacks;

        protected override void OnApplied()
        {
            Debug.Log($"[ChillEffect] Applied to {target?.name} — stack {StackCount}/{MaxStacks}");
        }

        public override void OnStackAdded()
        {
            base.OnStackAdded();
            Debug.Log($"[ChillEffect] Stack {StackCount}/{MaxStacks} — ShouldFreeze={ShouldFreeze}");
        }
    }
}
