using UnityEngine;
using Photon.Pun;
using ElementumDefense.Elements;
using ElementumDefense.Waves;
using ElementumDefense.Players;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Blocks one element from being built for the next wave AND optionally
    /// forces all enemies in that wave to be the OPPOSING element so the
    /// player is naturally weak against them — encourages build flexibility.
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_ElementBlock",
        menuName = "Tower Defense/Cards/Sabotages/Wave/Element Block")]
    public class ElementBlockSabotage : SabotageEffectBase
    {
        [Tooltip("Element that cannot be built")]
        public ElementType blockedElement = ElementType.Ice;

        [Tooltip("Force enemies of the next wave to this element (their weakness will match the blocked one).")]
        public bool overrideEnemyElement = true;
        public ElementType enemyElementOverride = ElementType.Fire;

        public override void Apply(PhotonView target, PhotonView caster)
        {
            // Block the element on BuildManager
            var build = target?.GetComponent<BuildManager>();
            build?.SetElementBlocked(blockedElement, true);

            // Override enemy element for next wave
            if (overrideEnemyElement)
            {
                var wm = GetWaveManager(target);
                wm?.ApplyWaveModifiers(m =>
                {
                    m.overrideElement = true;
                    m.newElement = enemyElementOverride;
                });
            }

            LogSabotage(target, caster, $"Element {blockedElement} blocked, enemies become {enemyElementOverride}");
        }

        public override void Remove(PhotonView target, PhotonView caster)
        {
            var build = target?.GetComponent<BuildManager>();
            build?.SetElementBlocked(blockedElement, false);
        }
    }
}
