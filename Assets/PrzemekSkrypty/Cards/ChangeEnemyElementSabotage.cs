using UnityEngine;
using Photon.Pun;
using ElementumDefense.Elements;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Changes enemy element for next wave
    /// Tag: Enemies, Duration: Temporary (1 round)
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_ChangeElement", menuName = "Tower Defense/Cards/Sabotage/Change Enemy Element")]
    public class ChangeEnemyElementSabotage : SabotageEffectBase
    {
        [Header("Element Override")]
        public ElementType newElement = ElementType.Fire;

        [Tooltip("Random element? (ignores newElement)")]
        public bool randomElement = true;

        public override void Apply(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            ElementType chosenElement = randomElement
                ? GetRandomElement()
                : newElement;

            // TODO: WaveManager (target's arena).OverrideNextWaveElement(chosenElement);
            LogSabotage(targetPhotonView, casterPhotonView, $"Next wave changed to {chosenElement}");
        }

        public override void Remove(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            // Automatic - wave ends
        }

        private ElementType GetRandomElement()
        {
            ElementType[] elements = new[]
            {
                ElementType.Fire, ElementType.Water, ElementType.Ice,
                ElementType.Earth, ElementType.Lightning, ElementType.Nature
            };
            return elements[Random.Range(0, elements.Length)];
        }

        public override string GetEffectDescription()
        {
            if (randomElement)
                return "🎲 Next enemy wave changes to RANDOM element";

            return $"🔥 Next enemy wave becomes {newElement}";
        }
    }
}