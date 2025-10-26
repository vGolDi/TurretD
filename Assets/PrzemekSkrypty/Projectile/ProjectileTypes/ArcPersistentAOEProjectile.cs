using UnityEngine;
using ElementumDefense.Elements;
using ElementumDefense.StatusEffects;

namespace ElementumDefense.Projectiles
{
    /// <summary>
    /// Arc projectile that creates persistent AOE zone on landing
    /// Good for: Grenades, dynamite, molotov cocktails, mortars
    /// </summary>
    public class ArcPersistentAOEProjectile : ArcProjectile
    {
        [Header("Persistent AOE Settings")]
        [SerializeField] private float zoneRadius = 4f;
        [SerializeField] private float zoneDuration = 5f;
        [SerializeField] private float tickInterval = 0.5f;
        [SerializeField] private float damagePerTick = 5f;

        [Header("Landing Behavior")]
        [SerializeField] private bool explodeOnLanding = true; // Instant damage + zone
        [SerializeField] private float explosionRadius = 2f; // Instant damage radius
        [SerializeField] private float explosionDamageMultiplier = 0.5f; // 50% instant damage

        [Header("Zone Effects")]
        [SerializeField] private GameObject aoeZonePrefab;
        [SerializeField] private GameObject explosionEffectPrefab;
        [SerializeField] private bool slowEnemiesInZone = false;
        [SerializeField] private float slowAmount = 0.5f;

        [Header("Ground Detection")]
        [SerializeField] private LayerMask groundLayer = -1;
        [SerializeField] private float groundOffset = 0.1f;

        protected override void OnHitTarget(EnemyHealth primaryTarget)
        {
            if (hasHit) return;
            hasHit = true;

            // Find ground position
            Vector3 landingPosition = FindGroundPosition(transform.position);

            // Optional: Instant explosion damage
            if (explodeOnLanding)
            {
                DealExplosionDamage(landingPosition);
                SpawnExplosionEffect(landingPosition);
            }

            // Create persistent zone
            CreateAOEZone(landingPosition);

            Debug.Log($"[ArcPersistentAOE] Grenade landed at {landingPosition}");

            ReturnToPool();
        }

        /// <summary>
        /// Finds ground position below landing point
        /// </summary>
        private Vector3 FindGroundPosition(Vector3 landingPoint)
        {
            RaycastHit hit;
            Vector3 rayStart = landingPoint + Vector3.up * 2f;

            if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f, groundLayer))
            {
                return hit.point + Vector3.up * groundOffset;
            }

            // Fallback to Y=0
            return new Vector3(landingPoint.x, groundOffset, landingPoint.z);
        }

        /// <summary>
        /// Deals instant explosion damage on landing
        /// </summary>
        private void DealExplosionDamage(Vector3 explosionCenter)
        {
            Collider[] hits = Physics.OverlapSphere(explosionCenter, explosionRadius);

            int enemiesHit = 0;

            foreach (Collider hit in hits)
            {
                EnemyHealth enemy = hit.GetComponent<EnemyHealth>();
                if (enemy == null) continue;

                // Calculate distance falloff
                float distance = Vector3.Distance(explosionCenter, hit.transform.position);
                float falloff = Mathf.Clamp01(1f - (distance / explosionRadius));

                // Deal instant damage
                int explosionDamage = Mathf.RoundToInt(damage * explosionDamageMultiplier * falloff);

                if (explosionDamage > 0)
                {
                    enemy.TakeDamage(explosionDamage, -1, elementType);
                    enemiesHit++;

                    // Apply status effect with reduced chance
                    if (statusChance > 0f && Random.Range(0f, 100f) <= statusChance * 0.3f)
                    {
                        ApplyStatusEffect(enemy);
                    }
                }
            }

            Debug.Log($"[ArcPersistentAOE] Explosion hit {enemiesHit} enemies for instant damage");
        }

        /// <summary>
        /// Spawns explosion visual effect
        /// </summary>
        private void SpawnExplosionEffect(Vector3 position)
        {
            GameObject explosionPrefab = explosionEffectPrefab != null ? explosionEffectPrefab : impactEffectPrefab;

            if (explosionPrefab != null)
            {
                GameObject explosion = Instantiate(explosionPrefab, position, Quaternion.identity);

                // Scale based on explosion radius
                explosion.transform.localScale = Vector3.one * (explosionRadius / 2f);

                // Apply element color
                ParticleSystem ps = explosion.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    var main = ps.main;
                    main.startColor = ElementUtility.GetElementColor(elementType);
                }

                Destroy(explosion, impactEffectLifetime);
            }
        }

        /// <summary>
        /// Creates persistent damage zone
        /// </summary>
        private void CreateAOEZone(Vector3 position)
        {
            GameObject zone = new GameObject($"AOE_Zone_{elementType}");
            zone.transform.position = position;

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

            if (aoeZonePrefab != null)
            {
                GameObject visual = Instantiate(aoeZonePrefab, position, Quaternion.identity, zone.transform);
                visual.transform.localScale = Vector3.one * (zoneRadius * 2f);

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
            // Explosion radius (red)
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, explosionRadius);

            // Zone radius (green)
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, zoneRadius);

            // Ground ray (yellow)
            Gizmos.color = Color.yellow;
            Vector3 rayStart = transform.position + Vector3.up * 2f;
            Gizmos.DrawLine(rayStart, rayStart + Vector3.down * 10f);
        }
    }
}
