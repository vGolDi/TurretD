using UnityEngine;

namespace ElementumDefense.StatusEffects
{
    /// <summary>
    /// Reduces enemy movement speed
    /// Associated with Water/Ice elements
    /// </summary>
    public class SlowEffect : StatusEffect
    {
        public override StatusEffectType EffectType => StatusEffectType.Slow;
        public override string DisplayName => "Slowed";
        public override string Icon => "SLOW";

        public override bool RefreshOnReapply => true;

        // Slow-specific property (public for StatusEffectManager access)
        public float SlowMultiplier { get; private set; }

        /// <summary>
        /// Creates slow effect
        /// </summary>
        /// <param name="slowAmount">Speed multiplier (0.5 = 50% speed)</param>
        /// <param name="duration">Duration in seconds</param>
        public SlowEffect(float slowAmount, float duration)
        {
            SlowMultiplier = Mathf.Clamp01(slowAmount);
            MaxDuration = duration;
        }

        protected override void OnApplied()
        {
            base.OnApplied();
            Debug.Log($"[SlowEffect]  Enemy slowed to {SlowMultiplier * 100}% speed");
        }
    }
}