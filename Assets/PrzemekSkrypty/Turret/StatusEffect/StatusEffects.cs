// Assets/PrzemekSkrypty/StatusEffects/StatusEffect.cs
using UnityEngine;
using ElementumDefense.Enemies;

namespace ElementumDefense.StatusEffects
{
    public abstract class StatusEffect
    {
        // ==========================================
        // PROPERTIES
        // ==========================================

        public abstract StatusEffectType EffectType { get; }
        public abstract string DisplayName { get; }

        public float RemainingDuration { get; protected set; }
        public float MaxDuration { get; protected set; }
        public int StackCount { get; protected set; }

        public virtual int MaxStacks => 1;
        public virtual bool IsStackable => MaxStacks > 1;
        public virtual bool RefreshOnReapply => true;

        protected EnemyHealth target;
        protected GameObject targetGameObject;

        public bool IsExpired => RemainingDuration <= 0f;

        // ==========================================
        // INITIALIZATION
        // ==========================================

        public virtual void Initialize(EnemyHealth enemy, float duration)
        {
            target = enemy;
            targetGameObject = enemy?.gameObject;
            MaxDuration = duration;
            RemainingDuration = duration;
            StackCount = 1;

            OnApplied();
        }

        // ==========================================
        // LIFECYCLE
        // ==========================================

        protected virtual void OnApplied() { }

        public virtual void Update(float deltaTime)
        {
            RemainingDuration -= deltaTime;
        }

        public virtual void OnRefreshed()
        {
            if (RefreshOnReapply)
            {
                RemainingDuration = MaxDuration;
            }
        }

        public virtual void OnStackAdded()
        {
            if (IsStackable && StackCount < MaxStacks)
            {
                StackCount++;

                // Od�wie� czas przy stackowaniu
                if (RefreshOnReapply)
                {
                    RemainingDuration = MaxDuration;
                }
            }
        }

        public virtual void OnExpired() { }
        public virtual void OnRemoved() { }

        // ==========================================
        // HELPERS
        // ==========================================

        public float GetProgress()
        {
            return MaxDuration > 0 ? Mathf.Clamp01(RemainingDuration / MaxDuration) : 0f;
        }

        public string GetDurationText()
        {
            return $"{RemainingDuration:F1}s";
        }
    }

    // ==========================================
    // ENUM
    // ==========================================

    public enum StatusEffectType
    {
        // --- Fire ---
        Burn,

        // --- Ice ---
        Chill,
        Freeze,

        // --- Lightning ---
        Stun,

        // --- Nature ---
        Poison,

        // --- Dark ---
        Curse,

        // --- Light (aura) ---
        Expose,

        // --- Generic ---
        Slow,
        Bleed,
        Speed
    }
}   