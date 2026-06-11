using UnityEngine;
using Photon.Pun;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Hides element colors on enemy healthbars for one wave.
    /// Player has to remember which type each enemy is.
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_ElementMask",
        menuName = "Tower Defense/Cards/Sabotages/Wave/Element Mask")]
    public class ElementMaskSabotage : SabotageEffectBase
    {
        public override void Apply(PhotonView target, PhotonView caster)
        {
            var wm = GetWaveManager(target);
            wm?.ApplyWaveModifiers(m => m.hideElementColors = true);
            LogSabotage(target, caster, "Element colors hidden for next wave");
        }
        // No Remove needed — flag is reset by WaveManager between waves.
    }
}
