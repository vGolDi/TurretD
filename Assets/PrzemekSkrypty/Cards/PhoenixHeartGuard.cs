using UnityEngine;
using Photon.Pun;
using ElementumDefense.Players;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Runtime guard for <see cref="PhoenixHeartEffect"/>.
    /// 
    /// Subscribes to <see cref="PlayerHealth.OnLethalDamage"/> — fires the
    /// moment HP would hit 0, BEFORE the death RPC. Heals the player to
    /// <see cref="reviveHp"/>, consuming one charge. PlayerHealth re-checks
    /// HP after the hook so death is canceled and the network sync sends the
    /// post-revive HP, keeping both clients in sync.
    /// </summary>
    [RequireComponent(typeof(PlayerHealth))]
    public class PhoenixHeartGuard : MonoBehaviour
    {
        private PlayerHealth health;
        private int charges = 0;
        private int reviveHp = 10;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
        }

        private void OnEnable()
        {
            if (health != null) health.OnLethalDamage += TryConsume;
        }

        private void OnDisable()
        {
            if (health != null) health.OnLethalDamage -= TryConsume;
        }

        public void AddCharges(int amount, int hpOnRevive)
        {
            charges += amount;
            // If two PhoenixHeart cards stack, use the highest revive HP.
            reviveHp = Mathf.Max(reviveHp, hpOnRevive);
        }

        public int RemainingCharges => charges;

        private void TryConsume()
        {
            if (charges <= 0 || health == null) return;
            charges--;
            // Heal handles RPC sync to opponent so they see the rebound HP
            // before the lethal-damage frame finishes.
            health.Heal(reviveHp);
            Debug.Log($"[PhoenixHeart] Revived {gameObject.name} at {reviveHp} HP " +
                      $"({charges} charge(s) left)");
        }
    }
}
