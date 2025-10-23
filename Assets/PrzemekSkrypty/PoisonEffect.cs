using UnityEngine;

namespace ElementumDefense.StatusEffects
{
    /// <summary>
    /// Nature DOT - similar to burn but stacks higher
    /// Associated with Nature element
    /// </summary>
    public class PoisonEffect : StatusEffect
    {
        public override StatusEffectType EffectType => StatusEffectType.Poison;
        public override string DisplayName => "Poisoned";
        public override string Icon => "🌿";

        public override int MaxStacks => 5; // Stacks higher than burn
        public override bool IsStackable => true;
        public override bool RefreshOnReapply => true;

        private float damagePerSecond;
        private float tickInterval = 1f; // Slower ticks than burn
        private float tickTimer = 0f;

        public PoisonEffect(float dps, float duration)
        {
            damagePerSecond = dps;
            MaxDuration = duration;
        }

        protected override void OnApplied()
        {
            base.OnApplied();
            Debug.Log($"[PoisonEffect]  Poisoned for {damagePerSecond} DPS");
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            tickTimer += deltaTime;

            if (tickTimer >= tickInterval)
            {
                ApplyTickDamage();
                tickTimer = 0f;
            }
        }

        private void ApplyTickDamage()
        {
            if (target == null) return;

            // Poison scales exponentially with stacks (more dangerous!)
            float stackMultiplier = 1f + (StackCount - 1) * 0.5f;
            float tickDamage = (damagePerSecond * tickInterval) * stackMultiplier;

            target.TakeDamage(
                Mathf.RoundToInt(tickDamage),
                -1,
                Elements.ElementType.Nature
            );

            Debug.Log($"[PoisonEffect]  Tick: {tickDamage} (stacks: {StackCount}, mult: {stackMultiplier}x)");
        }
    }
}