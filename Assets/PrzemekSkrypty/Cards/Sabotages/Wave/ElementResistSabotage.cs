using UnityEngine;
using Photon.Pun;
using ElementumDefense.Elements;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// One element does reduced damage for one wave.
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_ElementResist",
        menuName = "Tower Defense/Cards/Sabotages/Wave/Element Resist")]
    public class ElementResistSabotage : SabotageEffectBase
    {
        [Tooltip("Which element gets resisted")]
        public ElementType resistedElement = ElementType.Fire;

        [Range(0.1f, 1f), Tooltip("Damage multiplier (0.5 = takes half damage from this element)")]
        public float damageMultiplier = 0.5f;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            var wm = GetWaveManager(target);
            wm?.ApplyWaveModifiers(m =>
            {
                m.useEnemyResistElement = true;
                m.resistElement = resistedElement;
                m.resistMultiplier = damageMultiplier;
            });
            LogSabotage(target, caster, $"Enemies resist {resistedElement} x{damageMultiplier:F2}");
        }
    }
}
