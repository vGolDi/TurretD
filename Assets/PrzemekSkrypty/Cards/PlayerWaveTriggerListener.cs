using UnityEngine;
using Photon.Pun;
using ElementumDefense.Waves;

namespace ElementumDefense.Cards
{
    /// <summary>
    /// Bridges WaveManager wave-start events to active <see cref="WaveTriggerEffect"/>
    /// cards on this player.
    /// 
    /// We poll WaveManager's GetCurrentWaveIndex once per second instead of
    /// hooking into WaveStateMachine because the state machine flow is shared
    /// between hosts/clients and we don't want every state to know about cards.
    /// 
    /// The bridge lives on the player so it has direct access to its activator.
    /// </summary>
    [RequireComponent(typeof(PlayerCardActivator))]
    public class PlayerWaveTriggerListener : MonoBehaviour
    {
        private PlayerCardActivator activator;
        private PhotonView photonView;
        private int lastTriggeredWave = -1;

        private void Awake()
        {
            activator = GetComponent<PlayerCardActivator>();
            photonView = GetComponent<PhotonView>();
        }

        private void Update()
        {
            // Local player only — no need to fire payouts on the remote view.
            if (photonView != null && !photonView.IsMine) return;

            // Find the nearest WaveManager (cheap; cached lookup not needed at 1Hz).
            // Scope: WaveManager.Instance pattern doesn't exist; FindAnyObjectByType
            // is fine here since this runs once a wave change.
            var waveMgr = FindWaveManager();
            if (waveMgr == null) return;

            int wave = waveMgr.GetCurrentWaveIndex() + 1; // 1-based for designer-friendly N
            if (wave == lastTriggeredWave) return;
            lastTriggeredWave = wave;

            var cards = activator.ActiveCards;
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i]?.cardEffect is WaveTriggerEffect wte)
                {
                    wte.OnWaveStarted(wave, photonView);
                }
            }
        }

        private WaveManager cachedWaveManager;
        private WaveManager FindWaveManager()
        {
            if (cachedWaveManager != null) return cachedWaveManager;
            cachedWaveManager = FindAnyObjectByType<WaveManager>();
            return cachedWaveManager;
        }
    }
}
