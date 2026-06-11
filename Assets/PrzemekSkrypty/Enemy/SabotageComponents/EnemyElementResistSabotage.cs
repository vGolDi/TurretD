using UnityEngine;
using ElementumDefense.Elements;

namespace ElementumDefense.Enemies
{
    /// <summary>
    /// One-element resist applied at spawn for the duration of a wave. Read
    /// by EnemyHealth.TakeDamage to multiply damage when the incoming element
    /// matches.
    /// </summary>
    public class EnemyElementResistSabotage : MonoBehaviour, IEnemyPoolable
    {
        private ElementType resistedElement = ElementType.None;
        private float damageMultiplier = 0.5f; // 0.5 = takes half damage

        public ElementType ResistedElement => resistedElement;
        public float DamageMultiplier => damageMultiplier;

        public void SetResist(ElementType element, float multiplier)
        {
            resistedElement = element;
            damageMultiplier = Mathf.Clamp(multiplier, 0f, 2f);
        }

        public void OnSpawnedFromPool() { }
        public void OnReturnedToPool()
        {
            resistedElement = ElementType.None;
            damageMultiplier = 0.5f;
        }
    }
}
