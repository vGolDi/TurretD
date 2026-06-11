using System.Collections;
using UnityEngine;

namespace ElementumDefense.Waves
{
    /// <summary>
    /// Drives a sequence of <see cref="IWaveState"/> instances using a single
    /// coroutine on the host <see cref="WaveManager"/>. States set the next
    /// state with <see cref="GoTo"/>; the machine stops when no next state is
    /// queued.
    /// 
    /// Why a state machine? The previous flow lived in three intertwined
    /// coroutines (RunGameWaves / HandleDrafts / CheckAndStartMayhem) plus
    /// half a dozen booleans (isSpawning / isMayhemActive / normalWavesComplete).
    /// Splitting into states keeps each phase small, debuggable, and easy to
    /// extend (e.g. add a mini-boss intermission without touching wave loop).
    /// </summary>
    public class WaveStateMachine
    {
        public WaveManager Wave { get; }
        public IWaveState Current { get; private set; }

        private IWaveState next;

        public WaveStateMachine(WaveManager wave)
        {
            Wave = wave;
        }

        /// <summary>State should call this before returning from Run().</summary>
        public void GoTo(IWaveState state)
        {
            next = state;
        }

        /// <summary>
        /// Coroutine entry point — runs <paramref name="initial"/> and every
        /// state it queues until the chain terminates.
        /// </summary>
        public IEnumerator RunFrom(IWaveState initial)
        {
            Current = initial;

            while (Current != null)
            {
                next = null;
                IWaveState running = Current;

                Debug.Log($"[WaveStateMachine] Enter {running.GetType().Name}");
                yield return running.Run(this);
                Debug.Log($"[WaveStateMachine] Exit  {running.GetType().Name}");

                Current = next;
            }

            Debug.Log("[WaveStateMachine] Flow complete.");
        }
    }
}
