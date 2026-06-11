using System.Collections;
using ElementumDefense.UI;
using UnityEngine;

namespace ElementumDefense.Waves
{
    /// <summary>
    /// Spawns a wave, waits for all enemies to die, pays the completion bonus,
    /// resets per-wave modifiers, then synchronizes with other players via
    /// Photon custom properties.
    /// 
    /// Transitions:
    ///  - Last wave   -> <see cref="MayhemState"/> (post-game / endless check)
    ///  - More waves  -> <see cref="WaveDraftState"/> for the next index.
    /// </summary>
    public class WaveSpawnState : IWaveState
    {
        private const float BarrierTimeoutSeconds = 120f;

        private readonly int waveIndex;

        public WaveSpawnState(int waveIndex)
        {
            this.waveIndex = waveIndex;
        }

        public IEnumerator Run(WaveStateMachine machine)
        {
            WaveManager wm = machine.Wave;
            WaveData currentWave = wm.GetWaveData(waveIndex);

            // ----- Reconnect save point (c): combat start of this wave. -----
            // Captures the new wave index so a disconnect mid-combat resumes here.
            ElementumDefense.Multiplayer.Reconnect.MatchSnapshotService.Instance?.CaptureAndSave($"combat-start wave {waveIndex}");

            // Spawn enemies + bonus enemies (sabotage bosses) for this wave.
            yield return wm.SpawnWaveCoroutine(currentWave);

            // Wait for every enemy to die (or reach the goal — that decrements too).
            yield return new WaitUntil(() => wm.EnemiesAlive <= 0);

            WaveHUD.Instance?.HideSpawnProgress();

            wm.NotifyCardManagerWaveCompleted();
            wm.PayWaveCompletionBonus(currentWave);

            // Reset modifiers BEFORE next-wave sync so any NoBuildZone etc. is gone.
            wm.ResetActiveModifiers();

            // Photon: announce that we finished this wave, then wait for the rest.
            wm.MarkLocalWaveComplete(waveIndex);

            int totalWaves = wm.GetTotalWaves();
            bool moreWavesAhead = waveIndex < totalWaves - 1;

            if (moreWavesAhead)
            {
                WaveHUD.Instance?.ShowWaitingMessage("WAITING FOR OTHER PLAYER...");

                // Barrier with timeout: a disconnected opponent must not stall us
                // forever. MatchOpponentWatcher awards victory on their hard leave /
                // grace expiry; if that hasn't fired by the cap, we proceed anyway.
                float timeout = BarrierTimeoutSeconds;
                while (!wm.AreAllPlayersOnWave(waveIndex) && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }
                if (timeout <= 0f)
                    Debug.LogWarning("[WaveSpawnState] Barrier timeout — proceeding without opponent sync.");

                WaveHUD.Instance?.HideWaitingMessage();
            }

            // Per-wave delay (countdown to next).
            yield return new WaitForSeconds(currentWave.delayAfterWave);

            if (moreWavesAhead)
            {
                machine.GoTo(new WaveDraftState(waveIndex + 1));
            }
            else
            {
                wm.MarkAllNormalWavesComplete();
                machine.GoTo(new MayhemState());
            }
        }
    }
}
