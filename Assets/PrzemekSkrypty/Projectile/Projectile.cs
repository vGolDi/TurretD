using UnityEngine;
using ElementumDefense.Elements;
using ElementumDefense.StatusEffects;

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

            // Deal damage
            enemy.TakeDamage(damage, -1, elementType);

            // Try apply status effect
            if (statusChance > 0f && Random.Range(0f, 100f) <= statusChance)
            {
                ApplyStatusEffect(enemy);
            }

            // Spawn impact effect
            SpawnImpactEffect();

            // Play hit sound
            // TODO: AudioManager.PlaySound("projectile_impact");

            Debug.Log($"[Projectile] Hit {enemy.name} for {damage} damage ({elementType})");

            // Return to pool
            ReturnToPool();
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
        /// Creates appropriate status effect instance
        /// </summary>
        protected virtual StatusEffect CreateStatusEffect()
        {
            return statusEffect switch
            {
                StatusEffectType.Burn => new BurnEffect(statusStrength, statusDuration),
                StatusEffectType.Freeze => new FreezeEffect(statusDuration),
                StatusEffectType.Slow => new SlowEffect(statusStrength, statusDuration),
                StatusEffectType.Poison => new PoisonEffect(statusStrength, statusDuration),
                _ => null
            };
        }

        // ==========================================
        // VISUAL EFFECTS
        // ==========================================

        /// <summary>
        /// Applies element color to projectile visual
        /// </summary>
        protected virtual void ApplyElementColor()
        {
            Color elementColor = ElementUtility.GetElementColor(elementType);

            // Apply to renderer
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = elementColor;
            }

            // Apply to trail
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
                trailEffect.Clear(); // <-- DODAJ to ¿eby wyczyœciæ trail
            }

            // ========== POPRAWKA: ZnajdŸ pool parent ==========
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