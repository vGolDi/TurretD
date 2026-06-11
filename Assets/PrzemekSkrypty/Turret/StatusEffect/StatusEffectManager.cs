// Assets/PrzemekSkrypty/StatusEffects/StatusEffectManager.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using ElementumDefense.Enemies;

namespace ElementumDefense.StatusEffects
{
    public class StatusEffectManager : MonoBehaviour, ElementumDefense.Enemies.IEnemyPoolable
    {
        // Events
        public event Action<StatusEffectType> OnEffectApplied;
        public event Action<StatusEffectType> OnEffectExpired;
        public event Action<StatusEffectType, int> OnEffectStacked; // type, newStackCount
        public event Action OnSlowEffectEnded;
        public event Action OnFreezeEffectEnded;
        public event Action OnAnyEffectEnded;
        public event Action OnChillStackedToFreeze;
        public event Action<float> OnCurseDamageMultiplierChanged; // float = multiplier

        [Header("Debug")]
        [SerializeField] private bool logEffects = false;

        private List<StatusEffect> activeEffects = new List<StatusEffect>();
        private EnemyHealth enemyHealth;
        private EnemyMovement enemyMovement;
        private ElementumDefense.Enemies.EnemyArmor enemyArmor;

        private float cachedSpeedMultiplier = 1f;
        private bool cachedIsFrozen = false;

        private float cachedDamageMultiplier = 1f; // Curse: amplify all incoming damage
        private float cachedArmorReduction = 0f;   // Expose: armor reduction

        public float IncomingDamageMultiplier => cachedDamageMultiplier;
        public float ArmorReduction => cachedArmorReduction;
        public float SpeedModifier => cachedSpeedMultiplier;
        public bool IsFrozen => cachedIsFrozen;

        private void Awake()
        {
            enemyHealth = GetComponent<EnemyHealth>();
            enemyMovement = GetComponent<EnemyMovement>();
            enemyArmor = GetComponent<ElementumDefense.Enemies.EnemyArmor>();
        }

        // ==========================================
        // POOLING
        // ==========================================

        /// <summary>Wipe all status effects when an enemy is reused from the pool.</summary>
        public void OnSpawnedFromPool()
        {
            // RemoveAllEffects also resets cached modifiers and pushes 1f speed
            // back to EnemyMovement.
            RemoveAllEffects();
        }

        public void OnReturnedToPool()
        {
            RemoveAllEffects();
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

            // SprawdÄŹĹĽËť immunitet (opcjonalne - do implementacji)
            // if (IsImmuneToEffect(newEffect.EffectType)) return;

            // ARMOR GUARD: jeśli wróg jest opancerzony i flaga blockStatusEffectsWhileArmored
            // jest włączona, blokujemy aplikowanie WSZYSTKICH statusów. Spójne z guardem
            // ARMOR GUARD: jeśli wróg jest opancerzony i flaga blockStatusEffectsWhileArmored
            // jest włączona, blokujemy aplikowanie WSZYSTKICH statusów. Spójne z guardem
            // w EnemyHealth.TakeDamage - gracz musi zbić armor żeby wszystko zaczęło działać.
            if (enemyArmor != null && enemyArmor.IsArmored && enemyArmor.BlockStatusEffectsWhileArmored)
            {
                if (logEffects)
                    Debug.Log($"[StatusEffectManager:{name}] ApplyEffect({newEffect.EffectType}) BLOKOWANE przez armor");
                return;
            }

            StatusEffect existingEffect = GetEffect(newEffect.EffectType);

            if (existingEffect != null)
            {
                // Efekt juÄŹĹĽËť istnieje - stack lub refresh
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
            // Only Initialize if not pre-initialized (MaxDuration == 0 means fresh)
            if (newEffect.MaxDuration <= 0f)
                newEffect.Initialize(enemyHealth, 3f); // Default duration fallback
            else
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
            cachedDamageMultiplier = 1f;
            cachedArmorReduction = 0f;

            foreach (var effect in activeEffects)
            {
                // Freeze = complete stop
                if (effect.EffectType == StatusEffectType.Freeze)
                {
                    cachedIsFrozen = true;
                    cachedSpeedMultiplier = 0f;
                    break; // Nie trzeba sprawdzaÄŹĹĽËť dalej
                }

                // Stun = also stops movement
                if (effect.EffectType == StatusEffectType.Stun)
                {
                    cachedIsFrozen = true;
                    cachedSpeedMultiplier = 0f;
                    break;
                }

                // Chill = partial slow (Ice element)
                if (effect is ChillEffect chillEffect)
                {
                    cachedSpeedMultiplier *= chillEffect.SlowMultiplier;
                    continue;
}

                // Curse = amplify incoming damage (Dark element)
                if (effect is CurseEffect curseEffect)
                {
                    cachedDamageMultiplier *= curseEffect.DamageMultiplier;
                    continue;
                }

                // Expose = armor reduction (Light aura)
                if (effect is ExposeEffect exposeEffect)
                {
                    cachedArmorReduction = Mathf.Max(cachedArmorReduction, exposeEffect.ArmorReduction);
                    continue;
                }

                // Slow = reduce speed
                if (effect is SlowEffect slowEffect)
                {
                    cachedSpeedMultiplier *= slowEffect.SlowMultiplier;
                }

                // Speed buff = increase speed
                if (effect.EffectType == StatusEffectType.Speed)
                {
                    cachedSpeedMultiplier *= 1.5f; // Dostosuj mnoÄŹĹĽËťnik
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
