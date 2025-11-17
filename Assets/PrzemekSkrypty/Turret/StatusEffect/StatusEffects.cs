using UnityEngine;

namespace ElementumDefense.StatusEffects
{
    /// <summary>
    /// Base class for all status effects (Burn, Freeze, Slow, etc.)
    /// Handles duration, stacking, and visual feedback
    /// </summary>
    public abstract class StatusEffect
    {
        // ==========================================
        // PROPERTIES
        // ==========================================

        /// <summary>
        /// Unique identifier for this effect type
        /// Used to prevent stacking incompatible effects
        /// </summary>
        public abstract StatusEffectType EffectType { get; }

        /// <summary>
        /// Display name for UI
        /// </summary>
        public abstract string DisplayName { get; }

        /// <summary>
        /// Icon/emoji for debug display
        /// </summary>
       // public abstract string Icon { get; }

        /// <summary>
        /// Remaining duration in seconds
        /// </summary>
        public float RemainingDuration { get; protected set; }

        /// <summary>
        /// Maximum duration (for UI bar display)
        /// </summary>
        public float MaxDuration { get; protected set; }

        /// <summary>
        /// Current stack count (if stackable)
        /// </summary>
        public int StackCount { get; protected set; }

        /// <summary>
        /// Maximum allowed stacks
        /// </summary>
        public virtual int MaxStacks => 1;

        /// <summary>
        /// Can this effect stack with itself?
        /// </summary>
        public virtual bool IsStackable => MaxStacks > 1;

        /// <summary>
        /// Should duration refresh when re-applied?
        /// </summary>
        public virtual bool RefreshOnReapply => true;

        /// <summary>
        /// Reference to affected enemy
        /// </summary>
        protected EnemyHealth target;

        /// <summary>
        /// Reference to enemy's GameObject (for VFX)
        /// </summary>
        protected GameObject targetGameObject;

        /// <summary>
        /// Has this effect finished?
        /// </summary>
        public bool IsExpired => RemainingDuration <= 0f;

        // ==========================================
        // INITIALIZATION
        // ==========================================

        /// <summary>
        /// Initializes the status effect
        /// Called when first applied to enemy
        /// </summary>
        public virtual void Initialize(EnemyHealth enemy, float duration)
        {
            target = enemy;
            targetGameObject = enemy.gameObject;
            MaxDuration = duration;
            RemainingDuration = duration;
            StackCount = 1;

            OnApplied();
        }

        // ==========================================
        // LIFECYCLE METHODS (override in subclasses)
        // ==========================================

        /// <summary>
        /// Called when effect is first applied
        /// Use for initial setup, VFX spawning, etc.
        /// </summary>
        protected virtual void OnApplied()
        {
           // Debug.Log($"[StatusEffect] {Icon} {DisplayName} applied to {targetGameObject.name} for {MaxDuration}s");
        }

        /// <summary>
        /// Called every frame while effect is active
        /// Use for DOT (damage over time), movement modification, etc.
        /// </summary>
        public virtual void Update(float deltaTime)
        {
            RemainingDuration -= deltaTime;
        }

        /// <summary>
        /// Called when effect is refreshed (re-applied before expiring)
        /// </summary>
        public virtual void OnRefreshed()
        {
            if (RefreshOnReapply)
            {
                RemainingDuration = MaxDuration;
                //Debug.Log($"[StatusEffect] {Icon} {DisplayName} refreshed on {targetGameObject.name}");
            }
        }

        /// <summary>
        /// Called when a stack is added (if stackable)
        /// </summary>
        public virtual void OnStackAdded()
        {
            if (IsStackable && StackCount < MaxStacks)
            {
                StackCount++;
               // Debug.Log($"[StatusEffect] {Icon} {DisplayName} stack added ({StackCount}/{MaxStacks})");
            }
        }

        /// <summary>
        /// Called when effect expires naturally
        /// </summary>
        public virtual void OnExpired()
        {
           // Debug.Log($"[StatusEffect] {Icon} {DisplayName} expired on {targetGameObject.name}");
        }

        /// <summary>
        /// Called when effect is forcibly removed (cleanse, immunity, etc.)
        /// </summary>
        public virtual void OnRemoved()
        {
           // Debug.Log($"[StatusEffect] {Icon} {DisplayName} removed from {targetGameObject.name}");
        }

        // ==========================================
        // HELPER METHODS
        // ==========================================

        /// <summary>
        /// Returns progress ratio (0-1) for UI display
        /// </summary>
        public float GetProgress()
        {
            return MaxDuration > 0 ? RemainingDuration / MaxDuration : 0f;
        }

        /// <summary>
        /// Returns formatted duration string (for UI)
        /// </summary>
        public string GetDurationText()
        {
            return $"{RemainingDuration:F1}s";
        }
    }

    // ==========================================
    // ENUM: Status Effect Types
    // ==========================================

    /// <summary>
    /// All available status effect types
    /// Add new types here when creating new effects
    /// </summary>
    public enum StatusEffectType
    {
        Burn,       //  Fire DOT (damage over time)
        Freeze,     //  Complete stop (can't move)
        Slow,       //  Movement speed reduction
        Poison,     //  Nature DOT (stacks)
        Stun,       //  Lightning disable (short duration)
        Bleed,      // Physical DOT
        Weakness,   // Reduces damage dealt
        Armor,      // Reduces damage taken (buff)
        Speed       // Increases movement speed (buff)
    }
}