using UnityEngine;
using Photon.Pun;
using ElementumDefense.Players;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Drains gold from the target every second. Pure income drain — doesn't
    /// affect kill rewards. Use a Time-based duration on the SabotageCardData
    /// asset (e.g. 30s) or DurationRounds = 1.
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_Tax",
        menuName = "Tower Defense/Cards/Sabotages/Opponent/Tax")]
    public class TaxSabotage : SabotageEffectBase
    {
        [Header("Tax Settings")]
        [Min(1), Tooltip("Gold drained per tick interval")]
        public int goldPerTick = 5;

        [Min(0.1f), Tooltip("Seconds between drain ticks")]
        public float tickInterval = 1f;

        // Per-target tick accumulator. Multiple targets => one entry each.
        private readonly System.Collections.Generic.Dictionary<int, float> tickTimers
            = new System.Collections.Generic.Dictionary<int, float>();

        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            if (targetPhotonView == null) return;
            tickTimers[targetPhotonView.ViewID] = 0f;
            LogSabotage(targetPhotonView, casterPhotonView,
                $"Tax: -{goldPerTick} gold every {tickInterval}s");
        }

        public override void OnUpdate(PhotonView targetPhotonView, float deltaTime)
        {
            if (targetPhotonView == null) return;
            int id = targetPhotonView.ViewID;
            if (!tickTimers.TryGetValue(id, out float t)) return;

            t += deltaTime;
            if (t >= tickInterval)
            {
                var gold = GetPlayerGold(targetPhotonView);
                gold?.AddGold(-goldPerTick);
                t -= tickInterval;
            }
            tickTimers[id] = t;
        }

        public override void Remove(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            if (targetPhotonView == null) return;
            tickTimers.Remove(targetPhotonView.ViewID);
        }
    }
}
