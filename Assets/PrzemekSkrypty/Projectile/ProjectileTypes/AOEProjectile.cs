using ElementumDefense.Elements;
using UnityEngine;

namespace ElementumDefense.Projectiles
{
    /// <summary>
    /// Projectile that deals area damage on impact (INSTANT EXPLOSION)
    /// Good for: Explosions, fireballs, rockets
    /// </summary>
    public class AOEProjectile : StraightProjectile
    {
        [Header("AOE Settings")]
        [SerializeField] private float explosionRadius = 3f;
        [SerializeField] private float damageMultiplier = 0.7f;
        [SerializeField] private GameObject explosionEffectPrefab;

        protected override void OnHitTarget(EnemyHealth primaryTarget)
        {
            if (hasHit) return;
            hasHit = true;

            // Deal damage to primary target (full damage)
            primaryTarget.TakeDamage(damage, -1, elementType);

            // Find all enemies in explosion radius
            Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (Collider hit in hits)
            {
                EnemyHealth enemy = hit.GetComponent<EnemyHealth>();

                // Skip primary target (already hit) and non-enemies
                if (enemy == null || enemy == primaryTarget) continue;

                // Calculate distance falloff
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                float falloff = Mathf.Clamp01(1f - (distance / explosionRadius));

                // Deal reduced damage
                int aoeDamage = Mathf.RoundToInt(damage * damageMultiplier * falloff);

                if (aoeDamage > 0) 
                {
                    enemy.TakeDamage(aoeDamage, -1, elementType);

                    // Apply status effect with reduced chance
                    if (statusChance > 0f && Random.Range(0f, 100f) <= statusChance * 0.5f)
                    {
                        ApplyStatusEffect(enemy);
                    }
                }
            }

            // Spawn explosion effect
            SpawnExplosionEffect();

            Debug.Log($"[AOEProjectile] Explosion hit {hits.Length} enemies in {explosionRadius}m radius");

            ReturnToPool();
        }

        private void SpawnExplosionEffect()
        {
            GameObject explosionPrefab = explosionEffectPrefab != null ? explosionEffectPrefab : impactEffectPrefab;

            if (explosionPrefab != null)
            {
                GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);

                // Scale based on radius
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, explosionRadius);
        }
    }
}