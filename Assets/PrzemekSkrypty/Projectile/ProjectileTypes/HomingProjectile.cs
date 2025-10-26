using UnityEngine;

namespace ElementumDefense.Projectiles
{
    /// <summary>
    /// Projectile that tracks and follows target
    /// Good for: Missiles, magic bolts, seeking attacks
    /// </summary>
    public class HomingProjectile : Projectile
    {
        [Header("Homing Settings")]
        [SerializeField] private float rotationSpeed = 200f; // How fast it turns
        [SerializeField] private float homingStrength = 1f; // 0-1, how much it homes

        protected override void UpdateMovement()
        {
            // Get target direction
            Vector3 targetPos = GetTargetPosition();
            Vector3 direction = (targetPos - transform.position).normalized;

            // ========== DODAJ: Check if target still exists ==========
            if (target == null)
            {
                // Fly straight if target died
                transform.position += transform.forward * speed * Time.deltaTime;
                return;
            }
            // =========================================================

            // Blend between straight and homing
            Vector3 desiredDirection = Vector3.Lerp(transform.forward, direction, homingStrength);

            // Rotate towards target
            if (desiredDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(desiredDirection);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
            }

            // Move forward
            transform.position += transform.forward * speed * Time.deltaTime;
        }
    }
}

