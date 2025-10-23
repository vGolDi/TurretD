using UnityEngine;

namespace ElementumDefense.Projectiles
{
    /// <summary>
    /// Projectile that flies in an arc (parabola)
    /// Good for: Mortars, catapults, artillery
    /// </summary>
    public class ArcProjectile : Projectile
    {
        [Header("Arc Settings")]
        [SerializeField] private float arcHeight = 3f; // Peak height of arc
        [SerializeField] private AnimationCurve arcCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

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

        protected override void CheckCollision()
        {
            // Arc projectiles don't use OnTriggerEnter
            // They hit when reaching target position
        }
    }
}