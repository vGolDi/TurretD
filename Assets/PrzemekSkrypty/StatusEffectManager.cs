using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System; // <-- NOWE

namespace ElementumDefense.StatusEffects
{
    public class StatusEffectManager : MonoBehaviour
    {
        // ========== NOWE: Events ==========
        public event Action OnSlowEffectEnded;
        public event Action OnFreezeEffectEnded;
        public event Action OnAnyEffectEnded;
        // ==================================

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
            UpdateEffects(Time.deltaTime);
            RecalculateModifiers();
        }

        public void ApplyEffect(StatusEffect newEffect)
        {
            if (newEffect == null) return;

            StatusEffect existingEffect = GetEffect(newEffect.EffectType);

            if (existingEffect != null)
            {
                if (existingEffect.IsStackable)
                {
                    existingEffect.OnStackAdded();
                }

                if (existingEffect.RefreshOnReapply)
                {
                    existingEffect.OnRefreshed();
                }

                return;
            }

            newEffect.Initialize(enemyHealth, newEffect.MaxDuration);
            activeEffects.Add(newEffect);

            Debug.Log($"[StatusEffectManager] Applied {newEffect.Icon} {newEffect.DisplayName} to {gameObject.name}");
        }

        public void RemoveEffect(StatusEffectType effectType)
        {
            StatusEffect effect = GetEffect(effectType);
            if (effect != null)
            {
                effect.OnRemoved();
                activeEffects.Remove(effect);
            }
        }

        public void RemoveAllEffects()
        {
            foreach (var effect in activeEffects.ToList())
            {
                effect.OnRemoved();
            }
            activeEffects.Clear();

            Debug.Log($"[StatusEffectManager] Cleansed all effects from {gameObject.name}");
        }

        private void UpdateEffects(float deltaTime)
        {
            for (int i = activeEffects.Count - 1; i >= 0; i--)
            {
                StatusEffect effect = activeEffects[i];
                effect.Update(deltaTime);

                if (effect.IsExpired)
                {
                    // ========== NOWE: Trigger events ==========
                    TriggerEffectEndedEvents(effect.EffectType);
                    // ===========================================

                    effect.OnExpired();
                    activeEffects.RemoveAt(i);
                }
            }
        }

        // ========== NOWA FUNKCJA ==========
        /// <summary>
        /// Triggers appropriate events when effect ends
        /// </summary>
        private void TriggerEffectEndedEvents(StatusEffectType effectType)
        {
            OnAnyEffectEnded?.Invoke();

            switch (effectType)
            {
                case StatusEffectType.Slow:
                    OnSlowEffectEnded?.Invoke();
                    Debug.Log($"[StatusEffectManager] Slow effect ended - event triggered");
                    break;

                case StatusEffectType.Freeze:
                    OnFreezeEffectEnded?.Invoke();
                    Debug.Log($"[StatusEffectManager] Freeze effect ended - event triggered");
                    break;
            }
        }
        // ==================================

        private void RecalculateModifiers()
        {
            cachedSpeedMultiplier = 1f;
            cachedIsFrozen = false;

            foreach (var effect in activeEffects)
            {
                if (effect is FreezeEffect)
                {
                    cachedIsFrozen = true;
                    cachedSpeedMultiplier = 0f;
                    return;
                }

                if (effect is SlowEffect slowEffect)
                {
                    cachedSpeedMultiplier *= slowEffect.SlowMultiplier;
                }
            }

            if (enemyMovement != null)
            {
                enemyMovement.SetSpeedModifier(cachedSpeedMultiplier);
            }
        }

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

        private void OnGUI()
        {
            if (activeEffects.Count == 0) return;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);

            if (screenPos.z > 0)
            {
                string effectsText = string.Join(" ", activeEffects.Select(e => e.Icon));
                GUI.Label(new Rect(screenPos.x - 50, Screen.height - screenPos.y - 20, 100, 20), effectsText);
            }
        }
    }
}