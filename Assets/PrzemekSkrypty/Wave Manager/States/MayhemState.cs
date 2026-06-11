using System.Collections;
using ElementumDefense.UI;
using UnityEngine;
using ElementumDefense.Players;

namespace ElementumDefense.Waves
{
    /// <summary>
    /// Endless / post-game phase. Triggered after the last normal wave.
    /// 
    /// Flow:
    ///  1. If the local player is dead OR no Mayhem wave is configured -> exit.
    ///  2. Wait until every player has finished their last wave (sync barrier).
    ///  3. If anyone died during normal waves -> exit (no Mayhem).
    ///  4. Pay bonus gold, run draft, show announcement, spawn endless wave.
    ///  5. Wait for all enemies to die (or game-end via PlayerHealth.IsDead).
    /// </summary>
    public class MayhemState : IWaveState
    {
        public IEnumerator Run(WaveStateMachine machine)
        {
            WaveManager wm = machine.Wave;

            if (!wm.HasMayhemWave())
            {
                Debug.Log("[MayhemState] No Mayhem wave configured — flow ends.");
                yield break;
            }

            if (wm.IsLocalPlayerDead())
            {
                Debug.Log("[MayhemState] Local player dead — no Mayhem.");
                yield break;
            }

            Debug.Log("[MayhemState] Waiting for all players to finish their waves…");
            WaveHUD.Instance?.ShowWaitingMessage("WAITING FOR OTHER PLAYERS...");
            yield return new WaitUntil(() => wm.AreAllPlayersWavesComplete());
            WaveHUD.Instance?.HideWaitingMessage();

            if (!wm.BothPlayersAlive())
            {
                Debug.Log("[MayhemState] Someone died during waves — no Mayhem.");
                yield break;
            }

            Debug.Log("[MayhemState] Both players alive — Mayhem starts!");

            WaveHUD.Instance?.HideAllWavesComplete();

            wm.PayMayhemBonusGold();

            // Run draft synchronously without queueing a follow-up state.
            yield return WaveDraftState.RunDraftLogic(wm, wm.GetTotalWaves());

            var hud = WaveHUD.Instance;
            if (hud != null)
                yield return hud.ShowMayhemAnnouncement(wm.WaveAnnounceDuration);

            hud?.SetMayhemBadge();

            wm.BeginMayhem();

            yield return wm.SpawnWaveCoroutine(wm.GetMayhemWave());

            yield return new WaitUntil(() => wm.EnemiesAlive <= 0);

            wm.EndMayhem();
            hud?.HideSpawnProgress();

            Debug.Log("[MayhemState] Mayhem finished — both players survived.");
        }
    }
}
