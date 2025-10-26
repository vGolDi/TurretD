using UnityEngine;

namespace ElementumDefense.Projectiles
{
    public class ArcProjectile : Projectile
    {
        [Header("Arc Settings")]
        [SerializeField] private float arcHeight = 3f;
        [SerializeField] private AnimationCurve arcCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // ========== NOWE: Collision detection ==========
        [Header("Collision")]
        [SerializeField] private float collisionCheckRadius = 0.5f; // How close to trigger hit
        // ===============================================

        private Vector3 startPosition;
        private float journeyLength;
        private float distanceTraveled;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            startPosition = transform.position;
            journeyLength = Vector3.Distance(startPosition, targetPosition);
            distanceTraveled = 0f;
        }

        protected override void UpdateMovement()
        {
            if (journeyLength <= 0.01f) return;

            // Calculate progress (0-1)
            distanceTraveled += speed * Time.deltaTime;
            float progress = Mathf.Clamp01(distanceTraveled / journeyLength);

            // Linear interpolation for horizontal movement
            Vector3 horizontalPos = Vector3.Lerp(startPosition, targetPosition, progress);

            // Arc curve for vertical movement
            float arcOffset = arcCurve.Evaluate(progress) * arcHeight;

            // Combine horizontal + vertical
            transform.position = horizontalPos + Vector3.up * arcOffset;

            // ========== NOWE: Mid-flight collision check ==========
            CheckMidFlightCollision();
            // =====================================================

            // Rotate to face movement direction (optional)
            if (progress < 0.99f)
            {
                float nextProgress = Mathf.Clamp01((distanceTraveled + 0.1f) / journeyLength);
                Vector3 nextHorizontal = Vector3.Lerp(startPosition, targetPosition, nextProgress);
                float nextArc = arcCurve.Evaluate(nextProgress) * arcHeight;
                Vector3 nextPos = nextHorizontal + Vector3.up * nextArc;

                Vector3 direction = (nextPos - transform.position).normalized;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }

            // Check if reached target
            if (progress >= 1f)
            {
                // Force hit at target position
                if (target != null)
                {
                    OnHitTarget(target);
                }
                else
                {
                    ReturnToPool();
                }
            }
        }

        // ========== NOWA FUNKCJA ==========
        /// <summary>
        /// Checks for collision during flight (prevents missing moving targets)
        /// </summary>
        private void CheckMidFlightCollision()
        {
            if (hasHit) return;

            // Check sphere around projectile
            Collider[] hits = Physics.OverlapSphere(transform.position, collisionCheckRadius);

            foreach (Collider hit in hits)
            {
                EnemyHealth enemy = hit.GetComponent<EnemyHealth>();

                // If we hit our target (or any enemy if target died)
                if (enemy != null && (enemy == target || target == null))
                {
                    OnHitTarget(enemy);
                    return;
                }
            }
        }
        // ==================================

        protected override void CheckCollision()
        {
            // Arc projectiles use CheckMidFlightCollision() instead
        }

        // ========== NOWE: Debug visualization ==========
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collisionCheckRadius);
        }
        // ===============================================
    }
}