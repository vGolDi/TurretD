using UnityEngine;
using ElementumDefense.Turrets;

namespace ElementumDefense.StatusEffects
{
    /// <summary>
    /// Centralized factory for runtime <see cref="StatusEffect"/> instances.
    /// 
    /// Use this from anywhere that needs to spawn a status effect from a
    /// <see cref="StatusEffectType"/> + numeric parameters (turret hits,
    /// projectile hits, aura ticks, sabotage cards, debug menus).
    /// 
    /// Adding a new effect: add one branch here and the rest of the codebase
    /// gets it for free.
    /// </summary>
    public static class StatusEffectFactory
    {
        /// <summary>
        /// Creates a status effect by type. Returns null for unsupported types.
        /// </summary>
        /// <param name="type">Which effect to create.</param>
        /// <param name="strength">
        /// Type-dependent magnitude:
        ///   Burn / Poison -> damage per second.
        ///   Slow -> remaining speed multiplier (0.5 = 50% speed).
        ///   Curse -> bonus damage taken (0.35 = +35%).
        ///   Expose -> armor reduction fraction (0.30 = -30% armor).
        ///   Freeze / Chill -> ignored.
        /// </param>
        /// <param name="duration">Duration in seconds. Ignored by Chill/Curse/Expose
        /// (they fall back to StatusEffectManager's default of 3s).</param>
        public static StatusEffect Create(StatusEffectType type, float strength, float duration)
        {
            switch (type)
            {
                case StatusEffectType.Burn:
                    return new BurnEffect(strength, duration);
                case StatusEffectType.Poison:
                    return new PoisonEffect(strength, duration);
                case StatusEffectType.Slow:
                    return new SlowEffect(strength, duration);
                case StatusEffectType.Freeze:
                    return new FreezeEffect(duration);

                // Effects that ignore numeric duration (use manager default).
                case StatusEffectType.Chill:
                    return new ChillEffect();
                case StatusEffectType.Curse:
                    return new CurseEffect(strength);
                case StatusEffectType.Expose:
                    return new ExposeEffect(strength);

                default:
                    Debug.LogWarning($"[StatusEffectFactory] Unsupported effect type: {type}");
                    return null;
            }
        }
    }
}
