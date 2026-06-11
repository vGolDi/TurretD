using UnityEngine;
using Photon.Pun;
using ElementumDefense.Players;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Legendary "death save" — when player HP would drop to 0, intercept
    /// and restore <see cref="reviveHp"/>. Consumed once per match.
    /// 
    /// Wired up by <see cref="PlayerCardActivator"/> on Activate by attaching
    /// <see cref="PhoenixHeartGuard"/> to the player. PhoenixHeartGuard hooks
    /// into PlayerHealth.OnPlayerDied (or pre-empts via TakeDamage interception).
    /// 
    /// We don't permanently raise max HP here — that would unbalance the
    /// pseudo-PvP ranking system.
    /// </summary>
    [CreateAssetMenu(fileName = "PhoenixHeart_Effect", menuName = "Tower Defense/Cards/Effects/Phoenix Heart (Revive)")]
    public class PhoenixHeartEffect : CardEffectBase
    {
        [Header("Revive")]
        [Tooltip("HP restored when triggered.")]
        [Min(1)]
        public int reviveHp = 10;

        [Tooltip("How many times this can fire per match. 1 = classic Phoenix.")]
        [Min(1)]
        public int maxRevives = 1;

        public override void Activate(PhotonView ownerPhotonView)
        {
            if (ownerPhotonView == null) return;

            // Attach a runtime guard component if not already there.
            var guard = ownerPhotonView.GetComponent<PhoenixHeartGuard>();
            if (guard == null)
                guard = ownerPhotonView.gameObject.AddComponent<PhoenixHeartGuard>();

            guard.AddCharges(maxRevives, reviveHp);
            LogActivation(ownerPhotonView, $"+{maxRevives} revive(s) at {reviveHp} HP");
        }

        public override void Deactivate(PhotonView ownerPhotonView)
        {
            // Match end: nothing to clean up — guard sits on the player object
            // and gets destroyed with the rest of the player.
        }

        public override string GetEffectDescription()
        {
            string mult = maxRevives > 1 ? $" ×{maxRevives}" : "";
            return $"🪶 On lethal damage{mult}: revive at {reviveHp} HP\n" +
                   $"<i>(once per match)</i>";
        }
    }
}
