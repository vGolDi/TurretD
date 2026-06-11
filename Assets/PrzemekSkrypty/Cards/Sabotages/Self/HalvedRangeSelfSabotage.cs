using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// SELF: Towers have -30% range but +60% fire rate for one wave.
    /// Forces tighter / more clustered placement.
    /// </summary>
    [CreateAssetMenu(fileName = "SelfSabotage_HalvedRange",
        menuName = "Tower Defense/Cards/Sabotages/Self/Halved Range")]
    public class HalvedRangeSelfSabotage : SabotageEffectBase
    {
        [Range(0.4f, 1f)] public float rangeMultiplier = 0.7f;
        [Range(1f, 2f)] public float fireRateMultiplier = 1.6f;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var stack = target?.GetComponent<PlayerModifierStack>();
            if (stack == null) return;
            stack.ApplyById(MakeId(target, caster, "R"), PlayerModifierStack.SabotageStat.Range, rangeMultiplier);
            stack.ApplyById(MakeId(target, caster, "F"), PlayerModifierStack.SabotageStat.FireRate, fireRateMultiplier);
            LogSabotage(target, caster,
                $"Halved Range: range x{rangeMultiplier}, FR x{fireRateMultiplier}");
        }

        public override void Remove(PhotonView target, PhotonView caster)
        {
            var stack = target?.GetComponent<PlayerModifierStack>();
            stack?.RemoveById(MakeId(target, caster, "R"), PlayerModifierStack.SabotageStat.Range);
            stack?.RemoveById(MakeId(target, caster, "F"), PlayerModifierStack.SabotageStat.FireRate);
        }

        private string MakeId(PhotonView t, PhotonView c, string slot)
            => $"HalvedRange_{slot}_{t.ViewID}_{(c != null ? c.ViewID : 0)}";
    }
}
