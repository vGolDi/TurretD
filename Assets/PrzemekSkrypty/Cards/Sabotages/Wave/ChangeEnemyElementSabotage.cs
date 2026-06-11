using UnityEngine;
using Photon.Pun;
using ElementumDefense.Elements;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Changes enemy element for next wave
    /// Tag: Enemies, Duration: Temporary (1 round)
    /// </summary>
    [CreateAssetMenu(fileName = "Sabotage_ChangeElement", menuName = "Tower Defense/Cards/Sabotages/Wave/Change Enemy Element")]
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

            WaveManager targetWaveManager = GetWaveManager(targetPhotonView);
            
            if (targetWaveManager != null)
            {
                targetWaveManager.ApplyWaveModifiers(mod =>
                {
                    mod.overrideElement = true;
                    mod.newElement = chosenElement;
                });
                
                LogSabotage(targetPhotonView, casterPhotonView, $"Next wave changed to {chosenElement}");
            }
            else
            {
                Debug.LogError("[ChangeEnemyElementSabotage] Could not find WaveManager on target!");
            }
        }

        public override void Remove(PhotonView targetPhotonView, PhotonView casterPhotonView)
        {
            // Automatic - wave ends
        }

        private ElementType GetRandomElement()
        {
            ElementType[] elements = new[]
            {
                ElementType.Fire, ElementType.Ice, ElementType.Lightning,
                ElementType.Nature, ElementType.Dark, ElementType.Light
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