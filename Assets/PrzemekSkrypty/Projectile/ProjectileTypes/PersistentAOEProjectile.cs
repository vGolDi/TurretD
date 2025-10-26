using UnityEngine;
using ElementumDefense.Elements;
using ElementumDefense.StatusEffects;

namespace ElementumDefense.Projectiles
{
    /// <summary>
    /// Creates a damaging AOE zone at target location (PERSISTENT ZONE)
    /// Good for: Ice storms, poison clouds, fire zones
    /// </summary>
    public class PersistentAOEProjectile : StraightProjectile
    {
        [Header("Persistent AOE Settings")]
        [SerializeField] private float zoneRadius = 4f; 
        [SerializeField] private float zoneDuration = 5f;
        [SerializeField] private float tickInterval = 0.5f;
        [SerializeField] private float damagePerTick = 5f;

        [Header("Zone Effects")]
        [SerializeField] private GameObject aoeZonePrefab;
        [SerializeField] private bool slowEnemiesInZone = false;
        [SerializeField] private float slowAmount = 0.5f;

        [Header("Zone Placement")]
        [SerializeField] private bool snapToGround = true; 
        [SerializeField] private float groundOffset = 0.1f; 
        [SerializeField] private LayerMask groundLayer;

        protected override void OnHitTarget(EnemyHealth primaryTarget)
        {
            if (hasHit) return;
            hasHit = true;

            Vector3 zonePosition = FindGroundPosition(transform.position);
            CreateAOEZone(transform.position);

            // Spawn impact effect
            SpawnImpactEffect();

            Debug.Log($"[PersistentAOE] Created AOE zone at ground level: {zonePosition}");

            ReturnToPool();
        }
        /// <summary>
        /// Finds ground position below projectile impact point
        /// </summary>
        private Vector3 FindGroundPosition(Vector3 impactPoint)
        {
            if (!snapToGround)
            {
                return impactPoint;
            }

            Vector3 rayStart = impactPoint + Vector3.up * 10f; 

            RaycastHit hit;

            if (Physics.Raycast(rayStart, Vector3.down, out hit, 20f, groundLayer))
            {
                Vector3 groundPos = hit.point + Vector3.up * groundOffset;

                Debug.Log($"[PersistentAOE] Hit: {hit.collider.name} at Y={hit.point.y}, placing zone at Y={groundPos.y}");

                return groundPos;
            }
            else
            {
                Debug.LogWarning($"[PersistentAOE] No ground found! Using Y=0. Make sure ground has correct layer!");
                return new Vector3(impactPoint.x, groundOffset, impactPoint.z);
            }
        }
        private void CreateAOEZone(Vector3 position)
        {
            position.y = 0.26f; // groundOffset 
            GameObject zone = new GameObject($"AOE_Zone_{elementType}");
            zone.transform.position = position;

            // Add zone component
            AOEZone zoneScript = zone.AddComponent<AOEZone>();
            zoneScript.Initialize(
                zoneRadius,
                zoneDuration,
                tickInterval,
                damagePerTick,
                elementType,
                statusEffect,
                statusChance,
                statusDuration,
                statusStrength,
                slowEnemiesInZone,
                slowAmount
            );

            // Spawn visual effect
            if (aoeZonePrefab != null)
            {
                GameObject visual = Instantiate(aoeZonePrefab, position, Quaternion.identity, zone.transform);
                visual.transform.localScale = Vector3.one * (zoneRadius * 2f);

                // Apply element color
                ParticleSystem ps = visual.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startColor = ElementUtility.GetElementColor(elementType);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
            Gizmos.DrawSphere(transform.position, zoneRadius);

            if (snapToGround)
            {
                Gizmos.color = Color.yellow;
                Vector3 rayStart = transform.position + Vector3.up * 2f;
                Gizmos.DrawLine(rayStart, rayStart + Vector3.down * 10f);
            }
        }
    }

    /// <summary>
    /// Component that creates persistent damage zone
    /// </summary>
    public class AOEZone : MonoBehaviour
    {
        private float radius;
        private float duration;
        private float tickInterval;
        private float damagePerTick;
        private ElementType element;
        private StatusEffectType statusEffect;
        private float statusChance;
        private float statusDuration;
        private float statusStrength;
        private bool applySlow;
        private float slowAmount;

        private float tickTimer = 0f;
        private float lifetimeTimer = 0f;

        public void Initialize(
            float rad,
            float dur,
            float tickInt,
            float dmg,
            ElementType elem,
            StatusEffectType statusType,
            float statusChc,
            float statusDur,
            float statusStr,
            bool slow,
            float slowAmt)
        {
            radius = rad;
            duration = dur;
            tickInterval = tickInt;
            damagePerTick = dmg;
            element = elem;
            statusEffect = statusType;
            statusChance = statusChc;
            statusDuration = statusDur;
            statusStrength = statusStr;
            applySlow = slow;
            slowAmount = slowAmt;

            Debug.Log($"[AOEZone] Created {elem} zone: {rad}m radius, {dur}s duration, {dmg} dmg/tick");
        }

        private void Update()
        {
            lifetimeTimer += Time.deltaTime;
            tickTimer += Time.deltaTime;

            // Destroy after duration
            if (lifetimeTimer >= duration)
            {
                Debug.Log($"[AOEZone] Zone expired after {duration}s");
                Destroy(gameObject);
                return;
            }

            // Damage tick
            if (tickTimer >= tickInterval)
            {
                ApplyDamageToEnemiesInZone();
                tickTimer = 0f;
            }
        }

        private void ApplyDamageToEnemiesInZone()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, radius);

            int enemiesHit = 0;

            foreach (Collider hit in hits)
            {
                EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
                if (enemy == null) continue;

                // Deal damage
                enemy.TakeDamage(Mathf.RoundToInt(damagePerTick), -1, element);
                enemiesHit++;

                // Apply status effect
                if (statusChance > 0f && Random.Range(0f, 100f) <= statusChance)
                {
                    StatusEffectManager effectManager = enemy.GetComponent<StatusEffectManager>();
                    if (effectManager != null)
                    {
                        StatusEffect effect = CreateStatusEffect();
                        if (effect != null)
                        {
                            effectManager.ApplyEffect(effect);
                        }
                    }
                }

                // Apply slow
                if (applySlow)
                {
                    StatusEffectManager effectManager = enemy.GetComponent<StatusEffectManager>();
                    if (effectManager != null)
                    {
                        effectManager.ApplyEffect(new SlowEffect(slowAmount, tickInterval + 0.1f));
                    }
                }
            }

            if (enemiesHit > 0)
            {
                Debug.Log($"[AOEZone] Tick damaged {enemiesHit} enemies for {damagePerTick} each");
            }
        }

        private StatusEffect CreateStatusEffect()
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

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}