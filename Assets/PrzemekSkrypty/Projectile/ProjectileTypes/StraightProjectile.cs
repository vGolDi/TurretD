using UnityEngine;

namespace ElementumDefense.Projectiles
{
    /// <summary>
    /// Simple projectile that flies straight forward
    /// Good for: Fast turrets, machine guns, basic attacks
    /// </summary>
    public class StraightProjectile : Projectile
    {
        protected override void UpdateMovement()
        {
            Vector3 movement = transform.forward * speed * Time.deltaTime;
            transform.position += movement;

            // DEBUG: Draw ray showing flight path
            Debug.DrawRay(transform.position, transform.forward * 2f, Color.red, 0.1f);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Point towards initial target position
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

    }

}