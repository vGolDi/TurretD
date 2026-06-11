using UnityEngine;
using ElementumDefense.Enemies;

namespace ElementumDefense.Turrets
{
    /// <summary>
    /// Handles target acquisition, range checking, and rotation toward target.
    /// 
    /// Pluggable: replace this component for custom targeting (support turrets
    /// that target allies, area scanners, "lowest HP" priority, etc.).
    /// 
    /// Range is pushed in by Turret after every stat recalculation via SetRange().
    /// Rotating part is pushed in by Turret.UpdateVisuals() once display prefab
    /// is instantiated.
    /// 
    /// Performance: scans are throttled and use a shared NonAlloc buffer so we
    /// allocate zero garbage per frame. With 30 turrets at 60 fps the previous
    /// implementation produced ~1800 GC allocations/second from OverlapSphere
    /// alone; this version produces 0.
    /// </summary>
    public class TurretTargeting : MonoBehaviour
    {
        // ==========================================
        // SHARED BUFFER (zero-alloc OverlapSphereNonAlloc)
        // ==========================================

        // Static so all turrets in the scene share one allocation.
        // 64 colliders is plenty for a TD wave; if ever exceeded we just drop
        // the overflow (closest-target logic still gives a usable result).
        private static readonly Collider[] s_HitBuffer = new Collider[64];

        // ==========================================
        // INSPECTOR
        // ==========================================

        [Header("Rotation")]
        [SerializeField, Tooltip("Optional fallback if Turret.UpdateVisuals doesn't push one. Usually leave empty.")]
        private Transform rotatingPart;

        [SerializeField, Tooltip("How fast the turret rotates toward target (Slerp factor)")]
        private float rotationSpeed = 5f;

        [Header("Acquisition")]
        [SerializeField, Tooltip("How often (in seconds) to scan for new targets when none is cached. " +
            "Lower = snappier acquisition but more physics queries. 0.1 = 10 scans/sec.")]
        private float reacquireInterval = 0.1f;

        [SerializeField, Tooltip("Layers to scan for enemies. Default = Everything (matches old behavior).")]
        private LayerMask enemyLayerMask = ~0;

        // ==========================================
        // RUNTIME
        // ==========================================

        private float currentRange;
        private EnemyHealth currentTarget;
        private float nextScanTime;

        // ==========================================
        // PUBLIC API
        // ==========================================

        public EnemyHealth CurrentTarget => currentTarget;
        public Transform RotatingPart => rotatingPart;
        public float CurrentRange => currentRange;

        /// <summary>Pushed by Turret.UpdateVisuals when the visual is re-spawned.</summary>
        public void SetRotatingPart(Transform t) => rotatingPart = t;

        /// <summary>Pushed by Turret.RecalculateStats whenever current range changes.</summary>
        public void SetRange(float range) => currentRange = range;

        /// <summary>
        /// Returns a valid target. Cheap path validates the cached target every
        /// frame; expensive scan only runs when the cache is empty/invalid AND
        /// the scan throttle has expired.
        /// </summary>
        public EnemyHealth AcquireTarget()
        {
            // Fast path: existing target still valid.
            if (currentTarget != null && IsInRange(currentTarget))
                return currentTarget;

            // Throttle the expensive Physics scan.
            if (Time.time < nextScanTime)
                return null;

            currentTarget = FindNewTarget();
            nextScanTime = Time.time + reacquireInterval;
            return currentTarget;
        }

        /// <summary>True if target is alive, in range, and not currently armored.</summary>
        public bool IsInRange(EnemyHealth target)
        {
            if (target == null) return false;

            var armor = target.GetComponent<EnemyArmor>();
            if (armor != null && armor.IsArmored) return false;

            // sqrMagnitude avoids the sqrt in Vector3.Distance.
            float sqrDist = (transform.position - target.transform.position).sqrMagnitude;
            return sqrDist <= currentRange * currentRange;
        }

        /// <summary>Smoothly rotates the rotating part toward the current target.</summary>
        public void RotateTowardsTarget()
        {
            if (rotatingPart == null || currentTarget == null) return;

            Vector3 direction = currentTarget.transform.position - rotatingPart.position;
            direction.y = 0f;

            if (direction == Vector3.zero) return;

            Quaternion lookRotation = Quaternion.LookRotation(direction);
            rotatingPart.rotation = Quaternion.Slerp(
                rotatingPart.rotation,
                lookRotation,
                Time.deltaTime * rotationSpeed);
        }

        // ==========================================
        // INTERNAL
        // ==========================================

        private EnemyHealth FindNewTarget()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                currentRange,
                s_HitBuffer,
                enemyLayerMask,
                QueryTriggerInteraction.Collide);

            EnemyHealth nearest = null;
            float nearestSqrDistance = float.PositiveInfinity;
            float rangeSqr = currentRange * currentRange;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = s_HitBuffer[i];
                if (hit == null) continue;

                EnemyHealth potential = hit.GetComponent<EnemyHealth>();
                if (potential == null) continue;

                // Skip armored enemies — player must click them off first.
                var armor = potential.GetComponent<EnemyArmor>();
                if (armor != null && armor.IsArmored) continue;

                float sqrDist = (transform.position - hit.transform.position).sqrMagnitude;
                if (sqrDist < nearestSqrDistance && sqrDist <= rangeSqr)
                {
                    nearestSqrDistance = sqrDist;
                    nearest = potential;
                }
            }

            return nearest;
        }

        // ==========================================
        // DEBUG
        // ==========================================

        private void OnDrawGizmosSelected()
        {
            float drawRange = Application.isPlaying ? currentRange : 5f;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, drawRange);
        }
    }
}
