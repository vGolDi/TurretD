using UnityEngine;
using Photon.Pun;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Inflates turret cost over multiple waves (or seconds).
    /// Same mechanic as TowerTax but typically a stronger / longer variant.
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_Inflation",
        menuName = "Tower Defense/Cards/Sabotages/Opponent/Inflation")]
    public class InflationSabotage : SabotageEffectBase
    {
        [Range(1f, 3f), Tooltip("Cost multiplier (1.5 = +50%)")]
        public float costMultiplier = 1.5f;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var stack = target?.GetComponent<PlayerModifierStack>();
            if (stack == null) return;
            stack.ApplyById(MakeId(target, caster), PlayerModifierStack.SabotageStat.Cost, costMultiplier);
            LogSabotage(target, caster, $"Inflation: x{costMultiplier:F2} cost");
        }

        public override void Remove(PhotonView target, PhotonView caster)
        {
            var stack = target?.GetComponent<PlayerModifierStack>();
            stack?.RemoveById(MakeId(target, caster), PlayerModifierStack.SabotageStat.Cost);
        }

        private string MakeId(PhotonView t, PhotonView c) => $"Inflation_{t.ViewID}_{(c != null ? c.ViewID : 0)}";
    }
}
