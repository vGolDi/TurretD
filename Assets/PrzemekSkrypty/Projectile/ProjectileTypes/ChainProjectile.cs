using UnityEngine;
using System.Collections.Generic;

namespace ElementumDefense.Projectiles
{
    /// <summary>
    /// Bouncing projectile that hits one target, then instantly 
    /// changes trajectory towards the next nearest target.
    /// Good for: Chain Lightning, Glaives, Bouncing lasers.
    /// </summary>
    public class ChainProjectile : Projectile
    {
        [Header("Chain Settings")]
        [Tooltip("Maximum number of enemies it can bounce to")]
        [SerializeField] private int maxBounces = 3;
        
        [Tooltip("How far it can search for the next enemy")]
        [SerializeField] private float bounceRadius = 8f;
        
        [Tooltip("How much damage is lost per bounce (0.2 = 20% loss)")]
        [Range(0f, 1f)]
        [SerializeField] private float damageFalloff = 0.2f;

        [Tooltip("Turn speed for the projectile seeking next target")]
        [SerializeField] private float rotationSpeed = 360f;

        private int currentBounces;
        
        // Zapobiega uderzaniu tego samego wroga wielokrotnie w jednym "łańcuchu"
        private HashSet<EnemyHealth> hitEnemies = new HashSet<EnemyHealth>();

        protected override void OnInitialized()
        {
            base.OnInitialized();
            currentBounces = 0;
            hitEnemies.Clear();
            
            // Obróć w stronę początkowego celu
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        protected override void UpdateMovement()
        {
            Vector3 targetPos = GetTargetPosition();
            Vector3 direction = (targetPos - transform.position).normalized;

            if (target == null)
            {
                // Jeśli cel zginie w trakcie lotu, leć prosto (może coś trafisz po drodze)
                transform.position += transform.forward * speed * Time.deltaTime;
                return;
            }

            // Płynny obrót w stronę nowego celu po odbiciu
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            transform.position += transform.forward * speed * Time.deltaTime;
        }

        /// <summary>
        /// Nadpisujemy całkowicie bazową metodę OnHitTarget, 
        /// ponieważ nie chcemy niszczyć pocisku po pierwszym trafieniu (hasHit).
        /// </summary>
        protected override void OnHitTarget(EnemyHealth enemy)
        {
            // Ignoruj jeśli już dostaliśmy od tego wroga w obecnym łańcuchu
            if (hitEnemies.Contains(enemy)) return;

            // Rejestracja trafienia w statystykach
            if (ProjectileStatsManager.Instance != null)
            {
                ProjectileStatsManager.Instance.RegisterHit();
            }

            // Zadaj obrażenia i zastosuj efekty statusów
            enemy.TakeDamage(damage, -1, elementType);
            
            if (statusChance > 0f && Random.Range(0f, 100f) <= statusChance)
            {
                ApplyStatusEffect(enemy);
            }

            SpawnImpactEffect();

            // Odnotuj trafienie
            hitEnemies.Add(enemy);
            currentBounces++;

            Debug.Log($"[ChainProjectile] Bounced to {enemy.name} (Bounce {currentBounces}/{maxBounces})");

            // Sprawdź czy to był ostatni skok
            if (currentBounces >= maxBounces)
            {
                EndProjectile();
                return;
            }

            // Szukaj następnego wroga
            EnemyHealth nextTarget = FindNextTarget(enemy.transform.position);

            if (nextTarget != null)
            {
                // Ustaw nowy cel
                target = nextTarget;
                targetPosition = target.transform.position;

                // Redukcja obrażeń (Falloff) dla kolejnego skoku
                damage = Mathf.RoundToInt(damage * (1f - damageFalloff));
                
                // Opcjonalne: resetujemy czas życia, żeby pocisk zdążył dolecieć
                currentLifetime = 0f; 
            }
            else
            {
                // Brak kolejnych celów w zasięgu
                EndProjectile();
            }
        }

        private void EndProjectile()
        {
            hasHit = true;
            ReturnToPool();
        }

        /// <summary>
        /// Szuka najbliższego wroga, który jeszcze nie dostał z łańcucha
        /// </summary>
        private EnemyHealth FindNextTarget(Vector3 currentPos)
        {
            Collider[] hits = Physics.OverlapSphere(currentPos, bounceRadius);
            EnemyHealth nearest = null;
            float minDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                EnemyHealth e = hit.GetComponent<EnemyHealth>();
                if (e != null && !hitEnemies.Contains(e))
                {
                    float dist = Vector3.Distance(currentPos, e.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = e;
                    }
                }
            }

            return nearest;
        }
    }
}
