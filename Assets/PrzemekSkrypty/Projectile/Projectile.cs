using UnityEngine;
using ElementumDefense.Elements;
using ElementumDefense.StatusEffects;
using ElementumDefense.Enemies;
using ElementumDefense.Turrets;

namespace ElementumDefense.Projectiles
{
    /// <summary>
    /// Base class for all projectiles
    /// Handles movement, collision, damage dealing
    /// </summary>
    public abstract class Projectile : MonoBehaviour
    {
        // ==========================================
        // CONFIGURATION
        // ==========================================

        [Header("Projectile Settings")]
        [SerializeField] public float speed = 10f;
        [SerializeField] protected float lifetime = 5f; // Auto-destroy after X seconds
        [SerializeField] protected LayerMask hitLayers; // What can be hit

        [Header("Visual Effects")]
        [SerializeField] protected ParticleSystem trailEffect;
        [SerializeField] protected GameObject impactEffectPrefab;
        [SerializeField] protected float impactEffectLifetime = 2f;

        [Header("AOE Options")]
        [SerializeField] protected bool hasAOE = false;
        [SerializeField] protected float baseAOERadius = 0f;
        [SerializeField] protected float aoeDamageMultiplier = 0.7f;
        // ==========================================
        // RUNTIME DATA
        // ==========================================

        protected int damage;
        protected ElementType elementType;
        protected StatusEffectType statusEffect;
        protected float statusChance;
        protected float statusDuration;
        protected float statusStrength;

        protected EnemyHealth target;
        protected Vector3 targetPosition;
        protected float currentLifetime;
        protected bool hasHit = false;

        // Reference to pool (for returning)
        protected ProjectilePool pool;

        // ==========================================
        // INITIALIZATION
        // ==========================================

        /// <summary>
        /// Initializes projectile with combat data
        /// Called by turret when firing
        /// </summary>
        public virtual void Initialize(
            EnemyHealth targetEnemy,
            int dmg,
            ElementType element,
            StatusEffectType effect = StatusEffectType.Burn,
            float effectChance = 0f,
            float effectDuration = 0f,
            float effectStrength = 0f,
            ProjectilePool poolRef = null)
        {
            // Set target
            target = targetEnemy;
            targetPosition = targetEnemy != null ? targetEnemy.transform.position : transform.position + transform.forward * 10f;

            // Set damage data
            damage = dmg;
            elementType = element;
            statusEffect = effect;
            statusChance = effectChance;
            statusDuration = effectDuration;
            statusStrength = effectStrength;

            // Set pool reference
            pool = poolRef;

            // Reset state
            currentLifetime = 0f;
            hasHit = false;

            // Apply element color to visual
            ApplyElementColor();

            // Start trail effect
            if (trailEffect != null)
            {
                trailEffect.Play();
            }

            // NOTE: RegisterShotFired is called by the spawner (TurretShooter)
            // before Initialize. Calling it here too would double-count every
            // shot, halving the reported accuracy.

            OnInitialized();
        }

        /// <summary>
        /// Override for custom initialization logic
        /// </summary>
        protected virtual void OnInitialized() { }

        // ==========================================
        // UPDATE LOOP
        // ==========================================

        protected virtual void Update()
        {
            if (hasHit) return;

            // Update lifetime
            currentLifetime += Time.deltaTime;
            if (currentLifetime >= lifetime)
            {
                ReturnToPool();
                return;
            }

            // Update movement (implemented by subclasses)
            UpdateMovement();

            // Check for manual collision detection (optional)
            CheckCollision();
        }

        /// <summary>
        /// Override this in subclasses for different movement types
        /// </summary>
        protected abstract void UpdateMovement();

        // ==========================================
        // COLLISION HANDLING
        // ==========================================

        /// <summary>
        /// Manual collision check using raycast/sphere
        /// </summary>
        protected virtual void CheckCollision()
        {
            // Override in subclasses if needed
            // Most projectiles will use OnTriggerEnter instead
        }

        /// <summary>
        /// Physics trigger collision
        /// </summary>
        protected virtual void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            // Check if hit valid target
            EnemyHealth enemy = other.GetComponent<EnemyHealth>();
            if (enemy != null)
            {
                OnHitTarget(enemy);
            }
        }

        /// <summary>
        /// Called when projectile hits target
        /// </summary>
        protected virtual void OnHitTarget(EnemyHealth enemy)
        {
            if (hasHit) return;
            hasHit = true;

            // Register hit
            if (ProjectileStatsManager.Instance != null)
            {
                ProjectileStatsManager.Instance.RegisterHit();
            }
            // Deal damage to primary target
            enemy.TakeDamage(damage, -1, elementType);

            // Try apply status effect
            if (statusChance > 0f && Random.Range(0f, 100f) <= statusChance)
            {
                ApplyStatusEffect(enemy);
            }

            // ========== NOWE: AOE Damage ==========
            if (hasAOE && baseAOERadius > 0f)
            {
                ApplyAOEDamage(enemy);
            }
            // ======================================

            // Spawn impact effect
            SpawnImpactEffect();

            Debug.Log($"[Projectile] Hit {enemy.name} for {damage} damage ({elementType})");

            // Return to pool
            ReturnToPool();
        }

        /// <summary>
        /// Applies AOE damage to nearby enemies (if enabled)
        /// </summary>
        protected virtual void ApplyAOEDamage(EnemyHealth primaryTarget)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, baseAOERadius);

            int secondaryHits = 0;

            foreach (Collider hit in hits)
            {
                EnemyHealth enemy = hit.GetComponent<EnemyHealth>();

                // Skip primary target and non-enemies
                if (enemy == null || enemy == primaryTarget) continue;

                // Calculate distance falloff
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                float falloff = Mathf.Clamp01(1f - (distance / baseAOERadius));

                // Deal reduced damage
                int aoeDamage = Mathf.RoundToInt(damage * aoeDamageMultiplier * falloff);

                if (aoeDamage > 0)
                {
                    enemy.TakeDamage(aoeDamage, -1, elementType);
                    secondaryHits++;

                    // Apply status effect with reduced chance
                    if (statusChance > 0f && Random.Range(0f, 100f) <= statusChance * 0.5f)
                    {
                        ApplyStatusEffect(enemy);
                    }
                }
            }

            if (secondaryHits > 0)
            {
                Debug.Log($"[Projectile] AOE hit {secondaryHits} additional enemies");
            }
        }               
        // ==========================================
        // STATUS EFFECTS
        // ==========================================

        /// <summary>
        /// Applies status effect to hit enemy
        /// </summary>
        protected virtual void ApplyStatusEffect(EnemyHealth enemy)
        {
            StatusEffectManager effectManager = enemy.GetComponent<StatusEffectManager>();
            if (effectManager == null) return;

            StatusEffect effect = CreateStatusEffect();
            if (effect != null)
            {
                effectManager.ApplyEffect(effect);
            }
        }

        /// <summary>
        /// Creates appropriate status effect instance via the shared factory.
        /// Override only if a specific projectile type needs custom behavior.
        /// </summary>
        protected virtual StatusEffect CreateStatusEffect()
        {
            return StatusEffectFactory.Create(statusEffect, statusStrength, statusDuration);
        }

        // ==========================================
        // VISUAL EFFECTS
        // ==========================================

        // Cached MPB so we don't allocate one per shot. MPBs are cheap, but
        // they're also reusable across all renderers (just clear + set + apply).
        // 
        // IMPORTANT: cannot use a field initializer here — Unity forbids
        // `new MaterialPropertyBlock()` from MonoBehaviour constructors / static
        // initializers ("CreateImpl is not allowed to be called from a
        // MonoBehaviour constructor"). Lazily created on first ApplyElementColor
        // call instead.
        private static MaterialPropertyBlock s_PropBlock;
        private static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int s_LegacyColorId = Shader.PropertyToID("_Color");
        private static readonly int s_EmissionColorId = Shader.PropertyToID("_EmissionColor");

        /// <summary>
        /// Applies element color to projectile visual via MaterialPropertyBlock.
        /// 
        /// Why MPB instead of <c>renderer.material.color = c</c>? Touching
        /// <c>renderer.material</c> CLONES the shared material — every projectile
        /// then keeps its own GC-allocated material instance for life. With
        /// pooling and 100+ active projectiles that's a steady memory leak and
        /// breaks SRP batching. MPB writes per-renderer overrides without
        /// instantiating a material, so all projectiles share one source asset.
        /// 
        /// Sets BOTH <c>_BaseColor</c> (URP/HDRP/Shader Graph) and <c>_Color</c>
        /// (built-in / legacy) so it works across render pipelines without
        /// per-shader configuration.
        /// </summary>
        protected virtual void ApplyElementColor()
        {
            Color elementColor = ElementUtility.GetElementColor(elementType);

            // Lazy init — must NOT be in a static field initializer (Unity
            // throws CreateImpl error during MB construction otherwise).
            if (s_PropBlock == null)
                s_PropBlock = new MaterialPropertyBlock();

            // Apply to renderer via MPB (zero-alloc, no material clone).
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.GetPropertyBlock(s_PropBlock);
                s_PropBlock.SetColor(s_BaseColorId, elementColor);
                s_PropBlock.SetColor(s_LegacyColorId, elementColor);
                // Optional emission tint — harmless if shader doesn't read it.
                s_PropBlock.SetColor(s_EmissionColorId, elementColor);
                rend.SetPropertyBlock(s_PropBlock);
            }

            // Trail particle system: setting startColor on the main module
            // doesn't clone a material, so this is fine as-is.
            if (trailEffect != null)
            {
                var main = trailEffect.main;
                main.startColor = elementColor;
            }
        }

        /// <summary>
        /// Spawns impact VFX at hit position
        /// </summary>
        protected virtual void SpawnImpactEffect()
        {
            if (impactEffectPrefab != null)
            {
                GameObject impact = Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);

                // Apply element color
                ParticleSystem ps = impact.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startColor = ElementUtility.GetElementColor(elementType);
                }

                Destroy(impact, impactEffectLifetime);
            }
        }

        // ==========================================
        // POOLING
        // ==========================================

        /// <summary>
        /// Returns projectile to pool (or destroys if no pool)
        /// </summary>
        protected virtual void ReturnToPool()
        {
            // Stop trail
            if (trailEffect != null)
            {
                trailEffect.Stop();
                trailEffect.Clear(); // <-- DODAJ to �eby wyczy�ci� trail
            }

            // ========== POPRAWKA: Znajd� pool parent ==========
            // Try to find pool by checking parent hierarchy
            Transform currentParent = transform.parent;

            while (currentParent != null)
            {
                ProjectilePool poolComponent = currentParent.GetComponent<ProjectilePool>();
                if (poolComponent != null)
                {
                    poolComponent.ReturnProjectile(this);
                    return;
                }
                currentParent = currentParent.parent;
            }

            // If no pool found, just destroy
            Debug.LogWarning($"[Projectile] No pool found for {gameObject.name}, destroying instead");
            Destroy(gameObject);
            // ===================================================
        }

        // ==========================================
        // HELPERS
        // ==========================================

        /// <summary>
        /// Sets custom speed (for upgrades)
        /// </summary>
        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }

        /// <summary>
        /// Gets current target (for homing projectiles)
        /// </summary>
        protected Vector3 GetTargetPosition()
        {
            // If target still alive, track it
            if (target != null)
            {
                return target.transform.position + Vector3.up * 0.5f; // Aim at center
            }

            // Otherwise use last known position
            return targetPosition;
        }
    }
}