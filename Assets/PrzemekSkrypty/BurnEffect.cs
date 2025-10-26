using UnityEngine;

namespace ElementumDefense.StatusEffects
{
    /// <summary>
    /// Fire DOT (Damage Over Time) effect
    /// Deals periodic damage until expired
    /// Associated with Fire element
    /// </summary>
    public class BurnEffect : StatusEffect
    {
        // ==========================================
        // CONFIGURATION
        // ==========================================

        public override StatusEffectType EffectType => StatusEffectType.Burn;
        public override string DisplayName => "Burning";
        //public override string Icon => "FIREICON";

        public override int MaxStacks => 3; // Can stack up to 3 times
        public override bool IsStackable => true;
        public override bool RefreshOnReapply => true;

        // ==========================================
        // BURN-SPECIFIC PROPERTIES
        // ==========================================

        private float damagePerSecond;
        private float tickInterval = 0.5f; // Deal damage every 0.5s
        private float tickTimer = 0f;

        private GameObject burnVFX; // Visual effect instance

        // ==========================================
        // CONSTRUCTOR
        // ==========================================

        /// <summary>
        /// Creates new Burn effect
        /// </summary>
        /// <param name="dps">Damage per second</param>
        /// <param name="duration">Total duration in seconds</param>
        public BurnEffect(float dps, float duration)
        {
            damagePerSecond = dps;
            MaxDuration = duration;
        }

        // ==========================================
        // LIFECYCLE OVERRIDES
        // ==========================================

        protected override void OnApplied()
        {
            base.OnApplied();

            // Spawn fire VFX
            // TODO: Replace with actual particle prefab
            // burnVFX = Object.Instantiate(Resources.Load<GameObject>("VFX/BurnEffect"), targetGameObject.transform);

            Debug.Log($"[BurnEffect] Started burning for {damagePerSecond * StackCount} DPS");
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            // Tick damage
            tickTimer += deltaTime;

            if (tickTimer >= tickInterval)
            {
                ApplyTickDamage();
                tickTimer = 0f;
            }
        }

        public override void OnStackAdded()
        {
            base.OnStackAdded();

            // Scale VFX intensity (optional)
            // if (burnVFX != null)
            // {
            //     var particles = burnVFX.GetComponent<ParticleSystem>();
            //     particles.emission.rateOverTime = 10 * StackCount;
            // }
        }

        public override void OnExpired()
        {
            base.OnExpired();
            CleanupVFX();
        }

        public override void OnRemoved()
        {
            base.OnRemoved();
            CleanupVFX();
        }

        // ==========================================
        // BURN-SPECIFIC LOGIC
        // ==========================================

        /// <summary>
        /// Applies damage tick
        /// </summary>
        private void ApplyTickDamage()
        {
            if (target == null) return;

            // Calculate damage (scales with stacks)
            float tickDamage = (damagePerSecond * tickInterval) * StackCount;

            target.TakeDamage(
                Mathf.RoundToInt(tickDamage),
                -1, // No specific attacker (DOT)
                Elements.ElementType.Fire
            );

            Debug.Log($"[BurnEffect]  Tick damage: {tickDamage} (stacks: {StackCount})");
        }

        /// <summary>
        /// Destroys VFX when effect ends
        /// </summary>
        private void CleanupVFX()
        {
            if (burnVFX != null)
            {
                Object.Destroy(burnVFX);
            }
        }
    }
}