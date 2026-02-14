// Assets/PrzemekSkrypty/StatusEffects/StatusEffectManager.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace ElementumDefense.StatusEffects
{
    public class StatusEffectManager : MonoBehaviour
    {
        // Events
        public event Action<StatusEffectType> OnEffectApplied;
        public event Action<StatusEffectType> OnEffectExpired;
        public event Action<StatusEffectType, int> OnEffectStacked; // type, newStackCount
        public event Action OnSlowEffectEnded;
        public event Action OnFreezeEffectEnded;
        public event Action OnAnyEffectEnded;

        [Header("Debug")]
        [SerializeField] private bool logEffects = false;

        private List<StatusEffect> activeEffects = new List<StatusEffect>();
        private EnemyHealth enemyHealth;
        private EnemyMovement enemyMovement;

        private float cachedSpeedMultiplier = 1f;
        private bool cachedIsFrozen = false;

        public float SpeedModifier => cachedSpeedMultiplier;
        public bool IsFrozen => cachedIsFrozen;

        private void Awake()
        {
            enemyHealth = GetComponent<EnemyHealth>();
            enemyMovement = GetComponent<EnemyMovement>();
        }

        private void Update()
        {
            if (activeEffects.Count == 0) return;

            UpdateEffects(Time.deltaTime);
            RecalculateModifiers();
        }

        // ==========================================
        // APPLY / REMOVE
        // ==========================================

        public void ApplyEffect(StatusEffect newEffect)
        {
            if (newEffect == null) return;

            // Sprawdü immunitet (opcjonalne - do implementacji)
            // if (IsImmuneToEffect(newEffect.EffectType)) return;

            StatusEffect existingEffect = GetEffect(newEffect.EffectType);

            if (existingEffect != null)
            {
                // Efekt juø istnieje - stack lub refresh
                if (existingEffect.IsStackable && existingEffect.StackCount < existingEffect.MaxStacks)
                {
                    existingEffect.OnStackAdded();
                    OnEffectStacked?.Invoke(existingEffect.EffectType, existingEffect.StackCount);

                    if (logEffects)
                        Debug.Log($"[StatusEffect] {existingEffect.DisplayName} stacked to {existingEffect.StackCount}");
                }

                if (existingEffect.RefreshOnReapply)
                {
                    existingEffect.OnRefreshed();
                }

                return;
            }

            // Nowy efekt
            newEffect.Initialize(enemyHealth, newEffect.MaxDuration);
            activeEffects.Add(newEffect);

            OnEffectApplied?.Invoke(newEffect.EffectType);

            if (logEffects)
                Debug.Log($"[StatusEffect] Applied {newEffect.DisplayName} to {gameObject.name} for {newEffect.MaxDuration}s");
        }

        public void RemoveEffect(StatusEffectType effectType)
        {
            StatusEffect effect = GetEffect(effectType);
            if (effect != null)
            {
                effect.OnRemoved();
                activeEffects.Remove(effect);
                TriggerEffectEndedEvents(effectType);
            }
        }

        public void RemoveAllEffects()
        {
            foreach (var effect in activeEffects.ToList())
            {
                effect.OnRemoved();
                TriggerEffectEndedEvents(effect.EffectType);
            }
            activeEffects.Clear();

            // Reset modifiers
            cachedSpeedMultiplier = 1f;
            cachedIsFrozen = false;

            if (enemyMovement != null)
                enemyMovement.SetSpeedModifier(1f);
        }

        // ==========================================
        // UPDATE LOOP
        // ==========================================

        private void UpdateEffects(float deltaTime)
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                StatusEffect effect = activeEffects[i];
                effect.Update(deltaTime);

                if (effect.IsExpired)
                {
                    effect.OnExpired();
                    activeEffects.RemoveAt(i);
                    TriggerEffectEndedEvents(effect.EffectType);

                    if (logEffects)
                        Debug.Log($"[StatusEffect] {effect.DisplayName} expired on {gameObject.name}");
                }
            }
        }

        private void TriggerEffectEndedEvents(StatusEffectType effectType)
        {
            OnEffectExpired?.Invoke(effectType);
            OnAnyEffectEnded?.Invoke();

            switch (effectType)
            {
                case StatusEffectType.Slow:
                    OnSlowEffectEnded?.Invoke();
                    break;
                case StatusEffectType.Freeze:
                    OnFreezeEffectEnded?.Invoke();
                    break;
            }
        }

        // ==========================================
        // MODIFIER CALCULATION
        // ==========================================

        private void RecalculateModifiers()
        {
            cachedSpeedMultiplier = 1f;
            cachedIsFrozen = false;

            foreach (var effect in activeEffects)
            {
                // Freeze = complete stop
                if (effect.EffectType == StatusEffectType.Freeze)
                {
                    cachedIsFrozen = true;
                    cachedSpeedMultiplier = 0f;
                    break; // Nie trzeba sprawdzaÊ dalej
                }

                // Stun = also stops movement
                if (effect.EffectType == StatusEffectType.Stun)
                {
                    cachedIsFrozen = true;
                    cachedSpeedMultiplier = 0f;
                    break;
                }

                // Slow = reduce speed
                if (effect is SlowEffect slowEffect)
                {
                    cachedSpeedMultiplier *= slowEffect.SlowMultiplier;
                }

                // Speed buff = increase speed
                if (effect.EffectType == StatusEffectType.Speed)
                {
                    cachedSpeedMultiplier *= 1.5f; // Dostosuj mnoønik
                }
            }

            // Clamp speed
            cachedSpeedMultiplier = Mathf.Clamp(cachedSpeedMultiplier, 0f, 3f);

            if (enemyMovement != null)
            {
                enemyMovement.SetSpeedModifier(cachedSpeedMultiplier);
            }
        }

        // ==========================================
        // QUERIES
        // ==========================================

        public StatusEffect GetEffect(StatusEffectType type)
        {
            return activeEffects.FirstOrDefault(e => e.EffectType == type);
        }

        public bool HasEffect(StatusEffectType type)
        {
            return GetEffect(type) != null;
        }

        public List<StatusEffect> GetActiveEffects()
        {
            return new List<StatusEffect>(activeEffects);
        }

        public int GetEffectCount()
        {
            return activeEffects.Count;
        }

        /// <summary>
        /// Checks if enemy has any movement-impairing effect
        /// </summary>
        public bool IsMovementImpaired()
        {
            return cachedIsFrozen || cachedSpeedMultiplier < 1f;
        }

        /// <summary>
        /// Gets total DOT damage per second from all active effects
        /// </summary>
        public float GetTotalDOTDamage()
        {
            float totalDOT = 0f;

            foreach (var effect in activeEffects)
            {
                if (effect is BurnEffect burn)
                {
                    // BurnEffect doesn't expose DPS publicly, but you could add it
                    totalDOT += 5f * effect.StackCount; // Placeholder
                }
                // Add other DOT effects here
            }

            return totalDOT;
        }
    }
}