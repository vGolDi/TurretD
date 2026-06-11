using UnityEngine;
using Photon.Pun;
using ElementumDefense.Elements;
using ElementumDefense.StatusEffects;
using ElementumDefense.Projectiles;
using ElementumDefense.Enemies;

namespace ElementumDefense.Turrets
{
    /// <summary>
    /// Handles damage delivery: spawns projectile (if TurretData has prefab)
    /// or applies instant damage. Owns prediction math + status effect rolls.
    /// 
    /// Pluggable: replace for special shoot patterns (multi-shot, beam, burst,
    /// chained AOE that doesn't use a Projectile, etc.).
    /// </summary>
    public class TurretShooter : MonoBehaviour
    {
        [Header("Projectile Spawn")]
        [SerializeField, Tooltip("Optional fallback. Usually pushed in by Turret.UpdateVisuals from the display prefab's 'ProjectileSpawn' child.")]
        private Transform projectileSpawnPoint;

        [Header("Prediction")]
        [SerializeField, Tooltip("Reference projectile speed used for lead calculation (m/s)")]
        private float predictionBaseSpeed = 60f;

        private TurretTargeting targeting;

        private void Awake()
        {
            targeting = GetComponent<TurretTargeting>();
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        public Transform ProjectileSpawnPoint => projectileSpawnPoint;

        /// <summary>Pushed by Turret.UpdateVisuals once the display prefab is instantiated.</summary>
        public void SetSpawnPoint(Transform t) => projectileSpawnPoint = t;

        /// <summary>
        /// Performs an attack. Uses projectile if data has prefab, else instant damage.
        /// Called by Turret each time fireCooldown expires.
        /// </summary>
        public void Shoot(EnemyHealth target, TurretData data, float currentDamage, int ownerPhotonViewID)
        {
            if (target == null || data == null) return;

            // Resolve runtime modifiers (crit + conditional damage) from the owning
            // player's modifier stack — currentDamage already includes the base
            // multipliers, so we layer crit/conditional ON TOP of it.
            float finalDamage = currentDamage;
            bool wasCrit = false;
            ResolveRuntimeDamage(ref finalDamage, ref wasCrit, target, ownerPhotonViewID);

            if (data.projectilePrefab != null)
            {
                ShootProjectile(target, data, finalDamage);
            }
            else
            {
                // Instant-damage path (no projectile prefab assigned).
                // Count this as a "shot fired" too so stats stay consistent
                // between hitscan and projectile turrets.
                ProjectileStatsManager.Instance?.RegisterShotFired();

                target.TakeDamage((int)finalDamage, ownerPhotonViewID, data.elementType);
                TryApplyStatusEffect(target, data);
            }
        }

        /// <summary>
        /// Layers crit roll and conditional-damage bonuses on top of the
        /// pre-multiplied base damage. Read by both projectile and instant paths.
        /// </summary>
        private void ResolveRuntimeDamage(ref float damage, ref bool wasCrit, EnemyHealth target, int ownerPhotonViewID)
        {
            if (ownerPhotonViewID < 0) return;
            var ownerView = PhotonView.Find(ownerPhotonViewID);
            if (ownerView == null) return;

            var modStack = ownerView.GetComponent<ElementumDefense.Cards.PlayerModifierStack>();
            if (modStack == null) return;

            // Conditional bonus (vs boss / vs normal etc.)
            bool isBoss = target != null && target.IsBoss;
            float bonus = modStack.GetConditionalDamageBonusForTarget(isBoss);
            if (Mathf.Abs(bonus) > 0.0001f)
                damage *= 1f + bonus;

            // Crit roll
            if (modStack.CritChance > 0f && UnityEngine.Random.value < modStack.CritChance)
            {
                damage *= modStack.CritMultiplier;
                wasCrit = true;
            }
        }

        // ==========================================
        // PROJECTILE PATH
        // ==========================================

        private void ShootProjectile(EnemyHealth target, TurretData data, float currentDamage)
        {
            Vector3 spawnPos = GetSpawnPosition(data);
            Vector3 directionToTarget = GetPredictedDirection(target, data, spawnPos);

            Quaternion spawnRot = directionToTarget != Vector3.zero
                ? Quaternion.LookRotation(directionToTarget)
                : transform.rotation;

            Projectile projectile = ProjectileManager.Instance.SpawnProjectile(
                data.projectilePrefab,
                spawnPos,
                spawnRot);

            if (projectile == null)
            {
                Debug.LogError("[TurretShooter] Failed to spawn projectile!");
                return;
            }

            ProjectileStatsManager.Instance?.RegisterShotFired();

            projectile.Initialize(
                target,
                (int)currentDamage,
                data.elementType,
                data.appliedEffect,
                data.effectChance,
                data.effectDuration,
                data.effectStrength,
                null);

            if (data.projectileSpeedMultiplier != 1f)
            {
                projectile.SetSpeed(projectile.speed * data.projectileSpeedMultiplier);
            }
        }

        private Vector3 GetSpawnPosition(TurretData data)
        {
            if (projectileSpawnPoint != null)
                return projectileSpawnPoint.position;

            // Fallback: use the rotating part's local offset, then turret root.
            Transform rotPart = targeting != null ? targeting.RotatingPart : null;
            if (rotPart != null)
                return rotPart.position + rotPart.TransformDirection(data.projectileSpawnOffset);

            return transform.position + transform.TransformDirection(data.projectileSpawnOffset);
        }

        private Vector3 GetPredictedDirection(EnemyHealth target, TurretData data, Vector3 spawnPos)
        {
            Vector3 enemyPosition = target.transform.position + Vector3.up * 0.5f;
            UnityEngine.AI.NavMeshAgent enemyAgent = target.GetComponent<UnityEngine.AI.NavMeshAgent>();

            Vector3 predictedPosition = enemyPosition;

            if (enemyAgent != null && enemyAgent.velocity.magnitude > 0.1f)
            {
                float distanceToEnemy = Vector3.Distance(spawnPos, enemyPosition);
                float projectileSpeed = predictionBaseSpeed;

                if (data.projectileSpeedMultiplier > 0)
                    projectileSpeed *= data.projectileSpeedMultiplier;

                float timeToReach = distanceToEnemy / projectileSpeed;
                float extraLead = Mathf.Clamp(1.0f + (distanceToEnemy / 10f), 1.0f, 1.5f);
                timeToReach *= extraLead;

                predictedPosition = enemyPosition + (enemyAgent.velocity * timeToReach);
            }

            return (predictedPosition - spawnPos).normalized;
        }

        // ==========================================
        // STATUS EFFECTS (instant-damage path only)
        // ==========================================

        private void TryApplyStatusEffect(EnemyHealth target, TurretData data)
        {
            if (data.effectChance <= 0f) return;

            float roll = Random.Range(0f, 100f);
            if (roll > data.effectChance) return;

            StatusEffectManager effectManager = target.GetComponent<StatusEffectManager>();
            if (effectManager == null) return;

            StatusEffect newEffect = StatusEffectFactory.Create(
                data.appliedEffect,
                data.effectStrength,
                data.effectDuration);

            if (newEffect != null)
                effectManager.ApplyEffect(newEffect);
        }

        // ==========================================
        // DEBUG
        // ==========================================

        private void OnDrawGizmosSelected()
        {
            if (projectileSpawnPoint != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(projectileSpawnPoint.position, 0.2f);
                Gizmos.DrawRay(projectileSpawnPoint.position, projectileSpawnPoint.forward * 2f);
            }
        }
    }
}
