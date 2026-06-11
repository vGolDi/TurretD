using System.Collections;
using UnityEngine;

namespace ElementumDefense.Waves
{
    /// <summary>
    /// Mid-game draft phase. Pauses spawning, asks the DraftManager and
    /// SabotageDraftManager to offer choices, waits until each finishes
    /// (or its timeout fires), then transitions to the next wave's announcement.
    /// 
    /// Skipped on the first wave (waveIndex == 0) — that's the starter draft,
    /// handled outside WaveManager by DraftManager.StartStarterDraft.
    /// </summary>
    public class WaveDraftState : IWaveState
    {
        private const float DraftTimeoutSeconds = 120f;

        private readonly int waveIndex;

        public WaveDraftState(int waveIndex)
        {
            this.waveIndex = waveIndex;
        }

        public IEnumerator Run(WaveStateMachine machine)
        {
            WaveManager wm = machine.Wave;

            // First wave never has a mid-game draft.
            if (waveIndex == 0)
            {
                machine.GoTo(new WaveAnnounceState(waveIndex));
                yield break;
            }

            // Anchor the current wave for any snapshot saved during this draft,
            // so a disconnect mid-draft resumes at THIS wave, not the previous one.
            wm.SetCurrentWaveForDraft(waveIndex);

            // Save point at draft start: if the player disconnects mid-draft
            // (before picking), restore resumes at THIS wave and skips its draft,
            // rather than replaying the previous wave and re-entering a draft the
            // opponent already finished (which would hang on rarity sync).
            ElementumDefense.Multiplayer.Reconnect.MatchSnapshotService.Instance?.CaptureAndSave($"draft-start wave {waveIndex}");

            yield return RunDraftLogic(wm, waveIndex);

            machine.GoTo(new WaveAnnounceState(waveIndex));
        }

        /// <summary>
        /// Standalone draft execution — used by MayhemState which runs its
        /// own announcement flow and doesn't want a WaveAnnounceState queued.
        /// </summary>
        public static IEnumerator RunDraftLogic(WaveManager wm, int waveIndex)
        {
            // ----- Card draft -----
            var draftMgr = wm.ResolveDraftManager();
            if (draftMgr != null)
            {
                Debug.Log($"[WaveDraftState] Wave {waveIndex}: checking mid-game draft");
                draftMgr.CheckMidGameDraft(waveIndex);

                float timeout = DraftTimeoutSeconds;
                while (draftMgr.IsDrafting && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }

                if (timeout <= 0f)
                    Debug.LogError("[WaveDraftState] Card draft timeout!");
            }

            // ----- Sabotage draft -----
            var sabotageMgr = wm.ResolveSabotageDraftManager();
            if (sabotageMgr != null)
            {
                sabotageMgr.CheckSabotageDraft(waveIndex);

                float timeout = DraftTimeoutSeconds;
                while (sabotageMgr.IsDrafting && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }

                if (timeout <= 0f)
                    Debug.LogError("[WaveDraftState] Sabotage draft timeout!");
            }
        }
    }
}
