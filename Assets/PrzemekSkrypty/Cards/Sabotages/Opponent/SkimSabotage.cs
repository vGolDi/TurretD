using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// DOT-style gold drain — skims % of current gold every tick.
    /// Punishes hoarding (more gold = more drained).
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_Skim",
        menuName = "Tower Defense/Cards/Sabotages/Opponent/Skim")]
    public class SkimSabotage : SabotageEffectBase
    {
        [Range(0.01f, 0.5f), Tooltip("Fraction of current gold drained each tick")]
        public float skimPercent = 0.05f;

        [Min(0.5f), Tooltip("Seconds between ticks")]
        public float tickInterval = 5f;

        private readonly System.Collections.Generic.Dictionary<int, float> tickTimers
            = new System.Collections.Generic.Dictionary<int, float>();

        public override void Apply(PhotonView target, PhotonView caster)
        {
            if (target == null) return;
            tickTimers[target.ViewID] = 0f;
            LogSabotage(target, caster, $"Skim: -{skimPercent * 100f:F0}% gold every {tickInterval}s");
        }

        public override void OnUpdate(PhotonView target, float dt)
        {
            if (target == null) return;
            int id = target.ViewID;
            if (!tickTimers.TryGetValue(id, out float t)) return;
            t += dt;
            if (t >= tickInterval)
            {
                var gold = GetPlayerGold(target);
                if (gold != null)
                {
                    int drained = Mathf.RoundToInt(gold.GetGold() * skimPercent);
                    if (drained > 0) gold.AddGold(-drained);
                }
                t -= tickInterval;
            }
            tickTimers[id] = t;
        }

        public override void Remove(PhotonView target, PhotonView caster)
        {
            if (target == null) return;
            tickTimers.Remove(target.ViewID);
        }
    }
}
