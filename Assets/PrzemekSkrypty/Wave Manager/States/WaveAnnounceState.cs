using System.Collections;
using ElementumDefense.UI;

namespace ElementumDefense.Waves
{
    /// <summary>
    /// "Wave X / Y" HUD announcement. Updates wave badge, runs the announcement
    /// coroutine on WaveHUD, then resets spawn progress and hands off to
    /// <see cref="WaveSpawnState"/>.
    /// </summary>
    public class WaveAnnounceState : IWaveState
    {
        private readonly int waveIndex;

        public WaveAnnounceState(int waveIndex)
        {
            this.waveIndex = waveIndex;
        }

        public IEnumerator Run(WaveStateMachine machine)
        {
            WaveManager wm = machine.Wave;

            wm.PrepareWaveCounters(waveIndex);
            wm.UpdateWaveBadge();

            var hud = WaveHUD.Instance;
            if (hud != null)
            {
                yield return hud.ShowWaveAnnouncement(
                    waveIndex + 1,
                    wm.GetTotalWaves(),
                    wm.WaveAnnounceDuration);
            }

            wm.UpdateSpawnProgress();

            machine.GoTo(new WaveSpawnState(waveIndex));
        }
    }
}
