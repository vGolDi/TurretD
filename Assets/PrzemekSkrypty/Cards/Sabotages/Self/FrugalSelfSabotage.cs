using UnityEngine;
using Photon.Pun;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// SELF: Reduces passive gold income (from card effects) for one wave but
    /// boosts gold per kill. Pushes the player toward aggressive killing
    /// instead of waiting on income.
    /// 
    /// Implementation:
    ///  - <see cref="passiveGoldMultiplier"/> goes into PlayerModifierStack as
    ///    a SabotageStat.PassiveGold modifier (ID-based, so Remove cleanly pops).
    ///  - <see cref="killGoldMultiplier"/> stacks on the wave's goldRewardMultiplier.
    ///    WaveModifiers.Reset() handles cleanup at end of wave.
    /// 
    /// Set DurationRounds = 1 on the SabotageCardData asset. Apply runs at
    /// wave start, Remove fires when the wave ends.
    /// </summary>
    [CreateAssetMenu(fileName = "SelfSabotage_Frugal",
        menuName = "Tower Defense/Cards/Sabotages/Self/Frugal")]
    public class FrugalSelfSabotage : SabotageEffectBase
    {
        [Range(0f, 1f), Tooltip("Multiplier on passive gold income (0.5 = -50%)")]
        public float passiveGoldMultiplier = 0.5f;

        [Range(1f, 3f), Tooltip("Bonus on kill gold reward (1.5 = +50%)")]
        public float killGoldMultiplier = 1.5f;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            // 1. Passive gold cut (ID-based, removable).
            var stack = target?.GetComponent<PlayerModifierStack>();
            stack?.ApplyById(MakeId(target, caster), PlayerModifierStack.SabotageStat.PassiveGold, passiveGoldMultiplier);

            // 2. Kill gold boost for this wave.
            var wm = GetWaveManager(target);
            wm?.ApplyWaveModifiers(m => m.goldRewardMultiplier *= killGoldMultiplier);

            LogSabotage(target, caster,
                $"Frugal: passive x{passiveGoldMultiplier:F2}, kills x{killGoldMultiplier:F2}");
        }

        public override void Remove(PhotonView target, PhotonView caster)
        {
            var stack = target?.GetComponent<PlayerModifierStack>();
            stack?.RemoveById(MakeId(target, caster), PlayerModifierStack.SabotageStat.PassiveGold);
            // killGoldMultiplier auto-resets via WaveModifiers.Reset() at wave end.
        }

        private string MakeId(PhotonView t, PhotonView c)
            => $"Frugal_{t.ViewID}_{(c != null ? c.ViewID : 0)}";
    }
}
